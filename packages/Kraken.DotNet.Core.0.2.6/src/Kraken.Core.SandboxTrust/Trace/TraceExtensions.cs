using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Kraken.Tracing;
using Kraken.Reflection;

namespace Kraken.Tracing {
  public static class TraceExtensions {

    public static ITrace Ensure(this ITrace trace) {
      if (trace == null) trace = NullTrace.Default;
      return trace;
    }
    public static void Enter(this ITrace trace, MethodBase method, string extraMsg, params string[] args) {
      Enter(trace, method, string.Format(extraMsg, args));
    }
    public static void Enter(this ITrace trace, MethodBase method, string extraMsg = "") {
      trace.TraceVerbose("Entering '{0}'{1}{2}.", method.GetName(), (string.IsNullOrEmpty(extraMsg) ? " " : string.Empty), extraMsg);
      trace.Depth++;
    }

    public static void Exit(this ITrace trace, MethodBase method, string extraMsg, params string[] args) {
      Exit(trace, method, string.Format(extraMsg, args));
    }
    public static void Exit(this ITrace trace, MethodBase method, string extraMsg = "") {
      trace.Depth--;
      trace.TraceVerbose("Exiting '{0}'{1}{2}.", method.GetName(), (string.IsNullOrEmpty(extraMsg) ? " " : string.Empty), extraMsg);
    }

  }
}
