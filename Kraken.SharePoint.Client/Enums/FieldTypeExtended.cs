using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.SharePoint.Client;

namespace Kraken.SharePoint.Client {

  /// <summary>
  /// its a weird exception not sure why these typed were not included in FieldType
  /// </summary>
  public enum FieldTypeExtended {
    TaxonomyFieldType,
    TaxonomyFieldTypeMulti,
    UserMulti 
  }

}
