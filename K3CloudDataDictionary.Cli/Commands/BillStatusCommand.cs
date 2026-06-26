using System;
using System.Collections.Generic;
using K3CloudDataDictionary.Cli.Services;
using K3CloudDataDictionary.Cli;

namespace K3CloudDataDictionary.Cli.Commands
{
    /// <summary>
    /// billstatus 命令：查询单据状态字段的枚举值（elementType=40）
    /// </summary>
    public static class BillStatusCommand
    {
        public static int Execute(string[] args, GlobalOptions options)
        {
            JsonOutputWriter.SetPrettyPrint(options.PrettyPrint);

            // 检查帮助
            if (args.Length == 0 || Program.HasOption(args, "help") || Program.HasOption(args, "h"))
            {
                HelpCommand.ShowBillStatusHelp();
                return 0;
            }

            // 获取必填参数
            var formIdentifier = Program.GetArgValue(args, "form");
            if (string.IsNullOrEmpty(formIdentifier))
            {
                JsonOutputWriter.WriteError("billstatus", "缺少必填参数 --form <identifier>");
                HelpCommand.ShowBillStatusHelp();
                return 1;
            }

            // 获取可选参数
            var fieldKey = Program.GetArgValue(args, "field");
            var keyword = Program.GetArgValue(args, "keyword");

            try
            {
                var connectionString = Program.ResolveConnectionString(options);
                var service = new MetadataQueryService(connectionString);
                var results = service.QueryBillStatusItems(formIdentifier, fieldKey, keyword);

                var output = new List<object>();
                foreach (var row in results)
                {
                    var statusItems = row.GetValueOrDefault("FSTATUSITEMS");
                    var fieldOutput = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["formId"] = row.GetValueOrDefault("FFORMID")?.ToString() ?? "",
                        ["formName"] = row.GetValueOrDefault("FDJMC")?.ToString() ?? "",
                        ["entityName"] = row.GetValueOrDefault("FENTITYNAME")?.ToString() ?? "",
                        ["table"] = row.GetValueOrDefault("FTABLENAME")?.ToString() ?? "",
                        ["fieldKey"] = row.GetValueOrDefault("FKey")?.ToString() ?? "",
                        ["fieldName"] = row.GetValueOrDefault("FName")?.ToString() ?? "",
                        ["dbFieldName"] = row.GetValueOrDefault("FFieldName")?.ToString() ?? "",
                        ["propertyName"] = row.GetValueOrDefault("FPropertyName")?.ToString() ?? "",
                        ["elementType"] = row.GetValueOrDefault("FELEMENTTYPE")?.ToString() ?? "",
                        ["elementTypeName"] = row.GetValueOrDefault("FELEMENTTYPENAME")?.ToString() ?? ""
                    };

                    // 将 statusItems 作为嵌套子对象
                    if (statusItems != null)
                    {
                        fieldOutput["statusItems"] = statusItems;
                    }

                    output.Add(fieldOutput);
                }

                JsonOutputWriter.WriteSuccess("billstatus", output);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("billstatus", ex.Message);
                return 1;
            }
        }
    }
}
