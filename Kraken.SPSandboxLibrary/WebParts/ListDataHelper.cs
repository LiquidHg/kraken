using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Web.UI.WebControls;

using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;

namespace Kraken.SharePoint.WebParts {

  public class ListDataHelper {

    #region Properties

    //private Cache webCache = null;
    public Cache WebCache {
      get {
        return HttpRuntime.Cache;
        /*if (webCache == null)
          webCache = new Cache();
        return webCache;*/
      }
    }

    /// <summary>
    /// The fieldName name for target list that will be used for the text in dropdown.
    /// Default is 'Title'.
    /// </summary>
    public string TextFieldName { get; set; }

    /// <summary>
    /// The fieldName name for target list that will be used for the text in dropdown.
    /// Default is 'ID'.
    /// </summary>
    public string ValueFieldName { get; set; }

    /// <summary>
    /// The fieldName name for target list that will be used for the default selection state value for the item when used in dropdowns.
    /// </summary>
    public string DefaultSelectedFieldName { get; set; }

    /// <summary>
    /// If you wish to filter the results shownn in the toolpart dropdown,
    /// you can provide a CAML query to determine what will be retreived from
    /// the target list.
    /// </summary>
    public SPQuery Query { get; set; }

    public int MaxAllowedSPListItems { get; set; }
    public bool EnableQueryResultsCaching { get; set; }
    public int CachedListItemsExpireMinutes { get; set; }

    /// <summary>
    /// Use this property to access previously queried list data.
    /// The data will be cached in the web cache for x minutes
    /// based on the setting of CachedListItemsExpireMinutes.
    /// Returns null if EnableQueryResultsCaching is false.
    /// </summary>
    public SPListItemCollection CachedListItems {
      get {
        if (!this.EnableQueryResultsCaching)
          return null;
        string key = "cachedListItems:" + this.TargetListNameOrUrl;
        if (WebCache[key] == null) 
          return null;
        return WebCache[key] as SPListItemCollection;
      }
      set {
        if (!this.EnableQueryResultsCaching)
          return;
        string key = "cachedListItems:" + this.TargetListNameOrUrl;
        WebCache.Insert(key, value, null, DateTime.Now.AddMinutes(CachedListItemsExpireMinutes), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
      }
    }
    //private SPListItemCollection cachedListItems = null;

    public void DumpCache() {
      if (!this.EnableQueryResultsCaching)
        return;
      string key = "cachedListItems:" + this.TargetListNameOrUrl;
      if (WebCache[key] == null)
        return;
      WebCache.Remove(key);
    }

    public string TargetListNameOrUrl {
      get {
        return listNameOrUrl;
      }
      set {
        listNameOrUrl = value;
        if (string.IsNullOrEmpty(listNameOrUrl))
          throw new Exception("You must specify a value for TargetListNameOrUrl.");
        // cuts off any trailing slash, they are bad from anyhow
        if (listNameOrUrl.EndsWith("/")) {
          listNameOrUrl = listNameOrUrl.Substring(0, listNameOrUrl.Length - 1);
        }
        int lastSlash = listNameOrUrl.LastIndexOf("/");
        if (lastSlash < 0) {
          // no slash this is just the list name, the list is in the current web context
          listName = listNameOrUrl;
          webUrl = string.Empty;
        } else if (lastSlash == 0) {
          // the first and only slash, indicates list is root-site relative
          listName = listNameOrUrl.Substring(1);
          webUrl = "/";
        } else {
          // assumes the string does not end in a slash
          listName = listNameOrUrl.Substring(lastSlash + 1);
          webUrl = listNameOrUrl.Substring(0, lastSlash);
          // check for presence of Lists in the URL and remove it if needed
          if (webUrl.EndsWith("/Lists"))
            webUrl = webUrl.Substring(0, webUrl.Length - 5);
        }
      }
    }
    private string listNameOrUrl = string.Empty;

    /// <summary>
    /// The name of the list that contains items to be retreived.
    /// </summary>
    public string TargetListName {
      get {
        return listName;
      }
    }
    private string listName = string.Empty;

    /// <summary>
    /// The url for the target web site that contains the list to be displayed by this web part.
    /// Empty string implies current web site, single slash for site collection root, or provide
    /// a root-site relative url.
    /// </summary>
    public string TargetWebUrl {
      get {
        return webUrl;
      }
    }
    private string webUrl = string.Empty;

    #endregion

    public ListDataHelper() {
      this.MaxAllowedSPListItems = 50;
      this.ValueFieldName = "ID";
      this.TextFieldName = "Title";
      this.CachedListItemsExpireMinutes = 30;
      this.EnableQueryResultsCaching = false; // true;
      //HttpContext context
      //TargetSite = SPControl.GetContextSite(context);
      //TargetWeb = SPControl.GetContextWeb(context);
    }
    public ListDataHelper(string targetListNameOrUrl)
      : this() {
      this.TargetListNameOrUrl = targetListNameOrUrl;
    }

    /// <summary>
    /// Gets items from a list.
    /// Uses caching to improve performance.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public SPListItemCollection GetListItems(SPList list) {
      if (list == null)
        throw new ArgumentNullException(string.Format("list", "Failed to open list '{0}' at web '{1}'.", this.TargetListName, this.TargetWebUrl));
      SPListItemCollection items = this.CachedListItems;
      if (items != null)
        return items;
      if (this.Query == null) {
        if (list.ItemCount > this.MaxAllowedSPListItems)
          throw new Exception(string.Format("The target list contains more than {0} items. You should provide a CAML query to reduce the number of items retreived.", this.MaxAllowedSPListItems));
        items = list.Items;
      } else {
        items = list.GetItems(this.Query);
      }
      this.CachedListItems = items;
      return items;
    }

    #region Delegate Functions 

    /// <summary>
    /// Performs action on an SPWeb using the current properties of
    /// the helper class to get/create the SPWeb object. Safely disposes
    /// of SPSite and SPWeb objects not opoened from context.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public object WebFunc(Func<SPWeb, object[], object> action, object[] args) {
      //if (string.IsNullOrEmpty(this.TargetListNameOrUrl))
      //  throw new ArgumentNullException("TargetListNameOrUrl", "You must specify a value for TargetListNameOrUrl before calling this method.");
      //SPSite site;
      object result;
      Exception ex = new Exception(string.Format("Can't use list name format to retreive list '{0}', because there is no SPContext or required sub property.", listNameOrUrl));
      switch (this.TargetWebUrl) {
        case "": // list name relative to current web context
          if (SPContext.Current == null || SPContext.Current.Web == null)
            throw ex;
          result = action(SPContext.Current.Web, args);
          break;
        case "/":
          if (SPContext.Current == null || SPContext.Current.Site == null || SPContext.Current.Site.RootWeb == null)
            throw ex;
          result = action(SPContext.Current.Site.RootWeb, args);
          break;
        default:
          try {
            string url = this.TargetWebUrl;
            if (!(url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))) {
              if (url.StartsWith("/")) {
                // make a fully qualified server based URL out of a root path relative one
                Uri scUrl = new Uri(SPContext.Current.Site.Url);
                url = scUrl.GetLeftPart(UriPartial.Authority) + url;
              } else {
                throw new Exception("relative URLs are not supported; TargetWebUrl must begin with a '/' or a protocol such as 'http://' or 'https://'");
              }
            }
            using (SPSite site = new SPSite(url)) {
              if (site == null)
                throw new ArgumentNullException("site", string.Format("Target site is null for TargetWebUrl='{0}'.", this.TargetWebUrl));
              using (SPWeb web = site.OpenWeb()) {
                if (web == null)
                  throw new ArgumentNullException("web", string.Format("Target web is null for TargetWebUrl='{0}'.", this.TargetWebUrl));
                result = action(web, args);
              }
            }
          } catch (UriFormatException) {
            throw new UriFormatException(string.Format("TargetWebUrl='{0}' could not be resolved to a valid System.Uri object within SPSite(...) constructor. Most likely, the provided target web URL is in a format that is not valid in this context. Try using a fully qualified Url in the format 'http://www.domain.com/siteurl/' when attempting to create SPSite objects explicitly.", this.TargetWebUrl));
          }
          break;
      }
      return result;
    }

    /// <summary>
    /// Performs listAction on a list using the current properties of
    /// the helper class to get the web and list objects. Safely disposes
    /// of web objects not opoened from context.
    /// </summary>
    /// <param name="listAction"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public object ListFunc(Func<SPList, object[], object> listAction, object[] args) {
      if (string.IsNullOrEmpty(this.TargetListName))
        throw new ArgumentNullException("TargetListName", "You must specify a value for TargetListName before calling this method.");
      if (string.IsNullOrEmpty(this.TargetListNameOrUrl))
        throw new ArgumentNullException("TargetListNameOrUrl", "You must specify a value for TargetListNameOrUrl before calling this method.");
      Func<SPWeb, object[], object> webAction = delegate(SPWeb web, object[] nestedArgs) {
        if (web == null)
          throw new ArgumentNullException("web", string.Format("Target web is null for TargetWebUrl='{0}'.", this.TargetWebUrl));
        SPList list = null;
        if (web != null && !string.IsNullOrEmpty(this.TargetListName))
          list = web.Lists.TryGetList(this.TargetListName);
        if (list == null)
          throw new ArgumentNullException(string.Format("list", "Failed to open list '{0}' at web '{1}'.", this.TargetListName, this.TargetWebUrl));
        return listAction(list, nestedArgs);
      };
      return WebFunc(webAction, args);
    }

    #endregion

    #region Data Table Helpers

    public static void PopulateDataTable(DataTable dt, List<ListItem> items, bool clear) {
      if (clear) {
        dt.Rows.Clear();
        dt.AcceptChanges();
      }
      foreach (ListItem item in items) {
        DataRow dr = dt.NewRow();
        dr[BasicDataTable_ValueField] = item.Value;
        dr[BasicDataTable_TextField] = item.Text;
        dr[BasicDataTable_DefaultSelectedField] = item.Selected;
        dt.Rows.Add(dr);
      }
      dt.AcceptChanges();
    }

    public const string BasicDataTable_ValueField = "Value";
    public const string BasicDataTable_TextField = "Text";
    public const string BasicDataTable_DefaultSelectedField = "DefaultSelected";

    /// <summary>
    /// Creates a basic DataTable with columns Value and Text to hold the items
    /// that will be displayed by the CheckBoxList.
    /// </summary>
    /// <returns></returns>
    public static DataTable CreateBasicDataTable() {
      // Create the DataTable that will include some very basic properties
      DataTable dt = new DataTable();
      dt.Columns.Add(new DataColumn(BasicDataTable_ValueField, typeof(string)));
      dt.Columns.Add(new DataColumn(BasicDataTable_TextField, typeof(string)));
      dt.Columns.Add(new DataColumn(BasicDataTable_DefaultSelectedField, typeof(bool)));
      return dt;
    }

    public static void DataBindListControlToDataTable(DataTable dt, ListControl lc, bool defaultSelected) {
      if (lc != null) {
        DataView dv = new DataView(dt);
        lc.DataSource = dv;
        lc.DataTextField = BasicDataTable_TextField;
        lc.DataValueField = BasicDataTable_ValueField;
        lc.DataBind();
        foreach (DataRow dr in dt.Rows) {
          try {
            string value = (string)(dr[BasicDataTable_ValueField] ?? string.Empty);
            if (!string.IsNullOrEmpty(value)) {
              ListItem item = lc.Items.FindByValue(value);
              if (item != null)
                item.Selected = (bool)(dr[BasicDataTable_DefaultSelectedField] ?? defaultSelected);
            }
          } catch {
            // I give up...
          }
        }
      }
    }

    /// <summary>
    /// Generate a data table from an item collection and convert the column names
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public static DataTable CreateDataTable(SPListItemCollection items) {
      // Create the DataTable that will include properties for the list fields
      DataTable dt = new DataTable();

      dt = items.GetDataTable();
      if (dt == null)
        return null;
      foreach (DataColumn dc in dt.Columns) {
        dc.ColumnName = System.Xml.XmlConvert.DecodeName(dc.ColumnName);
        //dc.Caption = field.Title;
      }
      return dt;
      //dt.AcceptChanges();
    }

    #endregion

  }

}
