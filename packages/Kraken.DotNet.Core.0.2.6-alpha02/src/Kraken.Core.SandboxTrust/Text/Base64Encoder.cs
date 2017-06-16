
namespace Kraken.Text {

    using System;
    using System.Collections.Generic;
    using System.Text;

  public class Base64Encoder {

    public Base64Encoder() {
    }

    /*
    public virtual Encoding EncodingType {
      get {
        return Encoding.UTF8;
      }
    }
     */
    private Encoding _encoder;
    public virtual Encoding Encoder {
      get {
        if (_encoder == null)
          _encoder = new UTF8Encoding();
        return _encoder;
      }
    }

    public string EncodeXmlData(string xmlText) {
      byte[] bytes = this.Encoder.GetBytes(xmlText);
      string base64Result = Convert.ToBase64String(bytes);
      return base64Result;
    }
    public string DecodeXmlData(string base64Text) {
      byte[] bytes = Convert.FromBase64String(base64Text);
      Decoder decoder = this.Encoder.GetDecoder();
      int charCount = decoder.GetCharCount(bytes, 0, bytes.Length);
      char[] decodedChar = new char[charCount];
      decoder.GetChars(bytes, 0, bytes.Length, decodedChar, 0);
      string xmlResult = new String(decodedChar);
      return xmlResult;
    }

  } // class

} // namespace
