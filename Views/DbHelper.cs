using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace K3CloudDataDictionary.Views
{
    /// <summary>
    /// 元数据对象类型基础信息（不含XML），用于内存构建继承链和扩展链
    /// </summary>
    public class ObjectBasicInfo
    {
        /// <summary>对象唯一标识</summary>
        public string FId { get; set; } = "";
        /// <summary>对象名称（来自T_META_OBJECTTYPE_L多语言表）</summary>
        public string FName { get; set; } = "";
        /// <summary>所属子系统标识</summary>
        public string FSubSysId { get; set; } = "";
        /// <summary>基础对象标识，用于扩展链中指向被扩展的对象</summary>
        public string FBaseObjectId { get; set; } = "";
        /// <summary>模型类型标识，400=基础资料，100=单据</summary>
        public string FModelTypeId { get; set; } = "";
        /// <summary>继承路径，逗号分隔的FID列表，表示从根到当前对象的继承链</summary>
        public string FInheritPath { get; set; } = "";
        /// <summary>版本号</summary>
        public string FVersion { get; set; } = "";
        /// <summary>主版本号</summary>
        public string FMainVersion { get; set; } = "";
        /// <summary>开发类型，0或1=基础资料/单据，2=扩展</summary>
        public string FDevType { get; set; } = "";
    }

    /// <summary>
    /// 元数据对象类型完整信息，包含内核XML
    /// </summary>
    public class ObjectTypeInfo : ObjectBasicInfo
    {
        /// <summary>内核XML内容，包含BusinessInfo下的Entity、Field、SplitTable等元数据</summary>
        public string FKernelXml { get; set; } = "";
    }

    /// <summary>
    /// 数据库查询辅助类，封装对T_META_OBJECTTYPE表的查询操作
    /// </summary>
    public static class MetadataDbHelper
    {
        /// <summary>
        /// 一次性查询所有元数据对象的基础信息（不含XML），用于内存构建继承链和扩展链
        /// 查询条件：FMODELTYPEID IN (400, 100)，不限制FDEVTYPE（包含扩展类型）
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <returns>以FID为键的ObjectBasicInfo字典</returns>
        public static Dictionary<string, ObjectBasicInfo> LoadAllObjectBasicInfo(string connectionString)
        {
            var result = new Dictionary<string, ObjectBasicInfo>();
            string sql = "SELECT A.FID, L.FNAME, A.FSUBSYSID, A.FBASEOBJECTID, A.FMODELTYPEID, A.FINHERITPATH, A.FVERSION, A.FMAINVERSION, A.FDEVTYPE FROM T_META_OBJECTTYPE A INNER JOIN T_META_OBJECTTYPE_L L ON A.FID = L.FID AND L.FLOCALEID = 2052 WHERE A.FMODELTYPEID IN (400, 100)";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
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
        /// 批量查询指定FID列表的内核XML内容，一次数据库连接获取所有XML
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="fids">需要查询XML的FID列表</param>
        /// <returns>以FID为键的XML内容字典</returns>
        public static Dictionary<string, string> LoadKernelXmlBatch(string connectionString, IEnumerable<string> fids)
        {
            var result = new Dictionary<string, string>();
            var fidList = fids.ToList();
            if (fidList.Count == 0)
                return result;

            var sb = new StringBuilder();
            sb.Append("SELECT FID, FKERNELXML.query('//BusinessInfo/BusinessInfo') FKERNELXML FROM T_META_OBJECTTYPE WHERE FID IN (");
            for (int i = 0; i < fidList.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"@P{i}");
            }
            sb.Append(")");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sb.ToString(), connection))
                {
                    for (int i = 0; i < fidList.Count; i++)
                    {
                        command.Parameters.AddWithValue($"@P{i}", fidList[i]);
                    }
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var fid = reader["FID"]?.ToString() ?? "";
                            var xml = reader["FKERNELXML"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(fid))
                            {
                                result[fid] = xml;
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 根据FID查询元数据对象类型的完整信息，包括多语言名称和内核XML
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="fid">要查询的对象FID</param>
        /// <returns>查询到的ObjectTypeInfo，未找到则返回null</returns>
        public static ObjectTypeInfo QueryObjectType(string connectionString, string fid)
        {
            string sql = "SELECT A.FID, L.FNAME, A.FSUBSYSID, A.FBASEOBJECTID, A.FMODELTYPEID, A.FINHERITPATH, A.FVERSION, A.FMAINVERSION, A.FDEVTYPE, A.FKERNELXML.query('//BusinessInfo/BusinessInfo') FKERNELXML FROM T_META_OBJECTTYPE A INNER JOIN T_META_OBJECTTYPE_L L ON A.FID = L.FID AND L.FLOCALEID = 2052 WHERE A.FID = @Fid";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Fid", fid);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ObjectTypeInfo
                            {
                                FId = reader["FID"]?.ToString() ?? "",
                                FName = reader["FNAME"]?.ToString() ?? "",
                                FSubSysId = reader["FSUBSYSID"]?.ToString() ?? "",
                                FBaseObjectId = reader["FBASEOBJECTID"]?.ToString() ?? "",
                                FModelTypeId = reader["FMODELTYPEID"]?.ToString() ?? "",
                                FInheritPath = reader["FINHERITPATH"]?.ToString() ?? "",
                                FVersion = reader["FVERSION"]?.ToString() ?? "",
                                FMainVersion = reader["FMAINVERSION"]?.ToString() ?? "",
                                FDevType = reader["FDEVTYPE"]?.ToString() ?? "",
                                FKernelXml = reader["FKERNELXML"]?.ToString() ?? ""
                            };
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 根据FID查询内核XML内容
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="fid">要查询的对象FID</param>
        /// <returns>内核XML字符串，未找到则返回null</returns>
        public static string QueryFKernelXML(string connectionString, string fid)
        {
            var info = QueryObjectType(connectionString, fid);
            return info?.FKernelXml;
        }

        /// <summary>
        /// 查询符合条件的FID列表，按指定条数限制返回
        /// 条件：FMODELTYPEID IN (400, 100) 且 FDEVTYPE < 2（排除扩展类型）
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="top">返回条数，0表示返回全部</param>
        /// <returns>FID列表</returns>
        public static List<string> QueryFids(string connectionString, int top = 0)
        {
            var result = new List<string>();
            var sql = top > 0
                ? $"SELECT TOP {top} FID FROM T_META_OBJECTTYPE WHERE FMODELTYPEID IN (400, 100) AND ISNULL(FDEVTYPE, 0) < 2"
                : "SELECT FID FROM T_META_OBJECTTYPE WHERE FMODELTYPEID IN (400, 100) AND ISNULL(FDEVTYPE, 0) < 2";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var fid = reader["FID"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(fid))
                            {
                                result.Add(fid);
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 查询指定FID是否存在且符合条件（FMODELTYPEID IN (400, 100) 且 FDEVTYPE < 2）
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="specificFid">指定的FID</param>
        /// <returns>符合条件的FID列表</returns>
        public static List<string> QueryFids(string connectionString, string specificFid)
        {
            var result = new List<string>();
            string sql = "SELECT FID FROM T_META_OBJECTTYPE WHERE FID = @Fid AND FMODELTYPEID IN (400, 100) AND ISNULL(FDEVTYPE, 0) < 2";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Fid", specificFid);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var fid = reader["FID"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(fid))
                            {
                                result.Add(fid);
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
