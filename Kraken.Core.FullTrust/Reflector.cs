
namespace Kraken.Reflection {

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
    public class Reflector {

        private Reflector() { }

        /// <summary>
        /// Creates a generic instance of any class that has a public
        /// constructor with no parameters. Obviously, there are other
        /// ways to accomplish the same thing. :-) One example would be
        /// to use Activator.CreateUnstance(type) instead.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object GetInstance(Type type) {
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
        public static object InvokeMember(object typeOrInstance, string memberName, object[] args) {
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
        public static object SetFieldOrProperty(object instance, string propName, bool isProperty, object value) {
            Type instanceType = instance.GetType();
            return SetFieldOrProperty(instance, instanceType, propName, isProperty, value);
        }
        public static object SetFieldOrProperty(object instance, string propName, bool isProperty, string value) {
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
        public static object SetFieldOrProperty(object instance, Type instanceType, string propName, bool isProperty, object value) {
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
        public static object GetFieldOrProperty(object instance, string propName, bool isProperty) {
            Type instanceType = instance.GetType();
            if (instanceType == typeof(System.Type)) {
                instanceType = (Type)instance;
                instance = null;
            }
            return GetFieldOrProperty(instance, instanceType, propName, isProperty);
        }
        /// <param name="instanceType">The type, if you need to override it</param>
        public static object GetFieldOrProperty(object instance, Type instanceType, string propName, bool isProperty) {
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

        #endregion

        #region EnumName attribute implementation

        // TODO: implement similar methods for other types
        /// <summary>
        /// Shorthand class to get the value of any attribute tied to an enum's value(s)
        /// </summary>
        /// <param name="attrType"></param>
        /// <param name="en">The enum for whose value you want to retreive an attribute</param>
        /// <returns></returns>
        public static object GetValueAttribute(Type attrType, Enum en) {
            Type type = en.GetType();
            MemberInfo[] memInfo = type.GetMember(en.ToString());
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
        public static string GetValueDescription(Enum en) {
            object attr = GetValueAttribute(typeof(DescriptionAttribute), en);
            return (attr == null) ? en.ToString() : ((DescriptionAttribute)attr).Description;
        }

        /// <summary>
        /// Shorthand method to read the value of DisplayName attribute
        /// </summary>
        /// <param name="en">The enum for whose value you want to retreive its EnumName</param>
        /// <returns>The value of DisplayName attrib</returns>
        public static string GetDisplayName(Enum en) {
            object attr = GetValueAttribute(typeof(DisplayNameAttribute), en);
            return (attr == null) ? en.ToString() : ((DisplayNameAttribute)attr).DisplayName;
        }

        /// <summary>
        /// Uses a custom attribute, EnumName, that can be tied to the value
        /// of an item in an Enum class, to give it a friendly name. This is
        /// alos used by ParseEnumFromDisplayName to determine the enum value
        /// from its 'friendly' name string.
        /// </summary>
        /// <param name="en">The enum for whose value you want to retreive its EnumName</param>
        /// <returns>The friendly name represented by EnumName attrib</returns>
        public static string GetEnumName(Enum en) {
            object attr = GetValueAttribute(typeof(EnumName), en);
            return (attr == null) ? en.ToString() : ((EnumName)attr).Text;
        }

        /// <summary>
        /// Takes a friendly name and performs a reverse lookup agains the
        /// EnumName attribures tied to each of its values, to determine
        /// what value the string represents.
        /// </summary>
        /// <param name="value">The display name that should match the EnumName attribute</param>
        /// <param name="enumType">The type of the enum to perform the lookup on</param>
        /// <param name="matchStartWith">If true, any match of the start of the value will pass.</param>
        /// <returns>The enumerated value represented by the name string</returns>
        public static Enum ParseEnumFromDisplayName(string value, Type enumType, bool matchStartWith) {
            foreach (Enum enumVal in Enum.GetValues(enumType)) {
                string displayName = GetEnumName(enumVal);
                if ((matchStartWith) ? value.StartsWith(displayName) : string.Compare(value, displayName, true) == 0)
                    return enumVal;
            }
            throw new IndexOutOfRangeException(string.Format("Provided text does not correspond to any known choice for {0}. value='{1}'.", enumType.Name, value));
        }

        #endregion

    } // class

    /// <summary>
    /// Use this attribute to tag the individual values of an enum type
    /// with friendly names that can be looked up later using GetEnumName
    /// and ParseEnumFromDisplayName methods of Reflector class.
    /// </summary>
    public class EnumName : Attribute {
        public string Text;
        public EnumName(string text) {
            this.Text = text;
        }
    }

} // namespace
