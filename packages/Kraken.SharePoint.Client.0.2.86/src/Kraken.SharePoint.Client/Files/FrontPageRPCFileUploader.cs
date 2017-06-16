using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Specialized;
using System.Net;
using Microsoft.SharePoint.Client;
using Kraken.SharePoint.Client.Helpers.FPRPC;
using Kraken.SharePoint.Client.Helpers;

namespace Kraken.SharePoint.Client.Files
{
    public class FrontPageRPCFileUploader : BaseFileUploader
    {
        public FrontPageRPCFileUploader(FileTransferParams prms)
            : base(prms)
        {
        }

        protected override File DoUpload(LocalFileOrFolder f, Folder targetFolder)
        {
          if (!f.Exists)
            throw new Exception("Target file doesn't exist. Can't continue.");
          if (f.IsFolder)
            throw new Exception("Target is a folder and should be a file. Can't continue.");
          string fileUrl = string.Format("{0}/{1}", targetFolder.ServerRelativeUrl, f.FileName);
            string fullFileUrl = Utils.MakeFullUrl(Context, fileUrl);

            var frontPageService = new FrontPageRPC(Context);

            int estTimeOut = (int)GetEstimateUploadTime(f.FileInfo);
            if (estTimeOut > Constants.Inrevals.DefaultHttpRequestTimeout)
            {
                Trace.TraceInfo("Set request timeout: '{0}'...", estTimeOut);
                frontPageService.RequestTimeout = estTimeOut;
            }

            WebUrl url = frontPageService.UrlToWebUrl(fullFileUrl);

            using (System.IO.FileStream fstream = new System.IO.FileStream(f.EntryPath, System.IO.FileMode.Open))
            {
                frontPageService.PutDocument(url, fstream);
            }

            File newFile = GetFile(fileUrl);
            return newFile;
        }

        protected override void DoDownload(LocalFileOrFolder f, File file)
        {
          if (f.IsFolder)
            throw new Exception("Target is a folder and should be a file or not exist. Can't continue.");
          var frontPageService = new FrontPageRPC(Context);
            string fullFileUrl = Utils.MakeFullUrl(Context, file.ServerRelativeUrl);

            using (System.IO.FileStream fstream = new System.IO.FileStream(f.EntryPath, System.IO.FileMode.Create))
            {
                WebUrl url = frontPageService.UrlToWebUrl(fullFileUrl);
                DocumentInfo d = frontPageService.GetDocument(url, fstream);
            }
        }
    }
}
