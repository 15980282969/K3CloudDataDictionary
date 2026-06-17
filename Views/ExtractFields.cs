using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace K3CloudDataDictionary.Views
{
    public class MetadataFieldInfo
    {
        public string Oid { get; set; } = "";
        public string ElementType { get; set; } = "";
        public string Id { get; set; } = "";
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string FieldName { get; set; } = "";
        public string PropertyName { get; set; } = "";
        public string EntityKey { get; set; } = "";
        public string Suffix { get; set; } = "";
        public string TagName { get; set; } = "";
        public string LookUpObjectID { get; set; } = "";
        public string EnumType { get; set; } = "";
        public string Action { get; set; } = "";
        public List<FieldUpdateActionInfo> UpdateActions { get; set; } = new List<FieldUpdateActionInfo>();

        public override string ToString()
        {
            return $"[{TagName}] Oid={Oid}, ElementType={ElementType}, Id={Id}, Key={Key}, Name={Name}, FieldName={FieldName}, PropertyName={PropertyName}, EntityKey={EntityKey}, Suffix={Suffix}, LookUpObjectID={LookUpObjectID}, EnumType={EnumType}";
        }

        public MetadataFieldInfo Clone()
        {
            return new MetadataFieldInfo
            {
                Oid = Oid,
                ElementType = ElementType,
                Id = Id,
                Key = Key,
                Name = Name,
                FieldName = FieldName,
                PropertyName = PropertyName,
                EntityKey = EntityKey,
                Suffix = Suffix,
                TagName = TagName,
                LookUpObjectID = LookUpObjectID,
                EnumType = EnumType,
                Action = Action,
                UpdateActions = new List<FieldUpdateActionInfo>(UpdateActions.ConvertAll(a => a.Clone()))
            };
        }
    }

    public static class ExtractFields
    {
        public static (List<MetadataFieldInfo> WithOid, List<MetadataFieldInfo> WithoutOid) ExtractFromXml(string xmlContent)
        {
            if (string.IsNullOrEmpty(xmlContent))
            {
                return (new List<MetadataFieldInfo>(), new List<MetadataFieldInfo>());
            }

            XDocument doc = XDocument.Parse(xmlContent);
            return ParseFields(doc);
        }

        private static (List<MetadataFieldInfo> WithOid, List<MetadataFieldInfo> WithoutOid) ParseFields(XDocument doc)
        {
            var withOid = new List<MetadataFieldInfo>();
            var withoutOid = new List<MetadataFieldInfo>();

            var fieldElements = doc.Descendants()
                .Where(e => e.Name.LocalName.EndsWith("Field")
                    && e.Attribute("ElementType") != null
                    && e.Attribute("ElementType")?.Value != "0"
                    && e.Parent?.Parent != null && !e.Parent.Parent.Name.LocalName.EndsWith("Field"));

            foreach (var element in fieldElements)
            {
                var info = new MetadataFieldInfo
                {
                    TagName = element.Name.LocalName,
                    Oid = element.Attribute("oid")?.Value ?? "",
                    ElementType = element.Attribute("ElementType")?.Value ?? "",
                    Action = element.Attribute("action")?.Value ?? "",
                    Id = element.Element("Id")?.Value ?? "",
                    Key = element.Element("Key")?.Value ?? "",
                    Name = element.Element("Name")?.Value ?? "",
                    FieldName = element.Element("FieldName")?.Value ?? "",
                    PropertyName = element.Element("PropertyName")?.Value ?? "",
                    EntityKey = element.Element("EntityKey")?.Value ?? "",
                    Suffix = element.Element("Suffix")?.Value ?? "",
                    LookUpObjectID = element.Element("LookUpObjectID")?.Value ?? "",
                    EnumType = element.Element("EnumType")?.Value ?? ""
                };

                // 提取 UpdateActions（值更新事件）
                var updateActionsElement = element.Element("UpdateActions");
                if (updateActionsElement != null)
                {
                    foreach (var svcElement in updateActionsElement.Elements())
                    {
                        var action = new FieldUpdateActionInfo
                        {
                            ServiceTypeName = svcElement.Name.LocalName,
                            Oid = svcElement.Attribute("oid")?.Value ?? "",
                            Action = svcElement.Attribute("action")?.Value ?? "",
                            Id = svcElement.Element("Id")?.Value ?? "",
                            ActionId = svcElement.Element("ActionId")?.Value ?? "",
                            Description = svcElement.Element("Description")?.Value ?? "",
                            Parameters = svcElement.Element("Parameters")?.Value ?? "",
                            Seq = svcElement.Element("Seq")?.Value ?? "",
                            IsForbidden = svcElement.Element("IsForbidden")?.Value ?? "",
                            PreCondition = svcElement.Element("PreCondition")?.Value ?? "",
                            PreConditionDesc = svcElement.Element("PreConditionDesc")?.Value ?? ""
                        };
                        info.UpdateActions.Add(action);
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
    }
}
