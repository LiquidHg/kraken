using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Microsoft.SharePoint.Client;
using Kraken.SharePoint.Client.Helpers;

namespace Kraken.SharePoint.Client.Files {
  public class WebDAVFileUploader : BaseFileUploader {
    public WebDAVFileUploader(FileTransferParams prms)
        : base(prms) {
    }

    protected override File DoUpload(LocalFileOrFolder f, Folder targetFolder, ITransferTimeEstimator estimator) {
      if (!f.Exists)
        throw new Exception("Target file doesn't exist. Can't continue.");
      if (f.IsFolder)
        throw new Exception("Target is a folder and should be a file. Can't continue.");
      string fileUrl = string.Format("{0}/{1}", targetFolder.ServerRelativeUrl, f.FileName);
      using (System.IO.FileStream fstream = System.IO.File.OpenRead(f.EntryPath)) {
        DoUploadImpl(fileUrl, fstream, estimator);
      }

      File newFile = GetFile(fileUrl);
      return newFile;
    }

    protected virtual void DoUploadImpl(string fileUrl, System.IO.FileStream fstream, ITransferTimeEstimator estimator) {
      File.SaveBinaryDirect(Context, fileUrl, fstream, true);
    }

    protected override void DoDownload(LocalFileOrFolder f, File file, ITransferTimeEstimator estimator) {
      DownloadFromOpenBinaryDirect(f, file, estimator);
    }
  }

  public class WebDAVTimeoutFileUploader : WebDAVFileUploader {
    private enum SaveBinaryCheckMode {
      ETag,
      Overwrite
    }

    public WebDAVTimeoutFileUploader(FileTransferParams prms)
        : base(prms) {
    }

    protected override void DoUploadImpl(string fileUrl, System.IO.FileStream fstream, ITransferTimeEstimator estimator) {
      int estTimeOut = (int)estimator.TimeOutTicks; // (int)GetEstimateUploadTime(Params.LocalPath.FileInfo);
      SaveBinary(Context, estTimeOut, fileUrl, fstream, null, Params.OverwriteFiles, SaveBinaryCheckMode.Overwrite);
    }

    private void SaveBinary(ClientContext context, int timeOut, string serverRelativeUrl, System.IO.Stream stream, string etag, bool overwriteIfExists, SaveBinaryCheckMode checkMode) {
      if (context == null) {
        throw new ArgumentNullException("context");
      }
      if (context.HasPendingRequest) {
        throw new ClientRequestException(Microsoft.SharePoint.Client.Resources.GetString("NoDirectHttpRequest"));
      }
      string requestUrl = Utils.MakeFullUrl(context, serverRelativeUrl);
      WebRequestExecutor webRequestExecutor = context.WebRequestExecutorFactory.CreateWebRequestExecutor(context, requestUrl);
      if (timeOut > Constants.Inrevals.DefaultHttpRequestTimeout) {
        Trace.TraceInfo("Set request timeout: '{0}'...", timeOut);
        webRequestExecutor.WebRequest.Timeout = timeOut;
      }
      webRequestExecutor.RequestMethod = "PUT";
      if (checkMode == SaveBinaryCheckMode.ETag) {
        if (!string.IsNullOrEmpty(etag)) {
          webRequestExecutor.RequestHeaders[HttpRequestHeader.IfMatch] = etag;
        }
      } else if (!overwriteIfExists) {
        webRequestExecutor.RequestHeaders[HttpRequestHeader.IfNoneMatch] = "*";
      }

      MyOnExecutingWebRequest(context, new WebRequestEventArgs(webRequestExecutor));
      System.IO.Stream requestStream = webRequestExecutor.GetRequestStream();
      byte[] buffer = new byte[0x400];
      int count = 0;
      while ((count = stream.Read(buffer, 0, 0x400)) > 0) {
        requestStream.Write(buffer, 0, count);
      }
      requestStream.Flush();
      requestStream.Close();
      try {
        webRequestExecutor.Execute();
        if ((webRequestExecutor.StatusCode != HttpStatusCode.Created) && (webRequestExecutor.StatusCode != HttpStatusCode.OK)) {
          throw new ClientRequestException(Microsoft.SharePoint.Client.Resources.GetString("RequestUnexpectedResponseWithContentTypeAndStatus", new object[] { webRequestExecutor.ResponseContentType, webRequestExecutor.StatusCode }));
        }
      } catch (WebException exception) {
        HttpWebResponse response = exception.Response as HttpWebResponse;
        if ((response == null) || (response.StatusCode != HttpStatusCode.PreconditionFailed)) {
          throw;
        }
        if (checkMode == SaveBinaryCheckMode.ETag) {
          throw new ClientRequestException(Microsoft.SharePoint.Client.Resources.GetString("ETagNotMatch"));
        }
        throw new ClientRequestException(Microsoft.SharePoint.Client.Resources.GetString("FileAlreadyExists"));
      }
    }

    private static void MyOnExecutingWebRequest(ClientContext context, WebRequestEventArgs args) {
      if ((args != null) && (args.WebRequestExecutor != null)) {
        if (args.WebRequestExecutor.WebRequest != null) {
          args.WebRequestExecutor.WebRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
        }
#if !DOTNET_V35
        if (!string.IsNullOrEmpty(context.TraceCorrelationId)) {
          args.WebRequestExecutor.RequestHeaders["SPResponseGuid"] = context.TraceCorrelationId;
        }
        if (!string.IsNullOrEmpty(context.ClientTag)) {
          args.WebRequestExecutor.RequestHeaders["X-ClientService-ClientTag"] = context.ClientTag;
        }
#endif
      }
    }
  }
}
