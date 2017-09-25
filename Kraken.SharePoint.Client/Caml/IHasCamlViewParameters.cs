using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Client.Caml {
  public interface IHasCamlViewParameters {

    /// <summary>
    /// Specify the view scope: All, Recursive, RecursiveAll, FilesOnly
    /// </summary>
    CAML.ViewScope Scope { get; set; }

    /// <summary>
    /// Meat and potatoes of the CAML filter/query
    /// </summary>
    string WhereXml { get; set; }

    string[] ViewFields { get; set; }
    /// <summary>
    /// A hashtable : field names as keys
    /// values are 'ASC' or 'DESC'
    /// </summary>
    Hashtable OrderBy { get; set; }
    /// <summary>
    /// A hashtable : field names as keys
    /// values are 'ASC' or 'DESC'
    /// </summary>
    Hashtable GroupBy { get; set; }

    /// <summary>
    /// If groups are used, are they collapsed
    /// </summary>
    bool GroupCollapse { get; set; }

    /// <summary>
    /// If groups are used, is there a per-page limit
    /// 0 (or less) means no limit specified.
    /// </summary>
    uint GroupLimit { get; set; }
    // TODO should we make it optional?

    /// <summary>
    /// Limit on the total rows to return in the query (paging)
    /// 0 (or less) means no limit specified.
    /// Extremely high value (1 million+) indicates special behaviors
    /// </summary>
    /// <remarks>
    /// RowLimit is the only option that is seperate from view xml and can be set
    /// via a property of ViewCreationInfo.
    /// It was made optional to support updates that do not affect its value.
    /// </remarks>
    uint? RowLimit { get; set; }

  }
}
