using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using log4net;
using Kraken.Reflection;
using System.Diagnostics;

namespace Kraken.Logging {

  public static class Log4NetExtensions {

    public static void EnteringPreCheck(this ILog log, MethodBase method) {
      log.DebugFormat("Entering Pre-check '{0}'.", method.GetName());
    }
    public static void EnteringPreCheck(this ILog log, MethodBase method, string extraMsg) {
      log.DebugFormat("Entering Pre-check '{0}' {1}.", method.GetName(), extraMsg);
    }

    public static void Entering(this ILog log, MethodBase method) {
      log.DebugFormat("Entering '{0}'.", method.GetName());
    }
    public static void Entering(this ILog log, MethodBase method, string extraMsg) {
      log.DebugFormat("Entering '{0}' {1}.", method.GetName(), extraMsg);
    }

    public static void Leaving(this ILog log, MethodBase method) {
      log.DebugFormat("Leaving '{0}'.", method.GetName());
    }
    public static void Leaving(this ILog log, MethodBase method, string extraMsg) {
      log.DebugFormat("Leaving '{0}' {1}.", method.GetName(), extraMsg);
    }

    public static void HandleException(
      this ILog log,
      Exception ex, MethodBase method) {
      log.Error(string.Format("Unexpected error in {0}!", method.GetName()), ex);
    }
    public static void HandleException(
      this ILog log,
      Exception ex, MethodBase method, string extraMsg) {
      log.Error(string.Format("Unexpected error in {0}! {1}", method.GetName(), extraMsg), ex);
    }

    public static void StackTrace(
      this ILog log) {
      StringBuilder sb = new StringBuilder();
      sb.Append("Stack Trace");
      StackTrace stackTrace = new StackTrace();           // get call stack
      StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)
      foreach (StackFrame stackFrame in stackFrames) {
        sb.Append(" > " + stackFrame.GetMethod().GetName());   // write method name
      }
      log.Debug(sb.ToString());
    }

  }

}
