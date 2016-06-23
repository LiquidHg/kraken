using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls.WebParts;

using wp = System.Web.UI.WebControls.WebParts;
using com = System.ComponentModel;

using Kraken.SharePoint;
using Kraken.SharePoint.WebParts;
using System.IO;

namespace Kraken.SharePoint.WebParts.Cloud {

  public class SandboxWebPart : wp.WebPart {

    public SandboxWebPart() : base() {
      AbortOnException = false;
    }

    public virtual bool Abort { get; set; }
    public virtual bool AbortOnException { get; set; }

    /// <summary>
    /// Developers should override this method and return true when a web part runs as sandbox code
    /// </summary>
    public virtual bool IsSandboxWebPart {
      get {
        return true;
      }
    }

    /// <summary>
    /// Used to pass WebPartManager to some extension methods
    /// </summary>
    internal WebPartManager _WebPartManager {
      get {
        return this.WebPartManager;
      }
    }

    /// <summary>
    /// Safely tell if a web part page is in browse/edit/design mode even in sandbox code.
    /// </summary>
    public bool DisplayModeEquals(WebPartDisplayModes mode) {
      return this.WebPartManager.DisplayMode.DisplayModeEquals(mode);
      // do not use this in sandbox code
      //return (SPContext.Current.FormContext.FormMode == SPControlMode.Edit);
    }

    /// <summary>
    /// Developers should override this method to include debug information for their web part.
    /// </summary>
    /// <returns></returns>
    protected virtual NameValueCollection GetDebugInfo() {
      NameValueCollection debugInfo = new NameValueCollection();
      debugInfo.Add("ClientIDSuffix", this.ClientIDSuffix);
      debugInfo.Add("CssClass", this.CssClass);
      debugInfo.Add("JScriptPath", this.JScriptPath);
      debugInfo.Add("JScriptMin", this.JScriptMin.ToString());
      return debugInfo;
    }

    public readonly NotificationBag Notifications = new NotificationBag();

    internal SandboxScriptManager _ScriptManager {
      get {
        return this.ScriptManager;
      }
    }
    protected SandboxScriptManager ScriptManager {
      get {
        return SandboxScriptManager.CurrentScriptManger;
      }
    }

    public virtual void DoSaveProperties(bool runElevated) {
      if (!runElevated) {
        base.SetPersonalizationDirty();
        return;
      }
      throw new NotSupportedException("Cannot elevate a Sandbox web part.");
    }

    public const string CURRENT_JQUERY_VERSION = "jquery-1.6.4";
    public const string CURRENT_JQUERY_VERSION_MIN = "jquery-1.6.4.min";
    public const string DEFAULT_JSCRIPT_PATH = "/Style Library/Kraken/Scripts/";

    /*
    [wp.WebBrowsable(true),
    wp.WebDisplayName("JScript Path Format String"),
    wp.WebDescription("Root-relative path for jQuery and FullCalendar scripts."),
    wp.Personalizable(wp.PersonalizationScope.Shared),
    com.Category("Advanced"),
    com.DefaultValue("/Style Library/Kraken/Scripts/{0}.js")]
    public string JScriptPathFormat {
      get;
      set;
    }
     */

    [wp.WebBrowsable(true),
    wp.WebDisplayName("Container Tag ID Suffix"),
    wp.WebDescription("Optional: This value will be appended in the browser as part of the 'id' attribute in a top-level tag; it is used for distinguishing this HTML from similar web parts rendered on the page."),
    wp.Personalizable(wp.PersonalizationScope.Shared), // spwp.WebPartStorage(spwp.Storage.Shared),
    com.Category("Appearance"), // spwp.SPWebCategoryName("Appearance"),
    com.DefaultValue("")]
    public string ClientIDSuffix {
      get { return clientIDSuffix; }
      set { 
        clientIDSuffix = value;
      } 
    }
    private string clientIDSuffix;

    /// <summary>
    /// Gets or sets the Cascading Style Sheet (CSS) class rendered by the Web server control on the client.
    /// </summary>
    /// <returns>
    /// The CSS class rendered by the Web server control on the client. The default is <see cref="F:System.String.Empty"/>.
    ///   </returns>
    [wp.WebBrowsable(true),
    wp.WebDisplayName("CSS Class"),
    wp.WebDescription("Specify a custom CSS class for the web part's outermost tag; it is used for styling the HTML of web parts rendered on the page."),
    wp.Personalizable(wp.PersonalizationScope.Shared),
    com.Category("Appearance")]
    public new string CssClass {
      get { return base.CssClass; }
      set { base.CssClass = value; }
    }

    [wp.WebBrowsable(true),
    wp.WebDisplayName("JScript Path"),
    wp.WebDescription("Root-relative path for jQuery and FullCalendar scripts."),
    wp.Personalizable(wp.PersonalizationScope.Shared),
    com.Category("Advanced"),
    com.DefaultValue(DEFAULT_JSCRIPT_PATH)]
    public string JScriptPath {
      get;
      set;
    }

    [wp.WebBrowsable(true),
    wp.WebDisplayName("Use Minimized JScript"),
    wp.WebDescription("If checked, the web part will load 'min' scripts; disable for debug version"),
    wp.Personalizable(wp.PersonalizationScope.Shared),
    com.Category("Advanced"),
    com.DefaultValue(true)]
    public bool JScriptMin {
      get;
      set;
    }

    /* commented because the CSM does not work in Sandboxed solutions
     * http://blog.mastykarz.nl/dynamically-loading-javascript-sandbox/
    private void EnsureScript(string propertyName, string url) {
      ClientScriptManager csm = this.Page.ClientScript;
      if (!csm.IsClientScriptIncludeRegistered(propertyName)) {
        csm.RegisterClientScriptInclude(propertyName, url);
      }
    }
     */

    // TODO these have been supereded and need to be adjusted in child web parts

    /*
    protected readonly IList<string> ScriptsToRegister = new List<string>();

    protected void OnRegisterScriptsBeforeRender(HtmlTextWriter writer) {
      foreach (string script in this.ScriptsToRegister) {
        writer.RenderScriptInclude(this.JScriptPath, script);
      }
    }
    protected void OnRegisterScriptsAfterRender(HtmlTextWriter writer) {
      foreach (string script in this.ScriptsToRegister) {
        writer.NotifySODScriptInclude(this.JScriptPath, script);
      }
    }
     */

    #region Override Methods for Piping

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override FlexWebPart_Init instead.
    /// </summary>
    /// <param name="e"></param>
    protected override sealed void OnInit(EventArgs e) {
#if __Office365__
      string methodName = "OnInit";
#else
      System.Reflection.MethodBase currentMethod = System.Reflection.MethodInfo.GetCurrentMethod(); // "OnInit"
      string methodName = currentMethod.Name;
#endif
      AbortableWebPartEvent(methodName, null, delegate(EventArgs e2) {
        Flex_Init(e2);
      });
    }

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override FlexWebPart_Load instead.
    /// </summary>
    /// <param name="e"></param>
    protected override sealed void OnLoad(EventArgs e) {
#if __Office365__
      string methodName = "OnLoad";
#else
      System.Reflection.MethodBase currentMethod = System.Reflection.MethodInfo.GetCurrentMethod(); // "OnLoad"
      string methodName = currentMethod.Name;
#endif
      AbortableWebPartEvent(methodName, null, delegate(EventArgs e2) {
        Flex_Load(e2);
      });
    }

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override FlexWebPart_CreateChildControls instead.
    /// </summary>
    protected override sealed void CreateChildControls() {
#if __Office365__
      string methodName = "CreateChildControls";
#else
      System.Reflection.MethodBase currentMethod = System.Reflection.MethodInfo.GetCurrentMethod(); // "CreateChildControls"
      string methodName = currentMethod.Name;
#endif
      AbortableWebPartEvent(methodName, null, delegate(EventArgs e2) {
        this.ScriptManager.AddToWebPart(this);
        Flex_CreateChildControls();
      });
    }

    /// <summary>
    /// This method has been VITRIFIED.
    /// Developers should override FlexWebPart_PreRender.
    /// </summary>
    /// <param name="e"></param>
    protected override sealed void OnPreRender(EventArgs e) {
#if __Office365__
      string methodName = "OnPreRender";
#else
      System.Reflection.MethodBase currentMethod = System.Reflection.MethodInfo.GetCurrentMethod(); // "OnPreRender"
      string methodName = currentMethod.Name;
#endif
      AbortableWebPartEvent(methodName, null, delegate(EventArgs e2) {
        // TODO they are registered but if they are sandboxed we need to call SOD later
        // ensure that IRequiredPropertiesSet works correctly when used
        if ((this as IRequiredPropertiesWebPart) != null)
          this.RegisterIE5UpClientScript(); // is sandbox aware now
        // ensure that the Notification Bag will work properly
        this.RegisterShowHideClientScript(); // is sandbox aware now
        // inform the user if the web part is missing needed properties
        IRequiredPropertiesWebPart r = this as IRequiredPropertiesWebPart;
        if (r != null) {
          if (!r.RequiredPropertiesSet) {
            StringBuilder sb = new StringBuilder();
            TextWriter writer = new StringWriter(sb);
            r.RenderRequiredPropertiesMessage(writer, true, false);
            Notifications.AddWarning(sb.ToString());
          }
        }
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
        Notifications.Render(this, writer, this.ID); // now sandbox aware
        if (!Abort)
          Flex_Render(writer);
      } catch (Exception ex) {
        // fail-safe error message
        writer.WriteException(ex, string.Format("Error in {0}::Render().", this.GetType().FullName), null);
      }
    }

    #endregion

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
