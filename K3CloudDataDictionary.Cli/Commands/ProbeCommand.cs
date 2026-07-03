using System;
using System.Collections.Generic;
using K3CloudDataDictionary.Cli.Services;
using K3CloudDataDictionary.Cli;

namespace K3CloudDataDictionary.Cli.Commands
{
    /// <summary>
    /// probe 命令：探测物理表中的列（解决字典未收录字段无法查询的问题）
    /// 支持通配符 * 匹配多个表名
    /// </summary>
    public static class ProbeCommand
    {
        public static int Execute(string[] args, GlobalOptions options)
        {
            JsonOutputWriter.SetPrettyPrint(options.PrettyPrint);

            if (args.Length == 0 || Program.HasOption(args, "help") || Program.HasOption(args, "h"))
            {
                HelpCommand.ShowProbeHelp();
                return 0;
            }

            var tableName = Program.GetArgValue(args, "table");
            var keyword = Program.GetArgValue(args, "keyword");

            if (string.IsNullOrEmpty(tableName))
            {
                JsonOutputWriter.WriteError("probe", "缺少必填参数 --table <tableName>");
                HelpCommand.ShowProbeHelp();
                return 1;
            }

            try
            {
                var connectionString = Program.ResolveConnectionString(options);
                var service = new MetadataQueryService(connectionString);

                // 判断是否包含通配符
                bool isPattern = tableName.Contains("*");
                List<Dictionary<string, object>> results;

                if (isPattern)
                {
                    // 批量模式匹配
                    results = service.ProbePhysicalColumnsByPattern(tableName, keyword);
                }
                else
                {
                    // 单表探测
                    results = service.ProbePhysicalColumns(tableName, keyword);
                }

                var output = new List<object>();
                foreach (var row in results)
                {
                    var item = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["columnName"] = row.GetValueOrDefault("columnName")?.ToString() ?? "",
                        ["dataType"] = row.GetValueOrDefault("dataType")?.ToString() ?? "",
                        ["maxLength"] = row.GetValueOrDefault("maxLength"),
                        ["precision"] = row.GetValueOrDefault("precision"),
                        ["scale"] = row.GetValueOrDefault("scale"),
                        ["isNullable"] = row.GetValueOrDefault("isNullable")
                    };

                    // 批量模式时附加表名
                    if (isPattern && row.ContainsKey("table"))
                    {
                        item["table"] = row.GetValueOrDefault("table")?.ToString() ?? "";
                    }

                    output.Add(item);
                }

                JsonOutputWriter.WriteSuccess("probe", output);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("probe", ex.Message);
                return 1;
            }
        }
    }
}
