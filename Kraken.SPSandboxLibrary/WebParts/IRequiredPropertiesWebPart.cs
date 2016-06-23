using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;

namespace Kraken.SharePoint.WebParts {

  /// <summary>
  /// Interface used for web parts that implement required web part properties.
  /// </summary>
  public interface IRequiredPropertiesWebPart {

    /// <summary>
    /// Gets a value indicating whether all the required properties have been set.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if all required properties set; otherwise, <c>false</c>.
    /// </value>
    bool RequiredPropertiesSet { get; }

    /// <summary>
    /// Renders the required properties message. In general, this should include
    /// a link to the toolpane, using webPart.RenderToolPaneLink(HtmlTextWriter writer...)
    /// </summary>
    /// <param name="writer">The HTML text writer stream.</param>
    /// <seealso cref="WebPartToolPaneExtensions.RenderToolPaneLink" />
    void RenderRequiredPropertiesMessage(TextWriter writer, bool script, bool div);

  }

}
