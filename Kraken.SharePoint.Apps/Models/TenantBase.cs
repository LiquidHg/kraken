using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Apps.Models {
  public class TenantBase : ITenant {
    public virtual int Id { get; set; }
    public virtual string RealmId { get; set; }
    public virtual string UrlAuthority { get; set; }
    public virtual DateTime FirstTimeInstalled { get; set; }
  }
}
