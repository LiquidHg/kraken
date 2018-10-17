namespace Kraken.SharePoint.WebParts {

  using System;
  using System.Collections.Generic;
  using System.Collections.Specialized;
  using System.Linq;
  using System.Text;
  using System.Web.UI;

  using aspwp = System.Web.UI.WebControls.WebParts;
  using spwp = Microsoft.SharePoint.WebPartPages;

  /// <summary>
  /// This class includes extension methods useful for rendering eExceptions in HTML.
  /// </summary>
  public static class WebPartErrorExtensions {

    //NameValueCollection DebugValues = new NameValueCollection();

    /// <summary>
    /// Writes an exception using a standardized (sexy) format.
    /// </summary>
    /// <param name="writer">The HTML text writer stream</param>
    /// <param name="ex">The exception to be displayed</param>
    /// <param name="additionalInstructions">Additional instructions to be appended to the error message</param>
    /// <param name="debugValues">A collection of debug values displayed for diagnostic purposes</param>
    public static void WriteException(this HtmlTextWriter writer, Exception ex, string additionalInstructions, NameValueCollection debugValues) {
      writer.Write("<div class=\"kraken-error\">");
      writer.Write("The web part contents cannot be rendered because the following exception occurred. ");
      writer.Write(additionalInstructions);
      writer.Write("<div class=\"exception\">");
      WriteException(writer, ex);
      if (ex.InnerException != null)
        WriteException(writer, ex);
      if (debugValues != null && debugValues.Count > 0) {
        writer.Write("<div class=\"debug\">");
        writer.Write("<div class=\"debugHead\">Debug Info:</div>");
        writer.Write("<ul class=\"debugList\">");
        foreach (string key in debugValues.Keys) {
          writer.Write("<li>{0} = {1}</li>", key, debugValues[key]);
        }
        writer.Write("</ul>");
        writer.Write("</div>"); // debug
      }
      writer.Write("</div>"); // exception
      writer.Write("</div>"); // kraken-error
    }


    /// <summary>
    /// Writes an exception message and its stack for display in HTML.
    /// Replaces line breaks with HTML breaks.
    /// </summary>
    /// <param name="writer">The HTML text writer stream</param>
    /// <param name="ex">The exception to be written</param>
    private static void WriteException(HtmlTextWriter writer, Exception ex) {
      writer.Write("<div class=\"exceptionMessage\">{0}: \"{1}\"</div><div class=\"exceptionStackTrace\">{2}</div>", ex.GetType().Name, ex.Message, ex.StackTrace.Replace("\r\n", "<br />"));
    }

  }

}
