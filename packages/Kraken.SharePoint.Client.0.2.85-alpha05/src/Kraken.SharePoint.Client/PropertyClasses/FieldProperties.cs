using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml;

using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;

using Kraken;
using Kraken.Xml;

namespace Kraken.SharePoint.Client {

  /* Stub enum for older versions of CSOM */
#if DOTNET_V35
  public enum DateTimeFieldFriendlyFormatType {
    Unspecified,
    Relative
  }
#endif

  /// <summary>
  /// This is a POCO style clas used for passing large numbers of field properties 
  /// when creating or editing field definitions.
  /// </summary>
  /// <remarks>
  /// Thanks MS for using a boatload of optional attributed in XML
  /// without a decent way to deserialize the object. Ugh!
  /// </remarks>
  [XmlRoot("Field")]
  public class FieldProperties : IXmlSerializable {

    [XmlIgnore]
    public const int DefaultRegionId = 1033;

    public FieldProperties() {
    }
    /*
    public FieldProperties(Field f) {
      FieldProperties.Deserialize(f.SchemaXml);
    }
    */

    #region Properties

    /// <summary>
    /// Unique ID of the field. This property can only be set when creating 
    /// site column from XML elements and feature definitions.
    /// </summary>
    /// <remarks>
    /// ID was made read-only because it causes error
    /// "The local device name is already in use"
    /// http://www.sharepoint-tips.com/2010/07/local-device-name-is-already-in-use.html
    /// </remarks>
    //[XmlIgnore]
    [XmlAttribute]
    public Guid? Id { get; set; } // private 

    /// <summary>
    /// A static name for the field that can be used to retreive it when writing code.
    /// Unlike InternalName which could be adjusted to ensure the field is unique, the
    /// StaticName will be kept exactly as specified.
    /// </summary>
    [XmlAttribute]
    public string StaticName { get; set; }

    /// <summary>
    /// Name of the field in the underlying database.
    /// </summary>
    [XmlAttribute("ColName")]
    public string ColumnName { get; private set; }

    /// <summary>
    /// Non-changing system name of the field.
    /// Only certain characters are allowed.
    /// </summary>
    [XmlAttribute("Name")]
    public string InternalName { get; set; }

    /// <summary>
    /// Display name for the field.
    /// </summary>
    [XmlAttribute]
    public string DisplayName { get; set; }

    /// <summary>
    /// This description shows up in list forms.
    /// </summary>
    [XmlAttribute]
    public string Description { get; set; }

    /// <summary>
    /// This supplimental description is shown in pages when editing fields.
    /// It is targeted to site owners who are consuming the field in lists etc.
    /// </summary>
    [XmlAttribute]
    public string AuthoringInfo { get; set; }
    [XmlAttribute]
    public string Group { get; set; }

    [XmlIgnore]
    public string type = string.Empty;
    [XmlAttribute]
    public string Type {
      get { return type; }
      set {
        FieldTypeAlias typeAlias;
        if (Enum.TryParse(value, out typeAlias) && typeAlias != FieldTypeAlias.InvalidFieldType)
          this.TypeAlias = typeAlias; // automatically sets fieldProps.Type
        else {
          // TODO what about custom types???
          if (!string.IsNullOrEmpty(value) && !IsSupportedFieldType(value))
            throw new NotSupportedException(string.Format("The field type '{0}' is not a supported field type.", value));
          type = value;
        }
      }
    }

    public FieldType? TypeKind {
      get {
        FieldType t;
        if (Enum.TryParse(this.Type, out t))
          return t;
        return null;
      }
      set {
        this.Type = value.ToString();
      }
    }

    [XmlIgnore]
    private FieldTypeAlias _alias;
    [XmlIgnore]
    public FieldTypeAlias TypeAlias {
      get {
        /*
        FieldTypeAlias result = FieldTypeAlias.InvalidFieldType;
        if (!Enum.TryParse<FieldTypeAlias>(type, out result))
          return FieldTypeAlias.InvalidFieldType;
         */
        return _alias;
      }
      set {
        _alias = value;
        this.ConfigureFromAlias(value);
      }
    }

    /// <summary>
    /// For calculated fields and others, you should add one 
    /// one or more field references as needed.
    /// </summary>
    public List<Field> FieldRefs { get; set; } = new List<Field>();

    [XmlElement("Default")]
    public object DefaultValue { get; set; }
    [XmlElement("DefaulttFormula")]
    public string DefaultFormula { get; set; }

    /// <summary>
    /// The formula for calculated fields
    /// </summary>
    [XmlElement("Formula")]
    public string Formula { get; set; }

    /// <summary>
    /// The return type for calculated fields
    /// </summary>
    [XmlAttribute]
    public string ResultType { get; set; }
    // TODO valiudate that ResultType is a supported type

    [XmlAttribute]
    public bool? Hidden { get; set; }

    #region Helps with serialization - fucking obnoxious!

    /*
    [XmlAttribute("Hidden")] // or [XmlElement("SomeValue")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool XmlHidden { get { return Hidden.Value; } set { Hidden = value; } }
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool XmlHiddenSpecified { get { return Hidden.HasValue; } }
    */

    #endregion

    [XmlAttribute]
    public bool? ReadOnly { get; set; }
    [XmlAttribute]
    public bool? Required { get; set; }
    [XmlAttribute]

    public FieldLinkRequireStatus? Hiro {
      get {
        FieldLinkRequireStatus hiro = FieldLinkRequireStatus.Optional;
        if (this.Hidden.GetValueOrDefault())
          hiro = FieldLinkRequireStatus.Hidden;
        if (this.Required.GetValueOrDefault())
          hiro = FieldLinkRequireStatus.Required;
        return hiro;
      }
      set {
        switch (value) {
          case FieldLinkRequireStatus.Hidden:
            Hidden = true;
            Required = false;
            break;
          case FieldLinkRequireStatus.Required:
            Hidden = false;
            Required = true;
            break;
          case FieldLinkRequireStatus.Optional:
            Hidden = false;
            Required = false;
            break;
          case FieldLinkRequireStatus.Inherit:
            throw new NotSupportedException("FieldLinkRequireStatus.Inherit is not allowed for FieldProperties.");
        }
      }
    }

    public bool? AllowDeletion { get; set; }

    /// <summary>
    /// Show or hide the field in View Properties page.
    /// </summary>
    [XmlAttribute]
    public bool? ShowInDisplayForm { get; set; }

    /// <summary>
    /// Show or hide the field in Edit Properties page.
    /// </summary>
    [XmlAttribute]
    public bool? ShowInEditForm { get; set; }

    /// <summary>
    /// Show or hide the field in List Settings page.
    /// </summary>
    [XmlAttribute]
    public bool? ShowInListSettings { get; set; }

    /// <summary>
    /// Show or hide the field in Edit Properties page when creating a new item.
    /// </summary>
    [XmlAttribute]
    public bool? ShowInNewForm { get; set; }

    /// <summary>
    /// Show or hide the field in Create/Modify View pages.
    /// </summary>
    [XmlAttribute]
    public bool? ShowInViewForms { get; set; }

    /// <summary>
    /// Show or hide the field in Version History pages..
    /// </summary>
    [XmlAttribute]
    public bool? ShowInVersionHistory { get; set; }

    /// <summary>
    /// Show or hide the field in File Upload pages..
    /// </summary>
    public bool? ShowInFileDlg { get; set; }

    /// <summary>
    /// Force the value to be unique in the list.
    /// This setting is not appropriate to certain types of fields.
    /// </summary>
    [XmlAttribute]
    public bool? AllowDuplicateValues { get; set; }
    public bool? EnforceUniqueValues {
      get {
        if (!AllowDuplicateValues.HasValue)
          return null;
        return (!AllowDuplicateValues.Value);
      }
      set {
        if (!value.HasValue)
          AllowDuplicateValues = null;
        AllowDuplicateValues = !value.Value;
      }
    }
    //

    /// <summary>
    /// Index the field to optimize performance in large lists.
    /// </summary>
    [XmlAttribute]
    public bool? Indexed { get; set; }

    /// <summary>
    /// Flags for unique, indexed, and
    /// auto-indexed as represented by
    /// characters 'U', 'I', and 'A'
    /// </summary>
    public string UniqueIndexAutoFlags {
      get {
        string uniqueIndex = "";
        if (!this.AllowDuplicateValues.GetValueOrDefault())
          uniqueIndex += "U";
        if (this.Indexed.GetValueOrDefault())
          uniqueIndex += "I";
        if (this.AutoIndexed.GetValueOrDefault())
          uniqueIndex += "A";
        return uniqueIndex;
      }
      set {
        this.AllowDuplicateValues = !value.ToLower().Contains("u");
        this.Indexed = value.ToLower().Contains("i");
        this.AutoIndexed = value.ToLower().Contains("a");
      }
    }

    // TODO this is new, need to support it in XML
    public bool? AutoIndexed { get; set; }

    /// <summary>
    /// This field is used when updating site columns.
    /// </summary>
    [XmlAttribute]
    public bool? PushChangesToLists { get; set; }

    /// <summary>
    /// For secondary lookup fields it points to the primary lookup field
    /// </summary>
    [XmlAttribute]
    public Guid? FieldRef { get; set; }

    /// <summary>
    /// Returns true if the type is fully
    /// supported in Site Templates, false
    /// if support is limited or unsupported.
    /// </summary>
    /// <remarks>
    /// This is currently implemented as a test
    /// against Taxonomy fields.
    /// </remarks>
    public SiteTemplateSupportScope SiteTemplateSupport {
      get {
        // TODO also base this on if the field
        // is a lookup that references something
        // outside the current web
        // TODO base on locaton of formula field refs
        if (this.Type.Contains("Lookup"))
          return SiteTemplateSupportScope.Unknown;
        if (this.Type.Contains("TaxonomyFieldType"))
          return SiteTemplateSupportScope.Unknown;
        if (this.TypeAlias == FieldTypeAlias.Calculated)
          return SiteTemplateSupportScope.Unknown;
        // The rest should be OK
        return SiteTemplateSupportScope.Full;
      }
    }

    // TODO FileRefs

    /// <summary>
    /// Raw guid or string representing the target list for lookup fields.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: According to comment below, relative URLs are meant for WSP Solutions and have no 
    /// effect in this library.
    /// 
    /// If the target list does not yet exist, the value of the List attribute can be a web-relative 
    /// URL such as "Lists/My List" but only if the target list is created in the same feature as the 
    /// one that creates the lookup field. In this case, the value of the List attribute on the Field 
    /// element must be identical to the value of the Url attribute on the ListInstance element that 
    /// creates the target list.
    /// 
    /// On 3/3/2016 Tom adds: The exception to the above rule is user types, where List will equal
    /// "UserInfo" which is the name of a reserved table in the site collection root. Truly the ocean
    /// of SharePoint is deep and full of wondrous mysteries!
    /// </remarks>
    [XmlAttribute("List", Type = typeof(string), DataType = "string")]
    public string ListRaw {
      get;
      set;
    }

    /// <summary>
    /// GUID that identifies the target list. To indicate the same list ("Self"), leave this field 
    /// set to Guid.Empty or use the target list's ID.
    /// </summary>
    public Guid? ListId {
      get {
        Guid listId;
#if !DOTNET_V35
        if (Guid.TryParse(ListRaw, out listId))
#else
          bool isGuid = false; listId = Guid.Empty;
        try {
          listId = new Guid(ListRaw);
          isGuid = true;
        } catch { }
        if (isGuid)
#endif
          return listId;
        return null;
      }
      set {
        this.ListRaw = value.HasValue ? value.ToString() : string.Empty;
      }
    }

    /// <summary>
    /// For Lookup fields, allow multiple selections.
    /// </summary>
    /// <remarks>
    /// Don't believe that stuff in the documentation about Mult being for
    /// multiplication factor or some other weird math thing; it's Bull$h!t!
    /// </remarks>
    [XmlAttribute]
    public bool? Mult { get; set; }

    /// <summary>
    /// When the Type attribute is "Lookup" the value of the ShowField attribute specifies the 
    /// internal name of the target field to look up. If no value is specified, the hyperlinked 
    /// text from the Title field of the record in the target list is displayed.
    /// </summary>
    /// <remarks>
    /// The following field types are allowed as the target of a lookup field: Counter, DateTime, Number, 
    /// and Text. The Computed field type can be a target if lookups are enabled. For more information, 
    /// see the EnableLookup property of the SPFieldComputed class. The Calculated field type can be a 
    /// target if the output is text. For more information, see the OutputType property of the 
    /// SPFieldCalculated class.
    /// Other notes: ShowField="Text" | "Choice" | "Counter"
    /// </remarks>
    [XmlAttribute]
    public string ShowField { get; set; }
    [XmlAttribute]
    public Guid? WebId { get; set; }

    [XmlAttribute]
    public string Format { get; set; }

    /// <summary>
    /// For text types, the max length allowed.
    /// </summary>
    [XmlAttribute]
    public int? MaxLength { get; set; }

    [XmlAttribute]
    public bool? AppendOnly { get; set; }
    [XmlAttribute]
    public bool? RichText { get; set; }
    [XmlAttribute]
    public RichTextMode? RichTextMode { get; set; }
    /// <summary>
    /// The number of lines for a multi-line Notes (rich text) field.
    /// </summary>
    [XmlAttribute]
    public int? DisplaySize { get; set; }
    [XmlAttribute]
    public bool? UnlimitedLengthInDocumentLibrary { get; set; }

    /// <summary>
    /// For choice fields, determines if fill-in text is allowed.
    /// </summary>
    [XmlAttribute]
    public bool? FillInChoice { get; set; }

    [XmlElement("CHOICES")]
    public string[] Choices { get; set; }

    /// <summary>
    /// Specify what character will be used to parse ChoicesDelimited.
    /// Default is pipe '|'
    /// </summary>
    public char ChoicesDelimChar { get; set; } = '|';

    /// <summary>
    /// Shorthand property allows you to
    /// quickly get/set Choices using
    /// a pipe delimited string.
    /// </summary>
    public string ChoicesDelimited {
      get {
        string choices = (this.Choices != null)
          ? string.Join(ChoicesDelimChar.ToString(), this.Choices)
          : string.Empty;
        return choices;
      }
      set {
        char[] delim = new char[] { ChoicesDelimChar };
        this.Choices = value.Split(delim, StringSplitOptions.None);
      }
    }

    [XmlElement("MAPPINGS")]
    public Dictionary<string, string> MappedChoices { get; set; }

    /// <summary>
    /// For numeric types the maximum allowed value.
    /// </summary>
    [XmlAttribute]
    public decimal? Max { get; set; }
    /// <summary>
    /// For numeric types the minimum allowed value.
    /// </summary>
    [XmlAttribute]
    public decimal? Min { get; set; }
    [XmlAttribute]
    public bool? Percentage { get; set; }
    [XmlAttribute]
    public int? Decimals { get; set; }

    [XmlAttribute]
    public string TermSetName { get; set; }
    [XmlAttribute("LCID")]
    public int? RegionId { get; set; }

    [XmlAttribute]
    public bool? AllowHyperlink { get; set; }
    [XmlAttribute]
    public bool? CanToggleHidden { get; set; }
    [XmlAttribute]
    public bool? Commas { get; set; }
    [XmlAttribute]
    public bool? Filterable { get; set; }
    [XmlAttribute]
    public bool? FilterableNoRecurrence { get; set; }
    [XmlAttribute]
    public bool? HTMLEncode { get; set; }
    [XmlAttribute]
    public bool? IsolateStyles { get; set; }
    [XmlAttribute]
    public bool? NoEditFormBreak { get; set; }
    [XmlAttribute]
    public bool? Overwrite { get; set; }
    [XmlAttribute]
    public bool? OverwriteInChildScopes { get; set; }
    [XmlAttribute]
    public bool? PrependId { get; set; }
    [XmlAttribute]
    public bool? ReadOnlyEnforced { get; set; }
    [XmlAttribute]
    public bool? RestrictedMode { get; set; }
    [XmlAttribute]
    public bool? Sortable { get; set; }
    [XmlAttribute]
    public bool? StripWS { get; set; }
    [XmlAttribute]
    public bool? TextOnly { get; set; }
    [XmlAttribute]
    public bool? URLEncode { get; set; }
    [XmlAttribute]
    public bool? URLEncodeAsURL { get; set; }
    [XmlAttribute]
    public bool? Viewable { get; set; }
    [XmlAttribute]
    public bool? WikiLinking { get; set; }

    [XmlAttribute] // for deserialization
    protected string StorageTZ { get; set; }

    [XmlIgnore]
    public bool? IsStorageTZEnabled { get; set; }

    [XmlAttribute]
    protected FieldUserSelectionMode? UserSelectionMode { get; set; }
    // TODO resolve a group name to an ID for UserSelectionScope
    /// <summary>
    /// Set this to 0 for all users or to limit it to a specific group,
    /// look the group up in UserInfo table then resolve its ID and proivide that here.
    /// </summary>
    [XmlAttribute]
    protected int? UserSelectionScope { get; set; }

    [XmlAttribute]
    public RtlDirType? Dir { get; set; }
    [XmlAttribute]
    public string ForcedDisplay { get; set; }
    [XmlAttribute]
    public NegativeFormatType? NegativeFormat { get; set; }
    [XmlAttribute]
    public int? NumLines { get; set; }
    [XmlAttribute]
    public string SourceID { get; set; }
    [XmlAttribute]
    public string Title { get; set; }

    [XmlAttribute]
    public bool? LinkToItem { get; set; }
    [XmlAttribute]
    public ListItemMenuState? LinkToItemAllowed { get; set; }
    [XmlAttribute]
    public bool? ListItemMenu { get; set; }
    [XmlAttribute]
    public ListItemMenuState? ListItemMenuAllowed { get; set; }
    [XmlAttribute]
    public bool? CalloutMenu { get; set; }
    [XmlAttribute]
    public ListItemMenuState? CalloutMenuAllowed { get; set; }

    /// <summary>
    /// Make sure you use InternalName per the web articles below:
    ///  Through the UI you use the display name of the fields with brackets around it.
    ///  In the CAML you use the internal name of the fields with no brackets.
    /// http://spnovocaine.wordpress.com/2011/08/18/specify-list-item-validation-using-caml-in-schema-xml/
    /// http://sharepoint.stackexchange.com/questions/98122/column-validation-formula-within-and-outside-of-a-list
    /// </summary>
    [XmlElement("Validation")]
    public string ValidationFormula { get; set; }
    // TODO implement a sub-class to deserialize this...
    //[XmlAttribute("Message")]
    [XmlIgnore]
    public string ValidationMessage { get; set; }
    //[XmlAttribute("Script")]
    [XmlIgnore]
    public string ValidationEcmaScript { get; set; }

    [XmlAttribute]
    public int? CalType { get; set; }

#if !DOTNET_V35
    /// <summary>
    /// In SP14, it is a black hole that does nothing, but included here to allow code that works with it to function
    /// </summary>
#endif
    [XmlAttribute]
    public DateTimeFieldFriendlyFormatType? FriendlyDisplayFormat { get; set; }

    // to be implemented for future support
    [XmlAttribute]
    public int? Width { get; set; }
    [XmlAttribute]
    public int? Height { get; set; }
    [XmlAttribute]
    public bool? SuppressNameDisplay { get; set; }
    [XmlAttribute]
    public int? Div { get; set; }

    [XmlIgnore]
    public bool AllowMulti {
      get {
        return FieldUtility.IsMultiSelectFieldType(this.Type);
      }
    }

    [XmlIgnore]
    public string LookupListUrl { get; set; }
    [XmlIgnore]
    public string[] LookupAdditionalFields { get; set; }

    /// <summary>
    /// Type aware property that will set
    /// the correct properties based on Type
    /// and presence of = in the value
    /// </summary>
    public object DefaultValueOrFormula {
      get {
        if (this.TypeKind == FieldType.Calculated) {
          return Formula;
        } else if (!string.IsNullOrEmpty(this.DefaultFormula)) {
          return DefaultFormula;
        } else {
          return DefaultValue;
        }
      }
      set {
        if (value == null) {
          this.DefaultFormula = string.Empty;
          this.DefaultValue = null;
          if (this.TypeKind == FieldType.Calculated)
            this.Formula = string.Empty;
          return;
        }
        string defaultValueOrFormula = value.ToString();
        if (this.TypeKind == FieldType.Calculated) {
          if (!string.IsNullOrWhiteSpace(defaultValueOrFormula)) {
            // fixup the missing = when people forget
            if (!defaultValueOrFormula.StartsWith("="))
              defaultValueOrFormula = "=" + defaultValueOrFormula;
            this.Formula = defaultValueOrFormula;
            this.DefaultFormula = string.Empty;
            this.DefaultValue = null;
          }
        } else if (defaultValueOrFormula.StartsWith("=")) {
          this.DefaultFormula = defaultValueOrFormula;
          this.DefaultValue = null;
          this.Formula = string.Empty;
        } else {
          this.DefaultValue = value;
          this.DefaultFormula = string.Empty;
          this.Formula = string.Empty;
        }
      }
      // TODO lifted from FieldPropertiesExtensions
    }

    public bool IsLookupField {
      get {
        // TODO does User count as a lookup field??
        return FieldUtility.IsLookupFieldType(this.Type);
      }
    }
    public bool IsChoiceField {
      get {
        return FieldUtility.IsChoiceFieldType(this.Type);
      }
    }
    public bool IsTaxonomyField {
      get {
        return FieldUtility.IsTaxonomyFieldType(this.Type);
      }
    }

    #endregion

    public bool IsSupportedFieldType() {
      return IsSupportedFieldType(this.Type);
    }
    protected bool IsSupportedFieldType(string type) {
      // will throw an error if the field type isn't supported
      Enum ft = FieldUtility.ValidateFieldType(type, true, false);
      if (ft == null)
        return false;
      if (ft.GetType() == typeof(FieldTypeExtended)) {
        return true;
      }
      if (ft.GetType() == typeof(FieldType)) {
        switch ((FieldType)ft) {
          case FieldType.Boolean:
          case FieldType.Calculated:
          case FieldType.Choice:
          case FieldType.Currency:
          case FieldType.DateTime:
          case FieldType.MultiChoice:
          case FieldType.Note:
          case FieldType.Number:
          case FieldType.URL:
          case FieldType.User:
          case FieldType.Text:
          case FieldType.Lookup: // TODO still work to do in order to make this work properly
            return true;
            // TODO implement support for these types
            //case FieldType.Computed:
            //case FieldType.PageSeparator:
            //case FieldType.Recurrence:
            //case FieldType.AllDayEvent:
            //case FieldType.Guid:
            //case FieldType.CrossProjectLink:
        }
      }
      return false;
    }

    /// <summary>
    /// Clean up properties that don't make sense
    /// This is different from Validate in that it
    /// will never fail. It's called at the top of ValidateProperties()
    /// </summary>
    public void CleanUp() {
      if (FieldUtility.IsFormulaFieldType(this.Type)) {
        // p was probably meant for Formula - because how do you even have a default value in a calc field?
        if (this.DefaultValue != null && !string.IsNullOrWhiteSpace(this.DefaultValue.ToString()) && string.IsNullOrWhiteSpace(this.Formula)) {
          this.Formula = this.DefaultValue.ToString();
          this.DefaultValue = null;
        }
        // fixup the missing = when people forget
        if (!string.IsNullOrEmpty(Formula) && !this.Formula.StartsWith("="))
          this.Formula = "=" + this.Formula;
      }
      // create today where people are lazy and use the wrong syntax
      if (FieldUtility.IsFieldType(this.Type, FieldType.DateTime)
        && this.DefaultValueOrFormula != null && this.DefaultValueOrFormula.ToString().Equals("Today", StringComparison.InvariantCultureIgnoreCase)) {
        this.DefaultValueOrFormula = "=[TODAY]";
      }
      // TODO support same as above for [ME]
      if (FieldUtility.IsUserFieldType(this.Type)) {
        // NOTE we don't actually have List here we use LookupListID, so set the List attrib deeper in the code
        // it is actually done in the XML renderer for FieldProperties
        if (!this.UserSelectionMode.HasValue)
          this.UserSelectionMode = FieldUserSelectionMode.PeopleOnly;
        if (!this.UserSelectionScope.HasValue)
          this.UserSelectionScope = 0; // users in all groups;
        // TODO support Group attribute
      }
    }

    public void ConfigureFromAlias(FieldTypeAlias alias) {
      type = FieldUtility.Convert(alias);
      switch (alias) {
        case FieldTypeAlias.MoneyPenny: //haha - an omage!
          if (!this.Decimals.HasValue)
            this.Decimals = 2;
          break;
        case FieldTypeAlias.Money:
          if (!this.Decimals.HasValue)
            this.Decimals = 0;
          break;
        case FieldTypeAlias.Percent:
          this.Percentage = true;
          break;
        case FieldTypeAlias.MultilineText:
        case FieldTypeAlias.TextBox:
          if (!this.RichText.HasValue)
            this.RichText = false;
          break;
        case FieldTypeAlias.RichText:
          if (!this.RichText.HasValue)
            this.RichText = true;
          break;
        case FieldTypeAlias.Date:
#if !DOTNET_V35
          if (string.IsNullOrWhiteSpace(this.Format))
#else
          if (string.IsNullOrEmpty(this.Format))
#endif
            this.Format = DateTimeFieldFormatType.DateOnly.ToString();
          break;
        case FieldTypeAlias.FriendlyDate:
          this.FriendlyDisplayFormat = DateTimeFieldFriendlyFormatType.Relative;
          break;
        case FieldTypeAlias.Person:
        case FieldTypeAlias.People:
          this.UserSelectionMode = FieldUserSelectionMode.PeopleOnly;
          break;
        case FieldTypeAlias.PersonOrGroup:
        case FieldTypeAlias.PeopleAndGroups:
          this.UserSelectionMode = FieldUserSelectionMode.PeopleAndGroups;
          break;
        case FieldTypeAlias.ChoiceWithOther:
        case FieldTypeAlias.MultiChoiceWithOther:
          this.FillInChoice = true;
          break;
      }
      switch (alias) {
        case FieldTypeAlias.UserMulti:
        case FieldTypeAlias.LookupMulti:
        case FieldTypeAlias.People:
        case FieldTypeAlias.PeopleAndGroups:
          //this.AllowMulti = true;
          this.Mult = true;
          break;
      }
    }

    #region Property Validation

    private void ThrowInvalidProperty(string propertyName, ICollection<Enum> allowedTypes) {
      string typeList = string.Empty;
      foreach (Enum ft in allowedTypes) {
        if (!string.IsNullOrEmpty(typeList))
          typeList += ",";
        typeList += ft.GetType().Name + "." + ft.ToString();
      }
      throw new NotSupportedException(string.Format("You should only specify '{0}' property for '{1}' field type(s).", propertyName, typeList));
    }

    private bool ValidateOptionalProperty(string propertyName, string property, ICollection<Enum> allowedTypes, bool throwOnInvalid = true) {
      if (string.IsNullOrEmpty(property))
        return true;
      return ValidateOptionalProperty(propertyName, allowedTypes, throwOnInvalid);
    }
    private bool ValidateOptionalProperty<T>(string propertyName, Nullable<T> property, ICollection<Enum> allowedTypes, bool throwOnInvalid = true) where T : struct {
      if (!property.HasValue)
        return true;
      return ValidateOptionalProperty(propertyName, allowedTypes, throwOnInvalid);
    }
    private bool ValidateOptionalProperty<T>(string propertyName, object property, ICollection<Enum> allowedTypes, bool throwOnInvalid = true) where T : class {
      if (property == null)
        return true;
      return ValidateOptionalProperty(propertyName, allowedTypes, throwOnInvalid);
    }

    private bool ValidateOptionalProperty(string propertyName, ICollection<Enum> allowedTypes, bool throwOnInvalid = true) {
      foreach (Enum ft in allowedTypes) {
        if (FieldUtility.IsFieldType(this.Type, ft))
          return true;
      }
      if (throwOnInvalid)
        ThrowInvalidProperty(propertyName, allowedTypes);
      return false;
    }

    public void ValidateProperties() {
      // This takes care of some user-based dumbness
      CleanUp();
      // Can't do much without these required properties
      if (string.IsNullOrEmpty(this.InternalName))
        throw new ArgumentNullException("InternalName");
      if (string.IsNullOrEmpty(this.DisplayName))
        throw new ArgumentNullException("DisplayName");
      if (string.IsNullOrEmpty(this.Group))
        throw new ArgumentNullException("Group");
      if (string.IsNullOrEmpty(this.Type))
        throw new ArgumentNullException("Type");
      // Check the type make sure it is supported
      if (!IsSupportedFieldType(this.Type))
        throw new NotSupportedException(string.Format("The field type '{0}' is not a supported field type.", this.Type));
      // text
      ValidateOptionalProperty("MaxLength", this.MaxLength, new Enum[] { FieldType.Text });
      // TODO allow this for calculated and computed fields also?
      ValidateOptionalProperty("TextOnly", this.TextOnly, new Enum[] { FieldType.Text, FieldType.Note });
      ValidateOptionalProperty("HTMLEncode", this.HTMLEncode, new Enum[] { FieldType.Text, FieldType.Note });
      ValidateOptionalProperty("StripWS", this.StripWS, new Enum[] { FieldType.Text, FieldType.Note });
      ValidateOptionalProperty("URLEncode", this.URLEncode, new Enum[] { FieldType.Text, FieldType.Note });
      ValidateOptionalProperty("URLEncodeAsURL", this.URLEncodeAsURL, new Enum[] { FieldType.Text, FieldType.Note });
      ValidateOptionalProperty("Dir", this.Dir, new Enum[] { FieldType.Text, FieldType.Note });
      // note
      ValidateOptionalProperty("NumLines", this.NumLines, new Enum[] { FieldType.Note });
      ValidateOptionalProperty("AllowHyperlink", this.AllowHyperlink, new Enum[] { FieldType.Note });
      ValidateOptionalProperty("IsolateStyles", this.IsolateStyles, new Enum[] { FieldType.Note });
      ValidateOptionalProperty("NoEditFormBreak", this.NoEditFormBreak, new Enum[] { FieldType.Note });
      ValidateOptionalProperty("RestrictedMode", this.RestrictedMode, new Enum[] { FieldType.Note });
      ValidateOptionalProperty("WikiLinking", this.WikiLinking, new Enum[] { FieldType.Note });
      ValidateOptionalProperty("DisplaySize", this.DisplaySize, new Enum[] { FieldType.Note });
      ValidateOptionalProperty("RichText", this.RichText, new Enum[] { FieldType.Note });
      ValidateOptionalProperty("RichTextMode", this.NumLines, new Enum[] { FieldType.Note });
      ValidateOptionalProperty("DisplaySize", this.DisplaySize, new Enum[] { FieldType.Note });
      ValidateOptionalProperty("UnlimitedLengthInDocumentLibrary", this.UnlimitedLengthInDocumentLibrary, new Enum[] { FieldType.Note });
      // choice - IsChoiceFieldType comparison covers Choice and MultiChoice types
      if (this.Choices != null && this.Choices.Count() > 0 && !FieldUtility.IsChoiceFieldType(this.Type))
        throw new NotSupportedException("You should only specify Choices for Choice fields.");
      if (this.FillInChoice.HasValue && !FieldUtility.IsChoiceFieldType(this.Type))
        throw new NotSupportedException("You should only specify FillInChoice for Choice fields.");
      if (FieldUtility.IsChoiceFieldType(this.Type)) {
        if (this.Choices == null)
          throw new ArgumentNullException("You should provide choices collection when using a Choice field.");
        else if (this.Choices.Count() == 0)
          throw new ArgumentNullException("You should specify at least one valid choice for a Choice field.");
        if (this.MappedChoices != null)
          if (this.MappedChoices.Count() != this.Choices.Count())
            throw new ArgumentNullException("MappedChoices should have the same number of items as Choices.");
      }
      // lookup
      ValidateOptionalProperty("FieldRef", this.FieldRef, new Enum[] { FieldType.Lookup, FieldType.User, FieldType.Computed }); // TODO is this needed for calculated?
      ValidateOptionalProperty("ListId", this.ListId, new Enum[] { FieldType.Lookup, FieldType.User });
      ValidateOptionalProperty("PrependId", this.PrependId, new Enum[] { FieldType.Lookup });
      ValidateOptionalProperty("Mult", this.Mult, new Enum[] { FieldType.Lookup, FieldType.User, FieldTypeExtended.UserMulti });
      ValidateOptionalProperty("ShowField", this.ShowField, new Enum[] { FieldType.Lookup, FieldType.User, FieldTypeExtended.UserMulti });
      // users
      ValidateOptionalProperty("UserSelectionMode", this.UserSelectionMode, new Enum[] { FieldType.User, FieldTypeExtended.UserMulti });
      ValidateOptionalProperty("UserSelectionScope", this.UserSelectionScope, new Enum[] { FieldType.User, FieldTypeExtended.UserMulti });

      // TODO decide if we just want to warn when this happens
      if (this.Indexed.HasValue && this.Indexed.Value == true && FieldUtility.IsFieldType(this.Type, FieldType.Lookup))
        throw new NotSupportedException("Although you can index a Lookup column to improve performance, using an indexed Lookup column to prevent exceeding the list view threshold does not work. To avoid exceeding the list view threshold, use another type of column as the primary or secondary index.");
      // managed metadata
      // TODO this may also be used for LCID in which case, take this error away
      if (this.RegionId.HasValue && !FieldUtility.IsTaxonomyFieldType(this.Type))
        throw new NotSupportedException("You should only specify RegionId for managed metadata fields.");
      if (!string.IsNullOrEmpty(this.TermSetName) && !FieldUtility.IsTaxonomyFieldType(this.Type))
        throw new NotSupportedException("You should only specify TermSetName for managed metadata fields.");
      // number
      ValidateOptionalProperty("Percentage", this.Percentage, new Enum[] { FieldType.Number });
      ValidateOptionalProperty("Div", this.Div, new Enum[] { FieldType.Number });
      ValidateOptionalProperty("Decimals", this.Decimals, new Enum[] { FieldType.Number, FieldType.Currency });
      ValidateOptionalProperty("Max", this.Max, new Enum[] { FieldType.Number, FieldType.Currency, FieldType.Integer });
      ValidateOptionalProperty("Min", this.Min, new Enum[] { FieldType.Number, FieldType.Currency, FieldType.Integer });
      ValidateOptionalProperty("Commas", this.Commas, new Enum[] { FieldType.Number, FieldType.Currency, FieldType.Integer });
      ValidateOptionalProperty("NegativeFormat", this.NegativeFormat, new Enum[] { FieldType.Number, FieldType.Currency, FieldType.Integer });
      // calcualted fields
      ValidateOptionalProperty("Formula", this.Formula, new Enum[] { FieldType.Calculated });
      ValidateOptionalProperty("ResultType", this.ResultType, new Enum[] { FieldType.Calculated });
      // dates
      ValidateOptionalProperty("CalType", this.CalType, new Enum[] { FieldType.DateTime });
      ValidateOptionalProperty("IsStorageTZEnabled", this.IsStorageTZEnabled, new Enum[] { FieldType.DateTime });
#if !DOTNET_V35
      if (this.FriendlyDisplayFormat != null && this.FriendlyDisplayFormat.HasValue && this.FriendlyDisplayFormat.Value == DateTimeFieldFriendlyFormatType.Unspecified)
        this.FriendlyDisplayFormat = null;
      ValidateOptionalProperty("FriendlyDisplayFormat", this.FriendlyDisplayFormat, new Enum[] { FieldType.DateTime });
#endif
      // formats
      if (!string.IsNullOrEmpty(this.Format))
        FieldUtility.ValidateFieldFormat(this.Type, this.Format);
    }

    #endregion

    #region Implementation for XML generation

    /// <summary>
    /// Attempot to detemrine the optimal schema generation methodology
    /// based on how many fields 
    /// </summary>
    /// <returns></returns>
    protected FieldSchemaGenerationMethod SelectFieldSchemaGenerationMethod() {
      FieldSchemaGenerationMethod method = FieldSchemaGenerationMethod.Complex;
      /*
      if (this.DefaultValue == null && choices == null) {
        method = FieldSchemaGenerationMethod.UltraSimple;
      }
       */
      return method;
    }

    public string GenerateSchemaXml(FieldSchemaGenerationMethod method = FieldSchemaGenerationMethod.Auto) {
      if (FieldSchemaGenerationMethod.Auto == method)
        method = SelectFieldSchemaGenerationMethod();
      string schemaXml = string.Empty;
      if (string.IsNullOrEmpty(this.InternalName))
        throw new ArgumentNullException("InternalName");
      if (string.IsNullOrEmpty(this.Type))
        throw new ArgumentNullException("Type");
      if (string.IsNullOrEmpty(this.Group))
        throw new ArgumentNullException("Group");
      StringBuilder sb = new StringBuilder();
      // What a pain in the ass! It would've been MUCH less tedious to use reflection here
      // However there are CAS policies we need to think about, like sandbox code and app framework
      switch (method) {
        case FieldSchemaGenerationMethod.Complex:
          sb.Append("<Field");
          // required fields, passed to AppendAttribute so they get the proper encoding for XML attributes
          // TODO if we used an XmlWriter instead we'd also get better encoding here
          sb.AppendAttribute("Name", this.InternalName);
          sb.AppendAttribute("DisplayName", this.DisplayName);
          sb.AppendAttribute("Group", this.Group);
          sb.AppendAttribute("Type", this.Type);

          // TOOD aphabetize this list
          //sb.AppendAttribute("Aggregation", Aggregation);
          sb.AppendAttribute("ID", Id);
          sb.AppendAttribute("AllowHyperlink", AllowHyperlink);
          sb.AppendAttribute("AllowDeletion", AllowDeletion);
          sb.AppendAttribute("AllowDuplicateValues", AllowDuplicateValues);
          sb.AppendAttribute("AppendOnly", AppendOnly);
          sb.AppendAttribute("AuthoringInfo", AuthoringInfo);
          // for more info about using CalloutMenu, CalloutMenuAllowed ...
          sb.AppendAttribute("CalloutMenu", CalloutMenu);
          sb.AppendAttribute("CalloutMenuAllowed", CalloutMenuAllowed);
          sb.AppendAttribute("CalType", CalType);
          sb.AppendAttribute("CanToggleHidden", CanToggleHidden);
          sb.AppendAttribute("Commas", Commas);
          sb.AppendAttribute("Description ", Description);
          sb.AppendAttribute("Decimals", Decimals);
          sb.AppendAttribute("Dir", Dir);
          sb.AppendAttribute("DisplaySize", DisplaySize);
          sb.AppendAttribute("Div", Div);
          sb.AppendAttribute("FieldRef", FieldRef);
          sb.AppendAttribute("FillInChoice", FillInChoice);
          sb.AppendAttribute("Filterable", Filterable);
          sb.AppendAttribute("FilterableNoRecurrence", FilterableNoRecurrence);
          sb.AppendAttribute("ForcedDisplay", ForcedDisplay);
          sb.AppendAttribute("Format", Format);
#if !DOTNET_V35
          // Only include FriendlyDisplayFormat in cases where it can be used
          if (
            Type == FieldType.DateTime.ToString()
            || (Type == FieldType.Calculated.ToString()
            && ResultType == FieldType.DateTime.ToString())
          ) {
            // Only include it if it was provided
            // TODO in validation maybe we should be doing more sophisticated checks
            if (FriendlyDisplayFormat.HasValue) {
              sb.AppendAttribute("FriendlyDisplayFormat", FriendlyDisplayFormat.Value);
            }
          }
#endif
          sb.AppendAttribute("Hidden", Hidden);
          sb.AppendAttribute("HTMLEncode", HTMLEncode);
          sb.AppendAttribute("Indexed", Indexed);
          sb.AppendAttribute("IsolateStyles", IsolateStyles);
          sb.AppendAttribute("LCID", RegionId);
          // for more info about using LinkToItem, LinkToItemAllowed, ListItemMenu see: 
          // http://msdn.microsoft.com/en-us/library/microsoft.sharepoint.spfield.linktoitemallowed(v=office.15).aspx
          // http://sharepoint.stackexchange.com/questions/16766/display-the-sharepoint-context-menu-in-list-items-on-another-column-instead-of-t
          // have to admin it does not make much sense to configure these in the site column itself
          // here says to do it in the FieldRef instead, kinda weird
          // http://www.ilikesharepoint.de/2014/02/do-you-want-to-have-the-link-menu-and-link-to-item-at-a-different-column-in-sharepoint/
          // TODO these properties haven't been exposed in the PowerShell calling routine
          sb.AppendAttribute("LinkToItem", LinkToItem);
          sb.AppendAttribute("LinkToItemAllowed", LinkToItemAllowed);
          if (TypeKind == FieldType.User) {
            sb.AppendAttribute("List", "UserInfo");
          } else {
            if (ListId.HasValue)
              sb.AppendAttribute("List", (Guid.Empty == ListId.Value) ? "Self" : ListId.Value.ToString());
          }
          sb.AppendAttribute("ListItemMenu", ListItemMenu);
          sb.AppendAttribute("ListItemMenuAllowed", ListItemMenuAllowed);
          sb.AppendAttribute("Max", Max);
          sb.AppendAttribute("MaxLength", MaxLength);
          sb.AppendAttribute("Min", Min);
          sb.AppendAttribute("Mult", Mult);
          sb.AppendAttribute("NegativeFormat", NegativeFormat);
          sb.AppendAttribute("NoEditFormBreak", NoEditFormBreak);
          sb.AppendAttribute("NumLines", NumLines);
          sb.AppendAttribute("Overwrite", Overwrite);
          sb.AppendAttribute("OverwriteInChildScopes", OverwriteInChildScopes);
          sb.AppendAttribute("Percentage", Percentage);
          sb.AppendAttribute("PrependId", PrependId);
          sb.AppendAttribute("ReadOnly", ReadOnly);
          sb.AppendAttribute("ReadOnlyEnforced", ReadOnlyEnforced);
          sb.AppendAttribute("Required", Required);
          // for more info about how to create the different types of rich text fields, see:
          // http://www.concurrency.com/blog/sharepoint-lists-creating-multiline-text-columns/
          // somewhat unrelated but kinda fun  
          // http://stevemannspath.blogspot.com/2013/10/sharepoint-2013-adding-rich-text-column.html
          sb.AppendAttribute("RestrictedMode", RestrictedMode);
          if (TypeKind == FieldType.Calculated
            || TypeKind == FieldType.Computed) {
            sb.AppendAttribute("ResultType", ResultType);
          }
          sb.AppendAttribute("RichText", RichText);
          sb.AppendAttribute("RichTextMode", RichTextMode);
          sb.AppendAttribute("ShowField", ShowField);
          sb.AppendAttribute("ShowInDisplayForm", ShowInDisplayForm);
          sb.AppendAttribute("ShowInEditForm", ShowInEditForm);
          sb.AppendAttribute("ShowInFileDlg", ShowInFileDlg);
          sb.AppendAttribute("ShowInListSettings", ShowInListSettings);
          sb.AppendAttribute("ShowInNewForm", ShowInNewForm);
          sb.AppendAttribute("ShowInVersionHistory", ShowInVersionHistory);
          sb.AppendAttribute("ShowInViewForms", ShowInViewForms);
          sb.AppendAttribute("Sortable", Sortable);
          sb.AppendAttribute("SourceID", SourceID);
          sb.AppendAttribute("StaticName", StaticName);
          if (IsStorageTZEnabled.HasValue)
            sb.AppendAttribute("StorageTZ", (IsStorageTZEnabled.Value) ? "UTC" : "Abstract");
          sb.AppendAttribute("StripWS", StripWS);
          sb.AppendAttribute("SuppressNameDisplay", SuppressNameDisplay);
          sb.AppendAttribute("TextOnly", TextOnly);
          sb.AppendAttribute("Title", Title);
          sb.AppendAttribute("UnlimitedLengthInDocumentLibrary", UnlimitedLengthInDocumentLibrary);
          sb.AppendAttribute("URLEncode", URLEncode);
          sb.AppendAttribute("URLEncodeAsURL", URLEncodeAsURL);
          sb.AppendAttribute("UserSelectionMode", UserSelectionMode);
          sb.AppendAttribute("UserSelectionScope", UserSelectionScope);
          sb.AppendAttribute("Viewable", Viewable);
          sb.AppendAttribute("WikiLinking", WikiLinking);
          sb.AppendAttribute("WebId", WebId);
          // yet another advanced topic - title field override in Office 365
          // http://community.office365.com/en-us/f/154/t/45407.aspx
          /*
            * Here's the list of other attributes we chose not to implement at this time
          // XML properties
          Aggregation="sum" | "count" | "average" | "min" | "max" | "merge" | "plaintext" | "first" | "last"
          PIAttribute="Text"
          PITarget="Text"
          PrimaryPIAttribute="Text"
          PrimaryPITarget="Text"
          Node="Text"
          XName
          // Others
          AllowMultiVote="TRUE" | "FALSE"
          BaseType="Integer" | "Text"
          ClassInfo 
          ColName 
          Customization 
          DefaultListField="TRUE" | "FALSE"
          DisplaceOnUpgrade="TRUE" | "FALSE"
          DisplayImage="Text"
          DisplayNameSrcField="Text"
          EnableLookup="TRUE" | "FALSE"
          ExceptionImage="Text"
          FromBaseType="TRUE" | "FALSE"
          HeaderImage="Text"
          Height="Integer"
          ID="Text"
          IMEMode="active | inactive"
          IsRelationship="TRUE" | "FALSE"
          JoinColName="Text"
          JoinRowOrdinal="Integer"
          JoinType="INNER" | "LEFT OUTER" | "RIGHT OUTER"
          Presence="TRUE" | "FALSE"
          PrimaryKey="TRUE" | "FALSE"
          RelationshipDeleteBehavior="Restrict | Cascade | None"
          RenderXMLUsingPattern="TRUE" | "FALSE"
          RowOrdinal="Integer"
          Sealed="TRUE" | "FALSE"
          SeperateLine="TRUE" | "FALSE"
          SetAs="Text"
          ShowAddressBookButton="TRUE" | "FALSE"
          SuppressNameDisplay="TRUE" | "FALSE"
          UniqueId="Text"
          Width 
           * Some of the following are supported in CSOM but may not be strictly allowed in schema XML
          FieldReferences
          FieldRenderingControl
          JSLink 
          NoCrawl 
          RelatedField 
          XPath
          */
          sb.Append(">");
          // MappedChoices takes precendent over choices
          if (this.MappedChoices != null && this.MappedChoices.Count() > 0 && FieldUtility.IsChoiceFieldType(this.Type)) {
            sb.Append("<CHOICES>");
            foreach (string choice in this.MappedChoices.Values) {
              sb.AppendFormat("<CHOICE>{0}</CHOICE>", choice);
            }
            sb.Append("</CHOICES>");
            sb.Append("<MAPPINGS>");
            foreach (string key in this.MappedChoices.Keys) {
              sb.AppendFormat("<MAPPING Value=\"{0}\">{1}</MAPPING>", key, this.MappedChoices[key]);
            }
            sb.Append("</MAPPINGS>");
          } else {
            // ignore choices in cases where the fieldtype isn't compatible or there aren't any
            if (this.Choices != null && this.Choices.Count() > 0 && FieldUtility.IsChoiceFieldType(this.Type)) {
              sb.Append("<CHOICES>");
              foreach (string choice in Choices) {
                sb.AppendFormat("<CHOICE>{0}</CHOICE>", choice);
              }
              sb.Append("</CHOICES>");
            }
          }
          // TODO what if the defaultValue is not contained in choices???
          if (!string.IsNullOrEmpty(Formula))
            sb.AppendFormat("<Formula>{0}</Formula>", this.Formula);
#if !DOTNET_V35
          if (this.DefaultValue != null && !string.IsNullOrWhiteSpace(this.DefaultValue.ToString()))
#else
          // TODO an extension method for 3.5 support of IsNullOrWhiteSpace would be a nice thing to have
          if (this.DefaultValue != null && !StringTools.IsNullOrWhiteSpace(this.DefaultValue.ToString()))
#endif
            sb.AppendFormat("<Default>{0}</Default>", this.DefaultValue);
          if (!string.IsNullOrEmpty(DefaultFormula))
            sb.AppendFormat("<DefaultFormula>{0}</DefaultFormula>", this.DefaultFormula);
          // ex: >=OR([ExpirationDate]&gt;TODAY(), [ExpirationDate]=0)
          if (!string.IsNullOrEmpty(this.ValidationFormula) && !string.IsNullOrEmpty(this.ValidationMessage)) {
            string scriptAttrib = string.Empty;
            if (!string.IsNullOrEmpty(this.ValidationEcmaScript))
              scriptAttrib = string.Format(" Script=\"{0}\"", this.ValidationEcmaScript);
            // TODO HtmlFormat the value of the formula
            sb.AppendFormat("<Validation Message=\"{0}\" {1}>{2}</Validation>", this.ValidationMessage, scriptAttrib, this.ValidationFormula);
          }
          if (this.FieldRefs != null && this.FieldRefs.Count > 0) {
            sb.Append("<FieldRefs>");
            foreach (Field f in this.FieldRefs) {
              // TODO sb.Append(Caml.CAML.FieldRef());
              sb.AppendFormat("<FieldRefs Name=\"{0}\" ID=\"{1}\" />", f.InternalName, f.Id);
            }
            sb.Append("</FieldRefs>");
          }
          // TODO to properly support calculated fields, we need FormulaDisplayNames and FieldRefs
          // TODO DisplayPattern and DisplayBidiPattern are needed for computed fields
          sb.Append("</Field>");
          schemaXml = sb.ToString();
          break;
        case FieldSchemaGenerationMethod.Simple:
          schemaXml = string.Format("<Field DisplayName='{0}' Name='{1}' Group='{2}' Type='{3}' />", this.DisplayName, this.InternalName, this.Group, this.Type);
          break;
        default:
          throw new NotImplementedException(string.Format("FieldSchemaGenerationMethod.{0} is not yet implemented.", method.ToString()));
      }
      return schemaXml;
    }

    /// <summary>
    /// Copies settings to an existing FieldProperties object
    /// without overwriting any existing data in that class
    /// that isn't set explicitly in the source object.
    /// </summary>
    public void CopyTo(FieldProperties target, bool allowSetDangerousProperties = false, bool forceEmptyProperties = false) {
      // changing these fields can be DANGEROUS!
      if (allowSetDangerousProperties && (forceEmptyProperties || this.Id.HasValue))
        target.Id = this.Id;
      if (allowSetDangerousProperties && (forceEmptyProperties || !string.IsNullOrEmpty(this.InternalName)))
        target.InternalName = this.InternalName;
      if (allowSetDangerousProperties && (forceEmptyProperties || !string.IsNullOrEmpty(this.StaticName)))
        target.StaticName = this.StaticName;
      if (allowSetDangerousProperties && (forceEmptyProperties || !string.IsNullOrEmpty(this.Type)))
        target.Type = this.Type;

      if (forceEmptyProperties || !string.IsNullOrEmpty(this.DisplayName))
        target.DisplayName = this.DisplayName;
      if (forceEmptyProperties || !string.IsNullOrEmpty(this.Group))
        target.Group = this.Group;
      if (forceEmptyProperties || !string.IsNullOrEmpty(this.Description))
        target.Description = StringBuilderXmlExtensions.ReplaceEraseToken(this.Description);
      if (forceEmptyProperties || !string.IsNullOrEmpty(this.AuthoringInfo))
        target.AuthoringInfo = StringBuilderXmlExtensions.ReplaceEraseToken(this.AuthoringInfo);
      if (forceEmptyProperties || !string.IsNullOrEmpty(this.SourceID))
        target.SourceID = StringBuilderXmlExtensions.ReplaceEraseToken(this.SourceID);
      // TODO for computed fields
      //fieldProps.DisplayNameSrcField = this.DisplayNameSrcField
      if (forceEmptyProperties || !string.IsNullOrEmpty(this.Title))
        target.Title = StringBuilderXmlExtensions.ReplaceEraseToken(this.Title);

      if (forceEmptyProperties || this.AllowDeletion.HasValue)
        target.AllowDeletion = this.AllowDeletion;
      if (forceEmptyProperties || this.Required.HasValue)
        target.Required = this.Required;
      if (forceEmptyProperties || this.ReadOnly.HasValue)
        target.ReadOnly = this.ReadOnly;
      if (forceEmptyProperties || this.ReadOnlyEnforced.HasValue)
        target.ReadOnlyEnforced = this.ReadOnlyEnforced;
      if (forceEmptyProperties || this.Hidden.HasValue)
        target.Hidden = this.Hidden;
      if (forceEmptyProperties || this.CanToggleHidden.HasValue)
        target.CanToggleHidden = this.CanToggleHidden;
      if (forceEmptyProperties || this.Indexed.HasValue)
        target.Indexed = this.Indexed;
      if (forceEmptyProperties || this.AllowDuplicateValues.HasValue)
        target.AllowDuplicateValues = this.AllowDuplicateValues;

      // user props
      if (forceEmptyProperties || this.UserSelectionScope.HasValue)
        target.UserSelectionScope = this.UserSelectionScope;
      if (forceEmptyProperties || !this.UserSelectionMode.HasValue)
        target.UserSelectionMode = this.UserSelectionMode;

      // TODO determine the effect of setting these in code
      if (forceEmptyProperties || this.Overwrite.HasValue)
        target.Overwrite = this.Overwrite;
      if (forceEmptyProperties || this.OverwriteInChildScopes.HasValue)
        target.OverwriteInChildScopes = this.OverwriteInChildScopes;

      if (forceEmptyProperties || this.Viewable.HasValue)
        target.Viewable = this.Viewable;
      if (forceEmptyProperties || this.Sortable.HasValue)
        target.Sortable = this.Sortable;
      if (forceEmptyProperties || this.Filterable.HasValue)
        target.Filterable = this.Filterable;
      if (forceEmptyProperties || this.FilterableNoRecurrence.HasValue)
        target.FilterableNoRecurrence = this.FilterableNoRecurrence;
      if (forceEmptyProperties || this.DefaultValue != null) {
        if (this.DefaultValue.GetType() == typeof(string)) {
          target.DefaultValue = StringBuilderXmlExtensions.ReplaceEraseToken((string)this.DefaultValue);
        } else {
          target.DefaultValue = this.DefaultValue;
        }
      }
      if (forceEmptyProperties || !string.IsNullOrEmpty(this.DefaultFormula))
        target.DefaultFormula = StringBuilderXmlExtensions.ReplaceEraseToken(this.DefaultFormula);

      if (forceEmptyProperties || this.ShowInDisplayForm.HasValue)
        target.ShowInDisplayForm = this.ShowInDisplayForm;
      if (forceEmptyProperties || this.ShowInEditForm.HasValue)
        target.ShowInEditForm = this.ShowInEditForm;
      if (forceEmptyProperties || this.ShowInListSettings.HasValue)
        target.ShowInListSettings = this.ShowInListSettings;
      if (forceEmptyProperties || this.ShowInNewForm.HasValue)
        target.ShowInNewForm = this.ShowInNewForm;
      if (forceEmptyProperties || this.ShowInVersionHistory.HasValue)
        target.ShowInVersionHistory = this.ShowInVersionHistory;
      if (forceEmptyProperties || this.ShowInViewForms.HasValue)
        target.ShowInViewForms = this.ShowInViewForms;
      if (forceEmptyProperties || this.ShowInFileDlg.HasValue)
        target.ShowInFileDlg = this.ShowInFileDlg;

      if (forceEmptyProperties || this.LinkToItem.HasValue)
        target.LinkToItem = this.LinkToItem;
      if (forceEmptyProperties || this.LinkToItemAllowed.HasValue)
        target.LinkToItemAllowed = this.LinkToItemAllowed;
      if (forceEmptyProperties || this.ListItemMenu.HasValue)
        target.ListItemMenu = this.ListItemMenu;
      if (forceEmptyProperties || this.ListItemMenuAllowed.HasValue)
        target.ListItemMenuAllowed = this.ListItemMenuAllowed;
      if (forceEmptyProperties || this.CalloutMenu.HasValue)
        target.CalloutMenu = this.CalloutMenu;
      if (forceEmptyProperties || this.CalloutMenuAllowed.HasValue)
        target.CalloutMenuAllowed = this.CalloutMenuAllowed;

      if (forceEmptyProperties || !string.IsNullOrEmpty(this.ValidationFormula))
        target.ValidationFormula = StringBuilderXmlExtensions.ReplaceEraseToken(this.ValidationFormula);
      if (forceEmptyProperties || !string.IsNullOrEmpty(this.ValidationMessage))
        target.ValidationMessage = StringBuilderXmlExtensions.ReplaceEraseToken(this.ValidationMessage);
      if (forceEmptyProperties || !string.IsNullOrEmpty(this.ValidationEcmaScript))
        target.ValidationEcmaScript = StringBuilderXmlExtensions.ReplaceEraseToken(this.ValidationEcmaScript);

      // the following fields apply only to certain types
      // strings
      if (forceEmptyProperties || this.MaxLength.HasValue)
        target.MaxLength = this.MaxLength;
      if (forceEmptyProperties || this.AppendOnly.HasValue)
        target.AppendOnly = this.AppendOnly;
      if (forceEmptyProperties || this.UnlimitedLengthInDocumentLibrary.HasValue)
        target.UnlimitedLengthInDocumentLibrary = this.UnlimitedLengthInDocumentLibrary;
      if (forceEmptyProperties || this.Dir.HasValue)
        target.Dir = this.Dir;
      if (forceEmptyProperties || !string.IsNullOrEmpty(this.ForcedDisplay))
        target.ForcedDisplay = StringBuilderXmlExtensions.ReplaceEraseToken(this.ForcedDisplay);
      if (forceEmptyProperties || this.HTMLEncode.HasValue)
        target.HTMLEncode = this.HTMLEncode;
      if (forceEmptyProperties || this.URLEncode.HasValue)
        target.URLEncode = this.URLEncode;
      if (forceEmptyProperties || this.URLEncodeAsURL.HasValue)
        target.URLEncodeAsURL = this.URLEncodeAsURL;
      if (forceEmptyProperties || this.WikiLinking.HasValue)
        target.WikiLinking = this.WikiLinking;
      if (forceEmptyProperties || this.StripWS.HasValue)
        target.StripWS = this.StripWS;
      if (forceEmptyProperties || this.TextOnly.HasValue)
        target.TextOnly = this.TextOnly;
      // note
      if (forceEmptyProperties || this.RichText.HasValue)
        target.RichText = this.RichText;
      if (forceEmptyProperties || this.RichTextMode.HasValue)
        target.RichTextMode = this.RichTextMode;
      if (forceEmptyProperties || this.RestrictedMode.HasValue)
        target.RestrictedMode = this.RestrictedMode;
      if (forceEmptyProperties || this.DisplaySize.HasValue)
        target.DisplaySize = this.DisplaySize;
      if (forceEmptyProperties || this.NumLines.HasValue)
        target.NumLines = this.NumLines;
      if (forceEmptyProperties || this.AllowHyperlink.HasValue)
        target.AllowHyperlink = this.AllowHyperlink;
      if (forceEmptyProperties || this.IsolateStyles.HasValue)
        target.IsolateStyles = this.IsolateStyles;
      // numbers
      if (forceEmptyProperties || this.Max.HasValue)
        target.Max = this.Max;
      if (forceEmptyProperties || this.Min.HasValue)
        target.Min = this.Min;
      if (forceEmptyProperties || this.Percentage.HasValue)
        target.Percentage = this.Percentage;
      if (forceEmptyProperties || this.Decimals.HasValue)
        target.Decimals = this.Decimals;
      if (forceEmptyProperties || this.Commas.HasValue)
        target.Commas = this.Commas;
      if (forceEmptyProperties || this.Div.HasValue)
        target.Div = this.Div;
      if (forceEmptyProperties || this.NegativeFormat.HasValue)
        target.NegativeFormat = this.NegativeFormat;
      // dates
      if (forceEmptyProperties || this.CalType.HasValue)
        target.CalType = this.CalType;
      if (forceEmptyProperties || this.IsStorageTZEnabled.HasValue)
        target.IsStorageTZEnabled = this.IsStorageTZEnabled;
#if !DOTNET_V35
      if (forceEmptyProperties || this.FriendlyDisplayFormat.HasValue)
        target.FriendlyDisplayFormat = this.FriendlyDisplayFormat;
#endif
      // lookups
      if (forceEmptyProperties || this.ListId.HasValue)
        target.ListId = this.ListId;
      if (forceEmptyProperties || this.FieldRef.HasValue)
        target.FieldRef = this.FieldRef;
      if (forceEmptyProperties || this.Mult.HasValue)
        target.Mult = this.Mult;
      if (forceEmptyProperties || this.PrependId.HasValue)
        target.PrependId = this.PrependId;
      if (forceEmptyProperties || !string.IsNullOrEmpty(this.ShowField))
        target.ShowField = StringBuilderXmlExtensions.ReplaceEraseToken(this.ShowField);

      // multi-type
      if (forceEmptyProperties || !string.IsNullOrEmpty(this.Format))
        target.Format = StringBuilderXmlExtensions.ReplaceEraseToken(this.Format);
      if (forceEmptyProperties || this.NoEditFormBreak.HasValue)
        target.NoEditFormBreak = this.NoEditFormBreak;
      // calculated fields
      if (forceEmptyProperties || !string.IsNullOrEmpty(this.ResultType))
        target.ResultType = StringBuilderXmlExtensions.ReplaceEraseToken(this.ResultType);
      if (forceEmptyProperties || !string.IsNullOrEmpty(this.Formula))
        target.Formula = StringBuilderXmlExtensions.ReplaceEraseToken(this.Formula);
      // choice
      if (forceEmptyProperties || this.FillInChoice.HasValue)
        target.FillInChoice = this.FillInChoice;
      if (forceEmptyProperties || this.FillInChoice != null)
        target.Choices = this.Choices;
      if (forceEmptyProperties || this.MappedChoices != null)
        target.MappedChoices = this.MappedChoices;
      // managed metadata; currency; and maybe numbers
      if (forceEmptyProperties || this.RegionId.HasValue)
        target.RegionId = this.RegionId; // technically only intended for currency formats
      // managed metadata
      if (forceEmptyProperties || !string.IsNullOrEmpty(this.TermSetName))
        target.TermSetName = StringBuilderXmlExtensions.ReplaceEraseToken(this.TermSetName);
    }

    public static string Serialize(FieldProperties props) {
      XmlSerializer serializer = new XmlSerializer(typeof(FieldProperties));
      System.IO.TextWriter writer = new System.IO.StringWriter();
      serializer.Serialize(writer, props);
      string schemaXml = writer.ToString();
      return schemaXml;
    }
    public static FieldProperties Deserialize(string xmlSchema) {
      XmlSerializer serializer = new XmlSerializer(typeof(FieldProperties));
      System.IO.TextReader reader = new System.IO.StringReader(xmlSchema);
      FieldProperties props = serializer.Deserialize(reader) as FieldProperties;
      return props;
    }

    #endregion

    #region IXmlSerializable members

    public void WriteXml(XmlWriter writer) {
      //if (SomeInt != null) { writer.WriteValue(writer); }
      string schemaXml = GenerateSchemaXml();
      XmlDocument document = new XmlDocument();
      using (XmlTextReader reader = new XmlTextReader(new StringReader(schemaXml))) {
#if !DOTNET_V35
        reader.DtdProcessing = DtdProcessing.Prohibit; //reader.ProhibitDtd = true;
#endif
        document.Load(reader);
      }
      // TODO does this make trouble by writing the outer tag??
      writer.WriteString(document.OuterXml);
    }

    public void ReadXml(XmlReader reader2) {
      // to heck with the reader, we don't need it!
      XmlDocument document = new XmlDocument();
      XmlTextReader reader = (XmlTextReader)reader2;
      //using (XmlTextReader reader = (XmlTextReader)reader2) {
#if !DOTNET_V35
      reader.DtdProcessing = DtdProcessing.Prohibit; //reader.ProhibitDtd = true;
#endif
      document.Load(reader);
      //}
      XmlElement docElement = document.DocumentElement; // Field

      // Read all available attributes before entering the outer element
      this.Id = docElement.ReadAttribute(this.Id, "ID");
      this.InternalName = docElement.ReadAttribute("Name"); // mapped
      this.StaticName = docElement.ReadAttribute("StaticName");
      this.Type = docElement.ReadAttribute("Type");
      this.DisplayName = docElement.ReadAttribute("DisplayName");
      this.Group = docElement.ReadAttribute("Group");
      this.Description = docElement.ReadAttribute("Description");
      this.AuthoringInfo = docElement.ReadAttribute("AuthoringInfo");
      this.SourceID = docElement.ReadAttribute("SourceID");
      //this.DisplayNameSrcField
      this.Title = docElement.ReadAttribute("Title");
      this.AllowDeletion = docElement.ReadAttribute(this.AllowDeletion, "AllowDeletion");
      this.AllowDuplicateValues = docElement.ReadAttribute(this.AllowDuplicateValues, "AllowDuplicateValues");
      this.AppendOnly = docElement.ReadAttribute(this.AppendOnly, "AppendOnly");
      this.Hidden = docElement.ReadAttribute(this.Hidden, "Hidden");
      this.CanToggleHidden = docElement.ReadAttribute(this.CanToggleHidden, "CanToggleHidden");
      this.CalloutMenu = docElement.ReadAttribute(this.CalloutMenu, "CalloutMenu");
      this.CalloutMenuAllowed = docElement.ReadAttribute(this.CalloutMenuAllowed, "CalloutMenuAllowed");
      this.Dir = docElement.ReadAttribute(this.Dir, "Dir");
      this.ForcedDisplay = docElement.ReadAttribute("ForcedDisplay");
      this.HTMLEncode = docElement.ReadAttribute(this.HTMLEncode, "HTMLEncode");
      this.Indexed = docElement.ReadAttribute(this.Indexed, "Indexed");
      this.Overwrite = docElement.ReadAttribute(this.Overwrite, "Overwrite");
      this.OverwriteInChildScopes = docElement.ReadAttribute(this.OverwriteInChildScopes, "OverwriteInChildScopes");
      this.Sortable = docElement.ReadAttribute(this.Sortable, "Sortable");
      this.Filterable = docElement.ReadAttribute(this.Filterable, "Filterable");
      this.FilterableNoRecurrence = docElement.ReadAttribute(this.FilterableNoRecurrence, "FilterableNoRecurrence");
      this.Required = docElement.ReadAttribute(this.Required, "Required");
      this.ReadOnly = docElement.ReadAttribute(this.ReadOnly, "ReadOnly");
      this.ReadOnlyEnforced = docElement.ReadAttribute(this.ReadOnlyEnforced, "ReadOnlyEnforced");
      this.ShowInDisplayForm = docElement.ReadAttribute(this.ShowInDisplayForm, "ShowInDisplayForm");
      this.ShowInEditForm = docElement.ReadAttribute(this.ShowInEditForm, "ShowInEditForm");
      this.ShowInListSettings = docElement.ReadAttribute(this.ShowInListSettings, "ShowInListSettings");
      this.ShowInNewForm = docElement.ReadAttribute(this.ShowInNewForm, "ShowInNewForm");
      this.ShowInVersionHistory = docElement.ReadAttribute(this.ShowInVersionHistory, "ShowInVersionHistory");
      this.ShowInViewForms = docElement.ReadAttribute(this.ShowInViewForms, "ShowInViewForms");
      this.ShowInFileDlg = docElement.ReadAttribute(this.ShowInFileDlg, "ShowInFileDlg");
      this.LinkToItem = docElement.ReadAttribute(this.LinkToItem, "LinkToItem");
      this.LinkToItemAllowed = docElement.ReadAttribute(this.LinkToItemAllowed, "LinkToItemAllowed");
      this.ListItemMenu = docElement.ReadAttribute(this.ListItemMenu, "ListItemMenu");
      this.ListItemMenuAllowed = docElement.ReadAttribute(this.ListItemMenuAllowed, "ListItemMenuAllowed");
      this.MaxLength = docElement.ReadAttribute(this.MaxLength, "MaxLength");
      this.RichText = docElement.ReadAttribute(this.RichText, "RichText");
      this.RichTextMode = docElement.ReadAttribute(this.RichTextMode, "RichTextMode");
      this.RestrictedMode = docElement.ReadAttribute(this.RestrictedMode, "RestrictedMode");
      this.StripWS = docElement.ReadAttribute(this.StripWS, "StripWS");
      this.TextOnly = docElement.ReadAttribute(this.TextOnly, "TextOnly");
      this.UnlimitedLengthInDocumentLibrary = docElement.ReadAttribute(this.UnlimitedLengthInDocumentLibrary, "UnlimitedLengthInDocumentLibrary");
      this.URLEncode = docElement.ReadAttribute(this.URLEncode, "URLEncode");
      this.URLEncodeAsURL = docElement.ReadAttribute(this.URLEncodeAsURL, "URLEncodeAsURL");
      this.UserSelectionMode = docElement.ReadAttribute(this.UserSelectionMode, "UserSelectionMode");
      this.UserSelectionScope = docElement.ReadAttribute(this.UserSelectionScope, "UserSelectionScope");
      this.Viewable = docElement.ReadAttribute(this.Viewable, "Viewable");
      this.WikiLinking = docElement.ReadAttribute(this.WikiLinking, "WikiLinking");
      this.DisplaySize = docElement.ReadAttribute(this.DisplaySize, "DisplaySize");
      this.NumLines = docElement.ReadAttribute(this.NumLines, "NumLines");
      this.AllowHyperlink = docElement.ReadAttribute(this.AllowHyperlink, "AllowHyperlink");
      this.IsolateStyles = docElement.ReadAttribute(this.IsolateStyles, "IsolateStyles");
      this.Max = docElement.ReadAttribute(this.Max, "Max");
      this.Min = docElement.ReadAttribute(this.Min, "Min");
      this.Percentage = docElement.ReadAttribute(this.Percentage, "Percentage");
      this.Decimals = docElement.ReadAttribute(this.Decimals, "Decimals");
      this.Commas = docElement.ReadAttribute(this.Commas, "Commas");
      this.Div = docElement.ReadAttribute(this.Div, "Div");
      this.NegativeFormat = docElement.ReadAttribute(this.NegativeFormat, "NegativeFormat");
      this.CalType = docElement.ReadAttribute(this.CalType, "CalType");
      string storageTZ = docElement.ReadAttribute("StorageTZ");
      if (!string.IsNullOrEmpty(storageTZ))
        this.IsStorageTZEnabled = (storageTZ == "UTC");
#if !DOTNET_V35
      this.FriendlyDisplayFormat = docElement.ReadAttribute(this.FriendlyDisplayFormat, "FriendlyDisplayFormat");
#endif
      this.ListRaw = docElement.ReadAttribute("List");
      // The above also causes ListId to work correctly, assuming List is a GUID
      this.FieldRef = docElement.ReadAttribute(this.FieldRef, "FieldRef");
      this.Mult = docElement.ReadAttribute(this.Mult, "Mult");
      this.PrependId = docElement.ReadAttribute(this.PrependId, "PrependId");
      this.PrependId = docElement.ReadAttribute(this.PrependId, "PrependId");
      this.NoEditFormBreak = docElement.ReadAttribute(this.NoEditFormBreak, "NoEditFormBreak");
      this.FillInChoice = docElement.ReadAttribute(this.FillInChoice, "FillInChoice");
      this.RegionId = docElement.ReadAttribute(this.RegionId, "LCID"); // mapped
      this.ShowField = docElement.ReadAttribute("ShowField");
      this.Format = docElement.ReadAttribute("Format");
      this.ResultType = docElement.ReadAttribute("ResultType");
      // okay all done attributes, now elements must be read
      foreach (XmlNode node in docElement.ChildNodes) {
        switch (node.Name) {
          case "Formula":
            // TODO support formula field display name mappings
            this.Formula = node.InnerText;
            break;
          case "DefaultValue":
            this.DefaultValue = node.InnerText;
            break;
          case "DefaultFormula":
            this.DefaultFormula = node.InnerText;
            break;
          case "Validation":
            this.ValidationFormula = node.InnerText;
            this.ValidationMessage = ((XmlElement)node).ReadAttribute("Message");
            this.ValidationEcmaScript = ((XmlElement)node).ReadAttribute("Script");
            break;
          case "FieldsRefs":
            XmlNodeList fieldRefs = ((XmlElement)node).GetElementsByTagName("FieldRef");
            break;
          case "CHOICES":
            XmlNodeList choices = ((XmlElement)node).GetElementsByTagName("CHOICE");
            List<string> choiceList = new List<string>();
            foreach (XmlNode choice in choices) {
              choiceList.Add(choice.InnerText);
            }
            this.Choices = choiceList.ToArray();
            break;
          case "MAPPINGS":
            XmlNodeList mappings = ((XmlElement)node).GetElementsByTagName("MAPPINGS");
            Dictionary<string, string> mapDict = new Dictionary<string, string>();
            foreach (XmlNode mapping in mappings) {
              //<MAPPING Value=\"{0}\">{1}</MAPPING>", key, this.MappedChoices[key];
              string key = ((XmlElement)mapping).ReadAttribute("Value");
              string value = mapping.InnerText;
              mapDict.Add(key, value);
            }
            this.MappedChoices = mapDict;
            break;
        } // switch
      } // foreach nodes under root

    } // ReadXml

    public XmlSchema GetSchema() {
      return (null);
    }

    #endregion

  }

  /*
  [XmlType()]
  public class MyNullable<T> : Nullable<T> where T : struct, IXmlSerializable {

    private static MultiParser Parser = new MultiParser();

    public static void ReadAttribute<T>(XmlReader reader, Nullable<T> t, string attrName) where T : struct {
      string val = reader.GetAttribute(attrName);
      if (string.IsNullOrEmpty(val))
        return; // don't read/write anything
      t = Parser.Parse<T>(val); // we could do TryParse instead, but here it should be OK to throw the error
    }
    public static void ReadAttribute<T>(XmlReader reader, Nullable<T> t, int index) where T : struct {
      string val = reader.GetAttribute(index);
      if (string.IsNullOrEmpty(val))
        return; // don't read/write anything
      t = Parser.Parse<T>(val); // we could do TryParse instead, but here it should be OK to throw the error
    }

    public MyNullable() { }

    public void ReadXml(XmlReader reader) {
      ReadAttribute(reader, this as Nullable, 0);
      //string value = reader.GetAttribute(0);
      //GetAttribute("Format");
    }
    public XmlSchema GetSchema() {
      return (null);
    }

  }
  */

  public enum SiteTemplateSupportScope {
    None,
    Unknown,
    Web,
    SiteCollection,
    Tenant,
    Full
  }

  public enum FieldSchemaGenerationMethod {
    Auto,
    Simple,
    Complex,
    UltraComplex
  }

  public enum NegativeFormatType {
    MinusSign,
    Parens
  }

  /// <summary>
  /// if a filter can be created on the field in a view that does not expand recurring events. If Filterable contains TRUE, the field can be filtered in all views regardless of how FilterableNoRecurrence is set.
  /// </summary>
  public enum FilterableTypes {
    NotFilterable,
    Filterable,
    FilterableNoRecurrence,
  }

  public enum RtlDirType {
    LTR,
    RTL
  }

  public enum EncodingType {
    None,
    HTMLEncode,
    URLEncode,
    URLEncodeAsURL
  }

  /*
   * was not needed because Microsoft.SharePoint.Client.CalendarType exists
  public enum CalendarType {
    NoneSpecified = 0,
    Gregorian = 1,
    JapaneseEmperorEra = 3,
    TaiwanEra = 4,
    KoreanTangunEra = 5,
    HijriArabicLLunar = 6,
    Thai = 7,
    HebrewLunar = 8,
    GregorianMiddleEastFrench = 9,
    GregorianArabic = 10,
    GregorianTransliteratedEnglish = 11,
    GregorianTransliteratedFrench = 12,
    KoreanJapaneseLunar = 14,
    ChineseLunar = 15,
    SakaEra = 16
  }
  */

  public enum ListItemMenuState {
    /// <summary>
    /// The menu or link can be optionally shown.
    /// </summary>
    Allowed = 0,

    /// <summary>
    /// The menu or link must be shown.
    /// </summary>
    Required = 1,

    /// <summary>
    /// The menu or link cannot be shown.
    /// </summary>
    Prohibited = 2
  }

  /// <summary>
  /// Copied from Microsoft.SharePoint.SPRichTextMode
  /// </summary>
  public enum RichTextMode {

    /// <summary>
    /// Display plain text, or display rich text with bold, italic, or text alignment. Value = 0. 
    /// </summary>
    Compatible = 0,

    /// <summary>
    /// Display enhanced rich text, including pictures, tables, and hyperlinks.
    /// </summary>
    FullHtml = 1,

    /// <summary>
    /// Display HTML as XML. This value is not supported by multiline text fields.
    /// </summary>
    HtmlAsXml = 2,

    /// <summary>
    /// Displays HTML with inline style specifications. This value is not supported by multiline text fields.
    /// </summary>
    ThemeHtml = 3
  }

}
