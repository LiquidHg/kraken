using System;
using System.Net.Security;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;

using Microsoft.IdentityModel.Protocols.WSTrust;

namespace Kraken.SharePoint.Cloud.Authentication {

  [ServiceContract]
  public interface IWSTrustFeb2005Contract {
    [OperationContract(ProtectionLevel = ProtectionLevel.EncryptAndSign, Action = "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue", ReplyAction = "http://schemas.xmlsoap.org/ws/2005/02/trust/RSTR/Issue", AsyncPattern = true)]
    IAsyncResult BeginIssue(System.ServiceModel.Channels.Message request, AsyncCallback callback, object state);
    System.ServiceModel.Channels.Message EndIssue(IAsyncResult asyncResult);
  }

  /// <summary>
  /// </summary>
  /// <remarks>
  /// Attribution: This module is based on the work of Wictor Wilén and Steve
  /// Peschka, without whom all of us would still be banging rocks together 
  /// trying to make our stuff claims compatible.
  /// Wictor Wilén - SharePoint MCA, MCSM, MCM and MVP
  /// http://www.wictorwilen.se/Post/How-to-do-active-authentication-to-Office-365-and-SharePoint-Online.aspx
  /// Share-n-dipity
  /// http://blogs.technet.com/b/speschka/
  /// </remarks>
  public partial class WSTrustFeb2005ContractClient : ClientBase<IWSTrustFeb2005Contract>, IWSTrustFeb2005Contract {
    public WSTrustFeb2005ContractClient(Binding binding, EndpointAddress remoteAddress)
      : base(binding, remoteAddress) {
    }

    public IAsyncResult BeginIssue(Message request, AsyncCallback callback, object state) {
      return base.Channel.BeginIssue(request, callback, state);
    }

    public Message EndIssue(IAsyncResult asyncResult) {
      return base.Channel.EndIssue(asyncResult);
    }
  }

  /// <summary>
  /// </summary>
  /// <remarks>
  /// Attribution: This module is based on the work of Wictor Wilén and Steve
  /// Peschka, without whom all of us would still be banging rocks together 
  /// trying to make our stuff claims compatible.
  /// Wictor Wilén - SharePoint MCA, MCSM, MCM and MVP
  /// http://www.wictorwilen.se/Post/How-to-do-active-authentication-to-Office-365-and-SharePoint-Online.aspx
  /// Share-n-dipity
  /// http://blogs.technet.com/b/speschka/
  /// </remarks>
  public class RequestBodyWriter : BodyWriter {
    WSTrustRequestSerializer _serializer;
    RequestSecurityToken _rst;

    /// <summary>
    /// Constructs the Body Writer.
    /// </summary>
    /// <param name="serializer">Serializer to use for serializing the rst.</param>
    /// <param name="rst">The RequestSecurityToken object to be serialized to the outgoing Message.</param>
    public RequestBodyWriter(WSTrustRequestSerializer serializer, RequestSecurityToken rst)
      : base(false) {
      if (serializer == null)
        throw new ArgumentNullException("serializer");

      this._serializer = serializer;
      this._rst = rst;
    }

    /// <summary>
    /// Override of the base class method. Serializes the rst to the outgoing stream.
    /// </summary>
    /// <param name="writer">Writer to which the rst should be written.</param>
    protected override void OnWriteBodyContents(XmlDictionaryWriter writer) {
      _serializer.WriteXml(_rst, writer, new WSTrustSerializationContext());
    }
  }

}
