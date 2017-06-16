#if !DOTNET_V35
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Configuration;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

using Kraken;
using System.Text.RegularExpressions;

namespace Kraken.Configuration {

  public static class AppSettingsExtensions {

    public static void SaveValues(this ApplicationSettingsBase settings, Dictionary<string, object> newValues, string filename) {
      if (string.IsNullOrEmpty(filename))
        throw new ArgumentNullException("filename");

      XmlTextReader reader = new XmlTextReader(filename);
      XmlDocument doc = new XmlDocument();
      doc.Load(reader);
      XmlElement root = doc.DocumentElement;

      foreach (string key in newValues.Keys) {
        XmlNode settingNode = root.SelectSingleNode(".//setting[@name='" + key + "']");
        if (settingNode == null)
          throw new ArgumentOutOfRangeException(string.Format("'setting' element not found for setting[@name={0}]", key));
        XmlNode valNode = settingNode.SelectSingleNode("./value");
        if (valNode == null)
          throw new ArgumentNullException(string.Format("'value' element not found in setting[@name={0}].", key));
        if (settingNode.Attributes["serializeAs"] == null)
          throw new ArgumentNullException(string.Format("'serializeAs' attribute not found in setting[@name={0}].", key));
        string serializeAs = settingNode.Attributes["serializeAs"].Value;
        switch (serializeAs.ToLower()) {
          case "string":
            valNode.InnerText = newValues[key].ToString();
            break;
          case "xml":
            Type targetType = newValues[key].GetType();
            XmlSerializer serializer = new XmlSerializer(targetType);
            TextWriter writer = new StringWriter();
            serializer.Serialize(writer, newValues[key]);
            string newVal = writer.ToString();
            newVal = Regex.Replace(newVal, @"^[\?]*<\?xml.*?\?>", "");
            valNode.InnerXml = newVal;
            break;
          default:
            throw new NotSupportedException(string.Format("Unsupported serialization type {0}", serializeAs));
        }
      }
      reader.Close();
      doc.Save(filename);
    }

    public static void Load(this ApplicationSettingsBase settings, string filename) {
      if (string.IsNullOrEmpty(filename))
        throw new ArgumentNullException("filename");
      XDocument importFile = XDocument.Load(filename);
      XElement xml = importFile.Element("configuration");
      //string text = System.IO.File.ReadAllText(filename);
      //xml = System.Xml.Linq.XElement.Parse(text);

      string xmlNodeName = settings.Context["GroupName"].ToString();
      if (xml != null) {
        foreach (XElement currentElement in xml.Elements()) {
          // TODO support both of these
          switch (currentElement.Name.LocalName) {
            case "userSettings":
            case "applicationSettings":
              foreach (XElement settingNamespace in currentElement.Elements()) {
                // find the section for our specific project
                if (settingNamespace.Name.LocalName == xmlNodeName) {
                  // loop through its settings, reading each one
                  foreach (XElement setting in settingNamespace.Elements()) {
                    settings.LoadSetting(setting);
                  }
                }
              }
              break;
            default:
              break;
          }
        }
      }
    }

    /// <summary>
    /// Parses out an individual settings node from an XML file and copies it to the target.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="setting"></param>
    public static void LoadSetting(this ApplicationSettingsBase settings, XElement setting) {
      string name = null, serializeAs = null;
      object value = null;

      if (setting.Name.LocalName == "setting") {
        XAttribute xName = setting.Attribute("name");
        if (xName != null)
          name = xName.Value;
        XAttribute xSerialize = setting.Attribute("serializeAs");
        if (xSerialize != null)
          serializeAs = xSerialize.Value;

        XElement xValue = setting.Element("value");
        if (xValue != null) {
          Type targetType = settings[name].GetType();
          if (targetType == typeof(string) && serializeAs.Equals("String", StringComparison.InvariantCultureIgnoreCase)) {
            // These are easy... no conversion is really needed
            value = xValue.Value;
          } else if (serializeAs.Equals("Xml", StringComparison.InvariantCultureIgnoreCase)) {
            // In this case the contents are XML so we can use XmlSerializer
            //XmlReader reader = xValue.CreateReader(); reader.MoveToContent(); string xmlToDeserialize = reader.ReadInnerXml();
            // this gets the node inside of <value></value> and starts from there
            XmlSerializer serializer = new XmlSerializer(targetType);
            XmlReader reader = xValue.DescendantNodes().First().CreateReader();
            value = serializer.Deserialize(reader);
          } else {
            if (!string.IsNullOrWhiteSpace(xValue.Value)) {
              try {
                value = Convert.ChangeType(xValue.Value, targetType);
              } catch {
                value = MultiParser.Default.Parse(xValue.Value, targetType);
              }
            }
            // These are tricky since there is no way to use a generic Parse method.
            // For other primitive types we need to feed the value through XmlSerializer.
            // Easiest way to do this is to use our string deserializer utility class.
          }
        }
      }
      if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(serializeAs) && value != null)
        settings[name] = value;
    }

  }
}
#endif