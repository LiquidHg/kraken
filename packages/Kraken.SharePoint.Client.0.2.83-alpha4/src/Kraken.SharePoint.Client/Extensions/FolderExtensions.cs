namespace Microsoft.SharePoint.Client {

  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using System.Text;
  using System.Net;

  using Kraken.Security.Cryptography;
  using Kraken.SharePoint.Client;
  using Kraken.Tracing;
  using Kraken.SharePoint.Client.Caml;

  public static class KrakenFolderExtensions {

    private const int maxSize = 1600000; // really 2097152 but let's give it some margin;
    private const int maxRetries = 2;
    private const int retryPause = 2000;
    private const int hugeFileSize = 36700160; // about 35MB
    private const double hugeFileTimeOutMultiplier = 1.5; // 1.25 was good for a while, but when things get slow it causes significant timeouts
    //private const int hugeFileTimeOut = 300000; // 5 minutes!

    /// <summary>
    /// A typical number of MB/second on O365; used to calculate timeouts
    /// </summary>
    private const int office365KBPerSecond = 250;

    #region Sub-folders

    public static Folder GetFolder(this Folder folder, string folderName, bool ignoreCase) {
      if (string.IsNullOrEmpty(folderName))
        return folder;
      ClientContext context = (ClientContext)folder.Context;

      Folder existingFolder = null;
      string folderUrl = string.Format("{0}/{1}", folder.ServerRelativeUrl, folderName);
      IEnumerable<Folder> existingFolders = context.LoadQuery(
        (ignoreCase)
#if !DOTNET_V35
        ? folder.Folders.Include(f => f.ServerRelativeUrl, f => f.TimeCreated, f => f.TimeLastModified, f => f.ItemCount)
        : folder.Folders.Where(f => f.ServerRelativeUrl == folderUrl).Include(f => f.ServerRelativeUrl, f => f.TimeCreated, f => f.TimeLastModified, f => f.ItemCount)
#else
        ? folder.Folders.Include(f => f.ServerRelativeUrl) // TimeCreated and TimeLastModified not supported in older CSOM
        : folder.Folders.Where(f => f.ServerRelativeUrl == folderUrl).Include(f => f.ServerRelativeUrl)
#endif
);
      context.ExecuteQuery();
      // in the case of !ignoreCase there should only be one item
      existingFolder = existingFolders.FirstOrDefault(
        f => f.ServerRelativeUrl.ToLower() == folderUrl.ToLower());
      return existingFolder;
    }

    /// <summary>
    /// Create a sub-folder under a folder
    /// </summary>
    /// <param name="parentFolder">The parent folder that will contain the new folder</param>
    /// <param name="newFolderName">Just the folder name, does not support nested paths at this time</param>
    /// <param name="localFilePath">A local folder for purposes of copying metadata</param>
    /// <param name="localFilPathFieldName">Local path field name for storing original folder location</param>
    /// <param name="doExecuteQuery">When false, execute query will be deferred but this will prevent metadata operations</param>
    /// <param name="trace">A trace object for log/screen output</param>
    /// <returns></returns>
    /// <remarks>
    /// This works differently than the extension in ListExtensions
    /// because from here we don't have ready access to ListCreationInformation
    /// Instead, we're using folder.Folders.Add and then ListItemAllFields
    /// </remarks>
    public static Folder CreateFolder(
      this Folder parentFolder,
      string newFolderName,
      string localFilePath,
      string localFilPathFieldName,
      bool doExecuteQuery = true,
      ITrace trace = null
    ) {
      if (parentFolder == null)
        throw new ArgumentNullException("parentFolder");
      if (string.IsNullOrEmpty(newFolderName))
        throw new ArgumentNullException("newFolderName");
      // TODO we can't really do this for content types like doc set yet
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)parentFolder.Context;
      CoreMetadataInfo metaData = new CoreMetadataInfo(localFilePath, trace) {
        LocalFilePathFieldName = localFilPathFieldName
      };
      context.Load(parentFolder);
      Folder newFolder = parentFolder.Folders.Add(newFolderName);
      // if we wanted to do more than one, it would go like this
      /*
      string[] namesArray = new string[] { "Folder1", "Folder2", "Folder3" };
      Folder folder = parentFolder;
      foreach (string name in namesArray) {
        folder = folder.Folders.Add(name);
      }
      */
      ListItem item = null;
      if (doExecuteQuery) {
        context.ExecuteQuery();
#if !DOTNET_V35
        item = newFolder.ListItemAllFields;
#endif
      }
      if (item != null) {
        if (!string.IsNullOrEmpty(localFilPathFieldName))
          metaData.EnsureLocalFilePathField(item.ParentList);
        metaData.SetListItemMetadata(item);
        item.Update();
        if (doExecuteQuery)
          context.ExecuteQuery();
      }
      return newFolder;
    }

    #endregion

    public static IEnumerable<File> GetFiles(this Folder folder) {
      ClientContext context = (ClientContext)folder.Context;
      FileCollection files = folder.Files;
      IEnumerable<File> loadedFiles = context.LoadQuery(
        #if !DOTNET_V35
              files.Include(f => f.ServerRelativeUrl, f => f.TimeCreated, f => f.TimeLastModified, f => f.Length)
        #else
              files.Include(f => f.ServerRelativeUrl, f => f.TimeCreated, f => f.TimeLastModified)
        #endif
      );
      context.ExecuteQuery();
      return loadedFiles;
    }

    public static File GetFile(this Folder folder, string fileName, bool ignoreCase) {
      ClientContext context = (ClientContext)folder.Context;

      File existingFile = null;
      //FolderCollection folders = list.RootFolder.Folders;

      string fileUrl = string.Format("{0}/{1}", folder.ServerRelativeUrl, fileName);
      IEnumerable<File> existingFiles = context.LoadQuery(
        (ignoreCase)
#if !DOTNET_V35
        ? folder.Files.Include(f => f.ServerRelativeUrl, f => f.TimeCreated, f => f.TimeLastModified, f => f.Length)
        : folder.Files.Where(f => f.ServerRelativeUrl == fileUrl).Include(f => f.ServerRelativeUrl, f => f.TimeCreated, f => f.TimeLastModified, f => f.Length)
#else
 ? folder.Files.Include(f => f.ServerRelativeUrl, f => f.TimeCreated, f => f.TimeLastModified)
        : folder.Files.Where(f => f.ServerRelativeUrl == fileUrl).Include(f => f.ServerRelativeUrl, f => f.TimeCreated, f => f.TimeLastModified)
#endif
      );
      context.ExecuteQuery();
      // in the case of !ignoreCase there should only be one item
      existingFile = existingFiles.FirstOrDefault(
        file => file.ServerRelativeUrl.ToLower() == fileUrl.ToLower());
      return existingFile;
    }

    /*
    public enum UploadResult {
      Unknown,
      Uploaded,
      NotUploaded
    }
     */

    public static File UploadFile(this Folder folder/*, List parentList */, string localFilePath, string localFilPathFieldName, bool overwrite, string ctid, ITrace trace = null, bool tryWebDAVOnError = true, int retryCount = 0) {
      if (trace == null) trace = NullTrace.Default;
      File result = null;
      //UploadResult result = UploadResult.Unknown;
      System.IO.FileInfo fi = new System.IO.FileInfo(localFilePath);
      double estimatedSeconds = EstimateUploadTime(fi);
      trace.Trace(TraceLevel.Info, "File size is {0:0} KBytes; Estimated upload time {1:0} Seconds", fi.Length / 1024, estimatedSeconds);
      //bool doWebDAVUpload = (fi.Length > maxSize);
      //bool fileIsHuge = (fi.Length > hugeFileSize);
      UploadMethod um = ChooseUploadMethod(fi);
      if (um == UploadMethod.DirectClone)
        trace.Trace(TraceLevel.Warning, "File is {0:0}MB or bigger. Using the huge file workaround. Upload timeout extended to {1:0.00} minutes. Prepare yourself! This operation may take a long time.", hugeFileSize / (1024 * 1024), ((double)estimatedSeconds) / 60);
      DateTime startTime = DateTime.Now;

      try {
        result = folder.UploadFileInternals(um, HashAlgorithmType.None, localFilePath, localFilPathFieldName, overwrite, ctid, trace);
        //result = UploadResult.Uploaded;
      } catch (Microsoft.SharePoint.Client.ServerException ex) { // .InvalidClientQueryException
        //result = UploadResult.NotUploaded;
        bool alreadyExistsError = (ex.ServerErrorCode == -2130575257 || ex.Message.Contains("already exist"));
        if (alreadyExistsError && !overwrite) {
          trace.Trace(TraceLevel.Warning, "File failed upload because a file exists with the same name and overwrite options was not specified.");
        } else if (um == UploadMethod.CSOM && tryWebDAVOnError
          && !alreadyExistsError) {
          trace.Trace(TraceLevel.Warning, "Upload of file triggered exception. Handling exception for large files in CSOM.");
          try {
            result = folder.UploadFileInternals(UploadMethod.Direct, HashAlgorithmType.None, localFilePath, localFilPathFieldName, overwrite, ctid, trace);
            //result = UploadResult.Uploaded;
          } catch (Exception) {
            //result = UploadResult.NotUploaded;
          }
        } else
          throw ex;
      } catch (System.Net.WebException webex) {
        //result = UploadResult.NotUploaded;
        TimeSpan ts = DateTime.Now.Subtract(startTime);
        // this error occurs because Office 365 was being a pain
        trace.Trace(TraceLevel.Warning, "WebException was thrown by the server. Message = '{0}'", webex.Message); // webex.Response
        if (webex.Message == "The operation has timed out") {
          trace.Trace(TraceLevel.Error, "Operation timed out after {0}m:{1:00}s. File is too big!", ts.Minutes, ts.Seconds);
          // TODO find a way to optionally adjust the timeout estimates and retry...
          throw webex;
        }
        if (retryCount < maxRetries) {
          trace.Trace(TraceLevel.Info, "Pausing a bit to give SharePoint server time to recover.");
          System.Threading.Thread.Sleep(retryPause);
          trace.Trace(TraceLevel.Info, "Retrying... Attempt {0} out of {1}", retryCount + 1, maxRetries);
          result = folder.UploadFile(/* parentList, */ localFilePath, localFilPathFieldName, overwrite, ctid, trace, tryWebDAVOnError, retryCount + 1);
        } else {
          trace.Trace(TraceLevel.Error, "maxRetries reached. I give up!");
          throw webex;
        }
      }
      TimeSpan ts2 = DateTime.Now.Subtract(startTime);
      trace.Trace(TraceLevel.Info, "Upload operation finished after {0}h:{1}m:{2:00}s.", ts2.Hours, ts2.Minutes, ts2.Seconds);
      // TODO inform our estimates
      return result;
    }

		public static bool Rename(this Folder folder, string newTitle, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      var ctx = folder.Context;
			try {
#if !DOTNET_V35
        ListItem listitem = folder.ListItemAllFields;
#else
        throw new NotImplementedException("Can't convert folder to list item in older versions of CSOM.");
        ListItem listitem = null;
        // TODO implement SP14 CSOM convert from folder to item
#endif
        listitem["FileLeafRef"] = newTitle;
				listitem.Update();
				ctx.ExecuteQuery();
        return true;
			} catch (Microsoft.SharePoint.Client.ServerException ex) {
        trace.TraceError(ex);
        return false;
			}
		}

    public static double EstimateUploadTime(double fileKiloBytes) {
      double estimatedSeconds = (fileKiloBytes * hugeFileTimeOutMultiplier / office365KBPerSecond);
      return estimatedSeconds;
    }
    public static double EstimateUploadTime(System.IO.FileInfo fi) {
      return EstimateUploadTime(fi.Length / 1024);
    }

    private static UploadMethod ChooseUploadMethod(System.IO.FileInfo fi) {
      if (fi.Length <= maxSize)
        return UploadMethod.CSOM;
      if (fi.Length <= hugeFileSize)
        return UploadMethod.Direct;
      return UploadMethod.DirectClone;
    }

    public static File UploadFileInternals(this Folder folder, /* List parentList, */ UploadMethod uploadMethod, HashAlgorithmType hashMethod, string localFilePath, string localFilPathFieldName, bool overwrite, string ctid, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      System.IO.FileInfo fi = new System.IO.FileInfo(localFilePath);
      // TODO support files with lengths > int can support
      if (fi.Length > int.MaxValue)
        throw new NotSupportedException("The file is bigger than 2GB; Uploading to SharePoint is not supported.");

      // Prepare local file stuff
      string crcHash = string.Empty; string md5Hash = string.Empty;
      // TODO check to make sure "MD5Hash" or "CRC32" fields exist

      using (System.IO.FileStream fstream = System.IO.File.OpenRead(localFilePath)) {
#if !DOTNET_V35
        if (hashMethod.HasFlag(HashAlgorithmType.CRC32)) {
          crcHash = fstream.ComputeCrc32(true); // resets stream to 0 when done
        }
        if (hashMethod.HasFlag(HashAlgorithmType.MD5)) {
          md5Hash = fstream.ComputeMD5Hash(true); // resets stream to 0 when done
        }
#else
        if (0 < (hashMethod & HashAlgorithmType.CRC32)) {
          crcHash = fstream.ComputeCrc32(true); // resets stream to 0 when done
        }
        if (0 < (hashMethod & HashAlgorithmType.MD5)) {
          md5Hash = fstream.ComputeMD5Hash(true); // resets stream to 0 when done
        }
#endif
        fstream.Close();
      } // using
      // Use the stream always, because it will likely perform better then buffer array
      /*
#if !DOTNET_V35
      if (hashMethod.HasFlag(HashAlgorithmType.CRC32)) { 
        crcHash = buffer.ComputeCrc32();
      }
      if (hashMethod.HasFlag(HashAlgorithmType.MD5)) { 
        md5Hash = buffer.ComputeMD5Hash();
      }
#else
      if (0 < (hashMethod & HashAlgorithmType.CRC32)) { 
        crcHash = buffer.ComputeCrc32();
      }
      if (0 < (hashMethod & HashAlgorithmType.MD5Hash)) { 
        md5Hash = buffer.ComputeMD5Hash();
      }
#endif
       */

      ClientContext context = (ClientContext)folder.Context;
      File newFile = null;

      var scope = new ExceptionHandlingScope(context);
      using (scope.StartScope()) {
        using (scope.StartTry()) {
          switch (uploadMethod) {
            case UploadMethod.CSOM:
              byte[] buffer = System.IO.File.ReadAllBytes(localFilePath);
              FileCreationInformation fci = new FileCreationInformation();
              fci.Url = System.IO.Path.GetFileName(localFilePath);
              fci.Content = buffer;
              fci.Overwrite = overwrite;
              newFile = folder.Files.Add(fci);
              break;
            case UploadMethod.Direct:
            case UploadMethod.DirectClone:
              string fn = System.IO.Path.GetFileName(localFilePath);
              string fileUrl = string.Format("{0}/{1}", folder.ServerRelativeUrl, fn);
              using (System.IO.FileStream fstream = System.IO.File.OpenRead(localFilePath)) {
                // This was causing our files to be 0kb because the pointer was at the end of the buffer
                //fstream.Read(content, 0, (int)fi.Length);
                if (uploadMethod == UploadMethod.Direct) {
                  File.SaveBinaryDirect(context, fileUrl, fstream, true);
                } else { // about 40 MB
                  // note that this doesn't save the file creation and modification dates
                  // TODO move the size calculation to here
                  double estimatedSeconds = EstimateUploadTime(fi);
                  int estTimeOut = (int)(estimatedSeconds * 1000 * 2); // HACK x2 added because stuff was timing out too often
                  trace.TraceInfo(string.Format("Estimated {0} seconds.", estimatedSeconds));
                  // loads the file the brute force way
                  context.SaveBinaryDirect(estTimeOut, fileUrl, fstream, overwrite); // hugeFileTimeOut
                }
                fstream.Close();
              }
              newFile = context.Web.GetFileByServerRelativeUrl(fileUrl);
              break;
          }
        }
        using (scope.StartCatch()) {
          // TODO what do we want to do here???
        }
        using (scope.StartFinally()) {
        }
      }
      context.Load(newFile);
      context.Load(newFile.ListItemAllFields);
      context.ExecuteQuery();
      // ServerException will throw on failure to be caught by caller
      if (scope.HasException) {
        trace.TraceError(scope.ErrorMessage + " -> " + scope.ServerStackTrace);
        //throw new Exception();
      }

      // TODO can we reduce the number of fields here to save transfer time?
      ListItem item = newFile.ListItemAllFields;
      // core will need to do its own ExecuteQuery if fields need to be added
      CoreMetadataInfo core = new CoreMetadataInfo(localFilePath, item.ParentList, !string.IsNullOrEmpty(localFilPathFieldName), trace);

      ExceptionHandlingScope scope2 = new ExceptionHandlingScope(context);
      using (scope2.StartScope()) {
        using (scope2.StartTry()) {
          /*
          if (parentList == null)
            parentList = item.ParentList;
           */
          item.UpdateCoreMetadata(core, ctid, crcHash, md5Hash);
          item.Update();
        }
        using (scope2.StartCatch()) {
          // TODO what do we want to do here???
        }
        using (scope2.StartFinally()) {
        }
      }
      context.ExecuteQuery();
      // ServerException will throw on failure to be caught by caller
      if (scope2.HasException) {
        trace.TraceError(scope2.ErrorMessage + " -> " + scope2.ServerStackTrace);
        //throw new Exception();
      }

      if (item != null)
        item.ThrowOnZeroKBFile();

      return newFile;
    }

#region VERY large file support

    private enum SaveBinaryCheckMode {
      ETag,
      Overwrite
    }

    private static string MakeFullUrl(ClientContext context, string serverRelativeUrl) {
      if (context == null) {
        throw new ArgumentNullException("context");
      }
      if (serverRelativeUrl == null) {
        throw new ArgumentNullException("serverRelativeUrl");
      }
      if (!serverRelativeUrl.StartsWith("/")) {
        throw new ArgumentOutOfRangeException("serverRelativeUrl");
      }
      Uri baseUri = new Uri(context.Url);
      baseUri = new Uri(baseUri, serverRelativeUrl);
      return baseUri.AbsoluteUri;
    }

    public static void SaveBinaryDirect(this ClientContext context, int timeOut, string serverRelativeUrl, System.IO.Stream stream, bool overwriteIfExists) {
      SaveBinary(context, timeOut, serverRelativeUrl, stream, null, overwriteIfExists, SaveBinaryCheckMode.Overwrite);
    }
    public static void SaveBinaryDirect(this ClientContext context, int timeOut, string serverRelativeUrl, System.IO.Stream stream, string etag) {
      SaveBinary(context, timeOut, serverRelativeUrl, stream, etag, false, SaveBinaryCheckMode.ETag);
    }

    private static void SaveBinary(ClientContext context, int timeOut, string serverRelativeUrl, System.IO.Stream stream, string etag, bool overwriteIfExists, SaveBinaryCheckMode checkMode) {
      if (context == null) {
        throw new ArgumentNullException("context");
      }
      if (context.HasPendingRequest) {
        throw new ClientRequestException(Resources.GetString("NoDirectHttpRequest"));
      }
      string requestUrl = MakeFullUrl(context, serverRelativeUrl);
      WebRequestExecutor webRequestExecutor = context.WebRequestExecutorFactory.CreateWebRequestExecutor(context, requestUrl);
      webRequestExecutor.WebRequest.Timeout = timeOut;
      webRequestExecutor.RequestMethod = "PUT";
      if (checkMode == SaveBinaryCheckMode.ETag) {
        if (!string.IsNullOrEmpty(etag)) {
          webRequestExecutor.RequestHeaders[HttpRequestHeader.IfMatch] = etag;
        }
      } else if (!overwriteIfExists) {
        webRequestExecutor.RequestHeaders[HttpRequestHeader.IfNoneMatch] = "*";
      }
      //((MyClientContext)context).MyFireExecutingWebRequestEventInternal(new WebRequestEventArgs(webRequestExecutor));
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
          throw new ClientRequestException(Resources.GetString("RequestUnexpectedResponseWithContentTypeAndStatus", new object[] { webRequestExecutor.ResponseContentType, webRequestExecutor.StatusCode }));
        }
      } catch (WebException exception) {
        HttpWebResponse response = exception.Response as HttpWebResponse;
        if ((response == null) || (response.StatusCode != HttpStatusCode.PreconditionFailed)) {
          throw;
        }
        if (checkMode == SaveBinaryCheckMode.ETag) {
          throw new ClientRequestException(Resources.GetString("ETagNotMatch"));
        }
        throw new ClientRequestException(Resources.GetString("FileAlreadyExists"));
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
      /*
      EventHandler<WebRequestEventArgs> executingWebRequest = this.ExecutingWebRequest;
      if (executingWebRequest != null) {
        executingWebRequest(this, args);
      }
       */
    }

    /// <summary>
    /// Return the list item for a given folder object
    /// Assumes that folder is actually in parentList.
    /// Executes query.
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="parentList"></param>
    /// <param name="doExecute"></param>
    /// <returns></returns>
    public static ListItem GetListItem(this Folder folder, List parentList) {
      // TODO get the folder's server realtive URL
      CamlQuery query = new CamlQuery();
      query.FolderServerRelativeUrl = folder.ServerRelativeUrl;
      query.ViewXml = CAML.View(
        CAML.ViewScope.All, // not RecursiveAll, because we just want this subfolder
        CAML.Query(
          CAML.Where(CAML.And(
            CAML.Eq(CAML.FieldRef("ContentType"), CAML.Value("Folder")),
            CAML.Eq(CAML.FieldRef("FileLeafRef"), CAML.Value(folder.Name))
          )),
          "" /* order by */
        )
      );
      // TODO is it possible above CAML will mess up in cases where subfolders have the same name?
      ListItemCollection items = parentList.GetItems(query);
      parentList.Context.Load(items);
      parentList.Context.ExecuteQuery();
      // can't return FirstOrDefault unless we execute this now
      return items.FirstOrDefault();
    }

#endregion

  }
}
