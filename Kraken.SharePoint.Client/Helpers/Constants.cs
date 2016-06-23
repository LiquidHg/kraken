using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client.Helpers
{
    public static class Constants
    {

        public static class Inrevals
        {
            public const int DefaultHttpRequestTimeout = 100000;

            /// <summary>
            /// A typical number of MB/second on O365; used to calculate timeouts
            /// </summary>
            public const int SpeedOffice365KBPerSecond = 250;

            public const double HugeFileTimeOutMultiplier = 1.5; // 1.25 was good for a while, but when things get slow it causes significant timeouts
            //public const int HugeFileTimeOut = 300000; // 5 minutes!
        }

        public static class Values
        {
            public const int QueryPageSize = 2500;
        }
    }
}
