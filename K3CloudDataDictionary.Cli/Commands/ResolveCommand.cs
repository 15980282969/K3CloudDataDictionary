using System;
using System.Collections.Generic;
using K3CloudDataDictionary.Cli.Services;
using K3CloudDataDictionary.Cli;

namespace K3CloudDataDictionary.Cli.Commands
{
    /// <summary>
    /// resolve 命令：根据对象 ID 解析对应的表单信息（用于 lookUpObject 反查）
    /// </summary>
    public static class ResolveCommand
    {
        public static int Execute(string[] args, GlobalOptions options)
        {
            JsonOutputWriter.SetPrettyPrint(options.PrettyPrint);

            // 检查帮助
            if (args.Length == 0 || Program.HasOption(args, "help") || Program.HasOption(args, "h"))
            {
                HelpCommand.ShowResolveHelp();
                return 0;
            }

            // 获取必填参数
            var objectId = Program.GetArgValue(args, "id");
            if (string.IsNullOrEmpty(objectId))
            {
                JsonOutputWriter.WriteError("resolve", "缺少必填参数 --id <objectId>");
                HelpCommand.ShowResolveHelp();
                return 1;
            }

            try
            {
                var connectionString = Program.ResolveConnectionString(options);
                var service = new MetadataQueryService(connectionString);
                var results = service.ResolveObject(objectId);

                var output = new List<object>();
                foreach (var row in results)
                {
                    output.Add(new
                    {
                        lookupId = row.GetValueOrDefault("FID")?.ToString() ?? "",
                        formId = row.GetValueOrDefault("FFORMID")?.ToString() ?? "",
                        tableName = row.GetValueOrDefault("FTABLENAME")?.ToString() ?? "",
                        pkFieldName = row.GetValueOrDefault("FPKFIELDNAME")?.ToString() ?? "",
                        orgFieldName = row.GetValueOrDefault("FORGFIELDNAME")?.ToString() ?? ""
                    });
                }

                JsonOutputWriter.WriteSuccess("resolve", output);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("resolve", ex.Message);
                return 1;
            }
        }
    }
}
