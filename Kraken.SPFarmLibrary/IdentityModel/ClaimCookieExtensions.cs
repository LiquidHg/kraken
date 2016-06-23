using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Kraken.SharePoint.IdentityModel {

  public static class ClaimCookieExtensions {

    public static void DestroyCookie(this HttpContext context, string cookieName) {
      if (context.Response.Cookies[cookieName] != null) {
        context.Response.Cookies[cookieName].Expires = DateTime.Now.AddYears(-20);
        context.Response.Cookies.Remove(cookieName);
      }
      if (context.Request.Cookies[cookieName] != null) {
        context.Request.Cookies[cookieName].Expires = DateTime.Now.AddYears(-20);
        context.Request.Cookies.Remove(cookieName);
      }
    }

    public static void DestroySharePointSTSCookies(this HttpContext context) {
      context.DestroySharePointSTSCookies(SPSTS_FEDAUTH_COOKIE);
    }
    public static void DestroySharePointSTSCookies(this HttpContext context, string cookieName) {
      if (string.IsNullOrEmpty(cookieName))
        cookieName = SPSTS_FEDAUTH_COOKIE;
      DestroyCookie(context, cookieName);
    }

    public static void DestroyADFSCookies(this HttpContext context, bool signOut, bool resetRealmAffinity) {
      if (signOut) {
        DestroyCookie(context, ADFS_FEDAUTH_COOKIE);
        DestroyCookie(context, ADFS_LOOP_COOKIE);
      }
      if (resetRealmAffinity)
        DestroyCookie(context, ADFS_LSREALM_COOKIE);
    }

    /// <summary>
    /// It is important to note this can be changed in web.config
    /// </summary>
    public const string SPSTS_FEDAUTH_COOKIE = "LSRealm";

    public const string ADFS_LSREALM_COOKIE = "LSRealm";
    public const string ADFS_LOOP_COOKIE = "MSISLoopDetection";
    public const string ADFS_FEDAUTH_COOKIE = "_WebSsoAuth";

  }

}
