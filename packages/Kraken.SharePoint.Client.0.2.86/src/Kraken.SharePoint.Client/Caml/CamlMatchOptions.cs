namespace Kraken.SharePoint.Client.Caml {

  using System;
  using System.Collections;

  /// <summary>
  /// Base class to aid in json-like syntax
  /// for generating CAML queries. This is used
  /// because many languages like JavaScript or
  /// PowerShell have an easier time with JSON
  /// than XML.
  /// </summary>
  public abstract class CamlMatchOptions : ParsableOptions {

    /// <summary>
    /// Used when importing from a Hashtable to identify fields
    /// that are explictly targetted for this class.
    /// </summary>
    public const string MatchRulePrefix = "MatchOptions.";

    /// <summary>
    /// Developers should override this method
    /// to generate Caml syntax for <Where></Where>
    /// based on the provided properties.
    /// </summary>
    /// <returns></returns>
    virtual public string ToCamlWhere() {
      throw new NotImplementedException();
    }

  }
}
