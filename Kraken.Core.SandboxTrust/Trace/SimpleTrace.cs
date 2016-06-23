using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Kraken.Tracing
{
    public class SimpleTrace : ITrace
    {
        public SimpleTrace()
        {
            Handler = (level, msg) =>
            {
                if (level == TraceLevel.Warning)
                {
                    TraceWarning(msg);
                }
                if (level == TraceLevel.Error)
                {
                    TraceError(msg);
                }
                if (level == TraceLevel.Info)
                {
                    TraceInfo(msg);
                }
                if (level == TraceLevel.Verbose)
                {
                    TraceVerbose(msg);
                }
            };
        }

        public void Trace(TraceLevel level, string format, params object[] args) {
          string fmt = Enum.GetName(typeof(TraceLevel), level).ToUpper() + ": {0}";
          Console.WriteLine(fmt, string.Format(format, args));
        }

        public void TraceInfo(string format, params object[] args)
        {
          Trace(TraceLevel.Info, format, args);
        }

        public void TraceError(string format, params object[] args)
        {
          if (!this.SilenceErrors)
          Trace(TraceLevel.Error, format, args);
        }

        public void TraceError(Exception ex)
        {
          if (!this.SilenceErrors)
            TraceError(ex.Message);
        }

        public void TraceWarning(string format, params object[] args)
        {
          if (!this.SilenceWarnings)
            Trace(TraceLevel.Warning, format, args);
        }

        public void TraceVerbose(string format, params object[] args)
        {
          Trace(TraceLevel.Verbose, format, args);
        }

        public void TraceObject(object obj)
        {
            Console.WriteLine("OBJECT: {0}", obj.ToString());
        }

        public Action<TraceLevel, string> Handler { get; set; }

        public TraceLevel Level { get; set; }

        public bool SilenceErrors { get; set; }
        public bool SilenceWarnings { get; set; }

    }
}
