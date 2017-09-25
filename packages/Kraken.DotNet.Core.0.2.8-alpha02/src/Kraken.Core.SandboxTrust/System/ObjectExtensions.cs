using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System {
  public static class ObjectExtensions {

    #region HasValue / IsEmpty / IsDefault

    public static bool HasValue(this object o) {
      return (o != null);
    }
    public static bool IsEmpty(this object o) {
      return (o == null);
    }

    public const string SKIP_PROPERTY = "[SKIP_PROPERTY]";

    /// <summary>
    /// Checks a string for data using IsNullOrWhiteSpace;
    /// if supportsSkipProperty is true, then the const
    /// SKIP_PROPERTY is considered the "empty" value
    /// and an empty string is considered to have meaning.
    /// </summary>
    /// <param name="s"></param>
    /// <param name="supportsSkipProperty"></param>
    /// <returns></returns>
    public static bool HasValue(this string s, bool supportsSkipProperty = false) {
      if (supportsSkipProperty)
        return (s != null && !s.Equals(SKIP_PROPERTY));
      return (!string.IsNullOrWhiteSpace(s));
    }
    /// <summary>
    /// Checks a string for data using IsNullOrWhiteSpace;
    /// if supportsSkipProperty is true, then the const
    /// SKIP_PROPERTY is considered the "empty" value
    /// and an empty string is considered to have meaning.
    /// </summary>
    /// <param name="s"></param>
    /// <param name="supportsSkipProperty"></param>
    /// <returns></returns>
    public static bool IsEmpty(this string s, bool supportsSkipProperty = false) {
      if (supportsSkipProperty)
        return (s != null && s.Equals(SKIP_PROPERTY));
      return string.IsNullOrWhiteSpace(s);
    }

#if DOTNET_V35
    /// <summary>
    /// A version of IsNullOrWhiteSpace
    /// to support folks pre .NET 4.0
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static bool IsNullOrWhiteSpace(this string s) {
      if (string.IsNullOrEmpty(s))
        return true;
      return (string.IsNullOrEmpty(s.Trim()));
    }
#endif

    /// <summary>
    /// Returns true when the variable
    /// has been changed from its default(Type).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public static bool HasValue<T>(this T t) where T : struct { // 
      return (!t.Equals(default(T)));
    }

    /// <summary>
    /// Returns true when the variable
    /// matches its own default(Type).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public static bool IsDefault<T>(this T t) where T : struct {
      return (t.Equals(default(T)));
    }

    #endregion

    /// <summary>
    /// Non-generic version of Enum.TryParse<>
    /// </summary>
    /// <param name="t"></param>
    /// <param name="value"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TryParseEnum(this Type t, string value, out Enum result) {
      result = null;
      if (t.IsEnum)
        return false;
      try {
        object o = Enum.Parse(t, value);
      } catch {
        return false;
      }
      return true;
    }

    /// <summary>
    /// Returns true if the object o is either the 
    /// same type or am inherited subtype of Type t.
    /// </summary>
    /// <param name="o"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public static bool IsTypeOrSubtypeOf(this object o, Type t) {
      Type ot = o.GetType();
      return (ot.IsSubclassOf(t) || t == ot);
    }

  } // class
}
