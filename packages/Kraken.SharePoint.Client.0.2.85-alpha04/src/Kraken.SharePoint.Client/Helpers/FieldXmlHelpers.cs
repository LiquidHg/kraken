using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using Kraken;
using Kraken.Xml;

namespace Kraken.SharePoint.Client {
  public static class FieldXmlHelpers {

    private static MultiParser Parser = new MultiParser();

    /*
    public static void ReadAttribute<T>(this XmlElement element, XmlNullable<T> t, string attrName) where T : struct {
      XmlAttribute attr = element.Attributes[attrName];
      if (attr == null) {
        t = null;
      } else {
        t = Parser.Parse<T>(element.Attributes[attrName].Value);
      }
    }
     */
    public static Nullable<T> ReadAttribute<T>(this XmlElement element, Nullable<T> t, string attrName) where T : struct {
      XmlAttribute attr = element.Attributes[attrName];
      if (attr == null) {
        t = null;
      } else {
        t = Parser.Parse<T>(element.Attributes[attrName].Value);
      }
      return t;
    }
    public static string ReadAttribute(this XmlElement element, string attrName) {
      XmlAttribute attr = element.Attributes[attrName];
      if (attr == null)
        return null;
      return element.Attributes[attrName].Value;
    }

  }
}
