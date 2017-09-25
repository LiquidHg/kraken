using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Kraken.SharePoint.Client.Helpers;

namespace Kraken.SharePoint.Client.Files
{
    public class FancyFileUploader : BaseFileUploader
    {
      public FancyFileUploader(FileTransferParams prms)
            : base(prms)
        {
        }

        protected override File DoUpload(LocalFileOrFolder f, Folder targetFolder, ITransferTimeEstimator estimator)
        {
          if (!f.Exists)
            throw new Exception("Target file doesn't exist. Can't continue.");
          if (f.IsFolder)
            throw new Exception("Target is a folder and should be a file. Can't continue.");
          File newFile = targetFolder.UploadFile(
            //Params.ParentList, // no longer needed
            f.EntryPath, 
            "MetadataSourceURL", // TODO make this a param?
            Params.OverwriteFiles,
            string.Empty, // TODO Content Type ID should be supported?
            estimator,
            this.Trace);
          return newFile;
        }

        protected override void DoDownload(LocalFileOrFolder f, File file, ITransferTimeEstimator estimator)
        {
          if (f.IsFolder)
            throw new Exception("Target is a folder and should be a file or not exist. Can't continue.");
#if !DOTNET_V35
            if (Context.IsSP2013AndUp())
            {
                ClientResult<System.IO.Stream> data = file.OpenBinaryStream();

                Context.Load(file);
                Context.ExecuteQuery();

                int position = 1;
                int bufferSize = 200000;
                Byte[] readBuffer = new Byte[bufferSize];

                // TODO what about overwrite existing file??
                using (System.IO.Stream stream = System.IO.File.Create(f.EntryPath))
                {
                    while (position > 0)
                    {
                        position = data.Value.Read(readBuffer, 0, bufferSize);
                        stream.Write(readBuffer, 0, position);
                        readBuffer = new Byte[bufferSize];
                    }
                    stream.Flush();
                }
            }
            else
            {
#else
            if (true) {
#endif
                DownloadFromOpenBinaryDirect(f, file, estimator);
            }
        }
    }
}
