using System;
using System.Collections.Generic;
using System.Linq;
using K3CloudDataDictionary.Cli.Services;
using K3CloudDataDictionary.Cli;

namespace K3CloudDataDictionary.Cli.Commands
{
    /// <summary>
    /// fields 命令：查询表单字段信息
    /// </summary>
    public static class FieldsCommand
    {
        public static int Execute(string[] args, GlobalOptions options)
        {
            JsonOutputWriter.SetPrettyPrint(options.PrettyPrint);

            // 检查帮助
            if (args.Length == 0 || Program.HasOption(args, "help") || Program.HasOption(args, "h"))
            {
                HelpCommand.ShowFieldsHelp();
                return 0;
            }

            // 获取必填参数
            var formIdentifier = Program.GetArgValue(args, "form");
            if (string.IsNullOrEmpty(formIdentifier))
            {
                JsonOutputWriter.WriteError("fields", "缺少必填参数 --form <identifier>");
                HelpCommand.ShowFieldsHelp();
                return 1;
            }

            // 获取可选参数
            var entityKey = Program.GetArgValue(args, "entity");
            var keyword = Program.GetArgValue(args, "keyword");
            var exact = Program.HasOption(args, "exact") || Program.HasOption(args, "e");

            try
            {
                var connectionString = Program.ResolveConnectionString(options);
                var service = new MetadataQueryService(connectionString);
                var results = service.QueryFields(formIdentifier, entityKey, keyword, exact);

                // 转换为更友好的格式
                var output = new List<object>();
                foreach (var row in results)
                {
                    // 跳过提示行（在循环后单独处理）
                    if (row.ContainsKey("_hint")) continue;

                    var statusItems = row.GetValueOrDefault("FSTATUSITEMS");
                    var fieldOutput = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["formName"] = row.GetValueOrDefault("FDJMC")?.ToString() ?? "",
                        ["entityName"] = row.GetValueOrDefault("FENTITYNAME")?.ToString() ?? "",
                        ["entityKey"] = row.GetValueOrDefault("FENTITYKEY")?.ToString() ?? "",
                        ["ormEntityName"] = row.GetValueOrDefault("FENTRYNAME")?.ToString() ?? "",
                        ["seqFieldKey"] = row.GetValueOrDefault("FSEQFIELDKEY")?.ToString() ?? "",
                        ["entryPkFieldName"] = row.GetValueOrDefault("FENTRY_PK_FIELD_NAME")?.ToString() ?? "",
                        ["table"] = row.GetValueOrDefault("FTABLENAME")?.ToString() ?? "",
                        ["splitSuffix"] = row.GetValueOrDefault("FSUFFIX")?.ToString() ?? "",
                        ["splitTable"] = row.GetValueOrDefault("FSPLITTABlE")?.ToString() ?? "",
                        ["key"] = row.GetValueOrDefault("FKey")?.ToString() ?? "",
                        ["name"] = row.GetValueOrDefault("FName")?.ToString() ?? "",
                        ["fieldName"] = row.GetValueOrDefault("FFieldName")?.ToString() ?? "",
                        ["propertyName"] = row.GetValueOrDefault("FPropertyName")?.ToString() ?? "",
                        ["elementType"] = row.GetValueOrDefault("FELEMENTTYPENAME")?.ToString() ?? "",
                        ["elementTypeName"] = row.GetValueOrDefault("FELEMENTTYPECNNAME")?.ToString() ?? "",
                        ["tagName"] = row.GetValueOrDefault("FTagName")?.ToString() ?? "",
                        ["lookUpObject"] = row.GetValueOrDefault("FLookUpObjectID")?.ToString() ?? "",
                        ["enumType"] = row.GetValueOrDefault("FEnumType")?.ToString() ?? "",
                        ["splitDescription"] = row.GetValueOrDefault("FSPLITDESCRIPTION")?.ToString() ?? "",
                        ["updateActionCount"] = Convert.ToInt32(row.GetValueOrDefault("FUPDATEACTIONCOUNT") ?? 0)
                    };

                    // elementType=40 时，将 StatusItems 作为嵌套子对象
                    if (statusItems != null)
                    {
                        fieldOutput["statusItems"] = statusItems;
                    }

                    output.Add(fieldOutput);
                }

                // 检查实体未找到提示
                var hintRow = results.FirstOrDefault(r => r.ContainsKey("_hint") && r["_hint"]?.ToString() == "entity_not_found");
                if (hintRow != null && output.Count == 0)
                {
                    var hintOutput = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["hint"] = "entity_not_found",
                        ["message"] = hintRow.GetValueOrDefault("message")?.ToString() ?? "",
                        ["availableEntities"] = hintRow.GetValueOrDefault("availableEntities")
                    };
                    JsonOutputWriter.WriteSuccess("fields", new List<object> { hintOutput });
                    return 0;
                }

                JsonOutputWriter.WriteSuccess("fields", output);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("fields", ex.Message);
                return 1;
            }
        }
    }
}
