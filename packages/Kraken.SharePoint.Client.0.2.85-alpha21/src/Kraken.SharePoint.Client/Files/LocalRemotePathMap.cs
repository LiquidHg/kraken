using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client.Files {
  public class LocalRemotePathMap : Dictionary<string, string> {

    public LocalRemotePathMap(string root) {
      this.RootPath = root;
    }

    public string RootPath { get; set; }

    public string RootFolderPath {
      get {
        Kraken.SharePoint.Client.Files.LocalFileOrFolder f = new Kraken.SharePoint.Client.Files.LocalFileOrFolder(this.RootPath);
        string rootLocalFolder = (f.IsFolder) ? f.EntryPath : f.FolderPath;
        return rootLocalFolder;
      }
    }

    public string ConvertToLocalPath(Folder remoteObject, Folder rootFolder) {
      string relativeUrl = GetRelativeUrl(remoteObject, rootFolder);
      return ConvertRelativeUrlToLocalPath(relativeUrl);
    }
    public string ConvertToLocalPath(File remoteObject, Folder rootFolder) {
      string relativeUrl = GetRelativeUrl(remoteObject, rootFolder);
      return ConvertRelativeUrlToLocalPath(relativeUrl);
    }
    private string GetRelativeUrl(File remoteObject, Folder rootFolder) {
      string rootUrl = rootFolder.ServerRelativeUrl;
      string itemUrl = remoteObject.ServerRelativeUrl;
      return GetRelativeUrl(itemUrl, rootUrl);
    }
    private string GetRelativeUrl(Folder remoteObject, Folder rootFolder) {
      string rootUrl = rootFolder.ServerRelativeUrl;
      string itemUrl = remoteObject.ServerRelativeUrl;
      return GetRelativeUrl(itemUrl, rootUrl);
    }

    private string GetRelativeUrl(string itemServerUrl, string rootFolderServerUrl) {
      if (!itemServerUrl.StartsWith(rootFolderServerUrl))
        throw new ArgumentException(string.Format("remoteObject url must start with {0}.", rootFolderServerUrl));
      string relativeUrl = itemServerUrl.Substring(rootFolderServerUrl.Length);
      if (relativeUrl.StartsWith("/"))
        relativeUrl = relativeUrl.Substring(1);
      return relativeUrl;
    }

    /// <summary>
    /// Where RootPath is c:\rootpath
    /// Takes a url like folder1/folder2/file.aspx
    /// and converts it to c:\rootpath\folder1\folder2\file.aspx
    /// </summary>
    /// <param name="remoteUrl"></param>
    /// <param name="serverRelativeRootUrl"></param>
    /// <returns></returns>
    public string ConvertRelativeUrlToLocalPath(string remoteUrl) {
      string rootLocalFolder = this.RootFolderPath;
      if (!rootLocalFolder.EndsWith("\\"))
        rootLocalFolder += "\\";
      return rootLocalFolder + remoteUrl.Replace("/", "\\");
    }

    /// <summary>
    /// Uses a standardized technique to convert a folder structure on the local system into a relative URL
    /// </summary>
    /// <param name="rootFilePath"></param>
    /// <param name="localFilePath"></param>
    /// <returns></returns>
    public string ConvertLocalFolderToRelativeUrl(string localFilePath) {
      string rootLocalFolder = this.RootFolderPath;
      if (!localFilePath.StartsWith(rootLocalFolder))
        throw new ArgumentException(string.Format("localFilePath '{0}' must start with rootLocalFolder '{1}'.", localFilePath, rootLocalFolder));
      string trimPath = localFilePath.Substring(rootLocalFolder.Length);
      trimPath = trimPath.Replace("\\", "/");
      // get rid of any leading /
      if (trimPath.StartsWith("/"))
        return trimPath.Substring(1);
      return trimPath;
    }

    public static string GetParentFolderName(string url, out string leafFileOrfolderName) {
      string parentFolderPath = string.Empty;
      int lastSlash = url.LastIndexOf("/");
      if (lastSlash >= 0) {
        leafFileOrfolderName = url.Substring(lastSlash + 1);
        parentFolderPath = url.Substring(0, lastSlash);
      } else {
        leafFileOrfolderName = url;
      }
      return parentFolderPath;
    }

    /// <summary>
    /// Given a local path, this will create a collection
    /// of all the files and folders in that path with relative
    /// URL mappings that can be used with functions like
    /// UploadFile or CreateFolder.
    /// </summary>
    /// <param name="rootLocalPath"></param>
    /// <returns></returns>
    public void PopulateFromLocal(bool recurse) {
      //Dictionary<string, string> ret = new Dictionary<string, string>();
      string rootLocalPath = this.RootPath;
      Kraken.SharePoint.Client.Files.LocalFileOrFolder f = new Kraken.SharePoint.Client.Files.LocalFileOrFolder(rootLocalPath);
      // we don't ignore folderTreeTransformer in the case of files,
      // rather we're hoping it is set up to flatten the file as needed
      if (f.IsFolder) {
        // This sort of depends on the fact that the folder exists
        System.IO.SearchOption option = recurse ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly;
#if !DOTNET_V35
        List<string> entries = System.IO.Directory.EnumerateFileSystemEntries(f.EntryPath, "*", option).ToList();
#else
        List<string> entries = System.IO.Directory.GetDirectories(f.EntryPath, "*", option).ToList();
#endif
        foreach (string entry in entries) {
          string url = ConvertLocalFolderToRelativeUrl(entry);
          this.Add(entry, url);
        }
      } else { // assumed to be a single file
        string url = ConvertLocalFolderToRelativeUrl(f.EntryPath);
        this.Add(f.EntryPath, url);
      }
    }

  }

}
