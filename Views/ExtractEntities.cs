using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
                Action = Action
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

        public static (List<EntityInfo> WithOid, List<EntityInfo> WithoutOid) ExtractByFid(string connectionString, string fid)
        {
            string xmlContent = MetadataDbHelper.QueryFKernelXML(connectionString, fid);
            if (string.IsNullOrEmpty(xmlContent))
            {
                return (new List<EntityInfo>(), new List<EntityInfo>());
            }

            return ExtractFromXml(xmlContent);
        }

        private static (List<EntityInfo> WithOid, List<EntityInfo> WithoutOid) ParseEntities(XDocument doc)
        {
            var withOid = new List<EntityInfo>();
            var withoutOid = new List<EntityInfo>();

            var entityElements = doc.Descendants()
                .Where(e => e.Name.LocalName.EndsWith("Entity") && e.Name.LocalName != "LinkEntity" && e.Name.LocalName != "EntityServiceRule" && e.Parent?.Name.LocalName != "LinkEntitys");

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
