namespace Kraken.SharePoint.Client.Caching {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using Microsoft.SharePoint.Client;

  /// <summary>
  /// How to use: Create a ListCache and occassionally clear it out ;-)
  /// More than one web context is OK as long as its all the same user.
  /// </summary>
  public class ListCache {

    private Dictionary<string, List> cache = new Dictionary<string, List>();

    public void Clear() {
      // TODO do we need to dispose anything here?
      cache.Clear();
    }

    /// <summary>
    /// Tries to get the list from the cache of items already loaded
    /// otherwise, tries to retreive the list from SharePoint CSOM API.
    /// </summary>
    /// <param name="web"></param>
    /// <param name="listTitle"></param>
    /// <returns></returns>
    public List Read(Web web, string listTitle) {
      //web.Context.Load(web, w => w.ServerRelativeUrl, w => w.Title);
      //web.Context.ExecuteQuery();
			web.LoadBasicProperties();
			string id = web.ServerRelativeUrl + "/" + listTitle;
      if (cache.ContainsKey(id))
        return cache[id];
      else {
        List list;
				if (web.TryGetList(listTitle, out list) && list != null) {
					Add(list);
				}
        return list;
      }
    }

    public void Add(List list) {
      list.Context.Load(list, l => l.ParentWeb.ServerRelativeUrl, l => l.ParentWeb.Title);
      list.Context.ExecuteQuery();
      string id = list.ParentWeb.ServerRelativeUrl + "/" + list.Title;
      if (cache.ContainsKey(id))
        cache[id] = list;
      else
        cache.Add(id, list);
    }

  }

}
