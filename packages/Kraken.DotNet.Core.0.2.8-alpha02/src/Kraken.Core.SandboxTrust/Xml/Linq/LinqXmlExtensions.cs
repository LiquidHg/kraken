namespace Kraken.Xml.Linq {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Xml;
  using System.Xml.Linq;
  using System.Diagnostics.CodeAnalysis;

  public static class XmlLinqExtensions {

    public static XAttribute TryCloneAttribute(this XElement element, XName name) {
      if (element.Attribute(name) == null)
        return null;
      return new XAttribute(element.Attribute(name));
    }
    public static XAttribute TryCloneAttribute(this XElement element, XName name, XElement targetElement) {
      if (targetElement == null)
        throw new ArgumentNullException("targetElement");
      XAttribute attrib = TryCloneAttribute(element, name);
      if (attrib != null)
        targetElement.Add(attrib);
      return attrib;
    }

    public static string TryGetAttributeValue(this XElement element, XName name, string defaultValue) {
      if (element.Attribute(name) == null)
        return defaultValue;
      return element.Attribute(name).Value;
    }

    public static bool TryRemoveAttribute(this XElement element, string attributeName) {
      if (element.Attribute(attributeName) == null)
        return true;
      try {
        element.Attribute(attributeName).Remove();
      } catch {
        return false;
      }
      return true;
    }
    public static bool TryRemoveAttribute(this XElement element, string attributeName, bool doTry = true) {
      if (!doTry)
        return false;
      return TryRemoveAttribute(element, attributeName);
    }

    public static XElement TryGetSingleElementByName(this XElement element, string localName) {
      XElement subElement = (from node in element.Descendants()
                             where node.Name.LocalName == localName
                             select node).FirstOrDefault<XElement>();
      return subElement;
    }
    public static bool TryRemoveSingleElementByName(this XElement element, string localName) {
      XElement subElement = element.TryGetSingleElementByName(localName);
      if (subElement != null) {
        subElement.Remove();
        return true;
      }
      return false;
    }

    public static XElement ToXElement(this XmlNode node) {
      return GetXElement(node, XElementCreationMethod.UseWriter);
    }
    public static XElement GetXElement(this XmlNode node, XElementCreationMethod method) {
      if (node == null)
        return null;
      switch (method) {
        case XElementCreationMethod.UseWriter:
          XDocument xDoc = new XDocument();
          using (XmlWriter xmlWriter = xDoc.CreateWriter())
            node.WriteTo(xmlWriter);
          return xDoc.Root;
        case XElementCreationMethod.UseParser:
          return XElement.Parse(node.OuterXml);
        case XElementCreationMethod.UseConstructor:
          return new XElement(node.Name, node.InnerXml);
        default:
          return null;
      }
    }

    public static XmlNode ToXmlNode(this XElement element) {
      if (element == null)
        return null;
      using (XmlReader xmlReader = element.CreateReader()) {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(xmlReader);
        return xmlDoc;
      }
    }

    /// <summary>
    /// Returns a generic list of all elements where Name.LocalName matches the value provided.
    /// </summary>
    /// <param name="xe"></param>
    /// <param name="elementName"></param>
    /// <returns></returns>
    public static List<XElement> GetAllElementsOfType(this XElement xe, string elementName) {
      if (xe == null)
        return null;
      List<XElement> ctList = (from XElement ct in xe.DescendantsAndSelf()
                               where ct.Name.LocalName.Equals(elementName, StringComparison.InvariantCulture)
                               select ct).ToList();
      return (List<XElement>)ctList;
    }

    /// <summary>
    /// Removes namespaces from XML code so that it is much less annoying to work with.
    /// Not appropriate in all cases; use with care.
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public static XElement StripSchema(this XElement element) {
      foreach (XElement e in element.DescendantsAndSelf()) {
        if (e.Name.Namespace != XNamespace.None) {
          e.Name = XNamespace.None.GetName(e.Name.LocalName);
        }
        if (e.Attributes().Where(a => a.IsNamespaceDeclaration || a.Name.Namespace != XNamespace.None).Any()) {
          e.ReplaceAttributes(e.Attributes().Select(a => a.IsNamespaceDeclaration ? null : a.Name.Namespace != XNamespace.None ? new XAttribute(XNamespace.None.GetName(a.Name.LocalName), a.Value) : a));
        }
      }
      return element;
    }

    #region Legacy Support Functions

    internal const string XML_DOC_HEAD = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>";

    /// <summary>
    /// Removes schemas and stuff from XML
    /// </summary>
    /// <remarks>
    /// I just get so sick of this crap that comes back from web services
    /// with namespaces on it that make it next to impossible to get the
    /// XPath syntax correct! This method strips those namespaces out.
    /// </remarks>
    /// <param name="xml"></param>
    /// <returns></returns>
    [Obsolete("If possible use System.Xml.Linq and XElement queries instead.")]
    public static XmlDocument CreateCleanXmlDocument(this XmlNode xml) { // , List<string> nameSpacesToRemove
      /*
      XmlDocument xmlDoc = new XmlDocument();
      string strippedXml = xml.OuterXml;
      foreach (string nameSpaceToRemove in nameSpacesToRemove) {
        strippedXml = strippedXml.Replace("xmlns=\"" + nameSpacesToRemove + "\"", string.Empty);
      }
       */
      XmlDocument xmlDoc = new XmlDocument();
      string strippedXml = xml.ToXElement().StripSchema().ToString();
      xmlDoc.LoadXml(XML_DOC_HEAD + strippedXml);
      return xmlDoc;
    }

    [Obsolete("If possible use System.Xml.Linq and XElement queries instead.")]
    public static void RemovePrefixes(this XmlNode node) {
      node.Prefix = "";
      foreach (XmlNode subNode in node.ChildNodes) {
        RemovePrefixes(subNode);
      }
    }

    #endregion

  }

  /*
  class Program {

      [TestMethod()]
      static void Main(string[] args) {
          XElement e = new XElement("Root",
              new XElement("Child",
                  new XAttribute("Att", "1")
              )
          );

          XmlNode xmlNode = e.GetXmlNode();
          Console.WriteLine(xmlNode.OuterXml);

          XElement newElement = xmlNode.GetXElement();
          Console.WriteLine(newElement);
      }
  }
   */

  public enum XElementCreationMethod {
    UseWriter,
    UseConstructor,
    UseParser
  }

} // namespace