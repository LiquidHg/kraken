namespace Kraken.SharePoint.WebParts {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.IO;
  using System.Text;
  using System.Web.UI;
  using System.Web.UI.WebControls;

  using Microsoft.SharePoint;
  using Microsoft.SharePoint.Utilities;
  using Microsoft.SharePoint.WebPartPages;

  using aspwp = System.Web.UI.WebControls.WebParts;
  using spwp = Microsoft.SharePoint.WebPartPages;

  // WARN not properly globalized

  /// <summary>
  /// This class includes extension methods useful for linking web parts to the tool pane.
  /// </summary>
  public static class SPWebPartToolPaneExtensions {

    /// <summary>
    /// Gets a link to the web part tool pane.
    /// </summary>
    /// <param name="page">The web part page</param>
    /// <returns>The link to the tool pane. Uses javascript; no HTML anchor tag is included.</returns>
    /// <remarks>
    /// Uses the SharePoint methods, because they are able to pass the correct "type" of web part.
    /// </remarks>
    public static string GetToolPaneLink(this spwp.WebPart webPart) {
      webPart.EnsureIE5UpClientScript();
      //return GetToolPaneLink(webPart.ID);
      // Okay now we should be able to render a link that works
      aspwp.WebPartDisplayMode displayMode = aspwp.WebPartManager.EditDisplayMode;
      string script = ToolPane.GetShowToolPaneEvent(webPart, displayMode);
      return script;
    }

    /// <summary>
    /// Renders an A (anchor tag) to the web part tool pane.
    /// </summary>
    /// <param name="webPart">A reference to the web part</param>
    /// <param name="writer">The HTML text writer stream</param>
    /// <param name="text">The text to display in the link. If empty, uses the default, "open in tool pane".</param>
    /// <remarks>
    /// Uses the SharePoint methods, because they are able to pass the correct "type" of web part.
    /// </remarks>
    public static void RenderToolPaneLink(this spwp.WebPart webPart, HtmlTextWriter writer, string text) {
      //RenderToolPaneLink(webPart.ID, writer, text);
      webPart.EnsureIE5UpClientScript();
      if (string.IsNullOrEmpty(text))
        text = WebPartToolPaneExtensions.DEFAULT_LINK_TEXT;
      writer.Write(WebPartToolPaneExtensions.DEFAULT_LINK_HTML, GetToolPaneLink(webPart), text);
    }

  }

}
