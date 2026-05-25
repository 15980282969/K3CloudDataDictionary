using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace K3CloudDataDictionary.Views
{
    /// <summary>
    /// 元数据提取结果，包含一个FID对应的完整元数据信息
    /// </summary>
    public class MetadataResult
    {
        /// <summary>对象FID标识</summary>
        public string Fid { get; set; } = "";
        /// <summary>对象类型基础信息</summary>
        public ObjectBasicInfo ObjInfo { get; set; }
        /// <summary>有oid的实体列表（可参与继承合并）</summary>
        public List<EntityInfo> EntitiesWithOid { get; set; } = new List<EntityInfo>();
        /// <summary>无oid的实体列表（新增实体）</summary>
        public List<EntityInfo> EntitiesWithoutOid { get; set; } = new List<EntityInfo>();
        /// <summary>有oid的字段列表（可参与继承合并）</summary>
        public List<MetadataFieldInfo> FieldsWithOid { get; set; } = new List<MetadataFieldInfo>();
        /// <summary>无oid的字段列表（新增字段）</summary>
        public List<MetadataFieldInfo> FieldsWithoutOid { get; set; } = new List<MetadataFieldInfo>();
        /// <summary>拆分表信息列表</summary>
        public List<SplitTableInfo> Splits { get; set; } = new List<SplitTableInfo>();

        /// <summary>
        /// 将结果输出到控制台
        /// </summary>
        public void Print()
        {
            WriteTo(Console.Out);
        }

        /// <summary>
        /// 将结果写入指定文件
        /// </summary>
        /// <param name="filePath">输出文件路径</param>
        public void WriteToFile(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                WriteTo(writer);
            }
        }

        /// <summary>
        /// 将结果写入TextWriter，包含实体、字段、拆分表的汇总信息
        /// </summary>
        /// <param name="writer">目标TextWriter</param>
        private void WriteTo(System.IO.TextWriter writer)
        {
            writer.WriteLine($"=== FID: {Fid} ===");

            writer.WriteLine($"Entities with oid ({EntitiesWithOid.Count}):");
            foreach (var entity in EntitiesWithOid)
            {
                writer.WriteLine($"  {entity.TagName}: {entity.Name} | Key={entity.Key} | TableName={entity.TableName}");
            }

            writer.WriteLine($"Entities without oid ({EntitiesWithoutOid.Count}):");
            foreach (var entity in EntitiesWithoutOid)
            {
                writer.WriteLine($"  {entity.TagName}: {entity.Name} | Key={entity.Key} | TableName={entity.TableName}");
            }

            writer.WriteLine($"Fields with oid ({FieldsWithOid.Count}):");
            foreach (var field in FieldsWithOid)
            {
                writer.WriteLine($"  {field.TagName}: {field.Name} | Key={field.Key} | FieldName={field.FieldName}");
            }

            writer.WriteLine($"Fields without oid ({FieldsWithoutOid.Count}):");
            foreach (var field in FieldsWithoutOid)
            {
                writer.WriteLine($"  {field.TagName}: {field.Name} | Key={field.Key} | FieldName={field.FieldName}");
            }

            writer.WriteLine($"Splits ({Splits.Count}):");
            foreach (var split in Splits)
            {
                writer.WriteLine($"  {split}");
            }
        }
    }

    /// <summary>
    /// 元数据提取上下文（线程安全，纯只读），一次性加载所有基础信息（不含XML），
    /// 内存构建继承链+扩展链，XML由外部按批次加载后传入ExtractByFid
    /// </summary>
    public class MetadataContext
    {
        private readonly Dictionary<string, ObjectBasicInfo> _allObjects;
        private readonly Dictionary<string, List<string>> _extensionMappings;
        private readonly List<string> _targetFids;

        /// <summary>
        /// 初始化上下文：仅加载所有基础信息（不含XML），内存构建扩展映射和目标FID列表
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        public MetadataContext(string connectionString)
        {
            _allObjects = MetadataDbHelper.LoadAllObjectBasicInfo(connectionString);
            _extensionMappings = BuildExtensionMappings();
            _targetFids = _allObjects.Values
                .Where(o => o.FDevType != "2")
                .Select(o => o.FId)
                .ToList();
        }

        /// <summary>
        /// 获取需要处理的目标FID列表（FDEVTYPE < 2的基础资料和单据）
        /// </summary>
        public List<string> GetTargetFids() => _targetFids;

        /// <summary>
        /// 获取指定FID的基础信息（线程安全，只读操作）
        /// </summary>
        /// <param name="fid">对象FID</param>
        /// <returns>ObjectBasicInfo，未找到则返回null</returns>
        public ObjectBasicInfo GetBasicInfo(string fid)
        {
            _allObjects.TryGetValue(fid, out var info);
            return info;
        }

        /// <summary>
        /// 为指定FID构建完整的处理链（继承链 + 每层继承节点的扩展链）
        /// 继承链从FINHERITPATH解析并反转（根→当前），每个继承链节点后追加其扩展FID
        /// 线程安全：只读取_allObjects和_extensionMappings，不修改任何状态
        /// </summary>
        /// <param name="fid">目标FID</param>
        /// <returns>按处理顺序排列的FID列表</returns>
        public List<string> BuildFullChain(string fid)
        {
            var chain = new List<string>();
            var visited = new HashSet<string>();

            if (!_allObjects.TryGetValue(fid, out var objInfo))
                return chain;

            var inheritChain = ParseInheritPath(objInfo.FInheritPath);
            inheritChain.Reverse();
            inheritChain.Add(fid);

            foreach (var chainFid in inheritChain)
            {
                if (!visited.Add(chainFid))
                    continue;

                chain.Add(chainFid);

                AppendExtensions(chainFid, chain, visited);
            }

            return chain;
        }

        /// <summary>
        /// 收集一批目标FID的完整处理链中涉及的所有FID（去重），用于批量加载XML
        /// </summary>
        /// <param name="batchFids">本批次要处理的目标FID列表</param>
        /// <returns>去重后的所有需要加载XML的FID集合</returns>
        public HashSet<string> CollectNeededFids(IEnumerable<string> batchFids)
        {
            var neededFids = new HashSet<string>();
            foreach (var fid in batchFids)
            {
                foreach (var chainFid in BuildFullChain(fid))
                {
                    neededFids.Add(chainFid);
                }
            }
            return neededFids;
        }

        /// <summary>
        /// 从基础信息字典中构建扩展映射：FBASEOBJECTID → [extFid1, extFid2, ...]
        /// </summary>
        private Dictionary<string, List<string>> BuildExtensionMappings()
        {
            var result = new Dictionary<string, List<string>>();
            foreach (var obj in _allObjects.Values)
            {
                if (obj.FDevType == "2" && !string.IsNullOrEmpty(obj.FBaseObjectId))
                {
                    if (!result.TryGetValue(obj.FBaseObjectId, out var list))
                    {
                        list = new List<string>();
                        result[obj.FBaseObjectId] = list;
                    }
                    list.Add(obj.FId);
                }
            }
            return result;
        }

        /// <summary>
        /// 递归追加扩展FID到处理链，扩展的扩展也会被递归追加
        /// </summary>
        private void AppendExtensions(string baseFid, List<string> chain, HashSet<string> visited)
        {
            if (!_extensionMappings.TryGetValue(baseFid, out var extensions))
                return;

            foreach (var extFid in extensions)
            {
                if (!visited.Add(extFid))
                    continue;

                chain.Add(extFid);

                AppendExtensions(extFid, chain, visited);
            }
        }

        /// <summary>
        /// 解析继承路径字符串，将逗号分隔的FID列表转为List
        /// </summary>
        private static List<string> ParseInheritPath(string inheritPath)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(inheritPath))
                return result;

            foreach (var part in inheritPath.Split(','))
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    result.Add(trimmed);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// 元数据提取核心类，处理继承链合并和扩展链递归合并
    /// </summary>
    public static class MetadataExtractor
    {
        /// <summary>
        /// 提取一批FID的元数据：收集所需FID → 批量加载XML → 逐个提取 → 返回结果列表
        /// 每个线程独立加载XML，互不干扰，XML在方法返回后自动被GC回收
        /// </summary>
        /// <param name="context">元数据上下文（只读）</param>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="batchFids">本批次要处理的FID列表</param>
        /// <returns>提取结果列表，顺序与batchFids一致</returns>
        public static List<MetadataResult> ExtractBatch(MetadataContext context, string connectionString, List<string> batchFids)
        {
            var neededFids = context.CollectNeededFids(batchFids);
            var xmlCache = MetadataDbHelper.LoadKernelXmlBatch(connectionString, neededFids);

            var results = new List<MetadataResult>();
            foreach (var fid in batchFids)
            {
                results.Add(ExtractByFid(context, fid, xmlCache));
            }

            return results;
        }

        /// <summary>
        /// 根据FID提取完整的元数据信息，使用上下文中预构建的处理链和外部提供的XML缓存
        /// 流程：获取完整处理链 → 按顺序逐层合并Entity/Field/Split
        /// 线程安全：MetadataContext为只读，xmlCache为线程局部变量
        /// </summary>
        /// <param name="context">元数据上下文（只读），提供预构建链</param>
        /// <param name="fid">要提取的对象FID</param>
        /// <param name="xmlCache">XML内容缓存，FID→XML字符串</param>
        /// <returns>合并后的MetadataResult</returns>
        public static MetadataResult ExtractByFid(MetadataContext context, string fid, Dictionary<string, string> xmlCache)
        {
            var objInfo = context.GetBasicInfo(fid);
            if (objInfo == null)
            {
                return new MetadataResult { Fid = fid };
            }

            var fullChain = context.BuildFullChain(fid);

            var entityDict = new Dictionary<string, EntityInfo>();
            var fieldDict = new Dictionary<string, MetadataFieldInfo>();
            var allSplits = new List<SplitTableInfo>();

            foreach (var chainFid in fullChain)
            {
                if (!xmlCache.TryGetValue(chainFid, out var xml) || string.IsNullOrEmpty(xml))
                    continue;

                var (entitiesWithOid, entitiesWithoutOid) = ExtractEntities.ExtractFromXml(xml);
                MergeEntities(entityDict, entitiesWithOid, entitiesWithoutOid);

                var (fieldsWithOid, fieldsWithoutOid) = ExtractFields.ExtractFromXml(xml);
                MergeFields(fieldDict, fieldsWithOid, fieldsWithoutOid);

                MergeSplits(allSplits, ExtractSplits.ExtractFromXml(xml));
            }

            var result = BuildResult(fid, entityDict, fieldDict, allSplits);
            result.ObjInfo = objInfo;

            return result;
        }

        /// <summary>
        /// 将合并字典构建为MetadataResult，按oid有无分组
        /// </summary>
        /// <param name="fid">对象FID</param>
        /// <param name="entityDict">合并后的实体字典</param>
        /// <param name="fieldDict">合并后的字段字典</param>
        /// <param name="splits">合并后的拆分表列表</param>
        /// <returns>构建完成的MetadataResult</returns>
        private static MetadataResult BuildResult(string fid, Dictionary<string, EntityInfo> entityDict, Dictionary<string, MetadataFieldInfo> fieldDict, List<SplitTableInfo> splits)
        {
            var result = new MetadataResult { Fid = fid };

            foreach (var entity in entityDict.Values)
            {
                if (!string.IsNullOrEmpty(entity.Oid))
                {
                    result.EntitiesWithOid.Add(entity);
                }
                else
                {
                    result.EntitiesWithoutOid.Add(entity);
                }
            }

            foreach (var field in fieldDict.Values)
            {
                if (!string.IsNullOrEmpty(field.Oid))
                {
                    result.FieldsWithOid.Add(field);
                }
                else
                {
                    result.FieldsWithoutOid.Add(field);
                }
            }

            result.Splits = splits;
            return result;
        }

        /// <summary>
        /// 合并拆分表信息，按EntityKey+Suffix匹配：已存在则覆盖Description，不存在则新增
        /// </summary>
        /// <param name="allSplits">已有的拆分表列表</param>
        /// <param name="newSplits">待合并的新拆分表列表</param>
        private static void MergeSplits(List<SplitTableInfo> allSplits, List<SplitTableInfo> newSplits)
        {
            foreach (var split in newSplits)
            {
                var existing = allSplits.FirstOrDefault(s => s.EntityKey == split.EntityKey && s.Suffix == split.Suffix);
                if (existing == null)
                {
                    allSplits.Add(split.Clone());
                }
                else
                {
                    if (!string.IsNullOrEmpty(split.Description)) existing.Description = split.Description;
                }
            }
        }

        /// <summary>
        /// 合并实体信息到字典，处理继承链中的覆盖和删除逻辑：
        /// 有oid的实体通过oid匹配父级，action=remove则删除，action=edit则覆盖属性；
        /// 无oid的实体通过Id或Key作为字典键新增
        /// </summary>
        /// <param name="dict">实体合并字典，键为oid或Id/Key</param>
        /// <param name="withOid">有oid的实体列表</param>
        /// <param name="withoutOid">无oid的实体列表</param>
        private static void MergeEntities(Dictionary<string, EntityInfo> dict, List<EntityInfo> withOid, List<EntityInfo> withoutOid)
        {
            foreach (var entity in withOid)
            {
                if (entity.Action == "remove")
                {
                    dict.Remove(entity.Oid);
                    continue;
                }

                if (dict.TryGetValue(entity.Oid, out var parent))
                {
                    var merged = MergeEntityInfo(parent, entity);
                    dict[entity.Oid] = merged;
                }
                else
                {
                    dict[entity.Oid] = entity.Clone();
                }
            }

            foreach (var entity in withoutOid)
            {
                if (entity.Action == "remove")
                    continue;

                var key = !string.IsNullOrEmpty(entity.Id) ? entity.Id : entity.Key;
                if (!string.IsNullOrEmpty(key))
                {
                    dict[key] = entity.Clone();
                }
            }
        }

        /// <summary>
        /// 合并字段信息到字典，处理继承链中的覆盖和删除逻辑：
        /// 有oid的字段通过oid匹配父级，action=remove则删除，action=edit则覆盖属性；
        /// 无oid的字段通过Id或Key作为字典键新增
        /// </summary>
        /// <param name="dict">字段合并字典，键为oid或Id/Key</param>
        /// <param name="withOid">有oid的字段列表</param>
        /// <param name="withoutOid">无oid的字段列表</param>
        private static void MergeFields(Dictionary<string, MetadataFieldInfo> dict, List<MetadataFieldInfo> withOid, List<MetadataFieldInfo> withoutOid)
        {
            foreach (var field in withOid)
            {
                if (field.Action == "remove")
                {
                    dict.Remove(field.Oid);
                    continue;
                }

                if (dict.TryGetValue(field.Oid, out var parent))
                {
                    var merged = MergeFieldInfo(parent, field);
                    dict[field.Oid] = merged;
                }
                else
                {
                    dict[field.Oid] = field.Clone();
                }
            }

            foreach (var field in withoutOid)
            {
                if (field.Action == "remove")
                    continue;

                var key = !string.IsNullOrEmpty(field.Id) ? field.Id : field.Key;
                if (!string.IsNullOrEmpty(key))
                {
                    dict[key] = field.Clone();
                }
            }
        }

        /// <summary>
        /// 合并两个实体信息：以父级为基础，子级非空属性覆盖父级对应属性
        /// </summary>
        /// <param name="parent">父级实体信息</param>
        /// <param name="child">子级实体信息</param>
        /// <returns>合并后的EntityInfo</returns>
        private static EntityInfo MergeEntityInfo(EntityInfo parent, EntityInfo child)
        {
            var merged = parent.Clone();
            merged.Oid = child.Oid;
            merged.Action = child.Action;

            if (!string.IsNullOrEmpty(child.ElementType)) merged.ElementType = child.ElementType;
            if (!string.IsNullOrEmpty(child.EntryName)) merged.EntryName = child.EntryName;
            if (!string.IsNullOrEmpty(child.TableName)) merged.TableName = child.TableName;
            if (!string.IsNullOrEmpty(child.Name)) merged.Name = child.Name;
            if (!string.IsNullOrEmpty(child.EntryPkFieldName)) merged.EntryPkFieldName = child.EntryPkFieldName;
            if (!string.IsNullOrEmpty(child.Id)) merged.Id = child.Id;
            if (!string.IsNullOrEmpty(child.Key)) merged.Key = child.Key;
            if (!string.IsNullOrEmpty(child.KeyField)) merged.KeyField = child.KeyField;
            if (!string.IsNullOrEmpty(child.TagName)) merged.TagName = child.TagName;

            return merged;
        }

        /// <summary>
        /// 合并两个字段信息：以父级为基础，子级非空属性覆盖父级对应属性
        /// </summary>
        /// <param name="parent">父级字段信息</param>
        /// <param name="child">子级字段信息</param>
        /// <returns>合并后的FieldInfo</returns>
        private static MetadataFieldInfo MergeFieldInfo(MetadataFieldInfo parent, MetadataFieldInfo child)
        {
            var merged = parent.Clone();
            merged.Oid = child.Oid;
            merged.Action = child.Action;

            if (!string.IsNullOrEmpty(child.ElementType)) merged.ElementType = child.ElementType;
            if (!string.IsNullOrEmpty(child.Id)) merged.Id = child.Id;
            if (!string.IsNullOrEmpty(child.Key)) merged.Key = child.Key;
            if (!string.IsNullOrEmpty(child.Name)) merged.Name = child.Name;
            if (!string.IsNullOrEmpty(child.FieldName)) merged.FieldName = child.FieldName;
            if (!string.IsNullOrEmpty(child.PropertyName)) merged.PropertyName = child.PropertyName;
            if (!string.IsNullOrEmpty(child.EntityKey)) merged.EntityKey = child.EntityKey;
            if (!string.IsNullOrEmpty(child.Suffix)) merged.Suffix = child.Suffix;
            if (!string.IsNullOrEmpty(child.TagName)) merged.TagName = child.TagName;

            return merged;
        }
    }
}
