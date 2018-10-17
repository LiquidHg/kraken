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
  using Kraken.SharePoint.WebParts.Cloud;

  // WARN not properly globalized

  /// <summary>
  /// This class includes extension methods useful for linking web parts to the tool pane.
  /// </summary>
  public static class WebPartToolPaneExtensions {

    public const string SCRIPTKEY_IE5UP = "IE5Up";

    /// <summary>
    /// The default text used for rendering links.
    /// </summary>
    public const string DEFAULT_LINK_TEXT = "open the tool pane";

    /// <summary>
    /// Default HTML to be used in rendering links.
    /// </summary>
    public const string DEFAULT_LINK_HTML = "<a href=\"{0}\">{1}</a>";

    /// <summary>
    /// Renders an A (anchor tag) to the web part tool pane.
    /// </summary>
    /// <param name="webPart">A reference to the web part</param>
    /// <param name="writer">The HTML text writer stream</param>
    /// <param name="text">The text to display in the link. If empty, uses the default, "open in tool pane".</param>
    /// <remarks>
    /// Uses a 'hacked' (reverse engineered) method to create the links, since they have only ASP.net web part type to work with, not SharePoint web part.
    /// </remarks>
    public static void RenderToolPaneLink(this aspwp.WebPart webPart, bool checkForScript, TextWriter writer, string text) {
      if (string.IsNullOrEmpty(text))
        text = DEFAULT_LINK_TEXT;
      writer.Write(DEFAULT_LINK_HTML, webPart.GetToolPaneLink(checkForScript), text);
    }

    /// <summary>
    /// This code should work around missing JavaScript when the page is not in edit mode.
    /// Includes the IE5Up.js script onto the page.
    /// </summary>
    /// <param name="webPart">The web part</param>
    public static void RegisterIE5UpClientScript(this aspwp.WebPart webPart, bool alwaysRegister) {
      SandboxWebPart sbwp = webPart as SandboxWebPart;
      string scriptUrl = "/_layouts/1033/ie55up.js"; // TODO Dynamically determine the locale of SharePoint and pick link location appropriately
      bool doNeedScript = webPart.GetDisplayMode().DisplayModeEquals(WebPartDisplayModes.Browse);
      if (!doNeedScript)
        return; // don't bother registering the script, since we're in edit or design mode anyway
      if (sbwp != null && sbwp.IsSandboxWebPart) {
        sbwp._ScriptManager.RegisterClientScriptInclude(SCRIPTKEY_IE5UP, scriptUrl);
      } else {
        webPart.Page.ClientScript.RegisterClientScriptInclude(SCRIPTKEY_IE5UP, scriptUrl);
      }
    }
    // Added the override because there might very well be other places/displaymodes where this script is needed
    public static void RegisterIE5UpClientScript(this aspwp.WebPart webPart) {
      webPart.RegisterIE5UpClientScript(true);
    }

    /// <summary>
    /// This method detects if the IE5Up.js script has been loaded and throws and exception
    /// if it has not, but is needed. It is aware of SandboxWebPart and special SOD methods.
    /// </summary>
    /// <param name="webPart">The web part</param>
    /// <param name="throwErrorOnNotFound">Throws exception if the script is not registered</param>
    /// <param name="registerOnNotFound">Not yet implemented - will register the script if it wasn't already done</param>
    /// <returns>Nullable boolean: null is not tested, true is found, false is not found</returns>
    public static bool? EnsureIE5UpClientScript(this aspwp.WebPart webPart, List<WebPartDisplayModes> displayModesToCheck, bool throwErrorOnNotFound, bool registerOnNotFound) {
      bool doNeedScript = false;
      foreach(WebPartDisplayModes mode in displayModesToCheck) {
        doNeedScript |= webPart.GetDisplayMode().DisplayModeEquals(mode);
      }
      if (!doNeedScript)
        return null; // don't bother checking for the script, since we're not in teh expected display mode anyway
      bool throwError = false;
      SandboxWebPart sbwp = webPart as SandboxWebPart;
      if (sbwp != null && sbwp.IsSandboxWebPart) {
        throwError = (!sbwp._ScriptManager.IsClientScriptIncludeRegistered(SCRIPTKEY_IE5UP, false));
      } else {
        throwError = (!webPart.Page.ClientScript.IsClientScriptIncludeRegistered(SCRIPTKEY_IE5UP));
      }
      if (throwErrorOnNotFound && throwError)
        throw new Exception("You must call RegisterIE5UpClientScript() prior to render portion of page lifecycle to use this method.");
      return !throwError;
    }
    public static bool? EnsureIE5UpClientScript(this aspwp.WebPart webPart) {
      return webPart.EnsureIE5UpClientScript(
        new List<WebPartDisplayModes>() { WebPartDisplayModes.Browse },
        true,
        false
      );
    }

  } // class
}
