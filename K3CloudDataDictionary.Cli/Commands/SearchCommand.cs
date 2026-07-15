using System;
using System.Collections.Generic;
using K3CloudDataDictionary.Cli.Services;
using K3CloudDataDictionary.Cli;

namespace K3CloudDataDictionary.Cli.Commands
{
    /// <summary>
    /// search 命令：模糊搜索字段或表
    /// </summary>
    public static class SearchCommand
    {
        public static int Execute(string[] args, GlobalOptions options)
        {
            JsonOutputWriter.SetPrettyPrint(options.PrettyPrint);

            // 检查帮助
            if (args.Length == 0 || Program.HasOption(args, "help") || Program.HasOption(args, "h"))
            {
                HelpCommand.ShowSearchHelp();
                return 0;
            }

            // 获取必填参数
            var keyword = Program.GetArgValue(args, "keyword");
            if (string.IsNullOrEmpty(keyword))
            {
                JsonOutputWriter.WriteError("search", "缺少必填参数 --keyword <keyword>");
                HelpCommand.ShowSearchHelp();
                return 1;
            }

            // 获取搜索类型（默认 table，避免全量字段搜索导致慢查询）
            var searchType = Program.GetArgValue(args, "type")?.ToLowerInvariant() ?? "table";

            // 是否精确匹配
            var exact = Program.HasOption(args, "exact") || Program.HasOption(args, "e");

            try
            {
                var connectionString = Program.ResolveConnectionString(options);
                var service = new MetadataQueryService(connectionString);

                if (searchType == "table")
                {
                    // 搜索表
                    var results = service.SearchTables(keyword, exact);
                    var output = new List<object>();
                    foreach (var row in results)
                    {
                        output.Add(new
                        {
                            formId = row.GetValueOrDefault("FFORMID")?.ToString() ?? "",
                            formIdentifier = row.GetValueOrDefault("FFORMIDENTIFIER")?.ToString() ?? "",
                            formName = row.GetValueOrDefault("FDJMC")?.ToString() ?? "",
                            entityKey = row.GetValueOrDefault("FKey")?.ToString() ?? "",
                            entityName = row.GetValueOrDefault("FENTITYNAME")?.ToString() ?? "",
                            table = row.GetValueOrDefault("FTABLENAME")?.ToString() ?? "",
                            elementType = row.GetValueOrDefault("FELEMENTTYPENAME")?.ToString() ?? "",
                            fieldCount = Convert.ToInt32(row.GetValueOrDefault("FFIELDCOUNT") ?? 0)
                        });
                    }
                    JsonOutputWriter.WriteSuccess("search", output);
                }
                else
                {
                    // 搜索字段
                    var results = service.SearchFields(keyword, exact);
                    var output = new List<object>();
                    foreach (var row in results)
                    {
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
                            ["enumType"] = row.GetValueOrDefault("FEnumType")?.ToString() ?? ""
                        };

                        // elementType=40 时，将 StatusItems 作为嵌套子对象
                        if (statusItems != null)
                        {
                            fieldOutput["statusItems"] = statusItems;
                        }

                        output.Add(fieldOutput);
                    }
                    JsonOutputWriter.WriteSuccess("search", output);
                }

                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("search", ex.Message);
                return 1;
            }
        }
    }
}
