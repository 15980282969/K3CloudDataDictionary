using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using K3CloudDataDictionary.Models;

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
                             "IsDefault INTEGER NOT NULL DEFAULT 0)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<ConnectionInfo> LoadAll()
        {
            var list = new List<ConnectionInfo>();
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "SELECT Id, Name, ServerIp, Port, UserName, Password, Database, IsDefault FROM Connections ORDER BY Id";
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
                            Password = reader.GetString(5),
                            Database = reader.GetString(6),
                            IsDefault = reader.GetInt32(7) == 1
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
                string sql = "SELECT Id, Name, ServerIp, Port, UserName, Password, Database, IsDefault FROM Connections WHERE IsDefault = 1 LIMIT 1";
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
                            Password = reader.GetString(5),
                            Database = reader.GetString(6),
                            IsDefault = reader.GetInt32(7) == 1
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
                string sql = "INSERT INTO Connections (Name, ServerIp, Port, UserName, Password, Database, IsDefault) VALUES (@Name, @ServerIp, @Port, @UserName, @Password, @Database, @IsDefault)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", info.Name ?? "");
                    cmd.Parameters.AddWithValue("@ServerIp", info.ServerIp ?? "");
                    cmd.Parameters.AddWithValue("@Port", info.Port);
                    cmd.Parameters.AddWithValue("@UserName", info.UserName ?? "");
                    cmd.Parameters.AddWithValue("@Password", info.Password ?? "");
                    cmd.Parameters.AddWithValue("@Database", info.Database ?? "");
                    cmd.Parameters.AddWithValue("@IsDefault", info.IsDefault ? 1 : 0);
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
                string sql = "UPDATE Connections SET Name=@Name, ServerIp=@ServerIp, Port=@Port, UserName=@UserName, Password=@Password, Database=@Database, IsDefault=@IsDefault WHERE Id=@Id";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", info.Name ?? "");
                    cmd.Parameters.AddWithValue("@ServerIp", info.ServerIp ?? "");
                    cmd.Parameters.AddWithValue("@Port", info.Port);
                    cmd.Parameters.AddWithValue("@UserName", info.UserName ?? "");
                    cmd.Parameters.AddWithValue("@Password", info.Password ?? "");
                    cmd.Parameters.AddWithValue("@Database", info.Database ?? "");
                    cmd.Parameters.AddWithValue("@IsDefault", info.IsDefault ? 1 : 0);
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
    }
}
