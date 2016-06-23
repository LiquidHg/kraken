using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Administration.Claims;
using System.Collections.ObjectModel;

namespace Kraken.SharePoint.IdentityModel {

  public static class SPAuthExtensions {

    internal static SPIisSettings GetIISSettings(this SPWebApplication app, Uri requestUrl) {
      SPAlternateUrl u = app.AlternateUrls[requestUrl];
      if (u == null)
        throw new Exception(string.Format("No AAM configured for '{0}'", requestUrl));
      SPUrlZone zone = u.UrlZone;
      SPIisSettings settings = app.IisSettings[zone];
      if (settings == null)
        throw new Exception(string.Format("Couldn't get IIS settings for '{0}'", requestUrl));
      return settings;
    }

    /*
    // TODO move to claims lib
    public static SPIisSettings GetIisSettings(this SPSite site) {
      SPIisSettings iisSettings = null;
      if (site != null) {
        iisSettings = site.IisSettings;
      }
      return iisSettings;
    }
    public static SPIisSettings GetIisSettings(this HttpContext context) {
        SPIisSettings iisSettings = null;
        if (context != null) {
          iisSettings = SPClaimProviderManager.GetSettingsForContext(context.Request.Url);
        }
        return iisSettings;
    }
     */

    internal static SPAuthenticationProvider GetClaimsAuthenticationProvider(this SPWebApplication app, Uri requestUrl, string targetProvider) {
      SPIisSettings settings = app.GetIISSettings(requestUrl);
      return settings.GetClaimsAuthenticationProvider(targetProvider);
    }
    internal static SPAuthenticationProvider GetClaimsAuthenticationProvider(this SPIisSettings settings, string targetProvider) {
      return settings.GetClaimsAuthenticationProvider(targetProvider, AuthenticationProviderSearchProperty.Any);
    }
    internal static SPAuthenticationProvider GetClaimsAuthenticationProvider(this SPIisSettings settings, string targetProvider, AuthenticationProviderSearchProperty findBy) {
      if (string.IsNullOrEmpty(targetProvider))
        return null;
      if (findBy == AuthenticationProviderSearchProperty.Any) {
        // note that provider.Name could not be used here because it is internal
        foreach (SPAuthenticationProvider provider in settings.ClaimsAuthenticationProviders) {
          if (string.Equals(provider.GetName(), targetProvider, StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(provider.ClaimProviderName, targetProvider, StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(provider.DisplayName, targetProvider, StringComparison.InvariantCultureIgnoreCase)) {
            return provider;
          }
        }
      } else {
        string name = string.Empty;
        foreach (SPAuthenticationProvider provider in settings.ClaimsAuthenticationProviders) {
          switch (findBy) {
            case AuthenticationProviderSearchProperty.ClaimProviderName:
              name = provider.ClaimProviderName;
              break;
            case AuthenticationProviderSearchProperty.DisplayName:
              name = provider.DisplayName;
              break;
            case AuthenticationProviderSearchProperty.Name:
              // note that provider.Name could not be used here because it is internal
              name = provider.GetName();
              break;
          }
          if (!string.IsNullOrEmpty(name) && string.Equals(name, targetProvider, StringComparison.InvariantCultureIgnoreCase))
            return provider;
        }
      }
      return null;
    }

    public static void SetRealmAffinityProvider(this SPAuthenticationProvider provider, HttpRequest request, string cookieName, string cookieDomain, int cookieLifetimeDays) {
      if (request == null)
        throw new ArgumentNullException("request");
      if (string.IsNullOrEmpty(cookieName))
        throw new ArgumentNullException("cookieName");
      if (cookieLifetimeDays < 0)
        cookieLifetimeDays = 0;
      if (request.Cookies[cookieName] == null) {
        HttpCookie cookie = new HttpCookie(cookieName);
        if (!string.IsNullOrEmpty(cookieDomain))
          cookie.Domain = cookieDomain;
        if (cookieLifetimeDays > 0)
          cookie.Expires = DateTime.Now.AddDays(cookieLifetimeDays);
        cookie.Value = provider.GetName();
        request.Cookies.Add(cookie);
      }
    }
    public static SPAuthenticationProvider GetRealmAffinityProvider(this HttpRequest request, SPIisSettings settings, string cookieName) {
      if (string.IsNullOrEmpty(cookieName))
        throw new ArgumentNullException("cookieName");
      if (request.Cookies[cookieName] == null)
        return null;
      string realmAffinityProvider = request.Cookies[cookieName].Value;
      SPAuthenticationProvider provider = settings.GetClaimsAuthenticationProvider(realmAffinityProvider, AuthenticationProviderSearchProperty.Any);
      return provider;
    }

    /// <summary>
    /// Gets a dictionary of claim providers where key is the ClaimProviderName and value is the DisplayName
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    public static Dictionary<string, string> GetClaimsAuthenticationProviderNameList(this SPIisSettings settings) {
      Dictionary<string, string> providerNames = new Dictionary<string, string>();
      foreach (SPAuthenticationProvider provider in settings.ClaimsAuthenticationProviders) {
        // note that provider.Name could not be used here because it is internal, but that was OK because the same is true in GetClaimsAuthenticationProvider
        string name = provider.GetName();
        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(provider.DisplayName))
          providerNames.Add(name, provider.DisplayName);
      }
      return providerNames;
    }

    /// <summary>
    /// Does the same work as the internal proeprty "Name" which is inaccessible.
    /// </summary>
    /// <param name="provider"></param>
    public static string GetName(this SPAuthenticationProvider provider) {
      if (provider is SPWindowsAuthenticationProvider)
        return "Windows";
      if (provider is SPFormsAuthenticationProvider)
        return "Forms";
      if (provider is SPTrustedAuthenticationProvider) {
        SPTrustedAuthenticationProvider tp = provider as SPTrustedAuthenticationProvider;
        return "Trusted" + tp.LoginProviderName;
      }
      return string.IsNullOrEmpty(provider.DisplayName) ? "Unnamed Provider" : provider.DisplayName;
    }

    public static void RedirectToLoginPage(this SPAuthenticationProvider provider) {
      provider.RedirectToLoginPage(HttpContext.Current);
    }
    /// <summary>
    /// Redirect the browser to the default URL for an authentication provider
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="context"></param>
    public static void RedirectToLoginPage(this SPAuthenticationProvider provider, HttpContext context) {
      if (context == null)
        context = HttpContext.Current;
      if (context == null)
        throw new ArgumentNullException("context");
      string components = context.Request.Url.GetComponents(UriComponents.Query, UriFormat.SafeUnescaped);
      string url = provider.AuthenticationRedirectionUrl.ToString();
      if (provider is SPWindowsAuthenticationProvider) {
        // this internal method replaces the one that is sealed inside SPUtility
        components = AuthUrlTools.EnsureUrlSkipsFormsAuthModuleRedirection(components, true);
      }
      SPUtility.Redirect(url, SPRedirectFlags.Default, context, components);
    }


    public static Uri GetClaimsAuthenticationProviderLoginUrl(this SPWebApplication app, Uri requestUrl, string targetProvider) {
      SPIisSettings settings = app.GetIISSettings(requestUrl);
      return settings.GetClaimsAuthenticationProviderLoginUrl(targetProvider);
    }
    public static Uri GetClaimsAuthenticationProviderLoginUrl(this SPIisSettings settings, string targetProvider) {
      if (string.IsNullOrEmpty(targetProvider))
        return null;
      SPAuthenticationProvider provider = settings.GetClaimsAuthenticationProvider(targetProvider);
      if (provider == null)
        return null;
      return provider.AuthenticationRedirectionUrl;
    }

    internal static Uri GetClaimsAuthenticationLoginRedirectionUrl(this SPIisSettings settings, bool skipRedirectionPage, bool skipMultilogonPage) {
      return GetClaimsAuthenticationLoginRedirectionUrl(settings, skipRedirectionPage, skipMultilogonPage, string.Empty);
    }
    internal static Uri GetClaimsAuthenticationLoginRedirectionUrl(this SPIisSettings settings, bool skipRedirectionPage, bool skipMultilogonPage, string multilogonPageUrl) {
      if (!settings.UseClaimsAuthentication)
        throw new InvalidOperationException("You must have claims authentication enabled to use this method.");
      if (string.IsNullOrEmpty(multilogonPageUrl))
        multilogonPageUrl = CommonLoginUrls.MultiLogin;
      Uri claimsAuthenticationRedirectionUrl = null;
      if (!skipRedirectionPage)
        claimsAuthenticationRedirectionUrl = settings.ClaimsAuthenticationRedirectionUrl;
      Collection<SPAuthenticationProvider> claimsAuthenticationProviders = settings.ClaimsAuthenticationProviders as Collection<SPAuthenticationProvider>;
      if ((null == claimsAuthenticationRedirectionUrl) && (claimsAuthenticationProviders != null)) {
        if (claimsAuthenticationProviders.Count == 1) {
          return claimsAuthenticationProviders[0].AuthenticationRedirectionUrl;
        }
        if (!skipMultilogonPage) {
          claimsAuthenticationRedirectionUrl = new Uri(multilogonPageUrl, UriKind.Relative);
        }
      }
      return claimsAuthenticationRedirectionUrl;
    }


  }

  public enum AuthenticationProviderSearchProperty {
    Any,
    Name,
    DisplayName,
    ClaimProviderName
  }

}
