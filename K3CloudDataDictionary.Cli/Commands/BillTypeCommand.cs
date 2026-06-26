using System;
using System.Collections.Generic;
using K3CloudDataDictionary.Cli.Services;
using K3CloudDataDictionary.Cli;

namespace K3CloudDataDictionary.Cli.Commands
{
    /// <summary>
    /// billtype 命令：查询单据类型（支持按表单查列表，或按 ID/关键词查详情）
    /// </summary>
    public static class BillTypeCommand
    {
        public static int Execute(string[] args, GlobalOptions options)
        {
            JsonOutputWriter.SetPrettyPrint(options.PrettyPrint);

            // 检查帮助
            if (args.Length == 0 || Program.HasOption(args, "help") || Program.HasOption(args, "h"))
            {
                HelpCommand.ShowBillTypeHelp();
                return 0;
            }

            // 获取参数
            var formIdentifier = Program.GetArgValue(args, "form");
            var billTypeId = Program.GetArgValue(args, "id");
            var keyword = Program.GetArgValue(args, "keyword");

            if (string.IsNullOrEmpty(formIdentifier) && string.IsNullOrEmpty(billTypeId) && string.IsNullOrEmpty(keyword))
            {
                JsonOutputWriter.WriteError("billtype", "缺少参数：--form <identifier>、--id <billTypeId> 或 --keyword <keyword> 至少指定一个");
                HelpCommand.ShowBillTypeHelp();
                return 1;
            }

            try
            {
                var connectionString = Program.ResolveConnectionString(options);
                var service = new MetadataQueryService(connectionString);
                var results = service.QueryBillTypes(formIdentifier, billTypeId, keyword);

                var output = new List<object>();
                foreach (var row in results)
                {
                    output.Add(new
                    {
                        billTypeId = row.GetValueOrDefault("FBILLTYPEID")?.ToString() ?? "",
                        billFormId = row.GetValueOrDefault("FBILLFORMID")?.ToString() ?? "",
                        number = row.GetValueOrDefault("FNUMBER")?.ToString() ?? "",
                        name = row.GetValueOrDefault("FNAME")?.ToString() ?? "",
                        description = row.GetValueOrDefault("FDESCRIPTION")?.ToString() ?? ""
                    });
                }

                JsonOutputWriter.WriteSuccess("billtype", output);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("billtype", ex.Message);
                return 1;
            }
        }
    }
}
