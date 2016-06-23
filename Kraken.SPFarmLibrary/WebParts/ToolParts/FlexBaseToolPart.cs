using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using Microsoft.SharePoint;
using Microsoft.SharePoint.WebPartPages;

using spwp=Microsoft.SharePoint.WebPartPages;

namespace Kraken.SharePoint.WebParts.ToolParts {

  public class FlexBaseToolPart : ToolPart, IRequiredPropertiesWebPart {

    public FlexBaseToolPart() {
      AbortOnException = false;
    }

    /// <summary>
    /// A temporary holding place for the value of the picker.
    /// This value will be loaded/saved to the parent web part 
    /// by LoadWebPartPropertyValue/SaveWebPartPropertyValue methods.
    /// </summary>
    protected object webPartPropertyValue = null;

    /// <summary>
    /// Flag indicates if the value of the ToolPart has been saved to the
    /// parent WebPart.
    /// </summary>
    protected bool savedState = false;

    protected bool loadedState = false;

    /// <summary>
    /// A bag that holds all the info, warnings, and errors
    /// associated with this control
    /// </summary>
    public NotificationBag Notifications {
      get { return notifications; } 
    }
    private NotificationBag notifications = new NotificationBag();

    //private ULSTraceLogging _log = new ULSTraceLogging();

    /// <summary>
    /// Flag which indicates that an error has occurred and future
    /// events and code should be cancelled
    /// </summary>
    public virtual bool Abort { get; set; }

    /// <summary>
    /// If true, execution of methods is stopped after there is an
    /// exception in the event lifecycle. If false, execution of
    /// subsequent methods is attempted anyway, which may result in
    /// multiple error messages but could be desired in some cases.
    /// </summary>
    /// <remarks>
    /// If this is False, Abort will never be set to True.
    /// </remarks>
    public virtual bool AbortOnException { get; set; }

    /// <summary>
    /// The name of the [parent] web part's public property that will be loaded/updated by this tool part.
    /// </summary>
    public string WebPartPropertyName { get; set; }

    /// <summary>
    /// Developers should override this method to include debug information for their web part.
    /// </summary>
    /// <returns></returns>
    protected virtual NameValueCollection GetDebugInfo() {
      // TODO use reflection to create a collection of all the (useful) properties
      return null;
    }

    #region Override Methods for Piping

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override Flex_Init instead.
    /// </summary>
    /// <param name="e"></param>
    protected override sealed void OnInit(EventArgs e) {
      AbortableToolPartEvent("OnInit", e, delegate(EventArgs e2) {
        Flex_Init(e2);
      });
    }

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override Flex_Load instead.
    /// </summary>
    /// <param name="e"></param>
    protected override sealed void OnLoad(EventArgs e) {
      AbortableToolPartEvent("OnLoad", e, delegate(EventArgs e2) {
        Flex_Load(e2);
      });
    }

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override Flex_CreateChildControls instead.
    /// </summary>
    protected override sealed void CreateChildControls() {
      AbortableToolPartEvent("CreateChildControls", null, delegate(EventArgs e2) {
        // developer custom child controls
        Flex_CreateChildControls();
        // Overwrite current this.webPartPropertyValue with value from the parent web part
        LoadWebPartPropertyValue();
        // Copy this.webPartPropertyValue into child control values
        LoadControlState();
      });
    }

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override Flex_ApplyChanges.
    /// </summary>
    public sealed override void ApplyChanges() {
      AbortableToolPartEvent("ApplyChanges", null, delegate(EventArgs e2) {
        if (!savedState) {
          // Copy child control values into this.webPartPropertyValue
          SaveControlState();
          // Save this.webPartPropertyValue to the parent web part
          SaveWebPartPropertyValue();
          // Any user defined code here - we have some defaults
          Flex_ApplyChanges();
          // TCC - do these two lines make sense in this model - 
          // or would it make more sense to use SyncChanges here???
          ChildControlsCreated = false;
          EnsureChildControls();
          // Tell the user what we did
          Notifications.AddInfo("Your changes have been applied.");
          // mark it down for later reference to prevent dupes
          savedState = true;
        }
      });
    }

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override Flex_SyncChanges.
    /// If the ApplyChanges method succeeds, this method is called by the ToolPane object
    /// to refresh the specified property values in the toolpart user interface.
    /// </summary>
    public sealed override void SyncChanges() {
      AbortableToolPartEvent("SyncChanges", null, delegate(EventArgs e2) {
        Flex_SyncChanges();
        // Sync with the new property changes here.
        //savedState = false;
      });
    }

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override Flex_CancelChanges.
    /// This method is called by the ToolPane object if the user discards changes to the selected Web Part. 
    /// </summary>
    public sealed override void CancelChanges() {
      AbortableToolPartEvent("CancelChanges", null, delegate(EventArgs e2) {
        // future calls should save state
        savedState = false;
        // some user customizations here
        Flex_CancelChanges();
        // Overwrite current this.webPartPropertyValue with value from the parent web part
        LoadWebPartPropertyValue();
        // Copy this.webPartPropertyValue into child control values
        LoadControlState();
        // Inform the user
        Notifications.AddInfo("Your changes have been discarded.");
      });
    }

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override Flex_PreRender.
    /// </summary>
    /// <param name="e"></param>
    protected override sealed void OnPreRender(EventArgs e) {
      AbortableToolPartEvent("OnPreRender", e, delegate(EventArgs e2) {
        // ensure that the Notification Bag will work properly
        this.WebPartToEdit.RegisterShowHideClientScript();
        // inform the user if the web part is missing needed properties
        if (!this.RequiredPropertiesSet) {
          StringBuilder sb = new StringBuilder();
          TextWriter writer = new StringWriter(sb);
          RenderRequiredPropertiesMessage(writer, false, false);
          Notifications.AddWarning(sb.ToString());
        }
        Flex_PreRender(e2);
      });
    }

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override Flex_Render instead.
    /// </summary>
    /// <param name="writer"></param>
    protected override sealed void RenderToolPart(HtmlTextWriter writer) {
      try {
        Notifications.Render(this.WebPartToEdit, writer, this.ID);
        if (!RequiredPropertiesSet || Abort)
          return;
        Flex_Render(writer);
      } catch (Exception ex) {
        // fail-safe error message
        writer.WriteException(ex, string.Format("Error in {0}::Render().", this.GetType().FullName), null);
      }
    }

    protected void AbortableToolPartEvent(string methodName, EventArgs e, Action<EventArgs> DoThis) {
      Abort = false;
      if (!Abort) {
        try {
          DoThis(e);
        } catch (Exception ex) {
          Notifications.Add(new WebPartNotification() {
            Level = TraceLevel.Error,
            Exception = ex,
            Message = string.Format("Something went wrong in {0}", methodName),
            DebugInfo = GetDebugInfo()
          });
          //Notifications.AddError(ex);
          if (AbortOnException)
            Abort = true;
        }
      }
    }

    #endregion

    #region Methods for Piping (to be overridden by developers in sub-classes)

    /// <summary>
    /// Developers should override this method with code that
    /// would normally go in OnInit.
    /// </summary>
    protected virtual void Flex_Init(EventArgs e) {
      base.OnInit(e);
    }

    /// <summary>
    /// Developers should override this method with code that
    /// would normally go in OnLoad.
    /// </summary>
    protected virtual void Flex_Load(EventArgs e) {
      base.OnLoad(e);
    }

    /// <summary>
    /// Developers should override this method with code that
    /// would normally go in CreateChildControls.
    /// </summary>
    /// <remarks>
    /// Ensure that whater controls you reference in LoadControlState()
    /// are created here.
    /// </remarks>
    protected virtual void Flex_CreateChildControls() {
      base.CreateChildControls();
    }

    protected virtual void Flex_ApplyChanges() {
      base.ApplyChanges();
      // Enable viewstate of child controls
      foreach (WebControl ctl in this.Controls) {
        ctl.EnableViewState = true;
      }
      this.SaveViewState();
    }

    protected virtual void Flex_SyncChanges() {
      base.SyncChanges();
    }

    protected virtual void Flex_CancelChanges() {
      base.CancelChanges();
    }

    /// <summary>
    /// Developers should override this method with code that
    /// would normally go in OnPreRender.
    /// </summary>
    protected virtual void Flex_PreRender(EventArgs e) {
      base.OnPreRender(e);
    }

    /// <summary>
    /// Developers should override this method with code that
    /// would normally go in Render.
    /// </summary>
    /// <param name="writer"></param>
    protected virtual void Flex_Render(HtmlTextWriter writer) {
      EnsureChildControls();
      base.RenderToolPart(writer);
    }

    #endregion

    #region WebPart to ToolPart communication and Control state load/save

    /// <summary>
    /// Developers should override this method in order to populate the property
    /// that contains selected values for the tool part. In general, this is a
    /// string from a property of the parent web part. For example, it could
    /// have the format: value1=true;value2=false;
    /// </summary>
    protected virtual bool LoadWebPartPropertyValue() {
      this.loadedState = false;
      if (!string.IsNullOrEmpty(this.WebPartPropertyName)) {
        ToolPane tp = this.ParentToolPane;
        WebPart wp = tp.SelectedWebPart;
        this.webPartPropertyValue = wp.GetWebPartProperty(this.WebPartPropertyName);
        if (this.webPartPropertyValue != null)
          this.loadedState = true;
      }
      return this.loadedState;
    }

    /// <summary>
    /// Use this method to push data back into the parent web part.
    /// Assmues you have already set this.webPartPropertyValue before
    /// calling this method. Developers should override this method
    /// to do any conversions that are needed.
    /// </summary>
    protected virtual void SaveWebPartPropertyValue() {
      if (!string.IsNullOrEmpty(this.WebPartPropertyName)) {
        ToolPane tp = this.ParentToolPane;
        WebPart wp = tp.SelectedWebPart;
        wp.SetWebPartProperty(this.WebPartPropertyName, this.webPartPropertyValue);
      }
    }

    /// <summary>
    /// This method takes existing controls and populates them with data.
    /// Developers should override this method to copy data from this.webPartPropertyValue
    /// to the UI; it is called automatically after LoadWebPartPropertyValue in
    /// CreateChildControls, SyncChanges, and CancelChanges.
    /// </summary>
    protected virtual void LoadControlState() {
      EnsureChildControls();
      //base.LoadControlState();
    }

    /// <summary>
    /// Developers should override this method in order to save the user's
    /// selection by copying existing control data from the UI into
    /// this.webPartPropertyValue. It is called automatically before
    /// SaveWebPartPropertyValue in Flex_ApplyChanges.
    /// </summary>
    protected override object SaveControlState() {
      EnsureChildControls();

      base.SaveControlState();
      return null;
    }

    #endregion

    #region IRequiredPropertiesWebPart

    public virtual bool RequiredPropertiesSet {
      get {
        if (string.IsNullOrEmpty(this.WebPartPropertyName))
          return false;
        return true;
      }
    }

    public virtual void RenderRequiredPropertiesMessage(TextWriter writer, bool script, bool div) {
      string moreInfo = "You must specify a value for WebPartPropertyName and ListDataHandler.TargetListNameOrUrl in the ToolPart.";
      ToolPane tp = this.ParentToolPane;
      spwp.WebPart wp = tp.SelectedWebPart;
      wp.RenderRequiredPropertiesMessage(writer, moreInfo, script, div);
    }

    #endregion

  }

}
