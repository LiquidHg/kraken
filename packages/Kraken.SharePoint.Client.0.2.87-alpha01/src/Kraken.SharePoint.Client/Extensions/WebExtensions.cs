namespace Microsoft.SharePoint.Client {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Net;
  using System.IO;
  using System.Text.RegularExpressions;

#if !DOTNET_V35
  using Microsoft.SharePoint.Client.Taxonomy;
  //using Microsoft.SharePoint.Client.DocumentSet;
#endif

  using Kraken.SharePoint.Client;
  using Kraken.SharePoint.Client.Caching;
  using Kraken.SharePoint.Client.Connections;
  using Kraken.Net;
  using Kraken.SharePoint.Client.Helpers;
  using Kraken.Tracing;

  public static class KrakenWebExtensions {

    #region Basic

    /// <summary>
    /// Get the URL property whether it exists or not
    /// </summary>
    /// <param name="web"></param>
    /// <returns></returns>
    public static string UrlSafeFor2010(this Web web) {
      string url = string.Empty;
      try {
        ClientContext context = (ClientContext)web.Context;
#if !DOTNET_V35
        web.EnsureProperty(null, e => e.Url);
#else
				web.EnsureProperty(null, e => e.ServerRelativeUrl);
#endif
        url = Url(web, context.IsSP2013AndUp());
      } catch (ServerException ex) { // ex.Message == "Field or property "Url" does not exist."
        url = Url(web, false);
      }
      return url;
    }

#if DOTNET_V35
    public static string Url(this Web web, bool isSP2013OrBetter = false) {
      ClientContext context = (ClientContext)web.Context;
      string url = Utils.MakeFullUrl(context, web.ServerRelativeUrl);
      return url;
    }
#else
    public static string Url(this Web web, bool isSP2013OrBetter = true) {
      string url = string.Empty;
      if (isSP2013OrBetter) {
        url = web.Url;
      } else {
        ClientContext context = (ClientContext)web.Context;
        //context.Load(context.Site, s => s.Url);
        //context.ExecuteQueryIfNeeded();
        url = Utils.MakeFullUrl(context, web.ServerRelativeUrl);
      }
      return url;
    }
#endif

    public static void LoadBasicProperties(this Web web, bool execute = true, ITrace trace = null) {
      try {
        ClientContext context = (ClientContext)web.Context;
        web.LoadBasicProperties(context.IsSP2013AndUp(), execute, trace);
      } catch (ServerException ex) { // ex.Message == "Field or property "Url" does not exist."
        if (trace == null) trace = NullTrace.Default;
        trace.TraceError(ex);
        trace.TraceVerbose("Falling back on legacy (SP2010) method for web.Url");
        web.LoadBasicProperties(false, execute, trace);
      }
    }
    public static void LoadBasicProperties(this Web web, bool isSP2013OrBetter, bool execute = true, ITrace trace = null) {
      if (trace == null)
        trace = NullTrace.Default;
      ClientContext context = (ClientContext)web.Context;
#if !DOTNET_V35
      if (isSP2013OrBetter) {
        context.Load(web, w => w.Url, w => w.Id, w => w.ServerRelativeUrl, w => w.Title);
      } else {
#endif
        context.Load(web, w => w.Id, w => w.ServerRelativeUrl, w => w.Title);
        context.Load(context.Site, s => s.Url);
#if !DOTNET_V35
      }
#endif
      if (execute) {
        context.ExecuteQueryIfNeeded();
        trace.TraceVerbose("LoadBasicProperties: web ID = '{0}'", web.Id);
        trace.TraceVerbose("LoadBasicProperties: web.Url = '{0}'", web.UrlSafeFor2010());
      }
    }

    #endregion

    #region Folders

    public static IEnumerable<Folder> GetFoldersAtTopLevel(this Web web) {
      ClientContext context = (ClientContext)web.Context;
      FolderCollection folders = web.RootFolder.Folders;
      IEnumerable<Folder> existingFolders = context.LoadQuery(
        folders.Include(folder => folder.ServerRelativeUrl)
      );
      context.ExecuteQuery();
      return existingFolders;
    }

    #region GetFolder

    /// <summary>
    /// Tries to get a folder from a list using its name.
    /// Calls underlying extension method list.GetFolder.
    /// </summary>
    /// <param name="web"></param>
    /// <param name="listTitle"></param>
    /// <param name="folderName"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public static Folder GetFolder(this Web web, string listTitle, string folderName, bool ignoreCase) {
      List list;
      if (!web.TryGetList(listTitle, out list, ignoreCase) || list == null)
        return null;
      return list.GetFolder(folderName, ignoreCase);
    }

    public static Folder GetFolder(this Web web, Uri serverRelativeUrl) {
      if (serverRelativeUrl.IsAbsoluteUri)
        throw new ArgumentException("A server relative Url (starts with the leading '/' immediately after the hostname and port) is required. ", "serverRelativeUrl");
      return web.GetFolder(serverRelativeUrl.ToString());
    }

    /// <summary>
    /// Get a folder from Web object using server relative URL.
    /// Performs a treatment on the url string so that if it doesn't
    /// start with a '/' the web's serverRelativeUrl will be prepended.
    /// Executes query and does load (init) of the resulting folder object.
    /// </summary>
    /// <param name="web">CSOM Web object</param>
    /// <param name="serverRelativeUrl">Example: "/sites/web1/library1/subfolder1/subfolder2"</param>
    /// <returns>Null if not found, otherwise Folder object</returns>
    [System.Diagnostics.DebuggerNonUserCode]
    public static Folder GetFolder(this Web web, string serverRelativeUrl) {
      ClientContext context = (ClientContext)web.Context;
      if (string.IsNullOrEmpty(serverRelativeUrl) && !serverRelativeUrl.StartsWith("/"))
        serverRelativeUrl = string.Format("{0}/{1}", web.RootFolder.ServerRelativeUrl, serverRelativeUrl);
      if (string.IsNullOrEmpty(serverRelativeUrl))
        serverRelativeUrl = web.RootFolder.ServerRelativeUrl;
      Folder folder = null;
      try {
        folder = web.GetFolderByServerRelativeUrl(serverRelativeUrl);
        if (folder != null) {
          //context.Load(folder);
          folder.EnsureProperty(null, f => f.ServerRelativeUrl);
        }
      } catch (ServerException ex) {
        //if (ex.Message.Equals("File Not Found.", StringComparison.InvariantCultureIgnoreCase))
        if (ex.ServerErrorTypeName == "System.IO.FileNotFoundException")
          return null;
        if (ex.Message == "Unknown Error") {
          // did you mean to get a file and got a folder instead?
          return null;
        }
        throw;
      }
      return folder;
    }

    #endregion

    /// <summary>
    /// Tries to get a folder from Web object using server relative URL.
    /// Calls underlying extension method web.GetFolder and has same behaviors.
    /// </summary>
    /// <param name="web"></param>
    /// <param name="serverRelativeUrl">Example: "/sites/web1/library1/subfolder1/subfolder2"</param>
    /// <param name="folder"></param>
    /// <returns></returns>
    public static bool TryGetFolder(this Web web, string serverRelativeUrl, out Folder folder) {
      var ctx = web.Context;
      try {
        folder = web.GetFolder(serverRelativeUrl);
        return true;
      } catch (Microsoft.SharePoint.Client.ServerException ex) {
        if (ex.ServerErrorTypeName == "System.IO.FileNotFoundException") {
          folder = null;
          return false;
        } else
          throw;
      }
    }
    public static bool TryGetFolder(this Web web, Uri serverRelativeUrl, out Folder folder) {
      if (serverRelativeUrl.IsAbsoluteUri)
        throw new ArgumentException("A server relative Url (starts with the leading '/' immediately after the hostname and port) is required. ", "serverRelativeUrl");
      return web.TryGetFolder(serverRelativeUrl.ToString(), out folder);
    }

    #endregion

    #region Files

    public static bool TryGetFile(this Web web, string serverRelativeUrl, out Microsoft.SharePoint.Client.File file) {
      var ctx = web.Context;
      try {
        file = web.GetFileByServerRelativeUrl(serverRelativeUrl);
        file.EnsureProperty(null);
        return true;
      } catch (Microsoft.SharePoint.Client.ServerException ex) {
        if (ex.ServerErrorTypeName == "System.IO.FileNotFoundException") {
          file = null;
          return false;
        } else
          throw;
      }
    }

    #endregion

    #region DocLibs

    public static List CreateDocumentLibrary(this Web web, string listTitle) {
      return web.CreateList(listTitle, string.Empty, true, ListTemplateType.DocumentLibrary);
    }

    #endregion

    #region Lists

    /// <summary>
    /// The ultimate wrapper method. Will attempt to get a list by
    /// server relative Url, root folder name, or title.
    /// </summary>
    /// <param name="web"></param>
    /// <param name="listUrlTitleOrName"></param>
    /// <param name="list"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public static bool TryGetList(this Web web, string listUrlTitleOrName, out List list, bool ignoreCase = true) {
      var ctx = web.Context;
      try {
        Guid listId;
#if !DOTNET_V35
        if (Guid.TryParse(listUrlTitleOrName, out listId)) {
#else
        bool isGuid = false; listId = Guid.Empty;
        try {
          listId = new Guid(listUrlTitleOrName);
          isGuid = true;
        } catch { }
        if (isGuid) {
#endif
          // TODO but a list by ID can come from anywhere in the site collection
          list = web.Lists.GetById(listId);
          /* older versions of CSOM didn't implement web.GetList */
#if !DOTNET_V35
          /*
          } else if (listUrlTitleOrName.StartsWith("/")) {
            // the query in GetByTitleOrName should work to perform same thing as 
            //string tryByUrl = web.ServerRelativeUrl + "/" + listUrlTitleOrName;
            */
        } else if (listUrlTitleOrName.Contains("/")) {
          list = web.GetList(listUrlTitleOrName);
          /*
					ctx.Load(
							list => list.IncludeBasicProperties()
					);
					 */
#endif
        } else {
          list = web.Lists.GetByTitleOrName(listUrlTitleOrName, ignoreCase);
          // the query in GetByTitleOrName should work to perform same thing as 
          //string tryByUrl = web.ServerRelativeUrl + "/" + listUrlTitleOrName;
        }
        if (list != null) {
          list.EnsureProperty(null);
          return true;
        } else {
          return false;
        }
      } catch (Microsoft.SharePoint.Client.ServerException ex) {
        if (ex.Message.Contains("Value does not fall within the expected range")
          || ex.ServerErrorTypeName == "System.IO.FileNotFoundException") {
          list = null;
          return false;
        } else
          throw;
      }
    }

    public static List CreateList(this Web web, ListOptions props, WebContextManager cm = null, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      if (props == null)
        throw new ArgumentNullException("props");
      props.Validate((ClientContext)web.Context, true, true, trace);
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)web.Context;
      List list = null;
      if (web.TryGetList(props.Title, out list)) {
        if (props.ThrowOnError)
          throw new ArgumentException(string.Format("A list named '{0}' already exists at web '{1}'.", props.Title, web.UrlSafeFor2010()), "listTitle");
        return list;
      }
      // this was moved under props.Validate / props.DoConversions
      /*
      if (props.TemplateTypeDefined.HasValue) {
        props.TemplateType = (Int32)props.TemplateTypeDefined.Value;
      } else if (props.HasChangedValue(props.TemplateTypeCustom)) {
        context.Load(web, w => w.ListTemplates);
        context.ExecuteQuery();
        ListTemplate listTemplate = web.ListTemplates.FirstOrDefault(lt => lt.Name == props.TemplateTypeCustom);
        if (listTemplate == null) {
          if (props.ThrowOnError)
            throw new ArgumentOutOfRangeException(string.Format("List template with name '{0}' does not exist in web '{1}'.", props.TemplateTypeCustom, web.UrlSafeFor2010()), "TemplateTypeCustom");
          return null;
        }
        props.TemplateFeatureId = listTemplate.FeatureId;
        props.TemplateType = listTemplate.ListTemplateTypeKind;
      } else if (props.TemplateType <= 0) {
        if (props.ThrowOnError)
          throw new ArgumentNullException(string.Format("You must specify a pre-defined or custom list template."), "TemplateTypeDefined or TemplateTypeCustom");
        return null;
      }
      */

      // ensure that SKIP doesn't get saved as a description
      if (!props.HasChangedValue(props.Description))
        props.Description = string.Empty;
      string title = string.Empty;
      if (props.HasChangedValue(props.RootFolderName, props.Title)) {
        trace.TraceVerbose("Root folder name specified, saving title for update step.");
        title = props.Title;
        props.Title = props.RootFolderName;
      }
      trace.TraceVerbose("Creating list {0} [{1}]", props.Title, props.RootFolderName);
      try {
        ListCreationInformation lci = props.ConvertSP14Safe();
        list = web.Lists.Add(lci);
        context.ExecuteQuery();
      } catch (Exception ex) {
        string msg = string.Format("Couldn't create List '{0}' at '{1}'", props.Title, web.UrlSafeFor2010());
        trace.TraceWarning(msg);
        trace.TraceError(ex);
        if (props.ThrowOnError) {
          throw new Exception(msg, ex);
        }
      }
      if (list != null) {
        trace.TraceVerbose("Updating extended list properties...");
        if (string.IsNullOrEmpty(title))
          props.Title = title;
        list.Update(props, !string.IsNullOrEmpty(title), cm, trace);
      }
      return list;
    }

    public static List CreateList(this Web web, string listTitle, string description, bool isQuickLaunch, ListTemplateType template, bool throwIfExists = true) {
      ListOptions options = new ListOptions() {
        Title = listTitle,
        Description = description,
        OnQuickLaunch = isQuickLaunch,
        TemplateTypeDefined = template,
        ThrowOnError = throwIfExists
      };
      return CreateList(web, options);
    }
    public static List CreateList(this Web web, string listTitle, string description, bool isQuickLaunch, string customTemplateName, bool throwIfExists = true) {
      ListOptions options = new ListOptions() {
				Title = listTitle,
				Description = description,
        OnQuickLaunch = isQuickLaunch,
        TemplateTypeCustom = customTemplateName,
        ThrowOnError = throwIfExists
			};
      return CreateList(web, options);
    }

#endregion

    #region Content Types

		/// <summary>
    /// 
    /// </summary>
    /// <param name="web"></param>
    /// <param name="contentTypeNameOrId"></param>
    /// <returns></returns>
    public static ContentType GetContentType(this Web web, string contentTypeNameOrId) {
      ClientContext context = (ClientContext)web.Context;
      context.LoadIfRequired(web, new string[] { "ContentTypes" });
      return web.ContentTypes.GetByNameOrId(contentTypeNameOrId);
    }

    public static IQueryable<ContentType> GetDefaultProperties(this IQueryable<ContentType> ctq) {
      return ctq.Include(type => type.Id, type => type.Name, type => type.Group, type => type.SchemaXml);
    }

    public static ContentType GetContentType(this Web web, string contentTypeName, WebContextManager cm = null, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      web.LoadBasicProperties(true, false, trace);
      ContentTypeCache ctc = (cm == null) ? null : cm.ContentTypeCache;
      if (ctc != null)
        trace.TraceVerbose("Getting content type from web cache...");
      // get the web content type from the current web or from web CT cache if it is available
      ContentType webContentType = (ctc != null)
        ? ctc.GetByName(web, contentTypeName, false)
        : web.GetContentType(contentTypeName);
      return webContentType;
    }

    /*
    public static TResult RecurseAllParentWebs<TParam1, TParam2, TResult>(Web web, WebContextManager cm, TParam1 param1, TParam2 param2, Func<Web, WebContextManager, TParam1, TParam2, TResult> func) {
      if (cm == null)
        throw new InvalidOperationException("You must provide a client context manager object when using recurseAllParentWebs.");
      Web webLoop = web;
      bool isRoot = false;
      // TODO safety valve at root site
      while (!isRoot) {
        // get the context of the parent web site
        WebInformation parent = null;
        if (!webLoop.ParentWeb.IsNull()) {
          webLoop.Context.Load(webLoop, t => t.ParentWeb);
          webLoop.Context.ExecuteQueryIfNeeded();
          parent = webLoop.ParentWeb;
          webLoop.Context.Load(parent, t => t.ServerRelativeUrl);
          webLoop.Context.ExecuteQueryIfNeeded();
        }
        if (parent != null && !parent.IsNull()) {
          // disable the tracking of recently used context managers
          // because it is very likely this routine will need to connect
          // to a parent context and that could mess everything up later
          MultiWebContextManager.Current.SuppressRecentUseTracking = true;
          {
            string newUrl = cm.TargetWebUri.GetLeftPart(UriPartial.Authority) + parent.ServerRelativeUrl;
            WebContextManager subCm = MultiWebContextManager.Current.TryGetOrCopy(cm, newUrl); // new WebContextManager(cm, newUrl);
            subCm.EnsureContext(true); // we may need to connect here if this is our first trip to this sub-web
            webLoop = subCm.Context.Web;
            // TODO for whatever reason the content type cache does not appear to work here
            TResult result = func(webLoop, subCm, param1, param2);
          }
          MultiWebContextManager.Current.SuppressRecentUseTracking = false;
        } else {
          isRoot = true;
        }
      }
    }
     */

    /// <summary>
    /// 
    /// </summary>
    /// <param name="web"></param>
    /// <param name="contentTypeName"></param>
    /// <param name="recurseAllParentWebs"></param>
    /// <param name="ctc"></param>
    /// <returns></returns>
    public static ContentType GetContentType(this Web web, string contentTypeName, bool recurseAllParentWebs = true, WebContextManager cm = null, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      if (recurseAllParentWebs && cm == null)
        throw new InvalidOperationException("You must provide a client context manager object when using recurseAllParentWebs.");
      //ContentTypeCache ctc = (cm == null) ? null : cm.ContentTypeCache;
      ContentType webContentType = web.GetContentType(contentTypeName, cm, trace);
      if (webContentType != null)
        return webContentType;
      // the web CT wasn't found, so let's check the root or parent web site 
      // TODO make a an action called RecurseAllParentWebs
      if (recurseAllParentWebs) {
#if DOTNET_V35
        throw new NotSupportedException("Sorry, but recurseAllParentWebs relies on web.ParentWeb which is not implemented in this version of CSOM. You will need to open each parent web individually.");
#else
        Web webLoop = web;
        bool isRoot = false;
        // TODO safety valve at root site
        while (webContentType == null && !isRoot) {
          // get the context of the parent web site
          WebInformation parent = null;
          if (!webLoop.ParentWeb.IsNull()) {
            webLoop.Context.Load(webLoop, t => t.ParentWeb);
            webLoop.Context.ExecuteQueryIfNeeded();
            parent = webLoop.ParentWeb;
            webLoop.Context.Load(parent, t => t.ServerRelativeUrl);
            webLoop.Context.ExecuteQueryIfNeeded();
          }
          if (parent != null && !parent.IsNull()) {
            // disable the tracking of recently used context managers
            // because it is very likely this routine will need to connect
            // to a parent context and that could mess everything up later
            MultiWebContextManager.Current.SuppressRecentUseTracking = true;
            {
              string newUrl = cm.TargetWebUri.GetLeftPart(UriPartial.Authority) + parent.ServerRelativeUrl;
              WebContextManager subCm = MultiWebContextManager.Current.TryGetOrCopy(cm, newUrl); // new WebContextManager(cm, newUrl);
              subCm.EnsureContext(true); // we may need to connect here if this is our first trip to this sub-web
              webLoop = subCm.Context.Web;
              // TODO for whatever reason the content type cache does not appear to work here
              webContentType = webLoop.GetContentType(contentTypeName, subCm, trace);
            }
            MultiWebContextManager.Current.SuppressRecentUseTracking = false;
          } else {
            isRoot = true;
          }
        }
#endif
      } else {
        trace.TraceVerbose("Getting content type from ROOT site web cache...");
        ClientContext context = (ClientContext)web.Context;
        webContentType = context.Site.RootWeb.GetContentType(contentTypeName, cm, trace); 
      }
      return webContentType;
    }

    public static List<ContentType> GetContentTypesInGroup(this Web web, string groupName, bool executeQuery = true) { // , WebContextManager cm = null
      // TODO support content type cache for this method
      // TODO support crawl parent webs for this method
      ClientContext context = (ClientContext)web.Context;
      context.Load(web, w => w.ContentTypes);
      IEnumerable<ContentType> result = web.ContentTypes.GetByGroup(groupName, executeQuery);
      //IEnumerable<ContentType> result = context.LoadQuery(web.ContentTypes.Where(c => c.Group == groupName).GetDefaultProperties());
      return result.ToList();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="web"></param>
    /// <param name="parent">Optional parent content type; if provided with ctid it will be used to check the id is valid. If provided without ctid it will be passed to CSOM to assign a new ID automatically.</param>
    /// <param name="ctid">The requested content type id in 0x... format for the new content type.</param>
    /// <param name="name">The display name of the new content type</param>
    /// <param name="group">The group name of the new content type</param>
    /// <param name="description">An optional description for the new content type</param>
    /// <param name="isHidden"></param>
    /// <param name="isReadOnly"></param>
    /// <param name="isSealed"></param>
    /// <param name="ctxMgr"></param>
    /// <returns></returns>
    public static ContentType AddContentType(this Web web,
      ContentType parent, string ctid, string name, string group, string description = "", bool isHidden = false, bool isReadOnly = false, bool isSealed = false,
      WebContextManager ctxMgr = null) {
      ContentTypeProperties properties = new ContentTypeProperties() {
        Description = description,
        Group = group,
        Hidden = isHidden,
        Id = ctid,
        Name = name,
        ParentContentType = parent,
        ReadOnly = isReadOnly,
        Sealed = isSealed
      };
      return web.ContentTypes.AddContentType(properties, ctxMgr);
    }

    public static ContentType AddContentType(this Web web,
      ContentTypeProperties properties,
      WebContextManager ctxMgr = null) {
      return web.ContentTypes.AddContentType(properties, ctxMgr);
    }

#endregion

    #region Site Columns

    /// <summary>
    /// Gets a site column from the web site, using one of several methods.
    /// This overload does not implement the site column cache.
    /// </summary>
    /// <param name="web"></param>
    /// <param name="identifier">The internal name or title of the site column</param>
    /// <param name="searchByTitle">If true, search by Title instead of InternalName</param>
    /// <returns></returns>
    public static Field GetSiteColumn(this Web web, string identifier, FieldFindMethod findMethod = FieldFindMethod.Any) {
      return web.Fields.GetField(identifier, findMethod);
    }
    public static Field GetSiteColumn(this Web web, Guid uniqueId) {
      return web.Fields.GetField(uniqueId);
    }

    /// <summary>
    /// Gets the requested site columns, and potentially from
    /// parent webs as well.
    /// </summary>
    /// <param name="web"></param>
    /// <param name="recursParentWebs"></param>
    /// <param name="excludeBuiltInFields"></param>
    /// <param name="trace"></param>
    /// <returns>A List<Field> object with all the erequested fields</returns>
    public static IEnumerable<Field> GetSiteColumns(this Web web, bool recursParentWebs = true, bool excludeBuiltInFields = false, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      trace.Enter(System.Reflection.MethodBase.GetCurrentMethod());
      ClientContext context = (ClientContext)web.Context;
      web = context.Web;
      List<Field> allResults = new List<Field>();
      while (web != null) {
        trace.TraceVerbose("Querying web fields...");
        IEnumerable<Field> result = web.Fields.GetAllFields(excludeBuiltInFields);
        allResults.AddRange(result);
        trace.TraceVerbose("Done. {0} fields added.", result.Count());
        web = (recursParentWebs) ? web.GetParentWeb(trace) : null;
        if (web != null)
          trace.TraceVerbose("Recursing parent web.");
      } // end while
      trace.Exit(System.Reflection.MethodBase.GetCurrentMethod());
      return allResults;
    }

    public static IEnumerable<Field> GetSiteColumnsInGroup(this Web web, string groupName, bool recursParentWebs = true, bool excludeBuiltInFields = false, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      trace.Enter(System.Reflection.MethodBase.GetCurrentMethod());
      ClientContext context = (ClientContext)web.Context;
      web = context.Web;
      List<Field> allResults = new List<Field>();
      while (web != null) {
        trace.TraceVerbose("Querying web fields...");
        IEnumerable<Field> result = web.Fields.GetFieldsByGroup(groupName, excludeBuiltInFields);
        allResults.AddRange(result);
        trace.TraceVerbose("Done. {0} fields added.", result.Count());
        web = (recursParentWebs) ? web.GetParentWeb(trace) : null;
        if (web != null)
          trace.TraceVerbose("Recursing parent web.");
      } // end while
      trace.Exit(System.Reflection.MethodBase.GetCurrentMethod());
      return allResults;
    }

    public static Field GetSiteColumn(this Web web, string siteColumnName, FieldFindMethod findMethod = FieldFindMethod.Any, WebContextManager contextManager = null, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      //object scc = (contextManager == null) ? null : contextManager.SiteColumnCache;
      //trace.TraceVerbose("Getting site column from web cache...");
      web.LoadBasicProperties(true, false, trace);
      // get the web content type from the current web or from web CT cache if it is available
      Field webField = /* (scc != null)
        //? scc.(web, siteColumnName, false)
        : */ web.GetSiteColumn(siteColumnName, findMethod);
      return webField;
    }

    // TODO implement site column caching
    // TODO recurse parent webs can be simplified here
    public static Field GetSiteColumn(this Web web, string siteColumnName, FieldFindMethod findMethod = FieldFindMethod.Any, bool recurseAllParentWebs = true, WebContextManager contextManager = null, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      if (recurseAllParentWebs && contextManager == null)
        throw new InvalidOperationException("You must provide a client context manager object when using recurseAllParentWebs.");
      Field webField = web.GetSiteColumn(siteColumnName, findMethod, contextManager, trace);
      // the web CT wasn't found, so let's check the root or parent web site 
      if (recurseAllParentWebs) {
#if DOTNET_V35
        throw new NotSupportedException("Sorry, but recurseAllParentWebs relies on web.ParentWeb which is not implemented in this version of CSOM. You will need to open each parent web individually.");
#else
        ClientContext context = (ClientContext)web.Context;
        Web webLoop = web;
        context.Load(context.Site, t => t.Id, t => t.Url, t => t.ServerRelativeUrl);
        context.Load(context.Site, t => t.RootWeb.Id);
        //context.Load(context.Site, t => t.RootWeb.UrlSafeFor2010());
        // TODO safety valve at root site
        context.Load(webLoop, t => t.Id);
        context.ExecuteQueryIfNeeded();
        Guid siteRootId = context.Site.RootWeb.Id;
        string siteRootUrl2 = context.Site.RootWeb.UrlSafeFor2010();
        string siteRootUrl = context.Site.Url.Replace(context.Site.ServerRelativeUrl, string.Empty);
        while (webField == null && webLoop.Id != siteRootId) {
          // get the context of the parent web site
          context.Load(webLoop.ParentWeb, t => t.ServerRelativeUrl);
          context.ExecuteQueryIfNeeded();
          // when we try to do this with "normal" code, we get a 403
          //context = new ClientContext(siteRootUrl + webLoop.ParentWeb.ServerRelativeUrl);
          string newWebUrl = siteRootUrl + webLoop.ParentWeb.ServerRelativeUrl;
          // TODO determine if this needs to be Info or is Verbose OK?
          trace.TraceInfo("Connecting to '{0}'.", newWebUrl);
          WebContextManager cm2 = new WebContextManager(contextManager, newWebUrl);
          context = null;
          context = cm2.Connect(); // replaces previous context
          webField = context.Web.GetSiteColumn(siteColumnName, findMethod, contextManager, trace);
          webLoop = context.Web;
        }
        // regenerate the field in the current web context instead
        if (webField != null) {
          ClientContext newContext = (ClientContext)web.Context;
          Web newWebSameContext = newContext.Site.OpenWebById(webLoop.Id);
          webField = newWebSameContext.GetSiteColumn(siteColumnName, findMethod, contextManager, trace);
        }
#endif
      } else {
        trace.TraceVerbose("Getting site column from ROOT site web cache...");
        ClientContext context = (ClientContext)web.Context;
        webField = context.Site.RootWeb.GetSiteColumn(siteColumnName, findMethod, contextManager, trace);
      }
      return webField;
    }

    public static void UpdateSiteColumn(this Web web, Field existingField, string schemaXml, bool pushToLists = true, bool execute = true) {
      // TODO make sure field is in web
      existingField.Update(schemaXml, pushToLists, execute);
    }
    public static void UpdateSiteColumn(this Web web, Field existingField, FieldProperties properties, bool execute = true, ITrace trace = null) {
      // TODO make sure field is in web
      existingField.Update(properties, execute, trace);
    }

    public static Field AddSiteColumn(this Web web, string schemaXml, bool execute = true) {
      return web.Fields.Add(schemaXml, execute);
    }

    public static Field AddSiteColumn(this Web web, FieldProperties properties, bool execute = true, ITrace trace = null) {
      return web.Fields.Add(properties, execute, trace);
    }


    /// <summary>
    /// Creates a collection of FieldProperties
    /// for many fields, using LookupFieldProvisioner
    /// to populate additional field info.
    /// </summary>
    /// <param name="web"></param>
    /// <param name="groupName"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    public static IEnumerable<FieldProperties> GetFieldPropertiesList(
      this Web web,
      string groupName = "",
      bool recursParentWebs = true,
      bool excludeBuiltInFields = false,
      ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      IEnumerable<Field> fields =
          (string.IsNullOrEmpty(groupName))
          ? web.GetSiteColumns(recursParentWebs, excludeBuiltInFields, trace)
          : web.GetSiteColumnsInGroup(groupName, recursParentWebs, excludeBuiltInFields, trace);
      ClientContext context = (ClientContext)web.Context;
      LookupFieldProvisioner lookupFieldProvisioner = new LookupFieldProvisioner(context, trace);
      IEnumerable<FieldProperties> ret = lookupFieldProvisioner.CreateFieldPropertiesList(fields);
      return ret;
    }

    #endregion

    public static Web GetParentWeb(this Web web, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      ClientContext ctx = (ClientContext)web.Context;
      if (web != null
        && web.ParentWeb != null
        && !web.ParentWeb.ServerObjectIsNull.GetValueOrDefault()) {
        ctx.Load(web,
          w => w.ParentWeb.ServerRelativeUrl,
          w => w.ParentWeb.Id);
        ctx.ExecuteQueryIfNeeded();
        trace.TraceVerbose("Searching parent web {0}", web.ParentWeb.ServerRelativeUrl);
        return ctx.Site.OpenWebById(web.ParentWeb.Id);
      } else {
        return null;
      }
    }

    #region Sandbox Solutions

    /// <summary>
    /// 
    /// </summary>
    /// <param name="solutionFile">The solution file with at least its ID field</param>
    /// <param name="activate">True to activate, false to deactivate</param>
    /// <returns></returns>
    public static bool ActivateOrDeactivateSandboxSolution(this Microsoft.SharePoint.Client.File solutionFile, bool activate, WebContextManager contextManager) {
      if (contextManager == null)
        throw new ArgumentNullException("contextManager");
      int solutionId = solutionFile.ListItemAllFields.Id;
      string operation = (activate) ? "ACT" : "DEA";

      string slnPageUrl = string.Format("{0}/_catalogs/solutions/forms/activate.aspx?Op={1}&ID={2}", contextManager.TargetWebUrl, operation, solutionId);
      HttpWebRequest request = contextManager.CreateExecutorWebRequest(slnPageUrl);

      // gets all the input tags from the page HTML
		  // add them to a dictionary used for post
      Dictionary<string, string> inputs = new Dictionary<string, string>();
      using (WebResponse response = request.GetResponse()) {
        // decompress web response headers where needed
        Stream stream = response.GetStreamAndDecompressIfNeeded();
        string responseText = string.Empty;
        using (StreamReader sr = new StreamReader(stream)) {
          responseText = sr.ReadToEnd();
          sr.Close();
        }
        stream.Close();

        Regex regex = new Regex(@"<input.+?\/??>", RegexOptions.IgnoreCase);
        MatchCollection matches = regex.Matches(responseText);
        foreach (Match match in matches) {
          // wow -match is MUCH simpler!
          Regex regex2 = new Regex(@"name=\""(.+?)\""", RegexOptions.IgnoreCase);
          Match match2 = regex2.Match(match.Value);
          if (!match2.Success)
            continue;
          string name = match2.Groups[1].Value;
          Regex regex3 = new Regex(@"value=\""(.+?)\""", RegexOptions.IgnoreCase);
          Match match3 = regex3.Match(match.Value);
          if (!match3.Success)
            continue;
          string value = match3.Groups[1].Value;
          inputs.Add(name, value);
        }

        string searchText = (activate) ? "ActivateSolutionItem" : "DeactivateSolutionItem";

        Regex regex4 = new Regex(@"__doPostBack\(\&\#39\;(.*?$searchString)\&\#39\;", RegexOptions.IgnoreCase);
        Match match4 = regex4.Match(responseText);
        if (match4.Success) {
          string target = match4.Groups[1].Value;
          inputs.Add("__EVENTTARGET", target);
        }

        response.Close();
      }
      StringBuilder post = new StringBuilder();
	    // Format inputs as postback data string, but ignore the one that ends with iidIOGoBack
      foreach (string key in inputs.Keys) {
        if (!string.IsNullOrEmpty(key) && !key.EndsWith("iidIOGoBack")) {
          if (post.Length > 0)
            post.Append("&");
          post.Append(Uri.EscapeDataString(key));
          post.Append("=");
          post.Append(Uri.EscapeDataString(inputs[key]));
        }
      }
      byte[] postData = Encoding.UTF8.GetBytes(post.ToString());

      HttpWebRequest activationRequest = contextManager.CreateExecutorWebRequest(slnPageUrl);
      activationRequest.Method = "POST";
      activationRequest.Accept = "text/html, application/xhtml+xml, */*";
      activationRequest.ContentType = "application/x-www-form-urlencoded";
      activationRequest.ContentLength = postData.Length;
      // Use IE? I must be a masochist!
      activationRequest.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
      activationRequest.Headers["Cache-Control"] = "no-cache";
      activationRequest.Headers["Accept-Encoding"] = "gzip, deflate";
      activationRequest.Headers["Accept-Language"] = "fr-FR,en-US";
      // Add postback data to request stream
      using (Stream reqStream = activationRequest.GetRequestStream()) {
        reqStream.Write(postData, 0, postData.Length);
        reqStream.Close();
      }

      // Do the postback
      using (WebResponse response = activationRequest.GetResponse()) {
        response.Close();
        // TODO check for good results
      }
      return true;
    }

#endregion

  } // class

}
