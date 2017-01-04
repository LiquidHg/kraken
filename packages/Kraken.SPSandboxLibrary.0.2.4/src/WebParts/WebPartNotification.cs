namespace Kraken.SharePoint.WebParts {

  using System;
  using System.Collections.Generic;
  using System.Collections.Specialized;
  using System.Diagnostics;
  using System.Linq;
  using System.Text;
  using System.Web.UI;
  using Kraken.SharePoint.WebParts.Cloud;
  using System.Web.UI.WebControls.WebParts;

  /// <summary>
  /// This class contains data that needs to be collected in order to display status messages in the web part UI.
  /// </summary>
  public class WebPartNotification {

    /// <summary>
    /// The severity level of the notification.
    /// </summary>
    public TraceLevel Level;
    /// <summary>
    /// The message text, or additional text to be appended to system generated messages.
    /// </summary>
    public string Message;
    /// <summary>
    /// The message details, or additional text to be appended to system generated details.
    /// </summary>
    public string DetailedMessage;
    /// <summary>
    /// An (optional) exception that can be attached to the notification for additional information about the error.
    /// </summary>
    public Exception Exception;
    /// <summary>
    /// A collection of values that can be used for additional diagnostics.
    /// </summary>
    public NameValueCollection DebugInfo;

    /// <summary>
    /// Gets an image icon based on the Level.
    /// </summary>
    /// <returns>HTML for an IMG tag of the icon</returns>
    public string GetIconImgHtml() {
      string html = "<img src=\"/_layouts/{0}\" alt=\"{1}\" />";
      switch (this.Level) {
        case TraceLevel.Info:
          return string.Format(html, "1033/images/FilterInfo.gif", "Information");
        case TraceLevel.Error:
          return string.Format(html, "images/error16by16.gif", "Error");
        case TraceLevel.Warning:
          return string.Format(html, "images/FilterWarning.gif", "Warning");
      }
      return string.Empty;
    }

    /// <summary>
    /// Renders an image icon based on the Level.
    /// </summary>
    /// <param name="writer">The HTML text writer stream</param>
    public void RenderIcon(HtmlTextWriter writer) {
      writer.Write(GetIconImgHtml());
    }

    /// <summary>
    /// Renders the notification item.
    /// </summary>
    /// <param name="page">The web part page, where client script will be registered.</param>
    /// <param name="writer">The HTML text writer stream</param>
    /// <param name="webPartId">The ID property of web part, used in generating unique IDs for 'expenders' in HTML.</param>
    /// <param name="itemNumber">The item number, used in generating unique IDs for 'expenders' in HTML.</param>
    /// <remarks>The combination of  <paramref name="webPartId"/> and <paramref name="itemNumber"/> should be unique on the web page.</remarks>
    public void RenderItem(WebPart wp, HtmlTextWriter writer, string webPartId, int itemNumber) {
      bool hasDetails = !string.IsNullOrEmpty(this.DetailedMessage) || this.Exception != null;
      string heading = this.Message;
      if (this.Exception != null) {
        heading = "Web Part " + Level.ToString() + ": " + heading;
      } else if (string.IsNullOrEmpty(this.Message)) {
        heading = "Web Part " + Level.ToString();
      }
      writer.Write("<div>");

      bool expanded = false;
      string expandId = string.Format("{0}_errBagMsg{1}" , webPartId, itemNumber);
      if (!hasDetails) {
        writer.Write(heading);
      } else {
        wp.EnsureShowHideClientScript();
        ShowHideExpanderExtensions.RenderGroupExpander(writer, expandId, expanded, heading);
        ShowHideExpanderExtensions.RenderExpanderBeginTag(writer, expandId, expanded);
        if (this.Exception != null) {
          writer.WriteException(this.Exception, this.DetailedMessage, this.DebugInfo);
        } else if (!string.IsNullOrEmpty(this.DetailedMessage)) {
          writer.Write(this.DetailedMessage);
        }
        ShowHideExpanderExtensions.RenderExpanderCloseTag(writer);
      }
      writer.Write("</div>");
    }

    /// <summary>
    /// Renders the the item in a table row, with a leading icon.
    /// </summary>
    /// <param name="page">The web part page, where client script will be registered.</param>
    /// <param name="writer">The HTML text writer stream</param>
    /// <param name="webPartId">The ID property of web part, used in generating unique IDs for 'expenders' in HTML.</param>
    /// <param name="itemNumber">The item number, used in generating unique IDs for 'expenders' in HTML.</param>
    /// <remarks>The combination of  <paramref name="webPartId"/> and <paramref name="itemNumber"/> should be unique on the web page.</remarks>
    public void RenderTableRow(WebPart wp, HtmlTextWriter writer, string webPartId, int itemNumber) {
      writer.Write("<tr><td valign=\"top\" style=\"padding-left:4px;padding-right:4px;\">");
      this.RenderIcon(writer);
      writer.Write("</td><td width=\"100%\" style=\"padding-left:4px;padding-right:4px;\">");
      this.RenderItem(wp, writer, webPartId, itemNumber);
      writer.Write("</td></tr>");
    }

  }

}
