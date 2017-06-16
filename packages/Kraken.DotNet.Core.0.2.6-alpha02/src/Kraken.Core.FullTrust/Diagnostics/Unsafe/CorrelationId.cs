using System;
using System.Collections.Generic;
#if !DOTNET_V35
using System.Linq;
#endif
using System.Text;
using System.Runtime.InteropServices;
//from http://zimmergren.net/technical/sp-2010-find-error-messages-with-a-correlation-id-token-in-sharepoint-2010

namespace Kraken.Core.Diagnostics.Unsafe
{
    public unsafe class CorrelationId
    {
        [DllImport("advapi32.dll")]
        public static extern uint EventActivityIdControl(uint controlCode, ref  Guid activityId);
        public const uint EVENT_ACTIVITY_CTRL_GET_ID = 1;

        public static Guid GetCurrentCorrelationToken()
        {
            Guid g = Guid.Empty;
            try
            {
                EventActivityIdControl(EVENT_ACTIVITY_CTRL_GET_ID, ref  g);
            }
            finally { }
            return g;
        }
    }
}
