namespace Microsoft.SharePoint.Client {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using Kraken.SharePoint.Client;
  using Kraken.Tracing;

  public static class KrakenViewExtensions {

    /// <summary>
    /// Compare the fields in newViewFields and perform
    /// add/remove as needed for view.ViewFields.
    /// </summary>
    /// <param name="view"></param>
    /// <param name="newViewFields"></param>
    public static void SyncViewFields(this View view, string[] newViewFields) {
      if (newViewFields != null) {
        List<string> fieldsToRemove = new List<string>();
        foreach (string fieldName in view.ViewFields) {
          if (!newViewFields.Contains(fieldName))
            fieldsToRemove.Add(fieldName);
        }
        foreach (string fieldName in newViewFields) {
          if (!view.ViewFields.Contains(fieldName))
            view.ViewFields.Add(fieldName);
        }
        foreach (string fieldName in fieldsToRemove) {
          view.ViewFields.Remove(fieldName);
        }
      }
    }

    /// <summary>
    /// Create a view in a list using standard options
    /// </summary>
    /// <param name="targetList">target list in which to create the view</param>
    /// <param name="title">Name of the view; we do not have control over the ASPX name</param>
    /// <param name="queryXml">
    /// Caml query XML; should have: '&lt;VIEW&gt;&lt;QUERY&gt;&lt;WHERE&gt;&lt;/WHERE&gt;&lt;ORDERBY&gt;&lt;/ORDERBY&gt;&lt;/QUERY&gt;&lt;/VIEW&gt;' structure 
    /// </param>
    /// <param name="viewFields">An array of field names the view will return</param>
    /// <param name="isPersonalView">True for private view, false for public view</param>
    /// <param name="makeDefaultView">Make the view the default view for the list</param>
    /// <param name="rowLimit">Limit the view row results</param>
    /// <param name="isPaged">If true, rowLimit will return items in pages of that size</param>
    /// <returns>The newly created CSOM View object</returns>
    public static View CreateStandardListView(List targetList,
              string title, string queryXml, string[] viewFields, bool isPersonalView, bool makeDefaultView, uint rowLimit, 
              bool isPaged) {
      ClientContext context = (ClientContext)targetList.Context;

      context.Load(targetList.Views);
      context.ExecuteQuery();
      ViewCreationInformation vci = new ViewCreationInformation();
      vci.Title = title;
      if (rowLimit > 0 && rowLimit <= 5000)
        vci.RowLimit = rowLimit;
      vci.PersonalView = isPersonalView;
      vci.SetAsDefaultView = makeDefaultView && !isPersonalView;
      vci.ViewTypeKind = ViewType.None;
      if (viewFields != null && viewFields.Length > 0)
        vci.ViewFields = viewFields;
      // query should have: 
      //  <VIEW><QUERY>
      //    <WHERE></WHERE>
      //    <ORDERBY></ORDERBY>
      // </QUERY></VIEW> structure 
      if (!string.IsNullOrEmpty(queryXml))
        vci.Query = queryXml;
      vci.Paged = isPaged;
      // SET EVERYTHING UP BEFORE ADDING THE NEW VIEW!!!
      View newView = targetList.Views.Add(vci);
      //context.Load(targetList.Views);
      context.ExecuteQuery();
      return newView;
    }

    /// <summary>
    /// Creates a duplicate of a list view
    /// </summary>
    /// <param name="sourceView">The view that will be duplicated</param>
    /// <param name="targetList">The target list in which to create the copied view</param>
    /// <param name="newViewName">The name to give to the new view, or empty string to use the same name as the title of the source view</param>
    /// <param name="makeDefault">If true, set the copied view as the default view for the target list</param>
    /// <param name="overwrite">If true, overwrite any existing view with the same name</param>
    /// <param name="limitToContentType">Specify to restrict the newly copied view to only appear within a certain content type</param>
    /// <returns></returns>
    public static View Copy(this View sourceView, List targetList,
        string newViewName, bool makeDefault, bool overwrite, ContentTypeId limitToContentType) {

      ClientContext sourceContext = (ClientContext)sourceView.Context;
      ClientContext targetContext = (ClientContext)targetList.Context;

      targetContext.Load(targetList.Views);
      targetContext.ExecuteQuery();

      sourceContext.Load(sourceView);
      sourceContext.Load(sourceView.ViewFields);
      sourceContext.ExecuteQuery();
      //clientContext.Load(existingView.Scope); // not needed? not declared yet

      if (string.IsNullOrEmpty(newViewName)) {
        newViewName = sourceView.Title;
      }

      View existingView = null;
      try {
        existingView = targetList.Views.GetByTitle(newViewName);
        targetContext.Load(existingView);
        targetContext.ExecuteQuery();
      } catch {
        existingView = null;
      }
      if (existingView != null && !overwrite) {
        throw new InvalidOperationException("View with that name already exists [" + newViewName + "]. " +
            "Set Overwrite to $true (if not default view) or choose a different name. ");
      } else if (existingView != null && overwrite) {
        if (existingView.DefaultView) {
          throw new InvalidOperationException("View with that name already exists [" + newViewName + "] and is DEFAULT. " +
              "Cannot edit or delete default view.  Change the default view and try again. ");
        }
        existingView.DeleteObject();
        //clientContext.Load(existingView);
        targetContext.Load(targetList.Views);
        targetContext.ExecuteQuery();
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        do {
          if (sw.ElapsedMilliseconds > 500) break; // don't allow infinite loop
        } while (true);
      }
      string[] viewFields = sourceView.ViewFields.ToArray();
      ViewCreationInformation vci = new ViewCreationInformation();
      vci.Title = newViewName;
      vci.ViewTypeKind = (ViewType)Enum.Parse(typeof(ViewType), sourceView.ViewType, true);
      vci.RowLimit = sourceView.RowLimit;
      vci.Query = sourceView.ViewQuery;
      vci.ViewFields = viewFields;
      vci.SetAsDefaultView = makeDefault;
      vci.PersonalView = false;
      View newView = targetList.Views.Add(vci);
      targetContext.Load(newView);
      targetContext.ExecuteQuery();
      newView.Aggregations = sourceView.Aggregations;
      newView.AggregationsStatus = sourceView.AggregationsStatus;
      //newView.BaseViewId = existingView.BaseViewId; // read only
      //newView.EditorModified = newView.EditorModified; //causes error!
      newView.Formats = sourceView.Formats;
      newView.Hidden = sourceView.Hidden;
      newView.Method = sourceView.Method;
      newView.MobileView = sourceView.MobileView;
      newView.MobileDefaultView = sourceView.MobileDefaultView;
      newView.IncludeRootFolder = sourceView.IncludeRootFolder;
      newView.DefaultViewForContentType = sourceView.DefaultViewForContentType;
      //IMPORTANT!  In order to use the content type feature, the content type of the Destination List must be used.
      // Meaning, find the content type in the old list, find it's "parent" in the Web, then find the "child" in
      // the destination list.  This is assumed to be done by the caller.
      if (limitToContentType != null)
        newView.ContentTypeId = limitToContentType;
      newView.Scope = sourceView.Scope;
      //newView.StyleId = existingView.StyleId; //read only
      newView.ViewData = sourceView.ViewData;
      //theNewView.ViewFields = existingView.ViewFields; //read only
      newView.ViewJoins = sourceView.ViewJoins;
      newView.ViewProjectedFields = sourceView.ViewProjectedFields;
      newView.ViewQuery = sourceView.ViewQuery;
      //newView.ViewType = existingView.ViewType; // read only
      //newView.ModerationType = existingView.ModerationType; //read only
      //newView.HtmlSchemaXml = existingView.HtmlSchemaXml; //read only
      newView.Update();
      targetContext.ExecuteQuery();
      return newView;
    }

    /// <summary>
    /// Updates a View based on ViewProperties.
    /// Includes some property validation.
    /// </summary>
    /// <param name="view"></param>
    /// <param name="props"></param>
    /// <param name="skipCreateProperties"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    public static bool Update(this View view, ViewProperties props, bool skipCreateProperties, string listTitle, ITrace trace = null) {
      if (trace == null) trace = DiagTrace.Default;
      if (props.Validate(trace))
        return false;
      bool updateNeeded = false;

      if (!skipCreateProperties) {
        if (updateNeeded |= (!string.IsNullOrEmpty(props.Title) && props.Title != view.Title))
          view.Title = props.Title;
        if (updateNeeded |= (!string.IsNullOrEmpty(props.Query) && props.Query != ViewProperties.SKIP_PROPERTY && view.ViewQuery != props.Query))
          view.ViewQuery = props.Query;
        if (updateNeeded |= props.Paged != view.Paged)
          view.Paged = props.Paged;
        // TODO extend SyncViewFields to return whether it did anything tot eh view
        if (updateNeeded |= (props.ViewFields != null))
          view.SyncViewFields(props.ViewFields); // TODO check to make sure this is OK
        if (updateNeeded |= (view.RowLimit != props.RowLimit && props.RowLimit > 0))
          view.RowLimit = props.RowLimit;
        if (props.SetAsDefaultView) {
          if (view.DefaultView == true)
            trace.TraceVerbose("View '{0}' is already default in List '{1}'. Operation skipped.", view.Title, listTitle);
          else {
            trace.TraceVerbose("View '{0}' set as default from List '{1}'.", view.Title, listTitle);
            view.DefaultView = true;
            updateNeeded = true;
          }
        }
      }
      if (props.HasExtendedSettings) {
        if (updateNeeded |= props.JSLink != ViewProperties.SKIP_PROPERTY)
          view.JSLink = props.JSLink;
        if (updateNeeded |= props.TabularView.HasValue)
          view.TabularView = props.TabularView.Value;
        //Toolbar
        //ToolbarTemplateName
        //view.VisualizationInfo
      }
      if (updateNeeded) {
        try {
          view.Update();
          view.Context.ExecuteQueryIfNeeded();
          return true;
        } catch (Exception ex) {
          trace.TraceWarning("Unable to update view.");
          trace.TraceError(ex);
          return false;
        }
      } else
        return false;
    }
  }

}
