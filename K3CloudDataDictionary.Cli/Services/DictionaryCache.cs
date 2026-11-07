using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using K3CloudDataDictionary.Helpers;
using K3CloudDataDictionary.Views;

namespace K3CloudDataDictionary.Cli.Services
{
    /// <summary>
    /// 元数据字典本地缓存：把 T_META_OBJECTTYPE 对象清单和元素类型名称落到 data/dictionary.db。
    /// 缓存键由「账套 + 元数据版本戳」哈希得到，版本戳变化即自然失效，
    /// 避免 k3cli 每次进程启动都重新拉取 5000+ 个对象（单次 5-10 秒）。
    /// </summary>
    public static class DictionaryCache
    {
        /// <summary>由 --refresh-dict 设置，忽略已有缓存强制重建（进程级开关）</summary>
        public static bool ForceRefresh { get; set; }

        private static string DbPath => Path.Combine(SQLiteHelper.GetDataFolder(), "dictionary.db");

        private static string SqliteConnectionString => $"Data Source={DbPath};Version=3;";

        /// <summary>
        /// 尝试读取缓存。命中时返回对象清单与元素类型名称，未命中或版本变化返回 false。
        /// </summary>
        public static bool TryLoad(SqlConnection connection,
                                   out Dictionary<string, ObjectBasicInfo> objects,
                                   out Dictionary<string, string> elementTypes)
        {
            objects = null;
            elementTypes = null;
            if (ForceRefresh) return false;

            string cacheKey;
            try
            {
                cacheKey = BuildCacheKey(connection);
            }
            catch
            {
                // 版本戳查询失败时不阻塞主流程，退回实时加载
                return false;
            }

            try
            {
                EnsureSchema();
                using (var conn = new SQLiteConnection(SqliteConnectionString))
                {
                    conn.Open();
                    using (var check = new SQLiteCommand("SELECT 1 FROM DictEntry WHERE CacheKey = @key", conn))
                    {
                        check.Parameters.AddWithValue("@key", cacheKey);
                        if (check.ExecuteScalar() == null) return false;
                    }

                    objects = LoadObjects(conn, cacheKey);
                    elementTypes = LoadElementTypes(conn, cacheKey);
                    if (objects.Count == 0) return false;

                    Console.Error.WriteLine($"字典缓存命中: {objects.Count} 个对象（{cacheKey.Substring(0, 8)}，可用 --refresh-dict 重建）");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"字典缓存读取失败，改为实时加载: {ex.Message}");
                objects = null;
                elementTypes = null;
                return false;
            }
        }

        /// <summary>
        /// 写入缓存（同一缓存键先清空旧数据，失败时静默放弃，不影响本次查询结果）
        /// </summary>
        public static void Save(SqlConnection connection,
                                Dictionary<string, ObjectBasicInfo> objects,
                                Dictionary<string, string> elementTypes)
        {
            if (objects == null || objects.Count == 0) return;

            try
            {
                var cacheKey = BuildCacheKey(connection);
                EnsureSchema();

                using (var conn = new SQLiteConnection(SqliteConnectionString))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        using (var del = new SQLiteCommand("DELETE FROM DictEntry WHERE CacheKey = @key; DELETE FROM DictObject WHERE CacheKey = @key; DELETE FROM DictElementType WHERE CacheKey = @key;", conn, tx))
                        {
                            del.Parameters.AddWithValue("@key", cacheKey);
                            del.ExecuteNonQuery();
                        }

                        using (var ins = new SQLiteCommand("INSERT INTO DictEntry (CacheKey, Account, Stamp, ObjectCount, CachedAt) VALUES (@key, @account, @stamp, @count, @cachedAt)", conn, tx))
                        {
                            ins.Parameters.AddWithValue("@key", cacheKey);
                            ins.Parameters.AddWithValue("@account", DescribeAccount(connection));
                            ins.Parameters.AddWithValue("@stamp", ReadStamp(connection));
                            ins.Parameters.AddWithValue("@count", objects.Count);
                            ins.Parameters.AddWithValue("@cachedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            ins.ExecuteNonQuery();
                        }

                        using (var ins = new SQLiteCommand("INSERT INTO DictObject (CacheKey, FID, FNAME, FSUBSYSID, FBASEOBJECTID, FMODELTYPEID, FINHERITPATH, FVERSION, FMAINVERSION, FDEVTYPE) VALUES (@key, @fid, @fname, @subsys, @base, @model, @inherit, @version, @mainVersion, @devType)", conn, tx))
                        {
                            var pKey = ins.Parameters.Add("@key", DbType.String);
                            var pFid = ins.Parameters.Add("@fid", DbType.String);
                            var pName = ins.Parameters.Add("@fname", DbType.String);
                            var pSubsys = ins.Parameters.Add("@subsys", DbType.String);
                            var pBase = ins.Parameters.Add("@base", DbType.String);
                            var pModel = ins.Parameters.Add("@model", DbType.String);
                            var pInherit = ins.Parameters.Add("@inherit", DbType.String);
                            var pVersion = ins.Parameters.Add("@version", DbType.String);
                            var pMain = ins.Parameters.Add("@mainVersion", DbType.String);
                            var pDev = ins.Parameters.Add("@devType", DbType.String);

                            foreach (var o in objects.Values)
                            {
                                pKey.Value = cacheKey;
                                pFid.Value = o.FId;
                                pName.Value = o.FName;
                                pSubsys.Value = o.FSubSysId;
                                pBase.Value = o.FBaseObjectId;
                                pModel.Value = o.FModelTypeId;
                                pInherit.Value = o.FInheritPath;
                                pVersion.Value = o.FVersion;
                                pMain.Value = o.FMainVersion;
                                pDev.Value = o.FDevType;
                                ins.ExecuteNonQuery();
                            }
                        }

                        using (var ins = new SQLiteCommand("INSERT INTO DictElementType (CacheKey, FID, FNAME) VALUES (@key, @fid, @fname)", conn, tx))
                        {
                            var pKey = ins.Parameters.Add("@key", DbType.String);
                            var pFid = ins.Parameters.Add("@fid", DbType.String);
                            var pName = ins.Parameters.Add("@fname", DbType.String);
                            foreach (var kvp in elementTypes)
                            {
                                pKey.Value = cacheKey;
                                pFid.Value = kvp.Key;
                                pName.Value = kvp.Value;
                                ins.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                    }
                }

                Console.Error.WriteLine($"字典缓存已写入: {objects.Count} 个对象（{cacheKey.Substring(0, 8)}）");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"字典缓存写入失败（不影响本次查询）: {ex.Message}");
            }
        }

        private static void EnsureSchema()
        {
            var dir = Path.GetDirectoryName(DbPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using (var conn = new SQLiteConnection(SqliteConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
CREATE TABLE IF NOT EXISTS DictEntry (
    CacheKey TEXT PRIMARY KEY,
    Account TEXT,
    Stamp TEXT,
    ObjectCount INTEGER,
    CachedAt TEXT);
CREATE TABLE IF NOT EXISTS DictObject (
    CacheKey TEXT NOT NULL,
    FID TEXT NOT NULL,
    FNAME TEXT, FSUBSYSID TEXT, FBASEOBJECTID TEXT, FMODELTYPEID TEXT,
    FINHERITPATH TEXT, FVERSION TEXT, FMAINVERSION TEXT, FDEVTYPE TEXT);
CREATE INDEX IF NOT EXISTS IX_DictObject_CacheKey ON DictObject(CacheKey);
CREATE TABLE IF NOT EXISTS DictElementType (
    CacheKey TEXT NOT NULL,
    FID TEXT NOT NULL,
    FNAME TEXT);
CREATE INDEX IF NOT EXISTS IX_DictElementType_CacheKey ON DictElementType(CacheKey);", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static Dictionary<string, ObjectBasicInfo> LoadObjects(SQLiteConnection conn, string cacheKey)
        {
            var result = new Dictionary<string, ObjectBasicInfo>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = new SQLiteCommand("SELECT * FROM DictObject WHERE CacheKey = @key", conn))
            {
                cmd.Parameters.AddWithValue("@key", cacheKey);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var fid = ToString(reader["FID"]);
                        if (string.IsNullOrEmpty(fid)) continue;
                        result[fid] = new ObjectBasicInfo
                        {
                            FId = fid,
                            FName = ToString(reader["FNAME"]),
                            FSubSysId = ToString(reader["FSUBSYSID"]),
                            FBaseObjectId = ToString(reader["FBASEOBJECTID"]),
                            FModelTypeId = ToString(reader["FMODELTYPEID"]),
                            FInheritPath = ToString(reader["FINHERITPATH"]),
                            FVersion = ToString(reader["FVERSION"]),
                            FMainVersion = ToString(reader["FMAINVERSION"]),
                            FDevType = ToString(reader["FDEVTYPE"])
                        };
                    }
                }
            }
            return result;
        }

        private static Dictionary<string, string> LoadElementTypes(SQLiteConnection conn, string cacheKey)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = new SQLiteCommand("SELECT FID, FNAME FROM DictElementType WHERE CacheKey = @key", conn))
            {
                cmd.Parameters.AddWithValue("@key", cacheKey);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var fid = ToString(reader["FID"]);
                        if (!string.IsNullOrEmpty(fid)) result[fid] = ToString(reader["FNAME"]);
                    }
                }
            }
            return result;
        }

        private static string BuildCacheKey(SqlConnection connection)
        {
            var raw = $"{DescribeAccount(connection)}|{ReadStamp(connection)}";
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw))).Replace("-", "").ToLowerInvariant();
            }
        }

        private static string DescribeAccount(SqlConnection connection)
        {
            var builder = new SqlConnectionStringBuilder(connection.ConnectionString);
            return $"{builder.DataSource}|{builder.InitialCatalog}";
        }

        /// <summary>
        /// 元数据版本戳：对象数量 + 最后修改时间，任一变化即视为字典过期
        /// </summary>
        private static string ReadStamp(SqlConnection connection)
        {
            const string sql = @"SELECT COUNT(*) AS C, MAX(A.FMODIFYDATE) AS M
                                 FROM T_META_OBJECTTYPE A
                                 INNER JOIN T_META_OBJECTTYPE_L L ON A.FID = L.FID AND L.FLOCALEID = 2052
                                 WHERE A.FMODELTYPEID IN (400, 100)";
            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.CommandTimeout = 30;
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    return $"{reader.GetInt32(0)}|{reader.GetValue(1)}";
                }
            }
        }

        private static string ToString(object value)
        {
            return value == null || value == DBNull.Value ? "" : value.ToString();
        }
    }
}
