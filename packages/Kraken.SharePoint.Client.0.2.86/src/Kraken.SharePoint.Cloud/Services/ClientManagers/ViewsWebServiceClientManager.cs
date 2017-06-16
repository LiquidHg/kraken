using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Kraken.Xml.Linq;
using Kraken.SharePoint.Cloud.Authentication;
//using Kraken.SharePoint.Cloud.Fields;

namespace Kraken.SharePoint.Cloud.Client {

  /// <summary>
  /// Creates and consumes "old school" SharePoint web services.
  /// </summary>
  public class ViewsWebServiceClientManager : WebServiceClientManager<ViewsWS.Views> {

    public ViewsWebServiceClientManager(ViewsWS.Views viewsService) : base(viewsService) { }

    public XElement Views {
      get { return _views; }
    }
    private XElement _views;
    /*
     * Structure is
     * /Views/View[
     *  @Name // effectively a GUID
     *  @DefaultView // boolean
     *  @MobileView // boolean
     *  @Type // "HTML", "Grid", "Chart", "Calendar", "Gantt", or "None"
     *  @DisplayName
     *  @Url // a server relative URL to the ASPX page that will have to be string replaced with the new location
     *  @Level // integer
     *  @BaseViewID // integer
     *  @ContentTypeID // 0x... pointing to the target content type, if any, or just "0x" if none.
     *  @ImageUrl // string to PNG file in /_layouts/<ver>/images/<fn.png>?rev=##
     * ]
     */

    private string _currentListName;

    /// <summary>
    /// Determines if the provided list is the one we were working on before.
    /// If not, then we'll need to flush the cache items so we can load new ones.
    /// </summary>
    /// <returns></returns>
    private bool IsSameListAsPrevious(string listName) {
      return string.Equals(listName, _currentListName, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Makes sure that Views has some data in it.
    /// </summary>
    /// <param name="listName"></param>
    public void EnsureViewsCollection(string listName) {
      if (!IsSameListAsPrevious(listName)) {
        _views = null;
        _currentListName = listName;
      }
      if (_views == null)
        _views = XGetViews(listName); //.StripSchema();
    }

    /// <summary>
    /// Gets all the view schemas associated with a given list.
    /// Note: this method can be quite chatty and take a while to finish.
    /// </summary>
    /// <param name="listName"></param>
    /// <returns></returns>
    public XElement XGetAllViews(string listName) {
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      EnsureViewsCollection(listName);
      List<string> viewNames = (from v in this.Views.Descendants()
                                where v.Name.LocalName == "View"
                                select v.TryGetAttributeValue("Name", string.Empty)).ToList<string>();
      XElement views = new XElement("Views");
      foreach (string viewName in viewNames) {
        XElement viewSchema = this.XGetView(listName, viewName);
        viewSchema = viewSchema.StripSchema();
        views.Add(viewSchema);
      }
      return views;
    }

    /// <summary>
    /// Gets the collection of views associated with a list.
    /// Does not affect internal view caching in any way.
    /// </summary>
    /// <param name="listName"></param>
    /// <returns></returns>
    public XElement XGetViews(string listName) {
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      return this.WebService.GetViewCollection(listName).ToXElement();
    }

    /*
    public XElement XGetViewSchema(string listName, Guid listId) {
      this.WebService.GetView(listName, viewName).GetXElement();
    }
    */
    public XElement XGetView(string listName, string viewName) {
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      if (string.IsNullOrEmpty(viewName))
        throw new ArgumentNullException("viewName");
      EnsureViewsCollection(listName);
      return this.WebService.GetView(listName, viewName).ToXElement();
    }

    /// <summary>
    /// Makes a RowLimit element in the correct format for dealing with the Views service.
    /// </summary>
    /// <param name="maxRows"></param>
    /// <param name="rowPages"></param>
    /// <returns></returns>
    public XElement XCreateRowLimitElement(int maxRows, bool rowPages) {
      XElement rowLimit = new XElement("RowLimit");
      rowLimit.Value = maxRows.ToString();
      rowLimit.Add(new XAttribute("Paged", rowPages.ToString().ToUpper()));
      return rowLimit;
    }
    /// <summary>
    /// Creates ViewProperties XML element from View XML element.
    /// </summary>
    /// <param name="viewXml"></param>
    /// <returns></returns>
    public XElement XCreateViewPropertiesElement(XElement viewXml) {
      /*
      bool rowPages = false; int maxRows = 0;
      {
        XElement rowLimit = (from v in viewXml.Descendants() where v.Name.LocalName == "RowLimit" select v).FirstOrDefault<XElement>();
        bool.TryParse(rowLimit.TryGetAttributeValue("Pages", string.Empty), out rowPages);
        int.TryParse(rowLimit.Value, out maxRows);
      }
       */
      XElement viewProperties = new XElement("ViewProperties");
      // TODO determine if this is needed
      string displayName = viewXml.TryGetAttributeValue("DisplayName", string.Empty);
      viewProperties.Add(new XAttribute("Title", displayName));
      viewXml.TryCloneAttribute("Name", viewProperties);
      viewXml.TryCloneAttribute("DisplayName", viewProperties);
      viewXml.TryCloneAttribute("BaseViewID", viewProperties);
      viewXml.TryCloneAttribute("Type", viewProperties);
      viewXml.TryCloneAttribute("Editor", viewProperties);
      viewXml.TryCloneAttribute("Hidden", viewProperties);
      viewXml.TryCloneAttribute("ReadOnly", viewProperties);
      viewXml.TryCloneAttribute("DefaultView", viewProperties);
      return viewProperties;
    }
    /// <summary>
    /// Creates a ViewProperties XML element from simple parameters.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="displayName"></param>
    /// <param name="baseViewID"></param>
    /// <param name="viewType">"HTML", "Grid", "Chart", "Calendar", "Gantt", or "None"</param>
    /// <param name="editor"></param>
    /// <param name="isHidden"></param>
    /// <param name="isReadOnly"></param>
    /// <param name="isDefaultView">If true, make this the new default view.</param>
    /// <returns></returns>
    public XElement XCreateViewPropertiesElement(string name, string displayName, int baseViewID, string viewType, string editor, bool isHidden, bool isReadOnly, bool isDefaultView) {
      XElement viewProperties = new XElement("ViewProperties");
      viewProperties.Add(new XAttribute("Name", name));
      viewProperties.Add(new XAttribute("Title", displayName));
      viewProperties.Add(new XAttribute("DisplayName", displayName));
      viewProperties.Add(new XAttribute("BaseViewID", baseViewID));
      viewProperties.Add(new XAttribute("Type", viewType));
      viewProperties.Add(new XAttribute("Editor", displayName));
      viewProperties.Add(new XAttribute("Hidden", isHidden));
      viewProperties.Add(new XAttribute("ReadOnly", isReadOnly));
      viewProperties.Add(new XAttribute("DefaultView", isDefaultView));
      return viewProperties;
    }

    /// <summary>
    /// Creates a new view based on XML from an existing view (on this or another server).
    /// </summary>
    /// <param name="listName"></param>
    /// <param name="viewXml"></param>
    /// <returns></returns>
    public XElement XCreateView(string listName, XElement viewXml) {
      // TODO check the view XML to make sure the list name matches the target
      string parentViewName = viewXml.TryGetAttributeValue("BaseViewID", string.Empty);
      // copy properties into node
      XElement viewProperties = XCreateViewPropertiesElement(viewXml);
      XElement viewQuery = (from v in viewXml.Descendants() where v.Name.LocalName == "ViewQuery" select v).FirstOrDefault<XElement>();
      XElement viewFields = (from v in viewXml.Descendants() where v.Name.LocalName == "ViewFields" select v).FirstOrDefault<XElement>();
      XElement rowLimit = (from v in viewXml.Descendants() where v.Name.LocalName == "RowLimit" select v).FirstOrDefault<XElement>();
      XElement aggregations = (from v in viewXml.Descendants() where v.Name.LocalName == "Aggregations" select v).FirstOrDefault<XElement>();
      XElement formats = (from v in viewXml.Descendants() where v.Name.LocalName == "Formats" select v).FirstOrDefault<XElement>();
      return XCreateView(listName, parentViewName, viewProperties, viewQuery, viewFields, aggregations, formats, rowLimit);
    }
    /// <summary>
    /// Creates a new View for a specified list.
    /// </summary>
    /// <param name="listName">Name of the list that owns this view.</param>
    /// <param name="parentViewName">
    /// A string that contains the GUID for the view, which determines the 
    /// view to use for the default view attributes represented by the query, 
    /// viewFields, and rowLimit parameters. If this argument is not supplied, 
    /// the default view is assumed.
    /// </param>
    /// <param name="viewProperties">An XML fragment that contains all the view-level properties as attributes, such as Editor, Hidden, ReadOnly, and Title.</param>
    /// <param name="viewQuery">A CAML query</param>
    /// <param name="viewFields">A ViewFields node containing FieldRef nodes</param>
    /// <param name="aggregations">
    /// 
    /// </param>
    /// <param name="formats">
    /// 
    /// </param>
    /// <returns></returns>
    public XElement XCreateView(string listName, string parentViewName, XElement viewProperties, XElement viewQuery, XElement viewFields, XElement aggregations, XElement formats, XElement rowLimit) {
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      if (viewProperties == null)
        throw new ArgumentNullException("viewProperties");
      if (viewQuery == null)
        throw new ArgumentNullException("viewQuery");
      if (viewFields == null)
        throw new ArgumentNullException("viewFields");
      if (rowLimit == null)
        throw new ArgumentNullException("rowLimit");

      if (viewProperties.Name.LocalName != "ViewProperties")
        throw new ArgumentException("viewProperties parameter must contain XML of type <ViewProperties></ViewProperties>", "viewProperties");
      if (viewQuery.Name.LocalName != "Query")
        throw new ArgumentException("viewQuery parameter must contain XML of type <Query></Query>", "viewQuery");
      if (viewFields.Name.LocalName != "ViewFields")
        throw new ArgumentException("viewFields parameter must contain XML of type <ViewFields></ViewFields>", "viewFields");
      if (rowLimit.Name.LocalName != "RowLimit")
        throw new ArgumentException("rowLimit parameter must contain XML of type <RowLimit Paged=\"TRUE\">100</RowLimit>", "rowLimit");
      // deal with semi-optional elements
      if (aggregations != null && aggregations.Name.LocalName != "Aggregations")
        throw new ArgumentException("aggregations parameter must contain XML of type <Aggregations></Aggregations>", "aggregations");
      if (formats != null && formats.Name.LocalName != "Formats")
        throw new ArgumentException("formats parameter must contain XML of type <Formats></Formats>", "Formats");

      // TOOD some checking to make sure list name exists would be nice
      // TOOD some checking to make sure parentViewName exists would also be great
      // TODO some more error checking for each of the possible field refs existing would be good too
      // TODO even more error checking for each of the possible list content types would be groovy

      EnsureViewsCollection(listName);

      string viewType = viewProperties.TryGetAttributeValue("Type", string.Empty);
      bool makeDefault = false;
      {
        makeDefault = bool.TryParse(viewProperties.TryGetAttributeValue("DefaultView", string.Empty), out makeDefault);
      }
      XmlNode results = this.WebService.AddView(
        listName,
        parentViewName,
        viewFields.ToXmlNode(),
        viewQuery.ToXmlNode(),
        rowLimit.ToXmlNode(),
        viewType,
        makeDefault
      );
      XElement newView = results.ToXElement();
      string viewName = newView.TryGetAttributeValue("Name", string.Empty);
      // handle viewProperties (including name and displayName), formats, and aggregations
      if (!string.IsNullOrEmpty(viewName)) {
        XElement updatedView = XUpdateView(listName, viewName, viewProperties, viewQuery, viewFields, aggregations, formats, rowLimit);
        return updatedView;
      } else {
        // ruh-roh!
        return newView;
      }
    }

    public XElement XUpdateView(string listName, string viewName, XElement viewXml) {
      XElement viewProperties = XCreateViewPropertiesElement(viewXml);
      XElement viewQuery = (from v in viewXml.Descendants() where v.Name.LocalName == "ViewQuery" select v).FirstOrDefault<XElement>();
      XElement viewFields = (from v in viewXml.Descendants() where v.Name.LocalName == "ViewFields" select v).FirstOrDefault<XElement>();
      XElement rowLimit = (from v in viewXml.Descendants() where v.Name.LocalName == "RowLimit" select v).FirstOrDefault<XElement>();
      XElement aggregations = (from v in viewXml.Descendants() where v.Name.LocalName == "Aggregations" select v).FirstOrDefault<XElement>();
      XElement formats = (from v in viewXml.Descendants() where v.Name.LocalName == "Formats" select v).FirstOrDefault<XElement>();
      return XUpdateView(listName, viewName, viewProperties, viewQuery, viewFields, aggregations, formats, rowLimit);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="listName">A string that contains the internal name of the list.</param>
    /// <param name="viewName">A string that contains the GUID for the view.</param>
    /// <param name="viewProperties">An XML fragment that contains all the view-level properties as attributes, such as Editor, Hidden, ReadOnly, and Title.</param>
    /// <param name="viewQuery"></param>
    /// <param name="viewFields"></param>
    /// <param name="aggregations">An Aggregations element that specifies the fields to aggregate and that can be assigned to a System.Xml.XmlNode object, as in the following example:
    /// <example>
    ///   <Aggregations Value="On">
    ///     <FieldRef Name="Title" Type="Count">
    ///     <FieldRef Name="Number" Type="Sum">
    ///   </Aggregations>
    /// </example>
    /// </param>
    /// <param name="formats">
    /// A Formats element that defines the grid formatting for columns and that can be assigned to a System.Xml.XmlNode object, as in the following example:
    /// <example>
    ///   <Formats>
    ///     <FormatDef Type="RowHeight" Value="67" />
    ///     <Format Name="Attachments">
    ///       <FormatDef Type="ColWidth" Value="75" />
    ///     </Format>
    ///     <Format Name="LinkTitle">
    ///       <FormatDef Type="WrapText" Value="1" />
    ///       <FormatDef Type="ColWidth" Value="236" />
    ///     </Format>
    ///     ...
    ///   </Formats>
    /// </example>
    /// </param>
    /// <param name="rowLimit"></param>
    /// <returns></returns>
    public XElement XUpdateView(string listName, string viewName, XElement viewProperties, XElement viewQuery, XElement viewFields, XElement aggregations, XElement formats, XElement rowLimit) {
      if (string.IsNullOrEmpty(listName))
        throw new ArgumentNullException("listName");
      if (string.IsNullOrEmpty(viewName))
        throw new ArgumentNullException("viewName");
      EnsureViewsCollection(listName);
      XmlNode results = this.WebService.UpdateView(
        listName, 
        viewName, 
        viewProperties.ToXmlNode(),
        viewQuery.ToXmlNode(), 
        viewFields.ToXmlNode(), 
        aggregations.ToXmlNode(),
        formats.ToXmlNode(), 
        rowLimit.ToXmlNode()
      );
      return results.ToXElement();
    }

    public XElement XEnsureViews(string listName, XElement views) {
      EnsureViewsCollection(listName);
      XElement results = new XElement("Views");
      List<XElement> viewsList = (from v in views.Descendants() where v.Name.LocalName == "View" select v).ToList<XElement>();
      foreach (XElement newOrUpdatedView in viewsList) {
        //string title = view.TryGetAttributeValue("DisplayName", string.Empty);
        string url = newOrUpdatedView.TryGetAttributeValue("Url", string.Empty);
        XElement existingView = (from v in this.Views.Descendants()
                            where v.Name.LocalName == "View"
                            && v.TryGetAttributeValue("Url", string.Empty) == url // || v.TryGetAttributeValue("DisplayName", string.Empty) == title
                            select v).FirstOrDefault<XElement>();
        XElement result = null;
        try {
          if (existingView != null) {
            string existingViewName = existingView.TryGetAttributeValue("Name", string.Empty);
            string newViewName = newOrUpdatedView.TryGetAttributeValue("Name", string.Empty);
            if (string.Equals(newViewName, existingViewName, StringComparison.InvariantCultureIgnoreCase))
              newViewName = string.Empty;
            newOrUpdatedView.Attribute("Name").SetValue(existingViewName);
            result = this.XUpdateView(listName, existingViewName, newOrUpdatedView);
            result.Add(new XAttribute("ResultAction", "created"));
            if (!string.IsNullOrEmpty(newViewName))
              result.Add(new XAttribute("NameChangedFrom", newViewName));
          } else {
            result = this.XCreateView(listName, newOrUpdatedView);
            result.Add(new XAttribute("ResultAction", "updated"));
          }
        } catch (Exception ex) {
          XElement errXml = new XElement("ViewError");
          XElement e = new XElement("ExceptionDetails");
          e.Add(new XElement("Message") { Value = ex.Message });
          e.Add(new XElement("StackTrace") { Value = ex.StackTrace });
          errXml.Add(e);
          if (existingView != null) {
            XElement e1 = new XElement("ExistingView");
            errXml.Add(e1);
            e1.Add(existingView);
          }
          if (newOrUpdatedView != null) {
            XElement e2 = new XElement("NewOrUpdatedView");
            errXml.Add(e2);
            e2.Add(newOrUpdatedView);
          }
          if (results != null) {
            XElement e3 = new XElement("Results");
            errXml.Add(e3);
            e3.Add(results);
          }
          results.Add(errXml);
        }
        if (result != null)
          results.Add(result);
      }
      return results;
    }

    internal bool XDeleteAllViews(string listName) {
      EnsureViewsCollection(listName);
      List<string> viewNames = (from v in this.Views.Descendants()
                                where v.Name.LocalName == "View"
                                select v.TryGetAttributeValue("Name", string.Empty)).ToList<string>();
      foreach (string viewName in viewNames) {
        try {
          this.WebService.DeleteView(listName, viewName);
        } catch (Exception) {
          return false;
        }
      }
      return true;
    }

    internal XElement XDeleteAllAndCreateViews(string listName, XElement views) {
      EnsureViewsCollection(listName);
      XElement results = new XElement("Views");
      // step 1: delete all the existing views
      bool success = XDeleteAllViews(listName);
      if (!success)
        return results;
      // step 2: create new views based on the provided XML
      List<XElement> viewsList = (from v in views.Descendants() where v.Name.LocalName == "View" select v).ToList<XElement>();
      foreach (XElement view in viewsList) {
        XElement result = XCreateView(listName, view);
        results.Add(result);
      }
      return results;
    }

  }

}
