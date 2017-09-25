using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

#if DOTNET_V45
#endif
#if DOTNET_V4
#endif
#if DOTNET_V35
#endif

// TODO port this over to .NET 4.5
// included without qualifiers to ensure that extension methods will work
using Microsoft.IdentityModel.Protocols.WSTrust;
using Microsoft.IdentityModel.Protocols.WSTrust.Bindings;
// included with qualifiers to clear up ambiguity between this namesapce and System.ServiceModel.Security in .NET 4.0
using WIFTrust = Microsoft.IdentityModel.Protocols.WSTrust;
using WIFBindings = Microsoft.IdentityModel.Protocols.WSTrust.Bindings;
// end to do

using System.IdentityModel.Tokens;

using System.ServiceModel;
using System.ServiceModel.Security;
using System.Security.Cryptography.X509Certificates;

namespace Kraken.SharePoint.Client.Connections {
  class MyWSTrustFeb2005RequestSerializer : WIFTrust.WSTrustFeb2005RequestSerializer {
    public override void WriteXmlElement(XmlWriter writer, string elementName, object elementValue, WIFTrust.RequestSecurityToken rst, WIFTrust.WSTrustSerializationContext context) {
      switch (elementName) {
        case "PolicyReference":
          writer.WriteStartElement("wsp", elementName, "http://schemas.xmlsoap.org/ws/2004/09/policy");
          writer.WriteAttributeString("URI", (string)elementValue);
          writer.WriteEndElement();
          break;
        case "KeyType":
          break;
        default:
          base.WriteXmlElement(writer, elementName, elementValue, rst, context);
          break;
      }
    }

    //           var s0 = "urn:federation:MicrosoftOnline";
    //           var s1 = "https://outlook.office365.com/EWS/Exchange.asmx/WSSecurity";
    //
    //    GenericXmlSecurityToken token2 = do_test_exchangeoffice(token, new EndpointAddress( IP ), new EndpointAddress( IPMEX ), s1);
    //    t = token2.TokenXml.OuterXml;

    //  ExchangeService service1 = new ExchangeService(ExchangeVersion.Exchange2013);
    //  service1.Url = new Uri(s1);
    //  service1.PreAuthenticate = true;

    //  service1.Credentials = new TokenCredentials(t);

    //  EmailMessage message = new EmailMessage(service1);
    //  message.Subject = "Interesting";
    //  message.Body = "The merger is finalized.";
    //  message.ToRecipients.Add("rapstaff2@rapmlsqa.com");
    //  message.SendAndSaveCopy();

    private GenericXmlSecurityToken
        do_test_exchangeoffice(SecurityToken fromIP_STS, EndpointAddress issuerAddress, EndpointAddress mexAddress, string exchaddr) {
      const string office365STS = "https://login.microsoftonline.com/extSTS.srf";

      WIFTrust.WSTrustChannel channel = null;

      UriBuilder u = new UriBuilder(office365STS);

      var un = new WIFBindings.UserNameWSTrustBinding(SecurityMode.TransportWithMessageCredential);
      var iss = new WIFBindings.IssuedTokenWSTrustBinding(un, issuerAddress, SecurityMode.TransportWithMessageCredential, TrustVersion.WSTrustFeb2005, mexAddress) {
        EnableRsaProofKeys = false,
        KeyType = SecurityKeyType.BearerKey
      };
      WIFTrust.WSTrustChannelFactory trustChannelFactory2 = new WIFTrust.WSTrustChannelFactory(iss, new EndpointAddress(u.Uri.AbsoluteUri));

      trustChannelFactory2.TrustVersion = TrustVersion.WSTrustFeb2005;
      trustChannelFactory2.ConfigureChannelFactory();
      if (trustChannelFactory2.Credentials != null) trustChannelFactory2.Credentials.SupportInteractive = false;

      trustChannelFactory2.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
      trustChannelFactory2.Credentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;
      trustChannelFactory2.WSTrustRequestSerializer = new MyWSTrustFeb2005RequestSerializer();

      GenericXmlSecurityToken token = null;
      try {
        WIFTrust.RequestSecurityTokenResponse rstr = null;
        WIFTrust.RequestSecurityToken rst = new WIFTrust.RequestSecurityToken(WIFTrust.WSTrustFeb2005Constants.RequestTypes.Issue, WIFTrust.WSTrustFeb2005Constants.KeyTypes.Bearer);
        rst.AppliesTo = new EndpointAddress(exchaddr);
        rst.Properties.Add("PolicyReference", "MBI_FED_SSL");

        channel = (WIFTrust.WSTrustChannel)trustChannelFactory2.CreateChannelWithIssuedToken(fromIP_STS);

        token = channel.Issue(rst, out rstr) as GenericXmlSecurityToken;
      }
#pragma warning disable 0168
      catch (Exception ex) { ; }
#pragma warning restore 0168
      finally {
        if (null != channel) {
          channel.Abort();
        }
        trustChannelFactory2.Abort();
      }
      return token;
    }
  }
}
