using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace K3CloudDataDictionary.Views
{
    public class MetadataSqliteWriter : IDisposable
    {
        private SQLiteConnection _connection;
        private SQLiteTransaction _transaction;
        private int _formId = 100001;
        private int _entityId = 100001;
        private int _entitySplitId = 100001;
        private int _fieldId = 100001;
        private bool _disposed;

        public MetadataSqliteWriter(string dbPath) : this(dbPath, true) { }

        /// <param name="dbPath">数据库路径</param>
        /// <param name="recreateTables">是否重建表（全量获取时为true，增量更新时为false）</param>
        public MetadataSqliteWriter(string dbPath, bool recreateTables)
        {
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            _connection.Open();
            if (recreateTables)
            {
                CreateTables();
            }
            else
            {
                InitIdCounters();
            }
            _transaction = _connection.BeginTransaction();
        }

        /// <summary>
        /// 从现有表中读取最大FID，作为增量写入的起始值
        /// </summary>
        private void InitIdCounters()
        {
            _formId = GetMaxId("T_FORM") + 1;
            _entityId = GetMaxId("T_ENTITY") + 1;
            _entitySplitId = GetMaxId("T_ENTITYSPLIT") + 1;
            _fieldId = GetMaxId("T_FIELD") + 1;
        }

        private int GetMaxId(string tableName)
        {
            using (var cmd = new SQLiteCommand($"SELECT IFNULL(MAX(FID), 100000) FROM {tableName}", _connection))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void CreateTables()
        {
            ExecuteNonQuery("DROP TABLE IF EXISTS T_FIELD");
            ExecuteNonQuery("DROP TABLE IF EXISTS T_ENTITYSPLIT");
            ExecuteNonQuery("DROP TABLE IF EXISTS T_ENTITY");
            ExecuteNonQuery("DROP TABLE IF EXISTS T_FORM");
            ExecuteNonQuery("DROP TABLE IF EXISTS T_MDL_ELEMENTTYPE_L");
            ExecuteNonQuery("DROP TABLE IF EXISTS T_META_SUBSYSTEM");
            ExecuteNonQuery("DROP TABLE IF EXISTS T_META_TOPCLASS_L");

            ExecuteNonQuery(@"CREATE TABLE T_FORM (
                FID             INTEGER NOT NULL PRIMARY KEY,
                FFORMIDENTIFIER TEXT,
                FNAME           TEXT,
                FMODELTYPEID    TEXT,
                FSUBSYSTEMID    TEXT,
                FVERSION        TEXT)");
            ExecuteNonQuery("CREATE UNIQUE INDEX IDX_T_FORM_IDENTIFIER ON T_FORM(FFORMIDENTIFIER)");

            ExecuteNonQuery(@"CREATE TABLE T_ENTITY (
                FID            INTEGER NOT NULL PRIMARY KEY,
                FFORMID        INTEGER NOT NULL,
                FKey           TEXT,
                FEntryName     TEXT,
                FName          TEXT,
                FTableName     TEXT,
                FEntryPkFieldName TEXT,
                FElementType   TEXT)");
            ExecuteNonQuery("CREATE INDEX IDX_T_ENTITY_FORMID ON T_ENTITY(FFORMID)");
            ExecuteNonQuery("CREATE INDEX IDX_T_ENTITY_TABLENAME ON T_ENTITY(FTABLENAME)");

            ExecuteNonQuery(@"CREATE TABLE T_ENTITYSPLIT (
                FID          INTEGER NOT NULL PRIMARY KEY,
                FFORMID      INTEGER NOT NULL,
                FENTITYID    INTEGER NOT NULL,
                FSUFFIX      TEXT,
                FDESCRIPTION TEXT)");
            ExecuteNonQuery("CREATE INDEX IDX_T_ENTITYSPLIT_FORMID ON T_ENTITYSPLIT(FFORMID)");
            ExecuteNonQuery("CREATE INDEX IDX_T_ENTITYSPLIT_ENTITYID ON T_ENTITYSPLIT(FENTITYID)");

            ExecuteNonQuery(@"CREATE TABLE T_FIELD (
                FID            INTEGER NOT NULL PRIMARY KEY,
                FFORMID        INTEGER NOT NULL,
                FENTITYID      INTEGER NOT NULL,
                FENTITYSPLITID INTEGER,
                FKey           TEXT,
                FName          TEXT,
                FFieldName     TEXT,
                FPropertyName  TEXT,
                FElementType   TEXT)");
            ExecuteNonQuery("CREATE INDEX IDX_T_FIELD_FORMID ON T_FIELD(FFORMID)");
            ExecuteNonQuery("CREATE INDEX IDX_T_FIELD_ENTITYID ON T_FIELD(FENTITYID)");
            ExecuteNonQuery("CREATE INDEX IDX_T_FIELD_ENTITYSPLITID ON T_FIELD(FENTITYSPLITID)");
            ExecuteNonQuery("CREATE INDEX IDX_T_FIELD_KEY ON T_FIELD(FKEY)");

            ExecuteNonQuery(@"CREATE TABLE T_MDL_ELEMENTTYPE_L (
                FID       TEXT NOT NULL,
                FLOCALEID INTEGER NOT NULL,
                FNAME     TEXT,
                PRIMARY KEY (FID, FLOCALEID))");

            ExecuteNonQuery(@"CREATE TABLE T_META_TOPCLASS_L (
                FTOPCLASSID TEXT NOT NULL,
                FLOCALEID   INTEGER NOT NULL,
                FNAME       TEXT,
                PRIMARY KEY (FTOPCLASSID, FLOCALEID))");

            ExecuteNonQuery(@"CREATE TABLE T_META_SUBSYSTEM (
                FID         TEXT NOT NULL PRIMARY KEY,
                FTOPCLASSID TEXT,
                FNUMBER     TEXT,
                FSEQ        INTEGER,
                FNAME       TEXT,
                FDESCRIPTION TEXT)");
        }

        public void WriteLookupTables(string sqlServerConnectionString)
        {
            using (var sqlConn = new System.Data.SqlClient.SqlConnection(sqlServerConnectionString))
            {
                sqlConn.Open();

                WriteElementTypeL(sqlConn);
                WriteTopClassL(sqlConn);
                WriteSubsystem(sqlConn);
            }
        }

        /// <summary>
        /// 根据表单标识列表删除本地数据（T_FIELD → T_ENTITYSPLIT → T_ENTITY → T_FORM 级联删除）
        /// </summary>
        public void DeleteFormsByIdentifiers(IEnumerable<string> formIdentifiers)
        {
            var idList = formIdentifiers.ToList();
            if (idList.Count == 0) return;

            // 先获取要删除的 FFORMID 列表
            var formIds = new List<long>();
            string placeholders = string.Join(",", idList.Select((_, i) => $"@P{i}"));
            var queryParams = idList.Select((id, i) => new SQLiteParameter($"@P{i}", id)).ToArray();

            using (var cmd = new SQLiteCommand($"SELECT FID FROM T_FORM WHERE FFORMIDENTIFIER IN ({placeholders})", _connection))
            {
                cmd.Parameters.AddRange(queryParams);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        formIds.Add(reader.GetInt64(0));
                    }
                }
            }

            if (formIds.Count == 0) return;

            // 级联删除：T_FIELD → T_ENTITYSPLIT → T_ENTITY → T_FORM
            var formIdParams = formIds.Select((id, i) => new SQLiteParameter($"@FID{i}", id)).ToArray();
            string fidPlaceholders = string.Join(",", formIds.Select((_, i) => $"@FID{i}"));

            ExecuteNonQuery($"DELETE FROM T_FIELD WHERE FFORMID IN ({fidPlaceholders})", formIdParams);
            ExecuteNonQuery($"DELETE FROM T_ENTITYSPLIT WHERE FFORMID IN ({fidPlaceholders})", formIdParams);
            ExecuteNonQuery($"DELETE FROM T_ENTITY WHERE FFORMID IN ({fidPlaceholders})", formIdParams);
            ExecuteNonQuery($"DELETE FROM T_FORM WHERE FID IN ({fidPlaceholders})", formIdParams);

            Flush();

            // 删除后重新初始化ID计数器
            InitIdCounters();
        }

        private void WriteElementTypeL(System.Data.SqlClient.SqlConnection sqlConn)
        {
            string sql = "SELECT FID, FLOCALEID, FNAME FROM T_MDL_ELEMENTTYPE_L";
            using (var cmd = new System.Data.SqlClient.SqlCommand(sql, sqlConn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var fid = reader["FID"]?.ToString() ?? "";
                    var localeId = reader["FLOCALEID"]?.ToString() ?? "0";
                    var fname = reader["FNAME"]?.ToString() ?? "";
                    ExecuteNonQuery("INSERT INTO T_MDL_ELEMENTTYPE_L (FID, FLOCALEID, FNAME) VALUES (@FID, @FLOCALEID, @FNAME)",
                        new SQLiteParameter("@FID", fid ?? (object)DBNull.Value),
                        new SQLiteParameter("@FLOCALEID", int.TryParse(localeId, out var lid) ? lid : 0),
                        new SQLiteParameter("@FNAME", fname ?? (object)DBNull.Value));
                }
            }
        }

        private void WriteTopClassL(System.Data.SqlClient.SqlConnection sqlConn)
        {
            string sql = "SELECT FTOPCLASSID, FLOCALEID, FNAME FROM T_META_TOPCLASS_L";
            using (var cmd = new System.Data.SqlClient.SqlCommand(sql, sqlConn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var topClassId = reader["FTOPCLASSID"]?.ToString() ?? "";
                    var localeId = reader["FLOCALEID"]?.ToString() ?? "0";
                    var fname = reader["FNAME"]?.ToString() ?? "";
                    ExecuteNonQuery("INSERT INTO T_META_TOPCLASS_L (FTOPCLASSID, FLOCALEID, FNAME) VALUES (@FTOPCLASSID, @FLOCALEID, @FNAME)",
                        new SQLiteParameter("@FTOPCLASSID", topClassId ?? (object)DBNull.Value),
                        new SQLiteParameter("@FLOCALEID", int.TryParse(localeId, out var lid) ? lid : 0),
                        new SQLiteParameter("@FNAME", fname ?? (object)DBNull.Value));
                }
            }
        }

        private void WriteSubsystem(System.Data.SqlClient.SqlConnection sqlConn)
        {
            string sql = "SELECT a.FID, a.FTOPCLASSID, a.FNUMBER, a.FSEQ, b.FNAME, b.FDESCRIPTION " +
                         "FROM T_META_SUBSYSTEM a " +
                         "INNER JOIN T_META_SUBSYSTEM_L b ON a.FID = b.FID AND b.FLOCALEID = 2052";
            using (var cmd = new System.Data.SqlClient.SqlCommand(sql, sqlConn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var fid = reader["FID"]?.ToString() ?? "";
                    var topClassId = reader["FTOPCLASSID"]?.ToString() ?? "";
                    var fnumber = reader["FNUMBER"]?.ToString() ?? "";
                    var fseq = reader["FSEQ"] is DBNull ? "0" : Convert.ToInt32(reader["FSEQ"]).ToString();
                    var fname = reader["FNAME"]?.ToString() ?? "";
                    var fdesc = reader["FDESCRIPTION"]?.ToString() ?? "";
                    ExecuteNonQuery("INSERT INTO T_META_SUBSYSTEM (FID, FTOPCLASSID, FNUMBER, FSEQ, FNAME, FDESCRIPTION) VALUES (@FID, @FTOPCLASSID, @FNUMBER, @FSEQ, @FNAME, @FDESCRIPTION)",
                        new SQLiteParameter("@FID", fid ?? (object)DBNull.Value),
                        new SQLiteParameter("@FTOPCLASSID", topClassId ?? (object)DBNull.Value),
                        new SQLiteParameter("@FNUMBER", fnumber ?? (object)DBNull.Value),
                        new SQLiteParameter("@FSEQ", int.TryParse(fseq, out var seqVal) ? seqVal : 0),
                        new SQLiteParameter("@FNAME", fname ?? (object)DBNull.Value),
                        new SQLiteParameter("@FDESCRIPTION", fdesc ?? (object)DBNull.Value));
                }
            }
        }

        public void Write(MetadataResult result)
        {
            var objInfo = result.ObjInfo;
            var allEntities = result.EntitiesWithOid.Concat(result.EntitiesWithoutOid).ToList();
            var allFields = result.FieldsWithOid.Concat(result.FieldsWithoutOid).ToList();

            var currentFormId = _formId;
            _formId++;

            var entityKeyToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var headEntityId = 0;
            foreach (var entity in allEntities)
            {
                if (!string.IsNullOrEmpty(entity.Key))
                {
                    entityKeyToId[entity.Key] = _entityId;
                }
                if (entity.TagName == "HeadEntity" || string.IsNullOrEmpty(entity.Key))
                {
                    headEntityId = _entityId;
                }
                _entityId++;
            }

            var splitKeyToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var split in result.Splits)
            {
                var splitKey = $"{split.EntityKey}_{split.Suffix}";
                splitKeyToId[splitKey] = _entitySplitId;
                _entitySplitId++;
            }

            ExecuteNonQuery("INSERT INTO T_FORM (FID, FFORMIDENTIFIER, FNAME, FMODELTYPEID, FSUBSYSTEMID, FVERSION) VALUES (@FID, @FFORMIDENTIFIER, @FNAME, @FMODELTYPEID, @FSUBSYSTEMID, @FVERSION)",
                new SQLiteParameter("@FID", currentFormId),
                new SQLiteParameter("@FFORMIDENTIFIER", result.Fid ?? (object)DBNull.Value),
                new SQLiteParameter("@FNAME", objInfo?.FName ?? (object)DBNull.Value),
                new SQLiteParameter("@FMODELTYPEID", objInfo?.FModelTypeId ?? (object)DBNull.Value),
                new SQLiteParameter("@FSUBSYSTEMID", objInfo?.FSubSysId ?? (object)DBNull.Value),
                new SQLiteParameter("@FVERSION", objInfo?.FVersion ?? (object)DBNull.Value));

            var tmpEntityId = _entityId - allEntities.Count;
            foreach (var entity in allEntities)
            {
                ExecuteNonQuery("INSERT INTO T_ENTITY (FID, FFORMID, FKey, FEntryName, FName, FTableName, FEntryPkFieldName, FElementType) VALUES (@FID, @FFORMID, @FKey, @FEntryName, @FName, @FTableName, @FEntryPkFieldName, @FElementType)",
                    new SQLiteParameter("@FID", tmpEntityId),
                    new SQLiteParameter("@FFORMID", currentFormId),
                    new SQLiteParameter("@FKey", entity.Key ?? (object)DBNull.Value),
                    new SQLiteParameter("@FEntryName", entity.EntryName ?? (object)DBNull.Value),
                    new SQLiteParameter("@FName", entity.Name ?? (object)DBNull.Value),
                    new SQLiteParameter("@FTableName", entity.TableName ?? (object)DBNull.Value),
                    new SQLiteParameter("@FEntryPkFieldName", entity.EntryPkFieldName ?? (object)DBNull.Value),
                    new SQLiteParameter("@FElementType", entity.ElementType ?? (object)DBNull.Value));
                tmpEntityId++;
            }

            var tmpSplitId = _entitySplitId - result.Splits.Count;
            foreach (var split in result.Splits)
            {
                var parentEntityId = headEntityId;
                if (!string.IsNullOrEmpty(split.EntityKey) && entityKeyToId.TryGetValue(split.EntityKey, out var eid))
                {
                    parentEntityId = eid;
                }
                ExecuteNonQuery("INSERT INTO T_ENTITYSPLIT (FID, FFORMID, FENTITYID, FSUFFIX, FDESCRIPTION) VALUES (@FID, @FFORMID, @FENTITYID, @FSUFFIX, @FDESCRIPTION)",
                    new SQLiteParameter("@FID", tmpSplitId),
                    new SQLiteParameter("@FFORMID", currentFormId),
                    new SQLiteParameter("@FENTITYID", parentEntityId),
                    new SQLiteParameter("@FSUFFIX", split.Suffix ?? (object)DBNull.Value),
                    new SQLiteParameter("@FDESCRIPTION", split.Description ?? (object)DBNull.Value));
                tmpSplitId++;
            }

            var tmpFieldId = _fieldId;
            foreach (var field in allFields)
            {
                var fieldEntityId = headEntityId;
                if (!string.IsNullOrEmpty(field.EntityKey) && entityKeyToId.TryGetValue(field.EntityKey, out var eid))
                {
                    fieldEntityId = eid;
                }

                int fieldSplitId = 0;
                if (!string.IsNullOrEmpty(field.Suffix))
                {
                    var splitLookupKey = $"{field.EntityKey}_{field.Suffix}";
                    if (!splitKeyToId.TryGetValue(splitLookupKey, out fieldSplitId))
                    {
                        foreach (var kv in splitKeyToId)
                        {
                            if (kv.Key.EndsWith($"_{field.Suffix}"))
                            {
                                fieldSplitId = kv.Value;
                                break;
                            }
                        }
                    }
                }

                ExecuteNonQuery("INSERT INTO T_FIELD (FID, FFORMID, FENTITYID, FENTITYSPLITID, FKey, FName, FFieldName, FPropertyName, FElementType) VALUES (@FID, @FFORMID, @FENTITYID, @FENTITYSPLITID, @FKey, @FName, @FFieldName, @FPropertyName, @FElementType)",
                    new SQLiteParameter("@FID", tmpFieldId),
                    new SQLiteParameter("@FFORMID", currentFormId),
                    new SQLiteParameter("@FENTITYID", fieldEntityId),
                    new SQLiteParameter("@FENTITYSPLITID", fieldSplitId),
                    new SQLiteParameter("@FKey", field.Key ?? (object)DBNull.Value),
                    new SQLiteParameter("@FName", field.Name ?? (object)DBNull.Value),
                    new SQLiteParameter("@FFieldName", field.FieldName ?? (object)DBNull.Value),
                    new SQLiteParameter("@FPropertyName", field.PropertyName ?? (object)DBNull.Value),
                    new SQLiteParameter("@FElementType", field.ElementType ?? (object)DBNull.Value));
                tmpFieldId++;
            }
            _fieldId += allFields.Count;
        }

        public void Flush()
        {
            if (_transaction != null)
            {
                _transaction.Commit();
                _transaction = _connection.BeginTransaction();
            }
        }

        private void ExecuteNonQuery(string sql, params SQLiteParameter[] parameters)
        {
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                if (parameters != null)
                {
                    foreach (var p in parameters)
                         cmd.Parameters.Add(p);
                }
                cmd.ExecuteNonQuery();
            }
        }


        public void Dispose()
        {
            if (!_disposed)
            {
                _transaction?.Commit();
                _transaction?.Dispose();
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }
}
