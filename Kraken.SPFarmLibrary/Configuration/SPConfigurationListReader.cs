
namespace Kraken.SharePoint.Configuration {

  using System;
  using System.Collections.Generic;
  //using System.Diagnostics;
  using System.Linq;
  using System.Text;

  using Microsoft.SharePoint;
  using Microsoft.SharePoint.Administration;

  using Kraken.SharePoint.Caml;
  using Kraken.SharePoint.Logging;
  using Kraken.Configuration;
  using System.Reflection;

  /// <summary>
  /// Read configuration properties from a specified list in a SPWeb.
  /// We are trying to be cloud and sandbox compatible in this class.
  /// </summary>
  class SPConfigurationListReader {

    KrakenLoggingService log = KrakenLoggingService.CreateNew(LoggingCategories.KrakenConfiguration);
    //public SPConfigurationListReader() {
    //}

    public string PropertyListName {
      get;
      set;
    }

    public string PropertyListFieldKeyName {
      get;
      set;
    }

    public string PropertyListFieldValueName {
      get;
      set;
    }

    private string _parentWebUrl;
    public string ParentWebUrl {
      get {
        if (_parentWeb != null)
          return _parentWeb.Url;
        return _parentWebUrl;
      }
      set {
        _parentWebUrl = value;
      }
    }

    private SPWeb _parentWeb;
    public SPWeb ParentWeb {
      get { return _parentWeb; }
      set {
        if (this.RunElevated && value != null)
          throw new NotSupportedException("You cannot set ParentWeb when using RunElevated. Use ParentWebUrl instead.");
        _parentWeb = value;
      }
    }

    public bool RequiredPropertiesSet {
      get {
        return (!(string.IsNullOrEmpty(ParentWebUrl) || string.IsNullOrEmpty(PropertyListName) || string.IsNullOrEmpty(PropertyListFieldKeyName) || string.IsNullOrEmpty(PropertyListFieldValueName)));
      }
    }

    /// <summary>
    /// Must be true for callers where the user does not have read permission
    /// to the configuration list. Use with care.
    /// </summary>
    internal bool RunElevated {
      get;
      set;
    }

    private bool _hasPropertyList = false;
    public bool HasPropertyList {
      get {
        if (string.IsNullOrEmpty(this.PropertyListName))
          return false;
        return _hasPropertyList;
      }
    }

    private SPList GetPropertyList(SPWeb web) {
      if (!RequiredPropertiesSet)
        return null;
      SPList list = null;
      string listName = this.PropertyListName;
      // Start by trying to get the properties from the current web site.
      try {
        //list = web.Lists.TryGetList(listName);
        //list = web.Lists[listName];
        list = web.GetList(listName);
      } catch (Exception ex) {
        KrakenLoggingService.Default.Write(ex, LoggingCategories.KrakenConfiguration);
      }
      // If they cannot be gotten there, try to get them from the site collection root.
      if (list == null && !this.RunElevated && !web.IsRootWeb) {
        //list = web.Site.RootWeb.Lists.TryGetList(listName);
        list = web.Site.RootWeb.Lists[listName];
      }
      _hasPropertyList = (list != null);
      if (list == null)
        throw new Exception(string.Format(
          "Failed to get property list '{0}' at web '{1}'."
          , listName
          , web.Url
        ));
      return list;
    }

    /*
    public SPList PropertyList {
      get {
        SPList list = null;
        if (RunElevated && !_elevated) {
          SPWeb web = this.ParentWeb;
          string listName = this.PropertyListName;
          _elevated = true;
          SPSecurity.RunWithElevatedPrivileges(delegate() {
            list = this.GetPropertyList(web, listName);
          });
          _elevated = false;
        } else {
          list = this.GetPropertyList(this.ParentWeb, this.PropertyListName);
        }
        return list;
      }
    }
     */

    public Dictionary<string, string> GetMappingDictionary(string prefix, bool useDescValueOrder) {
      if (this.RunElevated) {
        Dictionary<string, string> values = new Dictionary<string, string>();
        SPSecurity.RunWithElevatedPrivileges(delegate() {
          //SPUser system = SPContext.Current.Web.Users[@"SharePoint\system"]; // casuses ThreadAbortException
          using (SPSite site = new SPSite(this.ParentWebUrl)) { // , system.UserToken
            using (SPWeb web = site.OpenWeb()) {
              values = GetMappingDictionary(web, prefix, useDescValueOrder);
            }
          }
        });
        return values;
      } else {
        if (this.ParentWeb == null)
          throw new ArgumentNullException("this.ParentWeb");
        return GetMappingDictionary(this.ParentWeb, prefix, useDescValueOrder);
      }
    }

    internal Dictionary<string, string> GetMappingDictionary(SPWeb web, string prefix, bool useDescValueOrder) {
      log.Write(string.Format("Entering '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      Dictionary<string, string> values = new Dictionary<string, string>();
      try {
        SPList list = GetPropertyList(web);
        if (list == null) {
          log.Write(string.Format("Could not load configuration list '{0}'. Empty data will be returned.", this.PropertyListName), TraceSeverity.Medium, EventSeverity.Warning, LoggingCategories.KrakenProfiles);
          return values;
        }
        SPListItemCollection items = DoMappingDictionaryCamlQuery(list, prefix, useDescValueOrder);
        if (items == null || items.Count == 0)
          log.Write(string.Format("Could not load configuration data from list '{0}'. Empty data will be returned.", this.PropertyListName), TraceSeverity.Medium, EventSeverity.Warning, LoggingCategories.KrakenProfiles);
        else {
          foreach (SPListItem item in items) {
            string key; item.TryGetValueAsString(this.PropertyListFieldKeyName, out key);
            string value; item.TryGetValueAsString(this.PropertyListFieldValueName, out value);
            if (!string.IsNullOrEmpty(prefix) && key.StartsWith(prefix))
              key = key.Substring(prefix.Length);
            values.Add(key, value);
          }
        }
      } catch (Exception ex) {
        log.Write(ex);
        log.Write(string.Format("Unexpected Exception thrown for configuration list '{0}'. Empty data will be returned.", this.PropertyListName), TraceSeverity.Medium, EventSeverity.Warning, LoggingCategories.KrakenProfiles);
      } finally {
        log.Write(string.Format("Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      }
      return values;
    }

    private SPListItemCollection DoMappingDictionaryCamlQuery(SPList list, string prefix, bool useDescValueOrder) {
      if (list == null)
        throw new ArgumentNullException("list");
      log.Write(string.Format("Entering '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      SPListItemCollection items = null;
      SPQuery query = new SPQuery();
      string where = string.Empty;
      if (!string.IsNullOrEmpty(prefix))
        where = CAML.Where(CAML.BeginsWith(CAML.FieldRef(this.PropertyListFieldKeyName), CAML.Value(prefix)));
      string orderby = CAML.OrderBy(new object[]{
          useDescValueOrder
          ? CAML.FieldRef(this.PropertyListFieldValueName, CAML.SortType.Descending)
          : CAML.FieldRef(this.PropertyListFieldKeyName, CAML.SortType.Ascending)
        });
      string fields = CAML.ViewFields(new object[]{
          CAML.FieldRef(this.PropertyListFieldKeyName),
          CAML.FieldRef(this.PropertyListFieldValueName)
        }).Replace("<ViewFields>", "").Replace("</ViewFields>", ""); // annoying SharePoint quirk
      query.Query = where + orderby;
      query.RowLimit = 400;
      query.ViewFields = fields;
      query.ViewFieldsOnly = true;
      //query.IncludeMandatoryColumns = true;
      items = list.GetItems(query);
      log.Write(string.Format("Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      return items;
    }

    public Dictionary<string, string> _cacheAllValues;
    public Dictionary<string, string> AllValues {
      get {
        if (_cacheAllValues == null)
          _cacheAllValues = GetMappingDictionary(string.Empty, false);
        return _cacheAllValues;
      }
    }

    public object GetProperty(string key) {
      if (AllValues.Keys.Contains(key))
        return AllValues[key];
      else
        return null;
      /*
      if (this.RunElevated) {
        object result = null;
        SPSecurity.RunWithElevatedPrivileges(delegate() {
          //SPUser system = SPContext.Current.Web.Users[@"SharePoint\system"];
          using (SPSite site = new SPSite(this.ParentWebUrl)) { // , system.UserToken
            using (SPWeb web = site.OpenWeb()) {
              result = GetProperty(web, key);
            }
          }
        });
        return result;
      } else {
        if (this.ParentWeb == null)
          throw new ArgumentNullException("this.ParentWeb");
        return GetProperty(this.ParentWeb, key);
      }*/
    }

    /*

    private const string CAML_GET_PROPERTY_FIELDS = "<Field Name=\"{0}\" /><Field Name=\"{1}\" />";
    private const string CAML_GET_PROPERTY_QUERY = "<Where><Eq><FieldRef Name=\"{0}\" /><Value Type=\"Text\">{1}</Value></Eq></Where>";

    /// <summary>
    /// This function retreives a single item property from a configuration list.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    internal object GetProperty(SPWeb web, string key) {
      object result = null;
      SPList list = GetPropertyList(web);
      SPQuery query = new SPQuery();
      query.ViewFields = string.Format(CAML_GET_PROPERTY_FIELDS, this.PropertyListFieldKeyName, this.PropertyListFieldValueName);
      query.ViewFieldsOnly = true;
      query.Query = string.Format(CAML_GET_PROPERTY_QUERY, this.PropertyListFieldKeyName, key);
      SPListItemCollection items = list.GetItems(query);
      if (items.Count == 0)
        return null;
      if (items.Count > 1)
        throw new Exception(string.Format(
          "There were duplicate items in property list '{0}' with the key '{1}'.",
          this.PropertyListName,
          key)
        );
      SPListItem item = items[0];
      result = item[this.PropertyListFieldValueName];
      return result;
    }

    */

  }

}
