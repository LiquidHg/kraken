using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Kraken.Tracing {
  public class SimpleTrace : ITrace {
    public SimpleTrace() {
      Handler = (level, msg) =>
      {
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

    public void Trace(TraceLevel level, string format, params object[] args) {
      string indent = string.Empty;
      for (int i = 1; i < this.Depth; i++)
        indent += "  ";
      Console.WriteLine("{0}{1}: {2}",
        indent,
        Enum.GetName(typeof(TraceLevel), level).ToUpper(),
        (args == null || args.Length == 0) ? format :  string.Format(format, args)
      );
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
        TraceError("{0} => {1}", ex.Message, ex.StackTrace);
#else
        TraceError(ex.Message);
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

    public Action<TraceLevel, string> Handler { get; set; }


    public int Depth { get; set; } = 0;

    public TraceLevel Level { get; set; }

    public bool SilenceErrors { get; set; }
    public bool SilenceWarnings { get; set; }

  }
}
