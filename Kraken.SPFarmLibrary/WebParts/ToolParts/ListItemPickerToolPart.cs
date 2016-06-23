
namespace Kraken.SharePoint.WebParts.ToolParts {

  using System;
  using System.Collections.Generic;
  using System.Data;
  using System.Diagnostics;
  using System.IO;
  using System.Runtime.InteropServices;
  using System.Web.UI;
  using System.Web.UI.WebControls;
  using System.Web.UI.WebControls.WebParts;
  using System.Text;

  using Microsoft.SharePoint;
  using Microsoft.SharePoint.Utilities;
  using Microsoft.SharePoint.WebControls;
  using Microsoft.SharePoint.WebPartPages;

  using Kraken.SharePoint;

  using aspwp = System.Web.UI.WebControls.WebParts;
  using spwp = Microsoft.SharePoint.WebPartPages;

  public class ListItemPickerToolPart : FlexBaseToolPart {

    /// <summary>
    /// Holds the data for the items that will be displayed in the checkbox list
    /// </summary>
    protected DataTable dt;

    protected DropDownList choicesDropDownList = new DropDownList();

    #region Public Tool Part Properties

    public ListDataHelper ListDataHandler = new ListDataHelper();

    public bool LoadSampleData { get; set; }

    /// <summary>
    /// If true, the dropdown will display a choice at the top allowing users to select None as an option.
    /// </summary>
    public bool AddNoSelectionChoice { get; set; }

    /// <summary>
    /// If enabled, the text that will appear for the 'None' choice at the top of the dropdown.
    /// </summary>
    public string NoneChoiceText { get; set; }

    /// <summary>
    /// If enabled, the value set for the 'None' choice at the top of the dropdown.
    /// </summary>
    public object NoneChoiceValue { get; set; }

    /// <summary>
    /// String value of the selected dropdown list item.
    /// This will be passed to the parent web control by the event model.
    /// </summary>
    public string SelectedItemValue {
      get { return (string)(this.webPartPropertyValue ?? string.Empty); }
      set {
        this.webPartPropertyValue = value;
        //UpdateSelectedItems();
      }
    }
    
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ListItemPickerToolPart"/> class.
    /// </summary>
    public ListItemPickerToolPart() {
      this.Title = "Select List Item";
      this.ChromeState = aspwp.PartChromeState.Minimized;
      this.WebPartPropertyName = "SelectedItemValue";
      this.AddNoSelectionChoice = true;
      this.NoneChoiceText = "None";
      this.LoadSampleData = false;
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
      if (!loaded) {
        // sample data if needed
        if (LoadSampleData)
          this.SelectedItemValue = "test2";
        else
          throw new Exception(string.Format("Could not get value of '{0}' from the parent WWebPart.", this.WebPartPropertyName));
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

    protected override void LoadControlState() {
      //EnsureChildControls();
      // init and populate the data table and bind it to control
      if (this.dt == null)
        this.dt = ListDataHelper.CreateBasicDataTable();
      PopulateDataTable(dt, true);
      ListDataHelper.DataBindListControlToDataTable(this.dt, this.choicesDropDownList, false);
      // set the selected items
      foreach (ListItem choice in choicesDropDownList.Items) {
        choice.Selected = (string.Compare(choice.Value, this.SelectedItemValue, true) == 0);
      }
      /*
      foreach (ListItem li in this.choicesDropDownList.Items) {
        li.Selected = false;
      }
      ListItem liv = this.choicesDropDownList.Items.FindByValue(this.SelectedItemValue);
      if (liv != null)
        liv.Selected = true;
       */
    }

    /// <summary>
    /// Converts the checkbox selections into a set of name/value pairs for storage in a single string.
    /// </summary>
    /// <remarks>
    /// For performance optimziation, this method bypasses the call to
    /// public accessor <see cref="SelectedItemsAsConcatinatedString"/>.
    /// </remarks>
    protected override object SaveControlState() {
      this.webPartPropertyValue = this.choicesDropDownList.SelectedValue;
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
      if (this.choicesDropDownList == null) {
        this.choicesDropDownList = new DropDownList();
        this.choicesDropDownList.ID = this.ID + "_DropDownList1";
        this.choicesDropDownList.EnableViewState = true;
        this.Controls.Add(this.choicesDropDownList);
      }
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
      //this.choicesDropDownList.RenderControl(writer);
      writer.Write("<input name='{0}' type='hidden' value='{1}' />", "SelectedItemValue", SPEncode.HtmlEncode(this.SelectedItemValue));
    }

    #region Data Population

    private List<ListItem> GetListItemsFromTargetList() {
      //if (TargetSite == null)
      //  throw new ArgumentNullException("_currentSite", "You must set the current web context before calling this method.");
      //SPList targetList = TryAndGetTargetList(targetWeb);
      //if (targetList == null)
      //  throw new ArgumentNullException("targetList", string.Format("Could not reteive target list names {0} from web {1}.", this.TargetListName, targetWeb.Url));
      Func<SPList, object[], object> listAction = delegate(SPList list, object[] args) {
        List<ListItem> choices = new List<ListItem>();
        if (this.AddNoSelectionChoice) {
          ListItem emptyCategoryListItem = new ListItem(this.NoneChoiceText, this.NoneChoiceValue.ToString());
          choices.Add(emptyCategoryListItem);
        }
        SPListItemCollection items = this.ListDataHandler.GetListItems(list);
        foreach (SPListItem item in items) {
          AddListItemFromTargetList(choices, item);
        }
        return choices;
      };
      return this.ListDataHandler.ListFunc(listAction, null) as List<ListItem>;
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
      choices.Add(currentListItem);
    }
    
    /// <summary>
    /// Developers should override this method in order to populate the data table
    /// with value/text pairs that will be displayed by the control.
    /// </summary>
    /// <param name="dt"></param>
    protected virtual void PopulateDataTable(DataTable dt, bool clear) {
      List<ListItem> items;
      if (this.LoadSampleData) {
        items = new List<ListItem>();
        for (int i = 1; i <= 5; i++) {
          items.Add(new ListItem("Test " + i, "test" + i));
        }
      } else
        items = GetListItemsFromTargetList();
      ListDataHelper.PopulateDataTable(dt, items, clear);
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

    /*
    private void ApplyWebPartToEditPropertyValue() {
      Type t = this.WebPartToEdit.GetWebPartPropertyType(this.WebPartPropertyName);
      object o = null;
      if (choicesDropDownList.SelectedValue != null && !choicesDropDownList.SelectedValue.Equals(this.NoneChoiceValue.ToString())) {
        bool success = Parser.TryParse(choicesDropDownList.SelectedValue, t, ParseFlags.Simple | ParseFlags.Invoke, out o);
        if (!success)
          throw new Exception(string.Format("Failed to parse dropdown value '{0}' as web part property {1} of type {2}; possible type mismatch.", choicesDropDownList.SelectedValue, this.WebPartPropertyName, t.FullName.ToString()));
      }
      this._webPartToEditPropertyValue = o;
      this.WebPartToEdit.SetWebPartProperty(this.WebPartPropertyName, this._webPartToEditPropertyValue);
    }
     */

  } // class
} // namespace

