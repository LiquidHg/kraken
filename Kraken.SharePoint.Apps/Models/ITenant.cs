using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Apps.Models {
  public interface ITenant {
    int Id { get; set; }
    string RealmId { get; set; }
    string UrlAuthority { get; set; }
    DateTime FirstTimeInstalled { get; set; }
  }
}
