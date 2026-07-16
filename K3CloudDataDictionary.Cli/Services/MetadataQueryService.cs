using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using K3CloudDataDictionary.Views;

namespace K3CloudDataDictionary.Cli.Services
{
    /// <summary>
    /// 元数据查询服务 - 直接连接 SQL Server 实时查询
    /// </summary>
    public class MetadataQueryService
    {
        private readonly string _connectionString;
        private MetadataContext _context;
        private Dictionary<string, ObjectBasicInfo> _allObjects;
        private Dictionary<string, string> _elementTypeNames;
        private HashSet<string> _lkTableCache; // LK 表检测结果缓存
        private bool _lkDetectionTimedOut; // LK 检测是否超时

        public MetadataQueryService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 归一化关键词：去除全角/半角括号和空格，便于容错匹配
        /// 例："剩余收料数量（基本）" → "剩余收料数量基本"
        /// </summary>
        public static string NormalizeKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return keyword;
            return Regex.Replace(keyword, @"[\s\(\)（）]+", "").Trim();
        }

        /// <summary>
        /// 归一化后的模糊匹配
        /// </summary>
        private static bool NormalizedContains(string text, string normalizedKeyword)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(normalizedKeyword)) return false;
            var normalizedText = NormalizeKeyword(text);
            return normalizedText.IndexOf(normalizedKeyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 初始化上下文（懒加载）
        /// </summary>
        private void EnsureContext()
        {
            if (_context == null)
            {
                Console.Error.WriteLine("正在加载元数据上下文...");
                _context = new MetadataContext(_connectionString);
                _allObjects = LoadAllObjectBasicInfo();
                _elementTypeNames = LoadElementTypeNames();
                Console.Error.WriteLine($"已加载 {_allObjects.Count} 个对象");
            }
        }

        /// <summary>
        /// 加载元素类型中文名称映射
        /// </summary>
        private Dictionary<string, string> LoadElementTypeNames()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string sql = "SELECT FID, FNAME FROM T_MDL_ELEMENTTYPE_L WHERE FLOCALEID = 2052";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 30;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var fid = reader["FID"]?.ToString() ?? "";
                            var fname = reader["FNAME"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(fid))
                            {
                                result[fid] = fname;
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 获取元素类型中文名称
        /// </summary>
        private string GetElementTypeName(string elementType)
        {
            if (string.IsNullOrEmpty(elementType)) return "";
            return _elementTypeNames.GetValueOrDefault(elementType, "");
        }

        /// <summary>
        /// 根据对象 ID 解析对应的表单信息（用于 lookUpObject 反查）
        /// </summary>
        /// <param name="objectId">对象 ID（即 lookUpObject 值）</param>
        public List<Dictionary<string, object>> ResolveObject(string objectId)
        {
            var results = new List<Dictionary<string, object>>();

            if (string.IsNullOrEmpty(objectId))
            {
                return results;
            }

            // 第一步：通过 LookUpObjectID 查找 T_Meta_LookupClass 获取 FFORMID
            string lookupSql = @"SELECT FID, FFORMID, FTABLENAME, FPKFIELDNAME, FORGFIELDNAME 
                                FROM T_Meta_LookupClass 
                                WHERE FID = @ObjectId";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(lookupSql, conn))
                {
                    cmd.Parameters.AddWithValue("@ObjectId", objectId);
                    cmd.CommandTimeout = 30;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["FID"] = reader["FID"]?.ToString() ?? "",
                                ["FFORMID"] = reader["FFORMID"]?.ToString() ?? "",
                                ["FTABLENAME"] = reader["FTABLENAME"]?.ToString() ?? "",
                                ["FPKFIELDNAME"] = reader["FPKFIELDNAME"]?.ToString() ?? "",
                                ["FORGFIELDNAME"] = reader["FORGFIELDNAME"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 加载所有对象基础信息
        /// </summary>
        private Dictionary<string, ObjectBasicInfo> LoadAllObjectBasicInfo()
        {
            var result = new Dictionary<string, ObjectBasicInfo>(StringComparer.OrdinalIgnoreCase);
            string sql = @"SELECT A.FID, L.FNAME, A.FSUBSYSID, A.FBASEOBJECTID, A.FMODELTYPEID, 
                                  A.FINHERITPATH, A.FVERSION, A.FMAINVERSION, A.FDEVTYPE 
                           FROM T_META_OBJECTTYPE A 
                           INNER JOIN T_META_OBJECTTYPE_L L ON A.FID = L.FID AND L.FLOCALEID = 2052 
                           WHERE A.FMODELTYPEID IN (400, 100)";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 60;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var info = new ObjectBasicInfo
                            {
                                FId = reader["FID"]?.ToString() ?? "",
                                FName = reader["FNAME"]?.ToString() ?? "",
                                FSubSysId = reader["FSUBSYSID"]?.ToString() ?? "",
                                FBaseObjectId = reader["FBASEOBJECTID"]?.ToString() ?? "",
                                FModelTypeId = reader["FMODELTYPEID"]?.ToString() ?? "",
                                FInheritPath = reader["FINHERITPATH"]?.ToString() ?? "",
                                FVersion = reader["FVERSION"]?.ToString() ?? "",
                                FMainVersion = reader["FMAINVERSION"]?.ToString() ?? "",
                                FDevType = reader["FDEVTYPE"]?.ToString() ?? ""
                            };
                            if (!string.IsNullOrEmpty(info.FId))
                            {
                                result[info.FId] = info;
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 查询表单信息
        /// </summary>
        public List<Dictionary<string, object>> QueryForm(string formIdentifier)
        {
            EnsureContext();
            var results = new List<Dictionary<string, object>>();

            // 查找匹配的 FID
            var matchingFids = _allObjects.Keys
                .Where(fid => fid.Equals(formIdentifier, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingFids.Count == 0)
            {
                return results;
            }

            foreach (var fid in matchingFids)
            {
                var objInfo = _allObjects[fid];
                var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["FFORMID"] = fid,
                    ["FFORMIDENTIFIER"] = fid,
                    ["FDJMC"] = objInfo.FName,
                    ["FELEMENTTYPENAME"] = objInfo.FModelTypeId == "400" ? "基础资料" : "单据",
                    ["FSUBSYSTEMNAME"] = objInfo.FSubSysId,
                    ["FFORMPLUGINCOUNT"] = 0,
                    ["FLISTPLUGINCOUNT"] = 0,
                    ["FBUILDERPLUGINCOUNT"] = 0,
                    ["FUPDATEACTIONCOUNT"] = 0,
                    ["FSERVICERULECOUNT"] = 0,
                    ["FFORMOPERATIONCOUNT"] = 0
                };

                // 提取完整元数据以统计插件、服务规则等
                try
                {
                    var metadata = ExtractMetadata(fid);
                    if (metadata != null)
                    {
                        result["FFORMPLUGINCOUNT"] = metadata.Plugins.Count(p => p.PluginType == "FormPlugins");
                        result["FLISTPLUGINCOUNT"] = metadata.Plugins.Count(p => p.PluginType == "ListPlugins");
                        result["FBUILDERPLUGINCOUNT"] = metadata.Plugins.Count(p => p.PluginType == "WebFormBuilderPlugins");
                        result["FSERVICERULECOUNT"] = metadata.EntitiesWithOid.Sum(e => e.ServiceRules.Count) +
                                                       metadata.EntitiesWithoutOid.Sum(e => e.ServiceRules.Count);
                        result["FUPDATEACTIONCOUNT"] = metadata.FieldsWithOid.Sum(f => f.UpdateActions.Count) +
                                                        metadata.FieldsWithoutOid.Sum(f => f.UpdateActions.Count);
                        result["FFORMOPERATIONCOUNT"] = metadata.FormOperations.Count;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"提取元数据时出错: {ex.Message}");
                }

                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// 查询表单的实体列表
        /// </summary>
        public List<Dictionary<string, object>> QueryEntities(string formIdentifier)
        {
            EnsureContext();
            var results = new List<Dictionary<string, object>>();

            var matchingFids = _allObjects.Keys
                .Where(fid => fid.Equals(formIdentifier, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var fid in matchingFids)
            {
                try
                {
                    var metadata = ExtractMetadata(fid);
                    if (metadata != null)
                    {
                        var allEntities = metadata.EntitiesWithOid.Concat(metadata.EntitiesWithoutOid).ToList();
                        foreach (var entity in allEntities)
                        {
                            results.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["FFORMID"] = fid,
                                ["FFORMIDENTIFIER"] = fid,
                                ["FENTITYID"] = entity.Key,
                                ["FKey"] = entity.Key,
                                ["FENTITYNAME"] = entity.Name,
                                ["FTABLENAME"] = entity.TableName,
                                ["FEntryName"] = entity.EntryName,
                                ["FELEMENTTYPENAME"] = entity.ElementType,
                                ["FSEQFIELDKEY"] = entity.SeqFieldKey,
                                ["FENTRY_PK_FIELD_NAME"] = entity.EffectivePkFieldName,
                                ["FSERVICERULECOUNT"] = entity.ServiceRules.Count,
                                ["FUPDATEACTIONCOUNT"] = 0 // 需要关联字段统计
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"提取实体时出错: {ex.Message}");
                }
            }

            return results;
        }

        /// <summary>
        /// 查询字段
        /// </summary>
        /// <param name="formIdentifier">表单标识</param>
        /// <param name="entityKey">实体 Key（可选）</param>
        /// <param name="keyword">字段搜索关键词（可选，支持模糊/精确匹配）</param>
        /// <param name="exact">true=精确匹配，false=模糊匹配（默认）</param>
        public List<Dictionary<string, object>> QueryFields(string formIdentifier, string entityKey = null, string keyword = null, bool exact = false)
        {
            EnsureContext();
            var results = new List<Dictionary<string, object>>();

            var matchingFids = _allObjects.Keys
                .Where(fid => fid.Equals(formIdentifier, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var fid in matchingFids)
            {
                try
                {
                    var metadata = ExtractMetadata(fid);
                    if (metadata != null)
                    {
                        var allFields = metadata.FieldsWithOid.Concat(metadata.FieldsWithoutOid).ToList();
                        var allEntities = metadata.EntitiesWithOid.Concat(metadata.EntitiesWithoutOid).ToList();

                        // 归一化关键词（兼容全角/半角括号）
                        var normalizedKeyword = string.IsNullOrEmpty(keyword) ? null : NormalizeKeyword(keyword);

                        // 构建 EntityKey -> Entity 映射
                        var entityMap = allEntities.ToDictionary(
                            e => e.Key,
                            e => e,
                            StringComparer.OrdinalIgnoreCase);

                        foreach (var field in allFields)
                        {
                            // 如果指定了 entityKey，只返回该实体的字段
                            if (!string.IsNullOrEmpty(entityKey) &&
                                !field.EntityKey.Equals(entityKey, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            // 如果指定了 keyword，进行字段搜索过滤
                            if (!string.IsNullOrEmpty(keyword))
                            {
                                var keywordLower = keyword.ToLowerInvariant();
                                bool matched;
                                if (exact)
                                {
                                    matched = field.Key.Equals(keyword, StringComparison.OrdinalIgnoreCase) ||
                                              field.Name.Equals(keyword, StringComparison.OrdinalIgnoreCase) ||
                                              field.FieldName.Equals(keyword, StringComparison.OrdinalIgnoreCase) ||
                                              field.PropertyName.Equals(keyword, StringComparison.OrdinalIgnoreCase);
                                }
                                else
                                {
                                    matched = field.Key.ToLowerInvariant().Contains(keywordLower) ||
                                              NormalizedContains(field.Name, normalizedKeyword) ||
                                              field.FieldName.ToLowerInvariant().Contains(keywordLower) ||
                                              field.PropertyName.ToLowerInvariant().Contains(keywordLower);
                                }

                                if (!matched) continue;
                            }

                            var entity = entityMap.ContainsKey(field.EntityKey) ? entityMap[field.EntityKey] : null;

                            // 构建 StatusItems 嵌套数据（仅 elementType=40 时）
                            object statusItemsData = null;
                            if (field.ElementType == "40" && field.StatusItems.Count > 0)
                            {
                                statusItemsData = field.StatusItems.ConvertAll(s => new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                                {
                                    ["value"] = s.StatusValue,
                                    ["name"] = s.StatusName
                                });
                            }

                            // 计算拆分表名
                            var splitTable = !string.IsNullOrEmpty(field.Suffix)
                                ? (entity?.TableName ?? "") + "_" + field.Suffix
                                : "";

                            results.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["FDJMC"] = _allObjects[fid].FName,
                                ["FENTITYNAME"] = entity?.Name ?? field.EntityKey,
                                ["FENTITYKEY"] = field.EntityKey,
                                ["FENTRYNAME"] = entity?.EntryName ?? "",
                                ["FSEQFIELDKEY"] = entity?.SeqFieldKey ?? "",
                                ["FENTRY_PK_FIELD_NAME"] = entity?.EffectivePkFieldName ?? "FEntryId",
                                ["FTABLENAME"] = entity?.TableName ?? "",
                                ["FSPLITTABlE"] = splitTable,
                                ["FFIELDDBID"] = field.Id,
                                ["FKey"] = field.Key,
                                ["FName"] = field.Name,
                                ["FFieldName"] = field.FieldName,
                                ["FPropertyName"] = field.PropertyName,
                                ["FELEMENTTYPENAME"] = field.ElementType,
                                ["FELEMENTTYPECNNAME"] = GetElementTypeName(field.ElementType),
                                ["FTagName"] = field.TagName,
                                ["FLookUpObjectID"] = field.LookUpObjectID,
                                ["FEnumType"] = field.EnumType,
                                ["FLookUpObjectDisplay"] = "",
                                ["FEnumTypeDisplay"] = "",
                                ["FSUFFIX"] = field.Suffix,
                                ["FSPLITDESCRIPTION"] = "",
                                ["FUPDATEACTIONCOUNT"] = field.UpdateActions.Count,
                                ["FSTATUSITEMS"] = statusItemsData
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"提取字段时出错: {ex.Message}");
                }
            }

            // 如果指定了 entityKey 但没匹配到任何字段，输出可用实体列表
            if (!string.IsNullOrEmpty(entityKey) && results.Count == 0)
            {
                foreach (var fid2 in matchingFids)
                {
                    try
                    {
                        var md = ExtractMetadata(fid2);
                        if (md != null)
                        {
                            var allEnt = md.EntitiesWithOid.Concat(md.EntitiesWithoutOid).ToList();
                            var suggestions = allEnt.Select(e => new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["entityKey"] = e.Key,
                                ["entityName"] = e.Name,
                                ["table"] = e.TableName
                            }).ToList();

                            var hint = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["_hint"] = "entity_not_found",
                                ["message"] = $"未找到实体 '{entityKey}' 的字段。该表单包含以下实体：",
                                ["availableEntities"] = suggestions
                            };
                            results.Add(hint);
                        }
                    }
                    catch { }
                }
            }

            return results;
        }

        /// <summary>
        /// 搜索字段
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <param name="exact">true=精确匹配（完全相等），false=模糊匹配（包含）</param>
        public List<Dictionary<string, object>> SearchFields(string keyword, bool exact = false)
        {
            EnsureContext();
            var results = new List<Dictionary<string, object>>();
            var keywordLower = keyword.ToLowerInvariant();
            var normalizedKeyword = NormalizeKeyword(keyword);

            // 遍历所有对象，搜索匹配的字段
            foreach (var kvp in _allObjects)
            {
                var fid = kvp.Key;
                var objInfo = kvp.Value;

                // 跳过扩展对象
                if (objInfo.FDevType == "2") continue;

                try
                {
                    var metadata = ExtractMetadata(fid);
                    if (metadata != null)
                    {
                        var allFields = metadata.FieldsWithOid.Concat(metadata.FieldsWithoutOid).ToList();
                        var allEntities = metadata.EntitiesWithOid.Concat(metadata.EntitiesWithoutOid).ToList();
                        var entityMap = allEntities.ToDictionary(
                            e => e.Key,
                            e => e,
                            StringComparer.OrdinalIgnoreCase);

                        foreach (var field in allFields)
                        {
                            // 搜索匹配
                            bool matched;
                            if (exact)
                            {
                                matched = field.Key.Equals(keyword, StringComparison.OrdinalIgnoreCase) ||
                                          field.Name.Equals(keyword, StringComparison.OrdinalIgnoreCase) ||
                                          field.FieldName.Equals(keyword, StringComparison.OrdinalIgnoreCase) ||
                                          field.PropertyName.Equals(keyword, StringComparison.OrdinalIgnoreCase);
                            }
                            else
                            {
                                matched = field.Key.ToLowerInvariant().Contains(keywordLower) ||
                                          NormalizedContains(field.Name, normalizedKeyword) ||
                                          field.FieldName.ToLowerInvariant().Contains(keywordLower) ||
                                          field.PropertyName.ToLowerInvariant().Contains(keywordLower);
                            }

                            if (!matched) continue;

                            var entity = entityMap.ContainsKey(field.EntityKey) ? entityMap[field.EntityKey] : null;

                            // 构建 StatusItems 嵌套数据（仅 elementType=40 时）
                            object statusItemsData = null;
                            if (field.ElementType == "40" && field.StatusItems.Count > 0)
                            {
                                statusItemsData = field.StatusItems.ConvertAll(s => new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                                {
                                    ["value"] = s.StatusValue,
                                    ["name"] = s.StatusName
                                });
                            }

                            var splitTable2 = !string.IsNullOrEmpty(field.Suffix)
                                ? (entity?.TableName ?? "") + "_" + field.Suffix
                                : "";

                            results.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["FDJMC"] = objInfo.FName,
                                ["FENTITYNAME"] = entity?.Name ?? field.EntityKey,
                                ["FENTITYKEY"] = field.EntityKey,
                                ["FENTRYNAME"] = entity?.EntryName ?? "",
                                ["FSEQFIELDKEY"] = entity?.SeqFieldKey ?? "",
                                ["FENTRY_PK_FIELD_NAME"] = entity?.EffectivePkFieldName ?? "FEntryId",
                                ["FTABLENAME"] = entity?.TableName ?? "",
                                ["FSPLITTABlE"] = splitTable2,
                                ["FFIELDDBID"] = field.Id,
                                ["FKey"] = field.Key,
                                ["FName"] = field.Name,
                                ["FFieldName"] = field.FieldName,
                                ["FPropertyName"] = field.PropertyName,
                                ["FELEMENTTYPENAME"] = field.ElementType,
                                ["FELEMENTTYPECNNAME"] = GetElementTypeName(field.ElementType),
                                ["FTagName"] = field.TagName,
                                ["FLookUpObjectID"] = field.LookUpObjectID,
                                ["FLookUpObjectDisplay"] = "",
                                ["FEnumTypeDisplay"] = "",
                                ["FSUFFIX"] = field.Suffix,
                                ["FSTATUSITEMS"] = statusItemsData
                            });

                            // 限制结果数量
                            if (results.Count >= 100) break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"搜索 {fid} 时出错: {ex.Message}");
                }

                if (results.Count >= 100) break;
            }

            return results;
        }

        /// <summary>
        /// 搜索表
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <param name="exact">true=精确匹配（完全相等），false=模糊匹配（包含）</param>
        public List<Dictionary<string, object>> SearchTables(string keyword, bool exact = false)
        {
            EnsureContext();
            var results = new List<Dictionary<string, object>>();
            var keywordLower = keyword.ToLowerInvariant();

            foreach (var kvp in _allObjects)
            {
                var fid = kvp.Key;
                var objInfo = kvp.Value;

                // 跳过扩展对象
                if (objInfo.FDevType == "2") continue;

                // 搜索匹配
                bool formMatch;
                if (exact)
                {
                    formMatch = fid.Equals(keyword, StringComparison.OrdinalIgnoreCase) ||
                                objInfo.FName.Equals(keyword, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    formMatch = fid.ToLowerInvariant().Contains(keywordLower) ||
                                objInfo.FName.ToLowerInvariant().Contains(keywordLower);
                }

                if (!formMatch) continue;

                try
                {
                    var metadata = ExtractMetadata(fid);
                    if (metadata != null)
                    {
                        var allEntities = metadata.EntitiesWithOid.Concat(metadata.EntitiesWithoutOid).ToList();

                        // 表单已匹配，返回该表单的所有实体（不再做实体级别的二次过滤）
                        foreach (var entity in allEntities)
                        {
                            results.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["FFORMID"] = fid,
                                ["FFORMIDENTIFIER"] = fid,
                                ["FDJMC"] = objInfo.FName,
                                ["FENTITYID"] = entity.Key,
                                ["FKey"] = entity.Key,
                                ["FENTITYNAME"] = entity.Name,
                                ["FTABLENAME"] = entity.TableName,
                                ["FELEMENTTYPENAME"] = entity.ElementType,
                                ["FFIELDCOUNT"] = 0
                            });

                            if (results.Count >= 100) break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"搜索表 {fid} 时出错: {ex.Message}");
                }

                if (results.Count >= 100) break;
            }

            return results;
        }

        /// <summary>
        /// 查询单据类型（支持按表单查询列表，或按 ID/关键词查询详情）
        /// </summary>
        /// <param name="formIdentifier">表单标识（可选，按表单查询列表）</param>
        /// <param name="billTypeId">单据类型 ID（可选，精确查询）</param>
        /// <param name="keyword">搜索关键词（可选，模糊搜索）</param>
        public List<Dictionary<string, object>> QueryBillTypes(string formIdentifier = null, string billTypeId = null, string keyword = null)
        {
            var results = new List<Dictionary<string, object>>();

            if (string.IsNullOrEmpty(formIdentifier) && string.IsNullOrEmpty(billTypeId) && string.IsNullOrEmpty(keyword))
            {
                return results;
            }

            string sql = @"SELECT a.FBILLTYPEID, a.FBILLFORMID, a.FNUMBER, b.FNAME, b.FDESCRIPTION
                           FROM T_BAS_BILLTYPE a
                           LEFT JOIN T_BAS_BILLTYPE_L b ON a.FBILLTYPEID = b.FBILLTYPEID AND b.FLOCALEID = 2052
                           WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(formIdentifier))
            {
                sql += " AND a.FBILLFORMID = @FormIdentifier";
                parameters.Add(new SqlParameter("@FormIdentifier", formIdentifier));
            }

            if (!string.IsNullOrEmpty(billTypeId))
            {
                sql += " AND a.FBILLTYPEID = @BillTypeId";
                parameters.Add(new SqlParameter("@BillTypeId", billTypeId));
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                sql += " AND (a.FNUMBER LIKE @Keyword OR b.FNAME LIKE @Keyword OR b.FDESCRIPTION LIKE @Keyword)";
                parameters.Add(new SqlParameter("@Keyword", $"%{keyword}%"));
            }

            sql += " ORDER BY a.FNUMBER";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    cmd.CommandTimeout = 30;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["FBILLTYPEID"] = reader["FBILLTYPEID"]?.ToString() ?? "",
                                ["FBILLFORMID"] = reader["FBILLFORMID"]?.ToString() ?? "",
                                ["FNUMBER"] = reader["FNUMBER"]?.ToString() ?? "",
                                ["FNAME"] = reader["FNAME"]?.ToString() ?? "",
                                ["FDESCRIPTION"] = reader["FDESCRIPTION"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 查询辅助资料列表
        /// </summary>
        /// <param name="lookUpObjectId">辅助资料 ID（即字段的 LookUpObjectID）</param>
        public List<Dictionary<string, object>> QueryAssistantData(string lookUpObjectId)
        {
            var results = new List<Dictionary<string, object>>();

            if (string.IsNullOrEmpty(lookUpObjectId))
            {
                return results;
            }

            string sql = @"SELECT a.FID, a.FNUMBER, b.FNAME, c.FENTRYID, c.FNUMBER AS FENTRYNUMBER, d.FDATAVALUE
                           FROM T_BAS_ASSISTANTDATA a
                           INNER JOIN T_BAS_ASSISTANTDATA_L b ON a.FID = b.FID AND b.FLOCALEID = 2052
                           INNER JOIN T_BAS_ASSISTANTDATAENTRY c ON a.FID = c.FID
                           INNER JOIN T_BAS_ASSISTANTDATAENTRY_L d ON c.FENTRYID = d.FENTRYID AND d.FLOCALEID = 2052
                           WHERE a.FID = @FID
                           ORDER BY c.FNUMBER";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@FID", lookUpObjectId);
                    cmd.CommandTimeout = 30;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["FID"] = reader["FID"]?.ToString() ?? "",
                                ["FNUMBER"] = reader["FNUMBER"]?.ToString() ?? "",
                                ["FNAME"] = reader["FNAME"]?.ToString() ?? "",
                                ["FENTRYID"] = reader["FENTRYID"]?.ToString() ?? "",
                                ["FENTRYNUMBER"] = reader["FENTRYNUMBER"]?.ToString() ?? "",
                                ["FDATAVALUE"] = reader["FDATAVALUE"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 查询枚举项列表（elementType=9 下拉列表）
        /// </summary>
        /// <param name="enumTypeId">枚举类型 ID（即字段的 EnumType / FEnumType）</param>
        public List<Dictionary<string, object>> QueryEnumItems(string enumTypeId)
        {
            var results = new List<Dictionary<string, object>>();

            if (string.IsNullOrEmpty(enumTypeId))
            {
                return results;
            }

            string sql = @"SELECT t1.FID, t2.FNAME, t3.FVALUE, t3.FENUMID, t4.FCAPTION
                           FROM T_META_FORMENUM t1
                           INNER JOIN T_META_FORMENUM_L t2 ON t1.FID = t2.FID AND t2.FLOCALEID = 2052
                           INNER JOIN T_META_FORMENUMITEM t3 ON t1.FID = t3.FID
                           INNER JOIN T_META_FORMENUMITEM_L t4 ON t3.FENUMID = t4.FENUMID AND t4.FLOCALEID = 2052
                           WHERE t1.FID = @FID
                           ORDER BY t3.FVALUE";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@FID", enumTypeId);
                    cmd.CommandTimeout = 30;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["FID"] = reader["FID"]?.ToString() ?? "",
                                ["FNAME"] = reader["FNAME"]?.ToString() ?? "",
                                ["FVALUE"] = reader["FVALUE"]?.ToString() ?? "",
                                ["FENUMID"] = reader["FENUMID"]?.ToString() ?? "",
                                ["FCAPTION"] = reader["FCAPTION"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 查询单据状态字段的枚举值（elementType=40，BillStatusField）
        /// 返回字段级别数据，statusItems 作为嵌套子对象
        /// </summary>
        /// <param name="formIdentifier">表单标识</param>
        /// <param name="fieldKey">字段 Key（可选，精确匹配）</param>
        /// <param name="keyword">搜索关键词（可选，模糊搜索状态名称/值）</param>
        public List<Dictionary<string, object>> QueryBillStatusItems(string formIdentifier, string fieldKey = null, string keyword = null)
        {
            EnsureContext();
            var results = new List<Dictionary<string, object>>();

            var matchingFids = _allObjects.Keys
                .Where(fid => fid.Equals(formIdentifier, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var fid in matchingFids)
            {
                try
                {
                    var metadata = ExtractMetadata(fid);
                    if (metadata != null)
                    {
                        var allFields = metadata.FieldsWithOid.Concat(metadata.FieldsWithoutOid).ToList();
                        var allEntities = metadata.EntitiesWithOid.Concat(metadata.EntitiesWithoutOid).ToList();
                        var entityMap = allEntities.ToDictionary(e => e.Key, e => e, StringComparer.OrdinalIgnoreCase);

                        foreach (var field in allFields)
                        {
                            // 只处理单据状态字段（ElementType=40）
                            if (field.ElementType != "40") continue;

                            // 如果指定了 fieldKey，只返回该字段
                            if (!string.IsNullOrEmpty(fieldKey) &&
                                !field.Key.Equals(fieldKey, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            var entity = entityMap.ContainsKey(field.EntityKey) ? entityMap[field.EntityKey] : null;

                            // 构建 statusItems 嵌套数据
                            var statusItemsData = new List<Dictionary<string, object>>();
                            foreach (var statusItem in field.StatusItems)
                            {
                                // 如果指定了 keyword，进行模糊搜索
                                if (!string.IsNullOrEmpty(keyword))
                                {
                                    var keywordLower = keyword.ToLowerInvariant();
                                    if (!statusItem.StatusName.ToLowerInvariant().Contains(keywordLower) &&
                                        !statusItem.StatusValue.ToLowerInvariant().Contains(keywordLower))
                                    {
                                        continue;
                                    }
                                }

                                statusItemsData.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                                {
                                    ["value"] = statusItem.StatusValue,
                                    ["name"] = statusItem.StatusName
                                });
                            }

                            // 如果有匹配的状态项，添加到结果中
                            if (statusItemsData.Count > 0 || string.IsNullOrEmpty(keyword))
                            {
                                results.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                                {
                                    ["FFORMID"] = fid,
                                    ["FFORMIDENTIFIER"] = fid,
                                    ["FDJMC"] = _allObjects[fid].FName,
                                    ["FENTITYNAME"] = entity?.Name ?? field.EntityKey,
                                    ["FTABLENAME"] = entity?.TableName ?? "",
                                    ["FKey"] = field.Key,
                                    ["FName"] = field.Name,
                                    ["FFieldName"] = field.FieldName,
                                    ["FPropertyName"] = field.PropertyName,
                                    ["FELEMENTTYPE"] = "40",
                                    ["FELEMENTTYPENAME"] = "BillStatusField",
                                    ["FSTATUSITEMS"] = statusItemsData
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"查询单据状态时出错: {ex.Message}");
                }
            }

            return results;
        }

        /// <summary>
        /// 通过 sys.columns 探测物理表列（不受字典覆盖范围限制）
        /// </summary>
        public List<Dictionary<string, object>> ProbePhysicalColumns(string tableName, string keyword = null)
        {
            var results = new List<Dictionary<string, object>>();
            if (string.IsNullOrEmpty(tableName)) return results;

            string sql = @"SELECT c.name AS ColumnName, tp.name AS DataType,
                                  c.max_length, c.precision, c.scale, c.is_nullable
                           FROM sys.columns c
                           INNER JOIN sys.tables t_obj ON c.object_id = t_obj.object_id
                           INNER JOIN sys.schemas s ON t_obj.schema_id = s.schema_id
                           INNER JOIN sys.types tp ON c.user_type_id = tp.user_type_id
                           WHERE (s.name + '.' + t_obj.name = @TableName OR t_obj.name = @TableName)";

            if (!string.IsNullOrEmpty(keyword))
                sql += " AND c.name LIKE @Keyword";

            sql += " ORDER BY c.column_id";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@Keyword", "%" + keyword + "%");
                    cmd.CommandTimeout = 30;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["columnName"] = reader["ColumnName"]?.ToString() ?? "",
                                ["dataType"] = reader["DataType"]?.ToString() ?? "",
                                ["maxLength"] = Convert.ToInt32(reader["max_length"] ?? 0),
                                ["precision"] = Convert.ToInt32(reader["precision"] ?? 0),
                                ["scale"] = Convert.ToInt32(reader["scale"] ?? 0),
                                ["isNullable"] = reader["is_nullable"] != null && (bool)reader["is_nullable"]
                            });
                        }
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// 按模式匹配批量探测物理表列（支持通配符 *）
        /// </summary>
        /// <param name="tablePattern">表名模式，支持 * 通配符，如 "t_PUR_POOrderEntry*"</param>
        /// <param name="keyword">列名关键词（可选）</param>
        public List<Dictionary<string, object>> ProbePhysicalColumnsByPattern(string tablePattern, string keyword = null)
        {
            var results = new List<Dictionary<string, object>>();
            if (string.IsNullOrEmpty(tablePattern)) return results;

            // 将通配符 * 转换为 SQL LIKE 的 %
            var likePattern = tablePattern.Replace("*", "%");

            string sql = @"SELECT t.name AS TableName, c.name AS ColumnName, tp.name AS DataType,
                                  c.max_length, c.precision, c.scale, c.is_nullable
                           FROM sys.columns c
                           INNER JOIN sys.tables t ON c.object_id = t.object_id
                           INNER JOIN sys.types tp ON c.user_type_id = tp.user_type_id
                           WHERE t.name LIKE @TablePattern";

            if (!string.IsNullOrEmpty(keyword))
                sql += " AND c.name LIKE @Keyword";

            sql += " ORDER BY t.name, c.column_id";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TablePattern", likePattern);
                    if (!string.IsNullOrEmpty(keyword))
                        cmd.Parameters.AddWithValue("@Keyword", "%" + keyword + "%");
                    cmd.CommandTimeout = 30;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["table"] = reader["TableName"]?.ToString() ?? "",
                                ["columnName"] = reader["ColumnName"]?.ToString() ?? "",
                                ["dataType"] = reader["DataType"]?.ToString() ?? "",
                                ["maxLength"] = Convert.ToInt32(reader["max_length"] ?? 0),
                                ["precision"] = Convert.ToInt32(reader["precision"] ?? 0),
                                ["scale"] = Convert.ToInt32(reader["scale"] ?? 0),
                                ["isNullable"] = reader["is_nullable"] != null && (bool)reader["is_nullable"]
                            });
                        }
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// 根据已知字段推测"基本"单位衍生字段的物理列名（FBASE 前缀规律）
        /// 例：FRECEIVEQTY → FBASERECEIVEQTY
        /// 先查字典，再查物理表
        /// </summary>
        public List<string> SuggestBaseFields(string tableName, string knownFieldName, List<MetadataFieldInfo> allFields = null)
        {
            var suggestions = new List<string>();
            if (string.IsNullOrEmpty(knownFieldName) || string.IsNullOrEmpty(tableName)) return suggestions;

            // 去掉开头的 F，再加上 FBASE 前缀
            var nameWithoutF = knownFieldName.ToUpperInvariant();
            if (nameWithoutF.StartsWith("F"))
                nameWithoutF = nameWithoutF.Substring(1);
            var baseFieldName = "FBASE" + nameWithoutF;

            // 优先从字典中查找
            if (allFields != null)
            {
                var dictMatch = allFields.FirstOrDefault(f =>
                    f.FieldName.Equals(baseFieldName, StringComparison.OrdinalIgnoreCase));
                if (dictMatch != null)
                {
                    suggestions.Add(baseFieldName);
                    return suggestions;
                }
            }

            // 字典中未找到，探测物理表
            string sql = @"SELECT c.name FROM sys.columns c
                           INNER JOIN sys.tables t ON c.object_id = t.object_id
                           WHERE t.name = @TableName AND c.name = @FieldName";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    cmd.Parameters.AddWithValue("@FieldName", baseFieldName);
                    cmd.CommandTimeout = 10;
                    var result = cmd.ExecuteScalar();
                    if (result != null) suggestions.Add(result.ToString());
                }
            }
            return suggestions;
        }

        /// <summary>
        /// 根据拆分表名生成简短别名，如 t_PUR_POOrderEntry_D → po_d
        /// </summary>
        private static string GenerateSplitAlias(string splitTableName)
        {
            if (string.IsNullOrEmpty(splitTableName)) return "st";
            // 去掉前缀 t_ 或 T_，取最后两段用下划线连接
            var name = splitTableName;
            if (name.StartsWith("t_", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(2);
            var parts = name.Split('_');
            if (parts.Length >= 2)
            {
                // 取倒数第二段的首字母 + 最后一段
                return parts[parts.Length - 2].Substring(0, 1).ToLowerInvariant() + "_" + parts[parts.Length - 1].ToLowerInvariant();
            }
            return name.ToLowerInvariant();
        }

        /// <summary>
        /// 批量探测物理表是否存在（用于检测 LK 关联表）
        /// 带超时保护和缓存机制
        /// </summary>
        public List<string> FindExistingTables(List<string> tableNames)
        {
            var existing = new List<string>();
            if (tableNames == null || tableNames.Count == 0) return existing;

            // 初始化缓存
            if (_lkTableCache == null)
                _lkTableCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 从缓存中查找
            var toQuery = tableNames.Where(t => !_lkTableCache.Contains(t)).ToList();
            var cached = tableNames.Where(t => _lkTableCache.Contains(t)).ToList();
            existing.AddRange(cached);

            if (toQuery.Count == 0) return existing;

            // 已超时则跳过
            if (_lkDetectionTimedOut) return existing;

            // 只按表名查询（不带 schema），使用短超时
            var paramNames = new List<string>();
            var parameters = new List<SqlParameter>();
            for (int i = 0; i < toQuery.Count; i++)
            {
                var paramName = "@T" + i;
                paramNames.Add(paramName);
                parameters.Add(new SqlParameter(paramName, toQuery[i]));
            }

            string sql = @"SELECT t.name FROM sys.tables t
                           WHERE t.name IN (" + string.Join(",", paramNames) + ")";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        cmd.CommandTimeout = 5; // 短超时 5 秒
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var name = reader["name"]?.ToString() ?? "";
                                existing.Add(name);
                                _lkTableCache.Add(name);
                            }
                        }
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == -2 || ex.Message.Contains("timeout"))
            {
                // 超时：标记并返回已有结果
                _lkDetectionTimedOut = true;
            }

            return existing;
        }

        /// <summary>
        /// 生成 SQL 辅助信息：物理表名、列名、JOIN 条件、行号字段、SQL 模板、LK 关联表提示
        /// </summary>
        public Dictionary<string, object> GenerateSqlHelper(string formIdentifier, string fieldKeywords)
        {
            EnsureContext();
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            var matchingFids = _allObjects.Keys
                .Where(k => k.Equals(formIdentifier, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingFids.Count == 0)
            {
                result["error"] = "未找到表单: " + formIdentifier;
                return result;
            }

            var fid = matchingFids[0];
            var metadata = ExtractMetadata(fid);
            if (metadata == null)
            {
                result["error"] = "无法提取表单元数据: " + formIdentifier;
                return result;
            }

            var allFields = metadata.FieldsWithOid.Concat(metadata.FieldsWithoutOid).ToList();
            var allEntities = metadata.EntitiesWithOid.Concat(metadata.EntitiesWithoutOid).ToList();
            var entityMap = allEntities.ToDictionary(e => e.Key, e => e, StringComparer.OrdinalIgnoreCase);

            // 识别单据头和明细体
            var headerEntity = allEntities.FirstOrDefault(e =>
                e.ElementType == "单据头" || (e.TagName != null && e.TagName.Contains("Head")));
            var entryEntity = allEntities.FirstOrDefault(e =>
                e.ElementType == "单据体" || (e.TagName != null && e.TagName.Contains("Entry")));

            // 识别行号字段和单据编号字段
            string seqField = null, billNoField = null;
            foreach (var field in allFields)
            {
                if (field.Key.Equals("FSeq", StringComparison.OrdinalIgnoreCase) ||
                    field.PropertyName.Equals("Seq", StringComparison.OrdinalIgnoreCase))
                    seqField = field.FieldName;
                if (field.ElementType == "12" || // BillNoField
                    field.Key.Equals("FBillNo", StringComparison.OrdinalIgnoreCase))
                    billNoField = field.FieldName;
            }

            // 如果字典中未找到行号字段，通过物理表探测 FSEQ
            if (seqField == null && entryEntity != null && !string.IsNullOrEmpty(entryEntity.TableName))
            {
                var probeResults = ProbePhysicalColumns(entryEntity.TableName, "FSEQ");
                if (probeResults.Count > 0)
                    seqField = "FSEQ";
            }

            // 收集表信息
            var tables = new List<Dictionary<string, object>>();
            if (headerEntity != null)
                tables.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["alias"] = "h", ["table"] = headerEntity.TableName,
                    ["entityKey"] = headerEntity.Key, ["entityName"] = headerEntity.Name, ["type"] = "单据头"
                });
            if (entryEntity != null)
                tables.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["alias"] = "e", ["table"] = entryEntity.TableName,
                    ["entityKey"] = entryEntity.Key, ["entityName"] = entryEntity.Name, ["type"] = "明细体"
                });

            // 解析目标字段（支持逗号分隔，中英文均可）
            var keywords = fieldKeywords
                .Split(new[] { ',', '\uFF0C' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrEmpty(k))
                .ToList();

            var matchedFields = new List<Dictionary<string, object>>();
            var unmatchedKeywords = new List<string>();

            foreach (var kw in keywords)
            {
                var normalizedKw = NormalizeKeyword(kw);
                var kwLower = kw.ToLowerInvariant();

                var match = allFields.FirstOrDefault(f =>
                    f.Key.Equals(kw, StringComparison.OrdinalIgnoreCase) ||
                    f.FieldName.Equals(kw, StringComparison.OrdinalIgnoreCase) ||
                    f.Name.Equals(kw, StringComparison.OrdinalIgnoreCase) ||
                    NormalizedContains(f.Name, normalizedKw) ||
                    f.Key.ToLowerInvariant().Contains(kwLower) ||
                    f.FieldName.ToLowerInvariant().Contains(kwLower));

                if (match != null)
                {
                    var entity = entityMap.ContainsKey(match.EntityKey) ? entityMap[match.EntityKey] : null;
                    var splitSuffix = match.Suffix ?? "";
                    var splitTable = !string.IsNullOrEmpty(splitSuffix)
                        ? (entity?.TableName ?? "") + "_" + splitSuffix
                        : "";

                    var fieldInfo = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["searchKeyword"] = kw,
                        ["name"] = match.Name,
                        ["key"] = match.Key,
                        ["fieldName"] = match.FieldName,
                        ["table"] = entity?.TableName ?? "",
                        ["splitSuffix"] = splitSuffix,
                        ["splitTable"] = splitTable,
                        ["entityKey"] = match.EntityKey,
                        ["elementType"] = match.ElementType,
                        ["elementTypeName"] = GetElementTypeName(match.ElementType)
                    };

                    // 推测基本单位字段
                    var baseSuggestions = SuggestBaseFields(entity?.TableName ?? "", match.FieldName, allFields);
                    if (baseSuggestions.Count > 0)
                        fieldInfo["suggestedBaseFields"] = baseSuggestions;

                    matchedFields.Add(fieldInfo);
                }
                else
                {
                    unmatchedKeywords.Add(kw);
                }
            }

            // 生成 SQL 模板（支持拆分表）
            var headerTable = headerEntity?.TableName ?? "";
            var entryTable = entryEntity?.TableName ?? "";
            var pkField = entryEntity?.EffectivePkFieldName ?? "FEntryId";
            var billNoCond = !string.IsNullOrEmpty(billNoField) ? "h." + billNoField + " = @BillNo" : "";
            var seqCond = !string.IsNullOrEmpty(seqField) ? " AND e." + seqField + " = @Seq" : "";

            // 收集需要 JOIN 的拆分表（去重）
            var splitTables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // alias -> tableName
            foreach (var f in matchedFields)
            {
                var st = f.GetValueOrDefault("splitTable")?.ToString() ?? "";
                if (!string.IsNullOrEmpty(st) && !splitTables.ContainsKey(st))
                {
                    // 生成别名：表名最后一段 + 后缀，如 po_d
                    var alias = GenerateSplitAlias(st);
                    splitTables[st] = alias;
                }
            }

            // SELECT 模板
            var selectCols = matchedFields.Select(f =>
            {
                var st = f.GetValueOrDefault("splitTable")?.ToString() ?? "";
                if (!string.IsNullOrEmpty(st) && splitTables.ContainsKey(st))
                {
                    return "    " + splitTables[st] + "." + f["fieldName"] + " AS [" + f["name"] + "]";
                }
                var tbl = f["table"]?.ToString() ?? "";
                var alias = tbl == entryTable ? "e" : "h";
                return "    " + alias + "." + f["fieldName"] + " AS [" + f["name"] + "]";
            });

            var selectSql = "SELECT\n" + string.Join(",\n", selectCols) + "\nFROM " + headerTable + " h";
            if (entryEntity != null)
                selectSql += "\nINNER JOIN " + entryTable + " e ON e.FID = h.FID";
            foreach (var kvp in splitTables)
            {
                selectSql += "\nINNER JOIN " + kvp.Key + " " + kvp.Value + " ON " + kvp.Value + ".FENTRYID = e.FENTRYID";
            }
            selectSql += "\nWHERE " + billNoCond + (entryEntity != null ? seqCond : "") + ";";

            // UPDATE 模板
            var updateSql = "";
            if (entryEntity != null && matchedFields.Count > 0)
            {
                // 检查是否有拆分表字段
                var hasSplitFields = matchedFields.Any(f => !string.IsNullOrEmpty(f.GetValueOrDefault("splitTable")?.ToString()));

                if (hasSplitFields)
                {
                    // 按拆分表分组生成 UPDATE
                    var splitGroups = matchedFields.GroupBy(f => f.GetValueOrDefault("splitTable")?.ToString() ?? "");
                    foreach (var group in splitGroups)
                    {
                        var groupName = group.Key ?? "";
                        var setClauses = group.Select(f => "    " + f["fieldName"] + " = @NewValue_" + f["fieldName"]);

                        if (string.IsNullOrEmpty(groupName))
                        {
                            // 主表字段
                            updateSql += "UPDATE " + entryTable + "\nSET\n" + string.Join(",\n", setClauses)
                                + "\nWHERE " + pkField + " = (\n    SELECT e." + pkField
                                + "\n    FROM " + entryTable + " e"
                                + "\n    INNER JOIN " + headerTable + " h ON e.FID = h.FID"
                                + "\n    WHERE " + billNoCond + seqCond
                                + "\n);\n\n";
                        }
                        else
                        {
                            // 拆分表字段
                            var alias = splitTables.ContainsKey(groupName) ? splitTables[groupName] : "st";
                            updateSql += "UPDATE " + groupName + "\nSET\n" + string.Join(",\n", setClauses)
                                + "\nWHERE FENTRYID = (\n    SELECT e.FENTRYID"
                                + "\n    FROM " + entryTable + " e"
                                + "\n    INNER JOIN " + headerTable + " h ON e.FID = h.FID"
                                + "\n    WHERE " + billNoCond + seqCond
                                + "\n);\n\n";
                        }
                    }
                    // 去掉末尾多余换行
                    updateSql = updateSql.TrimEnd();
                }
                else
                {
                    var setClauses = matchedFields.Select(f =>
                        "    " + f["fieldName"] + " = @NewValue_" + f["fieldName"]);

                    updateSql = "UPDATE " + entryTable + "\nSET\n" + string.Join(",\n", setClauses)
                        + "\nWHERE " + pkField + " = (\n    SELECT e." + pkField
                        + "\n    FROM " + entryTable + " e"
                        + "\n    INNER JOIN " + headerTable + " h ON e.FID = h.FID"
                        + "\n    WHERE " + billNoCond + seqCond
                        + "\n);";
                }
            }

            // 批量检测 LK 关联表
            var lkTableNames = allEntities
                .Where(e => !string.IsNullOrEmpty(e.TableName))
                .Select(e => e.TableName + "_LK")
                .ToList();

            var existingLkTables = FindExistingTables(lkTableNames);
            var existingLkSet = new HashSet<string>(existingLkTables, StringComparer.OrdinalIgnoreCase);

            var lkTables = new List<Dictionary<string, object>>();
            foreach (var entity in allEntities)
            {
                if (string.IsNullOrEmpty(entity.TableName)) continue;
                var lkTableName = entity.TableName + "_LK";
                if (existingLkSet.Contains(lkTableName))
                {
                    lkTables.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["lkTable"] = lkTableName,
                        ["entityTable"] = entity.TableName,
                        ["entityKey"] = entity.Key,
                        ["entityName"] = entity.Name,
                        ["joinCondition"] = "lk.FENTRYID = e." + entity.EffectivePkFieldName,
                        ["sourceJoinCondition"] = "lk.FSBILLID = src.FID AND lk.FSID = src." + entity.EffectivePkFieldName,
                        ["description"] = entity.TableName + " 的关联关系表，用于追溯上下游单据关系"
                    });
                }
            }

            // 组装结果
            result["formIdentifier"] = formIdentifier;
            result["formName"] = _allObjects[fid].FName;
            result["tables"] = tables;
            result["seqField"] = seqField ?? "(未找到行号字段)";
            result["billNoField"] = billNoField ?? "(未找到单据编号字段)";
            result["matchedFields"] = matchedFields;

            if (lkTables.Count > 0)
            {
                result["lkTables"] = lkTables;
                result["lkHint"] = "发现 " + lkTables.Count + " 个 LK 关联表。LK 表用于存储单据转换后的上下游关联关系，可通过 FSBILLID（源单单据头ID）和 FSID（源单明细ID）追溯源单。";
            }
            else if (_lkDetectionTimedOut)
            {
                result["lkTables"] = new List<object>();
                var timeoutHint = "LK 表检测超时，请手动确认是否存在关联表";
                if (entryEntity != null)
                    timeoutHint += "（如 " + entryEntity.TableName + "_LK）";
                timeoutHint += "。可使用 probe 命令验证：k3cli probe --table " + (entryEntity?.TableName ?? "") + "_LK";
                result["lkHint"] = timeoutHint;
            }

            if (unmatchedKeywords.Count > 0)
            {
                result["unmatchedKeywords"] = unmatchedKeywords;
                result["hint"] = "以下关键词未匹配到字段，可能是字典未收录。请使用 probe 命令探测物理表列。";
            }

            result["selectSql"] = selectSql;
            result["updateSql"] = updateSql;

            return result;
        }

        /// <summary>
        /// 提取指定 FID 的完整元数据
        /// </summary>
        private MetadataResult ExtractMetadata(string fid)
        {
            var fullChain = _context.BuildFullChain(fid);
            if (fullChain.Count == 0) return null;

            // 加载所需的 XML
            var xmlCache = MetadataDbHelper.LoadKernelXmlBatch(_connectionString, fullChain);

            // 提取元数据
            return MetadataExtractor.ExtractByFid(_context, fid, xmlCache);
        }

        /// <summary>
        /// 执行通用 SQL 查询（用于常用代码查询功能）
        /// </summary>
        public List<Dictionary<string, object>> ExecuteSql(string sql, Dictionary<string, object> parameters = null)
        {
            var results = new List<Dictionary<string, object>>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (parameters != null)
                    {
                        foreach (var kvp in parameters)
                        {
                            cmd.Parameters.AddWithValue(kvp.Key, kvp.Value);
                        }
                    }
                    cmd.CommandTimeout = 60;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.GetValue(i) == DBNull.Value ? "" : reader.GetValue(i);
                            }
                            results.Add(row);
                        }
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// 查询用户许可分配
        /// </summary>
        public List<Dictionary<string, object>> QueryUserLicenses(string orgName = null, string userName = null)
        {
            string sql = @"
SELECT org_l.FNAME               AS [组织名称],
       org.FNUMBER               AS [组织编码],
       u.FUSERID                 AS [用户ID],
       u.FNAME                   AS [用户名称],
       LTRIM(RTRIM(app.value))   AS [许可分组代码],
       CASE LTRIM(RTRIM(app.value))
           WHEN 'FIN' THEN '财务会计云'
           WHEN 'SCM' THEN '供应链云'
           WHEN 'FIN_SCM' THEN '财务会计+供应链'
           WHEN 'MFG' THEN '智能制造云'
           WHEN 'FIN_SCM_MFG' THEN '财务会计+供应链+标准制造'
           WHEN 'MFG_AdvMFG' THEN '高级制造云'
           WHEN 'FIN_SCM_MFG_AdvMFG' THEN '财务会计+供应链+高级制造'
           WHEN 'MA' THEN '管理会计云'
           WHEN 'BMCloud' THEN '预算管理云'
           WHEN 'CRCloud' THEN '合并报表云'
           WHEN 'QM' THEN '质量管理云'
           WHEN 'B2C_EBus' THEN 'B2C电商云'
           WHEN 'AllChannels' THEN '全渠道营销云'
           WHEN 'BBC' THEN 'BBC营销云'
           WHEN 'CRM' THEN '客户关系管理'
           WHEN 'SupplierCollaboration' THEN '供应协同云'
           WHEN 'EmployeeService' THEN '员工服务云'
           WHEN 'PLM' THEN 'PLM云'
           WHEN 'BI' THEN '经营分析'
           WHEN 'QING' THEN '数据服务云'
           WHEN 'BOS' THEN 'BOS运行平台'
           WHEN 'BOS_Indie' THEN 'BOS运行时-独立开发'
           WHEN 'BOS_Integration' THEN 'BOS运行平台-融合开发'
           WHEN 'BOS_ISV' THEN '行业产品BOS运行平台'
           WHEN 'BOS_Mobile' THEN '移动BOS运行平台'
           WHEN 'Pro' THEN '专业应用组'
           WHEN 'All' THEN '全员应用组'
           WHEN 'ViewOnly' THEN '仅查询应用'
           WHEN 'K3Cloud_ERP_RI' THEN '零售云'
           WHEN 'SmartShop' THEN '智能导购助手'
           WHEN 'WisdomWorkshop' THEN '智慧车间MES云'
           WHEN 'DeviceCloud' THEN '设备云'
           WHEN 'EKanban' THEN '电子看板'
           WHEN 'Kanban' THEN '数字大屏'
           WHEN 'DSStock' THEN '动态安全库存'
           WHEN 'YDTM' THEN '移动条码'
           WHEN 'MobileReport' THEN '移动工序报工'
           ELSE LTRIM(RTRIM(app.value))
           END                   AS [许可分组名称]
FROM T_SEC_USER u
INNER JOIN T_SEC_USERORG uo ON uo.FUSERID = u.FUSERID
INNER JOIN T_ORG_ORGANIZATIONS org ON org.FORGID = uo.FORGID
INNER JOIN T_ORG_ORGANIZATIONS_L org_l ON org_l.FORGID = org.FORGID AND org_l.FLOCALEID = 2052
CROSS APPLY STRING_SPLIT(u.FAPPGROUP, ',') app
WHERE u.FFORBIDSTATUS = 'A'
  AND org.FDOCUMENTSTATUS = 'C'
  AND org.FFORBIDSTATUS = 'A'
  AND LTRIM(RTRIM(app.value)) <> ''";

            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(orgName))
            {
                sql += " AND org_l.FNAME LIKE @OrgName";
                parameters["@OrgName"] = "%" + orgName + "%";
            }
            if (!string.IsNullOrEmpty(userName))
            {
                sql += " AND u.FNAME LIKE @UserName";
                parameters["@UserName"] = "%" + userName + "%";
            }
            sql += " ORDER BY org_l.FNAME, [许可分组代码]";

            return ExecuteSql(sql, parameters);
        }

        /// <summary>
        /// 查询所有可用常用查询的列表
        /// </summary>
        public List<Dictionary<string, object>> GetAvailableQueries()
        {
            return new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = "user-licenses",
                    ["description"] = "查询用户许可分配（组织、用户、许可分组）",
                    ["parameters"] = "--org <组织名称关键词>, --user <用户名称关键词>"
                }
            };
        }
    }
}
