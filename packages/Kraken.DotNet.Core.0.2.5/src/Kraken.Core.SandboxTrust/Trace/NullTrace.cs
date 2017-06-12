using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;

namespace Kraken.Tracing {
  public class NullTrace : ITrace {

    public Action<System.Diagnostics.TraceLevel, string> Handler { get; set; }
    public void Trace(TraceLevel level, string format, params object[] args) { }
    public void TraceError(Exception ex) { }
    public void TraceError(string format, params object[] args) { }
    public void TraceInfo(string format, params object[] args) { }
    public void TraceObject(object obj) { }
    public void TraceVerbose(string format, params object[] args) { }
    public void TraceWarning(string format, params object[] args) { }

    private static NullTrace _default;

    /// <summary>
    /// Creates a default instance of NullTrace that can be used
    /// when the caller does not specify any ITrace instance.
    /// </summary>
    public static NullTrace Default {
      get {
        if (_default == null)
          _default = new NullTrace();
        return _default;
      }
    }

    public TraceLevel Level { get; set; }

    public int Depth { get; set; } = 0;

    public bool SilenceErrors { get; set; }

    public bool SilenceWarnings { get; set; }

  }
}
