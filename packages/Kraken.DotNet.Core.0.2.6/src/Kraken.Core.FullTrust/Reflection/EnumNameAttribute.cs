using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Reflection {

  /// <summary>
  /// Use this attribute to tag the individual values of an enum type
  /// with friendly names that can be looked up later using GetEnumName
  /// and ParseEnumFromDisplayName methods of Reflector class.
  /// </summary>
  public class EnumNameAttribute : Attribute {
    public string Text;
    public EnumNameAttribute(string text) {
      this.Text = text;
    }
  }

}
