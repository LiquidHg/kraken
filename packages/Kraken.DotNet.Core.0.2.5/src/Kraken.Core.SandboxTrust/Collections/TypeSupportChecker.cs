using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections {

  /// <summary>
  /// Maintains an array of supported types
  /// and allow you to check (and optionally
  /// trim) any collection of objects for
  /// those that are supported.
  /// </summary>
  public class TypeSupportChecker {

    public TypeSupportChecker(Type[] t) {
      SupportedTypes = t;
    }

    public Type[] SupportedTypes { get; private set; }

    public bool CheckSupport(Type ot, List<string> invalidTypes = null) {
      bool anySupported = false;
      foreach (Type t in SupportedTypes) {
        anySupported |= (ot.IsTypeOrSubtypeOf(t));
      }
      if (!anySupported && invalidTypes != null)
        invalidTypes.Add(ot.FullName);
      return anySupported;
    }
    public bool CheckSupport(object testObject, List<string> invalidTypes = null) {
      Type ot = testObject.GetType();
      return CheckSupport(ot, invalidTypes);
    }

    /// <summary>
    /// Test an array of objects to see if they are supported
    /// </summary>
    /// <param name="testObjects">Array of objects to test</param>
    /// <param name="trimToSupportedOnly">If true, trim the array and return only supported objects</param>
    /// <param name="trace">Trace object to write warnings</param>
    /// <returns>If trimToSupportedOnly, a trimmed version of testObjects, otherwise testObjects</returns>
    public object[] CheckSupport(IEnumerable<object> testObjects, bool trimToSupportedOnly, Kraken.Tracing.ITrace trace = null) {
      if (testObjects == null)
        throw new ArgumentNullException("testObject");
      List<string> invalidTypes = new List<string>();
      List<object> trimmed = new List<object>();
      foreach (object o in testObjects) {
        if (CheckSupport(o, invalidTypes) && trimToSupportedOnly) {
          trimmed.Add(o);
        }
      }
      if (invalidTypes.Count > 0) {
        trace.TraceWarning("One or more types provided in the data set were unsupported. ", (trimToSupportedOnly) ? "Data will be truncated. " : "");
        trace.Depth++;
        foreach (string invalidType in invalidTypes) {
          trace.TraceWarning(invalidType);
        }
        trace.Depth--;
        if (trimToSupportedOnly) {
          trace.TraceWarning("Returning trimmed object array containing only the supported items. Count={0}", trimmed.Count);
          return trimmed.ToArray();
        }
      }
      return testObjects.ToArray();
    }

  }
}
