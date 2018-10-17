using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Microsoft.SharePoint;
using System.Security.Permissions;

namespace Kraken.SharePoint.WebParts.Cloud {

  /// <summary>
  /// Page.CSM does not work in Sandboxed solutions, so we made this one instead
  /// http://blog.mastykarz.nl/dynamically-loading-javascript-sandbox/
  /// </summary>
  [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Minimal)]
  public class SandboxScriptManager : WebControl {

    public const string DefaultID = "SPSandboxScriptManager";

    private ListDictionary _registeredScripts;
    // Find a better way to return this
    public SandboxScriptItem GetRegisteredItem(string key, bool fullKeyMatch) {
      foreach (ScriptKey skey in _registeredScripts.Keys) {
        SandboxScriptItem item = (SandboxScriptItem)_registeredScripts[skey];
        if (item.Key.IsMatch(key, fullKeyMatch))
          return item;
      }
      throw new IndexOutOfRangeException(string.Format("There is no script registered with the key '{0}'.", key));
      //return (SandboxScriptItem)_registeredScripts[key];
    }
    public bool IsClientScriptIncludeRegistered(string key, bool fullKeyMatch) {
      foreach (ScriptKey skey in _registeredScripts.Keys) {
        SandboxScriptItem item = (SandboxScriptItem)_registeredScripts[skey];
        if (item.Key.IsMatch(key, fullKeyMatch))
          return true;
      }
      return false;
    }

    private static SandboxScriptManager _scriptManager;
    public static SandboxScriptManager CurrentScriptManger {
      get {
        //_scriptManager = this.Page.FindControl(SandboxScriptManager.DefaultID) as SandboxScriptManager;
        if (_scriptManager == null) {
          _scriptManager = new SandboxScriptManager();
          _scriptManager.ID = DefaultID;
        }
        return _scriptManager;
      }
    }

    /// <summary>
    /// Since in the sandbox, we can't really attach to the Page object.
    /// Instead, we'll attach to the first web part that comes along and
    /// then detach and re-attach to each one so we end up on the last one
    /// To run on the page. You can control the execution of scripts using
    /// the various Render sub-commands to load scripts on demand.
    /// </summary>
    /// <param name="control"></param>
    internal void AddToWebPart(SandboxWebPart control) {
      if (string.IsNullOrEmpty(this.ID))
        this.ID = DefaultID;
      if (control != null) {
        if (this.Parent != null)
          this.Parent.Controls.Remove(this);
        control.Controls.Add(this);
      }
    }

    public SandboxScriptItem RegisterClientScriptInclude(string name, string url) {
      return this.RegisterClientScriptInclude(typeof(Page), name, url, string.Empty, true);
    }
    public SandboxScriptItem RegisterClientScriptInclude(string name, string url, string charset) {
      return this.RegisterClientScriptInclude(typeof(Page), name, url, charset, true);
    }
    public SandboxScriptItem RegisterClientScriptInclude(Type type, string name, string url, bool onDemand) {
      return RegisterClientScriptInclude(type, name, url, string.Empty, onDemand);
    }
    public SandboxScriptItem RegisterClientScriptInclude(Type type, string name, string url, string charset, bool onDemand) {
      if (type == null)
        throw new ArgumentNullException("type");
      if (string.IsNullOrEmpty(name))
        throw new ArgumentNullException("name", "Parameter 'name' is null or an empty string.");
      if (string.IsNullOrEmpty(url))
        throw new ArgumentNullException("url", "Parameter 'url' is null or an empty string.");

      SandboxScriptItem script = new SandboxScriptItem() {
        ParentType = type,
        FileName = name,
        Url = url,
        CharSet = charset,
        OnDemand = onDemand
      };
      Add(script);
      return script;
    }

    public SandboxScriptItem RegisterClientScriptBlock(string key, string scriptBody) {
      return this.RegisterClientScriptBlock(typeof(Page), key, scriptBody, true);
    }
    public SandboxScriptItem RegisterClientScriptBlock(Type type, string key, string scriptBody, bool onDemand) {
      if (type == null)
        throw new ArgumentNullException("type");
      if (string.IsNullOrEmpty(key))
        throw new ArgumentNullException("key", "Parameter 'key' is null or an empty string.");
      if (string.IsNullOrEmpty(scriptBody))
        throw new ArgumentNullException("scriptBody", "Parameter 'scriptBody' is null or an empty string.");
      SandboxScriptItem script = new SandboxScriptItem() {
        ParentType = type,
        FileName = key,
        ScriptBlock = scriptBody,
        OnDemand = onDemand
      };
      if (script.ScriptIncludesTags)
        throw new NotSupportedException("You must remove <script> tags from your scripts in order to be supported in this script handler.");
      Add(script);
      return script;
    }

    private void Add(SandboxScriptItem script) {
      ScriptKey key = script.CreateScriptKey();
      if (_registeredScripts == null)
        _registeredScripts = new ListDictionary();
      if (_registeredScripts[key] == null)
        _registeredScripts.Add(key, script);
    }

    /// <summary>
    /// This method will render all of the script includes and blocks in order,
    /// respecting their settings for SOD (script on demand).
    /// </summary>
    /// <remarks>
    /// This should be called before you need to execute the script, 
    /// but it could happen later if you are using callbacks.
    /// </remarks>
    /// <param name="writer"></param>
    public override void RenderControl(HtmlTextWriter writer) {
      if (this._registeredScripts == null)
        return;
      foreach (ScriptKey key in this._registeredScripts.Keys) {
        SandboxScriptItem script = (SandboxScriptItem)this._registeredScripts[key];
        script.RenderScript(writer);
      }
      //base.RenderControl(writer);
      // When we are completely done rendering, clear the flags
      foreach (ScriptKey key in this._registeredScripts.Keys) {
        SandboxScriptItem script = (SandboxScriptItem)this._registeredScripts[key];
        if (script.IsAlreadyRendered)
          script.IsAlreadyRendered = false;
      }
    }

    public void RenderNotifyForAllIncludes(HtmlTextWriter writer, bool renderScriptBlock) {
      if (this._registeredScripts == null)
        return;
      if (renderScriptBlock) {
        writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
        writer.RenderBeginTag(HtmlTextWriterTag.Script);
      }
      foreach (ScriptKey key in this._registeredScripts.Keys) {
        SandboxScriptItem script = (SandboxScriptItem)this._registeredScripts[key];
        if (script.OnDemand && script.IsInclude)
          script.RenderNotifyScriptLoadedAndExecuteWaitingJobs(writer, false);
      }
      if (renderScriptBlock)
        writer.RenderEndTag();
    }

    public static string GetFullScriptUrl(string path, string fn) {
      string serverPrefix = SPContext.Current.Site.ServerRelativeUrl.TrimEnd('/');
      if (string.IsNullOrEmpty(path)) {
        path = SandboxWebPart.DEFAULT_JSCRIPT_PATH;
      }
      if (!fn.EndsWith(".js", StringComparison.InvariantCultureIgnoreCase)) {
        fn = fn + ".js";
      }
      string formatString = "{0}{1}{2}";
      string fullUrl = string.Format(formatString, serverPrefix, path, fn);
      return fullUrl;
    }

  } // class

} // namespace
