using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client {
  /// <summary>
  /// Extends ContentTypeCreationInformation to include additional properties that must be set after creation
  /// </summary>
  /* In older versions of CSOM some classes are sealed
   * which makes life difficult for us, but we'll have to make-do.
   */
#if !DOTNET_V35
  public class ContentTypeProperties : ContentTypeCreationInformation {
#else
  public class ContentTypeProperties {

    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Group { get; set; }
    public ContentType ParentContentType { get; set; }

#endif

    public ContentTypeCreationInformation ConvertSP14Safe() {
      return new ContentTypeCreationInformation() {
        Description = this.Description,
        Group = this.Group,
        Name = this.Name,
        Id = this.Id,
        ParentContentType = this.ParentContentType
      };
    }

    // TODO put this to work when updating content types
    public const string SKIP_PROPERTY = "[SKIP_PROPERTY]";

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
    public string Scope { get; set; }

    public void CopyForm(ContentType ct) {
      this.Description = ct.Description;
      this.DisplayFormTemplateName = ct.DisplayFormTemplateName;
      this.DisplayFormUrl = ct.DisplayFormUrl;
      this.DocumentTemplate = ct.DocumentTemplate;
      this.EditFormTemplateName = ct.EditFormTemplateName;
      this.EditFormUrl = ct.EditFormUrl;
      this.Group = ct.Group;
      this.Hidden = ct.Hidden;
      this.Id = ct.Id.StringValue;
      this.JSLink = ct.JSLink;
      this.MobileDisplayFormUrl = ct.MobileDisplayFormUrl;
      this.MobileEditFormUrl = ct.MobileEditFormUrl;
      this.MobileNewFormUrl = ct.MobileNewFormUrl;
      this.Name = ct.Name;
      this.NewFormTemplateName = ct.NewFormTemplateName;
      this.NewFormUrl = ct.NewFormUrl;
      this.ParentContentType = ct.Parent;
      this.ReadOnly = ct.ReadOnly;
      this.Scope = ct.Scope;
      this.Sealed = ct.Sealed;
    }

    /// <summary>
    /// Returns true if one of props properties will
    /// have to be set after content type creation because
    /// it is not supported in ContentTypeCreationInformation
    /// </summary>
    public bool HasExtendedSettings {
      get {
        ContentTypeProperties props = this;
        return (Hidden.HasValue || ReadOnly.HasValue || Sealed.HasValue
        || !string.IsNullOrWhiteSpace(props.JSLink)
        || !string.IsNullOrWhiteSpace(props.Scope)
        || !string.IsNullOrWhiteSpace(props.DocumentTemplate)
        || !string.IsNullOrWhiteSpace(props.DisplayFormTemplateName)
        || !string.IsNullOrWhiteSpace(props.DisplayFormUrl)
        || !string.IsNullOrWhiteSpace(props.MobileDisplayFormUrl)
        || !string.IsNullOrWhiteSpace(props.EditFormTemplateName)
        || !string.IsNullOrWhiteSpace(props.EditFormUrl)
        || !string.IsNullOrWhiteSpace(props.MobileEditFormUrl)
        || !string.IsNullOrWhiteSpace(props.NewFormTemplateName)
        || !string.IsNullOrWhiteSpace(props.NewFormUrl)
        || !string.IsNullOrWhiteSpace(props.MobileNewFormUrl)
       );
      }
    }

  } // class
}
