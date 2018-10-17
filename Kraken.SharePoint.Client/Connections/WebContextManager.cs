using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using Microsoft.SharePoint.Client;
using System.Diagnostics;
using System.Net;
using Kraken.SharePoint.Client.Caching;
using Kraken.SharePoint.Cloud;
using Kraken.SharePoint.Client.Helpers;
using Kraken.Tracing;

namespace Kraken.SharePoint.Client.Connections {

  /// <summary>
  /// The WebContextManager is responsiblke for maintaining the state of a connection
  /// to an individual SharePoint web site. This is typically done by CSOM but web services
  /// also tend to establish seperate connections for seperate SPWeb sites.
  /// </summary>
  public class WebContextManager {

    private Guid _uniqueId = new Guid();

    /// <summary>
    /// Use this collection to store additional objects that you want
    /// to follow the web connection around, such as data for code
    /// that runs in AfterInit events.
    /// </summary>
    public Dictionary<string, object> ExtendedData = new Dictionary<string, object>();

    public override bool Equals(object obj) {
      WebContextManager mgr = obj as WebContextManager;
      if (mgr == null)
        return false;
      return (mgr._uniqueId == this._uniqueId);
      //return base.Equals(obj);
    }

    protected WebContextManager(bool enableCaching = true) {
      IsCachingEnabled = enableCaching;
      if (IsCachingEnabled) {
        FolderCache = new FolderCache();
        ListCache = new ListCache();
        ContentTypeCache = new ContentTypeCache();
      }
    }
    /// <summary>
    /// Provided to give New_SPOContextManager something to call upon
    /// </summary>
    /// <param name="cred"></param>
    /// <param name="webUrl"></param>
    /// <param name="enableCaching"></param>
    public WebContextManager(SharePointCredential cred, string webUrl, bool enableCaching = true)
      : this(enableCaching) {
        if (string.IsNullOrEmpty(webUrl))
          throw new ArgumentNullException("Can't create a WebContextManager without a SharePoint Web url.");
      Uri webUri = new Uri(webUrl);
      if (!webUri.IsAbsoluteUri)
        throw new InvalidOperationException("Provided Web url must be an absolute URL.");
      this.TargetWebUri = webUri;
      this.Credentials = cred;
    }
    /// <summary>
    /// If you find you need this, consider using MultiWebContextManager.TryGetOrCopy instead.
    /// </summary>
    /// <param name="copyFrom"></param>
    /// <param name="webUrl"></param>
    internal WebContextManager(WebContextManager copyFrom, string webUrl) : this(copyFrom.IsCachingEnabled && webUrl != copyFrom.TargetWebUrl) {
      copyFrom.CopyTo(this, webUrl == copyFrom.TargetWebUrl);
      // reset the context if we are now pointing to a new web site
      if (webUrl != copyFrom.TargetWebUrl) {
        Uri webUri = new Uri(webUrl);
        if (!webUri.IsAbsoluteUri)
          throw new InvalidOperationException("Provided Web url must be an absolute URL.");
        this.TargetWebUri = webUri;
      }
    }

    /// <summary>
    /// Opens a web by its ID based on the provided connection's context.
    /// Makes a copy of the provided connection manager and splices new context into it.
    /// </summary>
    /// <param name="copyFrom"></param>
    /// <param name="webId"></param>
    internal WebContextManager(WebContextManager copyFrom, Guid webId)
      : this(copyFrom.IsCachingEnabled) {
        if (copyFrom == null)
          throw new ArgumentNullException("copyFrom");
        ClientContext context = copyFrom.Context;
        Site site = context.Site;
        context.Load(site);
        Web web = site.OpenWebById(webId);
        web.LoadBasicProperties();
        Uri webUri = new Uri(web.UrlSafeFor2010());
        bool copyContext = webUri == copyFrom.TargetWebUri;
        copyFrom.CopyTo(this, copyContext);
        this.TargetWebUri = webUri;
        this.Context = (ClientContext)web.Context;
        IsConnected = (Context != null);
        if (IsConnected)
          AddToMultiClientContextManager();
    }

    public WebContextManager(ClientContext context, bool enableCaching)
      : this(enableCaching) {
      Site site = context.Site;
      context.Load(site);
      Web web = context.Web;
      web.LoadBasicProperties();
      Uri webUri = new Uri(web.UrlSafeFor2010());
      this.TargetWebUri = webUri;
      this.Context = context; // (ClientContext)web.Context;
      this.Credentials = new SharePointCredential(context.Credentials);
      IsConnected = true; // (Context != null);
      if (IsConnected)
        AddToMultiClientContextManager();
    }

    public MultiWebContextManager Parent { get; private set; }
    public Uri TargetWebUri { get; set; }
    public string TargetWebUrl { get { return TargetWebUri.ToString(); } }
    public SharePointCredential Credentials { get; protected set; }

    // TODO some are web optimized... others not so much
    public FolderCache FolderCache { get; protected set; }
    public ListCache ListCache { get; protected set; }
    public ContentTypeCache ContentTypeCache { get; protected set; }
    // TODO SiteColumnCache

    public bool IsCachingEnabled { get; private set; }
    public bool IsConnected { get; private set; }
    public bool IsInitialized { get; private set; }

    //internal ClientContext context;
    public ClientContext Context { get; private set; }
      /* get { return context; }
      set { context = value; } } */

    internal void CopyTo(WebContextManager target, bool copyContext = true) { // WebContextManager
      // authentication info - teachnically this is all that is needed
      target.Credentials = this.Credentials;
      if (copyContext) {
        // connection and context objects
        target.Context = this.Context;
        target.TargetWebUri = this.TargetWebUri;
        // conntection status
        target.IsConnected = this.IsConnected;
        target.IsInitialized = this.IsInitialized;
        // caching tied to the web url and user
        target.IsCachingEnabled = this.IsCachingEnabled;
        target.FolderCache = this.FolderCache; 
        target.ListCache = this.ListCache;
        target.ContentTypeCache = ContentTypeCache; 
      }
    }

    /// <summary>
    /// Connects to SharePoint and return generates teh necessary client context.
    /// May generate additional connection objects needed for legacy services too.
    /// </summary>
    /// <param name="forceConnection"></param>
    /// <returns></returns>
    public virtual ClientContext Connect(bool forceConnection = false) {
      // TODO support and test on prem and Office 365 connections
      if (Context == null || forceConnection) {
        IsConnected = false;
        // check the state of username and userpassword
        if (string.IsNullOrEmpty(this.TargetWebUrl))
          throw new ArgumentNullException("Can't establish credentials without SharePoint web URL.");
        WriteTrace(TraceLevel.Info, string.Format("Setting credentitals and connecting to SharePoint at web {0}...", this.TargetWebUrl));
        this.Context = new ClientContext(this.TargetWebUrl);
        ICredentials credentials = this.Credentials.GetCredential(new Uri(this.TargetWebUrl));
        if (credentials == null)
          throw new ArgumentNullException("You must specify user credentials.");
        this.Context.Credentials = credentials;
        if (credentials is SharePointCredential) {
          ((SharePointCredential)credentials).ConfigureContext(this.Context);
        }
        // TODO should we test the connection here? We're doing more in Init.
      } else {
        WriteTrace(TraceLevel.Info, "Using provided SharePoint client context");
      }
      //context.Load(context.Web, Web => Web.ServerRelativeUrl);
      //context.ExecuteQuery();
      // TODO test Conext to see if connected OK - that's partly what init is for
      IsConnected = (Context != null);
      // might it be better if we addeed the failed ones too?? probably not...
      if (IsConnected)
        AddToMultiClientContextManager();
      return Context;
    }

    /// <summary>
    /// Primes the client context to ensure the connection was indeed sucessful.
    /// Exposes authentication issues and handles that weird random 403 error thing.
    /// </summary>
    public void Init() {
      IsInitialized = false;
      ContextManagerEventArgs e = new ContextManagerEventArgs(); // TODO implement any needed custom event arguments
      if (!IsConnected || Context == null)
        throw new ArgumentNullException("context", "Cannot continue without a valid client context.");
      try {
        e.WhichAttempt = 1;
        Init_Internal(e); // DoInit(e);
      } catch (WebException ex) {
        if (ex.Message.Contains("403")) {
          WriteTrace(TraceLevel.Info, "Returned 403 from server; Trying Again...");
          WriteTrace(TraceLevel.Info, "Pausing 4 seconds...");
          System.Threading.Thread.Sleep(4000);
          try {
            this.Credentials.Validate();
          } catch (ArgumentNullException) {
            WriteTrace(TraceLevel.Info, "Can't re-login without user and password. Retrying with existing context object.");
            return;
          }
          try {
            Connect(true);
            e.WhichAttempt = 2;
            Init_Internal(e); // DoInit(e);
          } catch (Exception) {
            WriteTrace(TraceLevel.Info, "Returned error on re-try. Check your password; I give up!");
          }
        }
#if !DOTNET_V35
      } catch (IdcrlException idEx) {
        if (idEx.Message.Contains("name or password does not match")) {
        //"The sign-in name or password does not match one in the Microsoft account system."
          IsInitialized = false;
          throw new SecurityException("Access denied. Username or password does not match.", idEx);
        } else if (idEx.Message.Contains("could not look up the realm information")) {
          //"Identity Client Runtime Library (IDCRL) could not look up the realm information for a federated sign-in."
          IsInitialized = false;
          throw new SecurityException("Access denied. Unknown user domain.", idEx);
        }
#endif
      } catch (Exception ex2) {
        WriteTrace(TraceLevel.Error, string.Format("Unexpected exception in initialization. {0}: {1}", ex2.Message, ex2.StackTrace));
        throw ex2;
      }
      // fire custom events such as library setup
      // note that exceptions in attached events can cause IsInitialized to not be set
      if (AfterInit != null)
        AfterInit(this, e);
      IsInitialized = true;
      AddToMultiClientContextManager();
    }

    protected void AddToMultiClientContextManager() {
      MultiWebContextManager mgr = MultiWebContextManager.Current;
      mgr.Store(this);
      this.Parent = mgr;
    }

    public event ContextManagerEventHandler AfterInit;

    /// <summary>
    /// Checks to see if TargetWebUri/TargetWebUrl is set and if not it tries to get it from the current context
    /// </summary>
    protected void EnsureWebUrl() {
      if (string.IsNullOrEmpty(this.TargetWebUrl)) {
        this.TargetWebUri = new Uri(this.Context.Web.ServerRelativeUrl);
        WriteTrace(TraceLevel.Verbose, string.Format("Set TargetWebUrl '{0}' from context...", this.TargetWebUrl));
      } else {
        WriteTrace(TraceLevel.Verbose, string.Format("Web URL is '{0}'", this.TargetWebUrl));
      }
    }
    protected void EnsureCredentials() {
      if (this.Credentials == null)
        throw new ArgumentNullException("No credential specified and we can't convert this.Context.Credentials yet.");
        //this.Credentials = new SharePointCredential(this.Context.Credentials);
    }

    private void Init_Internal(ContextManagerEventArgs e) {
      WriteTrace(TraceLevel.Verbose, "Getting basic web properties...");
      // Renders the output to the screen
      Context.Web.LoadBasicProperties(true, true);
      // common Office 365 problem above: WebException "The remote server returned an error: (403) Forbidden."
    }

    /// <summary>
    /// Call this at the beginning of an operation to ensure that everything is set up OK
    /// </summary>
    /// <param name="doConnectAndInit"></param>
    public void EnsureContext(bool doConnect) {
      if (doConnect && !IsConnected)
        Connect();
      if (!IsInitialized) // last chance to try
        Init();
      if (!IsInitialized)
        throw new InvalidOperationException("Not initialized. Can't continue.");
      if (Context == null)
        throw new ArgumentNullException("context", "Cannot continue without a valid client context.");
    }

    public SharePointConnection CreateLegacyConnection(bool doConnect, bool doInitialize) {
      this.Credentials.Validate(true);
      SharePointConnection connection = new SharePointConnection() {
        Url = this.TargetWebUri,
        LoginUser = this.Credentials.UserName,
        LoginPassword = this.Credentials.UserPassword
      };
      // TODO consolidate this so they use the same class model
      switch (this.Credentials.AuthType) {
        case ClientAuthenticationType.SPOCredentials:
          connection.AuthenticationType = Cloud.Authentication.SharePointAuthenticationType.Office365Login;
          break;
        case ClientAuthenticationType.SharePointNTLMCurrentUser:
          connection.AuthenticationType = Cloud.Authentication.SharePointAuthenticationType.CurrentWindowsUser;
          break;
        case ClientAuthenticationType.SharePointNTLMUserPass:
          connection.AuthenticationType = Cloud.Authentication.SharePointAuthenticationType.SpecifyWindowsUser;
          break;
        default:
          throw new NotImplementedException(string.Format("A connection.AuthenticationType for '{0}' is not implemented.", this.Credentials.AuthType));
      }
      if (doConnect)
        connection.Connect();
      if (doInitialize)
        connection.WebsManager.EnsureSiteColumnsAndContentTypes();
      return connection;
    }

    #region ITrace delegate pattern

    public ITrace TraceWriter {
      get;
      set;
    }

    protected virtual void WriteTrace(TraceLevel level, string format, params object[] args) {
      if (TraceWriter == null)
        return;
      TraceWriter.Trace(level, format, args);
    }

    #endregion

    internal string GenerateUniqueKey() {
      string user = string.Empty;
      if (this.Credentials != null)
        user = this.Credentials.UserName;
      return GenerateUniqueKey(user, this.TargetWebUrl);
    }

    internal static string GenerateUniqueKey(WebContextManager copyFrom, string webUrl) {
      return GenerateUniqueKey(copyFrom.Credentials.UserName, webUrl);
    }
    internal static string GenerateUniqueKey(string user, string webUrl) {
      return user + "|" + webUrl;
    }
    internal static string GenerateUniqueKey(ICredentials cred, string webUrl) {
      string user = cred.GetUserName();
      return GenerateUniqueKey(user, webUrl);
    }
    internal static string GenerateUniqueKey(ClientContext context) {
      string webUrl = context.Url;
      return GenerateUniqueKey(context.Credentials, webUrl);
    }

    /// <summary>
    /// Creates an HttpWebRequest that can be used to make calls to SharePoint pages.
    /// </summary>
    /// <param name="pageUrl"></param>
    /// <returns></returns>
    public HttpWebRequest CreateExecutorWebRequest(string pageUrl) {
      HttpWebRequest request = this.Context.WebRequestExecutorFactory.CreateWebRequestExecutor(this.Context, pageUrl).WebRequest;
      SharePointCredential spCred = this.Credentials as SharePointCredential;
      // TODO this should really be based on 
      if (spCred != null) {
        switch(spCred.AuthType) {
          case ClientAuthenticationType.SPOCredentials:
            request.CookieContainer = spCred.CreateSharePointOnlineCookies(this.Context);
            break;
          case ClientAuthenticationType.SharePointNTLMCurrentUser:
            request.UseDefaultCredentials = true;
            break;
          default:
            throw new NotImplementedException(string.Format("CreateWebRequestExecutor is not yet implemented for authentication type {0}.", spCred.AuthType));
        }
      } else
        request.UseDefaultCredentials = true;
      request.ContentLength = 0;
      return request;
    }

  }

  public delegate void ContextManagerEventHandler(object sender, ContextManagerEventArgs e);

  public class ContextManagerEventArgs : EventArgs {
    public bool IsConnected;
    public bool IsInitialized;
    public string ErrorMessage;
    public int WhichAttempt = 0;
  }

}
