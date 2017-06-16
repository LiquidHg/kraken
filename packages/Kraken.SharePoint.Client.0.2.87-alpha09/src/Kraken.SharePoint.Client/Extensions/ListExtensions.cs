namespace Microsoft.SharePoint.Client {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Net;
  using System.Security;
  using System.Xml.Linq;

  //using Microsoft.SharePoint.Client;
  using System.Diagnostics;
  using System.ComponentModel;
  //using Microsoft.SharePoint.Client.DocumentSet;

  using Kraken.SharePoint.Client;
  using Kraken.SharePoint.Client.Caching;
  using Kraken.SharePoint.Client.Caml;
  using Kraken.SharePoint.Client.Connections;
  using Kraken.SharePoint.Client.Helpers;
  using kcloud=Kraken.SharePoint.Cloud;

  using Microsoft.SharePoint.Client.Utilities;
  using System.Collections;

  using wsClient = Kraken.SharePoint.Cloud.Client;
  using Kraken.Tracing;
#if !DOTNET_V35
  using Microsoft.SharePoint.Client.EventReceivers;
#endif

  public static class ListExpressions {

    // TODO use dynamic expressions to do this for each method
    public static void LoadBasicProperties(this List list, bool includeSchema = false) {
      ClientRuntimeContext context = list.Context;
      context.Load(list, 
          l => l.Title,
          l => l.Id,
          l => l.BaseType,
          l => l.BaseTemplate,
          l => l.ItemCount,
          l => l.AllowContentTypes,
          l => l.RootFolder,
          l => l.RootFolder.Name,
          l => l.RootFolder.ServerRelativeUrl);
      if (includeSchema)
        context.Load(list,
            l => l.SchemaXml);
      context.ExecuteQuery();
    }

    public static IQueryable<List> IncludeBasicProperties(this IQueryable<List> listQuery) {
      return listQuery.Include(
          l => l.Title,
          l => l.Id,
          l => l.BaseType,
          l => l.BaseTemplate,
          l => l.ItemCount,
          l => l.AllowContentTypes,
          l => l.RootFolder,
          l => l.RootFolder.Name,
          l => l.RootFolder.ServerRelativeUrl);
    }
    public static System.Linq.Expressions.Expression<Func<ListCollection, object>> IncludeBasicProperties() {
      System.Linq.Expressions.Expression<Func<ListCollection, object>> exp =
      l2 => l2.Include(
          l => l.Title,
          l => l.Id,
          l => l.BaseType,
          l => l.BaseTemplate,
          l => l.ItemCount,
          l => l.AllowContentTypes,
          l => l.RootFolder,
          l => l.RootFolder.Name,
          l => l.RootFolder.ServerRelativeUrl);
      return exp;
    }
    /*
    public static System.Linq.Expressions.Expression<Func<List, object>> IncludeBasicProperties() {
        System.Linq.Expressions.Expression<Func<List, object>> exp =
        l2 => l2.Include(
            l => l.Title,
            l => l.Id,
            l => l.BaseType,
            l => l.BaseTemplate,
            l => l.ItemCount,
            l => l.RootFolder,
            l => l.RootFolder.Name,
            l => l.RootFolder.ServerRelativeUrl);
        return exp;
    }
    */
  }

  /// <summary>
  /// </summary>
  /// <remarks>
  /// Name has been changed so it is unique compared
  /// to OfficeDevPnp class by the same name.
  /// </remarks>
  public static class KrakenListExtensions {

    /// <summary>
    /// Gets the full server relative URL coming from the root site.
    /// This is similar to what would be passed to web.GetList.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    /// <remarks>
    /// Note that there may be some issues with casing:
    /// https://www.simple-talk.com/blogs/2015/07/16/an-odd-behavior-with-the-serverrelativeurl-property-in-csom/
    /// </remarks>
    public static string GetServerRelativeUrl(this List list) {
      //ClientContext context = (ClientContext)list.Context;
      //context.Init(context.Web, e => e.ServerRelativeUrl);
      string webUrl = list.ParentWeb.ServerRelativeUrl;
      string rootFolderUrl = list.RootFolder.ServerRelativeUrl;
      rootFolderUrl = rootFolderUrl.Substring(rootFolderUrl.LastIndexOf("/") + 1);
      return Utils.CombineUrl(webUrl, rootFolderUrl);
    }

#region Folders

    public static IEnumerable<Folder> GetFoldersAtTopLevel(this List list) {
      ClientContext context = (ClientContext)list.Context;
      FolderCollection folders = list.RootFolder.Folders;
      IEnumerable<Folder> existingFolders = context.LoadQuery(
        folders.Include(folder => folder.ServerRelativeUrl)
      );
      context.ExecuteQuery();
      return existingFolders;
    }

    public static Folder GetFolder(this List list, Uri serverRelativeUrl, bool ignoreCase) {
      if (serverRelativeUrl.IsAbsoluteUri)
        throw new ArgumentException("A server relative Url (starts with the leading '/' immediately after the hostname and port) is required. ", "serverRelativeUrl");
      return list.GetFolder(serverRelativeUrl.ToString(), ignoreCase);
    }

    /// <summary>
    /// Get a folder from the list, with pre-treatment of folder url/name.
    /// Under certain cases it calls the extension method web.GetFolder.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="folderName">
    /// a) Simple name of the folder at root of list/library
    /// b) Relative Url path from the root of the list e.g. "subfolder1/subfolder2"
    /// c) Server Relative Url "/sites/web1/list1/subfolder1/subfolder2"
    ///    but in this case we do not check to see if it actually belongs to this list/library
    /// c) If empty, assumes the root folder
    /// </param>
    /// <param name="ignoreCase">
    /// For folderName type 'a' above, we can do a cases-insensitive Linq query
    /// The alternative requires GetFolderByServerRelativeUrl which is case sensitive
    /// </param>
    /// <returns></returns>
    public static Folder GetFolder(this List list, string folderName, bool ignoreCase) {
      ClientContext context = (ClientContext)list.Context;
      Folder existingFolder = null;
      if (string.IsNullOrEmpty(folderName)) {
        context.Load(list.RootFolder);
        context.ExecuteQuery();
        existingFolder = list.RootFolder;
      } else {
        string folderUrl = (folderName.StartsWith("/")) 
          ? folderName // don't reformat when actually a server relative URL was passed in
          : string.Format("{0}/{1}", list.RootFolder.ServerRelativeUrl, folderName);
        if (folderName.Contains("/")) {
          existingFolder = context.Web.GetFolder(folderUrl);
        } else {
          // uses the case insensitive search method
          // TODO implement a fix for GetFolder above
          FolderCollection folders = list.RootFolder.Folders;
          IEnumerable<Folder> existingFolders = context.LoadQuery(
            (ignoreCase)
            ? folders.Include(folder => folder.ServerRelativeUrl)
            : folders.Where(folder => folder.ServerRelativeUrl == folderUrl).Include(folder => folder.ServerRelativeUrl)
          );
          context.ExecuteQuery();
          existingFolder = existingFolders.FirstOrDefault(
            folder => folder.ServerRelativeUrl.ToLower() == folderUrl.ToLower());
        }
      }
      return existingFolder;
    }

    /*
    string thisItemName = string.Empty;
    string parentFolderUrl = GetParentFolderName(folderName, out thisItemName);
    string camlView = string.Format("<View><Query><Where><And><Eq><FieldRef Name='FileLeafRef'/><Value Type='Text'>{0}</Value></Eq><Eq><FieldRef Name='FSOObjType'/><Value Type='Number'>1</Value></Eq></And></Where></Query><RowLimit>1</RowLimit></View>", thisItemName);
    CamlQuery camlQuery = new CamlQuery() {
      ViewXml = camlView,
      FolderServerRelativeUrl = parentFolderUrl
    };
    ListItemCollection listItems = list.GetItems(camlQuery);
    context.Load(listItems);
    context.ExecuteQuery();
    if (listItems.Count != 1)
      return null;
    //context.Load(listItems.FirstOrDefault().Folder);
    existingFolder = listItems.FirstOrDefault().Folder;
     */

#endregion


    /// <summary>
    /// Get List Template Type
    /// </summary>
    /// <param name="list">List client object</param>
    /// <returns>returns List template type </returns>
    private static ListTemplateType GetListTemplateType(this List list) {
      try {
        return (ListTemplateType)Enum.Parse(typeof(ListTemplateType), list.BaseTemplate.ToString());
      } catch {
        throw new InvalidEnumArgumentException("ListTemplateType", list.BaseTemplate, typeof(ListTemplateType));
      }
    }

    /// <summary>
    /// Determines if the list is a document library
    /// </summary>
    /// <param name="list"></param>
    /// <param name="trace"></param>
    /// <returns>True if DocLib, false if List</returns>
    public static bool IsDocumentLibrary(this List list, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      bool isDocLib = false;
      if (list != null) {
        if (list.EnsureProperty(l => l.BaseType).BaseType == BaseType.DocumentLibrary)
          isDocLib = true;
      }
      return isDocLib;
    }

    #region Item Reteival

    /*
    private string GetSimpleCamlWhere(string fieldName, CAML.Operator op, string fieldType, string fieldValue) {
      StringBuilder sb = new StringBuilder();
      sb.Append(Caml.CAML.Where(
        Caml.GetOperator(CAML.Operator.Eq, Caml.CAML.FieldRef(fieldName),
          Caml.CAML.Value(fieldType, fieldValue)
        )
      ));
      return sb.ToString();
    }
     */

    /// <summary>
    /// 
    /// </summary>
    /// <param name="list"></param>
    /// <param name="matchOptions"></param>
    /// <param name="options"></param>
    /// <param name="pageIndex"></param>
    /// <param name="trace"></param>
    /// <returns>Collection of CSOM list items</returns>
    public static ListItemCollection GetItemsPage(
      this List list,
      CamlFieldToValueMatchOptions match = null,
      QueryItemOptions options = null,
      /*
      CAML.ViewScope scope = CAML.ViewScope.RecursiveAll,
      Dictionary<string, CAML.SortType> orderBy = null,
      List<string> viewFields = null, 
      int pageSize = -1, 
      */
      int pageIndex = 0,
      ITrace trace = null
    ) {
      // TODO match needs testing to see if properties are missing
      string where = match.ToCamlWhere();
      return list.GetItemsPage(
        where,
        /*
        match.FieldName,
        match.FieldValue,
        match.Operator,
        match.FieldType,
        */
        options,
        pageIndex,
        trace);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="list"></param>
    /// <param name="fieldName"></param>
    /// <param name="fieldValue"></param>
    /// <param name="op"></param>
    /// <param name="fieldType"></param>
    /// <param name="options"></param>
    /// <param name="pageIndex"></param>
    /// <param name="trace"></param>
    /// <returns>Collection of CSOM list items</returns>
    public static ListItemCollection GetItemsPage(
      this List list, 
      string fieldName, 
      string fieldValue, 
      CAML.Operator op = CAML.Operator.Eq, 
      string fieldType = "TEXT",
      QueryItemOptions options = null,
      /*
      CAML.ViewScope scope = CAML.ViewScope.RecursiveAll,
      Dictionary<string, CAML.SortType> orderBy = null,
      List<string> viewFields = null, 
      int pageSize = -1, 
      */
      int pageIndex = 0, 
      ITrace trace = null
    ) {
      string where = CAML.Where(op, fieldName, fieldType, fieldValue);
      return list.GetItemsPage(where, options, /* scope, orderBy, viewFields, pageSize, */ pageIndex, trace);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="list"></param>
    /// <param name="whereXml"></param>
    /// <param name="options"></param>
    /// <param name="pageIndex"></param>
    /// <param name="trace"></param>
    /// <returns>Collection of CSOM list items</returns>
    public static ListItemCollection GetItemsPage(
      this List list, 
      string whereXml = "",
      QueryItemOptions options = null,
      /*
      CAML.ViewScope scope = CAML.ViewScope.RecursiveAll,
      Dictionary<string, CAML.SortType> orderBy = null,
      List<string> viewFields = null,
      int pageSize = -1,
      */
      int pageIndex = 0, 
      ITrace trace = null
    ) {
      if (trace == null) trace = NullTrace.Default;
      // note that in both viewFields and orderBy cases below,
      // null behaves differently than an empty collection.
      string viewFieldsXml = options.ViewFields.GetCamlViewFieldsXml(list, trace);
      string orderXml = GetOrderXml(options);
      return list.GetItemsPage(
        whereXml, pageIndex, trace,
        options.Scope, viewFieldsXml, orderXml, options.PageSize
      );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="list"></param>
    /// <param name="whereXml"></param>
    /// <param name="pageIndex"></param>
    /// <param name="trace"></param>
    /// <param name="scope"></param>
    /// <param name="viewFieldsXml"></param>
    /// <param name="orderXml"></param>
    /// <param name="pageSize"></param>
    /// <returns>Collection of CSOM list items</returns>
    public static ListItemCollection GetItemsPage(
      this List list,
      string whereXml = "",
      int pageIndex = 0,
      ITrace trace = null,
      // query options long form
      CAML.ViewScope scope = CAML.ViewScope.RecursiveAll, 
      string viewFieldsXml = "",
      string orderXml = "",
      int pageSize = -1
    ) {
      if (trace == null) trace = NullTrace.Default;
      if (pageSize == LISTITEM_LIMIT_USEDEFAULT)
        pageSize = DEFAULT_LISTITEM_PAGE_SIZE;
      // TODO eliminate options that require hard CAML string encoding
      ClientContext context = (ClientContext)list.Context;
      // TODO make sure there are no other basetype that will throw an error with the default setting
      if (!list.IsDocumentLibrary() && (scope == CAML.ViewScope.RecursiveAll || scope == CAML.ViewScope.Recursive)) {
        scope = CAML.ViewScope.All;
        trace.TraceWarning("GetItemsPage: CAML scope of RecursiveAll or Recursive was used on a List. Reverting to All.");
      }
      CamlQuery camlQuery = new CamlQuery();
      int skipHowMany = (pageIndex * pageSize); // " - pageSize " removed because our index is zero based
      if (skipHowMany > 0) {
        ListItemCollectionPosition itemPosition = new ListItemCollectionPosition();
        trace.TraceVerbose(string.Format("skipHowMany = {0}", skipHowMany));
        itemPosition.PagingInfo = string.Format("Paged=TRUE&p_ID={0}", skipHowMany);
        camlQuery.ListItemCollectionPosition = itemPosition;
      }
      string viewXml = CAML.View(
        scope, 
        CAML.Query(whereXml, orderXml), 
        viewFieldsXml, 
        CAML.RowLimit(pageSize)
      );
      if (!string.IsNullOrEmpty(viewXml)) {
        trace.TraceVerbose("Generated CAML query: ");
        trace.TraceVerbose(viewXml);
        camlQuery.ViewXml = viewXml;
      }
      // diagnostic string
      trace.TraceVerbose(
        "DatesInUtc={0};FolderServerRelativeUrl={1};ListItemCollectionPosition={2}",
        camlQuery.DatesInUtc,
        camlQuery.FolderServerRelativeUrl ?? string.Empty,
        (camlQuery.ListItemCollectionPosition == null) ? string.Empty : camlQuery.ListItemCollectionPosition.PagingInfo
      );
      ListItemCollection items = list.GetItems(camlQuery);
      context.Load(items);
      try {
        context.ExecuteQuery();
      } catch (Exception ex) {
        throw new Exception(string.Format("Error in CAML query: '{0}'. InnerException='{1}'", viewXml, ex.Message), ex);
      }
      // report to the user what fields were brought back
      // TODO this currently seems to list them all
      if (pageIndex == 0 && items.Count > 0) {
        trace.TraceVerbose("Fields available (those not requested may not actually be loaded):");
        // Fields Values may not be what we need or all we need
        Dictionary<string, object> fv = items[0].FieldValues;
        foreach (string field in fv.Keys) {
          //if (fv[field] != null) // this didn't make sense
            trace.TraceVerbose("  " + field);
        }
      }
      return items;
    }

    // TODO move to CamlHelpers
    private static string GetOrderXml(QueryItemOptions options) {
      string orderXml = string.Empty;
      if (options.Order != null) {
        List<string> fields = new List<string>();
        // TODO ht convert - why is it not strongly types in the options class?
        var orderBy = CamlHelpers.ConvertToOrderBy(options.Order);
        foreach (string fieldName in orderBy.Keys) {
          fields.Add(CAML.FieldRef(fieldName, orderBy[fieldName]));
        }
        orderXml = CAML.OrderBy(fields.ToArray());
      }
      return orderXml;
    }

    // this can take a long time to run a query
    public const int DEFAULT_LISTITEM_PAGE_SIZE = 4000;
    public const int LISTITEM_LIMIT_USEDEFAULT = -1;
    public const int LISTITEM_LIMIT_NOLIMIT = -2;


    /// <summary>
    /// Gets all items matching a query, using pagination
    /// to ensure that no returned data structure is too large.
    /// Loops through all results pages to get the entire set.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="whereXml"></param>
    /// <param name="options"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    public static List<ListItem> GetItems(
      this List list,
      string whereXml = "",
      QueryItemOptions options = null,
      ITrace trace = null
      /*
      , CAML.ViewScope scope = CAML.ViewScope.RecursiveAll
      , Dictionary<string, CAML.SortType> orderBy = null
      , List<string> viewFields = null
      , int pageSize = -1
      */
    ) {
      if (trace == null) trace = NullTrace.Default;
      if (options == null) options = new QueryItemOptions();
      if (!options.UsePagination) {
        trace.TraceWarning("Call being made without pagination support; if the list is over 5,000 items (or configured throttle limit) the operation will fail.");
#pragma warning disable 618
        ListItemCollection getItems = list.GetItemsNoPaging( whereXml, options, trace );
#pragma warning restore 618
        List<ListItem> getItemsAsList = new List<ListItem>(getItems.Count);
        getItemsAsList.AddRange(getItems);
        return getItemsAsList;
      }
      if (options.PageSize == LISTITEM_LIMIT_USEDEFAULT)
        options.PageSize = DEFAULT_LISTITEM_PAGE_SIZE;
      ClientContext context = (ClientContext)list.Context;
      int numPages = (list.ItemCount / options.PageSize) + 1;
      // pre-allocate the list to hold the number of references we'll need
      int sizeOfList = list.ItemCount * 8; // hope this is not too big!
      List < ListItem> allItems = new List<ListItem>(sizeOfList);
      TraceLevel level = (numPages > 1) ? TraceLevel.Info : TraceLevel.Verbose;
      trace.Trace(level, "{0} total items; page size {1}; iterating through {2} pages.", list.ItemCount, options.PageSize, numPages);
      int itemNumber = 0;
      for (int pageNum = 0; pageNum < numPages; pageNum++) {
        trace.Trace(level, "Processing page number {0}.", pageNum);
        trace.Trace(TraceLevel.Verbose, "Getting SharePoint List data...");
        ListItemCollection items = list.GetItemsPage(
          whereXml,
          options,
          /*
          scope, 
          orderBy, 
          viewFields, 
          pageSize, 
          */
          pageNum,
          trace
        ); // TODO dissappearing default param 'string.Empty', what was it?
        trace.Trace(level, "Returned {0} items.", items.Count);

        string listUrl = list.RootFolder.ServerRelativeUrl;
        trace.Trace(TraceLevel.Verbose, "Copying relevant information to collection...");
        foreach (ListItem item in items) {
          allItems.Add(item);
          itemNumber++;
        } // foreach item
        trace.Trace(TraceLevel.Verbose, "Done page. ItemNumber = {0}", itemNumber);
      } // foreach page
      trace.Trace(TraceLevel.Verbose, "Done all pages. Count of items is {0}.", allItems.Count);
      return allItems;
    }

    /// <summary>
    /// Query a list and get a certain limited number of items back.
    /// Useful in cases where page/throlle protection is not needed.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="whereXml"></param>
    /// <param name="options"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    [Obsolete("Recommended to use GetItems (with pagination enabled in options) when querying large lists.")]
    public static ListItemCollection GetItemsNoPaging(
      this List list,
      string whereXml = "",
      QueryItemOptions options = null,
      ITrace trace = null
    ) {
      if (trace == null) trace = NullTrace.Default;
      int rowLimit = options.PageSize;
      if (rowLimit == LISTITEM_LIMIT_USEDEFAULT)
        rowLimit = DEFAULT_LISTITEM_PAGE_SIZE;
      string viewFieldsXml = options.ViewFields.GetCamlViewFieldsXml(list, trace);
      string orderByXml = GetOrderXml(options);
      CamlQuery camlQuery = new CamlQuery();
      camlQuery.ViewXml = CAML.View(
        options.Scope, 
        CAML.Query(whereXml, orderByXml),
        viewFieldsXml,
        (rowLimit == LISTITEM_LIMIT_NOLIMIT) ? string.Empty : CAML.RowLimit(rowLimit)
      );
      ListItemCollection items = list.GetItems(camlQuery);
      // this didn't make sense, since no property expressions were passed in
      // items.EnsureProperty(trace);
      ClientContext context = list.Context as ClientContext;
      context.Load(items);
      try {
        context.ExecuteQuery();
      } catch (Exception ex) {
        throw new Exception(string.Format("Error in CAML query: '{0}'. InnerException='{1}'", camlQuery.ViewXml, ex.Message), ex);
      }
      return items;
    }

    /// <summary>
    /// Query a list and get a certain limited number of items back.
    /// Useful in cases where page/throlle protection is not needed.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="fieldName"></param>
    /// <param name="fieldValue"></param>
    /// <param name="op"></param>
    /// <param name="fieldType"></param>
    /// <param name="options"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    [Obsolete("Recommended to use GetItems (with pagination enabled in options) when querying large lists.")]
    public static ListItemCollection GetItemsNoPaging(
      this List list,
      string fieldName,
      string fieldValue,
      CAML.Operator op = CAML.Operator.Eq,
      string fieldType = "TEXT",
      QueryItemOptions options = null,
      ITrace trace = null
    ) {
      string where = CAML.Where(op, fieldName, fieldType, fieldValue);
      return list.GetItemsNoPaging(where, options, trace);
    }

    /// <summary>
    /// Query a list and get a certain limited number of items back.
    /// Useful in cases where page/throlle protection is not needed.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="fieldName"></param>
    /// <param name="fieldValue"></param>
    /// <param name="op"></param>
    /// <param name="fieldType"></param>
    /// <param name="options"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    [Obsolete("Recommended to use GetItems (with pagination enabled in options) when querying large lists.")]
    public static ListItemCollection GetItemsNoPaging(
      this List list,
      CamlFieldToValueMatchOptions match = null,
      QueryItemOptions options = null,
      ITrace trace = null
    ) {
      string where = match.ToCamlWhere();
      return list.GetItemsNoPaging(where, options, trace);
    }

    // TODO implement a lookup-caching scheme
    public static IEnumerable<ListItem> GetLookupItem(this List list, string value,
      ResolveLookupOptions options = null, ITrace trace = null) {
      /*
      if (onRootWeb) {
        list = clientContext.Site.RootWeb.Lists.GetByTitle(listName);
      } else {
        list = clientContext.Web.Lists.GetByTitle(listName);
      }
       */
      if (trace == null) trace = NullTrace.Default;
      ClientContext clientContext = (ClientContext)list.Context;
      if (options.LookupFieldType != "Text"
        && options.LookupFieldType != "Choice"
        && options.LookupFieldType != "Counter") {
          trace.TraceWarning("The provided lookupFieldName='{0}'  and lookupFieldType='{1}' is not supported for ShowField of a lookup field target. This is not necessarily an error, but the caller should be aware. ", options.LookupFieldName, options.LookupFieldType);
      }

      if (list == null)
        throw new ArgumentNullException("list");
      try {
        // TODO don't we have lib function someplace that makes this more effecient??
        CamlQuery camlQueryForItem = new CamlQuery();
        string idFieldName = BuiltInFieldId.GetName(BuiltInFieldId.ID);
        List<string> fields = new List<string>();
        fields.Add(idFieldName);
        fields.Add(options.LookupFieldName);

        CamlFieldToValueMatchOptions match = new CamlFieldToValueMatchOptions() {
          FieldName = options.LookupFieldName,
          FieldType = options.LookupFieldType,
          FieldValue = value
          // defaults to Eq
        };
        QueryItemOptions qo = new QueryItemOptions() {
          ViewFields = fields,
          PageSize = options.AllowMultipleResults ? 100 : 5 // limit to 5 results, 2 is one too many
        };
#pragma warning disable 618
        ListItemCollection listItems = list.GetItemsNoPaging(match, qo, trace);
#pragma warning restore 618
        // commented because scope = all does not produce results in folders
        /*
        camlQueryForItem.ViewXml = CAML.View(CAML.ViewScope.All, 
          CAML.Query(
            CAML.Where(CAML.Operator.Eq, options.LookupFieldName, options.LookupFieldType, value),
            CAML.OrderBy(new object[]{ CAML.FieldRef(idFieldName) })
          ), 
          CAML.ViewFields(fields),
          "1000"); // because limit 2 is enough to know that we FAILED

        ListItemCollection listItems = list.GetItems(camlQueryForItem);
        clientContext.Load(listItems, items => items.Include
                                          (listItem => listItem[idFieldName],
                                           listItem => listItem[options.LookupFieldName]));
        clientContext.ExecuteQuery();
        */

        if (listItems != null) {
          if (listItems.Count > 1 && !options.AllowMultipleResults) {
            trace.TraceWarning("Lookup query returned multiple items. Provide a different value or modify the list data so it's unique. value: {0}", value);
            return null;
          }
          return listItems; 
        }
      } catch (Exception ex) {
        trace.TraceVerbose("Could not find lookup value '{0}' for field '{1}'(type={2}) in list '{3}' at web '{4}'. ", value, options.LookupFieldName, options.LookupFieldType, list.Title, list.ParentWebUrl);
        trace.TraceError(ex);
      }
      return null;
    }

    public static FieldLookupValue GetLookupValue(this List list, string value, ResolveLookupOptions options = null, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      if (options == null) options = new ResolveLookupOptions();
      FieldLookupValue lookupValue = null;
      IEnumerable<ListItem> items = list.GetLookupItem(value, options, trace);
      // never allow this option to return a value from multiple results, 
      // unless it is explicitly granted by the caller.
      if (items.Count() > 1 && !options.AllowMultipleResults)
        return null;
      ListItem item = items.FirstOrDefault();
      if (item == null)
        return null;
      lookupValue = new FieldLookupValue();
      lookupValue.LookupId = int.Parse(item["ID"].ToString());
      //lookupValue.LookupValue = item[lookupFieldName].ToString();
      return lookupValue;
    }

    public static ListItem GetListItemByDocumentUrl(this List list, string serverRelativeUrl) {
      if (string.IsNullOrEmpty(serverRelativeUrl))
        throw new ArgumentNullException("serverRelativeUrl");

      File file;
      if (list.ParentWeb.TryGetFile(serverRelativeUrl, out file)) {
        var ctx = list.Context;
        file.EnsureProperty(f => f.ListItemAllFields);
        return file.ListItemAllFields;
      }
      return null;
    }

#endregion

#region Item Creation

    private static ListItemCreationInformation CreateItemCreationInfo(this List list, ListItemHandlingType type) {
      return CreateItemCreationInfo(list, type, null, string.Empty);
    }
    private static ListItemCreationInformation CreateItemCreationInfo(this List list, ListItemHandlingType type, string folderName = "") {
      return CreateItemCreationInfo(list, type, null, folderName);
    }
    private static ListItemCreationInformation CreateItemCreationInfo(this List list, ListItemHandlingType type, Folder parentFolder, string folderName) {
      ListItemCreationInformation lci = new ListItemCreationInformation();
      if (type == ListItemHandlingType.Item)
        return lci;
      lci.FolderUrl = ((parentFolder == null) ? list.RootFolder : parentFolder).ServerRelativeUrl;
      if (type == ListItemHandlingType.File)
        lci.UnderlyingObjectType = FileSystemObjectType.File;
      if (type == ListItemHandlingType.Folder || type == ListItemHandlingType.DocSet) {
        lci.LeafName = folderName;
        lci.UnderlyingObjectType = FileSystemObjectType.Folder;
      }
      return lci;
    }


    public static ListItem CreateItem(this List list, Hashtable fieldValues, CreateItemOptions options = null, WebContextManager contextManager = null, ITrace trace = null) {
      if (options == null) options = new CreateItemOptions();
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)list.Context;
      trace.TraceVerbose("CreateItem overload 1...");
      options.EnsureDefaultValues(list.IsDocumentLibrary(trace)); // checks that options.TitleInternalFieldName has a value
      if (fieldValues.ContainsKey("ID") && !options.IgnoreIDField)
        throw new NotSupportedException("You cannot specify 'ID' as a field. Remove it from the collection or pass ignoreIDField=true when calling this method.");

      // pull title and content type as these have special purpose and may be required
      string title = string.Empty;
      if (fieldValues.ContainsKey(options.TitleInternalFieldName))
        title = fieldValues[options.TitleInternalFieldName].ToString();

      if (!fieldValues.ContainsKey(options.TitleInternalFieldName)) {
        trace.TraceWarning("Provided value table does not contain value for '{0}' and must abort.", options.TitleInternalFieldName);
        return null;
      }

      string contentTypeName = string.Empty;
      if (fieldValues.ContainsKey("ContentType"))
        contentTypeName = fieldValues["ContentType"].ToString();

      //bool doUpdate = false;
      trace.Trace(TraceLevel.Info, "Creating Item: {0} = \"{1}\"...", options.TitleInternalFieldName, fieldValues[options.TitleInternalFieldName]);

      // scoped 
      trace.Trace(TraceLevel.Verbose, "Creating core item...");
      ExecuteQueryFrequency freq = ExecuteQueryFrequency.Once;
      /*
      switch (options.UpdateFrequency) {
        case ItemUpdateFrequency.OncePerItem:
        case ItemUpdateFrequency.EveryField:
          freq = ExecuteQueryFrequency.Once;
          break;
        default:
          freq = ExecuteQueryFrequency.Skip;
          break;
      }
       */
      ListItem item = list.CreateItem(title, options.TitleInternalFieldName, null, null, contentTypeName, contextManager, freq, trace);
      trace.Trace(TraceLevel.Verbose, "Done.");
      if (item != null) {
        // scoped
        trace.Trace(TraceLevel.Verbose, "Calling item update...");
        UpdateItemResult result = list.UpdateItem(item, fieldValues, options, contextManager, trace);
        trace.Trace(TraceLevel.Verbose, "Done.");
        // TODO what should we do with result?
      }
      return item;
    }

    public static ListItem CreateItem(this List list, string titleValue, string titleInternalFieldName = "", Folder parentFolder = null, CoreMetadataInfo metaData = null, string contentTypeName = "", WebContextManager contextManager = null, ExecuteQueryFrequency doExecute = ExecuteQueryFrequency.Once, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      trace.TraceVerbose("CreateItem overload 2...");

      ContentTypeCache ctc = (contextManager == null) ? null : contextManager.ContentTypeCache;
      if (string.IsNullOrEmpty(titleInternalFieldName))
        titleInternalFieldName = "Title";
      if (list == null)
        throw new ArgumentNullException("list", "You must specify a valid SharePoint List object.");
      if (!string.IsNullOrEmpty(titleValue) && string.IsNullOrEmpty(titleInternalFieldName))
        throw new ArgumentNullException("titleInternalFieldName", "When titleValue is specified, you must provide a value for titleInternalFieldName.");

      ClientContext context = (ClientContext)list.Context;
      context.Load(list);

      string ctid = string.Empty;
      if (!string.IsNullOrEmpty(contentTypeName)) {
        ctid = list.ResolveContentTypeId(contentTypeName, contextManager, trace);
      }

      // clear the buffer!
      context.ExecuteQueryIfNeeded();

      ListItem item = null;
      // TODO determine best value for ListItemHandlingType.Item
      ListItemCreationInformation lci = list.CreateItemCreationInfo(ListItemHandlingType.Item);
      if (doExecute != ExecuteQueryFrequency.Skip) {
        ExceptionHandlingScope scope = new ExceptionHandlingScope(context);
        using (scope.StartScope()) {
          using (scope.StartTry()) {
            item = list.AddItem(lci);
            if (metaData != null)
              metaData.SetListItemMetadata(item);
            if (!string.IsNullOrEmpty(titleInternalFieldName) && !string.IsNullOrEmpty(titleValue))
            item[titleInternalFieldName] = titleValue;
            if (!string.IsNullOrEmpty(ctid))
              item["ContentTypeId"] = ctid;
            item.Update();
          }
          using (scope.StartCatch()) {
          }
          using (scope.StartFinally()) {
          }
        } // scope
        trace.TraceVerbose("Calling ExecuteQuery for initial item creation.");
        context.ExecuteQuery();
        if (item == null || scope.HasException) {
          trace.TraceWarning("Couldn't create item. Exiting CreateItem. Error=", scope.ErrorMessage);
          return item;
        }
        trace.TraceVerbose("Item created.");
      } else {
        item = list.AddItem(lci);
      }
      /*
      bool setMetadata = (metaData != null && item != null);
      bool setTitle = (!string.IsNullOrEmpty(titleValue) && item[titleInternalFieldName].ToString() != titleValue);
      bool setCt = (!string.IsNullOrEmpty(ctid) && item["ContentTypeId"].ToString() != ctid);
      bool needsUpdate = (setMetadata || setTitle || setCt);
      if (needsUpdate) {
        trace.TraceVerbose("Item update is needed after creation because core properties must be changed.");
        if (setMetadata)
          trace.TraceVerbose("  ...Updating core item metadata.");
        if (setTitle)
          trace.TraceVerbose("  ...Updating item {0}='{1}'.", titleInternalFieldName, titleValue);
        if (setCt)
          trace.TraceVerbose("  ...Updating ContentTypeId='{0}'.", ctid);
        if (doExecute != ExecuteQueryFrequency.Skip) {
          ExceptionHandlingScope scope2 = new ExceptionHandlingScope(context);
          using (scope2.StartScope()) {
            using (scope2.StartTry()) {
              if (setMetadata)
                metaData.SetListItemMetadata(item);
              if (setTitle)
                item[titleInternalFieldName] = titleValue;
              if (setCt)
                item["ContentTypeId"] = ctid;
              if (needsUpdate)
                item.Update();
            }
            using (scope2.StartCatch()) {
            }
            using (scope2.StartFinally()) {
            }
          } // scope
          // What happens to out of scope changes when DoUpdate is false??
          // Do they simply dissappear?
          trace.TraceVerbose("Calling ExecuteQuery for update.");
          context.ExecuteQuery();
          if (scope2.HasException) {
            trace.TraceVerbose("Error during update. Error=", scope2.ErrorMessage);
          }
        } else {
          if (setMetadata)
            metaData.SetListItemMetadata(item);
          if (setTitle)
            item[titleInternalFieldName] = titleValue;
          if (setCt)
            item["ContentTypeId"] = ctid;
          if (needsUpdate)
            item.Update();
        }
      }
       */
      trace.TraceVerbose("Leaving CreateItem overload 2.");
      return item;
    }

#endregion

#region Update Item

    /// <summary>
    /// This one should be used instead of the one from ListItem because
    /// this performs addtional checks against List.Fields info.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="item"></param>
    /// <param name="fieldValues"></param>
    /// <param name="options"></param>
    /// <param name="contextManager"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    public static UpdateItemResult UpdateItem(this List list, ListItem item, Hashtable fieldValues, UpdateItemOptions options = null, WebContextManager contextManager = null, ITrace trace = null) {
      if (item == null)
        throw new ArgumentNullException("item");
      if (options == null)
        throw new ArgumentNullException("options");
      if (fieldValues == null)
        throw new ArgumentNullException("fieldValues");
      if (trace == null) trace = NullTrace.Default;

      ClientContext context = (ClientContext)item.Context;
      Hashtable ht = new Hashtable();
      //CoreMetadataInfo metaData = null;

      // Does not generally actually go to CSOM, so left outside scope
      // plus if it did, it would execute query to get the value
      string nameOrTitle = item.GetNameOrTitle(fieldValues, options, trace);

      // TODO we never did anything with this???
      //trace.Trace(TraceLevel.Verbose, "Setting extended field values...");
      //metaData = new CoreMetadataInfo(item);
      // transform and reality check items

      trace.Trace(TraceLevel.Verbose, "Checking Provided Fields...");

      // found that after refactoring sometimes properties hadn't been loaded
      // its honestly better to get it out of the way all at once anyway
      context.Load(list.Fields);
      context.LoadQuery(list.Fields.Include(
        f => f.Id,
        f => f.InternalName,
        f => f.Title,
        f => f.FieldTypeKind,
        f => f.TypeAsString
      ));
      context.ExecuteQueryIfNeeded();
      
      foreach (string fieldName in fieldValues.Keys) {
        trace.TraceVerbose("Checking field: {0}", fieldName);

        Field field = (from f in list.Fields where (f.InternalName == fieldName) select f).FirstOrDefault();
        if (field == null) {
          if (!options.SupressSkippedFieldWarnings)
            trace.TraceWarning("Field name '{0}' provided in FieldValues hash-table does not exist in list '{1}' and will be skipped.", fieldName, list.GetServerRelativeUrl());
          continue;
        }
        // Can't be done in a scope because it calls execute query itself
        //context.LoadProperties(field, new string[] { "Id", "Title", "InternalName", "FieldTypeKind", "TypeAsString" }); // "TypedObject" not supported

        trace.Trace(TraceLevel.Verbose, "Getting translated field value...");
        object translatedValue = fieldValues[fieldName];
        if (options.ResolveLookups
          && field.FieldTypeKind == FieldType.Lookup) {
#if !DOTNET_V35
          translatedValue = field.ResolveLookupValue(translatedValue, contextManager, trace);
#else
          throw new NotSupportedException("Sorry, but field.ResolveLookupValue was not implemented in SP14/ because CSOM did not support fiel.TypedObject. Set options.ResolveLookups to bypass this error.");
#endif
        }
        if (options.HtmlEncodeText
          && (field.FieldTypeKind == FieldType.Text || field.FieldTypeKind == FieldType.Note)) {
          // TODO do we need some character set encoding conversion here??
          //translatedValue = field.EncodeTextValue(translatedValue, contextManager, trace);
        }

        if (options.SkipTitleOnUpdate && fieldName == options.TitleInternalFieldName) {
          trace.TraceVerbose("Field '{0}' skipped because SkipTitleOnUpdate option is true.", fieldName);
          continue;
        }
        if (options.SkipContentTypeIdOnUpdate && fieldName == BuiltInFieldId.GetName(BuiltInFieldId.ContentTypeId)) {
          trace.TraceVerbose("Field '{0}' skipped because SkipContentTypeIdOnUpdate option is true.", fieldName);
          continue;
        }
        if (fieldName == BuiltInFieldId.GetName(BuiltInFieldId.ID)
        && fieldName == BuiltInFieldId.GetName(BuiltInFieldId.ContentType)) {  // always skips this one
          trace.TraceVerbose("Field '{0}' skipped because setting this field is not supported.", fieldName);
          continue;
        }

        trace.TraceVerbose("Added translated value '{0}' for field '{1}'", translatedValue, fieldName);
        ht.Add(fieldName, translatedValue);
      } // field loop
      trace.TraceVerbose("There are {0} translated fields values", ht.Count);
      UpdateItemResult result = UpdateItemResult.NoResult;
      if (ht.Count > 0) {
        trace.TraceVerbose("Calling Item Update...");
        // called execute query and handles its own scope
        result = item.UpdateItem(ht, options, trace); //, contextManage
      } else {
        trace.TraceVerbose("Skipping Item Update; nothing to do.");
        result = UpdateItemResult.UpdateOK;
      }
      return result;
    }

    #endregion

    public static void Update(this List list, ListOptions props, bool skipCreateProperties = false, WebContextManager cm = null, ITrace trace = null) {
      if (props == null)
        throw new ArgumentNullException("props");
      if (list == null)
        throw new ArgumentNullException("list");
      if (skipCreateProperties && !props.HasExtendedSettings) {
        trace.TraceVerbose("Nothing to update on list '{0}' because skipCreateProperties && !props.HasExtendedSettings", props.Title);
        return;
      }

      if (props.DataSourceProperties != null && props.DataSourceProperties.Count > 0)
        trace.TraceWarning("List option 'DataSourceProperties' is unsupported; use at your own risk.");
      if (props.HasChangedValue(props.CustomSchemaXml) && !string.IsNullOrEmpty(props.CustomSchemaXml))
        trace.TraceWarning("List option 'CustomSchemaXml' is unsupported; use at your own risk.");
      if (props.DefaultDisplayFormUrl != null)
        trace.TraceWarning("List option 'DefaultDisplayFormUrl' is experimental; use at your own risk.");
      if (props.DefaultEditFormUrl != null)
        trace.TraceWarning("List option 'DefaultEditFormUrl' is experimental; use at your own risk.");
      if (props.DefaultNewFormUrl != null)
        trace.TraceWarning("List option 'DefaultNewFormUrl' is experimental; use at your own risk.");
      if (props.ReadSecurity.HasValue)
        trace.TraceWarning("List option 'ReadSecurity' is experimental; use at your own risk.");
      if (props.DocumentTemplateType > 0)
        trace.TraceWarning("List option 'DocumentTemplateType' is experimental; use at your own risk.");
      if (props.DocumentTemplateUrl != null)
        trace.TraceWarning("List option 'DocumentTemplateUrl' is experimental; use at your own risk.");
      if (props.ImageUrl != null)
        trace.TraceWarning("List option 'ImageUrl' is experimental; use at your own risk.");
      if (props.HasChangedValue(props.ValidationFormula))
        trace.TraceWarning("List option 'ValidationFormula' is unsupported; use at your own risk.");
      if (props.HasChangedValue(props.ValidationMessage))
        trace.TraceWarning("List option 'ValidationMessage' is unsupported; use at your own risk.");

      Web web = ((ClientContext)list.Context).Web;
      if (!skipCreateProperties) {
        if (props.HasChangedValue(props.Title)) {
          trace.TraceVerbose("Setting list property '{0}' = '{1}'", "Title", props.Title);
          list.Title = props.Title;
        }
        // allow blank descriptions
        if (props.HasChangedValue(props.Description) || string.IsNullOrEmpty(props.Description)) {
          trace.TraceVerbose("Setting list property '{0}' = '{1}'", "Description", props.Description);
          list.Description = props.Description;
        }
        if (props.OnQuickLaunch.HasValue) {
          trace.TraceVerbose("Setting list property '{0}' = '{1}'", "OnQuickLaunch", props.OnQuickLaunch.Value);
          list.OnQuickLaunch = props.OnQuickLaunch.Value;
        }
      }
      if (props.ContentTypesEnabled.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "ContentTypesEnabled", props.ContentTypesEnabled.Value);
        list.ContentTypesEnabled = props.ContentTypesEnabled.Value;
      }
      if (props.DraftVersionVisibility.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "DraftVersionVisibility", props.DraftVersionVisibility.Value);
        list.DraftVersionVisibility = props.DraftVersionVisibility.Value;
      }
      if (props.EnableAttachments.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "EnableAttachments", props.EnableAttachments.Value);
        list.EnableAttachments = props.EnableAttachments.Value;
      }
      if (props.EnableFolderCreation.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "EnableFolderCreation", props.EnableFolderCreation.Value);
        list.EnableFolderCreation = props.EnableFolderCreation.Value;
      }
      if (props.EnableMinorVersions.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "EnableMinorVersions", props.EnableMinorVersions.Value);
        list.EnableMinorVersions = props.EnableMinorVersions.Value;
      }
      if (props.EnableModeration.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "EnableModeration", props.EnableModeration.Value);
        list.EnableModeration = props.EnableModeration.Value;
      }
      if (props.EnableVersioning.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "EnableVersioning", props.EnableVersioning.Value);
        list.EnableVersioning = props.EnableVersioning.Value;
      }
      if (props.ForceCheckout.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "ForceCheckout", props.ForceCheckout.Value);
        list.ForceCheckout = props.ForceCheckout.Value;
      }
      if (props.Hidden.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "Hidden", props.Hidden.Value);
        list.Hidden = props.Hidden.Value;
      }
      if (props.NoCrawl.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "NoCrawl", props.NoCrawl.Value);
        list.NoCrawl = props.NoCrawl.Value;
      }
      if (props.DefaultDisplayFormUrl != null) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "DefaultDisplayFormUrl", props.DefaultDisplayFormUrl);
        list.DefaultDisplayFormUrl = props.DefaultDisplayFormUrl.ToString();
      }
      if (props.DefaultEditFormUrl != null) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "DefaultEditFormUrl", props.DefaultEditFormUrl);
        list.DefaultEditFormUrl = props.DefaultEditFormUrl.ToString();
      }
      if (props.DefaultNewFormUrl != null) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "DefaultNewFormUrl", props.DefaultNewFormUrl);
        list.DefaultNewFormUrl = props.DefaultNewFormUrl.ToString();
      }
      if (props.DocumentTemplateUrl != null) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "DocumentTemplateUrl", props.DocumentTemplateUrl);
        list.DocumentTemplateUrl = props.DocumentTemplateUrl.ToString();
      }
      if (props.ImageUrl != null) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "ImageUrl", props.ImageUrl);
        list.ImageUrl = props.ImageUrl.ToString();
      }
      if (props.HasChangedValue(props.ValidationFormula)) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "ValidationFormula", props.ValidationFormula);
        list.ValidationFormula = props.ValidationFormula;
      }
      if (props.HasChangedValue(props.ValidationMessage)) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "ValidationMessage", props.ValidationMessage);
        list.ValidationMessage = props.ValidationMessage;
      }
      if (props.MajorVersionLimit.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "MajorVersionLimit", props.MajorVersionLimit.Value);
        list.MajorVersionLimit = props.MajorVersionLimit.Value;
      }
      if (props.MajorWithMinorVersionsLimit.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "MajorWithMinorVersionsLimit", props.MajorWithMinorVersionsLimit.Value);
        list.MajorWithMinorVersionsLimit = props.MajorWithMinorVersionsLimit.Value;
      }
      if (props.ParserDisabled.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "ParserDisabled", props.ParserDisabled.Value);
        list.ParserDisabled = props.ParserDisabled.Value;
      }
      if (props.ReadSecurity.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "ReadSecurity", props.ReadSecurity.Value);
        list.ReadSecurity = props.ReadSecurity.Value;
      }
      if (props.MultipleDataList.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "MultipleDataList", props.MultipleDataList.Value);
        list.MultipleDataList = props.MultipleDataList.Value;
      }
      if (props.HasChangedValue(props.Direction)) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "Direction", props.Direction);
        list.Direction = props.Direction;
      }
      if (props.UXExperience.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "UXExperience", props.UXExperience.Value);
        list.ListExperienceOptions = props.UXExperience.Value;
      }
      if (props.AllowDeletion.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "AllowDeletion", props.AllowDeletion.Value);
        list.AllowDeletion = props.AllowDeletion.Value;
      }
      if (props.CrawlNonDefaultViews.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "CrawlNonDefaultViews", props.CrawlNonDefaultViews.Value);
        list.CrawlNonDefaultViews = props.CrawlNonDefaultViews.Value;
      }
      if (props.EnableAssignToEmail.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "EnableAssignToEmail", props.EnableAssignToEmail.Value);
        list.EnableAssignToEmail = props.EnableAssignToEmail.Value;
      }
      if (props.ExcludeFromOfflineClient.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "ExcludeFromOfflineClient", props.ExcludeFromOfflineClient.Value);
        list.ExcludeFromOfflineClient = props.ExcludeFromOfflineClient.Value;
      }
      if (props.ExemptFromBlockDownloadOfNonViewableFiles.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "ExemptFromBlockDownloadOfNonViewableFiles", props.ExemptFromBlockDownloadOfNonViewableFiles.Value);
        //list.ExemptFromBlockDownloadOfNonViewableFiles = props.ExemptFromBlockDownloadOfNonViewableFiles.Value;
        list.SetExemptFromBlockDownloadOfNonViewableFiles(props.ExemptFromBlockDownloadOfNonViewableFiles.Value);
      }
      if (props.IrmEnabled.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "IrmEnabled", props.IrmEnabled.Value);
        list.IrmEnabled = props.IrmEnabled.Value;
      }
      if (props.IrmExpire.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "IrmExpire", props.IrmExpire.Value);
        list.IrmExpire = props.IrmExpire.Value;
      }
      if (props.IrmReject.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "IrmReject", props.IrmReject.Value);
        list.IrmReject = props.IrmReject.Value;
      }
      if (props.IsApplicationList.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "IsApplicationList", props.IsApplicationList.Value);
        list.IsApplicationList = props.IsApplicationList.Value;
      }
      if (props.WriteSecurity.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "WriteSecurity", props.WriteSecurity.Value);
        list.WriteSecurity = props.WriteSecurity.Value;
      }
      // run the updates
      try {
        list.Update();
        list.Context.ExecuteQueryIfNeeded();
      } catch (Exception ex) {
        string msg = string.Format("Couldn't update List '{0}' at '{1}'", list.Title, web.UrlSafeFor2010());
        trace.TraceWarning(msg);
        trace.TraceError(ex);
        if (props.ThrowOnError) {
          throw new Exception(msg, ex);
        }
      }

      trace.TraceVerbose("Setting complex list properties...");
      if (props.HasChangedValue(props.DefaultContentType)) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "DefaultContentType", props.DefaultContentType);
        list.SetDefaultContentType(props.DefaultContentType, trace);
      }
      //if (props.HasChangedValue(props.DefaultView, list.DefaultView.Title))
      // it'll just skip if there's nothing to do
      if (props.HasChangedValue(props.DefaultView)) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "DefaultView", props.DefaultView);
        list.SetDefaultView(props.DefaultView, trace);
      }
      trace.TraceVerbose("Setting 'problematic' list properties...");
      if (props.Ordered.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "Ordered", props.Ordered.Value);
        list.SetLegacyAttribute("Ordered", props.Ordered.Value, cm, trace);
      }
      if (props.ShowUser.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "ShowUser", props.ShowUser.Value);
        list.SetLegacyAttribute("ShowUser", props.ShowUser.Value, cm, trace);
      }
      if (props.PreserveEmptyValues.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "PreserveEmptyValues", props.ShowUser.Value);
        list.SetLegacyAttribute("PreserveEmptyValues", props.PreserveEmptyValues.Value, cm, trace);
      }
      if (props.EnforceDataValidation.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "EnforceDataValidation", props.ShowUser.Value);
        list.SetLegacyAttribute("EnforceDataValidation", props.EnforceDataValidation.Value, cm, trace);
      }
      // TODO Flags and NavigateForFormsPages must be handled on creation of list
      /*
      if (props.NavigateForFormsPages.HasValue) {
        trace.TraceVerbose("Setting list property '{0}' = '{1}'", "NavigateForFormsPages", props.NavigateForFormsPages.Value);
        trace.TraceWarning("Set NavigateForFormsPages currently has no effect due to limitations of legacy web services.");
        list.SetNavigateForFormsPages(props.NavigateForFormsPages.Value, cm, trace);
      }
      */

      if (cm == null) {
        trace.TraceWarning("Had to skip EnsureContentTypes() because a context manager wasn't provided.");
      } else {
        string remove = string.Empty;
        if (props.RemoveContentTypes != null) {
          remove = props.RemoveContentTypes.FirstOrDefault();
          if (props.RemoveContentTypes.Count() > 1)
            trace.TraceWarning("RemoveContentTypes has more than one item, which isn't supported by EnsureContentType(). Only the first content type '{0}' will be removed. ", remove);
        }
        // add content types as specified by props.EnsureContentTypes
        if (props.EnsureContentTypes != null && props.EnsureContentTypes.Length > 0)
          list.EnsureContentType(props.EnsureContentTypes, remove, cm, trace);
      }
    }

    /// <summary>
    /// Set the default content type for a list.
    /// Other content types will have an aribtrary order.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="ContentTypeName"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    public static bool SetDefaultContentType(this List list, string ContentTypeName, ITrace trace) {
      if (trace == null)
        trace = NullTrace.Default;
      if (list == null)
        throw new ArgumentNullException("list");
      ClientContext context = (ClientContext)list.Context;
      try {
        ContentTypeCollection listContentTypes = list.ContentTypes;
        //clientContext.Load(listContentTypes, listCTs => listCTs.Include(ls => ls.Name).Where(ls => ls.Name == ContentTypeName));
        context.Load(listContentTypes);
        context.ExecuteQueryIfNeeded();
        if (listContentTypes.Count != 0) {
          IList<ContentTypeId> cTypes = new List<ContentTypeId>();
          foreach (ContentType ct in listContentTypes) {
            // TODO support skipping all folder content types
            if (ct.Name != "Folder") {
              if (ct.Name == ContentTypeName) {
                cTypes.Insert(0, ct.Id);
              } else {
                cTypes.Add(ct.Id);
              }
            }
          }
          list.RootFolder.UniqueContentTypeOrder = cTypes;
          list.RootFolder.Update();
          list.Update();
          context.ExecuteQuery();
          list.RefreshLoad();
          context.ExecuteQueryIfNeeded();
          return true;
        }
      } catch (Exception ex) {
        trace.TraceError(ex);
        throw;
      }
      return false;
    }

#region List Content Types

    public static void ResolveContentTypeId(this List list, Hashtable fieldValues, WebContextManager contextManager = null, ITrace trace = null) {
      if (!fieldValues.ContainsKey("ContentType"))
        return;
      object fieldValue = fieldValues["ContentType"];
      if (fieldValue == null)
        return;
      string contentTypeName = fieldValue.ToString();
      if (string.IsNullOrEmpty(contentTypeName))
        return;
      string ctid = list.ResolveContentTypeId(contentTypeName, contextManager, trace);
      if (!string.IsNullOrEmpty(ctid)) {
        fieldValues.Add("ContentTypeId", ctid);
        // TODO should we remove ContentType here??
        fieldValues.Remove("ContentType");
      }
    }
    public static string ResolveContentTypeId(this List list, string contentTypeName, WebContextManager contextManager = null, ITrace trace = null) {
      if (string.IsNullOrEmpty(contentTypeName))
        throw new ArgumentNullException("contentTypeName");
      // TODO do we always want to add the content type or do we just want to get it if possible?
      ContentType targetContentType = list.EnsureContentType(contentTypeName, contextManager);
      string ctid = targetContentType.Id.ToString();
      return ctid;
    }

    // NOTE do not implement caching here, that would be circular logic
    public static ContentType GetContentType(this List list, string contentTypeName) {
      ClientContext context = (ClientContext)list.Context;
      var result = context.LoadQuery(list.ContentTypes.Where(c => c.Name == contentTypeName).Include(type => type.Id, type => type.Name));
      context.ExecuteQuery();
      ContentType targetContentType = result.FirstOrDefault();
      return targetContentType;
    }

    public static int BulkChangeItemContentType(this List targetLibrary, ContentType currentCT, ContentType newCT, string orderBy) {
      ClientContext context = (ClientContext)targetLibrary.Context;
      CamlQuery cq = new CamlQuery();
      string where = CAML.Where(CAML.Eq(CAML.FieldRef("ContentType"), CAML.Value(currentCT.Name)));
      string order = CAML.OrderBy(new string[] { orderBy });
      string view = CAML.View(CAML.ViewScope.RecursiveAll, CAML.Query(where, order));

      // TODO support lists with more than 5000 items
      //"<RowLimit></RowLimit>"
      ListItemCollection items = targetLibrary.GetItems(cq);
      context.Load(items);
      context.ExecuteQuery();
      //Batching here to prevent individual requests for each list item, do 100 or so at a time in a single call
      int totalItemCount = 0; int itemCount = 0; int batchSize = 100; int batchNum = 0;
      foreach (ListItem li in items) {
        //context.Load(li); // commented because loading the item isn't necessary here
        li["ContentType"] = newCT.Name;
        itemCount++; totalItemCount++;
        if (itemCount >= batchSize) {
          context.ExecuteQuery();
          itemCount = 0; batchNum++;
        }
      }
      if (itemCount > 0) {
        context.ExecuteQuery();
      }
      return totalItemCount;
    }

    private static ContentType EnsureContentTypeInner(this List list, string contentTypeName, WebContextManager contextManager = null, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      if (string.IsNullOrEmpty(contentTypeName))
        throw new ArgumentNullException("contentTypeName");
      ClientContext context = (ClientContext)list.Context;
      bool doCaching = (contextManager != null && contextManager.IsCachingEnabled);
      if (doCaching)
        trace.TraceVerbose("Getting content type from list cache...");
      ContentType targetContentType = doCaching
        ? contextManager.ContentTypeCache.GetByName(list, contentTypeName, false)
        : list.GetContentType(contentTypeName);
      if (targetContentType == null) {
        // attempt to get the content type from the web or root web, with cache support
        ContentType webContentType = context.Web.GetContentType(contentTypeName, false, contextManager, trace);
        //context.ExecuteQuery();
        if (webContentType == null) {
          if (contentTypeName.Equals("Document Set", StringComparison.InvariantCultureIgnoreCase)) {
            throw new Exception("Please activate the \"Document Set\" feature on this site.");
          } else {
            trace.TraceVerbose("Returning empty handed...");
            return null;
          }
        }
        trace.TraceVerbose("Adding content type to list...");
        list.ContentTypes.AddExistingContentType(webContentType);
        context.ExecuteQuery();
        trace.TraceVerbose("Getting content type from list...");
        targetContentType = (contextManager != null && contextManager.IsCachingEnabled)
          ? contextManager.ContentTypeCache.GetByName(list, contentTypeName, true)
          : list.GetContentType(contentTypeName);
      }
      trace.TraceVerbose("Returning...");
      return targetContentType;
    }

    /// <summary>
    /// Ensures that a given content type has been added to a list, and that if the 
    /// content type is derived from Docuemnt Set, that Document Set has also been added.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="contentTypeName"></param>
    /// <param name="isDocSetCT"></param>
    /// <param name="cm"></param>
    /// <returns></returns>
    public static ContentType EnsureContentType(this List list, string contentTypeName, WebContextManager cm = null, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      if (string.IsNullOrEmpty(contentTypeName))
        throw new ArgumentNullException("contentTypeName");
      if (contentTypeName.Contains("|")) {
        throw new NotImplementedException("Please split your delimited array and call the overload of this method that allows for multiple content types.");
        //string[] contentTypes = contentTypeName.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
      }
      
      ClientContext context = (ClientContext)list.Context;
      bool isDocSetCT = false;

      // Ensure we have added support for the requested content type
      ContentType targetContentType = list.EnsureContentTypeInner(contentTypeName, cm, trace);
      if (targetContentType == null) {
        throw new Exception(string.Format("Best effort to get or add content type '{0}' for list '{1}' failed.", contentTypeName, list.RootFolder.ServerRelativeUrl));
      } else {
        // load the Id but only if we've never done so before
        targetContentType.EnsureProperty(trace, "Id");
        ContentTypeId ctId = targetContentType.Id;
#if !DOTNET_V35
        if (ctId.StringValue.StartsWith("0x0120D520"))
          isDocSetCT = true;
#else
        // TODO test to make sure this produces the value we'd expect
        if (ctId.ToString().StartsWith("0x0120D520"))
          isDocSetCT = true;
#endif
      }
      trace.TraceVerbose("Is Document Set = {0}", isDocSetCT);
      string DOCSET_NAME = "Document Set";
      string FOLDER_NAME = "Folder";
      if (isDocSetCT && (!contentTypeName.Equals(FOLDER_NAME, StringComparison.InvariantCultureIgnoreCase) &&
        !contentTypeName.Equals(DOCSET_NAME, StringComparison.InvariantCultureIgnoreCase))) {
        // Ensure we have added support for Document Set
        ContentType documentSetContentType = list.EnsureContentTypeInner(DOCSET_NAME, cm, trace);
        if (documentSetContentType == null)
          throw new Exception(string.Format("Best effort to get or add content type '{0}' for list '{1}' failed.", DOCSET_NAME, list.RootFolder.ServerRelativeUrl));
      }
      return targetContentType;
    }

    /// <summary>
    /// Adds content types to the list based on a list of content type names
    /// and if successful (even a bit) removes the specified default content type.
    /// </summary>
    public static void EnsureContentType(this List list,
      string[] contentTypes,
      string removeContentType,
      WebContextManager cm = null,
      ITrace trace = null
    ) {
      if (trace == null) trace = NullTrace.Default;
      ClientRuntimeContext context = list.Context;
      List<ContentType> newCts = new List<ContentType>();
      if (cm == null)
        trace.TraceWarning("EnsureContentTypes called without a WebContextManager; Content type cache can't be leveraged which may significantly impact performance.");
      if (contentTypes == null)
        trace.TraceVerbose("contentTypes was null; nothing to do.");
      foreach (string ctName in contentTypes) { 
        trace.TraceVerbose(string.Format("Adding content type name '{0}' to list.", ctName));
        ContentType newCt = null;
        try {
          if (!string.IsNullOrEmpty(ctName))
            newCt = list.EnsureContentType(ctName, cm, trace); 
        } catch (Exception ex) {
          trace.TraceError(ex);
          //cmd.WriteError(new ErrorRecord(ex, "LISTCT_ADD_ERROR", ErrorCategory.NotSpecified, list));
          trace.TraceWarning(string.Format("Content type name '{0}' could not be added to the list.", ctName));
        }
        if (newCt != null)
          newCts.Add(newCt);
      }
      if (newCts.Count > 0 && !string.IsNullOrEmpty(removeContentType)) { 
        list.RemoveContentType(removeContentType, trace);
      }
    }

    internal static void RemoveContentType(this List list, string contentTypeName, ITrace trace) {
      if (string.IsNullOrEmpty(contentTypeName))
        throw new ArgumentNullException("contentTypeName");
      ClientRuntimeContext context = list.Context;
      trace.TraceVerbose(string.Format("Removing content type name '{0}' to list.", contentTypeName));
      ContentType ct = list.GetContentType(contentTypeName);
      // if found, remove it from the list
      if (ct == null) {
        trace.TraceWarning(string.Format("Could not remove content type '{0}' from the list because it was not found.", contentTypeName));
      } else {
        ct.DeleteObject();
        context.ExecuteQuery();
      }
    }

    public static ContentType AddContentType(this List list,
      ContentTypeProperties properties,
      WebContextManager ctxMgr = null) {
      return list.ContentTypes.AddContentType(properties, ctxMgr);
    }

#endregion

#region Folders and DocSets

    public static Folder CreateFolderOrDocumentSet(this List list, string folderContentTypeName, string folderName, string localFilePath, string localFilPathFieldName, WebContextManager contextManager = null, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      return list.CreateFolderOrDocumentSet(null, folderContentTypeName, folderName, localFilePath, localFilPathFieldName, contextManager, trace);
    }
    /// <summary>
    /// Creates a document set or folder content type based on a folder in the local filesystem
    /// </summary>
    /// <param name="list"></param>
    /// <param name="folderContentTypeName"></param>
    /// <param name="newFolderName"></param>
    /// <param name="localFilePath"></param>
    /// <param name="localFilePathFieldName"></param>
    public static Folder CreateFolderOrDocumentSet(this List list, Folder parentFolder, string folderContentTypeName, string newFolderName, string localFilePath, string localFilePathFieldName, WebContextManager contextManager = null, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      if (list == null)
        throw new ArgumentNullException("list", "You must specify a valid SharePoint List object.");
      if (string.IsNullOrEmpty(folderContentTypeName))
        folderContentTypeName = "Folder";
      ClientContext context = (ClientContext)list.Context;
      CoreMetadataInfo metaData = new CoreMetadataInfo(localFilePath, list, !string.IsNullOrEmpty(localFilePathFieldName), trace) {
        LocalFilePathFieldName = localFilePathFieldName
      };
      context.Load(list);
      // TODO check parentFolder and make sure we aren't creating a Document Set in a sub-folder
      ContentType targetContentType = list.EnsureContentType(folderContentTypeName, contextManager);
      string ctid = targetContentType.Id.ToString();
      // another way to do this...
      //DocumentSet.Create(context, list.RootFolder, documentSetName, ctid);
      ListItemCreationInformation lci = list.CreateItemCreationInfo(ListItemHandlingType.Folder, parentFolder, newFolderName);
      ListItem item = list.AddItem(lci);
      item["ContentTypeId"] = ctid;
      item["Title"] = newFolderName;
      // Date fields don't seem to stick under doc set when we set them here
      metaData.SetListItemMetadata(item);
      item.Update();
      context.ExecuteQuery();
      context.Load(item);
      // force the creation and modification dates to be *more* correct
      // because if we don't do this, then modified won't stick
      metaData.SetListItemMetadata(item);
      item.Update();
#if !DOTNET_V35
      // load and return the folder object
      context.Load(item.Folder, f => f.ServerRelativeUrl);
      context.ExecuteQuery();
      return item.Folder;
#else
      return null; //HACK this will likely have unintended consequences since Folder was probably useful to the caller
#endif
    }

    /// <summary>
    /// Creates an empty folder.
    /// This code is almost the same as CreateFolderOrDocSet.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="parentFolder">Assumes no parent folder means we want the root folder</param>
    /// <param name="newFolderName"></param>
    /// <param name="localFilePath"></param>
    /// <param name="localFilPathFieldName"></param>
    /// <returns></returns>
    // TODO it would be good if CreateFolderOrDocSet would look for content type "Folder" and come back here
    public static Folder CreateFolder(this List list, Folder parentFolder, string newFolderName, string localFilePath, string localFilPathFieldName, ITrace trace) {
      if (trace == null) trace = NullTrace.Default;
      if (list == null)
        throw new ArgumentNullException("list", "You must specify a valid SharePoint List object.");
      ClientContext context = (ClientContext)list.Context;
      CoreMetadataInfo metaData = new CoreMetadataInfo(localFilePath, list, !string.IsNullOrEmpty(localFilPathFieldName), trace) {
        LocalFilePathFieldName = localFilPathFieldName
      };
      context.Load(list);
      ListItemCreationInformation lci = list.CreateItemCreationInfo(ListItemHandlingType.Folder, parentFolder, newFolderName);
      var item = list.AddItem(lci);
      metaData.SetListItemMetadata(item);
      item.Update();
#if !DOTNET_V35
      // load and return the folder object
      context.Load(item.Folder, f => f.ServerRelativeUrl);
      context.ExecuteQuery();
      return item.Folder;
#else
      return null; //HACK this will likely have unintended consequences since Folder was probably useful to the caller
#endif
    }
    

#endregion

#region Fields

    public static Field EnsureField(this List list, string fieldName, string fieldTypeAsString) {
      if (string.IsNullOrEmpty(fieldName))
        throw new ArgumentNullException("fieldName");
      if (string.IsNullOrEmpty(fieldTypeAsString))
        throw new ArgumentNullException("fieldTypeAsString");
      Field field = list.GetField(fieldName);
      if (field == null) {
        field = list.AddField(fieldName, fieldName, fieldTypeAsString, true, false);
      }
      return field;
    }

    // TODO support additional field types
    public static Field AddField(this List list, string fieldName, string fieldDisplayName, string fieldType, bool addToDefaultView, bool hidden) {
      ClientContext context = (ClientContext)list.Context;
      string fieldXml = string.Format("<Field InternalName='{0}' DisplayName='{1}' Type='{2}' Hidden='{3}' />", fieldName, fieldDisplayName, fieldType, hidden ? "TRUE" : "FALSE");
      Field newField = list.Fields.AddFieldAsXml(fieldXml, addToDefaultView, AddFieldOptions.AddToAllContentTypes); // DefaultValue
      switch (fieldType) {
        case "Text":
          //FieldText fieldText = context.CastTo<FieldText>(newField);
          //fieldText.UpdateAndPushChanges(true);
          break;
        case "Note":
          FieldMultiLineText fieldMultiLineText = context.CastTo<FieldMultiLineText>(newField);
          fieldMultiLineText.RichText = true;
          fieldMultiLineText.AllowHyperlink = true;
          fieldMultiLineText.UpdateAndPushChanges(true);
          break;
      }
      list.Update();
      context.ExecuteQuery();
      return newField;
    }

    // TODO test to see if this throw exceptions and if so, implement TryGetField
    public static Field GetField(this List list, string fieldNameOrDisplayName, bool doExecuteQuery = true, bool useCsomMethod = false) {
      ClientContext context = (ClientContext)list.Context;
      FieldCollection fields = list.Fields;
      context.Load(fields, fc => fc.Include(f => f.Id, f => f.InternalName, f => f.StaticName, f => f.Title, f => f.TypeAsString));
      Field field = null;
      if (useCsomMethod) {
        field = fields.GetByInternalNameOrTitle(fieldNameOrDisplayName);
        if (doExecuteQuery)
          context.ExecuteQuery();
      } else {
        IEnumerable<Field> result = context.LoadQuery(fields.Where(f => f.InternalName == fieldNameOrDisplayName || f.Title == fieldNameOrDisplayName));
        if (doExecuteQuery)
          context.ExecuteQuery();
        field = result.FirstOrDefault();
      }
      return field;
    }
    public static Field GetField(this List list, Guid id, bool doExecuteQuery = true, bool useCsomMethod = false) {
      ClientContext context = (ClientContext)list.Context;
      FieldCollection fields = list.Fields;
      context.Load(fields, fc => fc.Include(f => f.InternalName, f => f.Title, f => f.TypeAsString));
      Field field = null;
      if (useCsomMethod) {
        field = fields.GetById(id);
        if (doExecuteQuery)
          context.ExecuteQuery();
      } else {
        IEnumerable<Field> result = context.LoadQuery(fields.Where(f => f.Id == id));
        if (doExecuteQuery)
          context.ExecuteQuery();
        field = result.FirstOrDefault();
      }
      return field;
    }

#endregion

#region Views

    public static bool TryGetView(this List list, string title, out View view, bool ignoreCase = true) {
      try {
        // TODO implement ignoreCase
        view = list.Views.GetByTitle(title);
        list.Context.Load(view);
        list.Context.ExecuteQuery();
        return (view != null);
      } catch {
        view = null;
        return false;
      }
    }

    public static View GetDefaultViewSafeFor2010(this List list) {
      ClientContext ctx = (ClientContext)list.Context;
#if !DOTNET_V35
      if (ctx.IsSP2013AndUp()) {
        return list.DefaultView;
      } else {
#else
      if (true) {
#endif
        // This is a terrible way to do things but should work in SP2010
        IEnumerable<View> views = ctx.LoadQuery(list.Views.Include(v => v.DefaultView));
        ctx.ExecuteQueryIfNeeded();
        View view = views.Where(v => v.DefaultView == true).FirstOrDefault();
        ctx.Load(view);
        ctx.ExecuteQueryIfNeeded();
        return view;
      }
    }

    public static bool SetDefaultView(this List list, string viewName, ITrace trace) {
      if (list == null)
        throw new ArgumentNullException("list");
      if (string.IsNullOrEmpty(viewName))
        throw new ArgumentNullException("viewName");
      if (trace == null)
        throw new ArgumentNullException("trace");
      bool result = false;
      View view = null;
      ClientRuntimeContext context = list.Context;
      string viewTitle = string.Empty;
      trace.TraceVerbose(string.Format("Attempting to retrieve View '{1}' from List '{0}'.", list.Title, viewName));
      Guid viewId;
#if !DOTNET_V35
      if (Guid.TryParse(viewName, out viewId)) {
#else
      bool isGuid = false; viewId = Guid.Empty;
      try {
        viewId = new Guid(viewName);
        isGuid = true;
      } catch {}
      if (isGuid) {
#endif
        view = list.GetView(viewId);
      } else {
        view = list.Views.GetByTitle(viewName);
      }
      context.Load(view);
      context.ExecuteQuery();
      viewTitle = view.Title;
      if (view == null)
        throw new ArgumentOutOfRangeException("viewName", string.Format("The List '{0}' does not contain view '{1}'.", list.Title, viewName));

      bool doUpdate = false;
      trace.TraceVerbose(string.Format("Found view '{1}' in List '{0}'.", list.Title, viewTitle));

      if (view.DefaultView == true) {
        trace.TraceVerbose(string.Format("View '{1}' is already the default view in List '{0}'. Operation will be skipped. ", list.Title, viewTitle));
      } else {
        trace.TraceVerbose(string.Format("View '{1}' is being set to the default view in List '{0}'. ", list.Title, viewTitle));
        view.DefaultView = true;
        doUpdate = true;
      }
      if (doUpdate) {
        trace.TraceVerbose("Updating View properties.");
        view.Update();
        context.ExecuteQuery();
        trace.TraceVerbose("View properties saved.");
        result = true;
      }
      return result;
    }

#endregion

#region Custom Properties

    public static ClientObjectData GetObjectData(this List list) {
      //protected internal
      Type type = typeof(ClientObject);
      //System.Reflection.PropertyInfo pi = type.GetProperty("ObjectData", System.Reflection.BindingFlags.NonPublic);
      var return_object = type.InvokeMember(
                      "ObjectData",
                      System.Reflection.BindingFlags.Instance
                      | System.Reflection.BindingFlags.NonPublic
                      | System.Reflection.BindingFlags.GetProperty,
                      null,
                      list,
                      null);
      return (ClientObjectData)return_object;
    }

    /// <summary>
    /// Get the Flags attribute from SchemaXml
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static ulong GetFlags(this List list) { // , WebContextManager contextManager, XElement listNode = null
      XElement listNode = list.GetSchemaXml();
      /*
      if (listNode == null) {
        listNode = XDocument.Parse(list.SchemaXml).Root;
        if (contextManager != null) {
          kcloud.SharePointConnection conn = contextManager.CreateLegacyConnection(true, false);
          XElement listNode2 = conn.ListsManager.GetListSchema(list.Id);
        }
        // TODO test the list schema by web svc and make sure the value matches what we have in CSOM
        return GetFlags(list, contextManager, listNode);
      }
      */
      XAttribute flagsAttrib = listNode.Attributes().Where(x => x.Name == "Flags").FirstOrDefault();
      if (flagsAttrib != null) {
        ulong flags;
        if (ulong.TryParse(flagsAttrib.Value, out flags)) {
          return flags;
        }
      }
      return 0L;
      //This will always be empty, because CSOM doesn't populate it
      /*
      // This isn't likely to work because Flags isn't a valid property
      list.Context.LoadProperties(list, new string[] { "Flags" });
      list.CheckUninitializedProperty("Flags");
      ClientObjectData objectData = list.GetObjectData();
      if (!objectData.Properties.ContainsKey("Flags"))
        return 0L;
      return (ulong)objectData.Properties["Flags"];
        */
    }
    /// <summary>
    /// Returns a boolean based on the presence of a flag
    /// </summary>
    /// <param name="list"></param>
    /// <param name="mask"></param>
    /// <returns></returns>
    public static bool GetFlag(this List list, ulong mask) {
      return (0L != (list.GetFlags() & mask));
    }

    /// <summary>
    /// Since there isn't a way to set Flags except at list
    /// creation, use this to set CustomSchemaXml as needed.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static ulong SetFlags(this List list, ulong mask, bool value) {
      ulong flags = list.GetFlags();
      if (value)
        flags |= mask;
      else
        flags &= ~mask;
      return flags;
    }

    public const ulong FlagMask_NavigateForFormsPages = 0x80000000000000L;

    /*
    /// <summary>
    /// </summary>
    /// <remarks>
    /// This method makes a live call to the SP web service
    /// and does not wait for ExecuteQuery to complete.
    /// </remarks>
    /// <param name="list"></param>
    /// <param name="value"></param>
    /// <param name="contextManager"></param>
    /// <param name="listNode"></param>
    public static void SetFlags(this List list, ulong value, WebContextManager contextManager) { // , XElement listNode = null
      / *
      XAttribute flagsAttrib = listNode.Attributes().Where(x => x.Name == "Flags").FirstOrDefault();
      if (flagsAttrib == null) {
        flagsAttrib = new XAttribute("Flags", value);
        listNode.Add(flagsAttrib);
      } else {
        flagsAttrib.Value = value.ToString();
      }
       * /
      
      // This won't work; it is unsupported property
      list.SetLegacyAttribute("Flags", value, contextManager, contextManager.TraceWriter);

      // does not work, because in CSOM (even server-side) SchemaXml does not exist as a writable property
      //list.SchemaXml_SetCustom();
      //}
      // Commented because it causes 'Field or property "Flags" does not exist.'
      / *
      ClientObjectData objectData = list.GetObjectData();
      if (!objectData.Properties.ContainsKey("Flags"))
        objectData.Properties.Add("Flags", value);
      else
        objectData.Properties["Flags"] = value;
      if (list.Context != null)
        list.Context.AddQuery(new ClientActionSetProperty(list, "Flags", value));
        * /
    }
    */

    // nice try - doesn't work
    // Commented because it causes 'Field or property "Flags" does not exist.'
    /*
      public static void SchemaXml_SetCustom(this List list, string value) {
        ClientObjectData objectData = list.GetObjectData();
        objectData.Properties["SchemaXml"] = value;
        if (list.Context != null)
          list.Context.AddQuery(new ClientActionSetProperty(list, "SchemaXml", value));
      }
     */

    public static XElement GetSchemaXml(this List list) { // , WebContextManager contextManager
      ClientContext context = (ClientContext)list.Context;
      context.Load(list, l => l.SchemaXml);
      context.ExecuteQueryIfNeeded();
      XElement listNode = XDocument.Parse(list.SchemaXml).Root;
      return listNode;
    }

    public static bool? GetNavigateForFormsPages(this List list) { // , WebContextManager contextManager
      XElement listNode = list.GetSchemaXml();
      XAttribute navigateAttrib = listNode.Attributes().Where(x => x.Name == "NavigateForFormsPages").FirstOrDefault();
      if (navigateAttrib != null)
        return bool.Parse(navigateAttrib.Value);
      else {
        // another way to (maybe) do this
        return list.GetFlag(FlagMask_NavigateForFormsPages);
      }
      // and another method that didn't work
      /*
        object obj2 = this.m_ListAttributesDict["NavigateForFormsPages"];
        if (obj2 != null) {
          return (bool)obj2;
        }
      */
    }

    /// <summary>
    /// Uses Lists.asmx web serivce to set some legacy attributes that
    /// are still not accessible in CSOM. 
    /// </summary>
    /// <param name="list"></param>
    /// <param name="propName">Supported attributes are "Ordered", "ShowUser", "PreserveEmptyValues", and "EnforceDataValidation".</param>
    /// <param name="value"></param>
    /// <param name="cm"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    // StsSoap.dll ListSchemaImpl.UpdateProp makes it clear that only certain attributes are supported
    // here are a few that may work but aren't available in CSOM
    public static List SetLegacyAttribute(this List list, string propName, bool value, WebContextManager cm, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      if (cm == null) {
        trace.TraceWarning("Can't set '{0}' a WebConextManager. Operation skipped.", propName);
        return list;
      }
      if (propName != "Ordered"
        && propName != "ShowUser"
        && propName != "PreserveEmptyValues"
        && propName != "EnforceDataValidation") {
        trace.TraceWarning("You specified a proeprty name '{0}' that is either supported by CSOM or not supported by the Lists.asmx web service. Most likely this call will have no effect.", propName);
      }
      // emsure the properties we need are loaded
      cm.Context.Load(list, l => l.Id, l => l.SchemaXml);
      cm.Context.ExecuteQueryIfNeeded();

      // Make a legacy web services connection and do things that way
      kcloud.SharePointConnection connection = cm.CreateLegacyConnection(true, false);
      XElement listNode = XDocument.Parse(string.Format("<List {0}=\"{1}\" />", propName, value.ToString().ToUpper())).Root;
      XElement result = connection.ListsManager.UpdateListSchema(list.Id, listNode, null, false);
      // TODO check what is result??
      list.RefreshLoad();
      cm.Context.ExecuteQueryIfNeeded();
      return list;
    }
    public static List SetLegacyAttribute(this List list, string propName, ulong value, WebContextManager cm, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      if (cm == null) {
        trace.TraceWarning("Can't set '{0}' a WebConextManager. Operation skipped.", propName);
        return list;
      }
      if (propName != "Ordered"
        && propName != "ShowUser"
        && propName != "PreserveEmptyValues"
        && propName != "EnforceDataValidation") {
        trace.TraceWarning("You specified a proeprty name '{0}' that is either supported by CSOM or not supported by the Lists.asmx web service. Most likely this call will have no effect.");
      }
      // Make a legacy web services connection and do things that way
      kcloud.SharePointConnection connection = cm.CreateLegacyConnection(true, false);
      XElement listNode = XDocument.Parse(string.Format("<List {0}=\"{1}\" />", propName, value.ToString())).Root;
      XElement result = connection.ListsManager.UpdateListSchema(list.Id, listNode, null, false);
      // TODO check what is result??
      list.RefreshLoad();
      cm.Context.ExecuteQueryIfNeeded();
      return list;
    }

    /*
    /// <summary>
    /// Set list NavigateForFormsPages using legacy web service
    /// </summary>
    /// <remarks>
    /// This method makes a live call to the SP web service
    /// and does not wait for ExecuteQuery to complete.
    /// </remarks>
    /// <param name="list"></param>
    /// <param name="value"></param>
    /// <param name="contextManager"></param>
    public static void SetNavigateForFormsPages(this List list, bool value, WebContextManager cm, ITrace trace = null) { // , WebContextManager contextManager
      if (cm == null) {
        trace.TraceWarning("Can't set NavigateForFormsPages without a WebConextManager. Operation skipped.");
        return;
      }
      if (trace == null) trace = NullTrace.Default;
      bool? current = list.GetNavigateForFormsPages();
      if (current.HasValue && current.Value == value) {
        trace.TraceVerbose("SetNavigateForFormsPages for list '{0}' had nothing to do and was skipped.", list.Title);
        return;
      }

      // will not work because web service won't support this property
      list.SetLegacyAttribute("NavigateForFormsPages", value, cm, trace);

      // didn't work because NavigateForFormsPages isn't a client property
      / *
      ClientObjectData objectData = list.GetObjectData();
      if (!objectData.Properties.ContainsKey(propName))
        objectData.Properties.Add(propName, value);
      else
        objectData.Properties[propName] = value;
      // this is a public property on SPList. Should be interesting to see what happens.
      if (list.Context != null)
        list.Context.AddQuery(new ClientActionSetProperty(list, propName, value));
      * /
      // didn't work because Flags isn't a client property
      / *
      ulong flags = list.Flags(contextManager);
      if (value) {
        flags |= (ulong)0x0080000000000000L;
      } else {
        flags &= (~(ulong)0x0080000000000000L); // 18410715276690587647L;
      }
      list.Flags(flags, contextManager);
      //this.m_ListAttributesDict["NavigateForFormsPages"] = value;
       * /
    }
    */

    #endregion

#region Remote Event Receivers

    /* Sorry, but event receivers are a new thing in CSOM */
#if !DOTNET_V35

    // with thanks to https://blogs.msdn.microsoft.com/kaevans/2014/02/26/attaching-remote-event-receivers-to-lists-in-the-host-web/
    public static bool EnsureRemoteEvent(this List list, string receiverName, Uri receiverUrl, EventReceiverSynchronization sync, int seq, ITrace trace) {
      if (trace == null) trace = NullTrace.Default;
      ClientContext clientContext = (ClientContext)list.Context;
      foreach (var rer in list.EventReceivers) {
        if (rer.ReceiverName == receiverName) {
          trace.TraceVerbose("Found existing {0} receiver at {1}. Exiting. ", receiverName, rer.ReceiverUrl);
          return false;
        }
      }

      EventReceiverDefinitionCreationInformation receiver =
          new EventReceiverDefinitionCreationInformation();
      receiver.EventType = EventReceiverType.ItemAdded;
      //Get WCF URL where this message was handled

      receiver.ReceiverUrl = receiverUrl.ToString();
      receiver.ReceiverName = receiverName;
      receiver.Synchronization = sync;
      receiver.SequenceNumber = seq;
      list.EventReceivers.Add(receiver);
      clientContext.ExecuteQuery();
      trace.TraceInfo("Added {0} receiver at {1}", receiverName, receiverUrl);
      return true;
    }

#endif
#endregion

  }

}
