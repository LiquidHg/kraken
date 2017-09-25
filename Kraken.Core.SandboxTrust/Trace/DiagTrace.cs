using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;

namespace Kraken.Tracing {
  public class DiagTrace : ITrace {

    public DiagTrace() {
      Handler = (level, msg) => {
        if (level == TraceLevel.Warning) {
          TraceWarning(msg);
        }
        if (level == TraceLevel.Error) {
          TraceError(msg);
        }
        if (level == TraceLevel.Info) {
          TraceInfo(msg);
        }
        if (level == TraceLevel.Verbose) {
          TraceVerbose(msg);
        }
      };
    }

    public Action<System.Diagnostics.TraceLevel, string> Handler { get; set; }
    public void Trace(TraceLevel level, string format, params object[] args) {
      string indent = string.Empty;
      for (int i = 1; i < this.Depth; i++)
        indent += "  ";
      string msg = string.Format("{0}{1}{2}",
        indent,
        (level == TraceLevel.Verbose) ? "Verbose: " : "", //Enum.GetName(typeof(TraceLevel), level).ToUpper(),
        (args == null || args.Length == 0) ? format : string.Format(format, args));
      switch (level) {
        case TraceLevel.Warning:
          System.Diagnostics.Trace.TraceWarning(msg);
          break;
        case TraceLevel.Info:
          System.Diagnostics.Trace.TraceInformation(msg);
          break;
        case TraceLevel.Error:
          System.Diagnostics.Trace.TraceError(msg);
          break;
        case TraceLevel.Verbose:
          System.Diagnostics.Trace.WriteLine(msg);
          break;
      }
    }

    public void TraceInfo(string format, params object[] args) {
      Trace(TraceLevel.Info, format, args);
    }

    public void TraceError(string format, params object[] args) {
      if (!this.SilenceErrors)
        Trace(TraceLevel.Error, format, args);
    }

    public void TraceError(Exception ex) {
      if (!this.SilenceErrors) {
#if DEBUG
        TraceError("{0}: {1} => {2}", ex.GetType().Name, ex.Message, ex.StackTrace);
#else
        TraceError("{0}: {1}, ex.GetType().Name, ex.Message);
#endif
      }
    }

    public void TraceWarning(string format, params object[] args) {
      if (!this.SilenceWarnings)
        Trace(TraceLevel.Warning, format, args);
    }

    public void TraceVerbose(string format, params object[] args) {
      Trace(TraceLevel.Verbose, format, args);
    }

    public void TraceObject(object obj) {
      Console.WriteLine("OBJECT: {0}", obj.ToString());
    }

    private static DiagTrace _default;

    /// <summary>
    /// Creates a default instance of NullTrace that can be used
    /// when the caller does not specify any ITrace instance.
    /// </summary>
    public static DiagTrace Default {
      get {
        if (_default == null)
          _default = new DiagTrace();
        return _default;
      }
    }

    public TraceLevel Level { get; set; }

    public int Depth { get; set; } = 0;

    public bool SilenceErrors { get; set; }

    public bool SilenceWarnings { get; set; }

  }
}
