using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
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

        public MetadataQueryService(string connectionString)
        {
            _connectionString = connectionString;
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
                                              field.Name.ToLowerInvariant().Contains(keywordLower) ||
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

                            results.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["FDJMC"] = _allObjects[fid].FName,
                                ["FENTITYNAME"] = entity?.Name ?? field.EntityKey,
                                ["FTABLENAME"] = entity?.TableName ?? "",
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
                                          field.Name.ToLowerInvariant().Contains(keywordLower) ||
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

                            results.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["FDJMC"] = objInfo.FName,
                                ["FENTITYNAME"] = entity?.Name ?? field.EntityKey,
                                ["FTABLENAME"] = entity?.TableName ?? "",
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
    }
}
