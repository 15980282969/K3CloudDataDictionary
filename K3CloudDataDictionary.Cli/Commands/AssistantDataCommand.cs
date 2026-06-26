using System;
using System.Collections.Generic;
using K3CloudDataDictionary.Cli.Services;
using K3CloudDataDictionary.Cli;

namespace K3CloudDataDictionary.Cli.Commands
{
    /// <summary>
    /// assistantdata 命令：查询辅助资料列表
    /// </summary>
    public static class AssistantDataCommand
    {
        public static int Execute(string[] args, GlobalOptions options)
        {
            JsonOutputWriter.SetPrettyPrint(options.PrettyPrint);

            // 检查帮助
            if (args.Length == 0 || Program.HasOption(args, "help") || Program.HasOption(args, "h"))
            {
                HelpCommand.ShowAssistantDataHelp();
                return 0;
            }

            // 获取必填参数
            var lookUpObjectId = Program.GetArgValue(args, "id");
            if (string.IsNullOrEmpty(lookUpObjectId))
            {
                JsonOutputWriter.WriteError("assistantdata", "缺少必填参数 --id <lookUpObjectId>");
                HelpCommand.ShowAssistantDataHelp();
                return 1;
            }

            try
            {
                var connectionString = Program.ResolveConnectionString(options);
                var service = new MetadataQueryService(connectionString);
                var results = service.QueryAssistantData(lookUpObjectId);

                // 转换为更友好的格式
                var output = new List<object>();
                foreach (var row in results)
                {
                    output.Add(new
                    {
                        id = row.GetValueOrDefault("FID")?.ToString() ?? "",
                        number = row.GetValueOrDefault("FNUMBER")?.ToString() ?? "",
                        name = row.GetValueOrDefault("FNAME")?.ToString() ?? "",
                        entryId = row.GetValueOrDefault("FENTRYID")?.ToString() ?? "",
                        entryNumber = row.GetValueOrDefault("FENTRYNUMBER")?.ToString() ?? "",
                        dataValue = row.GetValueOrDefault("FDATAVALUE")?.ToString() ?? ""
                    });
                }

                JsonOutputWriter.WriteSuccess("assistantdata", output);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("assistantdata", ex.Message);
                return 1;
            }
        }
    }
}
