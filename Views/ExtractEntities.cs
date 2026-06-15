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
