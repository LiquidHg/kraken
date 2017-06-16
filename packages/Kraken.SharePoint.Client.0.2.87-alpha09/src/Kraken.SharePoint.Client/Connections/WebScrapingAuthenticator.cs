using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
#if !DOTNET_V35
using System.Threading.Tasks;
#endif
using System.Web;
using Microsoft.SharePoint.Client.Utilities;

namespace Kraken.SharePoint.Client.Connections
{

  /// <summary>
  /// Based on AuthenticationHelper from https://github.com/kunaal2809/SharePointCustomDomainAuthentication
  /// More info on blog here http://kunalkapoor.in/index.php/2013/04/26/active-authentication-to-sharepoint-for-office-365-with-custom-domains/
  /// </summary>
    public class WebScrapingAuthenticator
    {
        string _rpsCookie = String.Empty;
        string _valueOfT = String.Empty;

        string[] MSOLAuthCookies = new string[]{ "WLOpt", "PPAuth", "MSPPre", "MSPCID", "RPSTAuthTime", "MSPVis", "MSPSoftVis", "MSPRequ", "MSPBack", "PPLState", "RPSTAuth", "MSPAuth", "MSPProf", "MH" };

        public CookieContainer GetFedAuthCookieViaScreenScrape(string username, string password, string hostUrl)
        {
          bool isBpos = IsBposMigration(hostUrl);
            string fedAuth = String.Empty;
            CookieContainer cookieContainer = null;
            GetFedAuth(username, password, hostUrl, isBpos, out fedAuth);
            if (!String.IsNullOrEmpty(fedAuth))
            {
                cookieContainer = new CookieContainer();
                Cookie fedAuthCookie = new Cookie("FedAuth", fedAuth)
                {
                    Path = "/",
                    HttpOnly = true,
                    Domain = GetUriHost(hostUrl)
                };
                cookieContainer.Add(fedAuthCookie);
            }
            return cookieContainer;
        }

        #region requests

        private const string userAgent2 = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.17 (KHTML, like Gecko) Chrome/24.0.1312.57 Safari/537.17";
        private const string userAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; WOW64; Trident/5.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; .NET4.0C; .NET4.0E; InfoPath.3)";
        private const string requestAccept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

        private bool MakeFirstRequestToSharePoint(string hostUrl, out HttpWebResponse response)
        {
            response = null;
            try
            {
              HttpWebRequest request = CreateRequest(hostUrl, false, false);
              request.KeepAlive = true;
              request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip,deflate,sdch");
              request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");
              request.Headers.Set(HttpRequestHeader.AcceptCharset, "ISO-8859-1,utf-8;q=0.7,*;q=0.3");
              response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception ex)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        private bool MakeSecondRequestToSharePoint(string username, string password, string requiredAuthUrl, out HttpWebResponse response) {
          response = null;
          try {
            HttpWebRequest request = CreateRequest(requiredAuthUrl, true, true);
            response = (HttpWebResponse)request.GetResponse();
          } catch (WebException e) {
            if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
            else return false;
          } catch (Exception ex) {
            if (response != null) response.Close();
            return false;
          }
          return true;
        }

        private bool MakeThirdRequestToSharePoint(string url, out HttpWebResponse response) { // string username, string password, 
          response = null;
          try {
            HttpWebRequest request = CreateRequest(url, true, true);
            response = (HttpWebResponse)request.GetResponse();
          } catch (WebException e) {
            if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
            else return false;
          } catch (Exception ex) {
            if (response != null) response.Close();
            return false;
          }
          return true;
        }

        private bool MakeFourthRequestToSharePoint(string username, string password, string postUrl, string referralUrl, string cookie, string ppftText, out HttpWebResponse response) {
          response = null;
          try {
            HttpWebRequest request = CreateRequest(postUrl, true, true);
            request.Accept = "application/x-ms-application, image/jpeg, application/xaml+xml, image/gif, image/pjpeg, application/x-ms-xbap, */*";
            request.Referer = referralUrl;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Set(HttpRequestHeader.CacheControl, "no-cache");
            request.Headers.Set(HttpRequestHeader.Cookie, cookie);
            request.Method = "POST";

            string postString = @"login=" + username + "&passwd=" + password + "&type=11&LoginOptions=3&NewUser=1&MEST=&PPSX=Passpo&PPFT=" + ppftText + "&idsbho=1&PwdPad=&sso=&n1=-1353307140000&n2=-1353307140000&n3=-1353307140000&n4=93653&n5=93653&n6=93653&n7=93653&n8=&n9=93653&n10=93653&n11=93653&n12=93688&n13=93688&n14=93688&n15=&n16=95728&n17=95729&n18=95798&i13=MSIE&i14=&i1=&i2=1&i3=24774&i4=&i12=1";
            byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(postString);
            request.ContentLength = postBytes.Length;
            Stream stream = request.GetRequestStream();
            stream.Write(postBytes, 0, postBytes.Length);
            stream.Close();

            response = (HttpWebResponse)request.GetResponse();
          } catch (WebException e) {
            if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
            else return false;
          } catch (Exception ex) {
            if (response != null) response.Close();
            return false;
          }

          return true;
        }

        private bool MakeFifthRequestToSharePoint(string url, string referer, string valueOfT, out HttpWebResponse response) {
          response = null;

          try {
            HttpWebRequest request = CreateRequest(url, true, true);
            request.Accept = "application/x-ms-application, image/jpeg, application/xaml+xml, image/gif, image/pjpeg, application/x-ms-xbap, */*";
            request.Referer = referer;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Set(HttpRequestHeader.CacheControl, "no-cache");

            request.Method = "POST";

            string postString = @"t=" + valueOfT;
            byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(postString);
            request.ContentLength = postBytes.Length;
            Stream stream = request.GetRequestStream();
            stream.Write(postBytes, 0, postBytes.Length);
            stream.Close();

            response = (HttpWebResponse)request.GetResponse();
          } catch (WebException e) {
            if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
            else return false;
          } catch (Exception ex) {
            if (response != null) response.Close();
            return false;
          }

          return true;
        }

        private bool MakeSixthRequestToSharePoint(string url, string valueOfT, out HttpWebResponse response) {
          response = null;

          try {
            HttpWebRequest request = CreateRequest(url, true, true);
            request.Accept = "application/x-ms-application, image/jpeg, application/xaml+xml, image/gif, image/pjpeg, application/x-ms-xbap, */*";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Set(HttpRequestHeader.Pragma, "no-cache");
            request.Headers.Set(HttpRequestHeader.Cookie, @"RpsContextCookie=" + _rpsCookie);

            request.Method = "POST";

            string postString = @"t=" + valueOfT;
            byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(postString);
            request.ContentLength = postBytes.Length;
            request.AllowAutoRedirect = false;
            Stream stream = request.GetRequestStream();
            stream.Write(postBytes, 0, postBytes.Length);
            stream.Close();

            response = (HttpWebResponse)request.GetResponse();
          } catch (WebException e) {
            if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
            else return false;
          } catch (Exception ex) {
            if (response != null) response.Close();
            return false;
          }
          return true;
        }

        private HttpWebRequest CreateRequest(string url, bool acceptAll, bool addAcceptHeaders, bool autoRedirect = false) {
          HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
          request.Accept = (acceptAll) ? "*/*" : requestAccept;
          if (addAcceptHeaders) {
            request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US"); // "en-US,en;q=0.8");
            request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate"); //"gzip,deflate,sdch");
          }
          request.AllowAutoRedirect = autoRedirect;
          request.UserAgent = userAgent;
          return request;
        }

        #endregion

        private void GetFedAuth(string username, string password, string hostUrl, bool isBpos, out string fedAuthCookie) {
          HttpWebResponse response;
          fedAuthCookie = null;
          if (MakeFirstRequestToSharePoint(hostUrl, out response)) {
            //Process the respone
            var authRequiredUrl = response.Headers[HttpResponseHeader.Location];
            if (String.IsNullOrEmpty(authRequiredUrl)) throw new Exception("The SharePoint url could not be resolved. Please verify the specified Office 365 Host.");
            //Move to Next Redirection with required Authentication Url.
            MoveToSecondRedirection(username, password, authRequiredUrl, isBpos, out fedAuthCookie);
            response.Close();
          }
        }

        private void MoveToSecondRedirection(string username, string password, string requiredAuthUrl, bool isBpos, out string fedAuthCookie)
        {
            HttpWebResponse response;
            fedAuthCookie = null;
            if (MakeSecondRequestToSharePoint(username, password, requiredAuthUrl, out response))
            {
                //Process the response.
                //Getting the value of Rps Context Cookie, to be used in further requests.
                _rpsCookie = response.Headers[HttpResponseHeader.SetCookie];
                _rpsCookie = _rpsCookie.Substring(_rpsCookie.IndexOf("RpsContextCookie=") + 17, _rpsCookie.IndexOf(";") - 17);
                string locationUrl = response.Headers[HttpResponseHeader.Location];
                if (!IsValidUri(locationUrl))
                    locationUrl = GetValidUri(requiredAuthUrl, locationUrl);
                if (String.IsNullOrEmpty(locationUrl)) throw new Exception("The SharePoint url could not be resolved. Please verify the specified Office 365 Host.");
                MoveToThirdRedirection(username, password, locationUrl, isBpos, out fedAuthCookie);
                response.Close();
            }
        }


        private void MoveToThirdRedirection(string username, string password, string url, bool isBpos, out string fedAuthCookie)
        {
            HttpWebResponse response;
            fedAuthCookie = null;
            if (MakeThirdRequestToSharePoint(url, out response)) // username, password, 
            {
                //if (isBpos)
                //    MoveToBposFourthRedirection(response, username, password, out fedAuthCookie);

                //Process the response.
                var cookie = response.Headers[HttpResponseHeader.SetCookie];
                cookie = cookie.ToString().Replace("path=/;", "");
                cookie = cookie.Replace("version=1", "");
                cookie = cookie.Replace(",", "");
                var referralUrl = response.ResponseUri;
                var responseBytes = Decompress(response.GetResponseStream());
                string htmlText = System.Text.ASCIIEncoding.ASCII.GetString(responseBytes);
                //Retrieving the required values from the HTML response. It is required for the next requests.
                string postUrl = GetPostUrl(htmlText);
                string ppft = GetPpft(htmlText);
                MoveToFourthRedirection(username, password, postUrl, referralUrl.ToString(), cookie.ToString(), ppft, out fedAuthCookie);
                response.Close();
            }
        }

        #region Custom Domain

        private void MoveToFourthRedirection(string username, string password, string postUrl, string referralUrl, string cookie, string ppftText, out string fedAuthCookie)
        {
            HttpWebResponse response;
            fedAuthCookie = null;
            if (MakeFourthRequestToSharePoint(username, password, postUrl, referralUrl, cookie, ppftText, out response))
            {
                //Process the response.
                var responseBytes = Decompress(response.GetResponseStream());
                string htmlText = System.Text.ASCIIEncoding.ASCII.GetString(responseBytes);
                string url;
                //Reading the HTML response to get the required values.
                GetFormAction(htmlText, out url, out _valueOfT);
                MoveToFifthRedirection(url, response.ResponseUri.ToString(), _valueOfT, out fedAuthCookie);
                response.Close();
            }
        }

        private void MoveToFifthRedirection(string url, string referer, string valueOfT, out string fedAuthCookie)
        {
            HttpWebResponse response;
            fedAuthCookie = null;
            if (MakeFifthRequestToSharePoint(url, referer, valueOfT, out response))
            {
                //Process the response.
                var responseBytes = Decompress(response.GetResponseStream());
                string htmlText = System.Text.ASCIIEncoding.ASCII.GetString(responseBytes);
                string postUrl, valueOfT_yetAgain;
                //reading the HTML response to retrieve the required values.
                GetFormAction(htmlText, out postUrl, out valueOfT_yetAgain);
                MoveToSixthRedirection(postUrl, _valueOfT, out fedAuthCookie);
                response.Close();
            }
        }


        private void MoveToSixthRedirection(string url, string valueOfT, out string fedAuthCookie)
        {
            HttpWebResponse response;
            fedAuthCookie = null;
            if (MakeSixthRequestToSharePoint(url, valueOfT, out response))
            {
                var cookieHeader = response.Headers[HttpResponseHeader.SetCookie];
                cookieHeader = cookieHeader.Substring(cookieHeader.IndexOf("FedAuth=") + 8);
                fedAuthCookie = cookieHeader.Substring(0, cookieHeader.IndexOf(';'));
                response.Close();
            }
        }


        #endregion

        #region Helper Methods

        byte[] Decompress(Stream gzip)
        {
            using (GZipStream stream = new GZipStream(gzip,
                                  CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        string GetPostUrl(string htmlText)
        {
            var url = String.Empty;
            var text = String.Empty;
            if (htmlText.IndexOf("srf_uPost='") > 0)
            {
                text = htmlText.Substring(htmlText.IndexOf("srf_uPost='") + 11, 500);
                url = text.Substring(0, text.IndexOf("'"));
            }
            else if (htmlText.IndexOf("method=\"post\"") > 0)
            {
                text = htmlText.Substring(htmlText.IndexOf("method=\"post\"") + 22, 500);
                //var actionText = text.Substring(text.IndexOf("action=\"") + 8, 400);
                url = text.Substring(0, text.IndexOf("\""));
            }
            return url;
        }

        string GetPpft(string htmlText)
        {
            string subText = htmlText.Substring(htmlText.IndexOf("PPFT"), 500);
            string value = subText.Substring(subText.IndexOf("value=\"") + 7, 400);
            string ppft = value.Substring(0, value.IndexOf("\""));
            return ppft;
        }

        void GetFormAction(string htmlText, out string url, out string valueOfT)
        {
            url = String.Empty;
            valueOfT = String.Empty;
            int formIndex = htmlText.IndexOf("<form") + 5;
            var subText = htmlText.Substring(formIndex, htmlText.Length - formIndex);
            int actionIndex = subText.IndexOf("action=\"") + 8;
            var actionText = subText.Substring(actionIndex, subText.Length - actionIndex);
            url = actionText.Substring(0, actionText.IndexOf("\""));
            int tIndex = actionText.IndexOf("name=\"t\"") + 8;
            var tText = actionText.Substring(tIndex, actionText.Length - tIndex);
            int valueIndex = tText.IndexOf("value=\"") + 7;
            var valueText = tText.Substring(valueIndex, tText.Length - valueIndex);
            valueOfT = valueText.Substring(0, valueText.IndexOf("\""));
            valueOfT = System.Web.HttpUtility.UrlEncode(valueOfT.Trim());
        }

        private bool IsValidUri(string uri)
        {
            try
            {
                new Uri(uri);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetValidUri(string previousValidUrl, string path)
        {
            try
            {
                var validUrl = new Uri(previousValidUrl);
                var urlString = String.Format("{0}://{1}{2}", validUrl.Scheme, validUrl.Host, path);
                return urlString;
            }
            catch
            {
                return String.Empty;
            }
        }

        private string GetUriHost(string requestUriString)
        {
            try
            {
                var uri = new Uri(requestUriString);
                return uri.Host;
            }
            catch
            {
                return String.Empty;
            }
        }

        #endregion

        public static bool IsBposMigration(string hostUrl) {
          hostUrl = hostUrl.ToLowerInvariant();
          if (hostUrl.Contains("sharepoint.microsoftonline.com") ||
              hostUrl.Contains("noam.microsoftonline.com") ||
              hostUrl.Contains("emea.microsoftonline.com") ||
              hostUrl.Contains("apac.microsoftonline.com"))
            return true;
          return false;
        }
    
    }
}
