using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Microsoft.SharePoint;

using Kraken.Xml.Linq;
using Kraken.SharePoint.Cloud.Authentication;
using Kraken.SharePoint.Cloud.Client;
using Kraken.SharePoint.Configuration;

namespace Kraken.SharePoint.ContentTypes {

  /// <summary>
  /// This class basically does many of the same things as WebsWebServiceClientManager
  /// However, it uses the SharePoint Server API and therefore cannot be used across the
  /// network.
  /// </summary>
  /// <remarks>
  /// Supercedes SPContentTypeFeatureTools and SPContentTypeFeatureToolsX
  /// </remarks>
  public class SPContentTypeManager : IContentTypeManager, ISiteColumnManager {

    private SPWeb web;

    private WebsWebServiceClientManager websSvcMgr;
    public WebsWebServiceClientManager WebsClientManager {
      get {
        if (websSvcMgr == null)
          websSvcMgr = new WebsWebServiceClientManager(
            WebsWebServiceClientManager.CreateInstance(web.Url, SharePointAuthenticationType.CurrentWindowsUser, string.Empty, string.Empty, string.Empty)
          );
        return websSvcMgr;
      }
    }

    public SPContentTypeManager(SPWeb targetWeb) {
      web = targetWeb;
    }

    #region RefreshListContentTypes Event

    public event ListContentTypeRefreshEventHandler RefreshListContentTypes;

    public void RemoveAllRefreshListContentTypes() {
      RefreshListContentTypes = null;
    }

    //RefreshListContentTypes += new ListContentTypeRefreshEventHandler(Default_RefreshListContentTypes);
    public void OnRefreshListContentTypes(List<string> contentTypeNames) {
      if (RefreshListContentTypes != null) {
        ListContentTypeRefreshEventArgs e = new ListContentTypeRefreshEventArgs(contentTypeNames);
        RefreshListContentTypes(this.web, e);
      }
    }

    public static void DoRefreshListContentTypes(object web, ListContentTypeRefreshEventArgs args) {
      SPWeb targetWeb = web as SPWeb;
      if (web == null)
        throw new ArgumentNullException("Expecting a valid object of type SPWeb.", "web");
      if (args.UseTimerJob) {
        // runs ContentTypeRefreshTimerJob.DoRefreshListContentTypes as a run-once timer job
        ContentTypeRefreshTimerJob.CreateInstance(targetWeb, args);
      } else {
        ContentTypeRefreshTimerJob.DoRefreshListContentTypes(targetWeb, args);
      }

    }

    #endregion

    #region Content Types

    /// <summary>
    /// Checks a new set of content type in an element XML file
    /// and adds or updates them using the SP Webs.asmx web service.
    /// IMPORTANT: This override uses a file crawl of the 14 Hive to get the element.xml from an SP feature.
    /// </summary>
    /// <param name="web">
    /// Web you want to create content types for, 
    /// or use SPSite.RootWeb for site collection level.
    /// </param>
    /// <param name="elementFilePath">Path to the file in the 14 Hive</param>
    public void EnsureContentTypes(string elementFilePath) {
      XDocument doc = XDocument.Load(elementFilePath);
      XElement elementDoc = doc.Root;
      EnsureContentTypes(elementDoc);
    }

    /// <summary>
    /// Checks a new set of content type in an element XML file
    /// and adds or updates them using the SP Webs.asmx web service.
    /// IMPORTANT: This override uses reflection of server API to create new content types.
    /// </summary>
    /// <param name="web"></param>
    /// <param name="elementDoc"></param>
    public void EnsureContentTypes(XElement elementDoc) {
      // get all ContentType nodes in /ContentTypes/ContentType
      XElement currentContentTypeDefs = WebsClientManager.XGetContentTypesCollection();

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
        // TODO: we could do this by name and group too to prevent weird conflcits...

        if (existingCType != null) {
          WebsClientManager.XUpdateContentType(existingCType, updatingCType, cTypesNeedUpdate);
        } else { // if not found...
          // WARNING: the following uses reflection to hack the content type
          // uh-oh... uuurrrrtttt! (that's a breaking/record scratching sound, btw))
          XCreateContentType(updatingCType, string.Empty);
        }
      } // for
      // Now we have updated the content types. If that succeeded, update the list ct's too.
      OnRefreshListContentTypes(cTypesNeedUpdate);
    }

    public string XCreateContentType(XElement cType, string parentContentTypeId) {
      // WARNING: the following uses reflection to hack the content type
      // uh-oh... uuurrrrtttt! (that's a breaking/record scratching sound, btw))
      web.CreateContentType(cType.ToXmlNode());
      // TODO move this extension method so it only gets called here...
      return string.Empty;
    }
    public string GetParentContentTypeId(XElement contentTypes, string childID) {
      return WebsClientManager.GetParentContentTypeId(contentTypes, childID);
    }

    /// <summary>
    /// Updates an existing content type using Webs.asmx web service.
    /// Adds the CT to a running list useful for updating list content types later.
    /// </summary>
    /// <param name="web">SPWeb on the server API that houses the content type</param>
    /// <param name="updatingCTypeDefinition">The new definition or element.xml</param>
    /// <param name="existingCTypeDefinition">The old content type as it exists now.</param>
    /// <param name="cTypesNeedUpdate">A list of updated content types to append to</param>
    /// <returns></returns>
    public XElement XUpdateContentType(XElement existingCTypeDefinition, XElement updatingCTypeDefinition, List<string> cTypesNeedUpdate) {
      return WebsClientManager.XUpdateContentType(existingCTypeDefinition, updatingCTypeDefinition, cTypesNeedUpdate);
    }

    #endregion

    #region Site Columns Stuff

    /// <summary>
    /// Calls a web service to ensure that site columns exist.
    /// </summary>
    /// <param name="web">
    /// Web you want to create site columns for, 
    /// or use SPSite.RootWeb for site collection level.
    /// </param>
    /// <param name="elementFilePath"></param>
    /// <returns></returns>
    public XElement EnsureSiteColumns(string elementFilePath) {
      XDocument doc = XDocument.Load(elementFilePath);
      XElement elementDoc = doc.Root;
      return EnsureSiteColumns(elementDoc);
    }

    /// <summary>
    /// Given an element file and a web, ensures the fields have been created.
    /// Uses web service, rather than provisioning directly through a feature,
    /// which allows for some interesting "hacks".
    /// </summary>
    /// <param name="web">The target web</param>
    /// <param name="elementDoc">XmlDocument of the element.xml file with Feature nodes</param>
    public XElement EnsureSiteColumns(XElement elementDoc) {
      XElement result = WebsClientManager.EnsureSiteColumns(elementDoc);
      return result;
    }

    #endregion

  }

}
