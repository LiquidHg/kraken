using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.SharePoint;

namespace Kraken.SharePoint {

  public static class SPViewExtensions_SandboxSafe {

    /// <summary>
    /// Gets the view by title.
    /// </summary>
    /// <param name="views">The views.</param>
    /// <param name="titleOrId">The title or id.</param>
    /// <returns></returns>
    public static SPView GetViewByTitle(this SPViewCollection views, string titleOrId, bool throwExceptionOnNotFound) {
      try {
        SPView view = views[titleOrId];
        return view;
      } catch {
        foreach (SPView view in views) {
          if (string.Compare(view.Title, titleOrId) == 0) {
            return view;
          }
        }
      }
      if (throwExceptionOnNotFound)
        throw new Exception(string.Format("With with ID or Title '{0}' does not exist.", titleOrId));
      return null;
    }

  }

}
