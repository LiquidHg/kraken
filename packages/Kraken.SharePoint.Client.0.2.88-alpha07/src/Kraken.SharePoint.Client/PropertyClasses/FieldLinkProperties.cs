using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client {
  /* In older versions of CSOM some classes are sealed
   * which makes life difficult for us, but we'll have to make-do.
   */
#if !DOTNET_V35
  public class FieldLinkProperties : FieldLinkCreationInformation {
#else
  public class FieldLinkProperties {

    public Field Field { get; set; }

#endif

    public FieldLinkCreationInformation ConvertSP14Safe() {
      return new FieldLinkCreationInformation() {
        Field = this.Field
      };
    }

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
#if !DOTNET_V35
        if (base.Field == null)
          throw new ArgumentNullException("Field");
        return base.Field.InternalName;
#else
        if (this.Field == null)
          throw new ArgumentNullException("Field");
        return this.Field.InternalName;
#endif
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
