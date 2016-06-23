using System;
using System.Net;
using System.Web;
using System.Text.RegularExpressions;

namespace Kraken.Net.IpNetworking {

  public class IPTools {

    public static string GetIP4Address() {
      return GetIP4Address(HttpContext.Current, true);
    }
    public static string GetIP4Address(HttpContext context, bool doReplace) {
      string IP4Address = String.Empty;

      foreach (IPAddress ipa in Dns.GetHostAddresses(context.Request.UserHostAddress)) {
        if (ipa.AddressFamily.ToString() == "InterNetwork") {
          IP4Address = ipa.ToString();
          break;
        }
      }

      if (IP4Address != String.Empty) {
        return IP4Address;
      }

      foreach (IPAddress ipa in Dns.GetHostAddresses(Dns.GetHostName())) {
        if (ipa.AddressFamily.ToString() == "InterNetwork") {
          IP4Address = ipa.ToString();
          break;
        }
      }
      if (doReplace)
        IP4Address = Regex.Replace(IP4Address, @"^(?<Prefix>(\d{1,3}\.){3})\d{1,3}$", "${Prefix}*");
      return IP4Address;
    }
  }
}