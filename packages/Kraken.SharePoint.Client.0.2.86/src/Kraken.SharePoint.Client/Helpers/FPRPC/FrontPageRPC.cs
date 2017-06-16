using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Web;
using Microsoft.SharePoint.Client;

namespace Kraken.SharePoint.Client.Helpers.FPRPC
{
    public class WebUrl
    {
        public string SiteUrl;
        public string FileUrl;
    }

    /// <summary>
    /// Client for calling Front Page Server Extensions.
    /// </summary>
    public class FrontPageRPC
    {
        private readonly ICredentials credentials;

        private static string serverExtensionsVersion;

        public int RequestTimeout { get; set; }

        private const string STR_AUTHOR_DLL_PATH = "/_vti_bin/_vti_aut/author.dll";
        private const string STR_VTI_RPC_PATH = "/_vti_bin/shtml.dll/_vti_rpc";
        private const string defaultServerVersion = "6.0.2.5614";

        private ClientContext Context { get; set; }

        public FrontPageRPC()
            : this(CredentialCache.DefaultCredentials)
        {
        }

        public FrontPageRPC(ICredentials credentials)
        {
            this.credentials = credentials;
        }

        public FrontPageRPC(ClientContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Has WSS parse the site vs. file/folder portion of a URL.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public WebUrl UrlToWebUrl(string uri)
        {
            WebUrl webUrl = new WebUrl();
            Uri aUri = new Uri(uri);

            NameValueCollection methodData = new NameValueCollection();
            methodData.Add("method", "url to web url:" + defaultServerVersion);
            methodData.Add("url", aUri.AbsolutePath);
            methodData.Add("flags", "0");

            string response = SendSimpleRequest(GetVtiRPC(aUri.GetLeftPart(UriPartial.Authority)), methodData);

            webUrl.SiteUrl = aUri.GetLeftPart(UriPartial.Authority) + GetReturnValue(response, "webUrl");
            webUrl.FileUrl = HttpUtility.UrlDecode(GetReturnValue(response, "fileUrl"));

            return webUrl;
        }

        public DocumentInfo GetDocument(WebUrl webUrl, Stream outStream)
        {
            NameValueCollection methodData = new NameValueCollection();

            methodData.Add("method", "get document:" + GetServerExtensionsVersion(webUrl));
            methodData.Add("service_name", "");
            methodData.Add("document_name", webUrl.FileUrl);
            methodData.Add("get_option", "none");
            methodData.Add("timeout", "10");

            using (Stream responseStream = StartWebRequest(GetAuthorURL(webUrl), methodData))
            {
                BufferedStream bufferedResponseStream = new BufferedStream(responseStream, 4096);

                string metaInfo = GetDocumentResponse(bufferedResponseStream);

#if !DOTNET_V35
                bufferedResponseStream.CopyTo(outStream);
                return ParseMetaInformationResponse(metaInfo);
#else
                throw new NotImplementedException("Sorry, but an alternative for bufferedResponseStream.CopyTo(stream) has not been developed for .NET 3.5/SP2010 CSOM.");
#endif

            }
        }

        private static string GetDocumentResponse(Stream responseStream)
        {
            if (null == responseStream)
            {
                throw new ArgumentException("responseStream is null");
            }

            string metaInfo = ExtractResponsePreamble(responseStream);

            if (string.IsNullOrEmpty(metaInfo))
            {
                throw new FrontPageRPCException("unable to parse responseData");
            }

            CheckForInternalErrorMessage(metaInfo);
            CheckForSuccessMessage(metaInfo);

            return metaInfo;
        }

        public string GetDocumentMetaInfoRaw(WebUrl webUrl)
        {
            NameValueCollection methodData = new NameValueCollection();

            methodData.Add("method", "getDocsMetaInfo:" + GetServerExtensionsVersion(webUrl));
            methodData.Add("service_name", "");
            methodData.Add("listHiddenDocs", "true");
            methodData.Add("listLinkInfo", "false");
            methodData.Add("validateWelcomeNames", "false");
            methodData.Add("url_list", "[" + webUrl.FileUrl + "]");

            return SendSimpleRequest(GetAuthorURL(webUrl), methodData);
        }

        public DocumentInfo GetDocumentMetaInfo(WebUrl webUrl)
        {
            string response = GetDocumentMetaInfoRaw(webUrl);
            DocumentInfo docInfo = ParseMetaInformationResponse(response);

            return docInfo;
        }

        public string GetDocumentHash(WebUrl webUrl)
        {
            string response = GetDocumentMetaInfoRaw(webUrl);
            return HashUtil.GetHash(response);
        }

        public List<WebUrl> ListFolders(WebUrl webUrl)
        {
            NameValueCollection methodData = new NameValueCollection();

            methodData.Add("method", "list documents:" + GetServerExtensionsVersion(webUrl));
            methodData.Add("service_name", "/");
            methodData.Add("listHiddenDocs", "true");
            methodData.Add("listExplorerDocs", "false");
            methodData.Add("listRecurse", "false");
            methodData.Add("listFiles", "false");
            methodData.Add("listFolders", "true");
            methodData.Add("listLinkInfo", "true");
            methodData.Add("listIncludeParent", "false");
            methodData.Add("listDerived", "false");
            methodData.Add("listBorders", "false");
            methodData.Add("listChildWebs", "false");
            methodData.Add("initialUrl", webUrl.FileUrl);

            string response = SendSimpleRequest(GetAuthorURL(webUrl), methodData);

            return ParseFileList(webUrl.SiteUrl, response, "url");
        }

        public string ListWebs(WebUrl webUrl)
        {
            NameValueCollection methodData = new NameValueCollection();

            methodData.Add("method", "get manifest:" + GetServerExtensionsVersion(webUrl));
            methodData.Add("service_name", "/");
            methodData.Add("options", "structure,files,userlists,list_data,globallists,subscriptions, discussions,userinfo,webparts,security,nontemplatizable_data");            
//            methodData.Add("options", "[structure]");            
            return SendSimpleRequest(GetAuthorURL(webUrl), methodData);            
//            return ParseWebList(webUrl.SiteUrl, response, "url");
        }

        public List<WebUrl> ListDocuments(WebUrl webUrl, bool recursive)
        {
            NameValueCollection methodData = new NameValueCollection();

            methodData.Add("method", "list documents:" + GetServerExtensionsVersion(webUrl));
            methodData.Add("service_name", "/");
            methodData.Add("listHiddenDocs", "true");
            methodData.Add("listExplorerDocs", "false");
            methodData.Add("listRecurse", recursive ? "true" : "false");
            methodData.Add("listFiles", "true");
            methodData.Add("listFolders", "false");
            methodData.Add("listLinkInfo", "false");
            methodData.Add("listIncludeParent", "false");
            methodData.Add("listDerived", "false");
            methodData.Add("listBorders", "false");
            methodData.Add("listChildWebs", "true");
            methodData.Add("initialUrl", webUrl.FileUrl);

            string response = SendSimpleRequest(GetAuthorURL(webUrl), methodData);

            return ParseFileList(webUrl.SiteUrl, response, "document_name");
        }

        private static List<WebUrl> ParseFileList(string siteUrl, string responseData, string attributeName)
        {
            Regex fileMatchRegEx = new Regex(@"\<li\>" + attributeName + @"=(?<name>.*?)\n\<li\>", RegexOptions.Compiled | RegexOptions.Singleline);

            List<WebUrl> aRet = new List<WebUrl>();
            MatchCollection fileInfoMatches = fileMatchRegEx.Matches(responseData);
            foreach (Match m in fileInfoMatches)
            {
                string fileUrl = HttpUtility.UrlDecode(DecodeString(m.Groups["name"].Value));
                aRet.Add(new WebUrl { SiteUrl = siteUrl , FileUrl = fileUrl});
            }
            return aRet;
        }

        private static string DecodeString(string source)
        {
            if (!string.IsNullOrEmpty(source))
            {
                Regex rg = new Regex("&#([0-9]{1,3});&#([0-9]{1,3});");

                foreach (Match match in rg.Matches(source))
                {
                    byte[] bytes = new[] { byte.Parse(match.Groups[1].Value), byte.Parse(match.Groups[2].Value) };
                    source = source.Replace(match.Value, Encoding.UTF8.GetString(bytes));
                }

                source = HttpUtility.HtmlDecode(source);
            }

            return source;
        }

        public void CreateDirectory(WebUrl webUrl)
        {
            NameValueCollection methodData = new NameValueCollection();
            methodData.Add("method", "create url-directories:" + GetServerExtensionsVersion(webUrl));
            methodData.Add("service_name", "/");
            methodData.Add("urldirs", "[[url=" + webUrl.FileUrl + "]]");

            string response = SendSimpleRequest(GetAuthorURL(webUrl), methodData);
            CheckForSuccessMessage(response);
        }

        public string GetServerExtensionsVersion(WebUrl siteUrl)
        {
            if (null == serverExtensionsVersion)
            {
                NameValueCollection methodData = new NameValueCollection();

                methodData.Add("method", "server version:" + defaultServerVersion);
                methodData.Add("service_name", "/");

                string responseData = SendSimpleRequest(GetAuthorURL(siteUrl), methodData);

                serverExtensionsVersion = ExtractServerExtensionsVersion(responseData);
            }

            return serverExtensionsVersion;
        }

        public void CheckInDocument(WebUrl webUrl, string comment)
        {
            NameValueCollection methodData = new NameValueCollection();

            methodData.Add("method", "checkin document: " + GetServerExtensionsVersion(webUrl));
            methodData.Add("service_name", "/");
            methodData.Add("document_name", webUrl.FileUrl);
            methodData.Add("comment", comment);
            methodData.Add("keep_checked_out", "false");

            SendSimpleRequest(GetAuthorURL(webUrl), methodData);
        }

        public void CheckOutDocument(WebUrl webUrl)
        {
            NameValueCollection methodData = new NameValueCollection();

            methodData.Add("method", "checkout document: " + GetServerExtensionsVersion(webUrl));
            methodData.Add("service_name", "/");
            methodData.Add("document_name", webUrl.FileUrl);
            methodData.Add("force", "0");
            methodData.Add("timeout", "0");

            SendSimpleRequest(GetAuthorURL(webUrl), methodData);
        }

        public void UnCheckOutDocument(WebUrl webUrl)
        {
            NameValueCollection methodData = new NameValueCollection();
 
            methodData.Add("method", "uncheckout document: " + GetServerExtensionsVersion(webUrl));
            methodData.Add("service_name","/");
            methodData.Add("document_name", webUrl.FileUrl );
            methodData.Add("force", "false");

            SendSimpleRequest(GetAuthorURL(webUrl), methodData);
        }

        public void PutDocument(WebUrl webUrl, Stream file) 
        {
            PutDocument(webUrl, file, null);
        }

        public void PutDocument(WebUrl webUrl, Stream file, DocumentPropertyCollection properties)
        {
            NameValueCollection methodData = new NameValueCollection();

            methodData.Add("method", "put document:" + GetServerExtensionsVersion(webUrl));
            methodData.Add("service_name", "");
            methodData.Add("put_option", "overwrite,createdir,migrationsemantics");
            methodData.Add("keep_checked_out", "false");

            using (Stream responseStream =
                StartWebRequest(
                    GetAuthorURL(webUrl),
                    reqStream => WriteDocumentData(reqStream, webUrl.FileUrl, file, properties, methodData)
                    )
                )
            {
                string response = GetResponseString(responseStream);
                CheckForInternalErrorMessage(response);
                CheckForSuccessMessage(response);
            }
        }

        public void SetDocumentMetaInfo(WebUrl webUrl, DocumentInfo docInfo)
        {
            NameValueCollection methodData = new NameValueCollection();

            methodData.Add("method", "setDocsMetaInfo:" + GetServerExtensionsVersion(webUrl));
            methodData.Add("service_name", "");
            methodData.Add("listHiddenDocs", "true");
            methodData.Add("listLinkInfo", "true");
            methodData.Add("url_list", "[" + webUrl.FileUrl + "]");
            methodData.Add("metaInfoList", "[" + docInfo.GetMetaInfoList(false) + "]");

            string responseData = SendSimpleRequest(GetAuthorURL(webUrl), methodData);

            if (!SetMetaDataResponseSuccess(responseData))
            {
                throw new FrontPageRPCException("SetDocumentMetaInfo failed", webUrl.FileUrl);
            }
        }

        private static bool SetMetaDataResponseSuccess(string responseString)
        {
            return responseString.IndexOf("method") > -1 && -1 == responseString.IndexOf("failedUrls");
        }

        private static void AddCollectionData(TextWriter tw, NameValueCollection data) 
        {			
            string separator = string.Empty;

            foreach(string key in data) 
            {
                tw.Write(separator);
                tw.Write("{0}={1}", key, HttpUtility.UrlEncode(data[key]));
                separator = "&";
            }
        }
        private static void CheckForInternalErrorMessage(string response)
        {
            string message = DecodeString(GetReturnValue(response, "msg"));
            if (!string.IsNullOrEmpty(message))
            {
                throw new FrontPageRPCException(message);
            }
        }

        private static void CheckForSuccessMessage(string response)
        {
            string message = GetReturnValue(response, "message");
            if (null == message || !message.StartsWith("successfully"))
            {
                throw new FrontPageRPCException("Failed to perform operation.");
            }
        }

        /// <summary>
        /// Extract the &lt;html&gt;&lt;/html&gt; preamble in the response stream.
        /// </summary>
        /// <remarks>
        /// We're using the ReadStreamLine method instead of wrapping the stream in a StreamReader because we only
        /// want to consume up to the end of the preamble; the remaining stream is other, potentially binary, data.
        /// </remarks>
        /// <param name="stream"></param>
        /// <returns></returns>
        private static string ExtractResponsePreamble(Stream stream) 
        {
            // locate <html></html> response and extract.
            StringBuilder responseData = new StringBuilder();

            string line = ReadStreamLine(stream);

            if (line.StartsWith("<html>")) 
            {
                responseData.Append(line);

                while (!line.EndsWith("</html>\n"))
                {
                    line = ReadStreamLine(stream);
                    responseData.Append(line);
                } 
            }
            return responseData.ToString();
        }

        private static string ReadStreamLine(Stream stream)
        {
            StringBuilder result = new StringBuilder();
            int bytesRead = 0;
            for (byte[] buffer = new byte[1]; stream.Read(buffer, 0, 1) > 0; )
            {
                bytesRead++;
                result.Append((char)buffer[0]);
                if (buffer[0] == '\n')
                {
                    break;
                }
            }
            if (bytesRead == 0)
            {
                throw new FrontPageRPCException("unexpected end of response");
            }

            return result.ToString();
        }

        private static string GetResponseString(Stream responseStream)
        {
            StreamReader sr = new StreamReader(responseStream, Encoding.UTF8);
            return sr.ReadToEnd();
        }

        private static string GetAuthorURL(WebUrl url) 
        {
            return url.SiteUrl + STR_AUTHOR_DLL_PATH;
        }

        private static string GetVtiRPC(string url)
        {
            return url + STR_VTI_RPC_PATH;
        }

        private static string GetReturnValue(string response, string key)
        {
            key = key.TrimEnd('=') + "=";

            int startPos = response.IndexOf(key);
            if (-1 == startPos)
            {
                return null;
            }

            startPos += key.Length;
            int endPos = response.IndexOf("\n", startPos);

            return response.Substring(startPos, endPos - startPos);
        }

        public static DocumentInfo ParseMetaInformationResponse(string responseData)
        {
            Regex proprtyInfoRegEx = new Regex(@"\<li\>(?<propName>.*?)\n\<li\>(?<propType>.*?)\|(?<propValue>.*?)\n", RegexOptions.Compiled | RegexOptions.Multiline);
            Regex metaInfoMatchRegEx = new Regex(@"\<li\>meta_info=\n\<ul\>\n(?<metaInfo>.*?)\<\/ul\>", RegexOptions.Compiled | RegexOptions.Singleline);
            Regex nameAndTypeRegeEx = new Regex(@"\<li\>(?<type>(document_name|url)?)=(?<name>.*?)\n", RegexOptions.Compiled | RegexOptions.Multiline);

            DocumentInfo docInfo = new DocumentInfo();

            Match nameAndType = nameAndTypeRegeEx.Match(responseData);
            Match metaInfoMatch = metaInfoMatchRegEx.Match(responseData);

            if (metaInfoMatch.Success && nameAndType.Success) 
            {
                docInfo.IsFolder = nameAndType.Groups["type"].Value == "url";
                docInfo.DestinationFileName = nameAndType.Groups["name"].Value;

                MatchCollection propMatches = proprtyInfoRegEx.Matches(metaInfoMatch.Value);
                foreach(Match propMatch in propMatches) 
                {
                    DocumentProperty prop = new DocumentProperty(
                        propMatch.Groups["propName"].Value, 
                        propMatch.Groups["propType"].Value,
                        HttpUtility.UrlDecode(DecodeString(propMatch.Groups["propValue"].Value))
                        );

                    docInfo.Properties.Add(prop);
                }
            }

            return docInfo;
        }

        //public HttpWebRequest ExternalRequest { get; set; }

        private Stream StartWebRequest(string url, Action<Stream> writeData)
        {
            //Debug.WriteLine("URL: " + url);
            //HttpWebRequest request = ExternalRequest ?? (HttpWebRequest)WebRequest.Create(url);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            SetupRequestCredential(request);
            //request.Credentials = credentials;
            request.Method = "POST";

            if (RequestTimeout > Constants.Inrevals.DefaultHttpRequestTimeout)
            {
                request.Timeout = RequestTimeout;
            }
            request.KeepAlive = true;
            request.PreAuthenticate = true;
            request.ContentType = "application/x-www-form-urlencoded";
            //request.ContentType = "application/x-vermeer-urlencoded";
            request.Headers.Add("X-Vermeer-Content-Type", "application/x-www-form-urlencoded");
            //request.Headers.Add("X-Vermeer-Content-Type", "application/x-vermeer-urlencoded");

            using (Stream reqStream = request.GetRequestStream())
            {
                writeData(reqStream);
                reqStream.Flush();
            }

            WebResponse response = request.GetResponse();

            return response.GetResponseStream();
        }

        private void SetupRequestCredential(HttpWebRequest request)
        {
            ClientContext.SetupRequestCredential(Context.Web.Context, request);
        }

        private Stream StartWebRequest(string url, NameValueCollection methodData) 
        {
            return StartWebRequest(url, reqStream =>
                                            {
                                                StreamWriter sw = new StreamWriter(reqStream);
                                                AddCollectionData(sw, methodData);
                                                sw.Flush();
                                            });
        }

        private string SendSimpleRequest(string fullUrl, NameValueCollection methodData)
        {
            using (Stream responseStream = StartWebRequest(fullUrl, methodData))
            {
                string response = GetResponseString(responseStream);

                CheckForInternalErrorMessage(response);

                return response;
            }
        }

        private static void WriteDocumentData(Stream stream, string destinationFileName, Stream file, DocumentPropertyCollection properties, NameValueCollection methodData)
        {
            StreamWriter sw = new StreamWriter(stream);

            WindowsIdentity currentUser = WindowsIdentity.GetCurrent();

            if (null == currentUser)
            {
                throw new FrontPageRPCException("unable to get current user from context");
            }

            DocumentInfo docInfo = new DocumentInfo
                                       {
                                           ModifiedBy = currentUser.Name,
                                           ModifiedDate = DateTime.Now.ToUniversalTime(),
                                           DestinationFileName = destinationFileName,
                                           Title = Path.GetFileName(destinationFileName)
                                       };
            if (null != properties)
            {
                docInfo.Properties.Add(properties);
            }

            AddCollectionData(sw, methodData);
            docInfo.WriteDocumentData(sw);
            sw.Flush();

#if !DOTNET_V35
            file.CopyTo(stream);
            stream.Flush();
#else
            throw new NotImplementedException("Sorry, but an alternative for file.CopyTo(stream) has not been developed for .NET 3.5/SP2010 CSOM.");
#endif
        }


        //Helper method for GetServerExtensionsVersion()
        //The response from the server is in the format :
        //<html><head><title>vermeer RPC packet</title></head>\n<body>\n
        //<p>method=server version:6.0.0.0\n
        //<p>server version=\n<ul>\n
        //<li>major ver=6\n <li>minor ver=0\n<li>phase ver=2\n<li>ver incr=5528\n
        //</ul>\n<p>source control=1\n
        //</body>\n</html>\n
        private static string ExtractServerExtensionsVersion(string response)
        {
            int index = 0;
            string majorVer = GetVersionComponent(response, "major ver", ref index);
            string minorVer = GetVersionComponent(response, "minor ver", ref index);
            string phaseVer = GetVersionComponent(response, "phase ver", ref index);
            string verIncr = GetVersionComponent(response, "ver incr", ref index);

            return string.Format("{0}.{1}.{2}.{3}", majorVer, minorVer, phaseVer, verIncr);
        }

        private static string GetVersionComponent(string text, string part, ref int index)
        {
            index = text.IndexOf(part, index);
            if (-1 == index)
            {
                throw new FrontPageRPCException("Could not retrieve the server extension version");
            }
            int startIndex = index + part.Length + 1;
            int endIndex = text.IndexOf("<", index);
            return text.Substring(startIndex, endIndex - startIndex).Trim();
        }
    }
}