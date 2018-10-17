using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.WebParts.Cloud {

  /// <summary>
  /// Class reverse engineered from .NET internal class System.Web.UI.ScriptKey
  /// </summary>
  public class ScriptKey {
    // Fields
    private bool _isInclude;
    private string _key;
    private Type _type;

    public override string ToString() {
      return GenerateLongKeyName(_type, _key, _isInclude);
    }

    public static string GenerateLongKeyName(Type type, string key, bool isInclude) {
      return (type.Name + "_" + key + (isInclude ? "_Include" : string.Empty)).ToLower();
    }

    public bool IsMatch(string key, bool forceFullMatch) {
      if (forceFullMatch)
        return (this.ToString().Equals(key, StringComparison.InvariantCultureIgnoreCase));
      else
        return (this._key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
    }

    public ScriptKey(Type type, string key, bool isInclude) {
      this._type = type;
      if (string.IsNullOrEmpty(key)) {
        key = null;
      }
      this._key = key;
      this._isInclude = isInclude;
    }

    public override bool Equals(object o) {
      ScriptKey key = (ScriptKey)o;
      return (((key._type == this._type) && (key._key == this._key)) && (key._isInclude == this._isInclude));
    }

    public override int GetHashCode() {
      // reverse engineered from .NET internal class HashCodeCombiner
      return CombineHashCodes(this._type.GetHashCode(), this._key.GetHashCode(), this._isInclude.GetHashCode());
    }

    /// <summary>
    /// Method reverse engineered from .NET internal class HashCodeCombiner
    /// </summary>
    /// <param name="h1"></param>
    /// <param name="h2"></param>
    /// <returns></returns>
    internal static int CombineHashCodes(int h1, int h2) {
      return (((h1 << 5) + h1) ^ h2);
    }
    /// <summary>
    /// Method reverse engineered from .NET internal class HashCodeCombiner
    /// </summary>
    /// <param name="h1"></param>
    /// <param name="h2"></param>
    /// <param name="h3"></param>
    /// <returns></returns>
    internal static int CombineHashCodes(int h1, int h2, int h3) {
      return CombineHashCodes(CombineHashCodes(h1, h2), h3);
    }

  }

 

}
