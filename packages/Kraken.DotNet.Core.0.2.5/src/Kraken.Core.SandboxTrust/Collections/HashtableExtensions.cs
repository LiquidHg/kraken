using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections {
  public static class KrakenHashtableExtensions {

    /// <summary>
    /// Outputs the keys of a hash table as a string array.
    /// </summary>
    /// <param name="ht"></param>
    /// <returns></returns>
    public static string[] KeysAsArray(this Hashtable ht) {
      string[] keyArray = new string[ht.Keys.Count];
      int i = 0;
      foreach (string k in ht.Keys) {
        keyArray[i++] = k;
      }
      return keyArray;
    }

  }

}
