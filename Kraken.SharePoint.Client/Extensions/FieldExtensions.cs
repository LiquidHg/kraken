namespace Microsoft.SharePoint.Client {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  /* Older versions of CSOM did not include this API */
#if !DOTNET_V35
  using Microsoft.SharePoint.Client.Taxonomy;
#endif
  using Microsoft.SharePoint.Client.Utilities;

  using Kraken.SharePoint.Client;
  using Kraken.SharePoint.Client.Connections;
  using Kraken.SharePoint.Client.Helpers;
  using Kraken.Tracing;
  using System.Text.RegularExpressions;

  public static class KrakenFieldExtensions {

    public static void Update(this Field existingField, FieldProperties properties, bool execute = true, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)existingField.Context;
      if (properties.IsLookup) {
        LookupFieldProvisioner lookupFieldProvisioner = new LookupFieldProvisioner(context, trace);
        lookupFieldProvisioner.UpdateField(existingField, properties);
        return;
      }

      // TODO is this the best way to get the parent objet??
      // TODO what if this is a list field and not a site column???
      context.Web.EnsureProperty(trace, w => w.Fields);
      FieldCollection fields = context.Web.Fields;
      // convert formula display names to internal names and add fields refs
      fields.CanonicalizeFormula(properties, trace);

      bool pushToLists = (properties.PushChangesToLists.HasValue) ? properties.PushChangesToLists.Value : true;
      string schemaXml = properties.GenerateSchemaXml();
      existingField.Update(schemaXml, pushToLists, execute);
      //((allowMulti) ? FieldTypeExtended.TaxonomyFieldTypeMulti : FieldTypeExtended.TaxonomyFieldType).ToString()
      // if type is TaxonomyFieldTypeMulti then you still have work to do
      if (!string.IsNullOrEmpty(properties.TermSetName)) {
#if !DOTNET_V35
        // above call does an execute query
        // and this one will do an execute query - again
        existingField.ConfigureTaxonomyField(context, properties);
#else
        throw new NotSupportedException("Taxonomy fields are not supported in this version of CSOM.");
#endif
      }
      // TODO report success / fail
    }

    public static void Update(this Field existingField, string schemaXml, bool pushToLists, bool execute = true, ITrace trace = null) {
      //if (trace == null) trace = NullTrace.Default;
      try {
        ClientContext context = (ClientContext)existingField.Context;
        existingField.SchemaXml = schemaXml;
        if (pushToLists)
          existingField.UpdateAndPushChanges(pushToLists);
        else
          existingField.Update();

        if (execute)
          context.ExecuteQuery();
      } catch (Exception ex) {
        if (trace == null)
          throw;
        trace.TraceError(ex);
      }
    }

    // thanks http://sharepoint.stackexchange.com/questions/200380/csom-identify-columns-created-by-user-rather-than-built-in
    public static bool IsUserCreated(this Field field) {
      // TODO that SourceID could interfere with other custom fields if they follow that convention
      bool createdByUser = !field.FromBaseType
      && !field.SchemaXml.Contains(" SourceID=\"http") // remove "Title", "Combine", "RepairDocument" 
      && !field.EntityPropertyName.StartsWith("OData__");
      // field .Sealed == true could be working too but I don't know if it could exclude valid results
      return createdByUser;
    }

    public static object EncodeTextValue(this Field field, object fieldValue, WebContextManager contextManager = null, ITrace trace = null) {
      if (field.FieldTypeKind != FieldType.Text && field.FieldTypeKind == FieldType.Note)
        return fieldValue;

      // TODO this method of type checking only works in nwer versions of CSOM
#if !DOTNET_V35
      FieldText t1 = field.TypedObject as FieldText;
      FieldMultiLineText t = field.TypedObject as FieldMultiLineText;

      if (t1 != null || (t != null && !t.RichText))
#endif
        fieldValue = HttpUtility.HtmlEncode(fieldValue.ToString());
      return fieldValue;
    }

    /* Older versions of CSOM did not include field.TypedObject in API */
#if !DOTNET_V35
    public static object ResolveLookupValue(this Field field, object fieldValue, WebContextManager contextManager = null, ITrace trace = null) {
      FieldLookup fl = field as FieldLookup;
      if (fl == null || fieldValue == null || string.IsNullOrEmpty(fieldValue.ToString()))
        return null; //fieldValue;

      ClientContext context = (ClientContext)field.Context;
      if (fl.LookupWebId != Guid.Empty && fl.LookupWebId != context.Web.Id) {
        if (contextManager == null)
          throw new NotSupportedException("lookup is from another web context but no context manager was specified.");
        WebContextManager cm2 = MultiWebContextManager.Current.TryGetOrCopy(contextManager, fl.LookupWebId);
        context = cm2.Context;
      }

      string lookupListName = fl.LookupList;
      List lookupList = null;
      if (!context.Web.TryGetList(lookupListName, out lookupList)) {
        trace.TraceWarning("Could not find lookup list '{0}' in web '{1}'", lookupListName, context.Web.UrlSafeFor2010());
      } else {
        string lookupField = fl.LookupField;
        string lookupValue = fieldValue.ToString();
        ResolveLookupOptions options = new ResolveLookupOptions() {
          LookupFieldName = lookupField
          // TODO support different type
        };
        FieldLookupValue lookupResult = lookupList.GetLookupValue(lookupValue, options, trace);
        if (lookupResult != null)
          fieldValue = lookupResult;
      }
      return fieldValue;
    }
#endif

    public static bool IsLookupSupported(this Field field) {
      return (field.FieldTypeKind.Equals(FieldType.Counter) ||
          field.FieldTypeKind.Equals(FieldType.Text) ||
          field.FieldTypeKind.Equals(FieldType.Choice) || // Added by Tom and I am pretty fuckin' sure it works OK! 2016-12-14
          field.FieldTypeKind.Equals(FieldType.Number) ||
          field.FieldTypeKind.Equals(FieldType.Integer) || // Why wouldn't it be if Number is?? 2016-12-14
          field.FieldTypeKind.Equals(FieldType.Boolean) || // WTF? Lets try it!
          field.FieldTypeKind.Equals(FieldType.Currency) || // WTF? Lets try it!
          field.FieldTypeKind.Equals(FieldType.DateTime) ||
          (field.FieldTypeKind.Equals(FieldType.Computed) && ((FieldComputed)field).EnableLookup) ||
          (field.FieldTypeKind.Equals(FieldType.Calculated) && ((FieldCalculated)field).OutputType.Equals(FieldType.Text)));
    }

#if !DOTNET_V35
    internal static TaxonomyField ConfigureTaxonomyField(this Field field, ClientContext clientContext, FieldProperties properties) {
      if (string.IsNullOrEmpty(properties.TermSetName))
        throw new ArgumentNullException("properties.TermSetName");
      // Set the store and set id based on information stored in the taxonomy store
      Guid termStoreId = Guid.Empty;
      Guid termSetId = Guid.Empty;
      int region = (properties.RegionId.HasValue) ? properties.RegionId.Value : 1033;
      GetTaxonomyFieldInfo(clientContext, properties.TermSetName, out termStoreId, out termSetId, region);
      // Retrieve as Taxonomy Field
      TaxonomyField taxonomyField = clientContext.CastTo<TaxonomyField>(field);
      taxonomyField.SspId = termStoreId;
      taxonomyField.TermSetId = termSetId;
      taxonomyField.TargetTemplate = String.Empty;
      taxonomyField.AnchorId = Guid.Empty;
      taxonomyField.Update();
      clientContext.ExecuteQuery();
      return taxonomyField;
    }

    // TODO move to my own class
    private static void GetTaxonomyFieldInfo(ClientContext clientContext, string termSetName, out Guid termStoreId, out Guid termSetId, int regionId = 1033) {
      termStoreId = Guid.Empty;
      termSetId = Guid.Empty;
      TaxonomySession session = TaxonomySession.GetTaxonomySession(clientContext);
      // TODO we may need to allow options for using a different term store
      TermStore termStore = session.GetDefaultSiteCollectionTermStore();
      TermSetCollection termSets = termStore.GetTermSetsByName(termSetName, regionId);
      clientContext.Load(termSets, tsc => tsc.Include(ts => ts.Id));
      clientContext.Load(termStore, ts => ts.Id); clientContext.ExecuteQuery();
      termStoreId = termStore.Id;
      termSetId = termSets.FirstOrDefault().Id;
    }
#endif

  }
  public static class KrakenFieldCollectionExtensions {

    /// <summary>
    /// Add a field based on properties. Will detect Lookup 
    /// and Taxonomy fields and react to those appropriately.
    /// </summary>
    /// <param name="fields"></param>
    /// <param name="properties"></param>
    /// <param name="execute"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    public static Field Add(this FieldCollection fields, FieldProperties properties, bool execute = true, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)fields.Context;
      if (properties.IsLookup) {
        LookupFieldProvisioner lookupFieldProvisioner = new LookupFieldProvisioner(context, trace);
        return lookupFieldProvisioner.CreateField(properties);
      }

      // convert formula display names to internal names and add fields refs
      fields.CanonicalizeFormula(properties, trace);

      // string displayName, string name, /* Guid Id, */ string group, string type, string defaultValue = "", IEnumerable<string> choices = null
      // use auto-determination to generate a schema XML
      string schemaXml = properties.GenerateSchemaXml();
      Field field = field = fields.Add(schemaXml, execute);
      //((allowMulti) ? FieldTypeExtended.TaxonomyFieldTypeMulti : FieldTypeExtended.TaxonomyFieldType).ToString()
      // if type is TaxonomyFieldTypeMulti then you still have work to do
      if (!string.IsNullOrEmpty(properties.TermSetName)) {
#if !DOTNET_V35
        // above call does an execute query
        // and this one will do an execute query - again
        field.ConfigureTaxonomyField(context, properties);
#else
        throw new NotSupportedException("Taxonomy fields are not supported in this version of CSOM.");
#endif
      }
      return field;
    }

    public static Field Add(this FieldCollection fields, string schemaXml, bool execute, ITrace trace = null) {
      //if (trace == null) trace = NullTrace.Default;
      try {
        AddFieldOptions options = AddFieldOptions.AddFieldInternalNameHint | AddFieldOptions.DefaultValue;
        ClientContext context = (ClientContext)fields.Context;
        Field newField = fields.AddFieldAsXml(schemaXml, false, options);
        if (execute)
          context.ExecuteQuery();
        return newField;
      } catch (Exception ex) {
        if (trace == null)
          throw;
        trace.TraceError(ex);
        return null;
      }
    }

    /*
public static List<Field> FindAny(this FieldCollection fields, IEnumerable<string> identifier) {
  List<Field> matchedFields = fields.Where(
    f => identifier.Contains(f.Title)
    || identifier.Contains(f.Id.ToString(), StringComparer.InvariantCultureIgnoreCase)
    || identifier.Contains(f.InternalName, StringComparer.InvariantCultureIgnoreCase)
  ).ToList();
  return matchedFields;
}
public static Field EqualsAny(this FieldCollection fields, string identifier) {
  return fields.Where(f =>
      identifier.Equals(f.Title)
      || identifier.Equals(f.Id.ToString(), StringComparison.InvariantCultureIgnoreCase)
      || identifier.Equals(f.InternalName, StringComparison.InvariantCultureIgnoreCase)
    ).FirstOrDefault();
}
*/
    /*
    public static Field ExistsAny(this FieldCollection fields, string[] identifiers) {
      return fields
        
        (f =>
          identifier.Equals(f.Title)
          || identifier.Equals(f.Id.ToString(), StringComparison.InvariantCultureIgnoreCase)
          || identifier.Equals(f.InternalName, StringComparison.InvariantCultureIgnoreCase)
        ).FirstOrDefault();
    }
    */
    /// <summary>
    /// Returns the first field that matches
    /// by Id, Name, or Title. Only Title is
    /// case sensitive.
    /// </summary>
    /// <param name="fields"></param>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public static Field FindAny(this IEnumerable<Field> fields, string identifier) {
      return fields.Where(f =>
          identifier.Equals(f.Title)
          || identifier.Equals(f.Id.ToString(), StringComparison.InvariantCultureIgnoreCase)
          || identifier.Equals(f.InternalName, StringComparison.InvariantCultureIgnoreCase)
        ).FirstOrDefault();
      // TODO throw for > 1 found??
    }

    /// <summary>
    /// Find all fields that match Id, Name, 
    /// or Title. Only Title is case sensitive.
    /// <param name="fields"></param>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public static List<Field> FindAny(this IEnumerable<Field> fields, IEnumerable<string> identifier) {
      List<Field> matchedFields = fields.Where(
        f => identifier.Contains(f.Title)
        || identifier.Contains(f.Id.ToString(), StringComparer.InvariantCultureIgnoreCase)
        || identifier.Contains(f.InternalName, StringComparer.InvariantCultureIgnoreCase)
      ).ToList();
      return matchedFields;
    }

    /// <summary>
    /// Find all field matching a specific Group.
    /// Search is case insensitive.
    /// </summary>
    /// <param name="fields">web or list field collection to query</param>
    /// <param name="groupName">Name of the group</param>
    /// <param name="excludeBuiltInFields">If true, only user-created fields will be returned</param>
    /// <returns></returns>
    public static IEnumerable<Field> FindByGroup(this FieldCollection fields, string groupName, bool excludeBuiltInFields = false) {
      ClientContext context = (ClientContext)fields.Context;
      IEnumerable<Field> result =
        context.LoadQuery(
          ((excludeBuiltInFields)
          ? fields.Where(f =>
            !f.FromBaseType
            && !f.SchemaXml.Contains(" SourceID=\"http")
            && !f.EntityPropertyName.StartsWith("OData__")
            && groupName.Equals(f.Group, StringComparison.InvariantCultureIgnoreCase))
          : fields.Where(f =>
            groupName.Equals(f.Group, StringComparison.InvariantCultureIgnoreCase))
          ).IncludeSiteColumnDefaults());
      context.ExecuteQuery();
      return result;
    }

    public static IEnumerable<Field> GetAllFields(this FieldCollection fields, bool excludeBuiltInFields = false) {
      ClientContext context = (ClientContext)fields.Context;
      IEnumerable<Field> result =
        context.LoadQuery(
          ((excludeBuiltInFields)
          ? fields.Where(f =>
            !f.FromBaseType
            && !f.SchemaXml.Contains(" SourceID=\"http")
            && !f.EntityPropertyName.StartsWith("OData__"))
          : fields
          ).IncludeSiteColumnDefaults());
      context.ExecuteQuery();
      return result;
    }


    /// <summary>
    /// Converts a formula from the format you see when you edit
    /// it in Site Columns to the format that is stored in Xml.
    /// </summary>
    /// <param name="fields">The applicable collection of fields to be used for resolving references</param>
    /// <param name="formula">A formula string</param>
    /// <param name="fieldRefs">Pass in a collections of fields and this method will add field references to it.</param>
    /// <param name="trace"></param>
    /// <returns>The formula string with display values replaced by internal names</returns>
    public static string CanonicalizeFormula(this FieldCollection fields, string formula, List<Field> fieldRefs, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      if (fieldRefs == null)
        throw new ArgumentNullException("fieldRefs");
      if (string.IsNullOrWhiteSpace(formula))
        return formula;
      //if (fieldRefs.Count > 0)
      //  throw new ArgumentException("fieldRefs Collection must be empty", "fieldRefs");
      trace.TraceVerbose("Forumla text began life as: {0}", formula);

      // lead formula with an =
      if (!formula.StartsWith("="))
        formula = "=" + formula;

      // replaces all [] field names with internal names
      string pattern = @"\[(.*?)\]"; // all text in brackets
      MatchCollection matches = Regex.Matches(formula, pattern);
      foreach (Match m in matches) {
        string txtToReplace = m.Groups[0].Value;
        string fieldTitle = m.Groups[1].Value;
        Field f = fields.FindAny(fieldTitle);
        if (f == null)
          throw new Exception(string.Format("Provided formula uses field with title '{0}' that does not exist in the field collection. Formula is: {1}", fieldTitle, formula));
        // gets rid of [] and uses internal name instead
        formula = formula.Replace(txtToReplace, f.InternalName);
        fieldRefs.Add(f); // add to collection to be used later
      }
      // search for and add other field references not in []
      foreach (Field f2 in fields) {
        string fieldName = f2.InternalName;
        string fieldAsWord = string.Format("\\b{0}\\b", fieldName);
        bool isFieldInFormula = Regex.IsMatch(formula, fieldAsWord, RegexOptions.IgnoreCase);
        if (isFieldInFormula && fieldRefs.FindAny(fieldName) == null)
          fieldRefs.Add(f2);
      }
      trace.TraceVerbose("fieldRefs now contains the following fields: ");
      fieldRefs.ForEach(f => trace.TraceVerbose("" + f.InternalName));
      trace.TraceVerbose("Forumla text was converted to: {0}", formula);
      return formula;
    }

    public static void CanonicalizeFormula(this FieldCollection fields, FieldProperties properties, ITrace trace = null) {
      if (properties.DefaultValueOrFormula != null
        && !string.IsNullOrWhiteSpace(properties.DefaultValueOrFormula.ToString())
        && (properties.TypeKind == FieldType.Calculated
        || properties.DefaultValueOrFormula.ToString().StartsWith("="))
        ) {
        properties.DefaultValueOrFormula = fields.CanonicalizeFormula(properties.DefaultValueOrFormula.ToString(), properties.FieldRefs, trace);
      } else {
        if (!string.IsNullOrWhiteSpace(properties.Formula))
          properties.Formula = fields.CanonicalizeFormula(properties.Formula, properties.FieldRefs, trace);
        if (!string.IsNullOrWhiteSpace(properties.DefaultFormula))
          properties.DefaultFormula = fields.CanonicalizeFormula(properties.DefaultFormula, properties.FieldRefs, trace);
      }
    }

    /// <summary>
    /// Returns the supports fields that can be used
    /// as additional fields for a Lookup field.
    /// </summary>
    /// <param name="fields"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    /// <remarks>
    /// We try to be as permissive as possible about the fields to add
    /// allows us to pass in a collection of mixed names, titles, and ids
    /// </remarks>
    public static List<Field> GetLookupSupportedFields(this FieldCollection fields, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      List<Field> supportedFields = new List<Field>();
      foreach (Field field in fields) {
        // apparently hidden fields are never allowed?
        if (field.IsLookupSupported()) {
          field.EnsureProperty(trace, 
            e => e.InternalName,
            e => e.Id,
            e => e.Title);
          supportedFields.Add(field);
        }
      }
      return supportedFields;
    }

  }

}
