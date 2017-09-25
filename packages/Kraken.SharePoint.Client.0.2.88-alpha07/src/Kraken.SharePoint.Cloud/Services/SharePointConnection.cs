using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;

using Kraken.SharePoint.Cloud.Client;
using Kraken.SharePoint.Cloud.Authentication;
using Kraken.SharePoint.Services;
using System.Xml.Linq;
using System.Net;
using System.Web.Services.Protocols;

namespace Kraken.SharePoint.Cloud {

  public class SharePointConnection {

    public event SharePointConnectionEvent OnConnecting;
    public event SharePointConnectionEvent OnConnected;
    public event SharePointConnectionEvent OnConnectionFailed;

    public void DoConnecting(SharePointConnectionEventArgs e) {
      if (OnConnecting != null) {
        OnConnecting(this, e);
      }
    }
    public void DoConnected(SharePointConnectionEventArgs e) {
      if (OnConnected != null) {
        OnConnected(this, e);
      }
    }
    public void DoConnectionFailed(SharePointConnectionEventArgs e) {
      if (OnConnectionFailed != null) {
        OnConnectionFailed(this, e);
      }
    }

    public void Connect() {
      SharePointConnectionEventArgs e = new SharePointConnectionEventArgs();
      e.AuthenticationType = this.AuthenticationType;
      e.Domain = this.LoginDomain;
      e.UserName = this.LoginUser;
      try {
        DoConnecting(e);
        InitializeAllServiceConnections();
        DoConnected(e);
      } catch (WebException wex) {
        // wrap exception in readable error
        if (wex.Message.Contains("Object moved")) {
          Exception ex = new Exception("Site is Claims, Forms, or Multi Auth", wex);
          e.Error = ex;
          DoConnectionFailed(e);
        } else {
          e.Error = wex;
          DoConnectionFailed(e);
        }
      } catch (SoapException sex) {
        if (sex.Message.Contains("Site is not configured for Claims Forms Authentication.")) {
          Exception ex = new Exception("Site not set up for Form Auth", sex);
          e.Error = ex;
          DoConnectionFailed(e);
        } else {
          e.Error = sex;
          DoConnectionFailed(e);
        }
      } catch (Exception ex) {
        e.Error = ex;
        DoConnectionFailed(e);
      }
    }

    #region Web Service Client Managers

    private WebsWebServiceClientManager websManager = null;
    public WebsWebServiceClientManager WebsManager {
      get { return websManager; }
    }
    private WebsWS.Webs websSvc = null;
    public WebsWS.Webs WebsSvc {
      get { return websSvc; }
    }

    private ListsWebServiceClientManager listsManager = null;
    public ListsWebServiceClientManager ListsManager {
      get { return listsManager; }
    }
    private ListsWS.Lists listsSvc = null;
    public ListsWS.Lists ListsSvc {
      get { return listsSvc; }
    }

    private ViewsWebServiceClientManager viewsManager = null;
    public ViewsWebServiceClientManager ViewsManager {
      get { return viewsManager; }
    }
    private ViewsWS.Views viewsSvc = null;
    public ViewsWS.Views ViewsSvc {
      get { return viewsSvc; }
    }

    #endregion

    public Uri Url;
    public SharePointAuthenticationType AuthenticationType;
    public string LoginDomain;
    public string LoginUser;
    public SecureString LoginPassword;

    private void CheckAuthenticationSettings() {
      if (AuthenticationType == SharePointAuthenticationType.None)
        throw new ArgumentNullException("You must specify a value for AuthenticationType", "AuthenticationType");
      if (string.IsNullOrEmpty(LoginUser) && AuthenticationType != SharePointAuthenticationType.CurrentWindowsUser)
        throw new ArgumentNullException("You must specify a value for LoginUser", "LoginUser");
      if (string.IsNullOrEmpty(LoginDomain) && AuthenticationType == SharePointAuthenticationType.SpecifyWindowsUser)
        throw new ArgumentNullException("You must specify a value for LoginDomain", "LoginDomain");
      if (LoginPassword == null)
        throw new ArgumentNullException("You must specify a value for LoginPassword", "LoginPassword");
    }

    private void InitiateWebsServiceConnection() {
      CheckAuthenticationSettings();
      if (this.websSvc == null) {
        this.websSvc = WebsWebServiceClientManager.CreateInstance(
          this.Url,
          this.AuthenticationType,
          this.LoginUser,
          this.LoginDomain,
          this.LoginPassword
        );
      } else {
        this.websSvc.Url = this.Url.ToString();
      }
      this.websManager = new WebsWebServiceClientManager(websSvc);
    }
    private void InitiateListsServiceConnection() {
      CheckAuthenticationSettings();
      if (this.listsSvc == null) {
        this.listsSvc = ListsWebServiceClientManager.CreateInstance(
          this.Url,
          this.AuthenticationType,
          this.LoginUser,
          this.LoginDomain,
          this.LoginPassword
        );
      } else {
        this.listsSvc.Url = this.Url.ToString();
      }
      this.listsManager = new ListsWebServiceClientManager(listsSvc);
    }
    private void InitiateViewsServiceConnection() {
      CheckAuthenticationSettings();
      if (this.viewsSvc == null) {
        this.viewsSvc = ViewsWebServiceClientManager.CreateInstance(
          this.Url,
          this.AuthenticationType,
          this.LoginUser,
          this.LoginDomain,
          this.LoginPassword
        );
      } else {
        this.viewsSvc.Url = this.Url.ToString();
      }
      this.viewsManager = new ViewsWebServiceClientManager(viewsSvc);
    }

    private void InitializeAllServiceConnections() {
      CheckAuthenticationSettings();
      InitiateWebsServiceConnection();
      InitiateListsServiceConnection();
      InitiateViewsServiceConnection();
    }

    public void MoveToWeb(Uri url) {
      this.Url = url;
      Connect();
    }

    public XElement ExportListsViewsAndContentTypes(
      SharePointNode item,
      ListExportOptions listOptions,
      ContentTypeExportOptions ctOptions,
      SiteColumnExportOptions fieldOptions
    ) {
      XElement elements = new XElement("Elements");
      XElement listXml = this.ListsManager.ExportList(item, listOptions, this, ctOptions, fieldOptions);
      elements.Add(listXml);
      return elements;
    }

  }

  public class SharePointConnectionEventArgs : EventArgs {
    public Exception Error;
    public string Url;
    public SharePointAuthenticationType AuthenticationType;
    public string UserName;
    public string Domain;
  }

  public delegate void SharePointConnectionEvent(object sender, SharePointConnectionEventArgs e);

}
