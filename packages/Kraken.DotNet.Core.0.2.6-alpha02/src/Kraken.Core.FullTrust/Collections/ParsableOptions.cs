namespace System.Collections {

  using System.Reflection;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;

  /// <summary>
  /// A base class that can be used
  /// to support options and properties
  /// where conversion from inbound
  /// sources like PSObject or Hashtable
  /// are required.
  /// This version uses reflection to load
  /// properties from the Hashtable.
  /// </summary>
  public class ParsableOptions : ParsableOptionsBase {

    public ParsableOptions() : base() { }
    public ParsableOptions(Hashtable ht) : base(ht) { }

    /// <summary>
    /// Uses reflection to try and match the
    /// property passed by the constructor
    /// to the properties of this object.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public override bool SetProperty(string propertyName, object value) {
      ReflectionOperationResult result = this.ImportProperty(propertyName, value);
      if (!result.Success)
        ParseMessages.Add(result.Message);
      return result.Success;
    }

  }
}
