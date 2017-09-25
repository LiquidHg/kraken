using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Kraken;
using System.Xml;
using System.Security;

namespace Kraken.Xml {

  public static class StringBuilderXmlExtensions {

    // Since nullable strings aren't possible, these are used in certain instances where we need to pass something explicit to tell our code NOT to create an attribute

    public const string EraseToken = "{EMPTY}";

    public static string ReplaceEraseToken(string text) {
      return text.Replace(EraseToken, string.Empty);
    }
    public static string ReplaceEraseToken(object text) {
      return text.ToString().Replace(EraseToken, string.Empty);
    }

    private const string AttributeFormatString = " {0}=\"{1}\"";

    public static void AppendAttribute<T>(this StringBuilder sb, string attributeName, Nullable<T> attributeValue) where T : struct {
      if (!attributeValue.HasValue)
        return;
      string espacedValue = XmlEscape(ReplaceEraseToken(attributeValue.Value));
      sb.AppendFormat(AttributeFormatString, attributeName, espacedValue);
    }
    public static void AppendAttribute<T>(this StringBuilder sb, string attributeName, string attributeValue) {
      if (string.IsNullOrEmpty(attributeValue))
        return;
      string espacedValue = XmlEscape(ReplaceEraseToken(attributeValue));
      sb.AppendFormat(AttributeFormatString, attributeName, ReplaceEraseToken(espacedValue));
    }
    public static void AppendAttribute(this StringBuilder sb, string attributeName, object attributeValue) {
      if (attributeValue == null)
        return;
      string espacedValue = XmlEscape(ReplaceEraseToken(attributeValue));
      sb.AppendFormat(AttributeFormatString, attributeName, ReplaceEraseToken(espacedValue));
    }

    // in this case did not rely on System.Security.SecurityElement.Escape because it did not catch & and other chars
    public static string XmlEscape(string unescaped) {
      XmlDocument doc = new XmlDocument();
      XmlAttribute node = doc.CreateAttribute("foo");
      node.InnerText = unescaped;
      return SecurityElement.Escape(node.InnerXml); // this will take care of quotes among other BS
    }
    public static string XmlUnescape(string escaped) {
      // TODO there must be a better way to do this, no?
      if (escaped == null)
        throw new ArgumentNullException("escaped");
      return escaped.Replace("&apos;", "'").Replace("&quot;", "\"").Replace("&gt;", ">").Replace("&lt;", "<").Replace("&amp;", "&");
    }

    /*
    public static string EscapeXMLValue(string xmlString) {
      if (xmlString == null)
        throw new ArgumentNullException("xmlString");
      return xmlString.Replace("'", "&apos;").Replace("\"", "&quot;").Replace(">", "&gt;").Replace("<", "&lt;").Replace("&", "&amp;");
    }
    */

  }
}
