
namespace Kraken.SharePoint.Services {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceModel;
    using System.Text;

    using System.ServiceModel.Channels;

    using Kraken.SharePoint.Cloud.Webs;
    using Kraken.SharePoint.Cloud.Lists;

    /// <summary>
    /// Based on http://blogs.msdn.com/johnwpowell/archive/2009/01/03/consume-sharepoint-web-services-with-wcf-using-the-repository-gateway-mapper-domain-model-and-factory-design-patterns.aspx
    /// </summary>
    public class SPServicesClientFactory {

        // TODO refactor this... again
        #region Create instances of the various web services

        /// <summary>
        /// Creates a service client configured to call the SharePoint 2007 Webs service.
        /// </summary>
        /// <param name="webUrl">Url of the desired SPWeb object.</param>
        /// <returns>A service client configured to call Webs.asmx</returns>
        internal static WebsSoapClient CreateWebsClient(Uri webUrl) {
          WebsSoapClient client = CreateSoapClient(webUrl, SharePointService.Webs) as WebsSoapClient;
          return client;
        }

        internal static ListsSoapClient CreateListsClient(Uri webUrl) {
          ListsSoapClient client = CreateSoapClient(webUrl, SharePointService.Lists) as ListsSoapClient;
          return client;
        }

        #endregion

        private static EndpointAddress GetEndPointAddress(Uri webUrl, SharePointService service) {
            // generate the correct url for the desired web
            Uri sharePointServiceUrl = SPServiceUrl.Generate(webUrl, service);
            EndpointAddress endpointAddress = new EndpointAddress(sharePointServiceUrl.ToString());
            return endpointAddress;
        }

        internal static object CreateSoapClient(Uri webUrl, SharePointService serviceType) {
          // create the binding and proxy
          var endpointAddress = GetEndPointAddress(webUrl, serviceType);
          var binding = SPServicesBindingFactory.CreateBindingInstance();
          var client = CreateSoapClient(binding, endpointAddress, serviceType);
          return client;
        }

        private static object CreateSoapClient(Binding binding, EndpointAddress endpointAddress, SharePointService serviceType) {
          // TODO add Office 365 authentication here
          switch (serviceType) {
            case SharePointService.Webs:
              var client = new WebsSoapClient(binding, endpointAddress);
              client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
              return client;
            case SharePointService.Lists:
              var client2 = new ListsSoapClient(binding, endpointAddress);
              client2.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
              return client2;
            default:
              throw new NotSupportedException();
          }
        }

    } // class
} // namespace
