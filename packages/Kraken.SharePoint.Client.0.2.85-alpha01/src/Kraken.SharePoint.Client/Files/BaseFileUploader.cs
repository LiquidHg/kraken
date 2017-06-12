using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.SharePoint.Client;
using Kraken.SharePoint.Client.Helpers;
using Kraken.Tracing;

namespace Kraken.SharePoint.Client.Files
{
    public abstract class BaseFileUploader : IFileTransferEngine
    {
        protected FileTransferParams Params { get; set; }

        protected ClientContext Context { get { return Params.Context; } }

        /*
          protected string LocalFilePath { get { return Params.LocalFilePath.EntryPath; } }

          protected string LocalFileName { get { return Params.LocalFilePath.FilePath; } }

          protected System.IO.FileInfo LocalFileInfo { get { return new System.IO.FileInfo(LocalFilePath); } }
         */

        protected IFileMetadataUpdater FileMetadataUpdater { get { return Params.FileMetadataUpdater; } }

        protected ITrace Trace { get { return Params.Log; } }

        public Action<TraceLevel, string> Output { get; set; }

      // TODO this is also implemented in file sync manager, can we override it here??
        protected LocalRemotePathMap _map = null;
        public virtual LocalRemotePathMap Map {
          get {
            // get all the entries in the folder, accounting for ubfolder recursion
            if (_map == null) {
              _map = new LocalRemotePathMap(Params.LocalPath.EntryPath);
              _map.PopulateFromLocal(Params.RecurseSubfolders);
            }
            return _map;
          }
        }


        protected BaseFileUploader(FileTransferParams prms)
        {
            Params = prms;
        }

        public void Upload(File targetFile) {
          // TODO check if overwrite is true, otherwise this is useless
          Trace.TraceWarning("Upload directed at remote file rather than folder. Assuming parent folder is desired.");
          // TODO if the target file name is different than the local file, we may want to remap that here somehow, but we currently don't support it here
          Folder rootFolder = targetFile.GetParentFolder();
          if (rootFolder == null)
            throw new ArgumentNullException("rootFolder");
          Upload(rootFolder);
        }

      /// <summary>
      /// Upload a local file or folder to the remote target folder.
      /// If Params.LocalPath is a folder, will attempt to iterate all files
      /// and depending on other Params.RecurseSubfolders it may do
      /// files in subfolders also.
      /// </summary>
      /// <param name="targetFolder">This is the base parent folder to upload into</param>
        public void Upload(Folder targetFolder)
        {
          if (!Params.LocalPath.IsFolder) {
            UploadSingleFile(Params.LocalPath, targetFolder);
          } else { // is a folder
            foreach (KeyValuePair<string, string> kvp in this.Map) {
              try {
                LocalFileOrFolder f = new LocalFileOrFolder(kvp.Key);
                string url = kvp.Value;
                string leafName;
                string parentFolderUrl = LocalRemotePathMap.GetParentFolderName(url, out leafName);
                if (f.IsFolder) {
                  // check to see if it exists already
                  Folder folder = targetFolder.GetFolder(url, true);
                  if (folder == null) {
                    // create folder
                    folder = targetFolder.GetFolder(parentFolderUrl, true);
                    if (folder == null)
                      throw new System.IO.FileNotFoundException(string.Format("Required parent folder '{0}' could not be read and may not exist.", parentFolderUrl));
                    if (!Params.DoNotEnsureFolders)
                      CreateRemoteFolder(f, folder);
                    else
                      Trace.TraceWarning("Skipping folder for '{0}' creation at '{1}'. ", f.FileName, folder.ServerRelativeUrl);
                  }
                } else { // items is a file
                  Folder folder = targetFolder.GetFolder(parentFolderUrl, true);
                  if (folder == null)
                    throw new System.IO.FileNotFoundException(string.Format("Required parent folder '{0}' could not be read and may not exist.", parentFolderUrl));
                  UploadSingleFile(f, folder);
                }
              } catch (Exception ex) {
                Trace.TraceWarning("Failure on upload '{0}' -> '{1}'", kvp.Key, kvp.Value);
                Trace.TraceError(ex);
              }
            }
          } // foreach
        }

        #region Upload support

        private Folder CreateRemoteFolder(LocalFileOrFolder f, Folder parentFolder) {
          if (parentFolder == null)
            throw new ArgumentNullException("parentFolder");
          // primitive Folder type does not have a List object and so we can't really work with metadata yet
          string folderCt = (parentFolder == null) ? "Folder" : "Folder"; //options.RootFolderContentType : options.SubFolderContentType
          string newFolderName = f.FileName;
          string sourceFieldName = string.Empty; //this.MetadataOptions.LocalPathFieldName;
          Folder newFolder = null;

          Trace.TraceVerbose("Creating {1} '{0}' at '{2}'. ", newFolderName, folderCt, parentFolder.ServerRelativeUrl);
          if (!string.IsNullOrEmpty(folderCt) && folderCt != "Folder") {
            // method not current supported since it requires a list object
            /*
            newFolder = list.CreateFolderOrDocumentSet(targetFolder
                , (targetFolder == null) ? topFolderCt : subFolderCt
                , newFolderName
                , f.EntryPath
                , sourceFieldName);
            */
            newFolder = parentFolder.CreateFolder(newFolderName, f.EntryPath, sourceFieldName, true, this.Trace);
          } else {
            newFolder = parentFolder.CreateFolder(newFolderName, f.EntryPath, sourceFieldName, true, this.Trace);
          }
          return newFolder;
        }

        private void UploadSingleFile(LocalFileOrFolder f, Folder targetFolder) {
          if (f.IsFolder)
            return;
          // TODO event to update progress writer
          Trace.TraceInfo("Uploading file '{0}'...", f.FileName);
          Trace.TraceVerbose("Uploading type: {0}", this.GetType().Name);
          File newFile;
          using (var stopWatch = new StopWatchTrace(Trace)) {
            newFile = DoUpload(f, targetFolder);
          }
          if (FileMetadataUpdater != null && newFile != null)
            FileMetadataUpdater.UpdateListItem(newFile);
        }

        #endregion

        private Folder _rootFolder;
        public void Download(Folder folder) {
          _rootFolder = folder;
          DownloadSingleFolder(Params.LocalPath, folder);
        }

        public void Download(File file) {
          _rootFolder = file.GetParentFolder();
          DownloadSingleFile(Params.LocalPath, file);
        }

        private void DownloadSingleFolder(LocalFileOrFolder f, Folder folder) {
          if (!f.Exists && !Params.DoNotEnsureFolders) {
            Trace.TraceVerbose("Creating local folder '{0}'. ", f.EntryPath);
            f.EnsureParentFolder();
          } else if (f.Exists && !f.IsFolder) {
            Trace.TraceWarning("Provided local path '{0}' is not a folder. Can't continue. ", f.EntryPath);
            return;
          } 
          ClientContext context = (ClientContext)folder.Context;
          context.Load(folder.Files);
          context.Load(folder.Folders);
          context.ExecuteQuery();
          
          // loop through files and folders, downloading as we go
          foreach (File file in folder.Files) {
            string local = this.Map.ConvertToLocalPath(file, _rootFolder);
            LocalFileOrFolder subf = new LocalFileOrFolder(local);
            DownloadSingleFile(subf, file);
          }
          if (Params.RecurseSubfolders) {
            foreach (Folder subFolder in folder.Folders) {
              string local = this.Map.ConvertToLocalPath(subFolder, _rootFolder);
              LocalFileOrFolder subf = new LocalFileOrFolder(local);
              DownloadSingleFolder(subf, folder);
            }
          }
        }

        private void DownloadSingleFile(LocalFileOrFolder f, File file) {
            Trace.TraceInfo("Downloading file '{0}'...", file.Name);
            using (var stopWatch = new StopWatchTrace(Trace))
            {
                DoDownload(f, file);
            }
            if (FileMetadataUpdater != null)
              FileMetadataUpdater.UpdateFileProperties(file.ListItemAllFields);
        }

        protected abstract File DoUpload(LocalFileOrFolder f, Folder targetFolder);

        protected abstract void DoDownload(LocalFileOrFolder f, File file);

        private static double EstimateUploadTime(double fileKiloBytes)
        {
            double estimatedSeconds = (fileKiloBytes * Constants.Inrevals.HugeFileTimeOutMultiplier / Constants.Inrevals.SpeedOffice365KBPerSecond);
            return estimatedSeconds;
        }

        private static double EstimateUploadTime(System.IO.FileInfo fi)
        {
            return EstimateUploadTime(fi.Length / 1024);
        }

        protected double GetEstimateUploadTime(System.IO.FileInfo fi)
        {
            double estimatedSeconds = EstimateUploadTime(fi);
            int estTimeOut = (int)(estimatedSeconds * 1000 * 2); // HACK x2 added because stuff was timing out too often
            return estTimeOut;
        }

        protected File GetFile(string serverRelativeUrl)
        {
            File newFile = Context.Web.GetFileByServerRelativeUrl(serverRelativeUrl);
            Context.Load(newFile);
            Context.ExecuteQuery();
            return newFile;
        }

        protected void DownloadFromOpenBinaryDirect(LocalFileOrFolder f, File file)
        {
          if (f.IsFolder)
            throw new Exception("Target is a folder and should be a file or not exist. Can't continue.");
          using (FileInformation fileInfo = File.OpenBinaryDirect(Context, file.ServerRelativeUrl))
            {
                using (System.IO.FileStream fs = new System.IO.FileStream(f.EntryPath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    int size = 10 * 1024;
                    byte[] buffer = new byte[size];
                    int byteRead;
                    while ((byteRead = fileInfo.Stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fs.Write(buffer, 0, byteRead);
                    }
                }
            }
        }
    }
}
