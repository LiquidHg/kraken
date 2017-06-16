using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !DOTNET_V35
using System.Threading.Tasks;
#endif
using System.Xml.Serialization;
using Microsoft.SharePoint.Client;

namespace Kraken.SharePoint.Client.Helpers
{
    public class MetadataInfo
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Type { get; set; }

        public string Value { get; set; }

        public static MetadataInfo Create(ListItem item, KeyValuePair<string, string> kvp)
        {
            var ret = new MetadataInfo();

            try
            {
                ret.Value = Convert.ToString(item[kvp.Key]);
                ret.Name = kvp.Key;
                ret.Type = kvp.Value; //field.TypeAsString;
            }
            catch (Exception ex)
            {

            }

            return ret;
        }
    }

    public class MetadataInfoList
    {
        [XmlElement(typeof(MetadataInfo), ElementName = "Field")]
        public List<MetadataInfo> List { get; set; }

        public MetadataInfoList()
        {
            List = new List<MetadataInfo>();
        }

        public static MetadataInfoList Create(ListItem item, Dictionary<string, string> fieldInfoList)
        {
            var ret = new MetadataInfoList();

            //var list = item.ParentList;

            //FieldCollection fields = list.Fields;
            //ctx.Load(fields);
            //ctx.ExecuteQuery();

            //ctx.Load(item);
            //ctx.ExecuteQuery();

            foreach (var kvp in fieldInfoList)
            {
                ret.List.Add(MetadataInfo.Create(item, kvp));
            }

            return ret;
        }
    }
}
