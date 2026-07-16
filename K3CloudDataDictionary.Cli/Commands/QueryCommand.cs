using System;
using System.Collections.Generic;
using System.Linq;
using K3CloudDataDictionary.Cli.Services;
using K3CloudDataDictionary.Cli;

namespace K3CloudDataDictionary.Cli.Commands
{
    /// <summary>
    /// query 命令：常用代码查询（快速调用预定义 SQL 返回数据）
    /// </summary>
    public static class QueryCommand
    {
        public static int Execute(string[] args, GlobalOptions options)
        {
            JsonOutputWriter.SetPrettyPrint(options.PrettyPrint);

            if (args.Length == 0 || Program.HasOption(args, "help") || Program.HasOption(args, "h"))
            {
                HelpCommand.ShowQueryHelp();
                return 0;
            }

            var queryName = args[0].ToLowerInvariant();
            var queryArgs = args.Skip(1).ToArray();

            try
            {
                var connectionString = Program.ResolveConnectionString(options);
                var service = new MetadataQueryService(connectionString);

                switch (queryName)
                {
                    case "user-licenses":
                        return ExecuteUserLicenses(queryArgs, service);

                    case "blocking":
                        return ExecuteBlocking(service);

                    case "list":
                        var queries = service.GetAvailableQueries();
                        JsonOutputWriter.WriteSuccess("query", queries);
                        return 0;

                    default:
                        JsonOutputWriter.WriteError("query", $"未知的查询名称: {queryName}。使用 'k3cli query list' 查看可用查询。");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("query", ex.Message);
                return 1;
            }
        }

        private static int ExecuteUserLicenses(string[] args, MetadataQueryService service)
        {
            var orgName = Program.GetArgValue(args, "org");
            var userName = Program.GetArgValue(args, "user");

            var results = service.QueryUserLicenses(orgName, userName);
            JsonOutputWriter.WriteSuccess("query", results);
            return 0;
        }

        private static int ExecuteBlocking(MetadataQueryService service)
        {
            var results = service.QueryBlockingProcesses();
            JsonOutputWriter.WriteSuccess("query", results);
            return 0;
        }
    }
}
