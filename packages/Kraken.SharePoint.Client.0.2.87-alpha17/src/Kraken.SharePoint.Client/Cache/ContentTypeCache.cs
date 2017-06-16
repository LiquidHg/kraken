namespace Kraken.SharePoint.Client.Caching {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using Microsoft.SharePoint.Client;
  using Kraken.SharePoint.Client;

  public class ContentTypeCacheItem {
    public Guid ListId;
    public Guid WebId;
    public string Name;
    public ContentType ContentType;
  }

  public class ContentTypeCache {

    private Dictionary<string, ContentTypeCacheItem> cache = new Dictionary<string, ContentTypeCacheItem>();

    private ContentTypeCacheItem Find(List list, string ctName) {
      ClientContext ctx = (ClientContext)list.Context;
      // TODO need trace and context manager here
      list.EnsureProperty(l => l.Id);
      KeyValuePair<string, ContentTypeCacheItem> item = (from c in cache
                                                          where c.Value.ListId == list.Id && c.Value.Name.Equals(ctName, StringComparison.InvariantCultureIgnoreCase)
                                                          select c).FirstOrDefault();
      return (item.Key == default(KeyValuePair<string, ContentTypeCacheItem>).Key) ? null : item.Value;
    }
    private ContentTypeCacheItem Find(Web web, string ctName) {
      web.LoadBasicProperties();
      KeyValuePair<string, ContentTypeCacheItem> item = (from c in cache
                                   where c.Value.WebId == web.Id && c.Value.Name.Equals(ctName, StringComparison.InvariantCultureIgnoreCase)
                                   select c).FirstOrDefault();
      return (item.Key == default(KeyValuePair<string, ContentTypeCacheItem>).Key) ? null : item.Value;
    }
    private ContentTypeCacheItem Find(string ctid) {
      if (cache.ContainsKey(ctid))
        return cache[ctid];
      return null;
    }

    public ContentType GetByName(List list, string ctName, bool throwExceptionIfNotFound = true) {
      ContentTypeCacheItem item = Find(list, ctName);
      if (item != null)
        return item.ContentType;

      ContentType contentType = list.GetContentType(ctName);
      if (contentType == null) {
        if (throwExceptionIfNotFound)
          throw new ArgumentException(string.Format("Content type '{0}' does not exist in list '{1}'.", ctName, list.Title), "ctName");
        else
          return null; // Note we cannot cache the fact that a content type didn't exist because we don't have content type id
      }

      item = new ContentTypeCacheItem() {
        ListId = list.Id,
        Name = ctName,
        ContentType = contentType,
      };
      Add(item);
      return item.ContentType;
    }
    public ContentType GetByName(Web web, string ctName, bool throwExceptionIfNotFound = true) {
      ContentTypeCacheItem item = Find(web, ctName);
      if (item != null)
        return item.ContentType;

      ContentType contentType = web.GetContentType(ctName);
      if (contentType == null) {
        if (throwExceptionIfNotFound)
          throw new ArgumentException(string.Format("Content type '{0}' does not exist in web '{1}'.", ctName, web.ServerRelativeUrl), "ctName");
        else
          return null;
      }

      item = new ContentTypeCacheItem() {
        WebId = web.Id,
        Name = ctName,
        ContentType = contentType,
      };
      Add(item);
      return item.ContentType;
    }
    public ContentType GetById(List list, string ctId, bool throwExceptionIfNotFound = true) {
      ContentTypeCacheItem item = Find(ctId);
      if (item != null)
        return item.ContentType;

      ClientContext context = (ClientContext)list.Context;
      //context.Load(web.ContentTypes);
      ContentType contentType = list.ContentTypes.GetById(ctId);
      context.Load(contentType, type => type.Id, type => type.Name);
      context.ExecuteQuery();
      if (contentType == null && throwExceptionIfNotFound)
        throw new ArgumentException(string.Format("Content type '{0}' does not exist in list '{1}'.", ctId, list.Title), "ctId");

      item = new ContentTypeCacheItem() {
        ListId = list.Id,
        Name = contentType.Name,
        ContentType = contentType,
      };
      Add(item);
      return item.ContentType;
    }

    public ContentType GetById(Web web, string ctId, bool throwExceptionIfNotFound = true) {
      ContentTypeCacheItem item = Find(ctId);
      if (item != null)
        return item.ContentType;

      ClientContext context = (ClientContext)web.Context;
      //context.Load(web.ContentTypes);
      ContentType contentType = web.ContentTypes.GetById(ctId);
      context.Load(contentType, type => type.Id, type => type.Name);
      context.ExecuteQuery();
      if (contentType == null && throwExceptionIfNotFound)
        throw new ArgumentException(string.Format("Content type '{0}' does not exist in web '{1}'.", ctId, web.ServerRelativeUrl), "ctId");

      item = new ContentTypeCacheItem() {
        WebId = web.Id,
        Name = contentType.Name,
        ContentType = contentType,
      };
      Add(item);
      return item.ContentType;
    }

    public void Add(ContentTypeCacheItem item) {
      if (item.ContentType == null)
        return; // you can't add to the cache without the content type id
#if !DOTNET_V35
      string ctid = item.ContentType.Id.StringValue;
#else
      string ctid = item.ContentType.Id.ToString();
#endif
      if (cache.ContainsKey(ctid))
        cache[ctid] = item;
      else
        cache.Add(ctid, item);
    }

  }

}
