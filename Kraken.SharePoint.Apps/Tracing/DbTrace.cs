using Kraken.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Web;

namespace Kraken.Apps.Models {
  public class DatabaseTrace : ITrace {

    public Application AppContext { get; set; }

    public string SessionContext { get; set; }

    private TraceDb _databaseContext = null;
    public TraceDb DatabaseContext { 
      get {
        if (_databaseContext == null)
          _databaseContext = new TraceDb();
        return _databaseContext;
      }
    }


    /// <summary>
    /// Call this method at the start of a service call or page load
    /// Sets the property needed to trace a unique app and session ID
    /// </summary>
    /// <param name="context"></param>
    public void SetSessionContext(string appName, string context) {
      string session = string.Empty;
      if (OperationContext.Current != null) {
        try {
          session = OperationContext.Current.SessionId;
        } catch {
        }
      }
      if (string.IsNullOrEmpty(session) && HttpContext.Current != null && HttpContext.Current.Session != null) {
        session = HttpContext.Current.Session.SessionID;
      }
      if (string.IsNullOrEmpty(session))
        session = context;
      if (string.IsNullOrEmpty(session))
        session = "unknown";
      this.SessionContext = session;

      // adds the application if it doesn't exist
      this.AppContext = Application.EnsureApplication(this.DatabaseContext, appName);
    }

    public DatabaseTrace() : this(TraceLevel.Warning) { }
    public DatabaseTrace(TraceLevel minLevelToLog) {
      this.Level = minLevelToLog;
      Handler = (traceLevel, msg) => {
        if (traceLevel == TraceLevel.Warning) {
          TraceWarning(msg);
        }
        if (traceLevel == TraceLevel.Error) {
          TraceError(msg);
        }
        if (traceLevel == TraceLevel.Info) {
          TraceInfo(msg);
        }
        if (traceLevel == TraceLevel.Verbose) {
          TraceVerbose(msg);
        }
      };
    }

    public void Trace(TraceLevel level, string format, params object[] args) {
      if ((this.Level == TraceLevel.Off || level > this.Level)
        || (level == TraceLevel.Warning && this.SilenceWarnings)
        || (level == TraceLevel.Error && this.SilenceErrors))
        return;
      string fmt = Enum.GetName(typeof(TraceLevel), level).ToUpper() + ": {0}";
      string message = (args == null || args.Length == 0)
        ? string.Format(fmt, format)
        : string.Format(fmt, string.Format(format, args));
      LogEntry.Write(
        level,
        message,
        default(Guid),
        this
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
      if (!this.SilenceErrors)
        TraceError(ex.Message);
    }

    public void TraceWarning(string format, params object[] args) {
      if (!this.SilenceWarnings)
        Trace(TraceLevel.Warning, format, args);
    }

    public void TraceVerbose(string format, params object[] args) {
      Trace(TraceLevel.Verbose, format, args);
    }

    public void TraceObject(object obj) {
      throw new NotImplementedException("TraceObject is not implemented");
    }

    public Action<TraceLevel, string> Handler { get; set; }

    public TraceLevel Level { get; set; }
    public int Depth { get; set; } = 0;

    public bool SilenceErrors { get; set; }
    public bool SilenceWarnings { get; set; }

  }
}