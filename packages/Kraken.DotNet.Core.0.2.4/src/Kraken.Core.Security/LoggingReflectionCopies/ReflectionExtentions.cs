using System;
using System.Collections.Generic;
#if !DOTNET_V35
using System.Linq;
#endif
using System.Reflection;
using System.Text;

#if DOTNET_V35
/*
// HACK Work around "Cannot define a new extension method because the compiler required type 'System.Runtime.CompilerServices.ExtensionAttribute' cannot be found. Are you missing a reference?"
namespace System.Runtime.CompilerServices {
  public class ExtensionAttribute : Attribute { }
}
 */
#endif

namespace Kraken.Reflection {

  public static class ReflectionExtensions {

    public static string GetName(this MethodBase method) {
      if (method.DeclaringType == null)
        return method.Name;
      return method.DeclaringType.Name + "::" + method.Name;
    }

  } // class
} // namesapce
