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
                case "test":
                    return TestConnection(args);
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
                        displayName = conn.DisplayName
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

                if (setDefault)
                {
                    SQLiteHelper.SetDefault(newId);
                }

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

                Console.Error.WriteLine($"正在测试连接: {conn.DisplayName}...");

                if (DbHelper.TestConnection(conn.ConnectionString, out string error))
                {
                    var result = new
                    {
                        connectionId = conn.Id,
                        name = conn.Name,
                        server = $"{conn.ServerIp},{conn.Port}",
                        database = conn.Database,
                        success = true,
                        message = "连接成功"
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
    }
}
