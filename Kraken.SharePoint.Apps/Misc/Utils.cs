using Kraken.SharePoint.Apps.Models;
//using ChimeraAzure.Models;
//using Microsoft.IdentityModel.S2S.Tokens;
using Microsoft.Owin;
using Microsoft.SharePoint.Client.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Kraken.SharePoint.Apps
{
    public class Utils
    {




        public static string GetFrom(IOwinRequest request)
        {
            if (request.Method != "POST")
                return null;
            if (request.ContentType != "application/x-www-form-urlencoded")
                return null;

            try
            {
                var Form = Task.Run((Func<Task<IFormCollection>>)request.ReadFormAsync).Result;
                string[] paramNames = { "AppContext", "AppContextToken", "AccessToken", "SPAppToken" };
                foreach (string paramName in paramNames)
                {
                    if (!string.IsNullOrEmpty(Form[paramName]))
                    {
                        return Form[paramName];
                    }
                    if (!string.IsNullOrEmpty(request.Query[paramName]))
                    {
                        return request.Query[paramName];
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static string ToString(IOwinRequest request)
        {
            var requestOrException = new StringBuilder();
            try
            {
                requestOrException.AppendLine(string.Format("{0} {1} {2}", request.Method, request.Uri, request.Protocol ?? string.Empty));
                foreach (var header in request.Headers)
                {
                    foreach (var value in header.Value)
                    {
                        requestOrException.AppendLine(string.Format("{0}: {1}", header.Key, value));
                    }
                }

                requestOrException.AppendLine();
                requestOrException.AppendLine();

                var content = new byte[request.Body.Length];
                request.Body.Seek(0, System.IO.SeekOrigin.Begin);
                request.Body.Read(content, 0, (int)request.Body.Length);
                requestOrException.Append(Encoding.UTF8.GetString(content));
            }
            catch (Exception dumpEx)
            {
                requestOrException.Append(dumpEx.ToString());
            }
            return requestOrException.ToString();
        }
    }
}