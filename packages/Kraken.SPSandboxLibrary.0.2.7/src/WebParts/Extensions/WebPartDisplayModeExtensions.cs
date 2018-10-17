using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls.WebParts;
using Kraken.SharePoint.WebParts.Cloud;
using Microsoft.SharePoint.Utilities;

namespace Kraken.SharePoint.WebParts {

  public enum WebPartDisplayModes {
    Unknown,
    Browse,
    Edit,
    Design
  }

  public static class WebPartDisplayModeExtensions {

    /*
    public static bool DisplayModeEquals(this WebPartManager mgr, WebPartDisplayModes mode) {
      string name = GetDisplayModeName(mode);
      return mgr.DisplayMode.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase);
    }*/
    public static bool DisplayModeEquals(this WebPartDisplayMode mgrMode, WebPartDisplayModes mode) {
      string name = GetDisplayModeName(mode);
      return mgrMode.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase);
    }

    public static string GetDisplayModeName(WebPartDisplayModes mode) {
      string name = string.Empty;
      switch (mode) {
        case WebPartDisplayModes.Browse:
          name = WebPartManager.BrowseDisplayMode.Name;
          break;
        case WebPartDisplayModes.Edit:
          name = WebPartManager.EditDisplayMode.Name;
          break;
        case WebPartDisplayModes.Design:
          name = WebPartManager.DesignDisplayMode.Name;
          break;
        default:
          name = WebPartDisplayModes.Unknown.ToString();
          break;
      }
      return name;
    }
    public static WebPartDisplayMode GetDisplayMode(WebPartDisplayModes mode) {
      WebPartDisplayMode result = null;
      switch (mode) {
        case WebPartDisplayModes.Browse:
          result = WebPartManager.BrowseDisplayMode;
          break;
        case WebPartDisplayModes.Edit:
          result = WebPartManager.EditDisplayMode;
          break;
        case WebPartDisplayModes.Design:
          result = WebPartManager.DesignDisplayMode;
          break;
      }
      return result;
    }

    /// <summary>
    /// Gets the current display mode for the web part page.
    /// </summary>
    /// <param name="webPart">The web part</param>
    /// <returns></returns>
    public static string GetDisplayModeName(this WebPart webPart) {
      WebPartDisplayMode displayMode = webPart.GetDisplayMode();
      return displayMode.Name;
    }
    public static WebPartDisplayMode GetDisplayMode(this WebPart webPart) {
      WebPartManager wpm = webPart.GetWebPartManager();
      WebPartDisplayMode displayMode = wpm.DisplayMode;
      return displayMode;
    }

    /// <summary>
    /// Returns the WebPartManager for a web part in a way that partially supports sandbox code.
    /// Use with care! This method is sensitive to whether you are passing SandboxWebPart
    /// </summary>
    /// <param name="webPart"></param>
    /// <returns></returns>
    public static WebPartManager GetWebPartManager(this WebPart webPart) {
      SandboxWebPart sbwp = webPart as SandboxWebPart;
      WebPartManager wpm = null;
      if (sbwp != null) { // && sbwp.IsSandboxWebPart
        wpm = sbwp._WebPartManager;
      } else { // old school method for getting WPM
        // you shouldn't use "using" here, no matter the warnings from FxCop; it would be bad!
        wpm = WebPartManager.GetCurrentWebPartManager(webPart.Page);
      }
      return wpm;
    }

    /// <summary>
    /// Renders an A (anchor tag) to the web part tool pane.
    /// </summary>
    /// <param name="page">The web part page</param>
    /// <returns>The link to the tool pane. Uses javascript; no HTML anchor tag is included.</returns>
    /// <remarks>
    /// Uses a 'hacked' (reverse engineered) method to create the links, since they have only ASP.net web part type to work with, not SharePoint web part.
    /// </remarks>
    public static string GetToolPaneLink(this WebPart webPart, WebPartDisplayModes mode, bool checkForScript) {
      string webPartId = string.Empty;
      bool? found = null;
      if (webPart != null) {
        if (checkForScript)
          found = webPart.EnsureIE5UpClientScript();
        webPartId = webPart.ID;
      }
      if (!(found ?? false))
        return "TOOLPANE LINK ERROR: no script loaded";
      WebPartDisplayMode displayMode = GetDisplayMode(mode);
      //Commented because while this worked in SP2007, it no longer works in 2010.
      /* return string.Format(JSLinkText1, webPartId); */
      //This was lifted from inside ToolPane by using .NET Reflector
      if (string.IsNullOrEmpty(webPartId))
        return string.Format(JSLinkText2, SPHttpUtility.EcmaScriptStringLiteralEncode(displayMode.Name));
      return string.Format(JSLinkText3, SPHttpUtility.EcmaScriptStringLiteralEncode(displayMode.Name), webPartId);
      /*
      string iD = part.ID;
      if ((iD == null) || (iD.Length == 0)) {
        iD = part.ClientID;
      }*/
    }
    public static string GetToolPaneLink(this WebPart webPart, bool checkForScript) {
      return webPart.GetToolPaneLink(WebPartDisplayModes.Edit, checkForScript);
    }

    //Commented because while this worked in SP2007, it no longer works in 2010.
    //public const string JSLinkText1 = "javascript:MSOTlPn_ShowToolPane2Wrapper('Edit', this, this,'{0}')";
    public const string JSLinkText2 = "javascript:MSOTlPn_ShowToolPane2('{0}');";
    public const string JSLinkText3 = "javascript:MSOTlPn_ShowToolPane2('{0}','{1}');";

  }

}
