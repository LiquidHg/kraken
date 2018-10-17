using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using wp = System.Web.UI.WebControls.WebParts;
using com = System.ComponentModel;

using Kraken.SharePoint;
using Kraken.SharePoint.WebParts;
using System.Collections.Specialized;
using Microsoft.SharePoint;
using System.Web.UI;
using System.IO;
using System.Data;
using Kraken.SharePoint.WebParts.Cloud.ToolParts;
using System.Web.UI.WebControls.WebParts;

namespace Kraken.SharePoint.WebParts.Cloud {

  /// <summary>
  /// A SandboxWebPart with the base fields and methods needed to display data based on a single SharePoint list as a data source.
  /// </summary>
  public class SandboxListDataWebPart : SandboxWebPart, IRequiredPropertiesWebPart {

    public SandboxListDataWebPart() : base() {}

    #region Hidden Properties

    [wp.WebBrowsable(false),
    wp.Personalizable(wp.PersonalizationScope.Shared),
    wp.WebDisplayName("Target List Url"),
    wp.WebDescription("Root relative path to the List that will be used as the data source."),
    com.Category("Data Source")]
    public string TargetListURL {
      get {
        return targetListURL;
      }
      set {
        if (string.Equals(value, targetListURL, StringComparison.InvariantCultureIgnoreCase))
          return;
        targetListURL = value;
        // This break far too often.. :-(
        // new list, so reset the fields to prevent some errors
        //this.TargetListDisplayFields = DEFAULT_TargetListDisplayFields;
      }
    }
    private string targetListURL;

    #endregion

    /// <summary>
    /// Gets or sets the name of the view.
    /// </summary>
    /// <value>
    /// The name of the view.
    /// </value>
    [wp.WebBrowsable(true),
    wp.Personalizable(wp.PersonalizationScope.Shared),
    wp.WebDisplayName("List View Name"),
    wp.WebDescription("The name of the list view that represents the items to display."),
    com.Category("Data Source")]
    public string ListViewName { get; set; }

    /// <summary>
    /// Gets or sets the name of the text field.
    /// </summary>
    /// <value>
    /// The name of the text field.
    /// </value>
    [wp.WebBrowsable(true),
    wp.Personalizable(wp.PersonalizationScope.Shared),
    wp.WebDisplayName("Text Field"),
    wp.WebDescription("The name of the field from the list view that represents the items (text or html) to display."),
    com.Category("Data Source")]
    public string TextFieldName { get; set; }

    /// <summary>
    /// Gets or sets the name of the value field.
    /// </summary>
    /// <value>
    /// The name of the value field.
    /// </value>
    [wp.WebBrowsable(true),
    wp.Personalizable(wp.PersonalizationScope.Shared),
    wp.WebDisplayName("Value Field"),
    wp.WebDescription("Optional: The name of the field from the list view that represents a value corresponding to each displayed item."),
    com.Category("Data Source")]
    public string ValueFieldName { get; set; }

    #region List and Site Picker

    protected SiteAndListPickerToolPart TargetUrlPicker;

    public void SetTargetUrlFromEditorPart(object sender, EventArgs e) {
      SiteAndListPickerToolPart tp = sender as SiteAndListPickerToolPart; //FlexDotNetBaseToolPart
      if (tp == null || tp.WebPartPropertyValue == null)
        return;
      this.TargetListURL = tp.WebPartPropertyValue.ToString();
    }
    public void CopyTargetUrlToEditorPart(object sender, EventArgs e) {
      SiteAndListPickerToolPart tp = sender as SiteAndListPickerToolPart; //FlexDotNetBaseToolPart
      if (tp == null)
        return;
      tp.WebPartPropertyValue = this.TargetListURL;
    }

    public override EditorPartCollection CreateEditorParts() {
      // create and add the list/site picker editor part
      this.TargetUrlPicker = new SiteAndListPickerToolPart() {
        ID = "SiteAndListPicker"
      };
      this.TargetUrlPicker.Items.Add(new SiteAndListPickerItem("Target List URL", "TargetListURL", SiteAndListPickerType.List));
      if (this.IsSandboxWebPart) {
        this.TargetUrlPicker.LoadingWebPartPropertyValue += new EventHandler(CopyTargetUrlToEditorPart);
        this.TargetUrlPicker.SavingWebPartPropertyValue += new EventHandler(SetTargetUrlFromEditorPart);
      } else {
        // reflection based property get/set is not allowed in sandbox web parts
        this.TargetUrlPicker.WebPartPropertyName = "TargetListURL";
      }

      // Add other editor parts from parent web parts
      return new EditorPartCollection(base.CreateEditorParts(), new[] { this.TargetUrlPicker });
    }

    #endregion

    #region IRequiredPropertiesWebPart

    public virtual bool RequiredPropertiesSet {
      get {
        return (!(
          string.IsNullOrEmpty(this.TargetListURL) 
          ||string.IsNullOrEmpty(this.ListViewName) 
          || string.IsNullOrEmpty(this.TextFieldName)
        ));
      }
    }

    public virtual void RenderRequiredPropertiesMessage(TextWriter writer, bool script, bool div) {
      this.RenderRequiredPropertiesMessage(writer, "Required properties include the Target List URL, List View Name, and Text Field Name. ", script, div);
    }

    #endregion

    #region Server Side Data Handler

    protected ListDataHelper listDataHelper = new ListDataHelper();

    private void SetDataHelperProperties() {
      // Make sure the data handler has all the needed properties
      listDataHelper.TargetListNameOrUrl = this.TargetListURL;
      listDataHelper.TextFieldName = this.TextFieldName;
      listDataHelper.ValueFieldName = this.ValueFieldName;
      //listDataHelper.DefaultSelectedFieldName
    }

    /// <summary>
    /// Renders an individual SharePoint list item.
    /// Developers should override this function if they want to use RenderTargetList().
    /// </summary>
    /// <param name="item"></param>
    protected virtual void RenderListItem(HtmlTextWriter writer, SPListItem item) {
    }

    protected void RenderTargetList(HtmlTextWriter writer) {
      if (!this.RequiredPropertiesSet)
        return;
      SetDataHelperProperties();
      Func<SPList, object[], object> listAction = delegate(SPList list, object[] args) {
        SPListItemCollection items = listDataHelper.GetListItems(list);
        foreach (SPListItem item in items) {
          RenderListItem(writer, item);
        }
        return null;
      };
      try {
        object result = listDataHelper.ListFunc(listAction, null);
      } catch (ArgumentNullException ex) {
        // We can't do this yet, because we don't have a setting in another ToolPart
        StringBuilder sb = new StringBuilder();
        sb.Append("The Target List for this tool part has not been specified. ");
        Notifications.AddWarning(sb.ToString());
        Notifications.AddError(ex);
        this.Abort = true;
      }
    }

    /// <summary>
    /// Uses the SPListItems from listDataHelper to build a generic list
    /// </summary>
    /// <typeparam name="T">The type that will be created for the resulting generic list.</typeparam>
    /// <param name="createFromListItemFunc">A delegate that will populate a single item in the target list</param>
    /// <returns></returns>
    protected List<T> PopulateListFromTargetList<T>(
      Func<SPListItem, T> createFromListItemFunc
      ) where T: class {
      if (!this.RequiredPropertiesSet)
        return null;
      SetDataHelperProperties();
      List<T> anyList = new List<T>();
      Func<SPList, object[], object> listAction = delegate(SPList list, object[] args) {
        SPListItemCollection items = listDataHelper.GetListItems(list);
        foreach (SPListItem item in items) {
          T t = createFromListItemFunc(item);
          anyList.Add(t);
        }
        return null;
      };
      try {
        object result = listDataHelper.ListFunc(listAction, null);
      } catch (ArgumentNullException ex) {
        // We can't do this yet, because we don't have a setting in another ToolPart
        StringBuilder sb = new StringBuilder();
        sb.Append("The Target List for this tool part has not been specified. ");
        Notifications.AddWarning(sb.ToString());
        Notifications.AddError(ex);
        this.Abort = true;
      }
      return anyList;
    }

    protected DataTable GetDataTableFromListItemsInTargetList() {
      if (!this.RequiredPropertiesSet)
        return null;
      SetDataHelperProperties();
      DataTable dt = null;
      Func<SPList, object[], object> listAction = delegate(SPList list, object[] args) {
        SPListItemCollection items = listDataHelper.GetListItems(list);
        dt = ListDataHelper.CreateDataTable(items);
        return null;
      };
      try {
        object result = listDataHelper.ListFunc(listAction, null);
      } catch (ArgumentNullException ex) {
        // We can't do this yet, because we don't have a setting in another ToolPart
        StringBuilder sb = new StringBuilder();
        sb.Append("The Target List for this tool part has not been specified. ");
        Notifications.AddWarning(sb.ToString());
        Notifications.AddError(ex);
        this.Abort = true;
      }
      return dt;
    }

    #endregion

    /// <summary>
    /// Gets the debug info.
    /// </summary>
    /// <returns></returns>
    protected override NameValueCollection GetDebugInfo() {
      NameValueCollection debugInfo = base.GetDebugInfo();
      if (debugInfo == null)
        debugInfo = new NameValueCollection();
      debugInfo.Add("TargetListURL", this.TargetListURL);
      debugInfo.Add("ListViewName", this.ListViewName);
      debugInfo.Add("TextFieldName", this.TextFieldName);
      debugInfo.Add("ValueFieldName", this.ValueFieldName);
      return debugInfo;
    }

  }

}
