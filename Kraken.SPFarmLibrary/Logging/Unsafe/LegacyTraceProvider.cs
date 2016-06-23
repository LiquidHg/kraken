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

namespace Kraken.SharePoint.Logging.Unsafe {

  using System;
  using System.Diagnostics.Eventing;
  using System.Runtime.InteropServices;
  using System.Security.AccessControl;
  using System.Security.Principal;
  using System.Threading;

  using Microsoft.SharePoint.Administration;

  /// <summary>
  /// This class is provided as a drop-in replacement for the O12 code sample on MSDN.
  /// If you never used the O12 sample, you probably wouldn't want to use this;
  /// just call SPTraceLogger directly
  /// </summary>
  public static class LegacyTraceProvider {

    static SPTraceLogger logger = new SPTraceLogger();

    // these are now unnecessary, registration is handled by the SPTraceLogger
    //public static void RegisterTraceProvider() { }
    //public static void UnregisterTraceProver() { }

    public static void WriteTrace(uint tag, TraceSeverity level, Guid correlationGuid, string exeName, string productName, string categoryName, string message) {
      if (correlationGuid == Guid.Empty)
        logger.Write(tag, (TraceSeverity)level, exeName, productName, categoryName, message);
      else {
        using (new SPCorrelationActivity(correlationGuid)) {
          logger.Write(tag, (TraceSeverity)level, exeName, productName, categoryName, message);
        }
      }
    }

    public static uint TagFromString(string wzTag) {
      return SPTraceLogger.TagFromString(wzTag);
    }

  }
}
