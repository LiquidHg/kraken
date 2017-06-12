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
  public class ListsWebServiceClientManager : WebServiceClientManager<ListsWS.Lists> {

    public ListsWebServiceClientManager(ListsWS.Lists listsService) : base(listsService) { }

    private XElement lists = null;
    public XElement Lists {
      get {
        EnsureLists();
        return lists;
      }
    }
    public void EnsureLists() {
      if (lists == null)
        lists = GetLists(); // .StripSchema();
    }

    private string _currentListName;

    private XElement _listContentTypes;
    public XElement ListContentTypes {
      get { return _listContentTypes; }
    }

    /// <summary>
    /// Determines if the provided list is the one we were working on before.
    /// If not, then we'll need to flush the cache items so we can load new ones.
    /// </summary>
    /// <returns></returns>
    private bool IsSameListAsPrevious(string listName) {
      return string.Equals(listName, _currentListName, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Makes sure that ListContentTypes has some data in it.
    /// </summary>
    /// <param name="listName"></param>
    public void EnsureListContentTypesCollection(string listName) {
      if (!IsSameListAsPrevious(listName)) {
        _listContentTypes = null;
        _currentListName = listName;
      }
      if (_listContentTypes == null)
        _listContentTypes = GetListContentTypesCollection(listName); //.StripSchema();
    }

    #region Lists and List Schemas

    public XElement GetLists() {
      return this.WebService.GetListCollection().ToXElement();
    }

    public XElement GetListSchema(Guid listId) {
      return GetListSchema(listId.ToString());
    }
    public XElement GetListSchema(string listName) {
      XElement listSchema = this.WebService.GetList(listName).ToXElement().StripSchema();

      // for whatever reason the relative URL of the list doesn't seem to be included in the detailed list schema.
      XElement listInfo = (from l in this.Lists.Descendants()
                           where l.Name.LocalName == "List"
                           && (l.TryGetAttributeValue("Name", string.Empty) == listName
                           || l.TryGetAttributeValue("ID", string.Empty) == listName)
                           select l).FirstOrDefault<XElement>();
      if (listInfo != null && listSchema.Attributes("Url") == null) {
        string url = listInfo.TryGetAttributeValue("Url", string.Empty);
        if (!string.IsNullOrEmpty(url))
          listSchema.SetAttributeValue("Url", url);
      }
      return listSchema;
    }

    public XElement UpdateListSchema(Guid listId, XElement newListSchema, XElement currentListSchema = null, bool includeFields = true) {
      return UpdateListSchema(listId.ToString(), newListSchema, currentListSchema, includeFields);
    }
    /// <summary>
    /// Updates a list using the classic 2007 era Lists.asmx web service
    /// </summary>
    /// <param name="listName"></param>
    /// <param name="currentListSchema"></param>
    /// <param name="newListSchema">See remarks for supported attributes.</param>
    /// <param name="includeFields"></param>
    /// <returns></returns>
    /// <remarks>
    /// For newListSchema, only the following fields are supported.
    /// it's not our fault. Blame Microsoft!
    /// "Title":
    /// "Description":
    /// "Direction":
    /// "EnableAssignedToEmail": // bool - only for IssueList
    /// "AllowMultiResponses": bool
    /// "EnableAttachments": bool
    /// "EnableModeration": bool
    /// "EnableVersioning": bool
    /// "Hidden": bool
    /// "MultipleDataList": bool
    /// "Ordered": bool
    /// "ShowUser": bool
    /// "RequireCheckout": bool
    /// "EnableMinorVersion": bool
    /// "OnQuickLaunch": bool
    /// "PreserveEmptyValues": bool for calulculated fields
    /// "StrictTypeCoercion": bool for calulculated fields
    /// "EnforceDataValidation": bool
    /// Validation.Message
    /// Validation.Formula
    /// </remarks>
    public XElement UpdateListSchema(string listName, XElement newListSchema, XElement currentListSchema = null, bool includeFields = true) {
      if (currentListSchema == null)
        currentListSchema = GetListSchema(listName);

      // get this from the current list schema and increment it
      string listVersion = currentListSchema.TryGetAttributeValue("Version", string.Empty);
      if (!string.IsNullOrEmpty(listVersion)) {
        int version = 0;
        if (int.TryParse(listVersion, out version)) {
          // we put this through reflector and determined
          // the behavior is that if listVersion is empty or 0
          // it will not check for version conflicts
          // otherwise it needs to match the number on the server
          listVersion = version.ToString();
        }
      }
      // some work here to get the correct sub-element
      XElement currentFields = currentListSchema.TryGetSingleElementByName("Fields");
      XElement targetFields = newListSchema.TryGetSingleElementByName("Fields");

      XElement newFields = null;
      XElement updateFields = null;
      XElement deleteFields = null;
      // if we didn't pass them in, just ignore fields
      if (includeFields && currentFields != null && targetFields != null) {
        // divide fields up into buckets
        newFields = FieldXMLTools.BuildWebServiceDeltaFieldsNode(
          BuildWebServiceFieldsNodeType.NewFields,
          currentFields, targetFields, "Field", "Name", false);
        updateFields = FieldXMLTools.BuildWebServiceDeltaFieldsNode(
          BuildWebServiceFieldsNodeType.ExstingFields,
          currentFields, targetFields, "Field", "Name", false);
        // BuildFieldsNode(xmlDoc, deleteFieldsXQuery, false);
      }
      
      // clean the fields node out of list schema
      XElement newListProperties = new XElement(newListSchema).StripSchema();
      newListProperties.TryRemoveSingleElementByName("Fields");
      newListProperties.TryRemoveSingleElementByName("ServerSettings");
      newListProperties.TryRemoveSingleElementByName("RegionalSettings");
      // Things that don't make sense to copy
      newListProperties.TryRemoveAttribute("Version");
      newListProperties.TryRemoveAttribute("Created");
      newListProperties.TryRemoveAttribute("Modified");
      newListProperties.TryRemoveAttribute("LastDeleted");
      newListProperties.TryRemoveAttribute("ItemCount");

      // call the web service
      return this.WebService.UpdateList(
        listName,
        newListProperties.ToXmlNode(),
        (newFields == null) ? null : newFields.ToXmlNode(),
        (updateFields == null) ? null : updateFields.ToXmlNode(),
        (deleteFields == null) ? null : deleteFields.ToXmlNode(),
        listVersion
      ).ToXElement();
      // TODO interpret the results and signal any errors
    }

    #endregion

    #region List Content Types

    public XElement GetListContentType(string listName, string contentTypeId) {
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      XmlNode node = this.WebService.GetListContentType(listName, contentTypeId);
      XElement ctXml = node.ToXElement();
      if (ctXml.Name.LocalName != "ContentType")
        throw new Exception("Was expecting an XML element <ContentType />.");
      return ctXml;
    }
#if DOTNET_V35
    public XElement GetListContentTypesCollection(string listName) {
        return GetListContentTypesCollection(listName, string.Empty);
    }
    public XElement GetListContentTypesCollection(string listName, string contentTypeId) {
#else
    public XElement GetListContentTypesCollection(string listName, string contentTypeId = "") {
#endif
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      if (string.IsNullOrEmpty(contentTypeId))
        contentTypeId = "0x0101";
      XmlNode node = this.WebService.GetListContentTypes(listName, contentTypeId);
      XElement ctXml = node.ToXElement();
      if (ctXml.Name.LocalName != "ContentTypes")
        throw new Exception("Was expecting an XML element <ContentTypes />.");
      return ctXml;
    }
    public List<XElement> GetListContentTypes(string listName) {
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      XElement ctXml = GetListContentTypesCollection(listName);
      List<XElement> cTypes = ctXml.GetAllElementsOfType("ContentType");
      return cTypes;
    }

    /// <summary>
    /// Gets all the content type schemas associated with a given list.
    /// Note: this method can be quite chatty and take a while to finish.
    /// </summary>
    /// <param name="listName"></param>
    /// <returns></returns>
    public XElement GetAllListContentTypes(string listName) {
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      EnsureListContentTypesCollection(listName);
      List<string> contentTypeIDs = (from v in this.ListContentTypes.Descendants()
                                     where v.Name.LocalName == "ContentType"
                                     select v.TryGetAttributeValue("ID", string.Empty)).ToList<string>();
      XElement cts = new XElement("ContentTypes");
      foreach (string contentTypeID in contentTypeIDs) {
        XElement ct = this.GetListContentType(listName, contentTypeID);
        ct = ct.StripSchema();
        cts.Add(ct);
      }
      return cts;
    }

    public XElement ExportListContentTypes(string listName, ContentTypeExportOptions ctOptions, SiteColumnExportOptions fieldOptions, SharePointConnection connection) {
      EnsureLists();
      EnsureListContentTypesCollection(listName);
      //EnsureListContentTypes(listName);
      XElement contentTypes = GetAllListContentTypes(listName);
      return ExportListContentTypes(contentTypes, ctOptions, fieldOptions, connection);
    }
    public XElement ExportListContentTypes(XElement contentTypes, ContentTypeExportOptions ctOptions, SiteColumnExportOptions fieldOptions, SharePointConnection connection) {
      List<XElement> cts = (from ct in contentTypes.Descendants()
                            where ct.Name.LocalName == "ContentType"
                            select ct).ToList<XElement>();
      // contentTypes.Descendants().ToList<XElement>()
      foreach (XElement ct in cts) {
        // clean up garbage nodes that are not useful
        if (ctOptions.RemoveFolderXmlNode)
          ct.TryRemoveSingleElementByName("Folder");
        if (ctOptions.RemoveDocumentsXmlNode)
          ct.TryRemoveSingleElementByName("XmlDocuments");
        if (ctOptions.RemoveFeatureId) // TODO enhance support for this to provide warnings
          ct.TryRemoveAttribute("FeatureId");
        if (ctOptions.RemoveVersion) // TODO enhance support for this
          ct.TryRemoveAttribute("Version");
        // add Overwrite attribute
        if (ctOptions.AddOverwrite && ct.Attribute("Overwrite") == null) {
          XAttribute overwrite = new XAttribute("Overwrite", true.ToString().ToUpper());
          ct.Add(overwrite);
        }
        // strip out empty infopath/xml attributes that do not have any value
        FieldXMLTools.TrimFieldXmlAttributes(ct);
        // clean up some more junk attributes in Fields
        FieldXMLTools.TrimFieldAttributes(fieldOptions, ct);
        // replace lookup IDs with names
        if (ctOptions.ReplaceLookupListIDWithName)
          ReplaceLookupFieldLists(ct, ctOptions.EnableLookupListIDWarningFormat, ctOptions.SearchRootSiteForLookupListID, connection);
      }
      return contentTypes;
    }

    public void ReplaceListNamesWithIDs(XElement container) {
      if (true) {
        List<XElement> lookupFields = (from f in container.Descendants()
                                 where f.Name.LocalName == "Field"
                                 && f.TryGetAttributeValue("Type", string.Empty) == "Lookup"
                                 select f).ToList<XElement>();
        foreach (XElement lookupField in lookupFields) {
          string list = lookupField.TryGetAttributeValue("List", string.Empty);
          try {
            // if this works there is nothing we should do
            Guid tryGuid = new Guid(list);
          } catch {
            bool isCurrentSite = true;
            if (list.StartsWith(ROOT_WEB_TOKEN)) {
              isCurrentSite = false;
              // TODO handle doing the lookup from the root site instead
            } else {

              var listsToLookup = from l in this.Lists.Descendants()
                                       where l.Name.LocalName == "List"
                                       select new {
                                         ID = l.TryGetAttributeValue("ID", string.Empty),
                                         RootFolder = l.TryGetAttributeValue("RootFolder", string.Empty),
                                         WebFullUrl = l.TryGetAttributeValue("WebFullUrl", string.Empty),
                                         DefaultViewUrl = l.TryGetAttributeValue("DefaultViewUrl", string.Empty),
                                         Url = this.GetRelativeUrl(l)
                                       };
              /*
              XElement listToLookup = (from l in this.Lists.Descendants()
                                       where l.Name.LocalName == "List"
                                       && this.GetRelativeUrl(l).Equals(list, StringComparison.InvariantCultureIgnoreCase)
                                       select l).FirstOrDefault<XElement>();
              if (listToLookup != null) {
                string id = listToLookup.TryGetAttributeValue("ID", string.Empty);
                XElement listCopy = new XElement(listToLookup);
                // correct list was found, set the ID based on the list
                if (!string.IsNullOrEmpty(id))
                  lookupField.SetAttributeValue("ID", id);
              }
               */
              string listToLookupID = (from l in listsToLookup
                                      where l.Url.Equals(list)
                                      select l.ID).FirstOrDefault<string>();
              if (!string.IsNullOrEmpty(listToLookupID))
                lookupField.SetAttributeValue("ID", listToLookupID);

            }            
          }
        }
      }
    }

    /// <summary>
    /// This function attempts to resolve any Field element's List attribute and 
    /// convert it from an ID to a Url in order to support moving it to a new site.
    /// </summary>
    /// <param name="container">Any element that has Fields/Field elements below it.</param>
    public void ReplaceLookupFieldLists(XElement container, bool enableLookupListIDWarningFormat, bool enableSearchRootSiteForLookupList, SharePointConnection connection) {
      EnsureLists();
      List<XElement> lookupFields = (from f in container.Descendants()
                                     where f.Name.LocalName == "Field"
                                     && f.TryGetAttributeValue("Type", string.Empty) == "Lookup"
                                     select f).ToList<XElement>();
      foreach (XElement lookupField in lookupFields) {
        string listId = lookupField.TryGetAttributeValue("List", string.Empty);
        if (!string.IsNullOrEmpty(listId)) {
          bool isCurrentSite = true;
          XElement listToLookup = (from l in this.Lists.Descendants()
                                   where l.Name.LocalName == "List"
                                   && l.TryGetAttributeValue("Name", string.Empty).Equals(listId, StringComparison.InvariantCultureIgnoreCase)
                                   select l).FirstOrDefault<XElement>();
          if (enableSearchRootSiteForLookupList && listToLookup == null && connection != null) {
            XElement webx = connection.WebsManager.GetWebProperties();
            // TODO spawn a connection to the root web site
          }
          if (listToLookup != null) {
            bool success = SetRelativeUrl(listToLookup);
            string url = listToLookup.TryGetAttributeValue("Url", string.Empty);
            if (!string.IsNullOrEmpty(url)) {
              if (!isCurrentSite)
                url = ROOT_WEB_TOKEN + "|" + listId + "|" + url;
            } else
              url = string.Format(LIST_ID_WARNING_FORMAT, listId);
            lookupField.SetAttributeValue("List", url);
          } else if (enableLookupListIDWarningFormat) {
            lookupField.SetAttributeValue("List", string.Format(LIST_ID_WARNING_FORMAT, listId));
          }
        }
      }
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
    public void EnsureListContentTypes(string listName, XElement elementDoc) {
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      EnsureListContentTypesCollection(listName);
      // get all ContentType nodes in /ContentTypes/ContentType
      XElement currentContentTypeDefs = GetListContentTypesCollection(listName);

      // loop through element file
      List<string> cTypesNeedUpdate = new List<string>();
      List<XElement> updatingCTypes = elementDoc.GetAllElementsOfType("ContentType");
      foreach (XElement updatingCType in updatingCTypes) {

        // for currentContentTypesDoc find /ContentTypes/ContentType[@ID='']
        string cTypeID = updatingCType.Attribute("ID").Value;
        if (string.IsNullOrEmpty(cTypeID))
          throw new ArgumentNullException("cTypeID");

        //string queryExistingCTypeByID = string.Format("/ContentTypes/ContentType[@ID='{0}']", cTypeID);
        //XmlNodeList qryExistingCType = currentContentTypeDefs.SelectNodes(queryExistingCypeByID); // nsmgr
        XElement existingCType = (
            from XElement ct in currentContentTypeDefs.Descendants()
            where ct.Name.LocalName == "ContentType" && ct.Attribute("ID").Value == cTypeID
            select ct
        ).FirstOrDefault();
        XElement properties = null; // TODO support me!!!
        // TODO: we could do this by name and group too to prevent weird conflcits...
        string newId = updatingCType.Attribute("ID").Value;
        if (existingCType != null) {
          XElement deletedCType = null;
          UpdateListContentType(listName, newId, properties, existingCType, updatingCType, deletedCType);
        } else { // if not found...
          string parentId = currentContentTypeDefs.GetParentContentTypeId(newId);
          CreateListContentType(listName, updatingCType, parentId);
        }
      } // for
      // Now we have updated the content types. If that succeeded, update the list ct's too.
      //OnRefreshListContentTypes(web, cTypesNeedUpdate);
    }
    public void EnsureListContentTypes(string listName, string elementFilePath) {
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      XDocument doc = XDocument.Load(elementFilePath);
      XElement elementDoc = doc.Root;
      EnsureListContentTypes(listName, elementDoc);
    }

    /// <summary>
    /// Creates a new content type using Webs.asmx web service.
    /// Has limited ability to determine the new Content Type ID.
    /// </summary>
    /// <param name="websWebsService"></param>
    /// <param name="creatingCTypeDefinition"></param>
    /// <returns></returns>
    public string CreateListContentType(string listName, XElement creatingCTypeDefinition, string parentId) {
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      EnsureListContentTypesCollection(listName);
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
      XElement properties = new XElement(creatingCTypeDefinition);
      properties.RemoveNodes(); // gets rid of Field and FieldRef elements while keeping attributes
      // call create content type in web service
      string result = CreateListContentType(listName, cTypeName, parentId, newFields, properties);
      // TODO parse result, ensure success...
      return result;
    }
#if DOTNET_V35
    public string CreateListContentType(string listName, string displayName, string parentContentTypeId, XElement newFields, XElement properties) {
      return CreateListContentType(listName, displayName, parentContentTypeId, newFields, properties, string.Empty);
    }
    public string CreateListContentType(string listName, string displayName, string parentContentTypeId, XElement newFields, XElement properties, string addToView) {
#else
    public string CreateListContentType(string listName, string displayName, string parentContentTypeId, XElement newFields, XElement properties, string addToView = "") {
#endif
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      string result = this.WebService.CreateContentType(
        listName,
        displayName,
        parentContentTypeId,
        newFields.ToXmlNode(),
        properties.ToXmlNode(),
        addToView);
      // TODO what is result string?
      // TODO is there a way to pass a content type ID via 'properties' xml?
      return result;
    }

    /// <summary>
    /// Performs a refresh of a content type definition using the SP Webs.asmx web serivce
    /// </summary>
    /// <param name="websWebsService">Web service to use for the call to SP</param>
    /// <param name="updatingCTypeDefinition">Updated/to-be content type element.xml/defintion</param>
    /// <param name="existingCTypeDefinition">Existing content type definition</param>
    /// <param name="cTypesNeedUpdate">List of updated content types that we will need for list content type updates</param>
    public XElement UpdateListContentType(string listName, XElement existingCTypeDefinition, XElement updatingCTypeDefinition) {
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      EnsureListContentTypesCollection(listName);
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
      XElement anotherExistingCTypeDefinition = GetListContentType(listName, cTypeID);
      XElement properties = new XElement(updatingCTypeDefinition);
      properties.RemoveNodes(); // gets rid of Field and FieldRef elements while keeping attributes
      // do we need to remove stuff here???

      XElement newFields = FieldXMLTools.BuildContentTypesWebServiceFieldsNode(anotherExistingCTypeDefinition, updatingCTypeDefinition, BuildWebServiceFieldsNodeType.NewFields);
      XElement updateFields = FieldXMLTools.BuildContentTypesWebServiceFieldsNode(anotherExistingCTypeDefinition, updatingCTypeDefinition, BuildWebServiceFieldsNodeType.ExstingFields);
      XElement deleteFields = null; // TODO: implement me - maybe

      // call UpdateContentType
      XElement result = UpdateListContentType(listName, cTypeID, properties, newFields, updateFields, deleteFields);
      // TODO parse result, ensure success...
      return result;
    }
#if DOTNET_V35
    public XElement UpdateListContentType(string listName, string contentTypeId, XElement properties, XElement newFields, XElement updateFields, XElement deleteFields) {
      return UpdateListContentType(listName, contentTypeId, properties, newFields, updateFields, deleteFields, string.Empty);
    }
    public XElement UpdateListContentType(string listName, string contentTypeId, XElement properties, XElement newFields, XElement updateFields, XElement deleteFields, string addToView) {
#else
    public XElement UpdateListContentType(string listName, string contentTypeId, XElement properties, XElement newFields, XElement updateFields, XElement deleteFields, string addToView = "") {
#endif
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      XmlNode node = this.WebService.UpdateContentType(
        listName,
        contentTypeId,
        (properties == null) ? null : properties.ToXmlNode(),
        (newFields == null) ? null : newFields.ToXmlNode(),
        (updateFields == null) ? null : updateFields.ToXmlNode(),
        (deleteFields == null) ? null : deleteFields.ToXmlNode(),
        addToView);
      XElement result = node.ToXElement();
      //if (result.Name.LocalName != "ContentType")
      //    throw new Exception("Was expecting an XML element <ContentType />.");
      return result;
    }

    #endregion

    public const string WEB_FULL_URL_TOKEN = "$WEB_FULL_URL$";
    public const string ROOT_WEB_TOKEN = "$ROOT_WEB$";
    public const string LIST_ID_WARNING_FORMAT = "$REPLACE_LISTID$|{0}";

    /// <summary>
    /// Gets the relative URL from a list schema using its URL attribute, or if
    /// it doesn't have one, by playing games with DefaultViewUrl/RootFolder and WebFullUrl.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    private bool SetRelativeUrl(XElement list) {
      string url = list.TryGetAttributeValue("Url", string.Empty);
      if (!string.IsNullOrEmpty(url))
        return false; // already got one
      string rootFolder = list.TryGetAttributeValue("RootFolder", string.Empty);
      string webFullUrl = list.TryGetAttributeValue("WebFullUrl", string.Empty);
      // lists do not have RootFolder set; only document libraries
      if (string.IsNullOrEmpty(rootFolder)) {
        string viewUrl = list.TryGetAttributeValue("DefaultViewUrl", string.Empty);
        viewUrl = viewUrl.Substring(0, viewUrl.LastIndexOf("/"));
        // HACK strip off Forms folders - these appear in document libraries
        if (viewUrl.EndsWith("/Forms"))
          viewUrl = viewUrl.Substring(0, viewUrl.Length - 6);
        rootFolder = viewUrl;
      }
      if (!string.IsNullOrEmpty(rootFolder)
        && !string.IsNullOrEmpty(webFullUrl)) {
        // put a replacement token into web url and root folder
        int startAt = (webFullUrl == "/") ? webFullUrl.Length : webFullUrl.Length + 1; // +1 is to remove the / between WebFull Url and "Lists"
        rootFolder = WEB_FULL_URL_TOKEN + rootFolder.Substring(startAt);
        webFullUrl = WEB_FULL_URL_TOKEN; // order of ops is significant here
        // add a relative url
        if (rootFolder.StartsWith(WEB_FULL_URL_TOKEN)) {
          url = rootFolder.Substring(WEB_FULL_URL_TOKEN.Length);
        }
        list.SetAttributeValue("Url", url);
        list.SetAttributeValue("RootFolder", rootFolder);
        list.SetAttributeValue("WebFullUrl", webFullUrl);
        return true;
      }
      return false;
    }

    private string GetRelativeUrl(XElement list) {
      string url = list.TryGetAttributeValue("Url", string.Empty);
      if (!string.IsNullOrEmpty(url))
        return url; 
      string rootFolder = list.TryGetAttributeValue("RootFolder", string.Empty);
      string webFullUrl = list.TryGetAttributeValue("WebFullUrl", string.Empty);
      // lists do not have RootFolder set; only document libraries
      if (string.IsNullOrEmpty(rootFolder)) {
        string viewUrl = list.TryGetAttributeValue("DefaultViewUrl", string.Empty);
        if (!string.IsNullOrEmpty(viewUrl)) {
          viewUrl = viewUrl.Substring(0, viewUrl.LastIndexOf("/"));
          // HACK strip off Forms folders - these appear in document libraries
          if (viewUrl.EndsWith("/Forms"))
            viewUrl = viewUrl.Substring(0, viewUrl.Length - 6);
        }
        rootFolder = viewUrl;
      }
      if (!string.IsNullOrEmpty(rootFolder)
        && !string.IsNullOrEmpty(webFullUrl)) {
        // put a replacement token into web url and root folder
        int startAt = (webFullUrl == "/") ? webFullUrl.Length : webFullUrl.Length + 1; // +1 is to remove the / between WebFull Url and "Lists"
        rootFolder = WEB_FULL_URL_TOKEN + rootFolder.Substring(startAt);
        webFullUrl = WEB_FULL_URL_TOKEN; // order of ops is significant here
        // add a relative url
        if (rootFolder.StartsWith(WEB_FULL_URL_TOKEN)) {
          url = rootFolder.Substring(WEB_FULL_URL_TOKEN.Length);
        }
      }
      return url;
    }

    public XElement ExportList(XElement listDefinition, ListExportOptions listOptions, SiteColumnExportOptions fieldOptions, SharePointConnection connection) {
      XElement listCopy = new XElement(listDefinition);
      listCopy = listCopy.StripSchema();

      // TODO support remapping of DocTemplateUrl if possible

      // Cobble together the relative Url if you have to
      if (listOptions.ReplaceWebUrlInRootFolder)
        SetRelativeUrl(listCopy);
      // Things that definitely won't survive a web-to-web or server-to-server migration
      listCopy.TryRemoveAttribute("DefaultViewUrl", listOptions.RemoveDefaultViewUrl);
      listCopy.TryRemoveAttribute("DocTemplateUrl", listOptions.RemoveDocTemplateUrl);
      listCopy.TryRemoveAttribute("MobileDefaultViewUrl", listOptions.RemoveMobileDefaultViewUrl);
      listCopy.TryRemoveAttribute("RootFolder", listOptions.RemoveRootFolder);
      listCopy.TryRemoveAttribute("WebFullUrl", listOptions.RemoveWebFullUrl);
      listCopy.TryRemoveAttribute("WebId", listOptions.RemoveWebId);
      listCopy.TryRemoveAttribute("ScopeId", listOptions.RemoveScopeId);
      listCopy.TryRemoveAttribute("WorkFlowId", listOptions.RemoveWorkFlowId);
      listCopy.TryRemoveAttribute("Version", listOptions.RemoveVersion);
      if (listOptions.RemoveServerSettings) {
        XElement serverSettings = listCopy.TryGetSingleElementByName("ServerSettings");
        if (serverSettings != null)
          serverSettings.Remove();
      }
      // Things that don't make sense to copy
      listCopy.TryRemoveAttribute("Created", listOptions.RemoveCreated);
      listCopy.TryRemoveAttribute("Modified", listOptions.RemoveModified);
      listCopy.TryRemoveAttribute("LastDeleted", listOptions.RemoveLastDeleted);
      listCopy.TryRemoveAttribute("ItemCount", listOptions.RemoveItemCount);
      // Can potentially impact a migrated list
      listCopy.TryRemoveAttribute("HasRelatedLists", listOptions.RemoveHasRelatedLists);
      listCopy.TryRemoveAttribute("HasExternalDataSource", listOptions.RemoveHasExternalDataSource);
      listCopy.TryRemoveAttribute("FeatureId", listOptions.RemoveFeatureId);
      listCopy.TryRemoveAttribute("BaseType", listOptions.RemoveBaseType);
      // strip out empty infopath/xml attributes that do not have any value
      FieldXMLTools.TrimFieldXmlAttributes(listCopy);
      // clean up some more junk attributes in Fields
      FieldXMLTools.TrimFieldAttributes(fieldOptions, listCopy);
      // replace lookup list IDs with list names
      if (listOptions.ReplaceLookupListIDWithName)
        ReplaceLookupFieldLists(listCopy, listOptions.EnableLookupListIDWarningFormat, listOptions.ReplaceWebUrlInRootFolder, connection);
      return listCopy;
    }
    public XElement ExportList(SharePointNode selectedList, ListExportOptions listOptions, SharePointConnection connection, ContentTypeExportOptions ctOptions, SiteColumnExportOptions fieldOptions) {
      List<SharePointNode> selectedLists = new List<SharePointNode>();
      selectedLists.Add(selectedList);
      return ExportLists(selectedLists, listOptions, connection, ctOptions, fieldOptions);
    }
    public XElement ExportLists(List<SharePointNode> selectedLists, ListExportOptions listOptions, SharePointConnection connection, ContentTypeExportOptions ctOptions, SiteColumnExportOptions fieldOptions) {
      EnsureLists();
      XElement outputLists = new XElement("Lists");
      List<XElement> lists = (from XElement l in Lists.Descendants()
                              join SharePointNode ci in selectedLists
                                on (l.Attribute("Name") == null ? string.Empty : l.Attribute("Name").Value) equals ci.NameOrID
                              where l.Name.LocalName == "List"
                              orderby l.Attribute("Name").Value
                              select l).ToList<XElement>();
      foreach (XElement list in lists) {
        string listName = list.TryGetAttributeValue("Name", string.Empty);
        Guid listId = new Guid(listName); // item.NameOrID
        // TODO make async operation because it can run for a long time
        XElement fullListDefinition = GetListSchema(listId);
        XElement listCopy = ExportList(fullListDefinition, listOptions, fieldOptions, connection);
        XElement metaData = new XElement("MetaData");
        listCopy.Add(metaData);
        // Add content types and view to this list XML
        if (ctOptions != null) {
          XElement ctXml = this.ExportListContentTypes(listName, ctOptions, fieldOptions, connection);
          metaData.Add(ctXml);
        }
        if (listOptions.MoveFieldsToMetaDataNode) {
          XElement fieldsNode = (from XElement f in listCopy.Descendants()
                                 where f.Name.LocalName == "Fields"
                                 select f).FirstOrDefault();
          if (fieldsNode != null) {
            // move the node to the MetaData sub-node for compatibility with List Definitions in Visual Studio
            fieldsNode.Remove();
            metaData.Add(fieldsNode);
          }
        }
        if (connection != null) {
          XElement viewXml = connection.ViewsManager.XGetAllViews(listName);
          metaData.Add(viewXml);
        }
        // TODO we got nuthin for Forms element here
        outputLists.Add(listCopy);
      }
      return outputLists;
    }

  }

}
