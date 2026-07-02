using System;
using System.Collections.Generic;
using K3CloudDataDictionary.Cli.Services;
using K3CloudDataDictionary.Cli;

namespace K3CloudDataDictionary.Cli.Commands
{
    /// <summary>
    /// form 命令：查询表单元数据
    /// </summary>
    public static class FormCommand
    {
        public static int Execute(string[] args, GlobalOptions options)
        {
            JsonOutputWriter.SetPrettyPrint(options.PrettyPrint);

            // 检查帮助
            if (args.Length == 0 || Program.HasOption(args, "help") || Program.HasOption(args, "h"))
            {
                HelpCommand.ShowFormHelp();
                return 0;
            }

            // 获取必填参数
            var formIdentifier = Program.GetArgValue(args, "id");
            if (string.IsNullOrEmpty(formIdentifier))
            {
                JsonOutputWriter.WriteError("form", "缺少必填参数 --id <identifier>");
                HelpCommand.ShowFormHelp();
                return 1;
            }

            try
            {
                var connectionString = Program.ResolveConnectionString(options);
                var service = new MetadataQueryService(connectionString);

                // 查询表单信息
                var formResults = service.QueryForm(formIdentifier);
                if (formResults.Count == 0)
                {
                    JsonOutputWriter.WriteError("form", $"未找到表单: {formIdentifier}");
                    return 1;
                }

                // 查询实体列表
                var entityResults = service.QueryEntities(formIdentifier);

                // 构建输出
                var formRow = formResults[0];
                var entities = new List<object>();
                foreach (var entity in entityResults)
                {
                    entities.Add(new
                    {
                        entityKey = entity.GetValueOrDefault("FKey")?.ToString() ?? "",
                        entityName = entity.GetValueOrDefault("FENTITYNAME")?.ToString() ?? "",
                        table = entity.GetValueOrDefault("FTABLENAME")?.ToString() ?? "",
                        entryName = entity.GetValueOrDefault("FEntryName")?.ToString() ?? "",
                        elementType = entity.GetValueOrDefault("FELEMENTTYPENAME")?.ToString() ?? "",
                        seqFieldKey = entity.GetValueOrDefault("FSEQFIELDKEY")?.ToString() ?? "",
                        entryPkFieldName = entity.GetValueOrDefault("FENTRY_PK_FIELD_NAME")?.ToString() ?? "",
                        serviceRuleCount = Convert.ToInt32(entity.GetValueOrDefault("FSERVICERULECOUNT") ?? 0),
                        updateActionCount = Convert.ToInt32(entity.GetValueOrDefault("FUPDATEACTIONCOUNT") ?? 0)
                    });
                }

                var output = new
                {
                    formId = formRow.GetValueOrDefault("FFORMID")?.ToString() ?? "",
                    formIdentifier = formRow.GetValueOrDefault("FFORMIDENTIFIER")?.ToString() ?? "",
                    formName = formRow.GetValueOrDefault("FDJMC")?.ToString() ?? "",
                    modelType = formRow.GetValueOrDefault("FELEMENTTYPENAME")?.ToString() ?? "",
                    subsystem = formRow.GetValueOrDefault("FSUBSYSTEMNAME")?.ToString() ?? "",
                    formPluginCount = Convert.ToInt32(formRow.GetValueOrDefault("FFORMPLUGINCOUNT") ?? 0),
                    listPluginCount = Convert.ToInt32(formRow.GetValueOrDefault("FLISTPLUGINCOUNT") ?? 0),
                    builderPluginCount = Convert.ToInt32(formRow.GetValueOrDefault("FBUILDERPLUGINCOUNT") ?? 0),
                    updateActionCount = Convert.ToInt32(formRow.GetValueOrDefault("FUPDATEACTIONCOUNT") ?? 0),
                    serviceRuleCount = Convert.ToInt32(formRow.GetValueOrDefault("FSERVICERULECOUNT") ?? 0),
                    formOperationCount = Convert.ToInt32(formRow.GetValueOrDefault("FFORMOPERATIONCOUNT") ?? 0),
                    entities = entities
                };

                JsonOutputWriter.WriteSuccess("form", output);
                return 0;
            }
            catch (Exception ex)
            {
                JsonOutputWriter.WriteError("form", ex.Message);
                return 1;
            }
        }
    }
}
