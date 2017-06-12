#define __INCLUDE_SECURE_STRING_CODE__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Services.Protocols;
using System.Net;
using System.Security;

using Kraken.SharePoint.Cloud.Authentication;
using Kraken.SharePoint.Services;
//#if __INCLUDE_SECURE_STRING_CODE__
// NOTE this might stop a cloud based solution from working!
using Ksec = Kraken.Core.Security; // TODO rename it Kraken.Security
//#endif

using Kraken.SharePoint.Cloud;

namespace Kraken.SharePoint.Cloud.Client {

  public class WebServiceClientManager<T> where T : SoapHttpClientProtocol, new() {

    private T webService;
    protected T WebService {
      get { return webService; }
    }

    public WebServiceClientManager(T webService) {
      this.webService = webService;
    }

    /// <summary>
    /// Relocate the service to a new web URL on the same server.
    /// You should not use this method to move to another server or auth type.
    /// </summary>
    /// <param name="url"></param>
    protected virtual void MoveWeb(string url) {
      // override and provide your own functions
    }

    #region Web Service Initiators

    protected static T CreateInstance(Uri webUrl, ICredentials credentials, CookieContainer cookies) { //where T : SoapHttpClientProtocol, new() {
      T webServiceInstance = new T();
      SharePointService serviceType = GetSharePointServiceFromType(webServiceInstance.GetType());
      webServiceInstance.AllowAutoRedirect = true; // needed for some SP auth to work
      webServiceInstance.Url = SPServiceUrl.GenerateAsmx(webUrl, serviceType).ToString();
      if (cookies != null)
        webServiceInstance.CookieContainer = cookies;
      if (credentials != null)
        webServiceInstance.Credentials = credentials;
      else
        webServiceInstance.Credentials = System.Net.CredentialCache.DefaultCredentials;
      return webServiceInstance;
    }

#if DoCompletelyInsecureThingsAnyway
    public static T CreateInstance(string webUrl, SharePointAuthenticationType authType, string username, string domain, string password) { // where T : SoapHttpClientProtocol, new() {
      throw new NotImplementedException("This is completely insecure; use Kraken.Security.SecureStringMarshaller to pass in data.");
#if DoCompletelyInsecureThingsAnyway
      ICredentials cred = null;
      CookieContainer cookies = null;
      switch (authType) {
        case SharePointAuthenticationType.CurrentWindowsUser:
          cred = CredentialCache.DefaultCredentials;
          break;
        case SharePointAuthenticationType.SpecifyWindowsUser:
          // TODO be careful about storing passwords, even in memory!
          // un-marshall the secure string here...
          cred = new NetworkCredential(username, password, domain);
          break;
        case SharePointAuthenticationType.FormsBasedLogin:
        case SharePointAuthenticationType.Office365Login:
          SecureString securePass = new SecureString();
          foreach (char c in password) {
            securePass.AppendChar(c);
          }
          securePass.MakeReadOnly();
          // for any secure string password option
          return CreateInstance(webUrl, authType, username, domain, securePass);
      }
      // for the plaintext and implied password options
      T webSvc = CreateInstance(webUrl, cred, cookies);
      return webSvc;
#endif
    }
#endif

    public static T CreateInstance(Uri webUrl, SharePointAuthenticationType authType, string username, string domain, SecureString password) { // where T : SoapHttpClientProtocol, new() {
      // log into SharePoint if necessary (such as create cookie, etc)
      ICredentials cred = null;
      CookieContainer cookies = null;
      switch (authType) {
        case SharePointAuthenticationType.CurrentWindowsUser:
          cred = CredentialCache.DefaultCredentials;
          break;
        case SharePointAuthenticationType.SpecifyWindowsUser:
//#if __INCLUDE_SECURE_STRING_CODE__
          // TODO This probably only works on the client side
          //unsafe {
            using (Ksec.SecureStringMarshaller pwm = new Ksec.SecureStringMarshaller(password)) {
              pwm.Decrypt();
              cred = new NetworkCredential(username, pwm.ToString(), domain);
            }
          //}
//#else
//          throw new NotSupportedException();
//#endif
          break;
        case SharePointAuthenticationType.FormsBasedLogin:
//#if __INCLUDE_SECURE_STRING_CODE__
          // TODO This probably only works on the client side
//          unsafe {
            using (Ksec.SecureStringMarshaller pwm = new Ksec.SecureStringMarshaller(password)) {
              pwm.Decrypt();
              cred = new NetworkCredential(username, pwm.ToString(), domain);
            }
//          }
//#endif
          cookies = GetAuthenticationServiceCookies(username, password);
          if (cookies != null) {
          }
          break;
        case SharePointAuthenticationType.Office365Login:
          // Create Office 365 cookie
          O365ClientContext occ = new O365ClientContext(webUrl, username, password);
          if (occ.Context != null) {
            cred = CredentialCache.DefaultCredentials;
            cookies = occ.CookieContainer;
          } else {
            throw new SecurityException("Authentication to Office 365 didn't work!");
          }
          break;
      }
      T webSvc = CreateInstance(webUrl, cred, cookies);
      return webSvc;
    }

    #endregion

    private static CookieContainer GetAuthenticationServiceCookies(string username, SecureString password) {
      AuthenticationWS.Authentication authService = new AuthenticationWS.Authentication();
      authService.CookieContainer = new System.Net.CookieContainer();
      authService.AllowAutoRedirect = true;
      AuthenticationWS.LoginResult result = null;
      // TODO This probably only works on the client side
//      unsafe {
        using (Ksec.SecureStringMarshaller pwm = new Ksec.SecureStringMarshaller(password)) {
          pwm.Decrypt();
          result = authService.Login(username, pwm.ToString());
        }
//      }
      if (result.ErrorCode == AuthenticationWS.LoginErrorCode.NoError) {
        AuthenticationWS.AuthenticationMode mode = authService.Mode();
        return authService.CookieContainer;
      } else {
        return null;
      }
    }

    private static SharePointService GetSharePointServiceFromType(Type type) {
      if (type == typeof(SitesWS.Sites)) return SharePointService.sites;
      if (type == typeof(WebsWS.Webs)) return SharePointService.Webs;
      if (type == typeof(ListsWS.Lists)) return SharePointService.Lists;
      if (type == typeof(ViewsWS.Views)) return SharePointService.Views;
      if (type == typeof(WebPartPagesWS.WebPartPagesWebService)) return SharePointService.webpartpages;
      if (type == typeof(UserProfileServiceWS.UserProfileService)) return SharePointService.userprofileservice;
      if (type == typeof(SPSearchWS.QueryService)) return SharePointService.spsearch;
      if (type == typeof(SearchWS.QueryService)) return SharePointService.search;
      if (type == typeof(ExcelServiceWS.ExcelService)) return SharePointService.ExcelService;
      if (type == typeof(SiteDataWS.SiteData)) return SharePointService.SiteData;
      // TODO implement the rest of these...
      /*
        alerts,
        Authentication,
        bdcfieldsresolver,
        businessdatacatalog,
        contentAreaToolboxService,
        Copy,
        DspSts,
        DWS,
        Forms,
        FormsServiceProxy,
        FormsServices,
        Imaging,
        Meetings,
        officialfile,
        People,
        Permissions,
        publishedlinksservice,
        PublishingService,
        sharepointemailws,
        sites,
        SlideLibrary,
        SpellCheck,
        spscrawl,
        UserGroup,
        userprofilechangeservice,
        versions,
        workflow
          */
      throw new NotSupportedException();
    }

  }

}
