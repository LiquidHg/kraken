/* Older versions of CSOM did not include this API */
#if !DOTNET_V35
using Microsoft.SharePoint.Client.Publishing.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Client {

  public class NavigationProperties {

    public NavigationProperties() {
      Source = StandardNavigationSource.InheritFromParentWeb;
      IncludeTypes = NavigationIncludeTypes.All;
      DynamicChildLimit = -1;
    }

    /// <summary>
    /// Determines if the web will use its own structural navigation,
    /// use managed term set, or inherit from the parent
    /// </summary>
    public StandardNavigationSource Source { get; set; }

    /// <summary>
    /// In structural nav determines if Subsites and/or Pages will be shown
    /// </summary>
    public NavigationIncludeTypes IncludeTypes { get; set; }

    /// <summary>
    /// Number of dynamic nodes to be added, typically 20;
    /// Believes to have an upper limit of 50. 
    /// Currently, -1 is equivalent of "do not change".
    /// </summary>
    public int DynamicChildLimit { get; set; }

    /// <summary>
    /// Not yet implemented; believed to be a list of pages to ignore/hide
    /// </summary>
    public string NavigationExcludes { get; set; }

  }

  [Flags]
  public enum NavigationIncludeTypes {
    None = 0,
    Sites = 1,
    Pages = 2,
    All = 3
  }

  /*
  public enum FeatureScope {
    Web,
    Site,
    Farm
  }
   */

}
#endif