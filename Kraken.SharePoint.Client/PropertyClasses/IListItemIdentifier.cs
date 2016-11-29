using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Client {

  /// <summary>
  /// Denotes the basic fields that can be used
  /// to identify a ListItem in a variety of ways.
  /// Each property should link to only one item.
  /// </summary>
  public interface IListItemIdentifier {
    Guid? UniqueIdentifier { get; }
    int? Id { get; }
    string Name { get; }
    Uri Url { get; }
  }

}
