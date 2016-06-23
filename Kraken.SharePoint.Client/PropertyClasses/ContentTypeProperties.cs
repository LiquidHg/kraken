using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client {
  /// <summary>
  /// Extends ContentTypeCreationInformation to include additional properties that must be set after creation
  /// </summary>
  public class ContentTypeProperties : ContentTypeCreationInformation {

    public bool? Hidden { get; set; }
    public bool? ReadOnly { get; set; }
    public bool? Sealed { get; set; }

    public string DisplayFormTemplateName { get; set; }
    public string DisplayFormUrl { get; set; }
    public string DocumentTemplate { get; set; }
    public string EditFormTemplateName { get; set; }
    public string EditFormUrl { get; set; }
    public string JSLink { get; set; }
    public string MobileDisplayFormUrl { get; set; }
    public string MobileEditFormUrl { get; set; }
    public string MobileNewFormUrl { get; set; }
    public string NewFormTemplateName { get; set; }
    public string NewFormUrl { get; set; }

    public bool HasExtendedSettings {
      get {
        if (!string.IsNullOrEmpty(this.DisplayFormTemplateName))
          return true;
        if (!string.IsNullOrEmpty(this.DisplayFormUrl))
          return true;
        if (!string.IsNullOrEmpty(this.DocumentTemplate))
          return true;
        if (!string.IsNullOrEmpty(this.EditFormTemplateName))
          return true;
        if (!string.IsNullOrEmpty(this.EditFormUrl))
          return true;
        if (!string.IsNullOrEmpty(this.JSLink))
          return true;
        if (!string.IsNullOrEmpty(this.MobileDisplayFormUrl))
          return true;
        if (!string.IsNullOrEmpty(this.MobileEditFormUrl))
          return true;
        if (!string.IsNullOrEmpty(this.MobileNewFormUrl))
          return true;
        if (!string.IsNullOrEmpty(this.NewFormTemplateName))
          return true;
        if (!string.IsNullOrEmpty(this.NewFormUrl))
          return true;
        return (Hidden.HasValue || ReadOnly.HasValue || Sealed.HasValue);
      }
    }

  }
}
