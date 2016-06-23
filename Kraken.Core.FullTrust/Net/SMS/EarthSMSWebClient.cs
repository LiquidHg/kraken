using System;
using System.Collections.Generic;
#if !DOTNET_V35
using System.Linq;
#endif
using System.Text;
using System.Collections.Specialized;
using System.Net;
using System.Web;
using System.IO;
using System.Security;

namespace Kraken.Net.SMS {

  public class EarthSMSWebClient {

    public string UserName { get; set; }
    
    /// <summary>
    /// Because EarthSMS sends this password in the clear, we recommend that you
    /// use a simple password that is not used anywhere else
    /// </summary>
    public string Password { get; set; }
    //public SecureString Password { get; set; }

    private const string serviceEndpointUrl = "http://earthsms.net/sms/EarthsmsSendSMSAPI.php";

    protected static Uri BuildUri(string baseUri, NameValueCollection queryParameters) {
      // is Uri.EscapeDataString() same as HttpUtility.UrlEncode?
#if DOTNET_V35
      List<string> keyValuePairs = new List<string>();
      foreach (string k in queryParameters.AllKeys) {
        keyValuePairs.Add(HttpUtility.UrlEncode(k) + "=" + HttpUtility.UrlEncode(queryParameters[k]));
      }
#else
      List<string> keyValuePairs = queryParameters.AllKeys.Select(k => HttpUtility.UrlEncode(k) + "=" + HttpUtility.UrlEncode(queryParameters[k])).ToList();
      //var keyValuePairs = queryParameters.AllKeys.Select(k => HttpUtility.UrlEncode(k) + "=" + HttpUtility.UrlEncode(queryParameters[k]));
#endif
      var qs = string.Join("&", keyValuePairs.ToArray());
      var builder = new UriBuilder(baseUri) { Query = qs };
      return builder.Uri;
    }

    public bool SendSMS(string mobileNumber, string message, string fromInfo) {
      NameValueCollection parameters = new NameValueCollection() {
        { "number", mobileNumber },
        { "text", mobileNumber },
        { "from", fromInfo },
        { "username", this.UserName },
        { "password", this.Password }
      };
      Uri requestUri = BuildUri(serviceEndpointUrl, parameters);
      HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(requestUri);
      string response = string.Empty;
      using (var webResponse = webRequest.GetResponse())
      using (var stream = webResponse.GetResponseStream()) {
        if (stream == null)
          return false;
        using (var textReader = new StreamReader(stream)) {
          response = textReader.ReadToEnd();
        }
      }
      // TODO check the response values
      return true;
    }

  }
}
