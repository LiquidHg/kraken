using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;

using Microsoft.SharePoint;
using System.Web.UI.WebControls.WebParts;

namespace Kraken.SharePoint.WebParts.Cloud {

  public static class SandboxScriptExtensions {

    public static void OptionallyRenderLoadSodByKey(this WebPart wp, HtmlTextWriter writer, string key) {
      SandboxWebPart sbwp = wp as SandboxWebPart;
      if (sbwp != null && sbwp.IsSandboxWebPart) {
        if (sbwp._ScriptManager.IsClientScriptIncludeRegistered(key, false)) {
          SandboxScriptItem scriptForm = sbwp._ScriptManager.GetRegisteredItem(key, false);
          if (scriptForm != null)
            scriptForm.RenderLoadSodByKey(writer, true);
        }
      }
    }

    public static void RenderScriptInclude(this HtmlTextWriter writer, string urlFormat, string file) {
      string scriptPrefix = SPContext.Current.Site.ServerRelativeUrl.TrimEnd('/');
      writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
      writer.AddAttribute(HtmlTextWriterAttribute.Src, string.Format(urlFormat, file, scriptPrefix));
      writer.RenderBeginTag(HtmlTextWriterTag.Script);
      writer.RenderEndTag();
    }

    public static void RenderScriptBlock(this HtmlTextWriter writer, StringBuilder sb, string key) {
      writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
      writer.RenderBeginTag(HtmlTextWriterTag.Script);
      //writer.Flush();
      writer.Write(sb.ToString());
      writer.WriteLine();
      writer.Write("SP.SOD.notifyScriptLoadedAndExecuteWaitingJobs('{0}');", key);
      writer.WriteLine();
      //writer.Flush();
      writer.RenderEndTag();
    }

    public static void NotifySODScriptInclude(this HtmlTextWriter writer, string urlFormat, string file) {
      string scriptPrefix = SPContext.Current.Site.ServerRelativeUrl.TrimEnd('/');
      writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
      writer.RenderBeginTag(HtmlTextWriterTag.Script);
      writer.Write(string.Format("SP.SOD.registerSod('{0}.js', '", file));
      writer.Write(string.Format(urlFormat, file, scriptPrefix));
      writer.Write("');");
      writer.RenderEndTag();
    } 

  }
}
