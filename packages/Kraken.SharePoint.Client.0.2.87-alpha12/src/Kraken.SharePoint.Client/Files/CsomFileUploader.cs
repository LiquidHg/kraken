using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Kraken.SharePoint.Client.Helpers;

namespace Kraken.SharePoint.Client.Files
{
    public class CsomFileUploader : BaseFileUploader
    {
        public CsomFileUploader(FileTransferParams prms)
            : base(prms)
        {
        }

        protected override File DoUpload(LocalFileOrFolder f, Folder targetFolder)
        {
          if (!f.Exists)
            throw new Exception("Target file doesn't exist. Can't continue.");
          if (f.IsFolder)
            throw new Exception("Target is a folder and should be a file. Can't continue.");
          byte[] buffer = System.IO.File.ReadAllBytes(f.EntryPath);

          ClientContext context = (ClientContext)targetFolder.Context;
          File newFile = null;
          try {
            var scope = new ExceptionHandlingScope(context);
            using (scope.StartScope()) {
              using (scope.StartTry()) {
                FileCreationInformation fci = new FileCreationInformation();
                fci.Url = System.IO.Path.GetFileName(f.EntryPath);
                fci.Content = buffer;
                fci.Overwrite = Params.OverwriteFiles;
                newFile = targetFolder.Files.Add(fci);
                context.Load(newFile);
              }
            }
          } catch (Exception ex) {
            this.Trace.TraceWarning("Upload file '{0}' to '{1}' failed with error. Exception: {2}", f.EntryPath, targetFolder.ServerRelativeUrl, ex.Message);
          }
          context.ExecuteQuery();
          return newFile;
        }

        protected override void DoDownload(LocalFileOrFolder f, File file)
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
                DownloadFromOpenBinaryDirect(f, file);
            }
        }
    }
}
