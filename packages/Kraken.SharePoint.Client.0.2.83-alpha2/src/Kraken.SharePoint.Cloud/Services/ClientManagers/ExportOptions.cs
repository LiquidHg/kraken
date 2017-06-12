using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Cloud {

  public class SiteColumnExportOptions {
    public bool RemoveSourceId = true;
    public bool RemoveWebId = true;
    public bool RemoveStaticName = true;
    public bool RemoveColName = true;
    public bool RemoveVersion = true;
    public bool AddOverwrite = true;
    public bool ReplaceLookupListIDWithName = true;
    public bool EnableLookupListIDWarningFormat = true;
    public bool SearchRootSiteForLookupListID = true;
  }
  public class ContentTypeExportOptions {
    public bool RemoveFolderXmlNode;
    public bool RemoveDocumentsXmlNode;
    public bool RemoveFeatureId;
    public bool RemoveVersion;
    public bool AddOverwrite;
    public bool ReplaceLookupListIDWithName = true;
    public bool EnableLookupListIDWarningFormat = true;
    public bool SearchRootSiteForLookupListID = true;
    public bool IncludeParentFieldRefs = true;
  }
  public class ListExportOptions {
    public bool MoveFieldsToMetaDataNode = true;
    public bool ReplaceWebUrlInRootFolder = true;
    public bool ReplaceLookupListIDWithName = true;
    public bool SearchRootSiteForLookupListID = true;
    public bool EnableLookupListIDWarningFormat = true;
    // Things that definitely won't survive a web-to-web or server-to-server migration
    public bool RemoveDefaultViewUrl = true;
    public bool RemoveMobileDefaultViewUrl = true;
    public bool RemoveWebId = true;
    public bool RemoveServerSettings = true;
    public bool RemoveVersion = true;
    public bool RemoveDocTemplateUrl = true;
    public bool RemoveScopeId = true;
    public bool RemoveWorkFlowId = true;
    // Not destructive but have tokens in them
    public bool RemoveRootFolder = false;
    public bool RemoveWebFullUrl = false;
    // Things that don't make sense to copy
    public bool RemoveCreated = true;
    public bool RemoveModified = true;
    public bool RemoveLastDeleted = true;
    public bool RemoveItemCount = true;
    // Can potentially impact a migrated list
    public bool RemoveFeatureId = true;
    public bool RemoveHasRelatedLists = false;
    public bool RemoveHasExternalDataSource = false;
    public bool RemoveBaseType = false;
  }

}
