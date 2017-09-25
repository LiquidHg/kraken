using System;
using System.Globalization;
using System.Text;
// used for merge fieldName replacement
using System.Data;
using System.Collections;
using System.Collections.Specialized;

namespace Kraken {

	/// <summary>
	/// StringTools is a static library of useful string functions.
	/// There are more where this came from, including some for "unsafe"
	/// string manipulations, scrambling/memory obfuscation (gov format),
	/// and SecureString marshalling for .NET 2.0. I will import these
	/// as needed and when I have time. - Tom Carpe 3/15/07
	/// </summary>
	public static class StringTools {

    static StringTools() {}

#if _PreDotNet2_
    // Nobody cares about this anymore, circa 2005

    /// <summary>
    /// Provides support for a "shorthand" function similar to 
    /// string.IsNullOrEmpty that is provided in .NET 2.0.
    /// </summary>
    /// <param name="test"></param>
    /// <returns></returns>
    public static bool IsNullOrEmpty(string test) {
      //return string.IsNullOrEmpty(test);
      return (test == null || test == string.Empty);
    }
#endif

#if DOTNET_V35
    public static bool IsNullOrWhiteSpace(string text) {
      if (string.IsNullOrEmpty(text))
        return true;
      // TODO to match .NET 4.0 functionality do we need to modify the default whitespace chars?
      if (string.IsNullOrEmpty(text.Trim()))
        return true;
      return false;
    }
    
    public static bool GuidTryParse(string text, out Guid value) {
      value = Guid.Empty;
      try {
        value = new Guid(text);
        return (value != null);
      } catch {
        return false;
      }
    }
    public static bool EnumTryParse<EnumType>(string text, out EnumType value) {
      value = default(EnumType);
      try {
        object o = Enum.Parse(typeof(EnumType), text); // Type enumType, 
        if (o != null)
          value = (EnumType)o;
        return (o != null);
      } catch {
        return false;
      }
    }
#endif

    /// <summary>
    /// This function takes a string, breaks it into individual words and
    /// re-cases those words based on standard English rules for capitalizing 
    /// proper names. Includes support for "Mc", "Mac", and apostrophies like 
    /// "O'" and "D'".
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string ProperCase(this string name) {
      string[] prefixes = new string[] { "Mc", "Mac" };
      string casedName = string.Empty;
      bool capNextChar = true;
      for (int i = 0; i < name.Length; i++) {
        if (capNextChar)
          casedName += char.ToUpper(name[i]);
        else
          casedName += char.ToLower(name[i]);
        capNextChar = (!char.IsLetterOrDigit(name[i])); // char.IsWhiteSpace(name[i]) || name[i] == '\'
        foreach (string prefix in prefixes) {
          if (casedName.EndsWith(prefix))
            capNextChar = true;
        }
      }
      return casedName;
    }

    /// <summary>
    /// A very basic and error prone way to find the text that is between two "bookend" strings.
    /// If you want a real routine, use RegEx.
    /// </summary>
    /// <param name="searchText"></param>
    /// <param name="atStart"></param>
    /// <param name="atEnd"></param>
    /// <returns></returns>
    public static string LookForInnerText(this string searchText, string atStart, string atEnd) {
      int start = searchText.IndexOf(atStart);
      if (start >= 0) {
        start += atStart.Length;
        int end = searchText.IndexOf(atEnd, start);
        if (end >= 0) {
          int length = end - start;
          return searchText.Substring(start, length);
        }
      }
      return string.Empty;
    }

    /// <summary>
    /// For all I know the .NET framework has something in it that can do this... but
    /// Turns escaped sequences like \r\n into their character-code equivalents.
    /// </summary>
    /// <param name="text">Escaped text string</param>
    /// <returns>Converted string with unescaped charaters.</returns>
    public static string ReplaceEscapeSequences(this string text) {
      StringBuilder sb = new StringBuilder();
      // There is an Orwellian joke here... ;-P
      for (int unGood = 0; unGood < text.Length; ++unGood) {
        if (text[unGood] == '\\' && unGood < text.Length - 1) { // not the last character
          char nextChar = text[++unGood];
          switch (nextChar) {
            case 't':
              sb.Append('\t');
              break;
            case 'r':
              sb.Append('\r');
              break;
            case 'n':
              sb.Append('\n');
              break;
            case '\\':
              sb.Append('\\');
              break;
            default:
              throw new ArgumentException(
                string.Format(
                  CultureInfo.CurrentCulture,
                  "Unrecognized character escape sequence: '\\{0}'",
                  nextChar));
          }
        } else
          sb.Append(text[unGood]);
      }
      return sb.ToString();
    }

    // TODO do something with Regex instead
    #region Merge Fields

    public const string MergeDelimeter = "%";

#if DOTNET_V35
    public static string MergeFields(this string text, IDictionary fields) {
      return MergeFields(text, fields, string.Empty);
    }
    public static string MergeFields(this string text, IDictionary fields, string delimeter) {
#else
    public static string MergeFields(this string text, IDictionary fields, string delimeter = "") {
#endif
      if (string.IsNullOrEmpty(delimeter))
        delimeter = MergeDelimeter;
      foreach (DictionaryEntry item in fields) {
        text = text.Replace(
          delimeter + item.Key.ToString() + delimeter,
          item.Value.ToString()
        );
      }
      return text;
    }
#if DOTNET_V35
    public static string MergeFields(this string text, DataRow fieldsRow) {
      return MergeFields(text, fieldsRow, string.Empty);
    }
    public static string MergeFields(this string text, DataRow fieldsRow, string delimeter) {
#else
    public static string MergeFields(this string text, DataRow fieldsRow, string delimeter = "") {
#endif
      if (string.IsNullOrEmpty(delimeter))
        delimeter = MergeDelimeter;
      foreach (DataColumn col in fieldsRow.Table.Columns) {
        string key = col.ColumnName;
        string value = fieldsRow.ItemArray[col.Ordinal].ToString();
        text = text.Replace(delimeter + key + delimeter, value);
      }
      return text;
    }

    #endregion

    public static string TrimStart(this string target, string trimString) {
      string result = target;
      while (result.StartsWith(trimString)) {
        result = result.Substring(trimString.Length);
      }
      return result;
    }

    public static string TrimEnd(this string target, string trimString) {
      string result = target;
      while (result.EndsWith(trimString)) {
        result = result.Substring(0, result.Length - trimString.Length);
      }
      return result;
    }

  } // class

} // namespace
