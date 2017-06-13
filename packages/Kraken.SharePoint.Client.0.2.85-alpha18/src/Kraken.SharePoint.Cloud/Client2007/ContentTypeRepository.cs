
namespace Kraken.SharePoint.Client.Legacy {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

  using Kraken.SharePoint.Services.Gateways;

    public class ContentTypeRepository {

      public List<ContentType> GetContentTypes(Uri webUrl) {
            //WebsServiceGateway gw = new WebsServiceGateway();
            List<ContentType> ctCollection = WebsServiceGateway.GetContentTypes(webUrl);
            return ctCollection;
        }
        public ContentType GetContentType(Uri webUrl, string contentTypeId) {
            ContentType ct = WebsServiceGateway.GetContentType(webUrl, contentTypeId);
            return ct;
        }
        public ContentType GetContentTypeByName(Uri webUrl, string contentTypeName) {
            List<ContentType> ctCollection = WebsServiceGateway.GetContentTypes(webUrl);
            foreach (ContentType ct in ctCollection) {
                if (contentTypeName.Equals(ct.Name, StringComparison.InvariantCultureIgnoreCase))
                    return ct;
            }
            throw new Exception(string.Format("Content type name '{0}' not found in web '{1}'.", contentTypeName, webUrl));
        }

    }

}
