using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Apps.Models {
  public class WebBase : IWeb {
    public virtual int Id { get; set; }

    public virtual Guid SharepointId { get; set; }

    public virtual string Url { get; set; }

    public virtual TenantBase Tenant { get; set; }
  }
}
