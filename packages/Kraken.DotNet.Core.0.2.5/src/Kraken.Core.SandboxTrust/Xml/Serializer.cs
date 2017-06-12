// ----------------------------------------------------------------------------
// This file is lifted from my personal code library, but you can use it. :-)
//
// dotNet Development Tools. (c)2003-2007 Thomas Carpe. All Rights Reserved.
// Contact me at: www.Kraken.org or dotnet@Kraken.org.
//
// By providing you with this source code, I have granted you a license to
// use and modify this work for your own purposes, whether personal or
// commercial in nature, provided that you leave whole and intact the contents
// of this notice and do not attempt to sell or distribute this software to any
// third-party.
//
// This code is provided "AS IS". No warranty or guarantee that it is free
// of defects or fit for any particular purpose is express or implied.
// ----------------------------------------------------------------------------

namespace Kraken.Xml.Serialization {

  using System;
  using System.IO;
  using System.Text;
  using System.Xml;
  using System.Xml.Schema;
  using System.Xml.Serialization;
  using System.Collections;

  /// <summary>
  /// Inherit this and write your own extensions, or use like so:
  /// 
  /// Serializer ser = new Serializer(typeof(SerializationTarget));
  /// SerializationTarget target;
  /// string xml = ser.Serialize(target);
  /// SerializationTarget target = (SerializationTarget)Deserialize(xml);
  /// </summary>
  public class Serializer {

    public event EventHandler Validate;

    private ArrayList _validationErrors;
    public ArrayList ValidationErrors {
      get {
        if (_validationErrors == null)
          _validationErrors = new ArrayList();
        return _validationErrors;
      }
    }

    public Serializer(Type targetType) {
      _objectType = targetType;
    }

    private const string xmlDeclaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";

    protected Type _objectType;
    public Type ObjectType {
      get { return _objectType; }
    }

    protected Encoding encodingMethod = Encoding.UTF8;
    public Encoding EncodingMethod {
      get { return encodingMethod; }
      set { encodingMethod = value; }
    }

    #region Main Methods

    /// <summary>
    /// Use this method to convert an object into its serialized XML.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public string Serialize(object target) {
      XmlSerializer serializer = new XmlSerializer(_objectType);
      MemoryStream stream = new MemoryStream(); // read xml in memory
      StreamWriter writer = new StreamWriter(stream, this.EncodingMethod);
      try {
        serializer.Serialize(writer, target);
        stream = (MemoryStream)writer.BaseStream;
        string xml = ByteArrayToString(stream.ToArray());
        // HACK for some strange reason the serializer sometimes preceeds xml with a "?" question mark
        if (!xml.StartsWith("<"))
          xml = xml.Substring(1);
        // END HACK
        return xml;
      } catch {
        throw;
      } finally {
        if (stream != null) stream.Close();
        if (writer != null) writer.Close();
      }
    }

    public object Deserialize(string xml) {
      return Deserialize(xml, false);
    }
    /// <summary>
    /// Use this method to convert serialized XML back into a <strikethrough>real boy</strikethrough> object.
    /// </summary>
    /// <param name="xml"></param>
    /// <param name="isRawValue">Set to true if the xml contains the raw string to be parsed into a ValueType</param>
    /// <returns></returns>
    public object Deserialize(string xml, bool isRawValue) {
      if (!isRawValue) {
        // HACK for some strange reason the serializer sometimes preceeds xml with a "?" question mark
        if (!xml.StartsWith("<") && xml[1] == '<')
          xml = xml.Substring(1);
        if (!xml.StartsWith("<?"))
          xml = xmlDeclaration + xml;
      }
      XmlSerializer serializer = new XmlSerializer(_objectType);
      using (StringReader stream = new StringReader(xml)) { // read xml data
        // proivde a method for us to create custom serializer classes that validate our XML for us :-)
        if (!isRawValue)
          OnValidation(xml);
        XmlTextReader reader = new XmlTextReader(stream);  // create reader
        try {
          object target = serializer.Deserialize(reader);
          return target;
        } catch (InvalidOperationException ex) {
          throw new XmlException("Error in Xml format: " + xml, ex);
        } finally {
          if (stream != null) stream.Close();
          if (reader != null) reader.Close();
        }
      }
    }

    #endregion

    // TODO make the Xml validation logic and event model here actually useful
    // TODO it would also be awesome if we could use this in all cases where we have Xml passed as a string and we want to check it for proper format and schema compliance, not just in the content of serialization
    #region XML Validation Methods and Event Model

    public ArrayList DoValidate(string xml, string schemaUrl, string schemaFilePath) {
      try {

        // Set the schema type and add the schema to the reader.
        XmlSchemaSet myschema = new XmlSchemaSet();
        myschema.Add(schemaUrl, schemaFilePath);
        ValidationEventHandler eventHandler = null; //= new ValidationEventHandler(Class1.ShowCompileErrors);
        XmlReaderSettings settings =new XmlReaderSettings() {
          ValidationType = ValidationType.Schema,
          Schemas = myschema
        };
        if (eventHandler != null)
          settings.ValidationEventHandler += eventHandler;
        // Implement the reader
        using (StringReader stringReader = new StringReader(xml)) {
          XmlReader vReader = XmlReader.Create(stringReader, settings);
          while (vReader.Read()) { }
        }
      } catch (XmlException XmlExp) {
        this.ValidationErrors.Add(XmlExp.Message);
      } catch (XmlSchemaException XmlSchExp) {
        this.ValidationErrors.Add(XmlSchExp.Message);
      } catch (Exception GenExp) {
        this.ValidationErrors.Add(GenExp.Message);
      }
      return this.ValidationErrors;
    }

    protected virtual ArrayList OnValidation(string xml) {
      if (Validate != null)
        Validate(this, new EventArgs());
      /*
      if (_objectType == typeof(Constellation.CCG.Tools.XtraHelpers.PivotSettings.PivotSettings))
        DoValidate(xml, "http://tempuri.org/PivotSettings.xsd", @"C:\VS Projects\FOITWeb\FrontOfficeLibrary\XtraHelpers\PivotSettings.xsd");
      if (_objectType == typeof(Constellation.CCG.Tools.Data.QueryLibraryDataSetLogic))
        DoValidate(xml, "http://tempuri.org/QueryLibraryDataSet.xsd", @"C:\VS Projects\FOITWeb\FrontOfficeLibrary\Data\QueryLibraryDataSet.xsd");
      */
      return this.ValidationErrors;
    }

    #endregion

    // TODO move these into a different library class
    #region Static String to Stream Conversion Methods

    /// <summary>
    /// Creates a MemoryStream and pumps a string into it.
    /// </summary>
    /// <param name="encoding">Optional encoding type, default is UTF8</param>
    /// <param name="data">Data to input into the stream</param>
    /// <returns></returns>
    public static MemoryStream StringToStream(string data, Encoding encoding) {
      MemoryStream stream = new MemoryStream();
      Byte[] arr = encoding.GetBytes(data);
      for (long i = 0; i < arr.LongLength; i++)
        stream.WriteByte(arr[i]);
      return stream;
    }
    public static MemoryStream StringToStream(string data) {
      return StringToStream(data, Encoding.UTF8);
    }

    /// <summary>
    /// Takes a MemoryStream and reads it into a string.
    /// </summary>
    /// <param name="encoding">Optional encoding type, default is UTF8</param>
    /// <param name="stream">Stream to read into a string</param>
    /// <returns></returns>
    public static string StreamToString(MemoryStream stream, Encoding encoding) {
      Byte[] characters = stream.ToArray();
      string constructedString = encoding.GetString(characters);
      return (constructedString);
    }
    public static string StreamToString(MemoryStream stream) {
      return StreamToString(stream, Encoding.UTF8);
    }

    #endregion

    #region Other Type Conversions

    private string ByteArrayToString(Byte[] characters) {
      string constructedString = this.EncodingMethod.GetString(characters);
      return (constructedString);
    }

    private Byte[] StringToByteArray(string pXmlString) {
      Byte[] byteArray = this.EncodingMethod.GetBytes(pXmlString);
      return byteArray;
    }

    #endregion

  } // class SerializationEngine

} // namespace