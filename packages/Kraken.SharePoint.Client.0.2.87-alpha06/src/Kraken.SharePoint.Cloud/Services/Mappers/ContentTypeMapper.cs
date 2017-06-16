
namespace Kraken.SharePoint.Services.Mappers {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;

  using Kraken.SharePoint.Client.Legacy;

    public class ContentTypeMapper {

        public ContentType MapContentType(XElement xmlResponse) {
            // TODO can this be more efficient... not an expert yet at Linq to Xml
            List<ContentType> list = (from XElement element in xmlResponse.DescendantsAndSelf()
                                      where element.Name.LocalName == "ContentType"
                                      select MapInternal(element, true)).ToList();
            if (list.Count != 1)
                throw new Exception(string.Format("Expected to be passed an XElement with one and only one ContentType element. Xml: {0}", xmlResponse.Value));
            return list[0];
        }

        public List<ContentType> MapContentTypes(XElement xmlResponse) {
            List<ContentType> list = (from XElement element in xmlResponse.DescendantsAndSelf()
                    where element.Name.LocalName == "ContentType"
                    select MapInternal(element, false)).ToList();
            return list;
        }

        public static ContentType MapInternal(XElement element, bool deepMapping) {
            ContentType ct = new ContentType {
                Name = element.Attribute("Name").Value,
                ID = element.Attribute("ID").Value,
                Description = element.Attribute("Description").Value,
                Group = element.Attribute("Group").Value,
                NewDocumentControl = element.Attribute("NewDocumentControl").Value,
                Version = int.Parse(element.Attribute("Version").Value),
                RequireClientRenderingOnNew = bool.Parse(element.Attribute("RequireClientRenderingOnNew").Value)
            };
            if (deepMapping) {
                // TODO get the fieldName refs and all that stuff
            }
            // TODO any implementation of Parent Content Type
            return ct;
        }

    }

}

