
namespace Kraken.SharePoint.Client.Caching {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using Microsoft.SharePoint.Client;

  /// <summary>
  /// How to use: Create a FolderCache and occassionally clear it out ;-)
  /// More than one web context is OK as long as its all the same user.
  /// </summary>
  public class FolderCache {

    private Dictionary<string, Folder> cache = new Dictionary<string, Folder>();

    public void Clear() {
      // TODO do we need to dispose anything here?
      cache.Clear();
    }

    public Folder Read(List list, string url) {
      if (list == null)
        throw new ArgumentNullException("list");
      if (string.IsNullOrEmpty(url)) // root folder
        return list.RootFolder;
      // on the off chance we called the wrong override, will still work
      // as long as list is not null
      string serverRelativeUrl = (url.StartsWith("/")) 
        ? url : 
        string.Format("{0}/{1}", list.RootFolder.ServerRelativeUrl, url);
      return Read(list.ParentWeb, serverRelativeUrl);
    }

    public Folder Read(Web web, string serverRelativeUrl) {
      if (cache.ContainsKey(serverRelativeUrl))
        return cache[serverRelativeUrl];
      else {
        // TODO get folder
        Folder folder = web.GetFolder(serverRelativeUrl);
        if (folder != null)
          Add(folder);
        return folder;
      }
    }

    public void Add(Folder folder) {
      string serverRelativeUrl = folder.ServerRelativeUrl;
      if (cache.ContainsKey(serverRelativeUrl))
        cache[serverRelativeUrl] = folder;
      else
        cache.Add(serverRelativeUrl, folder);
    }

  }

}
