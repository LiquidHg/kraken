using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Net;
using Microsoft.SharePoint.Client;

namespace Kraken.SharePoint.Client.Connections {

  public class SharePointCredential : ICredentials {

    #region Needed for claims authentication

    private static object cookiesSyncLock = new object();

    private static CookieContainer cookies;

    #endregion

    public SharePointCredential() { }
    public SharePointCredential(string user, SecureString pass, ClientAuthenticationType authType) {
      this.UserName = user;
      this.UserPassword = pass;
      this.AuthType = authType;
    }
    public SharePointCredential(ICredentials credential) {
      if (credential == null) {
        this.AuthType = ClientAuthenticationType.Unknown;
      } else {
        this.UnderlyingCredentials = credential;
        this.UserName = credential.GetUserName();
        // derive the auth type based on cred type
#if !DOTNET_V35
        if (credential.GetType() == typeof(SharePointOnlineCredentials))
          this.AuthType = ClientAuthenticationType.SPOCredentials;
        else
#endif
          if (credential.GetType() == typeof(NetworkCredential))
            this.AuthType = ClientAuthenticationType.SharePointNTLMCurrentUser;
        // TODO what do we do when we can't reverse engineer this??
      }
    }

    public ICredentials UnderlyingCredentials { get; protected set; }

    public string UserName { get; set; }
    public SecureString UserPassword { get; set; }
    public ClientAuthenticationType AuthType { get; set; }

    public void Validate(bool requireUserPass = true) {
      if (string.IsNullOrEmpty(this.UserName) || this.UserPassword == null)
        throw new ArgumentNullException("Can't establish credentials without user name and password.");
    }

    public NetworkCredential GetCredential(Uri webUri, string authType = "") {
      if (!string.IsNullOrEmpty(authType))
        this.AuthType = (ClientAuthenticationType)Enum.Parse(typeof(ClientAuthenticationType), authType);
      // TODO go ahead and allow this in cases where we can do it, like NTLM
      switch (this.AuthType) {
        case ClientAuthenticationType.SharePointNTLMUserPass:
          return (NetworkCredential)GetCredential(webUri);
        default:
          throw new NotSupportedException("You should call ICredential = GetCredential(url) version of this function instead.");
      }
    }
    public ICredentials GetCredential(Uri webUri = null) {
      if (this.UnderlyingCredentials != null)
        return this.UnderlyingCredentials;
      //if (webUri == null)
      //  throw new ArgumentNullException("Can't establish credentials without SharePoint web URL.");
      // Auto-detect need for SharePointOnlineCredentials
      if (this.AuthType == ClientAuthenticationType.Unknown
        && webUri.Authority.ToLower().Contains(".sharepoint.com")) {
        this.AuthType = ClientAuthenticationType.SPOCredentials;
      }
      switch (this.AuthType) {
        case ClientAuthenticationType.SPOCredentials:
          Validate();
#if !DOTNET_V35
          UnderlyingCredentials = new SharePointOnlineCredentials(this.UserName, this.UserPassword);
          break;
#else
          throw new NotSupportedException("SharePointOnlineCredentials is not supported in this version of CSOM.");
#endif
        case ClientAuthenticationType.SharePointNTLMUserPass:
        case ClientAuthenticationType.SharePointForms:
        case ClientAuthenticationType.SharePointClaims:
          Validate();
#if !DOTNET_V35
          UnderlyingCredentials = new NetworkCredential(this.UserName, this.UserPassword);
#else
          using (Kraken.Core.Security.SecureStringMarshaller sm = new Core.Security.SecureStringMarshaller(this.UserPassword)) {
            UnderlyingCredentials = new NetworkCredential(this.UserName, sm.ToString());
          }
#endif
          break;
        case ClientAuthenticationType.SharePointNTLMCurrentUser:
          Validate();
          this.UnderlyingCredentials = System.Net.CredentialCache.DefaultNetworkCredentials;
          break;
        //case ClientAuthenticationType.SPOCustomCookie:
        // TODO support various SharePoint authentication schemes here
        default:
          throw new NotImplementedException(string.Format("The supplied client authentication type is not implemented. authType={0}", this.AuthType.ToString()));
      }
      return UnderlyingCredentials;
    }

    public CookieContainer CreateSharePointOnlineCookies(ClientContext context) {
      if (this.UnderlyingCredentials == null)
        throw new ArgumentNullException("this.UnderlyingCredentials");
#if !DOTNET_V35
      SharePointOnlineCredentials spoCred = this.UnderlyingCredentials as SharePointOnlineCredentials;
      if (spoCred == null)
        return null;
      return CreateSharePointOnlineCookies(context, spoCred);
#else
      throw new NotSupportedException("SharePointOnlineCredentials is not supported in this version of CSOM.");
#endif
    }

#if !DOTNET_V35
    // TODO a logical place for this would be WebContextManager
    public static CookieContainer CreateSharePointOnlineCookies(ClientContext context, SharePointOnlineCredentials spoCred) {
      CookieContainer cookies = new CookieContainer();
      Uri contextUri = new Uri(context.Url);
      string cookieValue = spoCred.GetAuthenticationCookie(contextUri);
      // Create FEDAUTH Cookie
      Cookie fedAuth = new Cookie();
      fedAuth.Name = "FedAuth";
      fedAuth.Value = cookieValue.TrimStart("SPOIDCRL=");
      fedAuth.Path = "/";
      fedAuth.Secure = true;
      fedAuth.HttpOnly = true;
      fedAuth.Domain = contextUri.Host;
      // Connect auth cookie to request
      cookies.Add(fedAuth);
      return cookies;
    }
#endif

    /// <summary>
    /// These methods can be used to conver MSOIDCLI tickets into cookies used for authentication
    /// </summary>
    /// <param name="baseUri"></param>
    /// <param name="endpoint"></param>
    /// <param name="ticket"></param>
    /// <returns></returns>
    public static string ConvertTicketToCookie(Uri baseUri, string endpoint, string ticket) {
      Uri uri = new Uri(baseUri, endpoint);
      return ConvertTicketToCookie(uri, ticket);
    }
    public static string ConvertTicketToCookie(Uri uri, string ticket) {
      HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
      CookieContainer container = new CookieContainer();
      request.CookieContainer = container;
      request.Headers[HttpRequestHeader.Authorization] = "BPOSIDCRL " + ticket;
      WebResponse response = request.GetResponse();
      string cookieHeader = container.GetCookieHeader(uri);
      if (response != null) {
        response.Close();
      }
      return cookieHeader;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// http://www.sharepoint-reference.com/Blog/Lists/Posts/Post.aspx?ID=34
    /// </remarks>
    /// <param name="ctx"></param>
    public void ConfigureContext(ClientContext ctx) {
      if (ctx == null)
        throw new ArgumentNullException("ctx");
      switch (this.AuthType) {
        case ClientAuthenticationType.SharePointClaims:
          var baseSiteUrl = ctx.Url;

          // Configure anonymous authentication, because we will use FedAuth cookie instead
          ctx.AuthenticationMode = ClientAuthenticationMode.Anonymous;

          // Register an anonymous delegate to the ExecutingWebRequest event handler
          ctx.ExecutingWebRequest += new EventHandler<WebRequestEventArgs>((s, e) => {

            // If we do not have a cookies variable, which will be a shared instance of a CookieContainer 
            if (null == cookies) {
              lock (cookiesSyncLock) {
                if (null == cookies) {
                  // Let’s create the CookieContainer instance
                  cookies = new CookieContainer();

                  // Make a “fake” request to the /_windows/default.aspx page
                  // emulating the flow previously illustrated
                  Uri baseUri = new Uri(baseSiteUrl);
                  var baseServerUrl = baseUri.AbsoluteUri.TrimEnd(baseUri.AbsolutePath.ToCharArray());

                  HttpWebRequest request = WebRequest.Create(
                      baseServerUrl + "/_windows/default.aspx?ReturnUrl=%2f_layouts%2fAuthenticate.aspx%3fSource%3d%252FDefault%252Easpx&Source=%2FDefault.aspx") as HttpWebRequest;

                  // Provide a set of Windows credentials (default or explicit)
                  request.Credentials = ctx.Credentials;
                  request.Method = "GET";

                  // Assign the CookieContainer object
                  request.CookieContainer = cookies;
                  request.AllowAutoRedirect = false;

                  // Execute the HTTP request
                  HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                  if (null != response) {
                    // The following variable simply holds the FedAuth cookie value, but that value
                    // is not used directly
                    var fedAuthCookieValue = response.Cookies["FedAuth"].Value;
                  }
                }
              }
            }

            // Grab the CookieContainer, which now holds the FedAuth cookie, and configure
            // it into the WebRequest that the ClientContext is going to execute and …
            // you have done all you need!
            e.WebRequestExecutor.WebRequest.CookieContainer = cookies;
          });
          break;

        case ClientAuthenticationType.SharePointForms:
          ctx.AuthenticationMode = ClientAuthenticationMode.FormsAuthentication;
          using (Kraken.Core.Security.SecureStringMarshaller sm = new Core.Security.SecureStringMarshaller(this.UserPassword)) {
            if (!sm.IsDecrypted) sm.Decrypt();
            ctx.FormsAuthenticationLoginInfo = new FormsAuthenticationLoginInfo(this.UserName, sm.ToString());
          }
          break;
      }

    }

  }
}
