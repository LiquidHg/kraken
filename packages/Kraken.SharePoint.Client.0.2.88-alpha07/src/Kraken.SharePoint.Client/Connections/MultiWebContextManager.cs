using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Net;
using Microsoft.SharePoint.Client;

namespace Kraken.SharePoint.Client.Connections {

  /// <summary>
  /// Stores client context managers for all the connected contexts.
  /// </summary>
  public class MultiWebContextManager : Dictionary<string, WebContextManager> {

    public MultiWebContextManager() {
    }
    public MultiWebContextManager(ICredentials credentials) {
      SharePointCredential spCred = credentials as SharePointCredential;
      if (spCred != null)
        this.MasterCredentials = spCred;
      else {
        throw new NotSupportedException("Sorry, but we can't convert other credential types to SharePointCredential yet.");
      }
    }
    public MultiWebContextManager(string user, SecureString pass, ClientAuthenticationType authType)
      : this(new SharePointCredential(user, pass, authType)) {
    }
    public MultiWebContextManager(SharePointCredential credentials) {
      this.MasterCredentials = credentials;
    }

    /// <summary>
    /// Serves as the master credential set for every WebContextManager in this collection
    /// </summary>
    public SharePointCredential MasterCredentials { get; set; }
    public bool IsCachingEnabled { get; private set; }

    private static MultiWebContextManager _current;
    public static MultiWebContextManager Current {
      get {
        if (_current == null)
          _current = new MultiWebContextManager();
        return _current;
      }
    }

    /// <summary>
    /// Please don't call on me unless you really need to
    /// </summary>
    /// <param name="mgr"></param>
    public void Add(WebContextManager mgr) {
      string key = mgr.GenerateUniqueKey();
      if (this.ContainsKey(key))
        throw new InvalidOperationException("Provided context manager already exists in the store!");
      Add(key, mgr);
      if (!SuppressRecentUseTracking)
        _mostRecentKey = key;
    }

    public bool SuppressRecentUseTracking {
      get;
      set;
    }

    public bool Contains(WebContextManager mgr) {
      string key = mgr.GenerateUniqueKey();
      return this.ContainsKey(key);
    }
    public void Remove(WebContextManager mgr) {
      string key = mgr.GenerateUniqueKey();
      if (this.ContainsKey(key))
        this.Remove(key);
    }

    /// <summary>
    /// Attempt to retreive a WebContextManager with
    /// limited information about the context.
    /// </summary>
    /// <param name="webUrl"></param>
    /// <param name="user"></param>
    /// <returns>Null if not found, otherwise a WebContextManager object.</returns>
    public WebContextManager Find(Uri webUrl, string user = "") {
      if (!string.IsNullOrEmpty(user)) {
        return GetByKey(user, webUrl.ToString());
      } else {
        foreach (string key in this.Keys) {
          if (key.EndsWith("|" + webUrl.ToString())) {
            return this[key];
          }
        }
        return null;
      }
    }

    internal WebContextManager GetByKey(string key) {
      if (string.IsNullOrEmpty(key))
        return null;
      if (this.ContainsKey(key)) {
        if (!SuppressRecentUseTracking)
          _mostRecentKey = key;
        return this[key];
      }
      return null;
    }
    internal WebContextManager GetByInstance(WebContextManager mgr) {
      string key = mgr.GenerateUniqueKey();
      return GetByKey(key);
    }
    internal WebContextManager GetByKey(string user, string webUrl) {
      string key = WebContextManager.GenerateUniqueKey(user, webUrl);
      return GetByKey(key);
    }
    internal WebContextManager GetByContext(ClientContext context) {
      string key = WebContextManager.GenerateUniqueKey(context);
      return GetByKey(key);
    }

    protected string _mostRecentKey;

    public WebContextManager GetMostRecentlyUsed(bool throwOnNone = true) {
      if (string.IsNullOrEmpty(_mostRecentKey)) {
        if (throwOnNone)
          throw new InvalidOperationException("Attempted to get recently used WebContextManager, but none has been used. Connect to and Initialize at least one WebContextManager before calling this method.");
        return null;
      }
      return GetByKey(_mostRecentKey);
    }
    public WebContextManager TryGetOrCopy(WebContextManager copyFrom, string webUrl) {
      string key = WebContextManager.GenerateUniqueKey(copyFrom, webUrl);
      if (this.ContainsKey(key))
        return this[key];
      return new WebContextManager(copyFrom, webUrl);
    }

    public WebContextManager TryGetOrCopy(WebContextManager copyFrom, Guid webId) {
      // You can't really cache by web url if all you have is the ID, so we have to
      // create a new manager here every time. This isn't ideal, but we can improve
      // the cache keys later.
      WebContextManager mgr = new WebContextManager(copyFrom, webId);
      string webUrl = mgr.TargetWebUrl;
      string key = WebContextManager.GenerateUniqueKey(copyFrom, webUrl);
      if (this.ContainsKey(key))
        return this[key];
      return mgr;
    }

    public WebContextManager TryGetOrCreateFromContext(ClientContext context) {
      WebContextManager mgr = GetByContext(context);
      if (mgr != null)
        return mgr;
      //
      // TODO since context must've connected, can't we just use it and say its been conntected?
      //
      // this method uses master credentials tied to this class
      if (this.MasterCredentials != null)
        return new WebContextManager(this.MasterCredentials, context.Url, IsCachingEnabled);
      // this method does not rely on using master credentials but generates an incomplete set of properties
      SharePointCredential cred = new SharePointCredential(context.Credentials);
      return new WebContextManager(cred, context.Url, IsCachingEnabled);
    }
    public WebContextManager TryGetOrCreate(SharePointCredential cred, string webUrl) {
      WebContextManager mgr = GetByKey(cred.UserName, webUrl);
      if (mgr != null)
        return mgr;
      return new WebContextManager(cred, webUrl, IsCachingEnabled);
    }

    /// <summary>
    /// Adds the WebContextManager to the store so it can be retrieved later on
    /// </summary>
    internal void Store(WebContextManager mgr) {
      if (GetByInstance(mgr) == null) {
        Add(mgr);
      }
    }

  }
}
