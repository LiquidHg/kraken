namespace Kraken.SharePoint.Client {

  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;

  using Microsoft.SharePoint.Client;
  using Kraken.SharePoint.Client.Caml;

  /// <summary>
  /// Provides a uniform mechanism for telling
  /// commands how queries for SharePoint List
  /// Items should be performed.
  /// </summary>
  public class QueryItemOptions {

    public QueryItemOptions() { }
    public QueryItemOptions(Hashtable ht) {
      SetProperties(ht);
    }
    public void SetProperties(Hashtable ht) {
      foreach (string key in ht.Keys) {
        SetProperty(key, ht[key]);
      }
    }
    public bool SetProperty(string propertyName, object value) {
      // remove leading - from powershell-like operators
      if (propertyName.StartsWith("-"))
        propertyName = propertyName.Substring(1);
      /*
      // strip queryoptions prefix
      if (propertyName.StartsWith(MatchRulePrefix))
        propertyName = propertyName.Substring(MatchRulePrefix.Length);
      */
      string val = value.ToString();
      try {
        switch (propertyName.ToLower()) {
          case "matchmethod":
            this.MatchMethod = (ListItemFindMethod)Enum.Parse(typeof(ListItemFindMethod), val, true);
            return true;
          case "noquerymeansall":
            this.NoQueryMeansAll = bool.Parse(val);
            return true;
          case "order":
            this.Order = ((Hashtable)value);
            return true;
          case "pagesize":
            this.PageSize = int.Parse(val);
            return true;
          case "scope":
            this.Scope = (CAML.ViewScope)Enum.Parse(typeof(CAML.ViewScope), val);
            return true;
          case "viewfields":
            if (value is IEnumerable<string>) {
              this.ViewFields = ((IEnumerable<string>)value).ToList();
              return true;
            } else if (value.GetType() == typeof(string[])) {
              this.ViewFields = ((string[])value).ToList();
              return true;
            } else if (value.GetType() == typeof(object[])) {
              // this is the one typically sent by PowerShell
              this.ViewFields = new List<string>();
              foreach (object o in (object[])value) {
                this.ViewFields.Add(o.ToString());
              }
              return true;
            } else {
              ParseMessages.Add(string.Format("Unexpected type '{0}' property name: '{1}'", value.GetType(), propertyName));
              return false;
            }
          default:
            ParseMessages.Add(string.Format("Unrecognized property name: '{0}'", propertyName));
            return false;
        }
      } catch (Exception ex) {
        ParseMessages.Add(string.Format("Error during parse pf property name: '{0}'='{1}'; {2} => {3}", propertyName, val, ex.GetType().Name, ex.Message));
      }
      return false;
    }
    /// <summary>
    /// Specify fields that the operation will return. You can get 
    /// all fields by supplying an array with "all" as it only member.
    /// Defaults to a minimal set of fields used in all lists or libraries.
    /// </summary>
    public List<string> ViewFields { get; set; } = null;

    public List<string> ParseMessages { get; protected set; } = new List<string>();

    /// <summary>
    /// Determines the size of pages that will be requested
    /// so that lists with more than 5,000 items are supported.
    /// Default value -1 translates to 4,000 items per page.
    /// </summary>
    public int PageSize { get; set; } = KrakenListExtensions.DEFAULT_LISTITEM_PAGE_SIZE;

    /// <summary>
    /// Determines the scope of the CAML query used to retrieve items.
    /// Defaults to RecursiveAll. Other choices include: All, Recursive,
    /// FilesOnly, and None.
    /// </summary>
    public CAML.ViewScope Scope { get; set; } = CAML.ViewScope.RecursiveAll;

    /// <summary>
    /// Specify the methodology used to find items.
    /// </summary>
    /// <remarks>
    /// Simple will perform a master CAML query,
    /// then apply in-memory logic within the set.
    /// Multi will generate a CAML query for each
    /// call within a set.
    /// </remarks>
    public ListItemFindMethod MatchMethod { get; set; } = ListItemFindMethod.MultiQueryMatch;

    /// <summary>
    /// When false, lack of a query means do nothing
    /// and return an empty collection. When true,
    /// all items in the list should be returned if
    /// a query is missing.
    /// </summary>
    public bool NoQueryMeansAll { get; set; } = true;

    /// <summary>
    /// Specify the order that items should be returned
    /// </summary>
    public Hashtable Order { get; set; }

    /// <summary>
    /// Default is true. Specifies if pagination will be enabled 
    /// to allow for queries in lists with over 5,000 items.
    /// </summary>
    public bool UsePagination { get; set; } = true;

  }

}
