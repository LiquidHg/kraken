namespace Kraken.SharePoint.WebParts.ToolParts {

  using System;
  using System.Collections.Generic;
  using System.Data;
  using System.IO;
  using System.Linq;
  using System.Runtime.InteropServices;
  using System.Web.UI;
  using System.Web.UI.WebControls;
  using System.Web.UI.WebControls.WebParts;
  using System.Text;

  using Microsoft.SharePoint;
  using Microsoft.SharePoint.WebControls;
  using Microsoft.SharePoint.WebPartPages;

  using spwp = Microsoft.SharePoint.WebPartPages;

  using Kraken.SharePoint;
  using Kraken.SharePoint.Logging;

  public class ListItemMultiPickerToolPart : CheckBoxListToolPart, IRequiredPropertiesWebPart {

    /// <summary>
    /// The fieldName name for target list that will be used for the default selection state value for the item when used in dropdowns.
    /// </summary>
    public string DefaultSelectedFieldName { get; set; }

    public ListDataHelper ListDataHandler = new ListDataHelper();

    /// <summary>
    /// Initializes a new instance of the <see cref="ListItemMultiPickerToolPart"/> class.
    /// </summary>
    public ListItemMultiPickerToolPart()
      : base() {
      this.Title = "Select List Items";
    }

    protected override void Flex_PreRender(EventArgs e) {
      if (this.choicesCheckBoxList.Items.Count == 0) {
        Notifications.AddInfo("The target list has no items. Add at least one item to activate and use this property.");
      }
      base.Flex_PreRender(e);
    }

    #region Hooks Into Web Part

    protected override bool LoadWebPartPropertyValue() {
      base.LoadWebPartPropertyValue();
      if (!this.loadedState)
        throw new Exception(string.Format("Could not get value of '{0}' from the parent WWebPart.", this.WebPartPropertyName));
      return this.loadedState;
    }

    #endregion

    #region Data Population

    public bool VerboseMode { get; set; }

    private List<ListItem> GetListItemsFromTargetList() {
      List<ListItem> choices = new List<ListItem>();
      Func<SPList, object[], object> listAction = delegate(SPList list, object[] args) {
        SPListItemCollection items = this.ListDataHandler.GetListItems(list);
        foreach (SPListItem item in items) {
          AddListItemFromTargetList(choices, item);
        }
        return null; // items;
      };
      try {
        object result = this.ListDataHandler.ListFunc(listAction, null); // as List<ListItem>;
      } catch (ArgumentNullException ex) {
        // We can't do this yet, because we don't have a setting in another ToolPart
        StringBuilder sb = new StringBuilder();
        sb.Append("The Target List for this tool part has not been specified. ");
        if (this.VerboseMode) { // TODO verbose mode
          sb.Append("Cause: ");
          sb.Append(ex.Message);
        }
        Notifications.AddWarning(sb.ToString());
        this.Abort = true;
      }
      return choices;
    }

    private void AddListItemFromTargetList(List<ListItem> choices, SPListItem currentItem) {
      if (currentItem == null)
        return;
      string currentItemTitle, currentItemId;
      bool success = true;
      success &= currentItem.TryGetValueAsString(this.ListDataHandler.TextFieldName, out currentItemTitle);
      success &= currentItem.TryGetValueAsString(this.ListDataHandler.ValueFieldName, out currentItemId);
      if (!success)
        currentItemTitle = "ERROR: " + currentItemTitle; 
      ListItem currentListItem = new ListItem(currentItemTitle, currentItemId);

      // here we percolate the value from the list item's DefaultSelectedFieldName
      // if it was specified
      bool defaultSelected = this.DefaultSelected;
      if (!string.IsNullOrEmpty(this.ListDataHandler.DefaultSelectedFieldName)) {
        currentItem.TryGetValue(this.ListDataHandler.DefaultSelectedFieldName, out defaultSelected);
      }
      currentListItem.Selected = defaultSelected;

      choices.Add(currentListItem);
    }

    protected override void PopulateDataTable(DataTable dt, bool clear) {
      List<ListItem> items = GetListItemsFromTargetList();
      base.PopulateDataTable(dt, clear);
      if (items.Count == 0 && this.LoadSampleData)
        return;
      foreach (ListItem item in items) {
        DataRow dr = dt.NewRow();
        dr[ListDataHelper.BasicDataTable_ValueField] = item.Value;
        dr[ListDataHelper.BasicDataTable_TextField] = item.Text;
        dr[ListDataHelper.BasicDataTable_DefaultSelectedField] = item.Selected;
        dt.Rows.Add(dr);
      }
    }

    #endregion

    #region IRequiredPropertiesWebPart

    public override bool RequiredPropertiesSet {
      get {
        if (string.IsNullOrEmpty(this.ListDataHandler.TargetListNameOrUrl))
          return false;
        return base.RequiredPropertiesSet;
      }
    }

    public override void RenderRequiredPropertiesMessage(TextWriter writer, bool script, bool div) {
      string moreInfo = "You must specify a value for WebPartPropertyName and ListDataHandler.TargetListNameOrUrl in the ToolPart.";
      ToolPane tp = this.ParentToolPane;
      spwp.WebPart wp = tp.SelectedWebPart;
      wp.RenderRequiredPropertiesMessage(writer, moreInfo, script, div);
    }

    #endregion

  } // class
} // namespace

