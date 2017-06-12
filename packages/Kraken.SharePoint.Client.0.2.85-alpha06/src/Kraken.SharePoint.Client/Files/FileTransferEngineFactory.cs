using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Kraken.SharePoint.Client.Helpers;
using Kraken.Tracing;

namespace Kraken.SharePoint.Client.Files {

  public static class FileTransferEngineFactory {
    private const long SmallFileMaxSize = 1600000;
    private const long MiddleFileMaxSize = 36700160;
    private const long LargeFileMaxSize = 2147483648;

    public static IFileTransferEngine Create(FileTransferParams prms) {
      if (prms.TransferMode != FileTransferMethod.None) {
        switch (prms.TransferMode) {
          case FileTransferMethod.CSOM:
            return new CsomFileUploader(prms);
          case FileTransferMethod.WebDAV:
            return new WebDAVFileUploader(prms);
          case FileTransferMethod.WebDAVTiming:
            return new WebDAVTimeoutFileUploader(prms);
          case FileTransferMethod.FrontPageRPC:
            return new FrontPageRPCFileUploader(prms);
        }
      }
      // Ru team tried to guess this based on length of file,
      // which is nice, but the folder.UploadFile method
      // has much cleaner error handling and retry abilities

      // TODO implement the extension method and the transfer engine to use the same logic always
      return new FancyFileUploader(prms);
      /*
      if (prms.FileLength <= SmallFileMaxSize)
          return new CsomFileUploader(prms);
      if (prms.FileLength <= MiddleFileMaxSize)
          return new WebDAVFileUploader(prms);
      if (prms.FileLength <= LargeFileMaxSize)
          return new WebDAVTimeoutFileUploader(prms);
      //return new FrontPageRPCFileUploader(prms);
      return null;
       */
    }
  }

  public struct FileTransferParams {
    public ClientContext Context;
    //public List ParentList;
    public LocalFileOrFolder LocalPath;
    public IFileMetadataUpdater FileMetadataUpdater;
    public ITrace Log;
    public long FileLength;
    public FileTransferMethod TransferMode;
    public bool OverwriteFiles;
    public bool RecurseSubfolders;
    public bool DoNotEnsureFolders;
  }

  public enum FileTransferMethod {
    None = 0,
    Fancy,
    CSOM,
    WebDAV,
    WebDAVTiming,
    FrontPageRPC
  }

  /// <summary>
  /// Determines how the upload engine will resolve a local file path
  /// </summary>
  public enum LocalPathResolutionMethod {
    None = 0,
    LiteralPath = 1,
    FromFieldValue = 2
  }
}
