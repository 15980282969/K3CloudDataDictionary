using System;
using System.Collections.Generic;
using K3CloudDataDictionary.Cli.Services;
using K3CloudDataDictionary.Cli;

namespace K3CloudDataDictionary.Cli.Commands
{
    /// <summary>
    /// enum 命令：查询枚举值列表（elementType=9 下拉列表）
    /// </summary>
    public static class EnumCommand
    {
        public static int Execute(string[] args, GlobalOptions options)
        {
            JsonOutputWriter.SetPrettyPrint(options.PrettyPrint);

            // 检查帮助
            if (args.Length == 0 || Program.HasOption(args, "help") || Program.HasOption(args, "h"))
            {
                HelpCommand.ShowEnumHelp();
                return 0;
            }

            // 获取必填参数
            var enumTypeId = Program.GetArgValue(args, "id");
            if (string.IsNullOrEmpty(enumTypeId))
            {
                JsonOutputWriter.WriteError("enum", "缺少必填参数 --id <enumTypeId>");
                HelpCommand.ShowEnumHelp();
                return 1;
            }

            try
            {
                var connectionString = Program.ResolveConnectionString(options);
                var service = new MetadataQueryService(connectionString);
                var results = service.QueryEnumItems(enumTypeId);

                // 转换为更友好的格式
                var output = new List<object>();
                foreach (var row in results)
                {
                    output.Add(new
                    {
                        id = row.GetValueOrDefault("FID")?.ToString() ?? "",
                        name = row.GetValueOrDefault("FNAME")?.ToString() ?? "",
                        value = row.GetValueOrDefault("FVALUE")?.ToString() ?? "",
                        enumId = row.GetValueOrDefault("FENUMID")?.ToString() ?? "",
                        caption = row.GetValueOrDefault("FCAPTION")?.ToString() ?? ""
                    });
                }

                JsonOutputWriter.WriteSuccess("enum", output);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("enum", ex.Message);
                return 1;
            }
        }
    }
}
