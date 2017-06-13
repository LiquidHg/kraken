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

  public static class KrakenFieldCollectionExtensions {

    #region Create

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
      if (properties.IsLookupField) {
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

    #endregion

    #region Find/Search

    /// <summary>
    /// Returns the first field that matches
    /// by Id, Name, or Title. Only Title is
    /// case sensitive.
    /// </summary>
    /// <remarks>
    /// This method doesn't do any client calls but uses local Linq only.
    /// </remarks>
    /// <param name="fields"></param>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public static Field MatchFirst(this IEnumerable<Field> fields, string identifier) {
      return fields.Where(f => f.Equals(identifier, true, StringComparison.InvariantCulture)).FirstOrDefault();
    }

    /// <summary>
    /// Returns all fields that match
    /// by Id, Name, or Title. Only Title is
    /// case sensitive.
    /// </summary>
    /// <remarks>
    /// This method doesn't do any client calls but uses local Linq only.
    /// </remarks>
    /// <param name="fields"></param>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public static IEnumerable<Field> MatchAny(this IEnumerable<Field> fields, string identifier) {
      return fields.Where(f => f.Equals(identifier, true, StringComparison.InvariantCulture));
    }

    /// <summary>
    /// Returns all fields that match
    /// by Id, Name, or Title. Only Title is
    /// case sensitive.
    /// </summary>
    /// <remarks>
    /// This method doesn't do any client calls but uses local Linq only.
    /// </remarks>
    /// <param name="fields"></param>
    /// <param name="identifier">A collection of identifiers to match</param>
    /// <returns></returns>
    public static IEnumerable<Field> MatchAny(this IEnumerable<Field> fields, IEnumerable<string> identifier) {
      List<Field> matchedFields = new List<Field>();
      foreach (string id in identifier) {
        var matches = MatchAny(fields, id);
        if (matches != null)
          matchedFields.AddRange(matches);
      }
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
    public static IEnumerable<Field> GetFieldsByGroup(this FieldCollection fields, string groupName, bool excludeBuiltInFields = false) {
      ClientContext context = (ClientContext)fields.Context;
      IEnumerable<Field> result =
        context.LoadQuery(
          // unforunately we can't pass all the query we need to do to CSOM
          // it can't handle contains or startswith on certain properties
          (fields.Where(
            f => !(excludeBuiltInFields && f.FromBaseType)
            && groupName.Equals(f.Group, StringComparison.InvariantCultureIgnoreCase))
          ).IncludeKeyProperties());
      context.ExecuteQuery();
      // this should be a local expression
      if (excludeBuiltInFields)
        result = result.Where(f => !f.IsBuiltIn());
      return result;
    }

    public static IEnumerable<Field> GetAllFields(this FieldCollection fields, bool excludeBuiltInFields = false) {
      ClientContext context = (ClientContext)fields.Context;
      IEnumerable<Field> result =
        context.LoadQuery(
          // unforunately we can't pass all the query we need to do to CSOM
          // it can't handle contains or startswith on certain properties
          fields.Where(f => !(excludeBuiltInFields && f.FromBaseType)).IncludeKeyProperties()
        );
      context.ExecuteQuery();
      // this should be a local expression
      if (excludeBuiltInFields)
        result = result.Where(f => !f.IsBuiltIn());
      return result;
    }

    /// <summary>
    /// Locate a single Field in the collection, using one of several 
    /// methods. This overload does not implement caching but uses some
    /// query optimizations.
    /// </summary>
    /// <param name="fields"></param>
    /// <param name="identifier">The internal name or title of the site column</param>
    /// <param name="findMethod">Specify the method used to find the field</param>
    /// <returns></returns>
    public static Field GetField(this FieldCollection fields, string identifier, FieldFindMethod findMethod = FieldFindMethod.Any) {
      ClientContext context = (ClientContext)fields.Context;
      Field field = null;
      IEnumerable<Field> result = null;
      // interpret flags in logical best-performance order
      // default option is to do the most permissive search possible
      if (findMethod == FieldFindMethod.Any) {
        result = context.LoadQuery(fields.Where(
          f => f.StaticName == identifier || f.Equals(identifier)
        ).IncludeKeyProperties());
        context.ExecuteQuery();
        field = result.FirstOrDefault();
      }
      // not found yet and *just* flags InternalName and DisplayName together
      if (field == null && findMethod == (FieldFindMethod.InternalName | FieldFindMethod.DisplayName)) {
        field = fields.GetByInternalNameOrTitle(identifier);
        field.LoadKeyProperties(context);
      }
      // not found yet and *just* flags InternalName and StaticName together
      if (field == null && findMethod == (FieldFindMethod.InternalName | FieldFindMethod.StaticName)) {
        result = context.LoadQuery(fields.Where(f =>
          f.StaticName == identifier
          || f.InternalName == identifier
        ).IncludeKeyProperties());
        context.ExecuteQuery();
        field = result.FirstOrDefault();
      }
      // not found yet and any flags with StaticName
      if (field == null) {
        switch (findMethod) {
          case FieldFindMethod.StaticName:
            result = context.LoadQuery(fields.Where(f => f.StaticName.Equals(identifier, StringComparison.InvariantCultureIgnoreCase)).IncludeKeyProperties());
            context.ExecuteQuery();
            field = result.FirstOrDefault();
            break;
          case FieldFindMethod.InternalName:
            result = context.LoadQuery(fields.Where(f => f.InternalName.Equals(identifier, StringComparison.InvariantCultureIgnoreCase)).IncludeKeyProperties());
            context.ExecuteQuery();
            field = result.FirstOrDefault();
            break;
          case FieldFindMethod.DisplayName:
            field = fields.GetByTitle(identifier);
            break;
        }
      }
      if (field != null)
        field.LoadKeyProperties(context);
      return field;
    }
    public static Field GetField(this FieldCollection fields, Guid uniqueId) {
      ClientContext context = (ClientContext)fields.Context;
      Field targetField = fields.GetById(uniqueId);
      targetField.LoadKeyProperties(context);
      return targetField;
    }

    #endregion

    #region Property Insurance

    public static void LoadKeyProperties(this IEnumerable<Field> fields, ClientContext ctx = null) {
      foreach (Field field in fields) {
        if (ctx == null)
          ctx = (ClientContext)field.Context;
        field.LoadKeyProperties(ctx, false);
      }
      ctx.ExecuteQueryIfNeeded();
    }

    public static void LoadKeyProperties(this FieldCollection fields, List<Field> fieldRefs = null) {
      // ensure we have properties we'll need
      ClientContext ctx = (ClientContext)fields.Context;
      ctx.LoadQuery(fields.Where(fc => true).IncludeKeyProperties());
      if (fieldRefs != null)
        LoadKeyProperties(fieldRefs, ctx);
      ctx.ExecuteQueryIfNeeded();
    }

    #endregion

    #region Formula Field Utility

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

      fields.LoadKeyProperties(fieldRefs);

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
        Field f = fields.MatchFirst(fieldTitle);
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
        if (isFieldInFormula && fieldRefs.MatchFirst(fieldName) == null)
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

    #endregion

    #region Lookup Field Utilities

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
      // apparently hidden fields are never allowed?
      List<Field> supportedFields = fields.Where(f => f.IsLookupSupported()).ToList();
      foreach (Field field in fields) {
        supportedFields.Add(field);
      }
      supportedFields.LoadKeyProperties();
      return supportedFields;
    }

    #endregion

  }
}
