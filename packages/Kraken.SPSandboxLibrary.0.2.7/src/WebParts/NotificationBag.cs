namespace Kraken.SharePoint.WebParts {

  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Collections.Specialized;
  using System.Diagnostics;
  using System.Text;
  using System.Web.UI;
  using Kraken.SharePoint.WebParts.Cloud;
  using System.Web.UI.WebControls.WebParts;

  /// <summary>
  /// This bag is a useful container for storing and rendering system messages displayed in web parts.
  /// </summary>
  public class NotificationBag : List<WebPartNotification> {

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationBag"/> class.
    /// </summary>
    public NotificationBag() {
      NotificationLevel = TraceLevel.Info;
    }

    /// <summary>
    /// Gets or sets the minimum notification level. WebPartNotification objects 
    /// will only be displayed if they are at or above this severity level.
    /// </summary>
    /// <value>
    /// The minimum visible notification level
    /// </value>
    public TraceLevel NotificationLevel {
      get;
      set;
    }

    /// <summary>
    /// Gets a value indicating whether this bag contains any WebPartNotifications with errors.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if the bag contains any WebPartNotifications with TraceLevel.Error; otherwise, <c>false</c>.
    /// </value>
    public bool HasErrors {
      get {
        foreach (WebPartNotification item in this) {
          if (item.Exception != null || item.Level == TraceLevel.Error)
            return true;
        }
        return false;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this bag contains any WebPartNotifications with warnings.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if the bag contains any WebPartNotifications with TraceLevel.Warning; otherwise, <c>false</c>.
    /// </value>
    public bool HasWarnings {
      get {
        foreach (WebPartNotification item in this) {
          if (item.Level == TraceLevel.Warning)
            return true;
        }
        return false;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this bag contains any WebPartNotifications with information.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if the bag contains any WebPartNotifications with TraceLevel.Info; otherwise, <c>false</c>.
    /// </value>
    public bool HasInfo {
      get {
        foreach (WebPartNotification item in this) {
          if (item.Level == TraceLevel.Info)
            return true;
        }
        return false;
      }
    }


    /// <summary>
    /// Adds a WebPartNotifcation to the bag that references an exception.
    /// </summary>
    /// <param name="ex">An exception to include in the notification.</param>
    /// <param name="addtlMsg">Additional message text to append to the exception message.</param>
    /// <param name="debugInfo">A supplimental collection of debug info.</param>
    public void AddError(Exception ex, string addtlMsg, NameValueCollection debugInfo) {
      this.Add(new WebPartNotification() { Level = TraceLevel.Error, Exception = ex, Message = addtlMsg, DebugInfo = debugInfo });
    }
    /// <summary>
    /// Adds a WebPartNotifcation to the bag that references an exception.
    /// </summary>
    /// <param name="ex">An exception to include in the notification.</param>
    /// <param name="addtlMsg">Additional message text to append to the exception message.</param>
    public void AddError(Exception ex, string addtlMsg) {
      this.Add(new WebPartNotification() { Level = TraceLevel.Error, Exception = ex, Message = addtlMsg });
    }
    /// <summary>
    /// Adds a WebPartNotifcation to the bag that references an exception.
    /// </summary>
    /// <param name="ex">An exception to include in the notification.</param>
    public void AddError(Exception ex) {
      this.Add(new WebPartNotification() { Level = TraceLevel.Error, Exception = ex });
    }
    /// <summary>
    /// Adds a WebPartNotifcation with an error message with debug info.
    /// </summary>
    /// <param name="msg">The error message to be displayed.</param>
    public void AddError(string msg, NameValueCollection debugInfo) {
      this.Add(new WebPartNotification() { Level = TraceLevel.Error, Message = msg, DebugInfo = debugInfo });
    }

    /// <summary>
    /// Adds a WebPartNotifcation with an error message.
    /// </summary>
    /// <param name="msg">The error message to be displayed.</param>
    public void AddError(string msg) {
      this.Add(new WebPartNotification() { Level = TraceLevel.Error, Message = msg });
    }


    /// <summary>
    /// Adds a WebPartNotifcation with an information message.
    /// </summary>
    /// <param name="msg">The info message to be displayed.</param>
    public void AddInfo(string msg) {
      this.Add(new WebPartNotification() { Level = TraceLevel.Info, Message = msg });
    }
    /// <summary>
    /// Adds a WebPartNotifcation with a warning message.
    /// </summary>
    /// <param name="msg">The warning message to be displayed.</param>
    public void AddWarning(string msg) {
      this.Add(new WebPartNotification() { Level = TraceLevel.Warning, Message = msg });
    }

    // TODO add a better sorting algorythm to the bag 
    /// <summary>
    /// Renders the notification messages to the writer. Messages are 
    /// displayed in the following order: Errors, Warnings, Info, and others.
    /// Only messages at or above NotficationLevel will be displayed.
    /// </summary>
    /// <param name="writer">The HTML text writer stream</param>
    /// <param name="webPartId">The web part ID property</param>
    public void Render(WebPart wp, HtmlTextWriter writer, string webPartId) {
      if (this.Count > 0) {
        // automatically demand the script needed here if it is running in the sandbox
        wp.OptionallyRenderLoadSodByKey(writer, ShowHideExpanderExtensions.SCRIPTKEY);

        int counter = 0;
        writer.Write("<table class=\"ms-WPBody\" style=\"padding:0px;width:100%;\">");
        if (NotificationLevel >= TraceLevel.Error) {
          foreach (WebPartNotification item in this) {
            if (item.Level == TraceLevel.Error)
              item.RenderTableRow(wp, writer, webPartId, counter++);
          } // foreach
        }
        if (NotificationLevel >= TraceLevel.Warning) {
          foreach (WebPartNotification item in this) {
            if (item.Level == TraceLevel.Warning)
              item.RenderTableRow(wp, writer, webPartId, counter++);
          } // foreach
        }
        if (NotificationLevel >= TraceLevel.Info) {
          foreach (WebPartNotification item in this) {
            if (item.Level == TraceLevel.Info)
              item.RenderTableRow(wp, writer, webPartId, counter++);
          } // foreach
        }
        if (NotificationLevel >= TraceLevel.Verbose) {
          foreach (WebPartNotification item in this) {
            if (item.Level == TraceLevel.Verbose && item.Level == TraceLevel.Off)
              item.RenderTableRow(wp, writer, webPartId, counter++);
          } // foreach
        }
        writer.Write("</table>");
      } // if HasErrors
    }

  } // class

}
