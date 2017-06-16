namespace Microsoft.SharePoint.Client {

  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using System.Text;

  using Kraken.SharePoint.Client;
  using Kraken.SharePoint.Client.Connections;
  using Kraken.Tracing;
#if !DOTNET_V35
  using Microsoft.SharePoint.Client.EventReceivers;
#endif

  public static class KrakenItemExtensions {

    /// <summary>
    /// Useful when rendering screen output for a list item
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public static Dictionary<string, string> GetAllFieldValuesAsText(this ListItem item) {
      Dictionary<string, string> textValues = new Dictionary<string, string>();
      foreach (string key in item.FieldValues.Keys) {
        object value = item.FieldValues[key];
        if (value == null) {
          textValues.Add(key, string.Empty);
        } else {
          // TODO support output of other types
          if (value.GetType() == typeof(FieldUserValue)) {
            value = ((FieldUserValue)value).LookupId + ";" + ((FieldUserValue)value).LookupValue;
          }
          textValues.Add(key, value.ToString());
        }
      }
      return textValues;
    }

    public static void ThrowOnZeroKBFile(this ListItem item) {
      string fsAsString = item["File_x0020_Size"].ToString();
      if (!string.IsNullOrEmpty(fsAsString)) {
        int fs = int.Parse(fsAsString);
        if (fs == 0) {
          throw new ZeroByteFileUploadException("Unintentionally wrote a 0 byte file to SharePoint!");
        }
      }
    }

    public static Uri GetUrl(this ListItem item, List parentList = null, ListItemUrlType urlType = ListItemUrlType.FileRefUrl, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      ClientContext ctx = ((parentList != null) ? parentList.Context : item.Context) as ClientContext;
      switch(urlType) {
        case ListItemUrlType.FileRefUrl:
          string fileRef = BuiltInFieldId.GetName(BuiltInFieldId.FileRef);
          string url = string.Empty;
          ctx.Site.EnsureProperty(trace, s => s.Url);
          url = ctx.Site.Url;
          try {
            url += item[fileRef];
          } catch (KeyNotFoundException) {
            trace.TraceWarning("Field '{0}' was not present in the list of returned fields. ", fileRef);
            return null;
          } catch (PropertyOrFieldNotInitializedException) {
            trace.TraceWarning("Field '{0}' was not initialized. ", fileRef);
            return null;
          }
          return new Uri(url);
        default:
          throw new NotImplementedException(string.Format("Provided ListItemUrlType '{0}' has not been implemented. ", urlType));
      }
    }

    /// <summary>
    /// Get a name or title to help identify a list item.
    /// Mostly used for debugging and diagnostic purposes.
    /// Also ensures that options has a default value if none specified.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="fieldValues"></param>
    /// <param name="options"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    /// <remarks>
    /// May indirectly call ExecuteQuery via EnsureProperty
    /// so make sure you either load item.ParentList.BaseType
    /// yourself, or use this only outside of exception scopes.
    /// </remarks>
    public static string GetNameOrTitle(this ListItem item, Hashtable fieldValues, UpdateItemOptions options = null, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      if (options == null) options = new UpdateItemOptions();
      trace.Trace(TraceLevel.Verbose, "Ensuring default options...");
      // Note that even if you've read BaseType for the list object that
      // has created item, item.ParentList will still not have it loaded
      // since CSOM will track them as different ClientObject instances.
      List list = item.ParentList;
      list.EnsureProperty(trace, "BaseType");
      options.EnsureDefaultValues(list.IsDocumentLibrary(trace)); // checks that options.TitleInternalFieldName has a value
      trace.Trace(TraceLevel.Verbose, "Getting title, file name, or primary search field...");
      string nameOrTitle = "unknown";
      // ignore field hashtable (new values) if this value is not provided
      if (fieldValues != null && fieldValues.ContainsKey(options.TitleInternalFieldName)) {
          var fv = fieldValues[options.TitleInternalFieldName];
          nameOrTitle = (fv != null) ? fv.ToString() : string.Empty;
      } else {
        // should have already been previously loaded
        var fv = item[options.TitleInternalFieldName];
        nameOrTitle = (fv != null) ? fv.ToString() : string.Empty;
      }
      trace.TraceVerbose("{0} = '{1}'", options.TitleInternalFieldName, nameOrTitle);
      return nameOrTitle;
    }

    public static Folder GetListItemFolder(this ListItem listItem) {
      var folderUrl = (string)listItem["FileDirRef"];
      var parentFolder = listItem.ParentList.ParentWeb.GetFolderByServerRelativeUrl(folderUrl);
      parentFolder.EnsureProperty(null, true);
      return parentFolder;
    }

    #region Updates of core metadata

    /// <summary>
    /// UpdateCoreMetadata with exception handling scope
    /// </summary>
    /// <param name="item"></param>
    /// <param name="core"></param>
    /// <param name="ctid"></param>
    /// <param name="crcHash"></param>
    /// <param name="md5Hash"></param>
    internal static void DoUpdateCoreMetadata(this ListItem item, CoreMetadataInfo core, string ctid, string crcHash, string md5Hash, ITrace trace) {
      ClientContext context = (ClientContext)item.Context;
      ExceptionHandlingScope scope = new ExceptionHandlingScope(context);
      using (scope.StartScope()) {
        using (scope.StartTry()) {
          item.UpdateCoreMetadata(core, ctid, crcHash, md5Hash);
          item.Update();
        }
        using (scope.StartCatch()) {
        }
        using (scope.StartFinally()) {
        }
      }
      context.ExecuteQuery();
      // ServerException will throw on failure to be caught by caller
      if (scope.HasException) {
        trace.TraceError(scope.ErrorMessage + " -> " + scope.ServerStackTrace);
      }
    }

    internal static void UpdateCoreMetadata(this ListItem item, CoreMetadataInfo core, string ctid, string crcHash, string md5Hash) {
      core.SetListItemMetadata(item);
      item.UpdateCoreMetadata(ctid, crcHash, md5Hash);
    }

    /// <summary>
    /// Use this method to update core system metadata such as modified date and time.
    /// Requires an item.Update() to work.
    /// It is better to use CoreMetadataInfo class where you can.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="created"></param>
    /// <param name="modified"></param>
    /// <param name="ctid"></param>
    /// <param name="localFilePathFieldName"></param>
    /// <param name="localFilePath"></param>
    /// <param name="crcHash"></param>
    /// <param name="md5Hash"></param>
    internal static void UpdateCoreMetadata(this ListItem item, DateTime created, DateTime modified, string ctid, string localFilePathFieldName, string localFilePath, string crcHash, string md5Hash) {
      // set the default content type if specified
      if (!string.IsNullOrEmpty(ctid))
        item["ContentTypeId"] = ctid;
      // set creation and modification date to same as the source file
      item["Created"] = created;
      item["Modified"] = modified;
      // TODO verify this will not throw errors and that it will save the time stamps properly
      /*
      Microsoft.SharePoint.Client.ServerException
      Invalid data has been used to update the list item. The field you are trying to update may be read only.
      item["Created_x0020_Date"] = created; //file.TimeCreated
      item["Last_x0020_Modified"] = modified; //file.TimeLastModified
       */
      // specify a different user than mine as author and editor
      //String targetAuthorAndEditor = "i:0#.f|membership|sergio.cappelletti@nubo-corp.com";
      //item["Author"] = context.Web.EnsureUser(targetAuthorAndEditor);
      //item["Editor"] = context.Web.EnsureUser(targetAuthorAndEditor);
      // TODO check to make sure "localFilPathFieldName" field exists
      if (!string.IsNullOrEmpty(localFilePathFieldName))
        item[localFilePathFieldName] = localFilePath;
      item.UpdateCoreMetadata(ctid, crcHash, md5Hash);
    }
    internal static void UpdateCoreMetadata(this ListItem item, string ctid, string crcHash, string md5Hash) {
      // TCC 2/28/2015 removed the check here since the caller is responsible for generating the hash and these are moving to open source code
      if (!string.IsNullOrEmpty(crcHash))
        item["CRC32"] = crcHash;
      if (!string.IsNullOrEmpty(md5Hash))
        item["MD5Hash"] = md5Hash;
      // TODO Unique Local FIle Sync ID
      // TODO Title
    }

    #endregion

    #region Update entire item

    public static UpdateItemResult UpdateItem(this ListItem item, Dictionary<string, object> fieldValues, UpdateItemOptions options = null, ITrace trace = null) { // , WebContextManager contextManager = null
      var ht = new Hashtable(fieldValues);
      return item.UpdateItem(ht, options, trace); // , contextManager
    }

    private static int _updateItemCounter = 0;

    public const string FIELD_TOKEN_KEEP_CURRENT_VALUE = "[KEEP_VALUE]";

    private static bool CheckSkippedFields(ListItem item, Hashtable fieldValues, string fieldName, UpdateItemOptions options, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      if (options == null)
        throw new ArgumentNullException("options");
      object fValue = fieldValues[fieldName];
      if (fValue != null && fValue.ToString() == FIELD_TOKEN_KEEP_CURRENT_VALUE) {
        if (!options.SupressSkippedFieldWarnings)
          trace.TraceVerbose("  Skipped field '{0}' because provided value was reserved keep_value token = '{1}'.", fieldName, FIELD_TOKEN_KEEP_CURRENT_VALUE);
        return true;
      }
      if (!options.OverwriteExistingData && item[fieldName] != null && !string.IsNullOrWhiteSpace(item[fieldName].ToString())) {
        if (!options.SupressSkippedFieldWarnings)
          trace.TraceVerbose("  Skipped field '{0}' because it has data and options.OverwriteExistingData was false.", fieldName);
        return true;
      } else if (!options.OverwriteWithWhitespace
        && item[fieldName] != null 
        && !string.IsNullOrWhiteSpace(item[fieldName].ToString())
        && fValue != null
        && string.IsNullOrWhiteSpace(fValue.ToString())
      ) {
        if (!options.SupressSkippedFieldWarnings)
          trace.TraceVerbose("  Skipped field '{0}' because it has data, provided value is empty/whitespace, and options.OverwriteWithWhitespace was false.", fieldName);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Update fields in an Item.
    /// Use the one in List because it actually checks field settings
    /// </summary>
    /// <param name="item"></param>
    /// <param name="fieldValues"></param>
    /// <param name="options"></param>
    /// <param name="contextManager"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    public static UpdateItemResult UpdateItem(this ListItem item, Hashtable fieldValues, UpdateItemOptions options = null, ITrace trace = null) { // , WebContextManager contextManager = null
      if (item == null)
        throw new ArgumentNullException("item");
      if (options == null)
        throw new ArgumentNullException("options");
      if (fieldValues == null)
        throw new ArgumentNullException("fieldValues");
      if (trace == null) trace = NullTrace.Default;
      if (fieldValues.Count == 0) {
        trace.TraceVerbose("Leaving because there are no entries in fieldValues");
        return UpdateItemResult.UpdateOK;
      }
      //List list = item.ParentList;
      ClientContext context = (ClientContext)item.Context;
      context.ExecuteQueryIfNeeded(); // flush pending opertations because we're using scope

      // remove fields that have explicitly been marked as skipped.
      Hashtable ht2 = new Hashtable();
      foreach (string fieldName in fieldValues.Keys) {
        if (CheckSkippedFields(item, fieldValues, fieldName, options, trace)) {
          trace.TraceVerbose("Skipping field {0} because it contained the skip value constant.", fieldName);
          continue;
        }
        ht2.Add(fieldName, fieldValues[fieldName]);
      }
      trace.TraceVerbose("{0} fields after skip-value check", ht2.Count);
      if (ht2.Count == 0)
        return UpdateItemResult.UpdateOK;
      fieldValues = ht2;

      string nameOrTitle = item.GetNameOrTitle(fieldValues, options, trace);

      trace.Trace(TraceLevel.Verbose, "Setting extended field values...");
      CoreMetadataInfo metaData = new CoreMetadataInfo(item);

      // This is redundant since it is being done in core item creation
      //list.ResolveContentTypeId(fieldValues, contextManager, trace);

      UpdateItemResult result = UpdateItemResult.NoResult;
      context.ExecuteQueryIfNeeded();
      ExceptionHandlingScope scope = new ExceptionHandlingScope(context);

      switch (options.UpdateFrequency) {
        case ItemUpdateFrequency.EveryField:
          // create a scope each time we update

          // This is redundant since it is being done in core item creation
          //list.ResolveContentTypeId(fieldValues, contextManager, trace);
          foreach (string fieldName in fieldValues.Keys) {
            trace.Trace(TraceLevel.Verbose, string.Format("Setting value for key '{0}' = '{1}'...", fieldName, fieldValues[fieldName]));
            object fValue = fieldValues[fieldName];
            using (scope.StartScope()) {
              using (scope.StartTry()) {
                  item[fieldName] = fValue;
                  item.Update();
              }
              using (scope.StartCatch()) {
              }
              using (scope.StartFinally()) {
              }
            } // scope
            context.ExecuteQuery();
            if (scope.HasException) {
              trace.TraceWarning("Error updating item that has {0}='{1}' on field='{2}'; Error='{3}'", options.TitleInternalFieldName, nameOrTitle, fieldName, scope.ErrorMessage);
              if (result != UpdateItemResult.UpdateFail)
                result = UpdateItemResult.UpdatePartialFail;
            }
          } // end field loop
          trace.Trace(TraceLevel.Verbose, "Done setting extended field values.");
          trace.Trace(TraceLevel.Verbose, "Preserving core metadata...");
          using (scope.StartScope()) {
            using (scope.StartTry()) {
              metaData.SetListItemMetadata(item);
              item.Update();
            }
            using (scope.StartCatch()) {
            }
            using (scope.StartFinally()) {
            }
          }
          trace.Trace(TraceLevel.Verbose, "Updating single field in item...");
          context.ExecuteQuery();
          if (scope.HasException) {
            trace.TraceWarning("Error updating item that has {0}='{1}' in closing metadata refresh; Error='{3}'", options.TitleInternalFieldName, nameOrTitle, scope.ErrorMessage);
            if (result != UpdateItemResult.UpdateFail)
              result = UpdateItemResult.UpdatePartialFail;
          }
          trace.Trace(TraceLevel.Verbose, "Done.");
          break;
        // TODO not sure that we can use scope here because this will exit the function
        default: //case ItemUpdateFrequency.OncePerItem:
          // create a single scope for all transactions
          foreach (string fieldName in fieldValues.Keys) {
            trace.Trace(TraceLevel.Verbose, string.Format("Setting value for key '{0}' = '{1}'...", fieldName, fieldValues[fieldName]));
          }
          trace.Trace(TraceLevel.Verbose, "Also preserving core metadata...");
          // This used to work, now we get "Incorrect usage of exception handling scope."
          using (scope.StartScope()) {
            using (scope.StartTry()) {
              foreach (string fieldName in fieldValues.Keys) {
                item[fieldName] = fieldValues[fieldName];
              } // end field loop
              metaData.SetListItemMetadata(item);
              item.Update();
            }
            using (scope.StartCatch()) {
            }
            using (scope.StartFinally()) {
            }
          } // scope.Start

          break;
      } // switch

      bool doExec = false;
      if (options.UpdateFrequency == ItemUpdateFrequency.OncePerItem) {
        doExec = true;
      } else if (++_updateItemCounter >= options.UpdateFrequencyAsNumber) {
        doExec = true;
        _updateItemCounter = 0;
      }

      if (doExec) {
        // TODO can we somehow pass in and use a progress writer
        trace.Trace(TraceLevel.Info, (options.UpdateFrequency == ItemUpdateFrequency.OncePerItem) ? "Updating metadata fields in item..." : "Updating items...");
        context.ExecuteQuery();
        if (scope != null && scope.HasException) {
          if (options.UpdateFrequency == ItemUpdateFrequency.OncePerItem)
            trace.TraceWarning("Error updating item that has {0}='{1}'; Error='{2}'", options.TitleInternalFieldName, nameOrTitle, scope.ErrorMessage);
          else
            trace.TraceWarning("Error updating {0} item batch ending with item that has title='{1}'; Error='{2}'", options.UpdateFrequencyAsNumber, nameOrTitle, scope.ErrorMessage);
          result = UpdateItemResult.UpdateFail;
        } else {
          trace.Trace(TraceLevel.Verbose, "Done.");
        }
      } else {
        trace.Trace(TraceLevel.Verbose, "Skipping update query {0} / {1}", _updateItemCounter, options.UpdateFrequencyAsNumber);
      }

      // if it didn't throw any errors then it worked
      if (result == UpdateItemResult.NoResult)
        result = UpdateItemResult.UpdateOK;
      else {
        // TODO put more detailed message here
        Exception ex = new Exception("Failed to update item");
        if (options.ThrowOnError)
          throw ex;
      }
      // TODO check to see if metadata above needed to be set too
      // TODO copy better pattern from other code Set-SPOMetadata
      return result;
    }

    /// <summary>
    /// A primitive version of UpdateItem. This version doesn't 
    /// do as much scope checking, has fewer batch processing options,
    /// and only casres about options UpdateFrequency == EveryField
    /// OverwriteExistingData, and PreserveModifiedDate.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="fieldValues"></param>
    /// <param name="options"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    [Obsolete("You should probably use UpdateItem instead, since it supports scope and more options.")]
    public static bool TrySetFieldValue(this ListItem item, Dictionary<string, object> fieldValues, UpdateItemOptions options = null, ITrace trace = null) {
      var ht = new Hashtable(fieldValues);
      return item.TrySetFieldValue(ht, options, trace);
    }
    [Obsolete("You should probably use UpdateItem instead, since it supports scope and more options.")]
    public static bool TrySetFieldValue(this ListItem item, Hashtable fieldValues, UpdateItemOptions options = null, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      if (options == null) options = new UpdateItemOptions();
      trace.Trace(TraceLevel.Verbose, "Setting values for Item ID = {0}", item.Id);
      DateTime modified = (DateTime)item["Modified"];
      bool doImmediateUpdate = (options.UpdateFrequency == ItemUpdateFrequency.EveryField && !options.PreserveModifiedDate);
      //the algebraic opposite of the above boolean condition, thus we can simplify  
      //options.UpdateFrequency != ItemUpdateFrequency.EveryField || options.PreserveModifiedDate
      foreach (string fieldName in fieldValues.Keys) {
        object fieldValue = fieldValues[fieldName];
        item.TrySetFieldValue(fieldName, fieldValue, options, doImmediateUpdate, trace);
      } // foreach fieldName
      if (options.PreserveModifiedDate) {
        trace.Trace(TraceLevel.Verbose, "Setting value for field {0} = {1}", "Modified", modified.ToString());
        item["Modified"] = modified;
      }
      try {
        item.ConditionalUpdate(!doImmediateUpdate, trace);
        return true;
      } catch (Exception ex) {
        trace.TraceError(ex);
        trace.TraceError("Failed to update multiple fields for item '{0}'. ", item.ParentList.ParentWebUrl + "/" + item.ParentList.Title);
        return false;
      }
    }

    private static void ConditionalUpdate(this ListItem item, bool doUpdate, ITrace trace = null) {
      if (!doUpdate)
        return;
      if (trace == null) trace = NullTrace.Default;
      trace.Trace(TraceLevel.Verbose, "Updating Item ID = {0}", item.Id);
      item.Update();
      item.Context.ExecuteQuery();
    }

    #endregion

    #region Set a single field value

    /// <summary>
    /// Attempt to set a single field in a list item.
    /// Only cares about option OverwriteExistingData.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="fieldName"></param>
    /// <param name="fieldValue"></param>
    /// <param name="options"></param>
    /// <param name="forceItemUpdate"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    public static bool TrySetFieldValue(
      this ListItem item
      , string fieldName
      , object fieldValue
      , UpdateItemOptions options = null
      , bool forceItemUpdate = false
      , ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      if (options == null) options = new UpdateItemOptions();
      // TODO set up some types with correct string conversions for parsing
      string valueAsText = fieldValue.ToString();
      trace.Trace(TraceLevel.Verbose, "Setting value for field {0} = {1}", fieldName, valueAsText);
      try {
        if (string.Equals(fieldName, "ContentType", StringComparison.InvariantCultureIgnoreCase)) {
          // TODO note this is ignoring the content type cache, which is declared at a much higher level in code
          item.Context.Load(item.ParentList);
          List list = item.ParentList;
          ContentType ct = list.EnsureContentType(valueAsText);
          if (ct == null) {
            ClientContext context = (ClientContext)list.Context;
            context.Load(context.Site);
            context.ExecuteQuery();
            var siteUrl = context.Site.Url;
            throw new InvalidOperationException(string.Format("Specified content type '{0}' does not exist on site '{1}'. Try adding it in site content types.", fieldValue, siteUrl));
          }
          item["ContentTypeId"] = ct.Id;
        } else {
          if (string.IsNullOrEmpty(valueAsText)) {
            trace.Trace(TraceLevel.Verbose, "Skipped because new value was empty.");
          } else if (options.OverwriteExistingData || item[fieldName] == null || string.IsNullOrEmpty(item[fieldName].ToString())) {
            item.ParseAndSetFieldValue(fieldName, valueAsText);
            //item[fieldName] = fieldValue;
          } else {
            trace.Trace(TraceLevel.Verbose, "Skipped because target field was not empty and options.OverwriteExistingData was false.");
          }
        }
        item.ConditionalUpdate(forceItemUpdate, trace);
        return true;
      } catch (Exception ex) {
        trace.TraceError("Failed to set field '{0}'. ", fieldName);
        trace.TraceError(ex);
        return false;
      }
    }

    /// <summary>
    /// In many ways, a dumber version of TrySetFieldValue.
    /// Always calls item.Update but does not execute the query.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="fieldNameOrDisplayName"></param>
    /// <param name="fieldValue"></param>
    [Obsolete("You should probably be using TrySetFieldValue instead.")]
    public static void UpdateField(this ListItem item, string fieldNameOrDisplayName, object fieldValue) {
      if (item == null)
        throw new ArgumentNullException("item");
      if (string.IsNullOrEmpty(fieldNameOrDisplayName))
        throw new ArgumentNullException("fieldNameOrDisplayName");
      if (fieldValue == null)
        throw new ArgumentNullException("fieldValue");

      var list = item.ParentList;
      var field = list.GetField(fieldNameOrDisplayName);
      if (field == null)
        throw new InvalidOperationException(string.Format("Field \"{0}\" does not exist", fieldNameOrDisplayName));

      item[field.InternalName] = fieldValue;
      item.Update();
    }

    #endregion

    // TODO belongs in a string library
    #region TryParse for pre .NET 4 - used by GetFieldValue

    public static T EnumParse<T>(this string v)
    {
        var ret = default(T);
        if (string.IsNullOrEmpty(v)) return ret;
        try
        {
            ret = (T)Enum.Parse(typeof(T), v);
        }
        catch { }
        return ret;
    }

    public static T EnumParse<T>(this string v, T defaultValue)
    {
        var ret = v.EnumParse<T>();
        if (ret.Equals(default(T)))
        {
            ret = defaultValue;
        }
        return ret;
    }

    #endregion

    // TODO get rid of this obnoxious crap - or prove it is useful
    public static T GetFieldValue<T>(this ListItem item, string fieldName, T defaultValue, ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      object o = item[fieldName];
      if (o == null) return defaultValue;

      var t = typeof(T);
      if (t.IsEnum) {
        return o.ToString().EnumParse(defaultValue);
      }
      if (!(o is IConvertible))
        return (T)o;
      try {
        return (T)Convert.ChangeType(o, t);
      } catch (Exception ex) {
        trace.TraceError(ex);
      }
      return defaultValue;
    }

    /* Older versions of CSOM did not include this API */
#if !DOTNET_V35
          #region Remote Item Event

    public static bool IsFieldChanged(
      this SPRemoteItemEventProperties itemEventProperties,
      string fieldName
          //IReadOnlyDictionary<string, object> beforeProperties,
          //IReadOnlyDictionary<string, object> afterProperties
    ) {
      Dictionary<string, object> afterProperties = itemEventProperties.AfterProperties;
      Dictionary<string, object> beforeProperties = itemEventProperties.BeforeProperties;

      // If the property fieldName doesn't exist, or the value of fieldName has changed
      // then we need to do something, so return true
      if (!beforeProperties.ContainsKey(fieldName) || !afterProperties.ContainsKey(fieldName))
        return true;
      return afterProperties[fieldName].ToString() != beforeProperties[fieldName].ToString();
    }

    #endregion
#endif

  }
}
