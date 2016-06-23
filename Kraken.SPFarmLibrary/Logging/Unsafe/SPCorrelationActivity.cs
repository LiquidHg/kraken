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
  using System.Diagnostics;
  using System.Diagnostics.Eventing;
  using System.Runtime.InteropServices;
  //using System.Security.AccessControl;
  //using System.Security.Principal;
  using System.Threading;

  using Microsoft.SharePoint.Administration;

  /// <summary>
  /// This class provides a simple API to configure the correlation guid.
  /// The SPDiagnosticsServiceBase doesn't provide API's to manipulate the corelation guid.
  /// Use this class with care, if used arbitrarily, it can make the logs harder to understand.
  /// Always dispose the object from the same thread as soon as possible.
  /// </summary>
  /// <example>
  /// using (SPCorrelationActivity activity = new SPCorrelationActivity()) {
  ///   // do some stuff here that will have the same correlation ID
  /// }
  /// </example>
  public class SPCorrelationActivity : IDisposable {

    class NativeMethods {
      [DllImport("advapi32.dll")]
      public static extern uint EventActivityIdControl(uint controlCode, ref Guid activityId);
      public const uint EVENT_ACTIVITY_CTRL_GET_ID = 1;
    }

    private Guid _previousGuid;
    private Guid _thisGuid;
    private int _parentThread;

    public SPCorrelationActivity() : this(Guid.NewGuid()) { }
    public SPCorrelationActivity(Guid newGuid) {
      _thisGuid = newGuid;
      NativeMethods.EventActivityIdControl(NativeMethods.EVENT_ACTIVITY_CTRL_GET_ID, ref _previousGuid);
      EventProvider.SetActivityId(ref newGuid);
      _parentThread = Thread.CurrentThread.ManagedThreadId;
    }
    ~SPCorrelationActivity() { Dispose(); }

    public Guid Id { get { return _thisGuid; } }
    public Guid PreviousId { get { return _previousGuid; } }
    static public Guid CurrentId {
      get {
        Guid g = Guid.Empty;
        NativeMethods.EventActivityIdControl(NativeMethods.EVENT_ACTIVITY_CTRL_GET_ID, ref g);
        return g;
      }
    }

    public void Dispose() {
      Debug.Assert(Thread.CurrentThread.ManagedThreadId == _parentThread, "the Activity object should be explicitly Disposed by the same thread that created it");
      EventProvider.SetActivityId(ref _previousGuid); GC.SuppressFinalize(this);
    }

  }
}
