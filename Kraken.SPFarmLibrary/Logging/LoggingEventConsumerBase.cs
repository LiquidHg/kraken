/*
  Project Kraken: SPARK for Microsoft SharePoint 2010
  Copyright (C) 2003-2011 Thomas Carpe. <http://www.ThomasCarpe.com/>
  Maintained by: <http://www.LiquidMercurySolutions.com/>

  This file is part of SPARK: SharePoint Application Resource Kit.
  SPARK projects are distributed via CodePlex: <http://www.codeplex.com/spark/>

  You may use this code for commercial purposes and derivative works, 
  provided that you maintain all copyright notices.

  SPARK is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version. You should have received a copy of
  the GNU General Public License along with SPARK.  If not, see
  <http://www.gnu.org/licenses/>.

  SPARK is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.
  
  We worked hard on all SPARK code, and we don't make any profit from
  sharing it with the world. Please do us a favor amd give us credit
  where credit is due, by leaving this notice unchanged. We all stand
  on the backs of giants. Wherever we have used someone else's code or
  blog article as the basis of our work, we have provided references
  to our source.
*/

namespace Kraken.SharePoint.Logging {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using Microsoft.SharePoint.Administration;

  /// <summary>
  /// Put this code in your class that produces log entries, or inherit 
  /// this class and create your own LOGGING_PRODUCT and LOGGING_CATEGORY.
  /// </summary>
  public abstract class LoggingEventConsumerBase {

    #region Logging Code Pattern v2.0

    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    public event LoggingEventHandler Logging;
    //public event EventHandler<LoggingEventArgs> Logging;

    protected abstract string LOGGING_AREA {
      get; // "Kraken";
    }
    protected abstract string LOGGING_CATEGORY {
      get; // "Logging Example";
    }

    /// <summary>
    /// Put a method like this in your class that produces log entries
    /// </summary>
    /// <param name="e"></param>
    protected virtual void Log(LoggingEventArgs e) {
      // if nobody could be bothered to attach their own events, we should
      // still log things in the default way
      if (Logging == null)
        Logging += new LoggingEventHandler(KrakenLoggingService.Default.Log);
      if (Logging != null)
        Logging(this, e);
    }
    protected virtual void Log(string msg) {
      Log(msg, TraceSeverity.Verbose, EventSeverity.Verbose);
    }
    protected virtual void Log(string msg, TraceSeverity traceLevel, EventSeverity eventLevel) {
      LoggingEventArgs e = new LoggingEventArgs(LOGGING_CATEGORY, LOGGING_AREA, msg, traceLevel, eventLevel);
      Log(e);
    }
    protected virtual void Log(Exception ex) {
      LoggingEventArgs e = new LoggingEventArgs(LOGGING_CATEGORY, LOGGING_AREA, ex);
      Log(e);
    }

    #endregion Logging Code Pattern

  } // class

} // namespace
