using System;
using System.Collections.Generic;
using K3CloudDataDictionary.Cli.Services;
using K3CloudDataDictionary.Cli;

namespace K3CloudDataDictionary.Cli.Commands
{
    /// <summary>
    /// sql 命令：生成 SQL 辅助信息（物理表名、列名、JOIN 条件、SQL 模板）
    /// </summary>
    public static class SqlHelperCommand
    {
        public static int Execute(string[] args, GlobalOptions options)
        {
            JsonOutputWriter.SetPrettyPrint(options.PrettyPrint);

            if (args.Length == 0 || Program.HasOption(args, "help") || Program.HasOption(args, "h"))
            {
                HelpCommand.ShowSqlHelp();
                return 0;
            }

            var formIdentifier = Program.GetArgValue(args, "form");
            var fields = Program.GetArgValue(args, "fields");

            if (string.IsNullOrEmpty(formIdentifier))
            {
                JsonOutputWriter.WriteError("sql", "缺少必填参数 --form <identifier>");
                HelpCommand.ShowSqlHelp();
                return 1;
            }

            if (string.IsNullOrEmpty(fields))
            {
                JsonOutputWriter.WriteError("sql", "缺少必填参数 --fields <field1,field2,...>");
                HelpCommand.ShowSqlHelp();
                return 1;
            }

            try
            {
                var connectionString = Program.ResolveConnectionString(options);
                var service = new MetadataQueryService(connectionString);
                var result = service.GenerateSqlHelper(formIdentifier, fields);

                JsonOutputWriter.WriteSuccess("sql", result);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("sql", ex.Message);
                return 1;
            }
        }
    }
}
