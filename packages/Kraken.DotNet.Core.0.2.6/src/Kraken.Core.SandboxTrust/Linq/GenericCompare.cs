using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Linq {

  /// <summary>
  /// Implements a generic IEqualityComparer
  /// see : https://stackoverflow.com/questions/6694508/how-to-use-the-iequalitycomparer
  /// usage
  /// </summary>
  /// <example>
  /// collection = collection
  ///   .Except(ExistedDataEles, new GenericCompare<DataEle>(x=>x.Id))
  ///   .ToList();
  /// </example>
  /// <typeparam name="T"></typeparam>

  public class GenericCompare<T> : IEqualityComparer<T> where T : class {
    private Func<T, object> _expr { get; set; }
    public GenericCompare(Func<T, object> expr) {
      this._expr = expr;
    }
    public bool Equals(T x, T y) {
      var first = _expr.Invoke(x);
      var sec = _expr.Invoke(y);
      if (first != null && first.Equals(sec))
        return true;
      else
        return false;
    }
    public int GetHashCode(T obj) {
      return obj.GetHashCode();
    }
  }
}
