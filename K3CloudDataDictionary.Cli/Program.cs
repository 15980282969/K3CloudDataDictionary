using System;
using System.IO;
using System.Linq;
using System.Text;
using K3CloudDataDictionary.Cli.Commands;
using K3CloudDataDictionary.Cli.Services;
using K3CloudDataDictionary.Helpers;
using K3CloudDataDictionary.Models;

namespace K3CloudDataDictionary.Cli
{
    /// <summary>
    /// K3Cloud 数据字典 CLI 工具入口
    /// </summary>
    public class Program
    {
        public static int Main(string[] args)
        {
            // 输出被重定向（管道/文件）时，.NET Framework 默认按系统 ANSI(GBK) 编码写字节流，
            // 会让 JSON 中的中文对 UTF-8 消费方乱码；交互式控制台走 Unicode API，无需改动
            if (Console.IsOutputRedirected)
                Console.OutputEncoding = new UTF8Encoding(false);
            if (Console.IsErrorRedirected)
                Console.SetError(new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false)) { AutoFlush = true });

            // 确保 SQLite 数据库初始化（用于存储连接配置）
            SQLiteHelper.EnsureDatabase();

            if (args.Length == 0)
            {
                HelpCommand.ShowHelp();
                return 0;
            }

            var command = args[0].ToLowerInvariant();
            var commandArgs = args.Skip(1).ToArray();

            // 解析全局选项
            var globalOptions = ParseGlobalOptions(commandArgs);

            try
            {
                // 需要连接数据库的命令列表
                var commandsRequiringConnection = new[] {
                    "fields", "search", "form", "billtype", "billstatus",
                    "assistantdata", "enum", "resolve", "probe", "sql", "query"
                };

                // 对需要连接的命令，自动检测连接可达性
                if (commandsRequiringConnection.Contains(command))
                {
                    if (!CheckConnection(globalOptions, out string connectionString))
                    {
                        return 1;
                    }
                    // 将连接字符串存入 globalOptions 供命令使用
                    globalOptions.ResolvedConnectionString = connectionString;
                }

                switch (command)
                {
                    case "fields":
                        return FieldsCommand.Execute(commandArgs, globalOptions);
                    case "search":
                        return SearchCommand.Execute(commandArgs, globalOptions);
                    case "form":
                        return FormCommand.Execute(commandArgs, globalOptions);
                    case "billtype":
                        return BillTypeCommand.Execute(commandArgs, globalOptions);
                    case "billstatus":
                        return BillStatusCommand.Execute(commandArgs, globalOptions);
                    case "assistantdata":
                        return AssistantDataCommand.Execute(commandArgs, globalOptions);
                    case "enum":
                        return EnumCommand.Execute(commandArgs, globalOptions);
                    case "resolve":
                        return ResolveCommand.Execute(commandArgs, globalOptions);
                    case "connections":
                        return ConnectionsCommand.Execute(commandArgs, globalOptions);
                    case "probe":
                        return ProbeCommand.Execute(commandArgs, globalOptions);
                    case "sql":
                        return SqlHelperCommand.Execute(commandArgs, globalOptions);
                    case "query":
                        return QueryCommand.Execute(commandArgs, globalOptions);
                    case "help":
                    case "--help":
                    case "-h":
                        HelpCommand.ShowHelp();
                        return 0;
                    default:
                        Console.Error.WriteLine($"未知命令: {command}");
                        Console.Error.WriteLine("使用 'k3cli help' 查看帮助");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError(command, ex.Message);
                return 1;
            }
        }

        /// <summary>
        /// 解析全局选项
        /// </summary>
        private static GlobalOptions ParseGlobalOptions(string[] args)
        {
            var options = new GlobalOptions();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--connection":
                    case "-c":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int connId))
                        {
                            options.ConnectionId = connId;
                            i++;
                        }
                        break;
                    case "--pretty":
                        options.PrettyPrint = true;
                        break;
                }
            }

            return options;
        }

        /// <summary>
        /// 获取命令行参数值
        /// </summary>
        public static string GetArgValue(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    args[i].Equals("--" + name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        /// <summary>
        /// 检查是否包含某个选项（支持 --name 和 -n 两种格式）
        /// </summary>
        public static bool HasOption(string[] args, string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return args.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                                 a.Equals("--" + name, StringComparison.OrdinalIgnoreCase) ||
                                 a.Equals("-" + name[0], StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取 SQL Server 连接字符串（优先使用已解析的连接，否则重新解析）
        /// </summary>
        public static string ResolveConnectionString(GlobalOptions options)
        {
            // 如果已经通过自动连接检测解析了连接字符串，直接使用
            if (!string.IsNullOrEmpty(options.ResolvedConnectionString))
            {
                return options.ResolvedConnectionString;
            }

            // 如果指定了 connection id
            if (options.ConnectionId.HasValue)
            {
                var connections = SQLiteHelper.LoadAll();
                var conn = connections.FirstOrDefault(c => c.Id == options.ConnectionId.Value);
                if (conn != null)
                {
                    return conn.ConnectionString;
                }
                throw new Exception($"未找到 ID 为 {options.ConnectionId.Value} 的连接");
            }

            // 使用默认连接
            var defaultConn = SQLiteHelper.LoadDefault();
            if (defaultConn != null)
            {
                return defaultConn.ConnectionString;
            }

            throw new Exception("没有默认连接。请使用 --connection 参数指定连接，或先配置默认连接。使用 'k3cli connections add' 添加连接。");
        }

        /// <summary>
        /// 解析连接信息（包含连接 ID，用于记录成功连接）
        /// </summary>
        public static ConnectionInfo ResolveConnectionInfo(GlobalOptions options)
        {
            if (options.ConnectionId.HasValue)
            {
                var connections = SQLiteHelper.LoadAll();
                var conn = connections.FirstOrDefault(c => c.Id == options.ConnectionId.Value);
                if (conn != null) return conn;
                throw new Exception($"未找到 ID 为 {options.ConnectionId.Value} 的连接");
            }

            var defaultConn = SQLiteHelper.LoadDefault();
            if (defaultConn != null) return defaultConn;

            throw new Exception("没有默认连接。请使用 --connection 参数指定连接，或先配置默认连接。使用 'k3cli connections add' 添加连接。");
        }

        /// <summary>
        /// 自动检测连接可达性，失败时显示明确错误信息
        /// </summary>
        public static bool CheckConnection(GlobalOptions options, out string connectionString)
        {
            connectionString = null;
            try
            {
                var connInfo = ResolveConnectionInfo(options);
                connectionString = connInfo.ConnectionString;

                Console.Error.WriteLine($"正在检测连接: {connInfo.DisplayName}...");
                if (DbHelper.TestConnection(connectionString, out string error))
                {
                    // 记录成功连接时间
                    SQLiteHelper.RecordSuccessfulConnection(connInfo.Id);
                    return true;
                }
                else
                {
                    Console.Error.WriteLine($"连接检测失败: {error}");
                    Console.Error.WriteLine("网络连接失败，请检查 VPN 是否已开启或数据库连接配置是否正确");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"连接解析失败: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 全局选项
    /// </summary>
    public class GlobalOptions
    {
        /// <summary>连接 ID</summary>
        public int? ConnectionId { get; set; }

        /// <summary>是否格式化 JSON 输出</summary>
        public bool PrettyPrint { get; set; }

        /// <summary>已解析的连接字符串（由自动连接检测设置）</summary>
        public string ResolvedConnectionString { get; set; }
    }
}
