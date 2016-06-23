using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Apps.Models {
  public interface ICustomer {
    int Id { get; set; }

    string SharepointId { get; set; }

    string Email { get; set; }

    string Name { get; set; }

    TenantBase Tenant { get; set; }
  }


}
