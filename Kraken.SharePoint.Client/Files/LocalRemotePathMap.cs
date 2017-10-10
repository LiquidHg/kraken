using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client.Files {
  public class LocalRemotePathMap : Dictionary<string, string> {

    public LocalRemotePathMap(string root, string remotePrefix = "") {
      this.RootPath = root;
      this.RemotePathPrefix = remotePrefix;
    }

    public string RootPath { get; set; }

    private string _remotePathPrefix = string.Empty;
    /// <summary>
    /// You should supply a prefix if you want to
    /// map to the subfolder of a library
    /// </summary>
    public string RemotePathPrefix {
      get {
        return _remotePathPrefix;
      }
      set {
        _remotePathPrefix = value;
        // always ends it with a / unless it is empty
        if (!string.IsNullOrEmpty(_remotePathPrefix)) {
          if (!_remotePathPrefix.EndsWith("/"))
            _remotePathPrefix += "/";
        }
      }
    }

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
    /// Attempt to find the entry and return local path.
    /// Optionally you can generate it using logic.
    /// </summary>
    /// <param name="remoteUrl">The [canonical] remote url</param>
    /// <param name="convertIfNotFound">If true, will calculate it when not found</param>
    /// <param name="found">True if found in collection</param>
    /// <returns></returns>
    public string TryGetLocalPath(string remoteUrl, bool convertIfNotFound, out bool found) {
      IEnumerable<KeyValuePair<string, string>> kvp = this.Where(x => x.Value == remoteUrl);
      if (kvp.Count() == 1) {
        found = true;
        return kvp.First().Key;
      } else {
        found = false;
        if (convertIfNotFound)
          return ConvertRelativeUrlToLocalPath(remoteUrl);
      }
      return string.Empty;
    }

    /// <summary>
    /// Given a Uri (from a SharePoint ListItem)
    /// will convert it to a format where you
    /// can search for it in this collection.
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="folderUrl">The server relative URL for the list root folder or subfolder.</param>
    /// <returns></returns>
    public string GetCanonicalUrl(Uri uri, string folderUrl) {
      string url = string.Empty;
      if (uri != null) {
        url = uri.LocalPath; // has everything but host and schema
        if (url.StartsWith(folderUrl))
          url = url.Substring(folderUrl.Length + 1); // also remove final slash
      }
      // because the url is stored with its remote path
      // but here we've just stripped that out
      if (!string.IsNullOrEmpty(url)) {
        url = this.RemotePathPrefix + url;
      }
      return url;
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
        trimPath = trimPath.Substring(1);
      // prepend a root folder prefix - it should always end in /
      if (!string.IsNullOrEmpty(this.RemotePathPrefix)) {
        if (!string.IsNullOrEmpty(trimPath)) {
          // return the trimPath with preprended root prefix
          trimPath = this.RemotePathPrefix + trimPath;
        } else {
          // when trimPath is totally empty, return the root folder prefix without a trailing slash
          trimPath = this.RemotePathPrefix;
          if (!string.IsNullOrEmpty(trimPath) && trimPath.EndsWith("/"))
            trimPath = trimPath.Substring(0, trimPath.Length - 1);
        }
      }
      return trimPath;
    }

    public static string GetParentFolderName(string url, out string leafFileOrfolderName) {
      string parentFolderPath = string.Empty;
      int lastSlash = url.LastIndexOf("/");
      if (lastSlash >= 0) {
        leafFileOrfolderName = url.Substring(lastSlash + 1);
        parentFolderPath = url.Substring(0, lastSlash);
        // HACK sometimes when this.RemotePathPrefix has a value
        // this will end up ending in a slash when it shouldn't
        // TODO is this caused by bad data in RemoteUrl??
        if (parentFolderPath.EndsWith("/"))
          parentFolderPath = parentFolderPath.Substring(0, parentFolderPath.Length - 1);
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
