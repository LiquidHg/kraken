namespace Microsoft.SharePoint.Client {

  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using System.Text;
  using System.Text.RegularExpressions;

  //using Microsoft.SharePoint.Client.DocumentSet;

  using Kraken.SharePoint.Client.Connections;
  using Kraken.SharePoint.Client;
  using Kraken.Tracing;

  public static class KrakenContentTypeExtensions {

    public static bool IsContentTypeIdValid(string contentTypeId, string parentId, bool throwOnError = true) {
      bool valid = true;
      if (string.IsNullOrEmpty(contentTypeId)) {
        if (throwOnError)
          throw new ArgumentNullException("contentTypeId");
        valid = false;
      } else if (!contentTypeId.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase)) {
        if (throwOnError)
          throw new InvalidOperationException("The provided content type ID must start with '0x'.");
        valid = false;
        /* sometimes parentId will be null and we'll have to skip this check
      } else if (string.IsNullOrEmpty(parentId)) {
        if (throwOnError)
          throw new ArgumentNullException("parentId");
        valid = false;
         */
      } else {
        Regex r = new Regex("[^0-9A-Fa-f]");
        if (r.IsMatch(contentTypeId.Substring(2))) { // ignore the 0x at the start
          if (throwOnError)
            throw new InvalidOperationException("The provided content type ID must use only hex digits 0-9 and A-F.");
          valid = false;
        } else if (!string.IsNullOrEmpty(parentId) && !contentTypeId.StartsWith(parentId, StringComparison.InvariantCultureIgnoreCase)) {
          if (throwOnError)
            throw new InvalidOperationException("The provided content type ID must start with the ID of the parent content type.");
          valid = false;
        } else if (!string.IsNullOrEmpty(parentId)) {
          // this can't be done as written when we don't know parent id
          int diff = contentTypeId.Length - parentId.Length;
          if (diff != 2 && diff != 34) {
            if (throwOnError)
              throw new InvalidOperationException(string.Format("The provided content type ID must be the parent's ID followed by either two hex digits or two zeros followed by a 32 digit hex guid. The difference in this case was {0} characters. ", diff));
            valid = false;
          }
        }
      }
      return valid;
    }

    /// <summary>
    /// Removed multiple site columns (aka FieldLink(s)) from a Content Type.
    /// </summary>
    /// <param name="ct">The target content type in which to remove the field link</param>
    /// <param name="internalNameOrIds">Array of the internal names of the fields to remove</param>
    /// <param name="updateChildTypes">Update child content types as well</param>
    /// <param name="trace">Trace output for screen or logging</param>
    /// <returns>True if the query has been executed, false if no query was run</returns>
    public static bool RemoveFieldLink(this ContentType ct, string[] internalNameOrIds, bool updateChildTypes, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)ct.Context;
      foreach (string internalNameOrId in internalNameOrIds) {
        ct.RemoveFieldLink(internalNameOrId, updateChildTypes, false, trace);
      }
      if (context.HasPendingRequest) {
        trace.Trace(TraceLevel.Verbose, "Calling Update on content type");
        ct.Update(updateChildTypes);
        trace.Trace(TraceLevel.Verbose, "Executing query.");
        context.ExecuteQuery();
        return true;
      }
      return false;
    }

    /// <summary>
    /// Removes a site column reference (aka FieldLink) from a Content Type.
    /// </summary>
    /// <param name="ct">The target content type in which to remove the field link</param>
    /// <param name="internalNameOrId">Internal name of the field</param>
    /// <param name="updateChildTypes">Update child content types as well</param>
    /// <param name="doExecuteQuery">Optional parameter defaults to true; when false will save ExecuteQuery step for later as useful for batch operations</param>
    /// <param name="trace">Trace output for screen or logging</param>
    /// <returns>True if the query has been executed, false if no query was run</returns>
    public static bool RemoveFieldLink(this ContentType ct, string internalNameOrId, bool updateChildTypes, bool doExecuteQuery = true, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)ct.Context;
      trace.Trace(TraceLevel.Verbose, "Removing site column '{0}'...", internalNameOrId);
      //trace.Trace(TraceLevel.Verbose, sc.SchemaXml);
      trace.Trace(TraceLevel.Verbose, "Searching for field links");
      string scName = internalNameOrId;
      Guid scId = new Guid();
#if !DOTNET_V35
      Guid.TryParse(scName, out scId);
#else
      StringTools.GuidTryParse(scName, out scId);
#endif
      IEnumerable<FieldLink> fieldLinks = context.LoadQuery(
        ct.FieldLinks.Where(f => f.Name == scName || f.Id == scId).Include(f => f.Id, f => f.Name)
      );
      /*
      IEnumerable<Field> fields = context.LoadQuery(
        ct.Fields.Where(f => f.InternalName == scName || f.Id == scId).Include(f => f.Id, f => f.InternalName)
      );
       */
      trace.Trace(TraceLevel.Verbose, "Calling server");
      context.ExecuteQueryIfNeeded();
      trace.Trace(TraceLevel.Verbose, "Results returned");
      //trace.Trace(TraceLevel.Verbose, "Found {0} field/s.", fields.Count());
      trace.Trace(TraceLevel.Verbose, "Found {0} field link/s.", fieldLinks.Count());
      if (fieldLinks.Count() > 1) { // || fields.Count() > 1
        trace.Trace(TraceLevel.Warning, "There was an ambiguous result which returned multiple field links by the same name or id.");
        return false;
      } else if (fieldLinks.Count() == 0) {
        return false;
      } else {
        FieldLink foundFieldLink = fieldLinks.FirstOrDefault();
        if (foundFieldLink != null) {
          trace.Trace(TraceLevel.Verbose, "Found field link - deleting");
          foundFieldLink.DeleteObject(); // this should set HasPendingRequest = true
        }
        /*
        Field foundField = fields.FirstOrDefault();
        if (foundField != null) {
          WriteVerbose("Found field - deleting");
          foundField.DeleteObject();
        }
         */
      }
      if (context.HasPendingRequest && doExecuteQuery) {
        trace.Trace(TraceLevel.Verbose, "Calling Update on content type");
        ct.Update(updateChildTypes);
        trace.Trace(TraceLevel.Verbose, "Executing query.");
        context.ExecuteQuery();
        return true;
      }
      return false;
    }

    /// <summary>
    /// Makes changes to Field Link. Caller is still 
    /// responsible to update the content type or other parent.
    /// </summary>
    /// <param name="fl"></param>
    /// <param name="properties"></param>
    /// <param name="updateChildTypes"></param>
    /// <param name="trace"></param>
    public static void Update(this FieldLink fl, FieldLinkProperties properties, bool updateChildTypes, ITrace trace = null) {
      // FieldLinkCollection flc, 
      // TODO support for (properties.Hiro.Value == FieldLinkRequireStatus.Inherit)
      // by default neither of these will be true;
      if (properties.IsHidden.HasValue)
        fl.Hidden = properties.IsHidden.Value;
      if (properties.IsRequired.HasValue)
        fl.Required = properties.IsRequired.Value;
    }

    // This was abandoned because what it looks like SharePoint is
    // actually doing is adding the fieldlink to the default list content type
    /*
    public static bool AddFieldLink(this List list, FieldLinkProperties properties, ITrace trace = null) {
      if (properties == null)
        throw new ArgumentNullException("properties");
      if (properties.Field == null)
        throw new ArgumentNullException("properties.Field");
      if (ct == null)
        throw new ArgumentNullException("ct");
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)lists.Context;
      trace.Trace(TraceLevel.Verbose, "Adding field link to list");

      //FieldLink fl = list.FieldLinks.Add(properties);
    }
     */

    public static FieldLink EnsureFieldLink(this ContentType ct, FieldLinkProperties properties, bool updateChildTypes, ITrace trace = null) {
      ClientContext context = (ClientContext)ct.Context;
      if (trace == null) trace = NullTrace.Default;

      FieldLinkCollection fieldLinks = ct.FieldLinks;
      //context.Load(fieldLinks);
      Guid fieldId = properties.Field.Id;
      IEnumerable<FieldLink> existingFields = context.LoadQuery(fieldLinks.Where(fl => fl.Id == fieldId));
      context.ExecuteQuery();
      FieldLink existingField = existingFields.FirstOrDefault();
      if (existingField != null) {
        trace.Trace(TraceLevel.Verbose, "FieldLink '{0}' in content type '{1}' already exists and will be skipped.", properties.Name, ct.Name);
        return null;
      }
      /*
      foreach (FieldLink fl in ct.FieldLinks) {
        if (fl.Name == properties.Name) {
          trace.Trace(TraceLevel.Verbose, "FieldLink '{0}' in content type '{1}' already exists and will be skipped.", properties.Name, ct.Name);
          return false;
        }
      }
       */
      return ct.AddFieldLink(properties, updateChildTypes, trace);
    }

    public static FieldLink AddFieldLink(this ContentType ct, FieldLinkProperties properties, bool updateChildTypes, ITrace trace = null) {
      if (properties == null)
        throw new ArgumentNullException("properties");
      if (properties.Field == null)
        throw new ArgumentNullException("properties.Field");
      if (ct == null)
        throw new ArgumentNullException("ct");
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)ct.Context;
      trace.Trace(TraceLevel.Verbose, "Adding site column to content type");

      //trace.Trace(TraceLevel.Verbose, "{0}", properties.Field.SchemaXml); // prevent problems with string.Format when xml has {0} etc in it

      FieldLink fl = ct.FieldLinks.Add(properties.ConvertSP14Safe()); // this should set HasPendingRequest = true
      fl.Update(properties, updateChildTypes, trace);
      if (context.HasPendingRequest) {
        trace.Trace(TraceLevel.Verbose, "Calling Update on content type");
        ct.Update(updateChildTypes);
        trace.Trace(TraceLevel.Verbose, "Executing query.");
        try {
          context.ExecuteQuery();
          //return fl;
        } catch (Exception ex) {
          if (ex.Message.ToLower().Contains("unknown")) {
            // TODO do something with this and figure out why...
            //return fl;
          } else {
            throw ex;
          }
          //return false;
        }
      }

      // needed to get a usable FieldLink object
      trace.TraceVerbose("Getting ContentType FieldLinks");
      ct.Context.Load(ct.FieldLinks);
      ct.FieldLinks.RefreshLoad();
      ct.Context.ExecuteQuery();
      trace.TraceVerbose("Getting Field Link Properties");
      string name = (string.IsNullOrEmpty(properties.Name)) ? properties.Field.InternalName : properties.Name;
      FieldLink fl2 = ct.FieldLinks.Where(l => l.Name == name).FirstOrDefault();
      return (fl2 != null) ? fl2 : fl;
    }

    public static FieldLink AddSiteColumn(this ContentType ct, string internalNameOrId, bool updateChildTypes, FieldLinkRequireStatus flStatus, WebContextManager contextManager = null, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)ct.Context;
      // TODO implement site column cache
      Web web = context.Web;
      SiteColumnFindMethod findMethod = SiteColumnFindMethod.Any;
      bool recurseAllParentWebs = true;
      // try getting the named site column from this web site or any parent web site
      Field sc = web.GetSiteColumn(internalNameOrId, findMethod, recurseAllParentWebs, contextManager, trace);
      if (sc == null) {
        trace.Trace(TraceLevel.Error, string.Format("Site column '{0}' was not found in any of the webs.", internalNameOrId));
        return null;
      }
      FieldLinkProperties flp = new FieldLinkProperties() {
        Hiro = flStatus,
        Field = sc
      };
      return ct.AddFieldLink(flp, updateChildTypes, trace);
    }

    // TODO make something useful of these in the site column extensions and fucntions

    // this does really make sense anymore unless we pass it to the sub-function
    /*
    if ((sc.Hidden || sc.Group == "_Hidden") && !AllowHiddenSiteColumns)
      throw new Exception("To add a hidden column you must specify -AllowHiddenSiteColumns = $true");
     */

    /*
    WriteVerbose("Unhiding site column");
    if (sc.Hidden) {
      sc.Hidden = false;
      sc.Group = "Custom Columns";
      sc.Update();
      context.ExecuteQuery();
    }
    context.ExecuteQueryIfNeeded();
     */

    public static ContentType Update(this ContentType ct,
      ContentTypeProperties properties,
      bool updateChildren = true,
      bool skipCreateInfo = false
    ) {
      ClientContext context = (ClientContext)ct.Context;
      if (!skipCreateInfo) {
        ct.Description = properties.Description;
        ct.Group = properties.Group;
        ct.Name = properties.Name;
        // these can't be changed after the fact
        //ct.StringId = properties.Id;
        //ct.Parent = properties.ParentContentType;
      }
      if (properties.HasExtendedSettings) {
        if (properties.Hidden.HasValue)
          ct.Hidden = properties.Hidden.Value;
        if (properties.ReadOnly.HasValue)
          ct.ReadOnly = properties.ReadOnly.Value;
#if !DOTNET_V35
        if (properties.Sealed.HasValue)
          ct.Sealed = properties.Sealed.Value;
#endif
        if (!string.IsNullOrEmpty(properties.DisplayFormTemplateName))
          ct.DisplayFormTemplateName = properties.DisplayFormTemplateName;
        if (!string.IsNullOrEmpty(properties.DisplayFormUrl))
          ct.DisplayFormUrl = properties.DisplayFormUrl;
        if (!string.IsNullOrEmpty(properties.DocumentTemplate))
          ct.DocumentTemplate = properties.DocumentTemplate;
        if (!string.IsNullOrEmpty(properties.EditFormTemplateName))
          ct.EditFormTemplateName = properties.EditFormTemplateName;
        if (!string.IsNullOrEmpty(properties.EditFormUrl))
          ct.EditFormUrl = properties.EditFormUrl;
#if !DOTNET_V35
        if (!string.IsNullOrEmpty(properties.JSLink))
          ct.JSLink = properties.JSLink;
        if (!string.IsNullOrEmpty(properties.MobileDisplayFormUrl))
          ct.MobileDisplayFormUrl = properties.MobileDisplayFormUrl;
        if (!string.IsNullOrEmpty(properties.MobileEditFormUrl))
          ct.MobileEditFormUrl = properties.MobileEditFormUrl;
        if (!string.IsNullOrEmpty(properties.MobileNewFormUrl))
          ct.MobileNewFormUrl = properties.MobileNewFormUrl;
#endif
        if (!string.IsNullOrEmpty(properties.NewFormTemplateName))
          ct.NewFormTemplateName = properties.NewFormTemplateName;
        if (!string.IsNullOrEmpty(properties.NewFormUrl))
          ct.NewFormUrl = properties.NewFormUrl;

        ct.Update(updateChildren); 
      }
      context.ExecuteQueryIfNeeded();
      return ct;
    }

#region Extensions for ContentTypeCollection

    /// <summary>
    /// Creates a new content type
    /// </summary>
    /// <param name="web"></param>
    /// <param name="ctxMgr">Optionally, the context manager can be used for client-side cache management</param>
    /// <returns></returns>
    public static ContentType AddContentType(this ContentTypeCollection ctc,
      ContentTypeProperties properties,
      //ContentType parent, string ctid, string name, string group, string description = "", bool isHidden = false, bool isReadOnly = false, bool isSealed = false,
      WebContextManager ctxMgr = null) {
      ClientContext context = (ClientContext)ctc.Context;
      // TODO test if we already created it
      // TODO we have library functions someplace that support testing for parents/children etc
#if !DOTNET_V35
      if (properties.ParentContentType == null && string.IsNullOrWhiteSpace(properties.Id))
#else
      if (properties.ParentContentType == null && StringTools.IsNullOrWhiteSpace(properties.Id))
#endif
        throw new ArgumentNullException("Id required when ParentContentType is null. Provide one or the other.");
      bool spVersionOld = false;
#if !DOTNET_V35
      if (spVersionOld && !string.IsNullOrWhiteSpace(properties.Id))
        throw new NotSupportedException("You can't specify ID in older versions of SharePoint.");
      string parentId = (properties.ParentContentType == null) ? string.Empty : properties.ParentContentType.Id.StringValue;
#else
      if (spVersionOld && !string.IsNullOrEmpty(properties.Id))
        throw new NotSupportedException("You can't specify ID in older versions of SharePoint.");
      string parentId = (properties.ParentContentType == null) ? string.Empty : properties.ParentContentType.Id.ToString();
#endif
      // if you provide both it is still wise to check that ctid is valid
      KrakenContentTypeExtensions.IsContentTypeIdValid(properties.Id, parentId);
      ContentType ct = ctc.Add(properties.ConvertSP14Safe());
      context.ExecuteQuery();
      context.Load(ct, t => t.Id, t => t.Group, t => t.Name, t => t.SchemaXml);
      ct.Update(properties, false, true); // It's just a baby, so I sure hope it doesn't have any kids!
      return ct;
    }

    public static IEnumerable<ContentType> GetByGroup(this ContentTypeCollection ctc, string groupName, bool executeQuery = true) {
      ClientContext context = (ClientContext)ctc.Context;
      var result = context.LoadQuery(ctc.Where(c => c.Group == groupName).GetDefaultProperties());
      if (executeQuery)
        context.ExecuteQuery();
      return result;
    }

    public static ContentType GetByNameOrId(this ContentTypeCollection ctc, string contentTypeNameOrId) {
      ClientContext context = (ClientContext)ctc.Context;
#if !DOTNET_V35
      var result = context.LoadQuery(ctc.Where(c => c.Name == contentTypeNameOrId || c.Id.StringValue == contentTypeNameOrId).GetDefaultProperties());
#else
      var result = context.LoadQuery(ctc.Where(c => c.Name == contentTypeNameOrId || c.Id.ToString() == contentTypeNameOrId).GetDefaultProperties());
#endif
      context.ExecuteQuery();
      ContentType targetContentType = result.FirstOrDefault();
      return targetContentType;
    }

#endregion

  }

}
