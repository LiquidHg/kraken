using Kraken.Tracing;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Client {

  /// <summary>
  /// This class is used by pipe binders and other
  /// classes to attach to a single list item when
  /// only its identifying characteristics are known
  /// and it has not been loaded by CSOM. 
  /// </summary>
  /// <remarks>
  /// In general, it should return a single itme and 
  /// throw an error when more than one items comes back.
  /// </remarks>
  public class ListItemIdentifier : IListItemIdentifier {
    public ListItemIdentifier() { }

    public ListItemIdentifier(IListItemIdentifier copyFrom, ITrace trace = null) : this() {
      if (copyFrom != null) {
        this.Id = copyFrom.Id;
        this.Url = copyFrom.Url;
        this.Name = copyFrom.Name;
        this.UniqueIdentifier = copyFrom.UniqueIdentifier;
      }
    }

    public ITrace Trace { get; set; } = NullTrace.Default;
    public int? Id { get; set; }
    public Guid? UniqueIdentifier { get; set; }
    public string Name { get; set; }
    public Uri Url { get; set; }

    /// <summary>
    /// Return an item from a collection using Simple logic.
    /// This searches memory in the collection and does not
    /// go back to CSOM to get the results. Unlike SimpleMatch
    /// you must pass the target directly to this method
    /// </summary>
    public ListItem MatchSingle(IEnumerable<ListItem> itemsToSearch) { // , IListItemIdentifier target
      if (itemsToSearch == null)
        throw new ArgumentNullException("itemsToSearch");
      /* this was sort of silly
      IListItemIdentifier target = this as IListItemIdentifier;
      if (target == null)
        throw new ArgumentNullException("target");
      */
      if (!this.HasIdentiyingProperty)
        throw new InvalidOperationException("HasIdentiyingProperty is false. Can't continue. ");
      ListItem foundItem = null;
      string fileRef = BuiltInFieldId.GetName(BuiltInFieldId.FileRef);
      string fileLeafRef = BuiltInFieldId.GetName(BuiltInFieldId.FileLeafRef);
      string title = BuiltInFieldId.GetName(BuiltInFieldId.Title);
      string uniqueId = BuiltInFieldId.GetName(BuiltInFieldId.UniqueId);
      if (this.Id.HasValue && this.Id.Value > 0)
        foundItem = (from i in itemsToSearch where i.Id == this.Id.Value select i).FirstOrDefault();
      if (foundItem == null && this.Url != null)
        foundItem = (from i in itemsToSearch where i[fileRef].ToString() == this.Url.ToString() select i).FirstOrDefault();
      if (foundItem == null && this.UniqueIdentifier.HasValue && this.UniqueIdentifier.Value != Guid.Empty)
        foundItem = (from i in itemsToSearch where i[uniqueId] != null && (Guid)(i[uniqueId]) == this.UniqueIdentifier.Value select i).FirstOrDefault();
      // implement NameIdentifier in a manner similar to how pipe binders do it
      if (foundItem == null && string.IsNullOrEmpty(this.Name)) {
        if (itemsToSearch.First().FieldValues.ContainsKey(fileLeafRef))
          foundItem = (from i in itemsToSearch where i[fileLeafRef] != null && i[fileLeafRef].ToString() == this.Name select i).FirstOrDefault();
        if (foundItem == null && itemsToSearch.First().FieldValues.ContainsKey(title))
          foundItem = (from i in itemsToSearch where i[title] != null && i[title].ToString() == this.Name select i).FirstOrDefault();
      }
      return foundItem;
    }

    public bool HasIdentiyingProperty {
      get {
        return (this.Url != null
          || (this.Id.HasValue && this.Id.Value > 0)
          || (this.UniqueIdentifier.HasValue && this.UniqueIdentifier.Value != Guid.Empty)
          || (!string.IsNullOrEmpty(this.Name))
        );
      }
    }

    /// <summary>
    /// Performs the operation needed to get a single
    /// list item based on one of its identiying characteristics
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public ListItem MatchSingle(List list) {
      // TODO implement scope??
      if (list == null)
        throw new ArgumentNullException("list");
      if (!this.HasIdentiyingProperty)
        throw new InvalidOperationException("HasIdentiyingProperty is false. Can't continue. ");
      ClientContext context = (ClientContext)list.Context;
      ListItem item = null;
      if (this.Id.HasValue) {
        item = list.GetItemById(this.Id.Value);
      } else if (this.Url != null) {
        item = list.GetListItemByDocumentUrl(this.Url.ToString());
      } else if (this.UniqueIdentifier != Guid.Empty) {
        // TODO test if this will work
        item = list.GetItemById(this.UniqueIdentifier.ToString());
      } else if (!string.IsNullOrEmpty(this.Name)) {
        // empty options here will product a simple match for title or fileleafref
        item = list.GetLookupItem(this.Name, null, this.Trace).FirstOrDefault();
      }
      context.ExecuteQueryIfNeeded();
      return item;
    }

  }

}
