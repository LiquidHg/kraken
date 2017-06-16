
namespace Kraken.SharePoint.Services.Mappers {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;

  using Kraken.SharePoint.Client.Legacy;

    public class ListMapper {

        public List<List> MapLists(XElement xmlResponse) {
            List<List> list = (from XElement element in xmlResponse.DescendantsAndSelf()
                    where element.Name.LocalName == "List"
                    select MapInternal(element)).ToList();
            return list;
        }

        public static List MapInternal(XElement element) {
            List list = new List {
                Title = element.Attribute("Title").Value,
                ID = new Guid(element.Attribute("Id").Value),
                Url = element.Attribute("Url").Value
            };
            //list.parentWebUrl = element.Attribute("ParentWeb").Value;
            return list;
        }

    }

}
