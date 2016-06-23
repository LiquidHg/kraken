using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using aspwp = System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using spwp = Microsoft.SharePoint.WebPartPages;

namespace Kraken.SharePoint.WebParts {

  [ToolboxItemAttribute(false)]
  public class FlexBaseSPWebPart : spwp.WebPart {

    public FlexBaseSPWebPart() {
      AbortOnException = false;
    }

    public NotificationBag Notifications = new NotificationBag();

    public virtual bool Abort { get; set; }
    public virtual bool AbortOnException { get; set; }

    /// <summary>
    /// Developers should override this method to include debug information for their web part.
    /// </summary>
    /// <returns></returns>
    protected virtual NameValueCollection GetDebugInfo() {
      return null;
    }

    /// <summary>
    /// Note this method will not work on sandboxed solutions.
    /// </summary>
    /// <param name="runElevated"></param>
    public void DoSaveProperties(bool runElevated) {
      if (!runElevated) {
        base.SetPersonalizationDirty();
        return;
      }
      SPSecurity.RunWithElevatedPrivileges(delegate() {
        base.SetPersonalizationDirty();
      });
    }

    #region Override Methods for Piping

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override FlexWebPart_Init instead.
    /// </summary>
    /// <param name="e"></param>
    protected override sealed void OnInit(EventArgs e) {
      AbortableWebPartEvent("OnInit", null, delegate(EventArgs e2) {
        Flex_Init(e2);
      });
    }

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override FlexWebPart_Load instead.
    /// </summary>
    /// <param name="e"></param>
    protected override sealed void OnLoad(EventArgs e) {
      AbortableWebPartEvent("OnLoad", null, delegate(EventArgs e2) {
        Flex_Load(e2);
      });
    }

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override FlexWebPart_CreateChildControls instead.
    /// </summary>
    protected override sealed void CreateChildControls() {
      AbortableWebPartEvent("CreateChildControls", null, delegate(EventArgs e2) {
        Flex_CreateChildControls();
      });
    }

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override FlexWebPart_PreRender.
    /// </summary>
    /// <param name="e"></param>
    protected override sealed void OnPreRender(EventArgs e) {
      AbortableWebPartEvent("OnPreRender", e, delegate(EventArgs e2) {
        // ensure that the Notification Bag will work properly
        this.RegisterShowHideClientScript();
        // ensure that IRequiredPropertiesSet works correctly when used
        this.RegisterIE5UpClientScript();
        /*
        // inform the user if the web part is missing needed properties
        if (!this.RequiredPropertiesSet) {
          StringBuilder sb = new StringBuilder();
          TextWriter writer = new StringWriter(sb);
          RenderRequiredPropertiesMessage(writer, false, false);
          Notifications.AddWarning(sb.ToString());
        }
         */
        Flex_PreRender(e2);
      });
    }

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override FlexWebPart_Render instead.
    /// </summary>
    /// <param name="writer"></param>
    protected override sealed void Render(HtmlTextWriter writer) {
      try {
        Notifications.Render(this, writer, this.ID);
        if (!Abort)
          Flex_Render(writer);
      } catch (Exception ex) {
        // fail-safe error message
        writer.WriteException(ex, string.Format("Error in {0}::Render().", this.GetType().FullName), null);
      }
    }

    protected void AbortableWebPartEvent(string methodName, EventArgs e, Action<EventArgs> DoThis) {
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

    #region Methods for Piping (will be overridden in sub-classes)

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
    protected virtual void Flex_CreateChildControls() {
      base.CreateChildControls();
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
      base.Render(writer);
    }

    #endregion

  }

}
