
namespace System.Reflection {
  using Collections;
  using Collections.Generic;
  using Collections.Specialized;
  using Kraken.Tracing;
  using System;
  using System.ComponentModel;
  using System.Reflection;

  /// <summary>
  /// This is a static library with useful code patterns for reflection.
  /// </summary>
  /// <remarks>
  /// Added function to get display-name-like attributes from the values of enums.
  /// Added ability to easily reflect against simple fields and properties.
  /// </remarks>
  public static class ReflectionExtensions {

    public static string GetName(this MethodBase method) {
      if (method.DeclaringType == null)
        return method.Name;
      return method.DeclaringType.Name + "::" + method.Name;
    }

    /// <summary>
    /// Creates a generic instance of any class that has a public
    /// constructor with no parameters. Obviously, there are other
    /// ways to accomplish the same thing. :-) One example would be
    /// to use Activator.CreateUnstance(type) instead.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static object GetInstance(this Type type) {
      if (type == null)
        throw new ArgumentNullException("type", "You must specify a valid System.Type parameter to construct an object instance.");
      ConstructorInfo constructor;
      constructor = type.GetConstructor(Type.EmptyTypes);
      if (constructor == null)
        throw new ApplicationException(type.FullName + " doesn't have public constructor with empty parameters.");
      object module = constructor.Invoke(null);
      return module;
    }

    /// <summary>
    /// Semi-automatically Invokes a static or instance member.
    /// For static member pass the Type instead of the object.
    /// </summary>
    /// <param name="typeOrInstance"></param>
    /// <param name="memberName"></param>
    /// <param name="isStatic"></param>
    /// <param name="args"></param>
    public static object InvokeMember(this object typeOrInstance, string memberName, object[] args) {
      bool isStatic = (typeOrInstance.GetType() == typeof(Type));
      object instance = (isStatic) ? null : typeOrInstance;
      Type t = (isStatic) ? typeOrInstance as Type : instance.GetType();
      BindingFlags flags =
        BindingFlags.InvokeMethod |
        BindingFlags.Public |
        BindingFlags.NonPublic;
      if (!isStatic) flags |= BindingFlags.Instance;
      object o = t.InvokeMember(
        memberName,
        flags,
        null,
        instance,
        args
      );
      return o;
    }

    #region Simple Field and Property Read/Write

    /*
public enum ReflectionTarget {
PublicProperty = 0,
ProtectedOrPrivateProperty = 1,
PublicField = 2,
ProtectedOrPrivateField = 3
}*/

    /// <summary>
    /// Shorthand method for setting simple values for properties and fields
    /// </summary>
    /// <param name="instance">The object to act upon</param>
    /// <param name="propName">The name of the fieldName or property</param>
    /// <param name="isProperty">Indicates true for property and false for fieldName</param>
    /// <param name="value">The value to set</param>
    /// <returns>An object indicating if the call succeeded or failed</returns>
    public static object SetFieldOrProperty(this object instance, string propName, bool isProperty, object value) {
      Type instanceType = instance.GetType();
      return SetFieldOrProperty(instance, instanceType, propName, isProperty, value);
    }
    public static object SetFieldOrProperty(this object instance, string propName, bool isProperty, string value) {
      Type instanceType = instance.GetType();
      if (instanceType == typeof(System.Type)) {
        instanceType = (Type)instance;
        instance = null;
      }
      return SetFieldOrProperty(instance, instanceType, propName, isProperty, (string.IsNullOrEmpty(value) && !isProperty) ? null : value);
    }
    /// <param name="instance">The object to act upon</param>
    /// <param name="instanceType">The type, if you need to override it</param>
    /// <param name="propName">The name of the fieldName or property</param>
    /// <param name="isProperty">Indicates true for property and false for fieldName</param>
    /// <param name="value">The value to set</param>
    public static object SetFieldOrProperty(this object instance, Type instanceType, string propName, bool isProperty, object value) {
      object o = instanceType.InvokeMember(
        propName,
        (isProperty ? BindingFlags.SetProperty : BindingFlags.SetField) |
        ((instance == null) ? BindingFlags.Static : BindingFlags.Instance) |
        BindingFlags.Public | BindingFlags.NonPublic, // we do not care about access levels, give us everything
        null,
        instance,
        new object[] { value }
      );
      return o;
    }

    /// <summary>
    /// Shorthand method for getting simple values for properties and fields
    /// </summary>
    /// <param name="instance">The object to act upon</param>
    /// <param name="propName">The name of the fieldName or property</param>
    /// <param name="isProperty">Indicates true for property and false for fieldName</param>
    /// <returns>An object containing the value</returns>
    public static object GetFieldOrProperty(this object instance, string propName, bool isProperty) {
      Type instanceType = instance.GetType();
      if (instanceType == typeof(System.Type)) {
        instanceType = (Type)instance;
        instance = null;
      }
      return GetFieldOrProperty(instance, instanceType, propName, isProperty);
    }
    /// <param name="instanceType">The type, if you need to override it</param>
    public static object GetFieldOrProperty(this object instance, Type instanceType, string propName, bool isProperty) {
      object o = instanceType.InvokeMember(
        propName,
        (isProperty ? BindingFlags.GetProperty : BindingFlags.GetField) |
        ((instance == null) ? BindingFlags.Static : BindingFlags.Instance) |
        BindingFlags.Public | BindingFlags.NonPublic,
        //(isProperty ? BindingFlags.Public : BindingFlags.NonPublic),
        null,
        instance,
        null //new object[]{} 
      );
      return o;
    }

    /*
    /// <summary>
      /// Uses reflect to get a basic field or property
      /// </summary>
    /// <param name="fieldOrProperty"></param>
      private void GetFieldOrPropertyValue(object fieldOrProperty) {
        PropertyInfo prop = fieldOrProperty as PropertyInfo;
        FieldInfo field = fieldOrProperty as FieldInfo;
        if (prop == null && field == null)
          throw new ArgumentException("fieldOrProperty", "Must be of type FieldInfo or PropertyInfo.");
        // set basic flag values from defaults and attribute values
        string key = (field == null) ? prop.Name : field.Name;
        // read the property for fieldName valString
        Type type = (field == null) ? prop.PropertyType : field.FieldType;
        object value = null; // GetConfigValue(type, configInfo, key, required, configFlags);

      }

    /// <summary>
    /// Uses reflect to set a basic field or property
    /// </summary>
    /// <param name="fieldOrProperty">FieldInfo or PropertyInfo</param>
    /// <param name="value"></param>
      private void SetFieldOrPropertyValue(object fieldOrProperty, object value) {
        PropertyInfo prop = fieldOrProperty as PropertyInfo;
        FieldInfo field = fieldOrProperty as FieldInfo;
        if (prop == null && field == null)
          throw new ArgumentException("fieldOrProperty", "Must be of type FieldInfo or PropertyInfo.");
        // set basic flag values from defaults and attribute values
        string key = (field == null) ? prop.Name : field.Name;
        if (field == null)
          prop.SetValue(this, value, null);
        else
          field.SetValue(this, value);
      }
    */

      /// <summary>
      /// A simple way to export all public
      /// properties to a hashtable.
      /// </summary>
      /// <param name="target"></param>
      /// <param name="convertValuesToString"></param>
      /// <returns></returns>
    public static Hashtable ExportProperties(this object target, bool convertValuesToString = false) {
      Hashtable ht = new Hashtable();
      PropertyInfo[] props = target.GetType().GetProperties();
      foreach (PropertyInfo prop in props) {
        string propertyName = prop.Name;
        object value = prop.GetValue(target);
        ht.Add(propertyName, (convertValuesToString) ? value.ToString() : value);
      }
      return ht;
    }

    public static IEnumerable<ReflectionOperationResult> ImportProperties<TTarget, TSource>(this TTarget target, TSource source, PropertyMap<TSource, TTarget> mappings = null)
      where TSource : class
      where TTarget : class 
      {
      if (mappings == null)
        mappings = new PropertyMap<TSource, TTarget>();
      mappings.Source = source;
      mappings.Target = target;
      IEnumerable<ReflectionOperationResult> results = mappings.CopyMappedValues();
      return results;
    }

    /// <summary>
    /// Uses reflection to try and import
    /// a value of unspecified type to a
    /// property of the target object.
    /// Does some semi-intelligent parsing.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="propertyName"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static ReflectionOperationResult ImportProperty(this object target, string propertyName, object value, ITrace trace = null) {
      trace = trace.Ensure();
      ReflectionOperationResult result = new ReflectionOperationResult(target) {};
      try {
        PropertyInfo prop = target.GetType().GetProperty(propertyName);
        result.TargetProperty = prop;
        if (prop == null) {
          trace.TraceVerbose("Property not found in target");
          result.Message = string.Format("Unrecognized property name: '{0}'", propertyName);
        } else if (!prop.CanWrite) {
          trace.TraceVerbose("Property is read-only");
          result.Message = string.Format("Can't set read-only property: '{0}'", propertyName);
        } else if (value == null || prop.PropertyType.IsTypeOrSubtypeOf(value.GetType())) {
          trace.TraceVerbose("Property type match / direct assignment");
          // this should always work 
          // TODO but what about with struct types where null is invalid??
          prop.SetValue(target, value);
          result.Success = true;
        } else if (prop.PropertyType == typeof(string)) {
          trace.TraceVerbose("Target Property is String / Converting");
          // convert whatever-to-string
          prop.SetValue(target, value.ToString());
          result.Success = true;
        } else if (prop.PropertyType.IsEnum
          && value.GetType() == typeof(string)) {
          trace.TraceVerbose("Target Property is Enum + Value is string / Parse");
          // try convert string-to-enum
          Enum o;
          if (prop.PropertyType.TryParseEnum(value.ToString(), out o)) {
            prop.SetValue(target, o);
            result.Success = true;
          } else
            result.Message = string.Format("Couldn't parse enum property '{0}' of type '{1}' with value '{2}'.", propertyName, prop.PropertyType, value.ToString());
        } else {
          string msg = string.Format("Type mismatch setting property '{0}' of type '{1}' with value of type '{2}'.", propertyName, prop.PropertyType, value.GetType());
          trace.TraceVerbose(msg);
          result.Message = msg;
        }
      } catch (Exception ex) {
        string exMsg = string.Format("Unexpected error setting property: '{0}'", propertyName, ex.Message);
        result.Message = exMsg;
        trace.TraceError(exMsg);
        trace.TraceError(ex);
      }
      return result;
    }

    #endregion

    #region EnumNameAttribute attribute implementation

    /// <summary>
    /// Shorthand class to get the value of any attribute tied to an enum's value(s)
    /// </summary>
    /// <param name="target">The object for whose value you want to retreive an attribute</param>
    /// <param name="attrType"></param>
    /// <returns></returns>
    public static object GetAttributeValue(this object target, Type attrType) {
      Type type = target.GetType();
      MemberInfo[] memInfo = type.GetMember(target.ToString());
      if (memInfo != null && memInfo.Length > 0) {
        object[] attrs = memInfo[0].GetCustomAttributes(attrType, false);
        if (attrs != null && attrs.Length > 0)
          return attrs[0];
      }
      return null;
    }

    /// <summary>
    /// Gets the DescriptionAttribute associated with the value in an enum
    /// </summary>
    /// <param name="en">The enum for whose value you want to retreive an attribute</param>
    /// <returns></returns>
    public static string GetDescription(this Enum en) {
      object attr = en.GetAttributeValue(typeof(DescriptionAttribute));
      return (attr == null) ? en.ToString() : ((DescriptionAttribute)attr).Description;
    }

    /// <summary>
    /// Shorthand method to read the value of DisplayName attribute
    /// </summary>
    /// <param name="en">The enum for whose value you want to retreive its EnumNameAttribute</param>
    /// <returns>The value of DisplayName attrib</returns>
    public static string GetDisplayName(this Enum en) {
      object attr = en.GetAttributeValue(typeof(DisplayNameAttribute));
      return (attr == null) ? en.ToString() : ((DisplayNameAttribute)attr).DisplayName;
    }

    /// <summary>
    /// Uses a custom attribute, EnumNameAttribute, that can be tied to the value
    /// of an item in an Enum class, to give it a friendly name. This is
    /// alos used by ParseEnumFromDisplayName to determine the enum value
    /// from its 'friendly' name string.
    /// </summary>
    /// <param name="en">The enum for whose value you want to retreive its EnumNameAttribute</param>
    /// <returns>The friendly name represented by EnumNameAttribute attrib</returns>
    public static string GetEnumNameAttribute(this Enum en) {
      // TODO believe we need to do this from the value/method not the type
      object attr = en.GetAttributeValue(typeof(EnumNameAttribute));
      return (attr == null) ? en.ToString() : ((EnumNameAttribute)attr).Text;
    }

    /// <summary>
    /// Takes a friendly name and performs a reverse lookup against the
    /// EnumNameAttribute attributes tied to each of its values, to determine
    /// what value the string represents.
    /// </summary>
    /// <param name="value">The display name that should match the EnumNameAttribute attribute</param>
    /// <param name="enumType">The type of the enum to perform the lookup on</param>
    /// <param name="matchStartWith">If true, any match of the start of the value will pass.</param>
    /// <returns>The enumerated value represented by the name string</returns>
    public static Enum ParseEnumFromDisplayName(this Type enumType, string value, bool matchStartWith) {
      if (!enumType.IsEnum)
        throw new ArgumentException("enumType");
      foreach (Enum enumVal in Enum.GetValues(enumType)) {
        string displayName = GetEnumNameAttribute(enumVal);
        if ((matchStartWith) ? value.StartsWith(displayName) : string.Compare(value, displayName, true) == 0)
          return enumVal;
      }
      throw new IndexOutOfRangeException(string.Format("Provided text does not correspond to any known choice for {0}. value='{1}'.", enumType.Name, value));
    }

    #endregion

  } // class

} // namespace
