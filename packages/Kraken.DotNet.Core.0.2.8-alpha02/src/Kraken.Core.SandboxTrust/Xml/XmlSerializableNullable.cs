using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;

using Kraken;

namespace Kraken.Xml {

  /// <summary>
  /// Resolves the never ending issue about how to serialize Nullable types
  /// without resorting to IXmlSerializable which can be a pain in the butt.
  /// </summary>
  /// <remarks>
  /// More info: http://stackoverflow.com/questions/244953/serialize-a-nullable-int
  /// </remarks>
  /// <typeparam name="T"></typeparam>
  public class XmlNullable<T> where T : struct { //, IXmlSerializable

    protected Nullable<T> MakeNullable() {
      Nullable<T> target = new Nullable<T>();
      CopyTo(target);
      return target;
    }
    protected void CopyTo(Nullable<T> target) {
      if (this.HasValue)
        target = this.Value;
      else
        target = null;
    }

    public XmlNullable() {
      _hasValue = false;
    }
    public XmlNullable(T value) {
      _value = value;
      _hasValue = true;
    }
    public XmlNullable(Nullable<T> value) {
      _hasValue = value.HasValue;
      if (value.HasValue)
        _value = value.Value;
    }

    /// <summary>
    /// Tells the XML Serializer not to serialize this when value is null
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool ValueSpecified { get { return this.HasValue; } }

    public T Value {
      get {
        if (!HasValue)
          throw new InvalidOperationException();
        return _value;
      }
      set {
        _value = value;
        _hasValue = true;
      }
    }

    [XmlIgnore]
    public bool HasValue { get { return _hasValue; } }

    public T GetValueOrDefault() { return _value; }
    public T GetValueOrDefault(T i_defaultValue) { return HasValue ? _value : i_defaultValue; }

    // coversions to struct value
    public static explicit operator T(XmlNullable<T> i_value) { return i_value.Value; }
    public static implicit operator XmlNullable<T>(T i_value) { return new XmlNullable<T>(i_value); }

    // conversions to Nullable<T>
    public static implicit operator Nullable<T>(XmlNullable<T> i_value) { return i_value.MakeNullable(); }
    public static implicit operator XmlNullable<T>(Nullable<T> i_value) { return new XmlNullable<T>(i_value); }

    public override bool Equals(object i_other) {
      if (!HasValue)
        return (i_other == null);
      if (i_other == null)
        return false;
      return _value.Equals(i_other);
    }

    public override int GetHashCode() {
      if (!HasValue)
        return 0;
      return _value.GetHashCode();
    }

    public override string ToString() {
      if (!HasValue)
        return string.Empty;
      return _value.ToString();
    }

    [XmlIgnore]
    bool _hasValue;
    [XmlIgnore]
    T _value;

    [XmlIgnore]
    private static MultiParser Parser = new MultiParser();

    public void ReadXml(XmlReader reader) {
      string value = reader.ReadString();
      if (string.IsNullOrEmpty(value))
        return; // don't read/write anything
      T t = Parser.Parse<T>(value); // we could do TryParse instead, but here it should be OK to throw the error
    }

    public void WriteXml(XmlWriter writer) {
      // TODO do we need an attribute name here?
      if (this.HasValue)
        writer.WriteString(this.ToString());
    }

    public XmlSchema GetSchema() {
      return (null);
    }

  }

}
