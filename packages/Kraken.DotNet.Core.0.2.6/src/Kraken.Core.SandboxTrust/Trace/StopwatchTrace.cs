using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;

namespace Kraken.Tracing {
  public class StopWatchTrace : IDisposable {
    private Stopwatch _stopWatch;
    private ITrace _trace;
    private Action<TraceLevel, string> _outputOp;

    public StopWatchTrace(ITrace trace) {
      _trace = trace;
      _stopWatch = new Stopwatch();
      _stopWatch.Start();
    }

    public StopWatchTrace(Action<TraceLevel, string> outputOp) {
      _outputOp = outputOp;
      _stopWatch = new Stopwatch();
      _stopWatch.Start();
    }

    public void Dispose() {
      _stopWatch.Stop();
      TimeSpan ts = _stopWatch.Elapsed;

      string elapsedTime = String.Format("Elapsed time {0:00}:{1:00}:{2:00}.{3:00}",
          ts.Hours, ts.Minutes, ts.Seconds,
          ts.Milliseconds / 10);

      if (_trace != null)
        _trace.TraceInfo(elapsedTime);
      if (_outputOp != null)
        _outputOp(TraceLevel.Info, elapsedTime);
    }
  }
}
