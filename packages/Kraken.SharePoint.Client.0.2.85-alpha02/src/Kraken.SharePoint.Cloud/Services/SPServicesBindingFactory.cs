
namespace Kraken.SharePoint.Services {

    using System;
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Security;
    using System.ServiceModel.Channels;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Based on http://blogs.msdn.com/johnwpowell/archive/2009/01/03/consume-sharepoint-web-services-with-wcf-using-the-repository-gateway-mapper-domain-model-and-factory-design-patterns.aspx
    /// </summary>
    public class SPServicesBindingFactory {

        /// <summary>
        /// Creates a <cref="Binding">Binding</cref> suitable for SharePoint 2007 web services.
        /// </summary>
        /// <returns></returns>
        public static Binding CreateBindingInstance() {
            var binding = new BasicHttpBinding();
            binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Ntlm;
            binding.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.Ntlm;
            binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
            binding.Security.Message.AlgorithmSuite = SecurityAlgorithmSuite.Default;
            binding.UseDefaultWebProxy = true;
            return binding;
        }

    } // class
} // namespace
