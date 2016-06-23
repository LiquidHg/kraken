using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Apps.Models {
  public class CustomerBase : ICustomer {
    public virtual int Id { get; set; }

    public virtual string SharepointId { get; set; }

    public virtual string Email { get; set; }

    public virtual string Name { get; set; }

    public virtual TenantBase Tenant { get; set; }
  }


}
