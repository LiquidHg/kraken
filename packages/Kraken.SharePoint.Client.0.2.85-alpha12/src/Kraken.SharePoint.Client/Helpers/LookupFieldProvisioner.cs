using Kraken.Tracing;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client.Helpers {
  public class LookupFieldProvisioner {
    private readonly ClientContext Context;
    private readonly ITrace Trace;

    public LookupFieldProvisioner(ClientContext context, ITrace trace) {
      Context = context;
      Trace = trace;
    }

    public void UpdateField(Field existingField, FieldProperties properties) {
      Web web = Context.Web;
      ClientContext lookupClientContext = GetLookupClientContext(properties);
      List lookupList = GetLookupList(lookupClientContext, properties);
      CanonicalizeLookupListProperties(lookupClientContext, lookupList, properties);
      string xml = properties.GenerateSchemaXml();
      // do not call the properties overload here; that'd create a infinite loop / stack overflow 
      existingField.Update(xml, true, true, this.Trace);
      AddAdditionalFields(existingField, lookupClientContext, lookupList, properties);
    }

    public Field CreateField(FieldProperties properties) {
      Web web = Context.Web;
      ClientContext lookupClientContext = GetLookupClientContext(properties);
      List lookupList = GetLookupList(lookupClientContext, properties);
      CanonicalizeLookupListProperties(lookupClientContext, lookupList, properties);
      string xml = properties.GenerateSchemaXml();
      // do not call the properties overload here; that'd create a infinite loop / stack overflow 
      Field newField = web.Fields.Add(xml, true, this.Trace);
      AddAdditionalFields(newField, lookupClientContext, lookupList, properties);
      return newField;
    }

    private void CanonicalizeLookupListProperties(ClientContext lookupClientContext, List lookupList, FieldProperties properties) {
      if (lookupList != null) {
        properties.ListId = lookupList.Id;
        // we'll do this all the time
        //if (!Context.Url.TrimEnd("/").EqualsIgnoreCase(lookupClientContext.Url.TrimEnd("/"))) {
        lookupClientContext.Web.EnsureProperty(this.Trace, e => e.Id);
        properties.WebId = lookupClientContext.Web.Id;
        //}
        // blank out properties that are bad for secondary fields
        properties.LookupListUrl = string.Empty;
      }
    }

    public IEnumerable<FieldProperties> CreateFieldPropertiesList(IEnumerable<Field> fields) {
      Dictionary<Guid, List<FieldProperties>> lookupAddFieldDict = new Dictionary<Guid, List<FieldProperties>>();
      List<FieldProperties> ret = new List<FieldProperties>();
      foreach (Field field in fields) {
        bool needAdd = true;
        try {
          var fp = FieldProperties.Deserialize(field.SchemaXml);
          if (fp.IsLookupField) {
            if (fp.ListId != null) {
              var list = Context.Web.Lists.GetById(fp.ListId.GetValueOrDefault());
              list.RootFolder.EnsureProperty(this.Trace, e => e.ServerRelativeUrl);
              var url = list.RootFolder.ServerRelativeUrl;
              fp.LookupListUrl = url.TrimStart(Context.Web.ServerRelativeUrl).TrimStart("/");
              if (fp.FieldRef != null) {
                lookupAddFieldDict.UpsertElementList(fp.FieldRef.GetValueOrDefault(), fp);
                needAdd = false;
              }
            }
          }
          if (needAdd) {
            ret.Add(fp);
          }
        } catch (Exception ex) {
          Trace.TraceError(ex);
        }
      }
      foreach (KeyValuePair<Guid, List<FieldProperties>> kvp in lookupAddFieldDict) {
        var parent = ret.FirstOrDefault(e => e.Id.Equals(kvp.Key));
        if (parent != null) {
          parent.LookupAdditionalFields = kvp.Value.Select(e => e.DisplayName).ToArray();
        }
      }
      return ret;
    }

    /*
    private Field AddSiteColumn(FieldProperties properties) {
      try {
        string schemaXml = properties.GenerateSchemaXml();
        return Context.Web.AddSiteColumn(schemaXml);
      } catch (Exception ex) {
        Trace.TraceError(ex);
        return null;
      }
    }
    private void UpdateSiteColumn(Field field, FieldProperties properties) {
      try {
        string schemaXml = properties.GenerateSchemaXml();
        field.SchemaXml = schemaXml;
      } catch (Exception ex) {
        Trace.TraceError(ex);
      }
    }
    */

    private ClientContext GetLookupClientContext(FieldProperties properties) {
      var lookupListFullUrl = "";
      try {
        if (string.IsNullOrEmpty(properties.LookupListUrl)
          && properties.ListId.HasValue) {
          List list = Context.Web.Lists.GetById(properties.ListId.Value);
          list.RootFolder.EnsureProperty(this.Trace, e => e.ServerRelativeUrl);
          properties.LookupListUrl = list.RootFolder.ServerRelativeUrl;
          lookupListFullUrl = list.GetServerRelativeUrl();
        } else {
          lookupListFullUrl = Utils.CombineUrl(Context.Web.UrlSafeFor2010(), properties.LookupListUrl);
        }
        Uri u = new Uri(lookupListFullUrl);
        ClientContext ctx;
        if (Utils.TryResolveClientContext(u, out ctx, Context.Credentials)) {
          return ctx;
        }
        return null;
      } catch (Exception ex) {
        Trace.TraceWarning("Unsuccessful attempt to get the client context for web: '{0}'.", lookupListFullUrl);
        Trace.TraceWarning(ex.Message);
        return null;
      }
    }

    private List GetLookupList(ClientContext context, FieldProperties properties) {
      if (context == null)
        return null;

      if (string.IsNullOrEmpty(properties.LookupListUrl)) {
        Trace.TraceWarning("Lookup List Url is null or empty.");
        return null;
      }

      // TODO seems to me this would be useful utility function elsewhere
      // This is getting only the list name / title and not Lists/name
      var listName = properties.LookupListUrl.Substring(properties.LookupListUrl.LastIndexOf("/") + 1);
      // Preserves Lists in urls as needed
      // This is not needed; underlying function will combine title
      // with ServerRelativeUrl if necessary
      var fullUrl = (properties.LookupListUrl.Contains("Lists")) ? "Lists/" + listName : listName;
      context.Web.EnsureProperty(this.Trace, e => e.ServerRelativeUrl);
      fullUrl = Utils.CombineUrl(context.Web.UrlSafeFor2010(), fullUrl);
      // TODO test how this interacts when Url and Title don't match

      List list;
      if (context.Web.TryGetList(listName, out list)) {
        list.EnsureProperty(this.Trace, e => e.Id, e => e.Title, e => e.ParentWeb);
        // broken into multiple commands for troubleshooting purpises to figure out which property causes 'cannot complete theis action'
        try {
          list.EnsureProperty(this.Trace, e => e.Fields);
        } catch (Exception ex) {
          throw new Exception(string.Format("One or more fields in list {0} are corrupted in a way that prevents us from loading the Fields collection. Fix the list and try this operatioion again.", listName), ex);
        }
        return list;
      } else {
        Trace.TraceWarning("Can't get Lookup List Name: '{0}' Url: '{1}'", listName, fullUrl);
        return null;
      }
    }

    private void AddAdditionalFields(Field primaryLookupField, ClientContext lookupClientContext, List lookupList, FieldProperties properties) {
      Trace.Enter(System.Reflection.MethodBase.GetCurrentMethod());
      if (lookupList == null) {
        Trace.Exit(System.Reflection.MethodBase.GetCurrentMethod(), "lookupList is null.");
        return;
      }

      Web web = ((ClientContext)primaryLookupField.Context).Web;
      List<Field> addFields = GetAdditionalFields(lookupClientContext, lookupList, properties);
      Trace.TraceVerbose("addFields count = {0}", addFields.Count);
      if (addFields != null && addFields.Count > 0) {
        primaryLookupField.EnsureProperty(this.Trace, e => e.Id, e => e.InternalName);
        foreach (Field field in addFields) {
          string newLookupFieldName = string.Format("{0}_x003A_{1}", primaryLookupField.InternalName, field.InternalName);
          string newLookupFieldTitle = string.Format("{0}:{1}", primaryLookupField.Title, field.Title);
          web.Fields.EnsureCriticalProperties();
          if (web.Fields.FindAny(newLookupFieldName) != null) {
            Trace.TraceVerbose("Auxiliary lookup field {0} already exists and will be skipped.", newLookupFieldName);
            continue;
          } else {
            Trace.TraceVerbose("Adding auxiliary lookup field {0}.", newLookupFieldName);
          }
          properties.InternalName = newLookupFieldName;
          properties.DisplayName = newLookupFieldTitle;
          properties.ShowField = field.InternalName; // field.Title;
          // this should maybe be moved deeper into code
          ///properties.WebId = web.Id;
          properties.FieldRef = primaryLookupField.Id;
          try {
            string xml = properties.GenerateSchemaXml();
            Trace.TraceVerbose("Secondary lookup field schema: {0}", xml);
            // do not call the properties overload here; that'd create a infinite loop / stack overflow 
            /* Field newField2 = */ web.Fields.Add(xml, true, this.Trace);
          } catch (Exception ex) {
            Trace.TraceError(string.Format("Error addition additional lookup field {0}", newLookupFieldName), ex);
          }
        }
      }
      Trace.Exit(System.Reflection.MethodBase.GetCurrentMethod());
    }

    /// <summary>
    /// Given a primary lookup field, returns
    /// all the additional fields.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="fields"></param>
    /// <returns></returns>
    public List<Field> GetAdditionalFields(Field field, IEnumerable<Field> fields) {
      if (!field.IsLookupField())
        throw new NotSupportedException("This method is only valid on lookup fields.");
      FieldProperties primaryFProps = field.CreateProperties();
      if (primaryFProps.FieldRef.HasValue)
        throw new NotSupportedException("This method is only valid on primary lookup fields. The provided field is an additional/secondary lookup field.");
      List<Field> results = new List<Field>();
      foreach (Field f in fields) {
        FieldProperties fp = f.CreateProperties();
        if (fp.FieldRef.HasValue && fp.FieldRef.Value == field.Id)
          results.Add(field);
      }
      return results;
    }

    private List<Field> GetAdditionalFields(ClientContext context, List list, FieldProperties properties) {
      if (properties.LookupAdditionalFields == null)
        return null;
      var addFields = properties.LookupAdditionalFields;
      if (addFields.Length == 0)
        return null;

      // Do a pre-check to make sure what the end-user provided
      // actually exists in SharePoint, because it might not!
      List<Field> requestedFields = list.Fields.FindAny(addFields);
      foreach (string fieldNameOrId in addFields) {
        if (requestedFields.FindAny(fieldNameOrId) == null)
          Trace.TraceWarning("FieldRef with internal name, id, or title '{0}' does not exist in Lookup List '{1}' at web: '{2}'.", fieldNameOrId, list.Title, list.ParentWeb.UrlSafeFor2010());
      }

      // This will automatically return only existing fields, so the 
      // above test is only to inform the caller what they did wrong.
      List<Field> supportedFields = list.Fields.GetLookupSupportedFields(Trace);

      // check type support now that we've narrowed it down.
      List<Field> matchedFields = new List<Field>();
      foreach (Field field in requestedFields) {
        // Warn the user they are trying to indirectly show a hidden field
        if (field.Hidden)
          Trace.TraceWarning("Field {0} is hidden, and nornally not suitable for addition to Lookup fields, but ya-kno-watt? F-da-man! We're gonna try it anyhow.", field.InternalName);
        if (supportedFields.FindAny(field.InternalName) == null) // fieldNameOrId
          Trace.TraceWarning("FieldRef with internal name '{0}' is a type '{1}' that is not supported in lookups. web='{2}'", field.InternalName, field.TypeAsString, list.ParentWeb.UrlSafeFor2010()); // fieldNameOrId
        else
          matchedFields.Add(field);
      }
      // TODO is there a more performant way to do this??
      // we tried a few but they turned out to be quite buggy
      return matchedFields;
    }

  }
}
