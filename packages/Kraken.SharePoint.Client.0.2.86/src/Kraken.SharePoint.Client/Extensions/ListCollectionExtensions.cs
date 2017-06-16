namespace Microsoft.SharePoint.Client {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;

  /*
  using Kraken.SharePoint.Client;
  using Kraken.SharePoint.Client.Caching;
  using Kraken.SharePoint.Client.Connections;
  using Kraken.Net;
  using Kraken.SharePoint.Client.Helpers;
  using Kraken.Tracing;
  */

  public static class KrakenListCollectionExtensions {

    /// <summary>
    /// Case insensitive search for a list/library by a given name.
    /// The following commonly used properties are initialized: Id, Title, ItemCount, RootFolder, RootFolder.ServerRelativeUrl
    /// </summary>
    /// <param name="context"></param>
    /// <param name="listTitleOrName"></param>
    /// <param name="ignoreCase"></param>
    /// <returns>A list object with Title, Id, and RootFolder.ServerRelativeUrl loaded</returns>
    public static List GetByTitleOrName(this ListCollection lists, string listTitleOrName, bool ignoreCase = true) {
      //StringComparison compareType = (ignoreCase) ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
      //return lists.GetByTitleOrName(listTitleOrName, compareType);
      ClientContext context = (ClientContext)lists.Context;
      //web.Lists.GetByTitle(listTitle);
      //context.Load(context, lists);
      if (ignoreCase) {
        listTitleOrName = listTitleOrName.ToLower();
        // when case insensitive, we need to load the entire list-of-lists locally
        context.Load(context.Web, w => w.Lists);
        context.Load(lists, ListExpressions.IncludeBasicProperties());
        // TODO is this smart enough not to travel across the wire multiple times?
        context.ExecuteQueryIfNeeded();
        foreach (List l in lists) {
          if (listTitleOrName == l.RootFolder.Name.ToLower() || listTitleOrName == l.Title.ToLower()) {
            // load root folder propery
            /*
            l => l.RootFolder,
            l => l.RootFolder.Name,
            l => l.RootFolder.ServerRelativeUrl);
            */
            return l;
          }
        }
        return null;
      } else {
        // TODO comment the above as its not needed
        StringComparison compare = ignoreCase? StringComparison.InvariantCultureIgnoreCase: StringComparison.InvariantCulture;
        IEnumerable<List> foundLists = context.LoadQuery(
          lists
            .Where(l => listTitleOrName.Equals(l.RootFolder.Name, compare)
            || listTitleOrName.Equals(l.Title, compare))
            .IncludeBasicProperties()
        );
        context.ExecuteQueryIfNeeded();
        return foundLists.FirstOrDefault();
      }
    }

    /*
		public static List GetByTitleOrName(this ListCollection lists, string listTitleOrName, StringComparison comp) {
			ClientRuntimeContext context = lists.Context;
			//ListCollection  = web.Lists;
			//web.Lists.GetByTitle(listTitle);
			IEnumerable<List> foundLists = context.LoadQuery(
				lists
					.Where(l => listTitleOrName.Equals(l.RootFolder.Name, comp) || listTitleOrName.Equals(l.Title, comp))
					.IncludeBasicProperties()
			);
			context.ExecuteQuery();
			return foundLists.FirstOrDefault();
		}
		*/

  }
}
