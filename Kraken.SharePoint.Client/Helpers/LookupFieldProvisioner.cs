using Kraken.Tracing;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client.Helpers
{
    public class LookupFieldProvisioner
    {
        private readonly ClientContext Context;
        private readonly ITrace Trace;

        public LookupFieldProvisioner(ClientContext context, ITrace trace)
        {
            Context = context;
            Trace = trace;
        }

        public Field CreateField(FieldProperties properties)
        {
            var web = Context.Web;

            var lookupClientContext = GetLookupClientContext(properties);
            var lookupList = GetLookupList(lookupClientContext, properties);
            if (lookupList != null)
            {
                properties.ListId = lookupList.Id;
                if (!Context.Url.TrimEnd("/").EqualsIgnoreCase(lookupClientContext.Url.TrimEnd("/")))
                {
                  lookupClientContext.Web.EnsureProperty(this.Trace, e => e.Id);
                  properties.WebId = lookupClientContext.Web.Id;
                }
            }

            Field newField = AddSiteColumn(properties);

            AddAdditionalFields(newField, lookupClientContext, lookupList, properties);

            return newField;
        }

        public List<FieldProperties> CreateFieldPropertiesList(List<Field> fields)
        {
            var lookupAddFieldDict = new Dictionary<Guid, List<FieldProperties>>();

            var ret = new List<FieldProperties>();
            foreach (var field in fields)
            {
                bool needAdd = true;
                try
                {
                    var fp = FieldProperties.Deserialize(field.SchemaXml);
                    if (fp.Type.Equals("Lookup"))
                    {
                        if (fp.ListId != null)
                        {
                            var list = Context.Web.Lists.GetById(fp.ListId.GetValueOrDefault());
                            list.RootFolder.EnsureProperty(this.Trace, e => e.ServerRelativeUrl);
                            var url = list.RootFolder.ServerRelativeUrl;
                            fp.LookupListUrl = url.TrimStart(Context.Web.ServerRelativeUrl).TrimStart("/");
                            if(fp.FieldRef != null)
                            {
                                lookupAddFieldDict.UpsertElementList(fp.FieldRef.GetValueOrDefault(), fp);
                                needAdd = false;
                            }
                        }
                    }

                    if (needAdd)
                    {
                        ret.Add(fp);
                    }
                }
                catch (Exception ex)
                {
                    //Trace.TraceError(ex);
                }
            }

            foreach(var kvp in lookupAddFieldDict)
            {
                var parent = ret.FirstOrDefault(e => e.Id.Equals(kvp.Key));
                if(parent != null)
                {
                    parent.LookupAdditionalFields = kvp.Value.Select(e => e.DisplayName).ToArray(); 
                }
            }

            return ret;
        }

        private Field AddSiteColumn(FieldProperties properties)
        {
            try
            {
                string schemaXml = properties.GenerateSchemaXml();
                return Context.Web.AddSiteColumn(schemaXml);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex);
                return null;
            }
        }

        private ClientContext GetLookupClientContext(FieldProperties properties)
        {
            var lookupListFullUrl = "";
            try
            {
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
                if (Utils.TryResolveClientContext(u, out ctx, Context.Credentials))
                {
                    return ctx;
                }
                return null;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Unsuccessful attempt to get the client context for web: '{0}'.", lookupListFullUrl);
                Trace.TraceWarning(ex.Message);
                return null;
            }
        }

        private List GetLookupList(ClientContext context, FieldProperties properties)
        {
            if (context == null)
                return null;

            if (string.IsNullOrEmpty(properties.LookupListUrl))
            {
                Trace.TraceWarning("Lookup List Url is null or empty.");
                return null;
            }

						// TODO seems to me this would be useful utility function elsewhere
						var listUrl = properties.LookupListUrl.Substring(properties.LookupListUrl.LastIndexOf("/") + 1);
            context.Web.EnsureProperty(this.Trace, e => e.ServerRelativeUrl);
						listUrl = Utils.CombineUrl(context.Web.ServerRelativeUrl, listUrl);
						
						List list;
						if (context.Web.TryGetList(listUrl, out list)) {
                list.EnsureProperty(this.Trace, e => e.Id, e => e.Title, e => e.ParentWeb, e => e.Fields);
                return list;
						} else {
                Trace.TraceWarning("Lookup List Url: '{0}' not found in web: '{1}'.", listUrl, context.Web.UrlSafeFor2010());
                return null;
						}
        }

        private void AddAdditionalFields(Field newField, ClientContext lookupClientContext, List lookupList, FieldProperties properties)
        {
            if (lookupList == null)
                return;

            List<Field> addFields = GetAdditionalFields(lookupClientContext, lookupList, properties);
            if (addFields != null && addFields.Count > 0)
            {
                newField.EnsureProperty(this.Trace, e => e.Id, e => e.InternalName);
                foreach (Field field in addFields)
                {
                    properties.InternalName = string.Format("{0}{1}", newField.InternalName, field.InternalName);
                    properties.DisplayName = field.Title;
                    properties.ShowField = field.Title;
                    properties.FieldRef = newField.Id;
                    try
                    {
                        Trace.TraceVerbose("Creating additional field {0} from properties.", properties.InternalName);
                        AddSiteColumn(properties);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex);
                    }
                }
            }
        }

        private List<Field> GetAdditionalFields(ClientContext context, List list, FieldProperties properties)
        {
            if (properties.LookupAdditionalFields == null)
                return null;
            var addFieldDisplayNames = properties.LookupAdditionalFields;
            if (addFieldDisplayNames.Length == 0)
                return null;

            var ret = new List<Field>();

            foreach (Field field in list.Fields)
            {
                if (field.Hidden)
                    continue;

                if (field.FieldTypeKind.Equals(FieldType.Counter) ||
                    field.FieldTypeKind.Equals(FieldType.Text) ||
                    field.FieldTypeKind.Equals(FieldType.Number) ||
                    field.FieldTypeKind.Equals(FieldType.DateTime) ||
                    (field.FieldTypeKind.Equals(FieldType.Computed) && ((FieldComputed)field).EnableLookup) ||
                    (field.FieldTypeKind.Equals(FieldType.Calculated) && ((FieldCalculated)field).OutputType.Equals(FieldType.Text)))
                {
                    if (addFieldDisplayNames.Contains(field.Title))
                    {
                        field.EnsureProperty(this.Trace, e => e.InternalName, e => e.Title);
                        ret.Add(field);
                    }
                }
            }

            foreach (var item in addFieldDisplayNames)
            {
                if (!ret.Exists(e => e.Title.Equals(item)))
                  Trace.TraceWarning("Field Ref '{0}' is not exists into Lookup List '{1}' on web: '{2}'.", item, list.Title, list.ParentWeb.UrlSafeFor2010());
            }

            return ret;
        }
    }
}
