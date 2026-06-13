using K3CloudDataDictionary.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace K3CloudDataDictionary.Helpers
{
    public static class SQLiteHelper
    {
        private static readonly string DbPath = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "data", "connections.db");

        private static string ConnectionString => $"Data Source={DbPath};Version=3;";

        public static void EnsureDatabase()
        {
            var dir = System.IO.Path.GetDirectoryName(DbPath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "CREATE TABLE IF NOT EXISTS Connections (" +
                             "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                             "Name TEXT NOT NULL, " +
                             "ServerIp TEXT NOT NULL, " +
                             "Port INTEGER NOT NULL DEFAULT 1433, " +
                             "UserName TEXT NOT NULL, " +
                             "Password TEXT NOT NULL, " +
                             "Database TEXT NOT NULL, " +
                             "IsDefault INTEGER NOT NULL DEFAULT 0, " +
                             "LocalDbFileName TEXT)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // 迁移：为旧表添加 LocalDbFileName 列
                try
                {
                    using (var cmd = new SQLiteCommand("ALTER TABLE Connections ADD COLUMN LocalDbFileName TEXT", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { /* 列已存在则忽略 */ }
            }
        }

        public static List<ConnectionInfo> LoadAll()
        {
            var list = new List<ConnectionInfo>();
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "SELECT Id, Name, ServerIp, Port, UserName, Password, Database, IsDefault, LocalDbFileName FROM Connections ORDER BY Id";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ConnectionInfo
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            ServerIp = reader.GetString(2),
                            Port = reader.GetInt32(3),
                            UserName = reader.GetString(4),
                            Password = PasswordHelper.Decrypt(reader.GetString(5)),
                            Database = reader.GetString(6),
                            IsDefault = reader.GetInt32(7) == 1,
                            LocalDbFileName = reader.IsDBNull(8) ? null : reader.GetString(8)
                        });
                    }
                }
            }
            return list;
        }

        public static ConnectionInfo LoadDefault()
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "SELECT Id, Name, ServerIp, Port, UserName, Password, Database, IsDefault, LocalDbFileName FROM Connections WHERE IsDefault = 1 LIMIT 1";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ConnectionInfo
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            ServerIp = reader.GetString(2),
                            Port = reader.GetInt32(3),
                            UserName = reader.GetString(4),
                            Password = PasswordHelper.Decrypt(reader.GetString(5)),
                            Database = reader.GetString(6),
                            IsDefault = reader.GetInt32(7) == 1,
                            LocalDbFileName = reader.IsDBNull(8) ? null : reader.GetString(8)
                        };
                    }
                }
            }
            return null;
        }

        public static int Save(ConnectionInfo info)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                ClearDefaultFlag(conn);
                string sql = "INSERT INTO Connections (Name, ServerIp, Port, UserName, Password, Database, IsDefault, LocalDbFileName) VALUES (@Name, @ServerIp, @Port, @UserName, @Password, @Database, @IsDefault, @LocalDbFileName)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", info.Name ?? "");
                    cmd.Parameters.AddWithValue("@ServerIp", info.ServerIp ?? "");
                    cmd.Parameters.AddWithValue("@Port", info.Port);
                    cmd.Parameters.AddWithValue("@UserName", info.UserName ?? "");
                    cmd.Parameters.AddWithValue("@Password", PasswordHelper.Encrypt(info.Password ?? ""));
                    cmd.Parameters.AddWithValue("@Database", info.Database ?? "");
                    cmd.Parameters.AddWithValue("@IsDefault", info.IsDefault ? 1 : 0);
                    cmd.Parameters.AddWithValue("@LocalDbFileName", (object)info.LocalDbFileName ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new SQLiteCommand("SELECT last_insert_rowid()", conn))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public static void Update(ConnectionInfo info)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                if (info.IsDefault) ClearDefaultFlag(conn);
                string sql = "UPDATE Connections SET Name=@Name, ServerIp=@ServerIp, Port=@Port, UserName=@UserName, Password=@Password, Database=@Database, IsDefault=@IsDefault, LocalDbFileName=@LocalDbFileName WHERE Id=@Id";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", info.Name ?? "");
                    cmd.Parameters.AddWithValue("@ServerIp", info.ServerIp ?? "");
                    cmd.Parameters.AddWithValue("@Port", info.Port);
                    cmd.Parameters.AddWithValue("@UserName", info.UserName ?? "");
                    cmd.Parameters.AddWithValue("@Password", PasswordHelper.Encrypt(info.Password ?? ""));
                    cmd.Parameters.AddWithValue("@Database", info.Database ?? "");
                    cmd.Parameters.AddWithValue("@IsDefault", info.IsDefault ? 1 : 0);
                    cmd.Parameters.AddWithValue("@LocalDbFileName", (object)info.LocalDbFileName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Id", info.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void Delete(int id)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM Connections WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void SetDefault(int id)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                ClearDefaultFlag(conn);
                using (var cmd = new SQLiteCommand("UPDATE Connections SET IsDefault=1 WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void ClearDefaultFlag(SQLiteConnection conn)
        {
            using (var cmd = new SQLiteCommand("UPDATE Connections SET IsDefault=0 WHERE IsDefault=1", conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 获取本地数据文件夹路径
        /// </summary>
        public static string GetDataFolder()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        }

        /// <summary>
        /// 获取指定连接的本地数据文件完整路径
        /// </summary>
        public static string GetLocalDbPath(ConnectionInfo connection)
        {
            if (connection == null) return null;
            return Path.Combine(GetDataFolder(), connection.EffectiveLocalDbFileName);
        }

        /// <summary>
        /// 扫描 data 文件夹中的所有 .db 文件（排除 connections.db），返回本地数据文件信息列表
        /// </summary>
        public static List<LocalDataFileInfo> ScanLocalDataFiles()
        {
            var result = new List<LocalDataFileInfo>();
            var dataFolder = GetDataFolder();
            if (!Directory.Exists(dataFolder)) return result;

            var connections = LoadAll();

            foreach (var file in Directory.GetFiles(dataFolder, "*.db"))
            {
                var fileName = Path.GetFileName(file);
                if (fileName.Equals("connections.db", StringComparison.OrdinalIgnoreCase)) continue;

                var fi = new FileInfo(file);
                var info = new LocalDataFileInfo
                {
                    FileName = fileName,
                    FilePath = file,
                    FileSizeBytes = fi.Length,
                    LastModified = fi.LastWriteTime
                };

                // 查找关联的连接（文件名匹配连接的 EffectiveLocalDbFileName 即为自动生成）
                var conn = connections.FirstOrDefault(c =>
                    c.EffectiveLocalDbFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
                if (conn != null)
                {
                    info.AssociatedConnectionId = conn.Id;
                    info.AssociatedConnectionName = conn.DisplayName;
                    info.IsAutoGenerated = true;
                }

                result.Add(info);
            }

            return result;
        }

        /// <summary>
        /// 导入本地数据文件：将外部 .db 文件复制到 data 文件夹
        /// 导入的文件名不会与任何连接的预期文件名冲突
        /// </summary>
        public static string ImportLocalData(string sourceFilePath, string targetFileName = null)
        {
            var dataFolder = GetDataFolder();
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            if (string.IsNullOrWhiteSpace(targetFileName))
                targetFileName = Path.GetFileName(sourceFilePath);

            // 避免导入文件名与任何连接的预期文件名冲突（防止被误识别为自动生成）
            var connections = LoadAll();
            var reservedNames = new HashSet<string>(connections.Select(c => c.EffectiveLocalDbFileName), StringComparer.OrdinalIgnoreCase);
            reservedNames.Add("connections.db");
            reservedNames.Add("metadata.db");

            if (reservedNames.Contains(targetFileName))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(targetFileName);
                var ext = Path.GetExtension(targetFileName);
                int counter = 1;
                while (reservedNames.Contains($"{nameWithoutExt}_{counter}{ext}") ||
                       File.Exists(Path.Combine(dataFolder, $"{nameWithoutExt}_{counter}{ext}")))
                    counter++;
                targetFileName = $"{nameWithoutExt}_{counter}{ext}";
            }

            var targetPath = Path.Combine(dataFolder, targetFileName);

            // 如果目标文件已存在且不是同一文件，添加序号
            if (File.Exists(targetPath) && !string.Equals(
                Path.GetFullPath(sourceFilePath), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(targetFileName);
                var ext = Path.GetExtension(targetFileName);
                int counter = 1;
                while (File.Exists(Path.Combine(dataFolder, $"{nameWithoutExt}_{counter}{ext}")))
                    counter++;
                targetFileName = $"{nameWithoutExt}_{counter}{ext}";
                targetPath = Path.Combine(dataFolder, targetFileName);
            }

            File.Copy(sourceFilePath, targetPath, true);
            return targetFileName;
        }

        /// <summary>
        /// 删除本地数据文件
        /// </summary>
        public static void DeleteLocalData(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        /// <summary>
        /// 重命名本地数据文件（仅修改文件名，不改变路径）
        /// </summary>
        public static string RenameLocalData(string currentFilePath, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return null;

            var dir = Path.GetDirectoryName(currentFilePath);
            var ext = Path.GetExtension(currentFilePath);

            // 确保新名称以 .db 结尾
            if (!newName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                newName += ext;

            var newPath = Path.Combine(dir, newName);

            if (string.Equals(Path.GetFullPath(currentFilePath), Path.GetFullPath(newPath), StringComparison.OrdinalIgnoreCase))
                return currentFilePath; // 名称未变

            if (File.Exists(newPath))
                return null; // 目标文件已存在

            File.Move(currentFilePath, newPath);
            return newPath;
        }

        /// <summary>
        /// 迁移旧的 metadata.db 到按连接命名的新文件
        /// 仅当 metadata.db 是唯一的数据文件时才迁移（旧版升级场景）
        /// 如果已有其他 .db 文件，说明用户已在使用多文件模式，metadata.db 可能是导入的
        /// </summary>
        public static void MigrateOldMetadataDb(ConnectionInfo defaultConnection)
        {
            if (defaultConnection == null) return;

            var dataFolder = GetDataFolder();
            var oldPath = Path.Combine(dataFolder, "metadata.db");
            if (!File.Exists(oldPath)) return;

            // 检查是否有其他数据文件（排除 connections.db 和 metadata.db 自身）
            var otherDataFiles = Directory.GetFiles(dataFolder, "*.db")
                .Where(f => !Path.GetFileName(f).Equals("connections.db", StringComparison.OrdinalIgnoreCase)
                         && !Path.GetFileName(f).Equals("metadata.db", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // 只有在没有其他数据文件时才迁移（旧版升级场景）
            if (otherDataFiles.Count > 0) return;

            var newPath = Path.Combine(dataFolder, defaultConnection.EffectiveLocalDbFileName);
            if (!string.Equals(Path.GetFullPath(oldPath), Path.GetFullPath(newPath), StringComparison.OrdinalIgnoreCase))
            {
                File.Move(oldPath, newPath);
            }
        }
    }
}
