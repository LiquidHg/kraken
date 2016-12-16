using Kraken.Tracing;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Client {
  public class ListOptions : ListCreationInformation {

    /*
    public string Title { get; set; }
    public string Description { get; set; }
      //base.DataSourceProperties
      //base.DocumentTemplateType
      //base.ListTemplate
      //base.TemplateType
      //base.TemplateFeatureId
      //base.QuickLaunchOption
      //base.Url
      //base.DataSourceProperties
      //base.CustomSchemaXml
    */

    public bool? OnQuickLaunch { get; set; }
    public bool? ContentTypesEnabled { get; set; }

    public string DocumentTemplateUrl { get; set; } = SKIP_PROPERTY;
    public DraftVisibilityType? DraftVersionVisibility { get; set; }
    public bool? EnableAttachments { get; set; }
    public string DefaultDisplayFormUrl { get; set; } = SKIP_PROPERTY;
    public string DefaultEditFormUrl { get; set; } = SKIP_PROPERTY;
    public string DefaultNewFormUrl { get; set; } = SKIP_PROPERTY;
    public bool? EnableFolderCreation { get; set; }
    public bool? EnableMinorVersions { get; set; }
    public bool? EnableModeration { get; set; }
    public bool? EnableVersioning { get; set; }
    public bool? ForceCheckout { get; set; }
    public bool? Hidden { get; set; }

    public Uri ImageUrl { get; set; }
    public bool? NoCrawl { get; set; }
    public ListTemplateType? TemplateTypeDefined { get; set; }
    public string TemplateTypeCustom { get; set; } = SKIP_PROPERTY;
    public string ValidationFormula { get; set; } = SKIP_PROPERTY;
    public string ValidationMessage { get; set; } = SKIP_PROPERTY;

    public string[] EnsureContentTypes { get; set; }
    public string[] RemoveContentTypes { get; set; }

    public string DefaultView { get; set; } = SKIP_PROPERTY;

    public const string SKIP_PROPERTY = "[SKIP_PROPERTY]";

    /// <summary>
    /// Indicates if the add / update operation
    /// should throw an error if it fails
    /// </summary>
    public bool ThrowOnError { get; set; } = true;

    public bool HasChangedValue(string val, string compareTo = "") {
      return (val != SKIP_PROPERTY && val != compareTo);
    }

    /// <summary>
    /// Returns true if one of the properties will
    /// have to be set after list creation because
    /// it is not supported in ListCreationInformation
    /// </summary>
    public bool HasExtendedSettings {
      get {
        return (this.ContentTypesEnabled.HasValue
          || this.DraftVersionVisibility.HasValue
          || this.EnableAttachments.HasValue
          || this.EnableFolderCreation.HasValue
          || this.EnableMinorVersions.HasValue
          || this.EnableModeration.HasValue
          || this.EnableVersioning.HasValue
          || this.ForceCheckout.HasValue
          || this.Hidden.HasValue
          || this.NoCrawl.HasValue
          || HasChangedValue(this.DefaultDisplayFormUrl)
          || HasChangedValue(this.DefaultEditFormUrl)
          || HasChangedValue(this.DefaultNewFormUrl)
          || HasChangedValue(this.DefaultView)
          || HasChangedValue(this.DocumentTemplateUrl)
          || this.ImageUrl != null
          || HasChangedValue(this.ValidationFormula)
          || HasChangedValue(this.ValidationMessage)
          //|| this.OnQuickLaunch.HasValue
          //|| this.QuickLaunchOption != QuickLaunchOptions.DefaultValue
          //|| HasChangedValue(this.Description)
          //|| HasChangedValue(this.Title)
        );
      }
    }

    public bool Validate(bool checkCreate, bool throwOnFail = true, ITrace trace = null) {
      DoConversions();
      bool isValid = true;
      try {
        if (checkCreate) {
          if (!HasChangedValue(Title))
            throw new ArgumentNullException("Title");
          if (!HasChangedValue(Description))
            throw new ArgumentNullException("Description");
          if (!HasChangedValue(TemplateTypeCustom)
            && !TemplateTypeDefined.HasValue)
            throw new ArgumentNullException("TemplateTypeDefined or TemplateTypeCustom");
        }
      } catch {
        isValid = false;
        if (throwOnFail)
          throw;
      }
      return isValid;
    }

    public void CopyFrom(List list) {
      this.Title = list.Title;
      //this.CustomSchemaXml = list.SchemaXml;
      //this.DataSourceProperties = list.DataSourceProperties;
      this.DefaultDisplayFormUrl = list.DefaultDisplayFormUrl;
      this.DefaultEditFormUrl = list.DefaultEditFormUrl;
      this.DefaultNewFormUrl = list.DefaultNewFormUrl;
      // TODO may need something special to load this...
      this.DefaultView = list.DefaultView.Title;
      this.Description = list.Description;
      //list.DescriptionResource
      //list.Direction 
      this.DocumentTemplateUrl = list.DocumentTemplateUrl;
      this.DraftVersionVisibility = list.DraftVersionVisibility;
      this.EnableAttachments = list.EnableAttachments;
      this.EnableFolderCreation = list.EnableFolderCreation;
      this.EnableMinorVersions = list.EnableMinorVersions;
      this.EnableModeration = list.EnableModeration;
      this.EnableVersioning = list.EnableVersioning;
      this.ForceCheckout = list.ForceCheckout;
      this.Hidden = list.Hidden;
      this.ImageUrl = (string.IsNullOrWhiteSpace(list.ImageUrl)) ? null : new Uri(list.ImageUrl);
      // TODO this needs fixing
      //list.ListExperienceOptions
      //this.ListTemplate = list.BaseTemplate;
      //list.MajorVersionLimit
      //list.MajorWithMinorVersionsLimit
      //list.MultipleDataList
      this.NoCrawl = list.NoCrawl;
      this.OnQuickLaunch = list.OnQuickLaunch;
      this.QuickLaunchOption = (list.OnQuickLaunch) ? QuickLaunchOptions.On : QuickLaunchOptions.Off;
      //list.ParserDisabled
      //list.ReadSecurity
      //list.RoleAssignments
      //list.Tag
      this.TemplateFeatureId = list.TemplateFeatureId;
      this.Title = list.Title;
      //list.TitleResource
      //list.Url
      //list.UserCustomActions
      this.ValidationFormula = list.ValidationFormula;
      this.ValidationMessage = list.ValidationMessage;
      DoConversions();
    }

    private void DoConversions(List list = null) {
      if (this.ListTemplate != null) {
        this.TemplateType = (Int32)this.ListTemplate.ListTemplateTypeKind;
        this.TemplateFeatureId = this.ListTemplate.FeatureId;
      } else if (this.TemplateTypeDefined.HasValue) {
        this.TemplateType = (Int32)this.TemplateTypeDefined.Value;
      } else if (HasChangedValue(this.TemplateTypeCustom)) {
        ClientContext context = (ClientContext)list.Context;
        Web web = context.Web;
        context.Load(web, w => w.ListTemplates);
        context.ExecuteQuery();
        ListTemplate listTemplate = web.ListTemplates.FirstOrDefault(lt => lt.Name == this.TemplateTypeCustom);
        if (listTemplate == null) {
          if (this.ThrowOnError)
            throw new ArgumentOutOfRangeException(string.Format("List template with name '{0}' does not exist in web '{1}'.", this.TemplateTypeCustom, web.UrlSafeFor2010()), "TemplateTypeCustom");
          return;
        }
        this.TemplateFeatureId = listTemplate.FeatureId;
        this.TemplateType = listTemplate.ListTemplateTypeKind;
      } else if (this.TemplateType <= 0) {
        if (this.ThrowOnError)
          throw new ArgumentNullException(string.Format("You must specify a pre-defined or custom list template."), "TemplateTypeDefined or TemplateTypeCustom");
        return;
      }
    }

public ListCreationInformation ConvertSP14Safe() {
      return new ListCreationInformation() {
        Title = this.Title,
        Description = this.Description,
        QuickLaunchOption = this.QuickLaunchOption, // (this.OnQuickLaunch ?? false) ? QuickLaunchOptions.On : QuickLaunchOptions.Off,
        TemplateFeatureId = this.TemplateFeatureId,
        TemplateType = this.TemplateType
      };
    }

    /*
    */
  }

}
