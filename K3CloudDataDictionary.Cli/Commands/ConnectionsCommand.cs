using System;
using System.Collections.Generic;
using System.Linq;
using K3CloudDataDictionary.Cli.Services;
using K3CloudDataDictionary.Helpers;
using K3CloudDataDictionary.Models;

namespace K3CloudDataDictionary.Cli.Commands
{
    /// <summary>
    /// connections 命令：管理数据库连接
    /// </summary>
    public static class ConnectionsCommand
    {
        public static int Execute(string[] args, GlobalOptions options)
        {
            JsonOutputWriter.SetPrettyPrint(options.PrettyPrint);

            // 检查帮助
            if (args.Length == 0 || Program.HasOption(args, "help") || Program.HasOption(args, "h"))
            {
                HelpCommand.ShowConnectionsHelp();
                return 0;
            }

            var subCommand = args[0].ToLowerInvariant();

            switch (subCommand)
            {
                case "list":
                    return ListConnections();
                case "add":
                    return AddConnection(args);
                case "update":
                    return UpdateConnection(args);
                case "delete":
                    return DeleteConnection(args);
                case "test":
                    return TestConnection(args);
                case "get-connection-string":
                    return GetConnectionString(args);
                case "set-default":
                    return SetDefaultConnection(args);
                default:
                    JsonOutputWriter.WriteError("connections", $"未知子命令: {subCommand}");
                    HelpCommand.ShowConnectionsHelp();
                    return 1;
            }
        }

        private static int ListConnections()
        {
            try
            {
                var connections = SQLiteHelper.LoadAll();
                var output = new List<object>();

                foreach (var conn in connections)
                {
                    output.Add(new
                    {
                        id = conn.Id,
                        name = conn.Name,
                        server = $"{conn.ServerIp},{conn.Port}",
                        database = conn.Database,
                        user = conn.UserName,
                        isDefault = conn.IsDefault,
                        displayName = conn.DisplayName,
                        lastSuccessfulConnection = conn.LastSuccessfulConnection ?? "从未连接"
                    });
                }

                JsonOutputWriter.WriteSuccess("connections", output);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("connections", ex.Message);
                return 1;
            }
        }

        private static int AddConnection(string[] args)
        {
            var server = Program.GetArgValue(args, "server");
            var port = Program.GetArgValue(args, "port");
            var db = Program.GetArgValue(args, "db");
            var user = Program.GetArgValue(args, "user");
            var password = Program.GetArgValue(args, "password");
            var name = Program.GetArgValue(args, "name");
            var setDefault = Program.HasOption(args, "default");

            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(db) || string.IsNullOrEmpty(user))
            {
                JsonOutputWriter.WriteError("connections", "缺少必填参数。需要 --server、--db、--user");
                return 1;
            }

            try
            {
                var conn = new ConnectionInfo
                {
                    Name = string.IsNullOrEmpty(name) ? db : name,
                    ServerIp = server,
                    Port = int.TryParse(port, out int p) ? p : 1433,
                    Database = db,
                    UserName = user,
                    Password = password ?? "",
                    IsDefault = setDefault
                };

                int newId = SQLiteHelper.Save(conn);

                var result = new
                {
                    id = newId,
                    name = conn.Name,
                    server = $"{conn.ServerIp},{conn.Port}",
                    database = conn.Database,
                    user = conn.UserName,
                    isDefault = setDefault,
                    message = "连接已保存"
                };

                JsonOutputWriter.WriteSuccess("connections", result);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("connections", ex.Message);
                return 1;
            }
        }

        private static int UpdateConnection(string[] args)
        {
            var idStr = Program.GetArgValue(args, "id");
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out int id))
            {
                JsonOutputWriter.WriteError("connections", "缺少参数 --id <connectionId>");
                return 1;
            }

            try
            {
                var conn = SQLiteHelper.LoadById(id);
                if (conn == null)
                {
                    JsonOutputWriter.WriteError("connections", $"未找到 ID 为 {id} 的连接");
                    return 1;
                }

                // 可选更新字段
                var server = Program.GetArgValue(args, "server");
                var port = Program.GetArgValue(args, "port");
                var db = Program.GetArgValue(args, "db");
                var user = Program.GetArgValue(args, "user");
                var password = Program.GetArgValue(args, "password");
                var name = Program.GetArgValue(args, "name");

                if (!string.IsNullOrEmpty(server)) conn.ServerIp = server;
                if (!string.IsNullOrEmpty(port) && int.TryParse(port, out int p)) conn.Port = p;
                if (!string.IsNullOrEmpty(db)) conn.Database = db;
                if (!string.IsNullOrEmpty(user)) conn.UserName = user;
                if (password != null) conn.Password = password;
                if (!string.IsNullOrEmpty(name)) conn.Name = name;

                SQLiteHelper.Update(conn);

                var result = new
                {
                    id = conn.Id,
                    name = conn.Name,
                    server = $"{conn.ServerIp},{conn.Port}",
                    database = conn.Database,
                    user = conn.UserName,
                    isDefault = conn.IsDefault,
                    message = "连接已更新"
                };

                JsonOutputWriter.WriteSuccess("connections", result);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("connections", ex.Message);
                return 1;
            }
        }

        private static int DeleteConnection(string[] args)
        {
            var idStr = Program.GetArgValue(args, "id");
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out int id))
            {
                JsonOutputWriter.WriteError("connections", "缺少参数 --id <connectionId>");
                return 1;
            }

            try
            {
                var conn = SQLiteHelper.LoadById(id);
                if (conn == null)
                {
                    JsonOutputWriter.WriteError("connections", $"未找到 ID 为 {id} 的连接");
                    return 1;
                }

                // 确认删除（--force 跳过确认）
                if (!Program.HasOption(args, "force"))
                {
                    Console.Error.Write($"确认删除连接 [{conn.Id}] {conn.DisplayName}? (y/N): ");
                    var input = Console.ReadLine();
                    if (!string.Equals(input, "y", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.Error.WriteLine("已取消删除");
                        return 0;
                    }
                }

                SQLiteHelper.Delete(id);

                var result = new
                {
                    id = conn.Id,
                    name = conn.Name,
                    database = conn.Database,
                    message = "连接已删除"
                };

                JsonOutputWriter.WriteSuccess("connections", result);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("connections", ex.Message);
                return 1;
            }
        }

        private static int SetDefaultConnection(string[] args)
        {
            var idStr = Program.GetArgValue(args, "id");
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out int id))
            {
                JsonOutputWriter.WriteError("connections", "缺少参数 --id <connectionId>");
                return 1;
            }

            try
            {
                var connections = SQLiteHelper.LoadAll();
                var conn = connections.FirstOrDefault(c => c.Id == id);
                if (conn == null)
                {
                    JsonOutputWriter.WriteError("connections", $"未找到 ID 为 {id} 的连接");
                    return 1;
                }

                SQLiteHelper.SetDefault(id);

                var result = new
                {
                    id = conn.Id,
                    name = conn.Name,
                    database = conn.Database,
                    isDefault = true,
                    message = "已设为默认连接"
                };

                JsonOutputWriter.WriteSuccess("connections", result);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("connections", ex.Message);
                return 1;
            }
        }

        private static int TestConnection(string[] args)
        {
            var idStr = Program.GetArgValue(args, "id");

            try
            {
                ConnectionInfo conn = null;

                if (!string.IsNullOrEmpty(idStr) && int.TryParse(idStr, out int id))
                {
                    // 指定了连接 ID
                    var connections = SQLiteHelper.LoadAll();
                    conn = connections.FirstOrDefault(c => c.Id == id);
                    if (conn == null)
                    {
                        JsonOutputWriter.WriteError("connections", $"未找到 ID 为 {id} 的连接");
                        return 1;
                    }
                }
                else
                {
                    // 未指定 ID，使用默认连接
                    conn = SQLiteHelper.LoadDefault();
                    if (conn == null)
                    {
                        JsonOutputWriter.WriteError("connections", "没有默认连接。请使用 --id 参数指定连接 ID，或先配置默认连接。");
                        return 1;
                    }
                    Console.Error.WriteLine($"未指定 --id，使用默认连接: {conn.DisplayName}");
                }

                Console.Error.WriteLine($"正在测试连接: {conn.DisplayName}...");

                if (DbHelper.TestConnection(conn.ConnectionString, out string error))
                {
                    // 记录成功连接时间
                    SQLiteHelper.RecordSuccessfulConnection(conn.Id);

                    var result = new
                    {
                        connectionId = conn.Id,
                        name = conn.Name,
                        server = $"{conn.ServerIp},{conn.Port}",
                        database = conn.Database,
                        success = true,
                        message = "连接成功",
                        lastSuccessfulConnection = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    JsonOutputWriter.WriteSuccess("connections", result);
                    return 0;
                }
                else
                {
                    var result = new
                    {
                        connectionId = conn.Id,
                        name = conn.Name,
                        server = $"{conn.ServerIp},{conn.Port}",
                        database = conn.Database,
                        success = false,
                        message = error
                    };
                    JsonOutputWriter.WriteSuccess("connections", result);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("connections", ex.Message);
                return 1;
            }
        }

        private static int GetConnectionString(string[] args)
        {
            var idStr = Program.GetArgValue(args, "id");

            try
            {
                ConnectionInfo conn = null;

                if (!string.IsNullOrEmpty(idStr) && int.TryParse(idStr, out int id))
                {
                    var connections = SQLiteHelper.LoadAll();
                    conn = connections.FirstOrDefault(c => c.Id == id);
                    if (conn == null)
                    {
                        JsonOutputWriter.WriteError("connections", $"未找到 ID 为 {id} 的连接");
                        return 1;
                    }
                }
                else
                {
                    conn = SQLiteHelper.LoadDefault();
                    if (conn == null)
                    {
                        JsonOutputWriter.WriteError("connections", "没有默认连接。请使用 --id 参数指定连接 ID，或先配置默认连接。");
                        return 1;
                    }
                    Console.Error.WriteLine($"未指定 --id，使用默认连接: {conn.DisplayName}");
                }

                var result = new
                {
                    connectionId = conn.Id,
                    name = conn.Name,
                    server = $"{conn.ServerIp},{conn.Port}",
                    database = conn.Database,
                    connectionString = conn.ConnectionString,
                    securityWarning = "连接字符串包含明文密码，请勿提交到代码库或分享给他人"
                };
                JsonOutputWriter.WriteSuccess("connections", result);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("connections", ex.Message);
                return 1;
            }
        }
    }
}
