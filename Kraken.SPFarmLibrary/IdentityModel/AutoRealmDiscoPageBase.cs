using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.IdentityModel;
using Microsoft.SharePoint.IdentityModel.Pages;

//using Kraken.Net.IpNetworking;
using Kraken.SharePoint;
using authControls = Kraken.SharePoint.IdentityModel.Controls;
using Kraken.SharePoint.Logging;

namespace Kraken.SharePoint.IdentityModel.Pages {

  public partial class AutoRealmDiscoPageBase : IdentityModelSignInPageBase {

    public AutoRealmDiscoPageBase() : base() { }

    protected EncodedLiteral ClaimsLogonPageMessage;
    protected EncodedLiteral ClaimsLogonPageTitle;
    protected EncodedLiteral ClaimsLogonPageTitleInTitleArea;
    //protected LogonSelector ClaimsLogonSelector;
    protected authControls.LogonSelector ClaimsLogonSelector;
    public const string ErrorCode = "errorCode";

    protected KrakenLoggingService log = KrakenLoggingService.CreateNew(
      new LoggingProperties() {
        DefaultCategory = LoggingCategories.KrakenClaims,
      }
    );

    internal SPWebApplication WebApplication {
      get {
        try {
          if (SPContext.Current == null)
            throw new ArgumentNullException("SPContext.Current");
          if (SPContext.Current.Site == null)
            throw new ArgumentNullException("SPContext.Current.Site");
          if (SPContext.Current.Site.WebApplication == null)
            throw new ArgumentNullException("SPContext.Current.Site.WebApplication");
          return SPContext.Current.Site.WebApplication;
        } catch (Exception ex) {
          log.Write(ex);
          return null;
        }
      }
    }

    /*
    internal SPIisSettings IisSettings {
      get {
        try {
          if (this.Request == null)
            throw new ArgumentNullException("this.Request");
          return this.WebApplication.GetIISSettings(this.Request.Url);
        } catch {
          return null;
        }
      }
    }
    */

    protected SPAuthenticationProvider GetAuthenticationProvider(string targetProvider, AuthenticationProviderSearchProperty findBy) {
      if (this.IisSettings == null)
        throw new ArgumentNullException("this.IisSettings");
      SPAuthenticationProvider provider = null;
      try {
        provider = this.IisSettings.GetClaimsAuthenticationProvider(targetProvider, findBy);
        if (provider == null)
          log.Write(string.Format("No claims authentication provider found with name '{0}'. Please check your configuration settings.", targetProvider), TraceSeverity.Medium, EventSeverity.Warning, LoggingCategories.KrakenProfiles);
      } catch (Exception ex) {
        log.Write(string.Format("No claims authentication provider found with name '{0}'. Please check your configuration settings.", targetProvider), TraceSeverity.High, EventSeverity.Error, LoggingCategories.KrakenProfiles);
        log.Write(ex);
      }
      return provider;
    }

    public override void ProcessRequest(HttpContext context) {
      try {
        base.ProcessRequest(context);
      } catch (Exception ex) {
        // Catch any errors and make sure we report in detail about the problem to the ULS logs.
        log.Write(ex);
        throw ex;
      }
    }

    protected override void CreateChildControls() {
      try {
        base.CreateChildControls();
      } catch (Exception ex) {
        // Catch any errors and make sure we report in detail about the problem to the ULS logs.
        log.Write(ex);
        throw ex;
      }
    }

    protected override void OnPreRender(EventArgs e) {
      try {
        base.OnPreRender(e);
      } catch (Exception ex) {
        // Catch any errors and make sure we report in detail about the problem to the ULS logs.
        log.Write(ex);
        throw ex;
      }
    }

    protected override void LoadControlState(object savedState) {
      base.LoadControlState(savedState);
    }

    protected override void LoadViewState(object savedState) {
      base.LoadViewState(savedState);
    }

    protected override void TryToRedirectMobileAccess() {
      base.TryToRedirectMobileAccess();
    }

    /// <summary>
    /// Setting this to false should prevent the 403 error in base.OnLoad()
    /// </summary>
    protected override bool CheckForFormsAccess {
      get {
        return false;
      }
    }

    protected SPAuthenticationProvider GetRealmAffinityProvider(HttpRequest request, string cookieName) {
      SPAuthenticationProvider provider = request.GetRealmAffinityProvider(this.IisSettings, cookieName);
      return provider;
    }

    #region Essentially copied from MultiLoginPage

    protected string GetErrorMessage(HttpRequest request) {
      if (request == null) {
        throw new ArgumentNullException("request");
      }
      string str = null;
      string a = null;
      if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)) {
        a = request.QueryString["errorCode"];
      }
      if (a == null) {
        return null;
      }
      if (string.Equals(a, "TrustedMissingIdentityClaim", StringComparison.OrdinalIgnoreCase)) {
        return SPResource.GetString("DefaultTrustedClaimAuthenticationError", new object[0]);
      }
      if (!string.IsNullOrEmpty(a)) {
        str = SPResource.GetString("DefaultClaimAuthenticationError", new object[0]);
      }
      return str;
    }


    protected override void OnInit(EventArgs e) {
      base.SetThreadCultureFromRequestedWeb();
    }

    protected override void OnLoad(EventArgs e) {
      log.Write(string.Format("Entering '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      base.OnLoad(e);
      if (HttpContext.Current == null)
        throw new ArgumentNullException("HttpContext.Current");
      if (HttpContext.Current.Request == null)
        throw new ArgumentNullException("HttpContext.Current.Request");

      HttpRequest request = HttpContext.Current.Request;

      // render the realm selector
      string errorMessage = this.GetErrorMessage(request);
      if (errorMessage != null) {
        this.ClaimsLogonPageTitle.Text = SPHttpUtility.NoEncode((string)HttpContext.GetGlobalResourceObject("wss", "error_pagetitle", Thread.CurrentThread.CurrentUICulture));
        this.ClaimsLogonPageTitleInTitleArea.Text = SPHttpUtility.NoEncode((string)HttpContext.GetGlobalResourceObject("wss", "error_pagetitle", Thread.CurrentThread.CurrentUICulture));
        this.ClaimsLogonPageMessage.Text = SPHttpUtility.NoEncode(errorMessage);
        this.ClaimsLogonSelector.Visible = false;
        this.ClaimsLogonSelector.Enabled = false;
      } else {
        this.ClaimsLogonPageTitle.Text = SPHttpUtility.NoEncode((string)HttpContext.GetGlobalResourceObject("wss", "login_pagetitle", Thread.CurrentThread.CurrentUICulture));
        this.ClaimsLogonPageTitleInTitleArea.Text = SPHttpUtility.NoEncode((string)HttpContext.GetGlobalResourceObject("wss", "login_pagetitle", Thread.CurrentThread.CurrentUICulture));
        this.ClaimsLogonPageMessage.Text = SPHttpUtility.NoEncode(SPResource.GetString("SelectAuthenticationMethod", new object[0]));
        this.ClaimsLogonSelector.Focus();
        // When there is only one authentication provider, this method will redirect to its login page
        // Note that passing true to skipRedirectionPage and skipMultiLoginPage we are staying on this one if there is any choice to be made
        Uri claimsAuthenticationLoginRedirectionUrl = this.IisSettings.GetClaimsAuthenticationLoginRedirectionUrl(true, true);
        if (null != claimsAuthenticationLoginRedirectionUrl)
        {
            string components = HttpContext.Current.Request.Url.GetComponents(UriComponents.Query, UriFormat.SafeUnescaped);
            SPUtility.Redirect(claimsAuthenticationLoginRedirectionUrl.ToString(), SPRedirectFlags.Default, this.Context, components);
        }

      }
      log.Write(string.Format("Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
    }

    protected override string MobilePageUrl {
      get {
        string components = HttpContext.Current.Request.Url.GetComponents(UriComponents.Query, UriFormat.SafeUnescaped);
        return ("/_layouts/mobile/mblmultilogin.aspx" + '?' + components);
      }
    }
  }

  #endregion

}
