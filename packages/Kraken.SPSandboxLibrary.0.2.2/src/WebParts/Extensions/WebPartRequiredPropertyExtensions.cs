using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using aspwp = System.Web.UI.WebControls.WebParts;
using Kraken.SharePoint.WebParts.Cloud;
using System.Web.UI;

namespace Kraken.SharePoint.WebParts {

  public static class WebPartRequiredPropertyExtensions {

    private static void RenderRequiredPropertiesMessageBegin(TextWriter writer, string additionalInfo, bool divContainer) {
      if (divContainer)
        writer.Write("<div class=\"requiredPropertiesMessage\">");
      writer.Write("Required properties have not been set. ");
      writer.Write(additionalInfo);
      writer.Write("To configure these properties ");
    }
    private static void RenderRequiredPropertiesMessageEnd(TextWriter writer, bool divContainer) {
      writer.Write(".");
      if (divContainer)
        writer.Write("</div>");
    }

    /// <summary>
    /// Renders the required properties message.
    /// </summary>
    /// <param name="writer">The writer.</param>
    public static void RenderRequiredPropertiesMessage(this aspwp.WebPart webPart, TextWriter writer, string additionalInfo, bool script, bool divContainer) {
      if (script) {
        bool? found = webPart.EnsureIE5UpClientScript();
        if (found ?? false) { // don't attempt to get a registered item that does not exist!
          // special handling for SOD in Sandbox web parts
          SandboxWebPart sbwp = webPart as SandboxWebPart;
          if (sbwp != null && sbwp.IsSandboxWebPart) {
            SandboxScriptItem scriptIE5Up = sbwp._ScriptManager.GetRegisteredItem(WebPartToolPaneExtensions.SCRIPTKEY_IE5UP, false);
            HtmlTextWriter htmlWriter = writer as HtmlTextWriter;
            if (scriptIE5Up != null && htmlWriter != null)
              scriptIE5Up.RenderLoadSodByKey(htmlWriter, true);
          }
        }
      }
      // TODO resource abstraction and globalization
      RenderRequiredPropertiesMessageBegin(writer, additionalInfo, divContainer);
      webPart.RenderToolPaneLink(script, writer, string.Empty);
      RenderRequiredPropertiesMessageEnd(writer, divContainer);
    }

    public static void RenderRequiredPropertiesMessage(this aspwp.WebPart webPart, TextWriter writer, string additionalInfo) {
      webPart.RenderRequiredPropertiesMessage(writer, additionalInfo, true, true);
    }

  }

}
