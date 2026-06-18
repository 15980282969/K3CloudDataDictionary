using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace K3CloudDataDictionary.Views
{
    public class EntityInfo
    {
        public string Oid { get; set; } = "";
        public string ElementType { get; set; } = "";
        public string EntryName { get; set; } = "";
        public string TableName { get; set; } = "";
        public string Name { get; set; } = "";
        public string EntryPkFieldName { get; set; } = "";
        public string Id { get; set; } = "";
        public string Key { get; set; } = "";
        public string KeyField { get; set; } = "";
        public string TagName { get; set; } = "";
        public string Action { get; set; } = "";
        public List<EntityServiceRuleInfo> ServiceRules { get; set; } = new List<EntityServiceRuleInfo>();

        public override string ToString()
        {
            return $"[{TagName}] Oid={Oid}, ElementType={ElementType}, EntryName={EntryName}, TableName={TableName}, Name={Name}, EntryPkFieldName={EntryPkFieldName}, Id={Id}, Key={Key}, KeyField={KeyField}";
        }

        public EntityInfo Clone()
        {
            return new EntityInfo
            {
                Oid = Oid,
                ElementType = ElementType,
                EntryName = EntryName,
                TableName = TableName,
                Name = Name,
                EntryPkFieldName = EntryPkFieldName,
                Id = Id,
                Key = Key,
                KeyField = KeyField,
                TagName = TagName,
                Action = Action,
                ServiceRules = ServiceRules.Select(r => r.Clone()).ToList()
            };
        }
    }

    public static class ExtractEntities
    {
        public static (List<EntityInfo> WithOid, List<EntityInfo> WithoutOid) ExtractFromXml(string xmlContent)
        {
            if (string.IsNullOrEmpty(xmlContent))
            {
                return (new List<EntityInfo>(), new List<EntityInfo>());
            }

            XDocument doc = XDocument.Parse(xmlContent);
            return ParseEntities(doc);
        }

        private static (List<EntityInfo> WithOid, List<EntityInfo> WithoutOid) ParseEntities(XDocument doc)
        {
            var withOid = new List<EntityInfo>();
            var withoutOid = new List<EntityInfo>();

            var entityElements = doc.Descendants()
                .Where(e => e.Name.LocalName.EndsWith("Entity") && e.Name.LocalName != "LinkEntity" && e.Parent?.Name.LocalName != "LinkEntitys" && e.Parent?.Name.LocalName != "EntityServiceRules");

            foreach (var element in entityElements)
            {
                var info = new EntityInfo
                {
                    TagName = element.Name.LocalName,
                    Oid = element.Attribute("oid")?.Value ?? "",
                    ElementType = element.Attribute("ElementType")?.Value ?? "",
                    Action = element.Attribute("action")?.Value ?? "",
                    EntryName = element.Element("EntryName")?.Value ?? "",
                    TableName = element.Element("TableName")?.Value ?? "",
                    Name = element.Element("Name")?.Value ?? "",
                    EntryPkFieldName = element.Element("EntryPkFieldName")?.Value ?? "",
                    Id = element.Element("Id")?.Value ?? "",
                    Key = element.Element("Key")?.Value ?? "",
                    KeyField = element.Element("KeyField")?.Value ?? ""
                };

                // 提取该 Entity 下的 EntityServiceRules
                var serviceRulesElement = element.Element("EntityServiceRules");
                if (serviceRulesElement != null)
                {
                    foreach (var ruleElement in serviceRulesElement.Elements("EntityServiceRule"))
                    {
                        var rule = new EntityServiceRuleInfo
                        {
                            Oid = ruleElement.Attribute("oid")?.Value ?? "",
                            Action = ruleElement.Attribute("action")?.Value ?? "",
                            Id = ruleElement.Element("Id")?.Value ?? "",
                            Description = ruleElement.Element("Description")?.Value ?? "",
                            IsEnabled = ruleElement.Element("IsEnabled")?.Value ?? "",
                            PreCondition = ruleElement.Element("PreCondition")?.Value ?? "",
                            PreConditionDesc = ruleElement.Element("PreConditionDesc")?.Value ?? "",
                            Seq = ruleElement.Element("Seq")?.Value ?? "",
                            EntityKey = info.Key
                        };

                        // 提取 WhenTrueBusinessServices（包含所有类型的服务元素）
                        var whenTrue = ruleElement.Element("WhenTrueBusinessServices");
                        if (whenTrue != null)
                        {
                            foreach (var svc in whenTrue.Elements())
                            {
                                rule.WhenTrueServices.Add(ParseBusinessService(svc, rule.Id, "WhenTrue"));
                            }
                        }

                        // 提取 WhenFalseBusinessServices（包含所有类型的服务元素）
                        var whenFalse = ruleElement.Element("WhenFalseBusinessServices");
                        if (whenFalse != null)
                        {
                            foreach (var svc in whenFalse.Elements())
                            {
                                rule.WhenFalseServices.Add(ParseBusinessService(svc, rule.Id, "WhenFalse"));
                            }
                        }

                        info.ServiceRules.Add(rule);
                    }
                }

                if (!string.IsNullOrEmpty(info.Oid))
                {
                    withOid.Add(info);
                }
                else
                {
                    withoutOid.Add(info);
                }
            }

            return (withOid, withoutOid);
        }

        /// <summary>
        /// 从XML中提取三类插件：FormPlugins、ListPlugins、WebFormBuilderPlugins
        /// </summary>
        public static List<PluginInfo> ExtractPlugins(string xmlContent)
        {
            var result = new List<PluginInfo>();
            if (string.IsNullOrEmpty(xmlContent)) return result;

            XDocument doc = XDocument.Parse(xmlContent);

            string[] pluginContainers = { "FormPlugins", "ListPlugins", "WebFormBuilderPlugins" };

            foreach (var containerName in pluginContainers)
            {
                var containers = doc.Descendants(containerName);
                foreach (var container in containers)
                {
                    foreach (var pluginElement in container.Elements("PlugIn"))
                    {
                        var plugin = new PluginInfo
                        {
                            Oid = pluginElement.Attribute("oid")?.Value ?? "",
                            Action = pluginElement.Attribute("action")?.Value ?? "",
                            ClassName = pluginElement.Element("ClassName")?.Value ?? "",
                            OrderId = pluginElement.Element("OrderId")?.Value ?? "",
                            PluginType = containerName,
                            ElementType = pluginElement.Attribute("ElementType")?.Value ?? "",
                            ElementStyle = pluginElement.Attribute("ElementStyle")?.Value ?? "",
                            IsEnabled = pluginElement.Element("IsEnabled")?.Value ?? ""
                        };
                        result.Add(plugin);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 从XML中提取FormOperations信息
        /// </summary>
        public static List<FormOperationInfo> ExtractFormOperations(string xmlContent)
        {
            var result = new List<FormOperationInfo>();
            if (string.IsNullOrEmpty(xmlContent)) return result;

            XDocument doc = XDocument.Parse(xmlContent);

            var formOpsContainer = doc.Descendants("FormOperations").FirstOrDefault();
            if (formOpsContainer == null) return result;

            foreach (var opElement in formOpsContainer.Elements("FormOperation"))
            {
                var op = new FormOperationInfo
                {
                    Oid = opElement.Attribute("oid")?.Value ?? "",
                    Action = opElement.Attribute("action")?.Value ?? "",
                    Id = opElement.Element("Id")?.Value ?? "",
                    Operation = opElement.Element("Operation")?.Value ?? "",
                    OperationName = opElement.Element("OperationName")?.Value ?? ""
                };

                // 提取 Validations
                var validationsElement = opElement.Element("Validations");
                if (validationsElement != null)
                {
                    foreach (var valElement in validationsElement.Elements())
                    {
                        // 跳过 action=remove 的空验证标签
                        var valAction = valElement.Attribute("action")?.Value ?? "";
                        var val = new ValidationInfo
                        {
                            Oid = valElement.Attribute("oid")?.Value ?? "",
                            Action = valAction,
                            Id = valElement.Element("Id")?.Value ?? "",
                            ValidationType = valElement.Attribute("ValidationType")?.Value ?? "",
                            ValidationTypeName = valElement.Name.LocalName,
                            ErrorMessage = valElement.Element("ErrorMessage")?.Value ?? "",
                            Description = valElement.Element("Description")?.Value ?? "",
                            IsUsed = valElement.Element("IsUsed")?.Value ?? ""
                        };
                        op.Validations.Add(val);
                    }
                }

                // 提取 ServicePlugins
                var servicePluginsElement = opElement.Element("ServicePlugins");
                if (servicePluginsElement != null)
                {
                    foreach (var pluginElement in servicePluginsElement.Elements("PlugIn"))
                    {
                        var plugin = new FormOperationPluginInfo
                        {
                            Oid = pluginElement.Attribute("oid")?.Value ?? "",
                            Action = pluginElement.Attribute("action")?.Value ?? "",
                            ClassName = pluginElement.Element("ClassName")?.Value ?? "",
                            OrderId = pluginElement.Element("OrderId")?.Value ?? "",
                            ElementType = pluginElement.Attribute("ElementType")?.Value ?? "",
                            ElementStyle = pluginElement.Attribute("ElementStyle")?.Value ?? "",
                            IsEnabled = pluginElement.Element("IsEnabled")?.Value ?? ""
                        };
                        op.ServicePlugins.Add(plugin);
                    }
                }

                // 提取 AppBusinessService
                var appBizSvcElement = opElement.Element("AppBusinessService");
                if (appBizSvcElement != null)
                {
                    // 检查是否 action="setnull"
                    var appBizAction = appBizSvcElement.Attribute("action")?.Value ?? "";
                    if (appBizAction != "setnull")
                    {
                        foreach (var svcElement in appBizSvcElement.Elements())
                        {
                            var svc = new FormOperationAppServiceInfo
                            {
                                Oid = svcElement.Attribute("oid")?.Value ?? "",
                                Action = svcElement.Attribute("action")?.Value ?? "",
                                Id = svcElement.Element("Id")?.Value ?? "",
                                ServiceTypeName = svcElement.Name.LocalName,
                                Description = svcElement.Element("Description")?.Value ?? "",
                                IsForbidden = svcElement.Element("IsForbidden")?.Value ?? ""
                            };
                            op.AppBusinessServices.Add(svc);
                        }
                    }
                }

                result.Add(op);
            }

            return result;
        }

        private static FormBusinessServiceInfo ParseBusinessService(XElement svcElement, string parentRuleId, string serviceType)
        {
            return new FormBusinessServiceInfo
            {
                Oid = svcElement.Attribute("oid")?.Value ?? "",
                Action = svcElement.Attribute("action")?.Value ?? "",
                Id = svcElement.Element("Id")?.Value ?? "",
                ActionId = svcElement.Element("ActionId")?.Value ?? "",
                Description = svcElement.Element("Description")?.Value ?? "",
                Parameters = svcElement.Element("Parameters")?.Value ?? "",
                ParentRuleId = parentRuleId,
                ServiceType = serviceType,
                ServiceTypeName = svcElement.Name.LocalName
            };
        }
    }
}
