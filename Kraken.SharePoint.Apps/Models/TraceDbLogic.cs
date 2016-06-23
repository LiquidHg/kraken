using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.Apps.Models {
  //class TraceDbLogic {
  //}

  public partial class LogEntry {

    public static void Write(
      System.Diagnostics.TraceLevel level,
      string message,
      Guid correlationId = default(Guid),
      DatabaseTrace trace = null
    ) {
      TraceDb db = null;
      if (trace != null)
        db = trace.DatabaseContext;
      if (db == null)
        db = new TraceDb();

      LogEntry log = new LogEntry() {
        Id = Guid.NewGuid(),
        Level = level,
        Message = message,
        CorrelationId = correlationId,
        Application = (trace == null) ? null : trace.AppContext,
        Session = (trace == null) ? null : trace.SessionContext,
        Time = DateTime.Now.ToUniversalTime()
      };
      db.Log.Add(log);
      db.SaveChanges();
    }
  } // class LogEntry

  public partial class Application {

    /// <summary>
    /// Get an existing application by name and create it isf it doesn't already exist
    /// </summary>
    /// <param name="appName"></param>
    /// <returns></returns>
    public static Application EnsureApplication(TraceDb dbContext, string appName) {
      if (dbContext == null)
        dbContext = new TraceDb();
      Application app = dbContext.Applications.Where(a => appName.Equals(a.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
      if (app == null) {
        app = new Application() {
          Id = Guid.NewGuid(),
          Name = appName
        };
        dbContext.SaveChanges();
      }
      return app;
    }

  }
}
