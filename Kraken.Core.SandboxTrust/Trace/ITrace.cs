using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;

namespace Kraken.Tracing {
  public interface ITrace {
    void Trace(TraceLevel level, string format, params object[] args);
    void TraceInfo(string format, params object[] args);
    void TraceError(string format, params object[] args);
    void TraceError(Exception ex);
    void TraceWarning(string format, params object[] args);
    void TraceVerbose(string format, params object[] args);
    void TraceObject(object obj);
    Action<TraceLevel, string> Handler { get; set; }

    TraceLevel Level { get; set; }
    int Depth { get; set; }

    bool SilenceErrors { get; set; }
    bool SilenceWarnings { get; set; }

  }
}
