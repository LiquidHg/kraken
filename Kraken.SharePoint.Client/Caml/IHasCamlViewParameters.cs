using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Client.Caml {
  public interface IHasCamlViewParameters {
    CAML.ViewScope Scope { get; set; }
    string[] ViewFields { get; set; }
    Hashtable OrderBy { get; set; }
    int RowLimit { get; set; }
    string WhereXml { get; set; }
  }
}
