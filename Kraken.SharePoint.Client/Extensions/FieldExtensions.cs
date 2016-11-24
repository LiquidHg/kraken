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

  public static class KrakenFieldExtensions {

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

#region FieldCollection extensions

    public static List<Field> GetByGroup(this FieldCollection fields, string groupName) {
      ClientContext context = (ClientContext)fields.Context;
      IEnumerable<Field> result = context.LoadQuery(fields.Where(f =>
        f.Group == groupName
      ).IncludeSiteColumnDefaults());
      context.ExecuteQuery();
      return result.ToList();
    }

    public static Field AddField(this FieldCollection fields, string schemaXml, bool execute = true) {
      AddFieldOptions options = AddFieldOptions.AddFieldInternalNameHint | AddFieldOptions.DefaultValue;
      ClientContext context = (ClientContext)fields.Context;
      Field newField = fields.AddFieldAsXml(schemaXml, false, options);
      if (execute)
        context.ExecuteQuery();
      return newField;
    }

    // TODO doesn't support lookups yet!
    public static Field AddField(this FieldCollection fields, FieldProperties properties, ITrace trace, bool execute = true) {
      ClientContext context = (ClientContext)fields.Context;
      if (properties.type.Equals("Lookup"))
        return AddLookupField((ClientContext)context, properties, trace);
      // string displayName, string name, /* Guid Id, */ string group, string type, string defaultValue = "", IEnumerable<string> choices = null
      // use auto-determination to generate a schema XML
      string schemaXml = properties.GenerateSchemaXml();
      Field field = field = fields.AddField(schemaXml, execute);
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

    private static Field AddLookupField(ClientContext clientContext, FieldProperties properties, ITrace trace) {
      var lookupFieldProvisioner = new LookupFieldProvisioner(clientContext, trace);
      return lookupFieldProvisioner.CreateField(properties);
    }

#if !DOTNET_V35
    private static TaxonomyField ConfigureTaxonomyField(this Field field, ClientContext clientContext, FieldProperties properties) {
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

#endregion

  }

}
