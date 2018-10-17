using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls.WebParts;

using Kraken.SharePoint.WebParts.Cloud;

namespace Kraken.SharePoint.WebParts {

  public static class ShowHideExpanderExtensions {

    public const string SCRIPTKEY = "ShowHideGroup";

    /// <summary>
    /// Ensure the group expanded javascipt is included
    /// </summary>
    /// <param name="webPart"></param>
    public static void RegisterShowHideClientScript(this WebPart webPart) {
      SandboxWebPart sbwp = webPart as SandboxWebPart;
      string scriptUrl = "/_layouts/Kraken.SharePoint.WebParts/ShowHideGroup.js";
      if (sbwp != null && sbwp.IsSandboxWebPart) {
        sbwp._ScriptManager.RegisterClientScriptInclude(SCRIPTKEY, scriptUrl);
      } else {
        webPart.Page.ClientScript.RegisterClientScriptInclude(SCRIPTKEY, scriptUrl);
      }
    }
    public static void EnsureShowHideClientScript(this WebPart webPart) {
      SandboxWebPart sbwp = webPart as SandboxWebPart;
      bool throwError = false;
      if (sbwp != null && sbwp.IsSandboxWebPart) {
        if (!sbwp._ScriptManager.IsClientScriptIncludeRegistered(SCRIPTKEY, false))
          throwError = true;
      } else {
        if (!webPart.Page.ClientScript.IsClientScriptIncludeRegistered(SCRIPTKEY))
          throwError = true;
      }
      if (throwError)
        throw new Exception("You must call RegisterShowHideClientScript() prior to render portion of page lifecycle to use this method.");
    }


    /// <summary>
    /// Registers a client javascript and renders the group expander.
    /// </summary>
    /// <param name="page">The web part page, where client script will be registered.</param>
    /// <param name="writer">The HTML text writer stream</param>
    /// <param name="clientIdToExpand">The client of the target object that will be shown/hidden.</param>
    /// <param name="expandText">The (optional) expander text, displayed next to the plus/minus icon.</param>
    /// <param name="isExpanded">if set to <c>true</c> rendered as expanded. <c>false</c> for collapsed.</c></param>
    public static void RenderGroupExpander(WebPart webPart, HtmlTextWriter writer, string clientIdToExpand, bool isExpanded, string expandText) {
      webPart.EnsureShowHideClientScript();
      RenderGroupExpander(writer, clientIdToExpand, isExpanded, expandText);
    }
    public static void RenderGroupExpander(HtmlTextWriter writer, string clientIdToExpand, bool isExpanded, string expandText) {
      // SP 2007 land worked like this
      //writer.Write("<img id=\"{1}\" src=\"/_layouts/images/plus.gif\" alt=\"Expand\" onClick=\"MessagePanel_ToggleHelpText('{0}', '{1}')\" style=\"cursor:hand;\" />", clientIdToExpand, imgId);
      string imgId = clientIdToExpand + "_clickImg";
      writer.Write("<a href=\"javascript:ShowHideGroup()\" onclick=\"javascript:ShowHideGroup(document.getElementById('{0}'),document.getElementById('{1}'));return false;\">", clientIdToExpand, imgId);
      writer.Write("<img id=\"{0}\" src=\"/_layouts/images/{2}.gif\" border=\"0\" alt=\"Click to Expand\" />{1}</a>", imgId, expandText, isExpanded ? "minus" : "plus");
    }

    /// <summary>
    /// Renders the expander begin DIV tag.
    /// </summary>
    /// <param name="writer">The HTML text writer stream</param>
    /// <param name="clientIdToExpand">The client of the target object that will be shown/hidden.</param>
    /// <param name="isExpanded">if set to <c>true</c> rendered as expanded. <c>false</c> for collapsed.</c></param>
    public static void RenderExpanderBeginTag(WebPart webPart, HtmlTextWriter writer, string clientIdToExpand, bool isExpanded) {
      webPart.EnsureShowHideClientScript();
      RenderExpanderBeginTag(writer, clientIdToExpand, isExpanded);
    }
    public static void RenderExpanderBeginTag(HtmlTextWriter writer, string clientIdToExpand, bool isExpanded) {
      writer.Write("<div id=\"{0}\" style=\"{1}\">", clientIdToExpand, isExpanded ? "" : "display:none");
    }
    /// <summary>
    /// Renders the expander close DIV tag.
    /// </summary>
    /// <param name="writer">The HTML text writer stream</param>
    public static void RenderExpanderCloseTag(WebPart webPart, HtmlTextWriter writer) {
      webPart.EnsureShowHideClientScript();
      RenderExpanderCloseTag(writer);
    }
    public static void RenderExpanderCloseTag(HtmlTextWriter writer) {
      writer.Write("</div>");
    }

  }

}
