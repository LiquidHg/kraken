namespace Kraken.SharePoint.Logging {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;
  using System.Text;

  using Microsoft.SharePoint.Administration;
  using Kraken.SharePoint.Logging;
  using Kraken.Reflection;
  using System.Diagnostics;

  public static class ReflectionExtensions {

    public static void EnteringPreCheck(this KrakenLoggingService uls, MethodBase method) {
      uls.Write(string.Format("Entering Pre-check '{0}'.", method.GetName()), TraceSeverity.Verbose, EventSeverity.Verbose);
    }
    public static void EnteringPreCheck(this KrakenLoggingService uls, MethodBase method, string extraMsg) {
      uls.Write(string.Format("Entering Pre-check '{0}' {1}.", method.GetName(), extraMsg), TraceSeverity.Verbose, EventSeverity.Verbose);
    }

    public static void Entering(this KrakenLoggingService uls, MethodBase method) {
      uls.Write(string.Format("Entering '{0}'.", method.GetName()), TraceSeverity.Verbose, EventSeverity.Verbose);
    }
    public static void Entering(this KrakenLoggingService uls, MethodBase method, string extraMsg) {
      uls.Write(string.Format("Entering '{0}' {1}.", method.GetName(), extraMsg), TraceSeverity.Verbose, EventSeverity.Verbose);
    }

    public static void Leaving(this KrakenLoggingService uls, MethodBase method) {
      uls.Write(string.Format("Leaving '{0}'.", method.GetName()), TraceSeverity.Verbose, EventSeverity.Verbose);
    }
    public static void Leaving(this KrakenLoggingService uls, MethodBase method, string extraMsg) {
      uls.Write(string.Format("Leaving '{0}' {1}.", method.GetName(), extraMsg), TraceSeverity.Verbose, EventSeverity.Verbose);
    }

    public static void HandleException(
      this KrakenLoggingService uls, 
      Exception ex, MethodBase method) {
        uls.Write(string.Format("Unexpected error in {0}!", method.GetName()), TraceSeverity.Unexpected, EventSeverity.Error);
      uls.Write(ex);
    }
    public static void HandleException(
      this KrakenLoggingService uls,
      Exception ex, MethodBase method, string extraMsg) {
        uls.Write(string.Format("Unexpected error in {0}! {1}", method.GetName(), extraMsg), TraceSeverity.Unexpected, EventSeverity.Error);
      uls.Write(ex);
    }

    public static void StackTrace(
      this KrakenLoggingService uls) {
      StringBuilder sb = new StringBuilder();
      sb.Append("Stack Trace");
      StackTrace stackTrace = new StackTrace();           // get call stack
      StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)
      foreach (StackFrame stackFrame in stackFrames) {
        sb.Append(" > " + stackFrame.GetMethod().GetName());   // write method name
      }
      uls.Write(sb.ToString(), TraceSeverity.Verbose, EventSeverity.Verbose);
    }

  }
}
