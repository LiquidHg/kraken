using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.IdentityModel {

  public static class AuthUrlTools {

    /// <summary>
    /// Checks a url query string to ensure that ReturnUrl is included.
    /// </summary>
    /// <param name="queryString"></param>
    /// <returns></returns>
    public static string EnsureReturnUrl(string queryString, string returnUrl) {
      if (string.IsNullOrEmpty(queryString) || !queryString.Contains("ReturnUrl=")) {
        queryString += (string.IsNullOrEmpty(queryString) ? "?" : "&") + "ReturnUrl=/";
      }
      return queryString;
    }
    public static string EnsureReturnUrl(string queryString) {
      return EnsureReturnUrl(queryString, "/");
    }

    // copied from SPUtility because it was marked internal
    private const string SPGlobal_strReturnUrl = "ReturnUrl";
    internal static string EnsureUrlSkipsFormsAuthModuleRedirection(string url, bool urlIsQueryStringOnly) {
      if (!url.Contains(SPGlobal_strReturnUrl + "=")) {
        if (urlIsQueryStringOnly) {
          url = url + (string.IsNullOrEmpty(url) ? "" : "&");
        } else {
          url = url + ((url.IndexOf('?') == -1) ? "?" : "&");
        }
        url = url + SPGlobal_strReturnUrl + "=";
      }
      return url;
    }
    // end copied internal

  }

  public static class CommonLoginUrls {
    public readonly static string MultiLogin = "/_login/default.aspx";
    public readonly static string ClaimsLogin = "/_trust/default.aspx";
    public readonly static string WindowsLogin = "/_windows/default.aspx"; // "/_layouts/Authenticate.aspx";
    public readonly static string FormsLogin = "/_forms/default.aspx";
  }

}
