using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;

namespace Kraken.SharePoint.WebParts.Cloud {

  public class SandboxScriptItem {

    public SandboxScriptItem() {
      ParentType = typeof(Page);
      IsAlreadyRendered = false;
    }

    public ScriptKey CreateScriptKey() {
      _key = new ScriptKey(ParentType, CleanFilenameSuffix(_fileName), this.IsInclude);
      return _key;
    }

    public static string NormalizeName(string name) {
      if (!name.EndsWith(".js", StringComparison.InvariantCultureIgnoreCase))
        name += ".js";
      return name.ToLower(); // lowercase to deal with an issue in the SharePoint
    }
    private static string CleanFilenameSuffix(string name) {
      if (name.EndsWith(".js", StringComparison.InvariantCultureIgnoreCase))
        name = name.Substring(0, name.Length - 3);
      return name.ToLower();
    }

    private ScriptKey _key;
    public ScriptKey Key {
      get {
        return _key; 
      }
    }

    private string _fileName;
    public string FileName {
      get {
        return _fileName;
      }
      set {
        _fileName = NormalizeName(value);
      }
    }

    public Type ParentType;
    public string Url;
    public string ScriptBlock;
    public string CharSet;
    public bool OnDemand;
    public bool LoadAfterUI;
    public bool IsAlreadyRendered;

    public readonly List<SandboxScriptItem> DependsOn = new List<SandboxScriptItem>();

    public bool IsInclude {
      get {
        return (string.IsNullOrEmpty(this.ScriptBlock) && !string.IsNullOrEmpty(this.Url));
      }
    }

    public bool ScriptIncludesTags {
      get {
        if (this.IsInclude)
          return false;
        return ScriptBlock.StartsWith("<script", StringComparison.InvariantCultureIgnoreCase);
      }
    }

    /// <summary>
    /// Renders the script to the page's HTML writer.
    /// </summary>
    /// <remarks>
    /// In the case where this is registering as a script include by Url and OnDemand is true, the developer
    /// will have to add the following line to the end of their script file where {0} matches this.Key.
    /// if (typeof (NotifyScriptLoadedAndExecuteWaitingJobs) != "undefined") 
    ///   SP.SOD.notifyScriptLoadedAndExecuteWaitingJobs('{0}');
    /// </remarks>
    /// <param name="writer"></param>
    public void RenderScript(HtmlTextWriter writer) {
      if (IsAlreadyRendered)
        return;

      // determine if we need to do a weird workaround for the all-to-buggy registerSodDep
      bool doAlternativeSODDependency = !this.IsInclude;
      if (!doAlternativeSODDependency && DependsOn != null && DependsOn.Count > 0) {
        foreach (SandboxScriptItem scriptInfo in DependsOn) {
          if (!scriptInfo.IsInclude) {
            doAlternativeSODDependency = true;
            break;
          }
        }
      }
      string notifyFunctionName = string.Empty;

      writer.WriteLine();
      writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
      if (IsInclude && !OnDemand) // Renders as a standard jScript include
        writer.AddAttribute(HtmlTextWriterAttribute.Src, this.Url);
      if (!string.IsNullOrEmpty(this.CharSet))
        writer.AddAttribute("charset", this.CharSet);
      writer.RenderBeginTag(HtmlTextWriterTag.Script);
      try {
        writer.WriteLine();
        if (IsInclude) { // Url based include file
          if (OnDemand) // Register for load on demand; keep in mind that now the method for calling functions is more complex
            writer.WriteLine(string.Format("SP.SOD.registerSod('{0}', '{1}');", this.Key, this.Url)); // this.FileName
        } else { // dynamically generated script block
          // This was commented out because it doesn't seem to help
          /*
          if (OnDemand) // we are trying to fool SOD into accepting the key into its dictionary
            writer.WriteLine(string.Format("SP.SOD.registerSod('{0}', '{1}');", this.Key, string.Empty));
           */
          writer.Write(this.ScriptBlock);
          writer.WriteLine();
          // SOD dependencies don't seem to work for dynamically generated scripts, so we'll try a different approach below
          if (OnDemand) {
            if (doAlternativeSODDependency) {
              notifyFunctionName = RenderAlternateNotifyScriptLoadedAndExecuteWaitingJobs(writer, false);
            } else {
              RenderNotifyScriptLoadedAndExecuteWaitingJobs(writer, false);
            }
          }
        }
        if (this.OnDemand && DependsOn != null && DependsOn.Count > 0) {
          writer.WriteLine();
          foreach (SandboxScriptItem scriptInfo in DependsOn) {
            if (doAlternativeSODDependency) {
              scriptInfo.RenderLoadSodByKey(writer, false);
              // calls the notification when script loading is completed
              scriptInfo.RenderExecuteOrDelayUntilScriptLoaded(writer, notifyFunctionName, false);
            } else
              writer.WriteLine("SP.SOD.registerSodDep('{0}', '{1}');", this.Key, scriptInfo.Key);
          }
          writer.WriteLine();
        }
      } catch {
        writer.RenderEndTag();
        throw;
      }
      writer.RenderEndTag();
      IsAlreadyRendered = true;
    }

    /// <summary>
    /// Creates a small javascript function that will count down the number of dependencies
    /// which need to be loaded, and when that number reaches 0, it will fire NotifyScriptLoadedAndExecuteWaitingJobs.
    /// </summary>
    /// <param name="writer">output stream</param>
    /// <param name="renderScriptBlock">Enclude the script in tags?</param>
    /// <returns>The name of the javascript function that was generated.</returns>
    public string RenderAlternateNotifyScriptLoadedAndExecuteWaitingJobs(HtmlTextWriter writer, bool renderScriptBlock) {
      if (renderScriptBlock) {
        writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
        writer.RenderBeginTag(HtmlTextWriterTag.Script);
      }
      string notifyFunctionName = string.Format("notify_{0}", this.Key);
      // Add some script we can call to indicate that this script is actually fully loaded
      // but wrap it in a function so it will only get called after all dependencies are also loaded
      writer.WriteLine("var num_{0} = {1};", notifyFunctionName, this.DependsOn.Count);
      writer.WriteLine();
      writer.WriteLine("function {0}(){{", notifyFunctionName); // note double curly braces
      // at this point check to see that all the dependent scripts are ready
      writer.WriteLine("  num_{0}--;", notifyFunctionName);
      writer.WriteLine("  if (num_{0} <= 0) {{", notifyFunctionName); // note double curly braces
      //writer.WriteLine("    alert('Test: {0} has loaded all dependencies.');", this.Key);
      RenderNotifyScriptLoadedAndExecuteWaitingJobs(writer, false);
      writer.WriteLine("  }");
      writer.WriteLine("}");
      if (renderScriptBlock)
        writer.RenderEndTag();
      return notifyFunctionName;
    }

    public void RenderNotifyScriptLoadedAndExecuteWaitingJobs(HtmlTextWriter writer, bool renderScriptBlock) {
      if (renderScriptBlock) {
        writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
        writer.RenderBeginTag(HtmlTextWriterTag.Script);
      }
      writer.WriteLine();
      writer.WriteLine("// This code added to trigger SharePoint Script on Demand");
      writer.WriteLine("if (typeof(NotifyScriptLoadedAndExecuteWaitingJobs) != \"undefined\")");
      writer.WriteLine("  SP.SOD.notifyScriptLoadedAndExecuteWaitingJobs('{0}');", this.Key);
      if (renderScriptBlock)
        writer.RenderEndTag();
    }

    /// <summary>
    /// Use this method to render to the HTML writer a call to the SOD to
    /// execute certain code only after a script is fully loaded.
    /// Don't forget to wrap this code inside a script block.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="function">A function delegate (fn name or text of a javascript function)</param>
    /// <param name="renderScriptBlock">If true, script tags will be rendered around the javascript command</param>
    public void RenderExecuteOrDelayUntilScriptLoaded(HtmlTextWriter writer, string function, bool renderScriptBlock) {
      if (renderScriptBlock) {
        writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
        writer.RenderBeginTag(HtmlTextWriterTag.Script);
      }
      writer.Write("SP.SOD.executeOrDelayUntilScriptLoaded({0}, \"{1}\");", function, this.Key);
      writer.WriteLine();
      if (renderScriptBlock)
        writer.RenderEndTag();
    }

    /// <summary>
    /// Use this method to render to the HTML writer a call to the SOD to
    /// execute certain code only after a script is fully loaded.
    /// Don't forget to wrap this code inside a script block.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="ajaxFunction">A function delegate for AJAX, or leave empty for null</param>
    /// <param name="function">A function delegate (fn name or text of a javascript function)</param>
    /// <param name="renderScriptBlock">If true, script tags will be rendered around the javascript command</param>
    public void RenderExecuteFunc(HtmlTextWriter writer, string ajaxFunction, string function, bool renderScriptBlock) {
      if (renderScriptBlock) {
        writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
        writer.RenderBeginTag(HtmlTextWriterTag.Script);
      }
      if (string.IsNullOrEmpty(ajaxFunction)) {
        ajaxFunction = "null";
      }
      writer.WriteLine("SP.SOD.executeFunc(\"{0}\", {1}, {2});", this.Key, ajaxFunction, function);
      if (renderScriptBlock)
        writer.RenderEndTag();
    }

    /// <summary>
    /// Triggers a function to execute when the BODY is fully loaded.
    /// Don't forget to wrap this code inside a script block.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="functionName">String which is the name of a specific javascript function</param>
    /// <param name="renderScriptBlock">If true, script tags will be rendered around the javascript command</param>
    public static void RenderSPBodyOnLoadFunctionNames(HtmlTextWriter writer, string functionName, bool renderScriptBlock) {
      if (renderScriptBlock) {
        writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
        writer.RenderBeginTag(HtmlTextWriterTag.Script);
      }
      writer.WriteLine("_spBodyOnLoadFunctionNames.push(\"{0}\");", functionName);
      if (renderScriptBlock)
        writer.RenderEndTag();
    }

    public void RenderLoadSodByKey(HtmlTextWriter writer, bool renderScriptBlock) {
      RenderLoadSodByKey(writer, "function(){return;}", renderScriptBlock);
    }
    /// <summary>
    /// Forces the SOD to load a specific script-on-demand.
    /// Don't forget to wrap this code inside a script block.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="renderScriptBlock">If true, script tags will be rendered around the javascript command</param>
    public void RenderLoadSodByKey(HtmlTextWriter writer, string callBackFunction, bool renderScriptBlock) {
      if (renderScriptBlock) {
        writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
        writer.RenderBeginTag(HtmlTextWriterTag.Script);
      }
      // 2nd null arg prevents the overloaded function from stripping out path names if they exist in your FileName
      writer.WriteLine("LoadSodByKey(\"{0}\", {1});", this.Key, callBackFunction); // note the escaped curly braces
      if (renderScriptBlock)
        writer.RenderEndTag();
    }

  }

}
