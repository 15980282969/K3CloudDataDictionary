using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

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

        public MetadataSqliteWriter(string dbPath)
        {
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            _connection.Open();
            CreateTables();
            _transaction = _connection.BeginTransaction();
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
                    ExecuteNonQuery($"INSERT INTO T_MDL_ELEMENTTYPE_L (FID, FLOCALEID, FNAME) VALUES ({SqlStr(fid)}, {localeId}, {SqlStr(fname)})");
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
                    ExecuteNonQuery($"INSERT INTO T_META_TOPCLASS_L (FTOPCLASSID, FLOCALEID, FNAME) VALUES ({SqlStr(topClassId)}, {localeId}, {SqlStr(fname)})");
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
                    ExecuteNonQuery($"INSERT INTO T_META_SUBSYSTEM (FID, FTOPCLASSID, FNUMBER, FSEQ, FNAME, FDESCRIPTION) VALUES ({SqlStr(fid)}, {SqlStr(topClassId)}, {SqlStr(fnumber)}, {fseq}, {SqlStr(fname)}, {SqlStr(fdesc)})");
                }
            }
        }

        public void Write(MetadataResult result)
        {
            var objInfo = result.ObjInfo;
            var allEntities = result.EntitiesWithOid.Concat(result.EntitiesWithoutOid).ToList();
            var allFields = result.FieldsWithOid.Concat(result.FieldsWithoutOid).ToList();

            var currentFormId = _formId;

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

            ExecuteNonQuery($"INSERT INTO T_FORM (FID, FFORMIDENTIFIER, FNAME, FMODELTYPEID, FSUBSYSTEMID, FVERSION) VALUES ({currentFormId}, {SqlStr(result.Fid)}, {SqlStr(objInfo?.FName ?? "")}, {SqlStr(objInfo?.FModelTypeId ?? "")}, {SqlStr(objInfo?.FSubSysId ?? "")}, {SqlStr(objInfo?.FVersion ?? "")})");

            var tmpEntityId = _entityId - allEntities.Count;
            foreach (var entity in allEntities)
            {
                ExecuteNonQuery($"INSERT INTO T_ENTITY (FID, FFORMID, FKey, FEntryName, FName, FTableName, FEntryPkFieldName, FElementType) VALUES ({tmpEntityId}, {currentFormId}, {SqlStr(entity.Key)}, {SqlStr(entity.EntryName)}, {SqlStr(entity.Name)}, {SqlStr(entity.TableName)}, {SqlStr(entity.EntryPkFieldName)}, {SqlStr(entity.ElementType)})");
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
                ExecuteNonQuery($"INSERT INTO T_ENTITYSPLIT (FID, FFORMID, FENTITYID, FSUFFIX, FDESCRIPTION) VALUES ({tmpSplitId}, {currentFormId}, {parentEntityId}, {SqlStr(split.Suffix)}, {SqlStr(split.Description)})");
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

                ExecuteNonQuery($"INSERT INTO T_FIELD (FID, FFORMID, FENTITYID, FENTITYSPLITID, FKey, FName, FFieldName, FPropertyName, FElementType) VALUES ({tmpFieldId}, {currentFormId}, {fieldEntityId}, {fieldSplitId}, {SqlStr(field.Key)}, {SqlStr(field.Name)}, {SqlStr(field.FieldName)}, {SqlStr(field.PropertyName)}, {SqlStr(field.ElementType)})");
                tmpFieldId++;
            }
            _fieldId += allFields.Count;
            _formId++;
        }

        public void Flush()
        {
            if (_transaction != null)
            {
                _transaction.Commit();
                _transaction = _connection.BeginTransaction();
            }
        }

        private void ExecuteNonQuery(string sql)
        {
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static string SqlStr(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "NULL";
            return $"'{value.Replace("'", "''")}'";
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
