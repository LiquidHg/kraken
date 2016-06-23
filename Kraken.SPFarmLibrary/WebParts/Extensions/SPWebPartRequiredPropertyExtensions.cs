using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;

using spwp = Microsoft.SharePoint.WebPartPages;

namespace Kraken.SharePoint.WebParts {

  public static class SPWebPartRequiredPropertyExtensions {

    public static void RenderRequiredPropertiesMessage(this spwp.WebPart webPart, HtmlTextWriter writer, string additionalInfo, bool script, bool divContainer) {
      RenderRequiredPropertiesMessage(webPart, writer, additionalInfo, true, true);
    }

    /// <summary>
    /// Renders the required properties message.
    /// </summary>
    /// <param name="writer">The writer.</param>
    public static void RenderRequiredPropertiesMessage(this spwp.WebPart webPart, TextWriter writer, string additionalInfo, bool script, bool divContainer) {
      bool? found = webPart.EnsureIE5UpClientScript(); // better to throw the error early than halfway through a div tag
      // TODO resource abstraction and globalization
      if (divContainer)
        writer.Write("<div class=\"requiredPropertiesMessage\">");
      writer.Write("Required properties have not been set. ");
      writer.Write(additionalInfo);
      writer.Write("To configure these properties ");
      webPart.RenderToolPaneLink(script, writer, string.Empty);
      writer.Write(".");
      if (divContainer)
        writer.Write("</div>");
    }

  }

}
