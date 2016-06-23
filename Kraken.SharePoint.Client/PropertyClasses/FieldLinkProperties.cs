using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client {
  public class FieldLinkProperties : FieldLinkCreationInformation {

    public FieldLinkRequireStatus? Hiro { get; set; }

    public bool? IsHidden {
      get {
        if (!Hiro.HasValue)
          return null;
        return (Hiro == FieldLinkRequireStatus.Hidden);
      }
    }
    public bool? IsRequired {
      get {
        if (!Hiro.HasValue)
          return null;
        return (Hiro == FieldLinkRequireStatus.Required);
      }
    }

    public string Name {
      get {
        if (base.Field == null)
          throw new ArgumentNullException("Field");
        return base.Field.InternalName;
      }
    }

  }

  public enum FieldLinkRequireStatus {
    Inherit = 0, // used for setting properties
    Hidden = 1,
    Optional = 2, // neither hidden nor required
    Required = 3
  }

}
