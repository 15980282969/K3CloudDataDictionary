using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Xml.Linq;

namespace K3CloudDataDictionary.Views
{
    public class SplitTableInfo
    {
        public string EntityKey { get; set; } = "";
        public string Suffix { get; set; } = "";
        public string Description { get; set; } = "";

        public SplitTableInfo Clone()
        {
            return new SplitTableInfo
            {
                EntityKey = EntityKey,
                Suffix = Suffix,
                Description = Description
            };
        }

        public override string ToString()
        {
            return $"EntityKey={EntityKey}, Suffix={Suffix}, Description={Description}";
        }
    }

    public static class ExtractSplits
    {
        public static List<SplitTableInfo> ExtractFromXml(string xmlContent)
        {
            var result = new List<SplitTableInfo>();
            if (string.IsNullOrEmpty(xmlContent))
            {
                return result;
            }

            XDocument doc = XDocument.Parse(xmlContent);

            var splitTableElements = doc.Descendants("SplitTable");
            foreach (var split in splitTableElements)
            {
                var parentEntity = split.Parent;
                var entityKey = parentEntity?.Element("Key")?.Value ?? "";

                var info = new SplitTableInfo
                {
                    EntityKey = entityKey,
                    Suffix = split.Element("Suffix")?.Value ?? "",
                    Description = split.Element("Description")?.Value ?? ""
                };
                result.Add(info);
            }

            return result;
        }

        public static List<SplitTableInfo> ExtractByFid(string connectionString, string fid)
        {
            string xmlContent = MetadataDbHelper.QueryFKernelXML(connectionString, fid);
            if (string.IsNullOrEmpty(xmlContent))
            {
                return new List<SplitTableInfo>();
            }

            return ExtractFromXml(xmlContent);
        }
    }
}
