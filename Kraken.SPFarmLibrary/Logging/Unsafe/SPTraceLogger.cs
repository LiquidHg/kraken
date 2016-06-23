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
  /// This class demonstrates API's to interact with the SharePoint tracing service 
  /// and could be used by new development targeting SharePoint 14.
  /// When possible, it is preferred to use SPDiagnosticsServiceBase:
  /// it provides integration with the Central Admin, and honors the admin configured logging levels.
  /// </summary>
  public class SPTraceLogger : EventProvider {

    private static string s_ExeName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
    private const uint TRACE_VERSION_CURRENT = 2;
    private const uint TRACE_FLUSH_TAG = 0xFFFFFFFFu;
    private const TraceSeverity TRACE_FLUSH_LEVEL = 0;
    private const string TRACE_FLUSH_EVENT_NAME = "Global\\Tracing_Service_Flush_Event";

    public SPTraceLogger() : base(SPFarm.Local.TraceSessionGuid) { }

    private void WriteImpl(TraceSeverity level, Payload payload) {
      EventDescriptor descriptor = new EventDescriptor(0, 0, 0, (byte)level, 0, 0, 0);

      using (EVENT_DATA_DESCRIPTOR data = new EVENT_DATA_DESCRIPTOR(payload))
      using (DataDescriptorWrapper wrapper = new DataDescriptorWrapper(data)) {
        bool fResult = WriteEvent(ref descriptor, 1, wrapper.Ptr);
        if (!fResult)
          Console.WriteLine("Failed to call WriteEvent for real payload {0}", Marshal.GetLastWin32Error());
      }
    }

    private void Write(TraceFlags flags, uint id, TraceSeverity level, string exeName, string area, string category, string message) {
      Payload payload = new Payload() {
        Size = (ushort)Marshal.SizeOf(typeof(Payload)),
        dwVersion = TRACE_VERSION_CURRENT,
        Id = id,
        TimeStamp = DateTime.Now.ToFileTime(),
        wzExeName = exeName,
        wzProduct = area,
        wzCategory = category
      };
      // if the message is smaller than 800 characters, no need to break it up 
      if (message == null || message.Length <= 800) {
        payload.wzMessage = message;
        payload.dwFlags = flags;
        WriteImpl(level, payload); return;
      }
      // for larger messages, break it into 800 character chunks 
      for (int i = 0; i < message.Length; i += 800) {
        int cchRemaining = Math.Min(800, message.Length - i);
        payload.wzMessage = message.Substring(i, cchRemaining);

        if (i == 0)
          payload.dwFlags = TraceFlags.TRACE_FLAG_START | flags;
        else if (i + 800 < message.Length)
          payload.dwFlags = TraceFlags.TRACE_FLAG_MIDDLE | flags;
        else payload.dwFlags = TraceFlags.TRACE_FLAG_END | flags;

        WriteImpl(level, payload);
      }
    }

    public void Write(uint id, TraceSeverity level, string area, string category, string message) {
      Write(TraceFlags.None, id, level, s_ExeName, area, category, message);
    }

    public void Write(uint id, TraceSeverity level, string exeName, string area, string category, string message) {
      Write(TraceFlags.None, id, level, exeName, area, category, message);
    }

    private void FlushImpl() {
      Write(TraceFlags.TRACE_FLAG_FLUSH, TRACE_FLUSH_TAG, TRACE_FLUSH_LEVEL, "", "", "", " ");
    }

    public bool Flush(int timeout) {
      // special case for timeout = 0; just send the request and immediately return 
      if (timeout == 0) { FlushImpl(); return true; }
      // create the wait handle with appropriate permissions 
      bool fCreatedNew;
      EventWaitHandleSecurity security = new EventWaitHandleSecurity();
      security.SetAccessRule(new EventWaitHandleAccessRule(new SecurityIdentifier(WellKnownSidType.ServiceSid, null), EventWaitHandleRights.Modify, AccessControlType.Allow));
      using (EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset, TRACE_FLUSH_EVENT_NAME, out fCreatedNew, security)) {
        // request the trace service to flush data, and wait for the service to signal completion 
        FlushImpl();
        return waitHandle.WaitOne(timeout);
      }
    }

    public bool Flush() {
      return Flush(5000);
    }

    public static uint TagFromString(string wzTag) {
      System.Diagnostics.Debug.Assert(wzTag.Length == 4);
      return (uint)(wzTag[0] << 24 | wzTag[1] << 16 | wzTag[2] << 8 | wzTag[3]);
    }
  }

}
