using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System.Net;
using System.Diagnostics.CodeAnalysis;

using Kraken.Xml.Linq;
using System.Security; // SecureStringMarshaller
using Kraken.SharePoint.Cloud.Authentication; // SharePointAuthenticationType
using Kraken.SharePoint.ContentTypes; // IContentTypeManager and ISiteColumnManager
using Kraken.SharePoint.Cloud.Fields; // BuildWebServiceFieldsNodeType
using Kraken.SharePoint.Cloud.ContentTypes; // GetParentContentTypeId

namespace Kraken.SharePoint.Cloud.Client {

  /// <summary>
  /// Creates and consumes "old school" SharePoint web services.
  /// </summary>
  public class WebsWebServiceClientManager : WebServiceClientManager<WebsWS.Webs>, IContentTypeManager, ISiteColumnManager {

    public WebsWebServiceClientManager(WebsWS.Webs websService) : base(websService) { }

    //const string SOAP_NAMESPACE = "http://schemas.microsoft.com/sharepoint/soap/";
    public const string NO_GROUP_NAME = "{NO GROUP NAME}";

    private XElement siteColumns = null;
    public XElement SiteColumns {
      get {
        return siteColumns;
      }
    }

    private XElement contentTypes = null;
    public XElement ContentTypes {
      get {
        return contentTypes;
      }
    }

    private List<string> siteColumnGroups;
    public List<string> SiteColumnGroups {
      get {
        return siteColumnGroups;
      }
    }

    private List<string> contentTypeGroups;
    public List<string> ContentTypeGroups {
      get {
        return contentTypeGroups;
      }
    }

    public XElement GetWebProperties() {
      string url = this.WebService.Url;
      int pos = url.IndexOf("/_vti_bin/");
      if (pos >= 0)
        url = url.Substring(0, pos);
      XElement web = this.WebService.GetWeb(url).ToXElement();
      return web;
    }

    public void EnsureSiteColumnsAndContentTypes() {
      // Get the lists of Site Columns
      if (siteColumns == null) {
        siteColumns = this.XGetColumns();
        siteColumns = siteColumns.StripSchema();
      }
      // Create abstracted list of Site Column Groups
      if (siteColumnGroups == null)
        siteColumnGroups = GetAllSiteColumnGroups(siteColumns);
      // Get the lists of Content Types
      if (contentTypes == null) {
        contentTypes = this.XGetContentTypesCollection();
        contentTypes = contentTypes.StripSchema();
      }
      // Create abstracted list of Content Type Groups
      if (contentTypeGroups == null)
        contentTypeGroups = GetAllContentTypeGroups(contentTypes);
    }

    protected override void MoveWeb(string url) {
      base.MoveWeb(url);
      siteColumns = null;
      contentTypes = null;
      siteColumnGroups = null;
      contentTypeGroups = null;
    }

    #region Content Types

    public XElement XGetContentType(string cTypeID) {
      XmlNode node = this.WebService.GetContentType(cTypeID);
      XElement ctXml = node.ToXElement();
      if (ctXml.Name.LocalName != "ContentType")
        throw new Exception("Was expecting an XML element <ContentType />.");
      return ctXml;
    }
    public XElement XGetContentTypesCollection() {
      XmlNode node = this.WebService.GetContentTypes();
      XElement ctXml = node.ToXElement();
      if (ctXml.Name.LocalName != "ContentTypes")
        throw new Exception("Was expecting an XML element <ContentTypes />.");
      return ctXml;
    }
    public List<XElement> XGetContentTypes() {
      XElement ctXml = XGetContentTypesCollection();
      List<XElement> cTypes = ctXml.GetAllElementsOfType("ContentType");
      return cTypes;
    }
    public string XCreateContentType(string displayName, string parentContentTypeId, XElement newFields, XElement properties) {
      string result = this.WebService.CreateContentType(displayName, parentContentTypeId, newFields.ToXmlNode(), properties.ToXmlNode());
      // TODO what is result string?
      // TODO is there a way to pass a content type ID via 'properties' xml?
      return result;
    }
    public XElement XUpdateContentType(string cTypeID, XElement cTypeProperties, XElement newFields, XElement updateFields, XElement deleteFields) {
      XmlNode node = this.WebService.UpdateContentType(
          cTypeID,
          (cTypeProperties == null) ? null : cTypeProperties.ToXmlNode(),
          (newFields == null) ? null : newFields.ToXmlNode(),
          (updateFields == null) ? null : updateFields.ToXmlNode(),
          (deleteFields == null) ? null : deleteFields.ToXmlNode()
      );
      XElement result = node.ToXElement();
      //if (ctXml.Name.LocalName != "ContentType")
      //    throw new Exception("Was expecting an XML element <ContentType />.");
      return result;
    }

    [Obsolete("If possible use System.Xml.Linq and XElement queries instead.")]
    [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage", Justification = "This method has also been marked as obsolete.")]
    public static XmlDocument GetContentTypes(WebsWS.Webs websWebsService) {
      XmlNode ctXml = websWebsService.GetContentTypes();
      return ctXml.CreateCleanXmlDocument();
    }

    [Obsolete("If possible use System.Xml.Linq and XElement queries instead.")]
    [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage", Justification = "This method has also been marked as obsolete.")]
    public static XmlDocument GetContentType(WebsWS.Webs websWebsService, string cTypeID) {
      XmlNode ctXml = websWebsService.GetContentType(cTypeID);
      return ctXml.CreateCleanXmlDocument();
    }

    [Obsolete("If possible use System.Xml.Linq and XElement queries instead.")]
    [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage", Justification = "This method has also been marked as obsolete.")]
    public static XmlNode UpdateContentType(WebsWS.Webs websWebsService, string cTypeID, XmlNode cTypeProperties, XmlNode newFields, XmlNode updateFields, XmlNode deleteFields) {
      XmlNode result = websWebsService.UpdateContentType(cTypeID, cTypeProperties, newFields, updateFields, deleteFields);
      return result;
    }

    /// <summary>
    /// Checks a new set of content type in an element XML file
    /// and adds or updates them using the SP Webs.asmx web service.
    /// IMPORTANT: All calls are to web service, but this override
    /// may have limited control over Content Type ID.
    /// </summary>
    /// <remarks>
    /// CT attribs available are... 
    ///  Name (CT Display Name, is also in element file)
    ///  ID (the bizarre "octet" 0x0000 format, is also in element file)
    ///  Group (string like "_Hidden", is also in element file)
    ///  Description (string, is also in element file)
    ///  NewDocumentControl (string)
    ///  Scope (a SP web address)
    ///  Version (a whole number from 0 to ...)
    ///  RequireClientRenderingOnNew (TRUE or FALSE)
    /// </remarks>
    /// <param name="web">SPWeb object</param>
    /// <param name="elementDoc">Element XML file containing content type definition(s)</param>
    /// <param name="useReflectionBasedCTCreation">True to use reflection, false to use web service call</param>
    public void EnsureContentTypes(XElement elementDoc, bool matchByNameAndGroup) {
      // get all ContentType nodes in /ContentTypes/ContentType
      XElement currentContentTypeDefs = XGetContentTypesCollection();

      // loop through element file
      List<string> cTypesNeedUpdate = new List<string>();
      List<XElement> updatingCTypes = elementDoc.GetAllElementsOfType("ContentType");
      foreach (XElement updatingCType in updatingCTypes) {

        // for currentContentTypesDoc find /ContentTypes/ContentType[@ID='']
        string cTypeID = updatingCType.Attribute("ID").Value;
        string cTypeName = updatingCType.Attribute("Name").Value;
        string cTypeGroup = updatingCType.Attribute("Group").Value;
        if (string.IsNullOrEmpty(cTypeID))
          throw new ArgumentNullException("cTypeID");
        if (string.IsNullOrEmpty(cTypeName))
          throw new ArgumentNullException("cTypeName");
        if (string.IsNullOrEmpty(cTypeGroup))
          throw new ArgumentNullException("cTypeGroup");

        //string queryExistingCTypeByID = string.Format("/ContentTypes/ContentType[@ID='{0}']", cTypeID);
        //XmlNodeList qryExistingCType = currentContentTypeDefs.SelectNodes(queryExistingCypeByID); // nsmgr
        XElement existingCType = (
            from XElement ct in currentContentTypeDefs.Descendants()
            where ct.Name.LocalName == "ContentType"
            && (
              (!matchByNameAndGroup && ct.Attribute("ID").Value == cTypeID)
              || (matchByNameAndGroup
                && ct.Attribute("Name").Value == cTypeName
                && ct.Attribute("Group").Value == cTypeGroup)
            )
            select ct
        ).FirstOrDefault();
        // TODO: we could do this by name and group too to prevent weird conflcits...
        if (existingCType != null) {
          XUpdateContentType(existingCType, updatingCType, cTypesNeedUpdate);
        } else { // if not found...
          // This will not work if content types are indeterminate!
          string newId = cTypeID;
          string parentId = currentContentTypeDefs.GetParentContentTypeId(newId);
          XCreateContentType(updatingCType, parentId);
        }
      } // for
      // Now we have updated the content types. If that succeeded, update the list ct's too.
      //OnRefreshListContentTypes(web, cTypesNeedUpdate);
    }
    public void EnsureContentTypes(string elementFilePath, bool matchByNameAndGroup) {
      XDocument doc = XDocument.Load(elementFilePath);
      XElement elementDoc = doc.Root;
      EnsureContentTypes(elementDoc, matchByNameAndGroup);
    }

    /// <summary>
    /// Creates a new content type using Webs.asmx web service.
    /// Has limited ability to determine the new Content Type ID.
    /// </summary>
    /// <param name="websWebsService"></param>
    /// <param name="creatingCTypeDefinition"></param>
    /// <returns></returns>
    public string XCreateContentType(XElement creatingCTypeDefinition, string parentId) {
      string cTypeID = creatingCTypeDefinition.Attribute("ID").Value;
      if (string.IsNullOrEmpty(cTypeID))
        throw new ArgumentNullException("cTypeID");
      string cTypeName = creatingCTypeDefinition.Attribute("Name").Value;
      if (string.IsNullOrEmpty(cTypeName))
        throw new ArgumentNullException("cTypeName");
      // TODO make sure CT does not already exist, but make this error pre-check optional
      // create new fields xml bucket
      XElement newFields = FieldXMLTools.BuildContentTypesWebServiceFieldsNode(null, creatingCTypeDefinition, BuildWebServiceFieldsNodeType.NewFields);
      // create properties node
      XElement cTypeProperties = new XElement(creatingCTypeDefinition);
      cTypeProperties.RemoveNodes(); // gets rid of Field and FieldRef elements while keeping attributes
      // call create content type in web service
      string result = XCreateContentType(cTypeName, parentId, newFields, cTypeProperties);
      // TODO parse result, ensure success...
      return result;
    }

    /// <summary>
    /// Performs a refresh of a content type definition using the SP Webs.asmx web serivce
    /// </summary>
    /// <param name="websWebsService">Web service to use for the call to SP</param>
    /// <param name="updatingCTypeDefinition">Updated/to-be content type element.xml/defintion</param>
    /// <param name="existingCTypeDefinition">Existing content type definition</param>
    /// <param name="cTypesNeedUpdate">List of updated content types that we will need for list content type updates</param>
    public XElement XUpdateContentType(XElement existingCTypeDefinition, XElement updatingCTypeDefinition, List<string> cTypesNeedUpdate) {
      // count up the fields in the current content type
      // seperate into new fields and updated fields
      string cTypeID = updatingCTypeDefinition.Attribute("ID").Value;
      if (string.IsNullOrEmpty(cTypeID))
        throw new ArgumentNullException("cTypeID");
      string cTypeName = updatingCTypeDefinition.Attribute("Name").Value;
      if (string.IsNullOrEmpty(cTypeName))
        throw new ArgumentNullException("cTypeName");
      // TODO if I am passing in existing already, why do I need it again?
      // Can we compare existingCTypeDefinition amd anotherExistingCTypeDefinition in debugger and see if tehy are truly the same?
      XElement anotherExistingCTypeDefinition = XGetContentType(cTypeID);
      XElement cTypeProperties = new XElement(updatingCTypeDefinition);
      cTypeProperties.RemoveNodes(); // gets rid of Field and FieldRef elements while keeping attributes
      // do we need to remove stuff here???

      XElement newFields = FieldXMLTools.BuildContentTypesWebServiceFieldsNode(anotherExistingCTypeDefinition, updatingCTypeDefinition, BuildWebServiceFieldsNodeType.NewFields);
      XElement updateFields = FieldXMLTools.BuildContentTypesWebServiceFieldsNode(anotherExistingCTypeDefinition, updatingCTypeDefinition, BuildWebServiceFieldsNodeType.ExstingFields);
      XElement deleteFields = null; // TODO: implement me - maybe

      // call UpdateContentType
      XElement result = XUpdateContentType(cTypeID, cTypeProperties, newFields, updateFields, deleteFields);
      // TODO parse result, ensure success...

      // add the content type to the list content type update queue, we'll fire the event off later
      cTypesNeedUpdate.Add(cTypeName);
      return result;
    }


    #endregion

    /*
    // TODO this could easily be made more efficient still, just by generating new and updated in the same call
    /// <summary>
    /// Divides new and existing site column definitions into their respective buckets.
    /// Performs this opration one "bucket" at a time.
    /// </summary>
    /// <param name="currentWebCTypeDef">Existing CT as it is now.</param>
    /// <param name="featureElementCTypeDef">To-be element CT definition for "target state".</param>
    /// <param name="typeOfFields">Which bucket to fill.</param>
    /// <returns></returns>
    private XElement BuildContentTypesWebServiceFieldsNode(
          XElement currentWebCTypeDef,
          XElement featureElementCTypeDef,
          BuildWebServiceFieldsNodeType typeOfFields
      ) {
        return FieldXMLTools.BuildWebServiceDeltaFieldsNode(
          typeOfFields, currentWebCTypeDef, featureElementCTypeDef, "FieldRef", "ID", true);
    }
    private XElement BuildSiteColumnsWebServiceFieldsNode(
      XElement currentSiteColumnsDef,
      XElement featureElementSiteColumnsDef,
      BuildWebServiceFieldsNodeType typeOfFields
) {
      return FieldXMLTools.BuildWebServiceDeltaFieldsNode(
        typeOfFields, currentSiteColumnsDef, featureElementSiteColumnsDef, "Field", "Name", false);
    }
    */

    #region Site Columns

    public void ReplaceWebTokens(XElement elementDoc) {
      if (true) {
        List<XElement> fields = (from f in elementDoc.Descendants()
                                 where f.Name.LocalName == "Field"
                                 && f.TryGetAttributeValue("Type", string.Empty) == "Lookup"
                                 select f).ToList();
        foreach (XElement field in fields) {
        }
      }
    }

    /// <summary>
    /// Given an element file and a web, ensures the fields have been created.
    /// Uses web service, rather than provisioning directly through a feature,
    /// which allows for some interesting "hacks".
    /// </summary>
    /// <param name="web">The target web</param>
    /// <param name="elementDoc">XmlDocument of the element.xml file with Feature nodes</param>
    public XElement EnsureSiteColumns(XElement elementDoc) {
      XElement currentFieldsDoc = XGetColumns();
      XElement newColumns = FieldXMLTools.BuildSiteColumnsWebServiceFieldsNode(currentFieldsDoc, elementDoc, BuildWebServiceFieldsNodeType.NewFields);
      XElement updateColumns = FieldXMLTools.BuildSiteColumnsWebServiceFieldsNode(currentFieldsDoc, elementDoc, BuildWebServiceFieldsNodeType.ExstingFields);
      XElement deleteColumns = null; // BuildFieldsNode(xmlDoc, deleteFieldsXQuery, false);
      XElement result = XUpdateColumns(newColumns, updateColumns, deleteColumns);
      return result;
    }
    public XElement EnsureSiteColumns(string elementFilePath) {
      XDocument doc = XDocument.Load(elementFilePath);
      XElement elementDoc = doc.Root;
      return EnsureSiteColumns(elementDoc);
    }

    public XElement DeleteSiteColumns(List<SharePointNode> checkedItems) {
      XElement currentFieldsDoc = XGetColumns();
      /*
      // TODO figure out how we ever got a site column with no Name!
      List<XElement> columns = (from XElement sc in SiteColumns.Descendants()
                                join SharePointNode ci in checkedItems
                                on (sc.Attribute("Name") == null ? string.Empty : sc.Attribute("Name").Value) equals ci.NameOrID
                                where sc.Name.LocalName == "Field"
                                orderby GetGroupAttribute(sc), sc.Attribute("Name").Value
                                select sc).ToList<XElement>();
       */
      XElement containsColumnsToDelete = new XElement("DeleteColumns");
      foreach (SharePointNode item in checkedItems) {
        containsColumnsToDelete.Add(item.XmlSchema);
      }

      XElement newColumns = null;
      XElement updateColumns = null;
      XElement deleteColumns = FieldXMLTools.BuildSiteColumnsWebServiceFieldsNode(currentFieldsDoc, containsColumnsToDelete, BuildWebServiceFieldsNodeType.DeleteFields);
      XElement result = XUpdateColumns(newColumns, updateColumns, deleteColumns);
      return result;
    }

    /// <summary>
    /// Gets the xml from GetColumns web method and cleans it up to prepare for an update.
    /// </summary>
    /// <param name="websWebsService"></param>
    /// <param name="web"></param>
    /// <returns></returns>
    public XElement XGetColumns() {
      XmlNode node = this.WebService.GetColumns();
      XElement columnXml = node.ToXElement();
      if (columnXml.Name.LocalName != "Fields")
        throw new Exception("Was expecting an XML element <Fields />.");
      return columnXml;
    }

    // TODO refactor to use GetColumnCollection similar to content types
    public XElement XUpdateColumns(XElement newColumns, XElement updateColumns, XElement deleteColumns) {
      XmlNode node = this.WebService.UpdateColumns(
          (newColumns == null) ? null : newColumns.ToXmlNode(),
          (updateColumns == null) ? null : updateColumns.ToXmlNode(),
          (deleteColumns == null) ? null : deleteColumns.ToXmlNode()
      );
      XElement result = node.ToXElement();
      // TODO check type
      return result;
    }

    /// <summary>
    /// Gets the xml from GetColumns web method and cleans it up to prepare for an update.
    /// </summary>
    /// <param name="websWebsService"></param>
    /// <param name="web"></param>
    /// <returns></returns>
    [Obsolete("If possible use System.Xml.Linq and XElement queries instead.")]
    [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage", Justification = "This method has also been marked as obsolete.")]
    public static XmlDocument GetColumns(WebsWS.Webs websWebsService) {
      XmlNode columnXml = websWebsService.GetColumns();
      // TODO update this older function to a newer better one
      return columnXml.CreateCleanXmlDocument();
    }
    [Obsolete("If possible use System.Xml.Linq and XElement queries instead.")]
    [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage", Justification = "This method has also been marked as obsolete.")]
    public XmlNode UpdateColumns(XmlNode newColumns, XmlNode updateColumns, XmlNode deleteColumns) {
      XmlNode result = this.WebService.UpdateColumns(newColumns, updateColumns, deleteColumns);
      return result;
    }

    #endregion

    #region Xml Linq Helpers

    protected List<string> GetAllSiteColumnGroups(XElement siteColumns) {
      List<string> scGroups = (from XElement sc in siteColumns.Descendants()
                               where sc.Name.LocalName == "Field" && sc.Attribute("Group") != null //&& ct.Attribute("ID").Value == cTypeID
                               orderby sc.Attribute("Group").Value
                               select (string.IsNullOrEmpty(sc.Attribute("Group").Value) ? NO_GROUP_NAME : sc.Attribute("Group").Value)
                               ).Distinct<string>().ToList<string>();
      return scGroups;
    }
    protected List<string> GetAllContentTypeGroups(XElement contentTypes) {
      List<string> ctGroups = (from XElement ct in contentTypes.Descendants()
                               where ct.Name.LocalName == "ContentType" && ct.Attribute("Group") != null
                               orderby ct.Attribute("Group").Value
                               select (string.IsNullOrEmpty(ct.Attribute("Group").Value) ? NO_GROUP_NAME : ct.Attribute("Group").Value)
                               ).Distinct<string>().ToList<string>();
      return ctGroups;
    }

    /// <summary>
    /// This is used to retreive a normalized Site Column or Content Type group name from an XML element.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static string GetGroupAttribute(XElement e) {
      string result = e.TryGetAttributeValue("Group", NO_GROUP_NAME);
      if (string.IsNullOrEmpty(result))
        result = NO_GROUP_NAME;
      return result;
      //return (e.Attribute("Group") == null || (e.Attribute("Group").Value) ? NO_GROUP_NAME : e.Attribute("Group").Value);
    }

    public static SharePointNode ToSharePointNode(XElement x) {
      if (x.Name.LocalName == "ContentType") {
        return new SharePointNode() {
          NameOrID = x.Attribute("ID").Value,
          Group = WebsWebServiceClientManager.GetGroupAttribute(x),
          DisplayName = x.Attribute("Name").Value,
          XmlSchema = x.StripSchema()
        };
      }
      if (x.Name.LocalName == "Field" || x.Name.LocalName == "SiteColumn") {
        return new SharePointNode() {
          NameOrID = x.Attribute("Name").Value,
          Group = WebsWebServiceClientManager.GetGroupAttribute(x),
          DisplayName = x.Attribute("DisplayName").Value,
          XmlSchema = x.StripSchema()
        };
      }
      if (x.Name.LocalName == "List") {
        return new SharePointNode() {
          NameOrID = x.Attribute("ID").Value,
          Group = x.Attribute("Name").Value,
          DisplayName = x.Attribute("Title").Value,
          XmlSchema = x
        };
      }
      throw new NotSupportedException(string.Format("Xml element passed in XElement x is not supported. Type='{0}'.", x.Name.LocalName));
    }

    public List<SharePointNode> GetSelectedContentTypeGroups() {
      List<SharePointNode> selectedTypes;
      selectedTypes = (from XElement ct in this.ContentTypes.Descendants()
                       where ct.Name.LocalName == "ContentType"
                       orderby GetGroupAttribute(ct), ct.Attribute("Name").Value
                       select ToSharePointNode(ct)).ToList<SharePointNode>();
      return selectedTypes;
    }
    public List<SharePointNode> GetSelectedContentTypeGroups(List<string> groupNames) {
      List<SharePointNode> selectedTypes;
      selectedTypes = (from XElement ct in this.ContentTypes.Descendants()
                       join string groupName in groupNames
                       on GetGroupAttribute(ct) equals groupName
                       where ct.Name.LocalName == "ContentType"
                          && ct.Attribute("Group") != null && ct.Attribute("Name") != null
                       orderby GetGroupAttribute(ct), ct.Attribute("Name").Value
                       select ToSharePointNode(ct)).ToList<SharePointNode>();
      return selectedTypes;
    }

    public List<SharePointNode> GetSelectedSiteColumnGroups() {
      List<SharePointNode> selectedColumns;
      selectedColumns = (from XElement sc in this.SiteColumns.Descendants()
                         where sc.Name.LocalName == "Field"
                         orderby GetGroupAttribute(sc), sc.Attribute("Name").Value
                         select ToSharePointNode(sc)).ToList<SharePointNode>();
      return selectedColumns;
    }
    public List<SharePointNode> GetSelectedSiteColumnGroups(List<string> groupNames) {
      List<SharePointNode> selectedColumns;
      selectedColumns = (from XElement sc in this.SiteColumns.Descendants()
                         join string groupName in groupNames
                         on GetGroupAttribute(sc) equals groupName
                         where sc.Name.LocalName == "Field"
                            && sc.Attribute("Group") != null && sc.Attribute("Name") != null
                         orderby GetGroupAttribute(sc), sc.Attribute("Name").Value
                         select ToSharePointNode(sc)).ToList<SharePointNode>();
      return selectedColumns;
    }

    #endregion

    #region Import and Export

    /// <summary>
    /// Exports cleaned up site column XML for use in Element.xml or in calls to web services.
    /// </summary>
    /// <param name="checkedItems">The list of SharePoint Site Columns that we want to export</param>
    /// <param name="options">Export options for cleaning up the XML</param>
    /// <returns>An XML element "SiteColumns"</returns>
    public XElement ExportSiteColumns(List<SharePointNode> checkedItems, SharePointConnection connection, SiteColumnExportOptions options) {
      EnsureSiteColumnsAndContentTypes();
      XElement outputSiteColumns = new XElement("SiteColumns");
      /*
      XElement outputSiteColumns = new XElement(siteColumns); // get rid of everything from inside the collection node
      outputSiteColumns.RemoveNodes();
       */
      // TODO figure out how we ever got a site column with no Name!
      List<XElement> columns = (from XElement sc in SiteColumns.Descendants()
                                join SharePointNode ci in checkedItems
                                on (sc.Attribute("Name") == null ? string.Empty : sc.Attribute("Name").Value) equals ci.NameOrID
                                where sc.Name.LocalName == "Field"
                                orderby GetGroupAttribute(sc), sc.Attribute("Name").Value
                                select sc).ToList<XElement>();
      foreach (XElement col in columns) {
        XElement colCopy = new XElement(col).StripSchema();
        outputSiteColumns.Add(colCopy);
      }
      // TODO allow custom source schema ID
      // clean up some more junk attributes
      FieldXMLTools.TrimFieldAttributes(options, outputSiteColumns);
      // replace lookup list ID with name
      if (connection != null && options.ReplaceLookupListIDWithName)
        connection.ListsManager.ReplaceLookupFieldLists(outputSiteColumns, options.EnableLookupListIDWarningFormat, options.SearchRootSiteForLookupListID, connection);
      return outputSiteColumns;
    }
    /// <summary>
    /// Exports cleaned up content type XML for use in Element.xml or in calls to web services.
    /// NOTE: This method depends on having up to date info in each SharePointNode SchemaXML property.
    /// </summary>
    /// <param name="checkedItems"></param>
    /// <returns></returns>
    public XElement ExportContentTypes(List<SharePointNode> checkedItems, ContentTypeExportOptions ctOptions) {
      XElement outputContentTypes = new XElement("ContentTypes");
      //outputContentTypes.RemoveNodes();
      foreach (SharePointNode item in checkedItems) {
        outputContentTypes.Add(item.XmlSchema);
      }
      outputContentTypes = outputContentTypes.StripSchema();
      return outputContentTypes;
    }

    /// <summary>
    /// Gets the updated content type data from the web service and
    /// caches it into the XmlSchema of the SharePointNode object.
    /// </summary>
    /// <param name="selectedTypeItem"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public XElement ExportContentType(SharePointNode selectedTypeItem, ContentTypeExportOptions options) {
      if (selectedTypeItem.XmlSchema.Name.LocalName != "ContentType")
        throw new ArgumentException("Parameter 'selectedTypeItem.XmlSchema' must be an xml element 'ContentType'.", "selectedTypeItem.XmlSchema");
      XElement cleanContentType = ExportContentType(selectedTypeItem.NameOrID, options);
      selectedTypeItem.XmlSchema = cleanContentType; // update the XML reference
      return selectedTypeItem.XmlSchema;
    }

    /// <summary>
    /// Gets the content type from the web service and cleans it up for export
    /// to XML that can be used for element manifests and calls to web services.
    /// </summary>
    /// <param name="contentTypeID"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public XElement ExportContentType(string contentTypeID, ContentTypeExportOptions options) {
      // get the parent content type for comparisons
      XElement parentContentType = null;
      if (!string.IsNullOrEmpty(contentTypeID)) {
        string parentId = this.ContentTypes.GetParentContentTypeId(contentTypeID);
        if (!string.IsNullOrEmpty(parentId))
          parentContentType = this.XGetContentType(parentId);
      }
      // get the content type from the web service
      XElement contentType = this.XGetContentType(contentTypeID);
      contentType = contentType.StripSchema();

      XElement cleanContentType = new XElement(contentType);
      cleanContentType = cleanContentType.StripSchema(); // strip the namespace from the root element

      // clean up garbage nodes that are not useful
      if (options.RemoveFolderXmlNode)
        cleanContentType.TryRemoveSingleElementByName("Folder");
      if (options.RemoveDocumentsXmlNode)
        cleanContentType.TryRemoveSingleElementByName("XmlDocuments");
      if (options.RemoveFeatureId) // TODO enhance support for this to provide warnings
        cleanContentType.TryRemoveAttribute("FeatureId");
      if (options.RemoveVersion) // TODO enhance support for this
        cleanContentType.TryRemoveAttribute("Version");
      // add Overwrite attribute
      if (options.AddOverwrite && cleanContentType.Attribute("Overwrite") == null) {
        XAttribute overwrite = new XAttribute("Overwrite", true.ToString().ToUpper());
        cleanContentType.Add(overwrite);
      }
      // copy field references
      XElement fieldsNode = (from node in cleanContentType.Descendants()
                             where node.Name.LocalName == "Fields"
                             select node).FirstOrDefault<XElement>();
      if (fieldsNode != null) {
        List<XElement> fields = (from node in fieldsNode.Descendants()
                                 where node.Name.LocalName == "Field"
                                 select node).ToList<XElement>();
        XElement fieldRefs = new XElement("FieldRefs");
        foreach (XElement field in fields) {
          XElement fieldRef = FieldXMLTools.CreateFieldRefFromField(field, parentContentType, options.IncludeParentFieldRefs, false);
          if (fieldRef != null)
            fieldRefs.Add(fieldRef);
        } // foreach
        cleanContentType.Add(fieldRefs);
        // get rid of the now superflouous Fields node
        fieldsNode.Remove();
      }
      return cleanContentType;
    }

    #endregion

  }

}
