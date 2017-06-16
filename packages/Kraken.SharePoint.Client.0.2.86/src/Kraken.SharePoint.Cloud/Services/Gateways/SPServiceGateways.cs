
namespace Kraken.SharePoint.Services.Gateways {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

  using Kraken.SharePoint.Client.Legacy;
  using Kraken.SharePoint.Services;
  using Kraken.SharePoint.Services.Mappers;
    using Kraken.Xml.Linq;

    /// <summary>
    /// Implements the service interactions that will be used by the Client OM
    /// </summary>
    public class ListsServiceGateway {

        public static List<List> GetLists(Uri webUrl) {
            var client = SPServicesClientFactory.CreateListsClient(webUrl);
            // something must have changed to caus eit to start returning XElement instead of XmlNode
            //string result = client.GetListCollection().GetXmlNode().OuterXml;
            //XElement xmlResponse = XElement.Parse(result);
            XElement xmlResponse = client.GetListCollection();
            ListMapper mapper = new ListMapper();
            List<List> lists = mapper.MapLists(xmlResponse);
            return lists;
        }

    } // ListsServiceGateway

    /// <summary>
    /// Implements the service interactions that will be used by the Client OM
    /// </summary>
    public class WebsServiceGateway {

        public static ContentType GetContentType(Uri webUrl, string contentTypeId) {
            var client = SPServicesClientFactory.CreateWebsClient(webUrl);
            // something must have changed to caus eit to start returning XElement instead of XmlNode
            //string result = client.GetContentType(contentTypeId).GetXmlNode().OuterXml;
            //XElement xmlResponse = XElement.Parse(result);
            XElement xmlResponse = client.GetContentType(contentTypeId);
            ContentTypeMapper mapper = new ContentTypeMapper();
            ContentType cType = mapper.MapContentType(xmlResponse);
            return cType;
        }

        public static List<ContentType> GetContentTypes(Uri webUrl) {
            var client = SPServicesClientFactory.CreateWebsClient(webUrl);
            // something must have changed to caus eit to start returning XElement instead of XmlNode
            //string result = client.GetContentTypes().GetXmlNode().OuterXml;
            //XElement xmlResponse = XElement.Parse(result);
            XElement xmlResponse = client.GetContentTypes();
            // Linq is awesome, but in this case we had lots of code using "old school" xml namespaces
            ContentTypeMapper mapper = new ContentTypeMapper();
            List<ContentType> cTypes = mapper.MapContentTypes(xmlResponse);
            return cTypes;
        }

    } // class WebsServiceGateway

} // namespace
