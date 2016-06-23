namespace Kraken.SharePoint.WebParts.ToolParts {

  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Data;
  using System.Linq;
  using System.Text;
  using System.Web;
  using System.Web.UI;
  using System.Web.UI.WebControls;
  using aspwp = System.Web.UI.WebControls.WebParts;

  using Microsoft.SharePoint;
  using Microsoft.SharePoint.Utilities;
  using Microsoft.SharePoint.WebControls;
  using Microsoft.SharePoint.WebPartPages;

  /// <summary>
  /// Description of the toolpart. Override the GetToolParts method in your WebPart
  /// class to invoke this toolpart. To establish a reference to the Web Part 
  /// the user has selected, use the ParentToolPane.SelectedWebPart property.
  /// </summary>
  public class CheckBoxListToolPart : FlexBaseToolPart {

    #region Properties

    /// <summary>
    /// Set to true if you want to load the control with some
    /// sample data for testing purposes.
    /// </summary>
    public bool LoadSampleData = false;

    /// <summary>
    /// For items that appear in the DataTable that were not there previously
    /// or items where the parser fails, this is the default setting for item.Selected.
    /// </summary>
    public bool DefaultSelected = true;

    /// <summary>
    /// Contaisn the checkbox list
    /// </summary>
    protected CheckBoxList choicesCheckBoxList;

    /// <summary>
    /// Holds the data for the items that will be displayed in the checkbox list
    /// </summary>
    protected DataTable dt;

    /// <summary>
    /// Fully concatinated string value of all the checkboxesm as in SelectedItems.
    /// This will be passed to the parent web control by the event model.
    /// </summary>
    /// <remarks>
    /// Setting this property resets SelectedItems so they are in sync.
    /// Note that it does not work the other way around though.
    /// </remarks>
    public string SelectedItemsAsConcatinatedString {
      get { return (string)(this.webPartPropertyValue ?? string.Empty); }
      set { 
        this.webPartPropertyValue = value;
        // erases the current collection of items
        this.selectedItems = GenerateSelectedItems(null, this.SelectedItemsAsConcatinatedString, this.DefaultSelected);
      }
    }

    /// <summary>
    /// A collection of name/value pairs as stored in SelectedItemsAsConcatinatedString.
    /// </summary>
    /// <remarks>
    /// These are kept in sync by SaveControlState(), so if you need to change them
    /// in code you will have to manage the sync also.
    /// </remarks>
    public Dictionary<string, bool> SelectedItems {
      get {
        if (selectedItems == null)
          selectedItems = new Dictionary<string,bool>();
        return selectedItems;
      }
    }
    private Dictionary<string, bool> selectedItems;

    #endregion

    /// <summary>
    /// Constructor for the class.
    /// </summary>
    public CheckBoxListToolPart() {
      this.Title = "Check Box List Selection";
      this.ChromeState = aspwp.PartChromeState.Minimized;
      this.WebPartPropertyName = "SelectedItemsAsConcatinatedString";
    }

    #region Hooks Into WebPart

    /// <summary>
    /// Developers should override this method in order to populate the string
    /// that contains selected values. In general, this string is pulled in from
    /// a web part property that is stored in the parent web part. It should have
    /// the format: value1=true;value2=false;
    /// </summary>
    protected override bool LoadWebPartPropertyValue() {
      bool loaded = base.LoadWebPartPropertyValue();
      if (loaded && !string.IsNullOrEmpty(this.SelectedItemsAsConcatinatedString)) {
        // because the above was done by the base class, we need to build the collection now
        this.selectedItems = GenerateSelectedItems(null, this.SelectedItemsAsConcatinatedString, this.DefaultSelected);
      } else if (LoadSampleData && string.IsNullOrEmpty(this.SelectedItemsAsConcatinatedString)) {
        // sample data if needed
        this.SelectedItemsAsConcatinatedString = "test1=true;test2=false;test3=true;";
      }
      return loaded;
    }

    /*
    /// <summary>
    /// Use this method to copy data back up into the parent web part.
    /// </summary>
    protected override void SaveWebPartPropertyValue() {
      base.SaveWebPartPropertyValue();
      // there is currently nothing to do here that is not already handled by the control state save
    }
    */
    public static string GenerateConcatString(Dictionary<string, bool> selectedItems) {
      StringBuilder sb = new StringBuilder();
      foreach (string key in selectedItems.Keys) {
        sb.AppendFormat("{0}={1};", key, selectedItems[key]);
      }
      return sb.ToString();
    }
    public static Dictionary<string, bool> GenerateSelectedItems(Dictionary<string, bool> items, string selectedItemsAsConcatinatedString, bool defaultValue) {
      if (items == null)
        items = new Dictionary<string, bool>();
      if (string.IsNullOrEmpty(selectedItemsAsConcatinatedString))
        return items;
      // parse into a set of value=bool
      string[] sarray = selectedItemsAsConcatinatedString.Split(';');
      foreach (string pair in sarray) {
        if (!string.IsNullOrEmpty(pair)) {
          string[] vs = pair.Split('=');
          if (vs.GetLength(0) == 2) {
            string value = vs[0];
            bool selected = defaultValue;
            bool.TryParse(vs[1], out selected);
            items.Add(value, selected);
          }
        }
      }
      return items;
    }

    protected override void LoadControlState() {
      base.LoadControlState();
      // init and populate the data table and bind it to control
      if (this.dt == null)
        this.dt = ListDataHelper.CreateBasicDataTable();
      PopulateDataTable(dt, true);
      ListDataHelper.DataBindListControlToDataTable(this.dt, this.choicesCheckBoxList, this.DefaultSelected);
      // set the selected items
      foreach (ListItem li in this.choicesCheckBoxList.Items) {
        string key = li.Value;
        if (this.SelectedItems.ContainsKey(key))
          li.Selected = this.SelectedItems[key];
        // This was commented out, because now that DataBindListControlToDataTable
        // handles item level defaults, we have to set the global default there.
        //else
        //  li.Selected = this.DefaultSelected;
      }
    }

    /// <summary>
    /// Converts the checkbox selections into a set of name/value pairs for storage in a single string.
    /// </summary>
    /// <remarks>
    /// For performance optimziation, this method bypasses the call to
    /// public accessor <see cref="SelectedItemsAsConcatinatedString"/>.
    /// </remarks>
    protected override object SaveControlState() {
      base.SaveControlState();
      if (this.choicesCheckBoxList == null)
        return null; // Somehow we have gotten here before the control has been loaded
      // erase the current collection of items
      if (this.selectedItems != null)
        this.selectedItems = null;
      StringBuilder sb = new StringBuilder();
      foreach (ListItem item in this.choicesCheckBoxList.Items) {
        this.SelectedItems.Add(item.Value, item.Selected);
        sb.AppendFormat("{0}={1};", item.Value, item.Selected);
      }
      // use the private field so as not to trigger collection rebuild
      this.webPartPropertyValue = sb.ToString();

      base.SaveControlState();
      return this;
    }

    #endregion

    //public override void Flex_ApplyChanges() {
    //}
    //public override void Flex_SyncChanges() {
    //}
    //public override void Flex_CancelChanges() {
    //}

    /// <summary>
    /// Creates the CheckBoxList and populates it with the desired values.
    /// Note that this.SelectedItemsAsConcatinatedString must be set at this point.
    /// </summary>
    protected override void Flex_CreateChildControls() {
      base.Flex_CreateChildControls();
      // Create the CheckBoxList to manage the profile properties selection
      if (choicesCheckBoxList == null) {
        choicesCheckBoxList = new CheckBoxList();
        choicesCheckBoxList.ID = this.ID + "_CheckBoxList1";
        choicesCheckBoxList.EnableViewState = true;
      }
      if (!this.Controls.Contains(choicesCheckBoxList))
        this.Controls.Add(choicesCheckBoxList);
    }

    /// <summary>
    /// Render the controls for this tool part.
    /// </summary>
    /// <remarks>
    /// Notifications and exception handling are handled by Render and Flex_Render.
    /// </remarks>
    /// <param name="writer">The output stream / HTML writer to write into.</param>
    protected override void Flex_Render(HtmlTextWriter writer) {
      base.Flex_Render(writer);
      // This was commented because the base web part is already rendering the ListControl
      // choicesCheckBoxList.RenderControl(writer);
      writer.Write("<input name='{0}' type='hidden' value='{1}' />", "SelectedItemsAsConcatinatedString", SPEncode.HtmlEncode(SelectedItemsAsConcatinatedString));
    }

    #region DataTable Helpers

    /// <summary>
    /// Developers should override this method in order to populate the data table
    /// with value/text pairs that will be displayed by the control.
    /// </summary>
    /// <param name="dt"></param>
    protected virtual void PopulateDataTable(DataTable dt, bool clear) {
      List<ListItem> items = new List<ListItem>();
      if (this.LoadSampleData) {
        for (int i = 1; i <= 5; i++) {
          items.Add(new ListItem("Test " + i, "test" + i));
        }
      }
      ListDataHelper.PopulateDataTable(dt, items, clear);
    }

    #endregion

  } // class

} // namespace