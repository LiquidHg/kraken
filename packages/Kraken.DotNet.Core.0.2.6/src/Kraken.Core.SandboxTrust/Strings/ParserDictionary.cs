using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken {

  /// <summary>
  /// Parses many different types without resorting to reflection
  /// </summary>
  public class MultiParser : Dictionary<Type, Delegate> {

    public static MultiParser Default {
      get {
        return new MultiParser();
      }
    }

    public MultiParser() {
      AddParser<decimal>(decimal.TryParse);
      AddParser<float>(float.TryParse);
      AddParser<double>(double.TryParse);
      AddParser<int>(int.TryParse);
      AddParser<long>(long.TryParse);
      AddParser<short>(short.TryParse);
      AddParser<uint>(uint.TryParse);
      AddParser<ulong>(ulong.TryParse);
      AddParser<ushort>(ushort.TryParse);
      AddParser<bool>(bool.TryParse);
      AddParser<byte>(byte.TryParse);
      AddParser<char>(char.TryParse);
// TODO how can we implement this in .NET 3.5
#if DOTNET_V4
      AddParser<Guid>(Guid.TryParse);
#endif
      AddParser<DateTime>(DateTime.TryParse);
      AddParser<TimeSpan>(TimeSpan.TryParse);
    }

    /*
    public bool TryParse(string value, Type type, out object result) {
      // attempt to parse any enum types
      /*
      if (type.IsEnum) {
        if (Enum.TryParse<typeof<type>(value, out result))
          return true;
      }
       * /
      foreach (Type t in this.Keys) {
        if (t == type) {
          //Parser<T> parser = (Parser<T>)this[t];
          object resObj = this[t].DynamicInvoke(new object[] { value, out result });
          //if (parser(value, out result))
            return true;
        }
      }
      //result = default(T);
      return false;
    }
     */

    public bool TryParseValue(string value, out ValueType result) {
      result = default(ValueType);
      Type type = result.GetType();
      object objResult;
      bool success = TryParse(value, type, out objResult);
      result = (ValueType)objResult;
      return success;
    }
    public bool TryParse(string value, Type type, out object result) {
      if (type == typeof(decimal)) { return TryParseAsObject<decimal>(value, out result); }
      if (type == typeof(float)) { return TryParseAsObject<float>(value, out result); }
      if (type == typeof(double)) { return TryParseAsObject<double>(value, out result); }
      if (type == typeof(int)) { return TryParseAsObject<int>(value, out result); }
      if (type == typeof(long)) { return TryParseAsObject<long>(value, out result); }
      if (type == typeof(short)) { return TryParseAsObject<short>(value, out result); }
      if (type == typeof(uint)) { return TryParseAsObject<uint>(value, out result); }
      if (type == typeof(ulong)) { return TryParseAsObject<ulong>(value, out result); }
      if (type == typeof(ushort)) { return TryParseAsObject<ushort>(value, out result); }
      if (type == typeof(bool)) { return TryParseAsObject<bool>(value, out result); }
      if (type == typeof(byte)) { return TryParseAsObject<byte>(value, out result); }
      if (type == typeof(char)) { return TryParseAsObject<char>(value, out result); }
      if (type == typeof(Guid)) { return TryParseAsObject<Guid>(value, out result); }
      if (type == typeof(DateTime)) { return TryParseAsObject<DateTime>(value, out result); }
      if (type == typeof(TimeSpan)) { return TryParseAsObject<TimeSpan>(value, out result); }
      if (type.IsEnum) { 
        try { 
          result = Enum.Parse(type, value); return true; 
        } catch { 
          result = null; return false; 
        } 
      }
      throw new NotImplementedException(string.Format("No parser implemented for type {1}", value, type.FullName));
    }

    public object Parse(string value, Type type) {
      object result = null;
      if (!TryParse(value, type, out result))
        throw new NotSupportedException(string.Format("Couldn't parse '{0}' to type {1}", value, type.FullName));
      return result;
    }

    public bool TryParseAsObject<T>(string value, out object result) where T : struct {
      T typedResult;
      if (TryParse<T>(value, out typedResult)) {
        result = typedResult;
        return true;
      }
      result = null;
      return false;
    }

    public bool TryParse<T>(string value, out T result) where T : struct {
      // attempt to parse any enum types
      if (typeof(T).IsEnum) {
#if DOTNET_V4
        if (Enum.TryParse<T>(value, out result))
          return true;
#else
        throw new NotImplementedException("This method cannot be implemented in .NET Framework 3.5");
#endif
      }
      foreach (Type t in this.Keys) {
        if (t == typeof(T)) {
          Parser<T> parser = (Parser<T>)this[t];
          if (parser(value, out result))
            return true;
        }
      }
      result = default(T);
      return false;
    }

    public T Parse<T>(string value) where T : struct {
      T result; 
      if (!TryParse<T>(value, out result))
        throw new NotSupportedException(string.Format("Couldn't parse '{0}' to type {1}", value, typeof(T)));
      return result;
    }

    public void AddParser<T>(Parser<T> parser) {
       this.Add(typeof(T), parser);
    }

  }

  public delegate bool Parser<T>(string value, out T result);

}
