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

    public TimeSpan Stop() {
      if (_stopWatch != null) {
        if (_stopWatch.IsRunning)
          _stopWatch.Stop();
        TimeSpan ts = _stopWatch.Elapsed;
        return ts;
      } 
      return TimeSpan.MinValue;
    }

    public void Dispose() {
      TimeSpan ts = Stop();
      // TODO measure in a way we can push up the stack
      string msg = string.Format("Elapsed time {0:00}:{1:00}:{2:00}.{3:00}",
          ts.Hours, ts.Minutes, ts.Seconds,
          ts.Milliseconds / 10);
      if (_trace != null)
        _trace.TraceVerbose(msg);
      if (_outputOp != null)
        _outputOp(TraceLevel.Verbose, msg);
    }

  }
}
