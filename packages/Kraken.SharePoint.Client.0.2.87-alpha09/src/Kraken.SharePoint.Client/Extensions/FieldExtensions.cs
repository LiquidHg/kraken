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

    /// <summary>
    /// Compare a Field object to a value passed by string
    /// which can be the InternalName, Id (without {} decoration),
    /// or Title (case insensitive unless specified otherwise).
    /// </summary>
    /// <param name="x"></param>
    /// <param name="identifier"></param>
    /// <param name="titleCounts"></param>
    /// <param name="titleCompare"></param>
    /// <returns>True if identifier can be used to represent the Field</returns>
    public static bool Equals(this Field x, string identifier, bool titleCounts = true, StringComparison titleCompare = StringComparison.InvariantCultureIgnoreCase, string idFormat = "D") {
      return ((titleCounts && identifier.Equals(x.Title, titleCompare))
      || identifier.Equals(x.Id.ToString(idFormat), StringComparison.InvariantCultureIgnoreCase)
      || identifier.Equals(x.InternalName, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// Shorthand function to determine if a Field
    /// is a SharePoint built-in field or one created
    /// by a user.
    /// </summary>
    /// <remarks>
    /// With thanks to: http://sharepoint.stackexchange.com/questions/200380/csom-identify-columns-created-by-user-rather-than-built-in
    /// </remarks>
    /// <param name="field"></param>
    /// <returns></returns>
    public static bool IsBuiltIn(this Field field) {
      // TODO that SourceID could interfere with other custom fields if they follow that convention
      return (field.FromBaseType
        || field.SchemaXml.Contains(" SourceID=\"http") // removes "Title", "Combine", "RepairDocument" 
        || field.EntityPropertyName.StartsWith("OData__"));
        // field.Sealed could work too, but I don't know if it could exclude valid results
    }
    /// <summary>
    /// Literally just the opposite of IsBuiltIn()
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    public static bool IsUserCreated(this Field field) {
      return !field.IsBuiltIn();
    }

    public static FieldProperties CreateProperties(this Field field) {
      if (!field.IsLoaded(f => f.SchemaXml)) {
        ClientContext ctx = (ClientContext)field.Context;
        ctx.LoadIfRequired(field, null, false, // no trace, only load what isn't already there
          f => f.SchemaXml);
        ctx.ExecuteQueryIfNeeded();
      }
      return FieldProperties.Deserialize(field.SchemaXml);
    }

    #region Property Insurance

    // TODO is there a way we can make this configurable in case the caller needs more fields??
    public static void LoadKeyProperties(this Field field, ClientContext ctx = null, bool execute = true) {
      if (ctx == null)
        ctx = (ClientContext)field.Context;
      ctx.LoadIfRequired(field, null, false, // no trace, only load what isn't already there
        f => f.InternalName, f => f.Title, f => f.Id, f => f.StaticName,
        f => f.Group, f => f.TypeAsString, f => f.Hidden,
        f => f.Required, f => f.ReadOnlyField, f => f.Sealed,
        f => f.FromBaseType, f => f.EntityPropertyName, f => f.SchemaXml
      );
      if (execute)
        ctx.ExecuteQueryIfNeeded();
    }

    // TODO is there a way we can make this configurable in case the caller needs more fields??
    /// <summary>
    /// Provided to give a single point to manage included columns to be returned in site columns by default
    /// </summary>
    /// <param name="where"></param>
    /// <returns></returns>
    internal static IQueryable<Field> IncludeKeyProperties(this IQueryable<Field> where) {
      return where.Include(
        f => f.InternalName, f => f.Title, f => f.Id, f => f.StaticName,
        f => f.Group, f => f.TypeAsString, f => f.Hidden,
        f => f.Required, f => f.ReadOnlyField, f => f.Sealed,
        f => f.FromBaseType, f => f.EntityPropertyName, f => f.SchemaXml
      );
    }

    #endregion

    public static void Update(this Field existingField, FieldProperties properties, bool execute = true, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)existingField.Context;
      if (properties.IsLookupField) {
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

    public static bool IsLookupField(this Field field) {
      return FieldUtility.IsLookupFieldType(field.TypeAsString);
    }
    public static bool IsChoiceField(this Field field) {
      return FieldUtility.IsChoiceFieldType(field.TypeAsString);
    }
    public static bool IsTaxonomyField(this Field field) {
      return FieldUtility.IsTaxonomyFieldType(field.TypeAsString);
    }

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

}
