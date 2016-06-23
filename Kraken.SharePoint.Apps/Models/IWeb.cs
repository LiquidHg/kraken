using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Apps.Models {
  public interface IWeb {
    int Id { get; set; }

    Guid SharepointId { get; set; }

    string Url { get; set; }

    TenantBase Tenant { get; set; }
  }
}
