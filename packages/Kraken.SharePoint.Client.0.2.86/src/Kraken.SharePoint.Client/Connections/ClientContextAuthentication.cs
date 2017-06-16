using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using Microsoft.SharePoint.Client;
using System.Diagnostics;
using System.Net;

namespace Kraken.SharePoint.Client.Connections
{
    // http://www.sharepoint-reference.com/Blog/Lists/Posts/Post.aspx?ID=34

    public class ClientContextAuthentication
    {
        private static object cookiesSyncLock = new object();

        private static CookieContainer cookies;

        public static void Configure(ClientContext ctx, ClientAuthenticationType authType)
        {
            if (authType.Equals(ClientAuthenticationType.SharePointClaims))
            {
                var baseSiteUrl = ctx.Url;

                // Configure anonymous authentication, because we will use FedAuth cookie instead
                ctx.AuthenticationMode = ClientAuthenticationMode.Anonymous;

                // Register an anonymous delegate to the ExecutingWebRequest event handler
                ctx.ExecutingWebRequest += new EventHandler<WebRequestEventArgs>((s, e) =>
                {

                    // If we do not have a cookies variable, which will be a shared instance of a CookieContainer 
                    if (null == cookies)
                    {
                        lock (cookiesSyncLock)
                        {
                            if (null == cookies)
                            {
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
                                if (null != response)
                                {
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
            }
        }
    }
}
