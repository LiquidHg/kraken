
namespace Kraken.Configuration {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Diagnostics;
    #if !DOTNET_V35
    using System.Linq;
    #endif
    using System.Reflection;
    using System.Text;

    public class StrongTypedArgsBase : IStrongTypeConfig {

   protected ConfigurationReaderStatus _initStatus = ConfigurationReaderStatus.NotIntialized;
   public ConfigurationReaderStatus InitStatus {
      get { return this._initStatus; }
    }

	#region Constructors

    public StrongTypedArgsBase(string[] args) : this(args, true) { } 
    public StrongTypedArgsBase(string[] args, bool doInit) {
      //try {
      if (doInit)
        Initialize(args);
      //} catch () {
      //}
    } 

	#endregion
    #region IStrongTypeConfig Members

    virtual public void Initialize() {
        throw new NotImplementedException("Use Initialize(string[] args) instead.");
    }
    virtual public void Initialize(string config) {
        throw new NotImplementedException("Use Initialize(string[] args) instead.");
    }
    virtual public void Initialize(string[] args) {
      if (this.InitStatus == ConfigurationReaderStatus.Initializing)
        throw new InvalidOperationException("Already initializing.");
      _initStatus = ConfigurationReaderStatus.Initializing;
      try {
        InitializeInstance(args);
      } catch {
          _initStatus = ConfigurationReaderStatus.InitFailed;
        throw;
      }
      _initStatus = ConfigurationReaderStatus.Initialized;
    }

    #endregion

    private void InitializeInstance(string[] args) {
        Trace.WriteLine("InitializeInstance() invoked.");

        List<string> nvArgs = new List<string>();
        // TODO Find all the boolean flags and determine what need to be an nvArg
        HybridDictionary argDictionary = GetArgs(args, nvArgs);

        // get all the fields for the class
        Type strongTypeConfigType = this.GetType();
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        FieldInfo[] fields = strongTypeConfigType.GetFields(flags);
        foreach (FieldInfo field in fields) {
            ReadAndSetFieldOrPropertyValue(argDictionary, field);
        }
        // do the same thing for properties as we did for fields
        PropertyInfo[] props = strongTypeConfigType.GetProperties(flags);
        foreach (PropertyInfo prop in props) {
            ReadAndSetFieldOrPropertyValue(argDictionary, prop);
        }

        Trace.WriteLine("InitializeInstance() completed.");
    }

    #region Basically Unchanged from StrongTypeConfigBase

    private bool GetStrongTypeConfigEntryAttributeValues(object[] attributes, ref string key, ref bool required, ref ConfigFlags flags) {
      // There should be only 1 - subsequent fields would overwrite the original value(s), but they are prohibited by the attrib's configuration
      foreach (Attribute attrib in attributes) {
        StrongTypeConfigEntryAttribute appConfigEntry = attrib as StrongTypeConfigEntryAttribute;
        if (appConfigEntry != null) {
          required = appConfigEntry.Required;
          flags = appConfigEntry.Flags;

          if (appConfigEntry.IsKeyDefined && appConfigEntry.Key != string.Empty)
            key = appConfigEntry.Key;

          return true;
        }
      }
      return false;
    }

    private static bool HasFlag(ConfigFlags var, ConfigFlags flag) {
      return ((int)var & (int)flag) == (int)flag;
    }

    #endregion
    #region Private Methods - Read Property Values

    private void ReadAndSetFieldOrPropertyValue(HybridDictionary configInfo, object fieldOrProperty) {
      PropertyInfo prop = fieldOrProperty as PropertyInfo;
      FieldInfo field = fieldOrProperty as FieldInfo;
      if (prop == null && field == null)
        throw new ArgumentException("fieldOrProperty", "Must be of type FieldInfo or PropertyInfo.");

      // set basic flag values from defaults and attribute values
      object[] attributes = (field == null)
        ? prop.GetCustomAttributes(typeof(StrongTypeConfigEntryAttribute), false)
        : field.GetCustomAttributes(typeof(StrongTypeConfigEntryAttribute), false);
      bool required = false;
      string key = (field == null) ? prop.Name : field.Name;
      ConfigFlags configFlags = ConfigFlags.None;
      bool hasAttrib = GetStrongTypeConfigEntryAttributeValues(attributes, ref key, ref required, ref configFlags);
      if (!hasAttrib)
        return;

      // read the property for fieldName value
      Type type = (field == null) ? prop.PropertyType : field.FieldType;
      object value = GetConfigValue(type, configInfo, key, required, configFlags);
      if (value == null) {
        Trace.Write(string.Format("Using default value for field or property {0}.", key));
        return;
      }

      // set the property for fieldName value
      Trace.Write(string.Format("Setting field or property: {0}={1}.", key, value.ToString()));
      if (field == null)
        prop.SetValue(this, value, null);
      else
        field.SetValue(this, value);
    }

    private bool IsPropertySet(HybridDictionary configInfo, string propName) {
      return (!(configInfo == null || configInfo[propName] == null));
    }
    private string GetDictionaryProperty(HybridDictionary configInfo, string propName) {
      if (!IsPropertySet(configInfo, propName))
        return null;
      object prop = configInfo[propName];
      return prop.ToString();
    }
    protected object GetConfigValue(Type type, HybridDictionary configInfo, string key, bool required, ConfigFlags flags) {
      Trace.Write(string.Format("Reading proeprty with key '{0}'.", key));
      object o = GetDictionaryProperty(configInfo, key);
      if (required && o == null)
          throw new ConfigurationErrorsException(
              string.Format("Required configuration setting '{0}' was not found.", key)
          );
      if (o != null) {
        Type propType = o.GetType();
        if (propType == type) // these types match no parsing is needed
          return o;
        if (propType == typeof(string)) {
          string value = (o == null) ? string.Empty : o.ToString();
          if (!string.IsNullOrEmpty(value)) { // in .NET Framework 1.1 use StringTools.IsNullOrEmpty() instead
            try {
              return ConvertValue(type, value, flags);
            } catch (Exception ex) {
                throw new ConfigurationErrorsException(
                    string.Format("An error occured while parsing {0}.", key), ex
                );
            }
          }
        } else {
          throw new ArgumentException(string.Format("Type mismatch; property key {0} was type {1} and should be {2}", key, propType, type));
        }
      }
      return null;
    }

    #endregion
    #region Private Methods - Type Conversion

    /// <summary>
    /// This will convert a string to the requested type.  It can convert 
    /// the base value types, enumerations, classes that implement the 
    /// IStrongTypeConfig interface, and any object that has a 
    /// Parse(string value) method.
    /// </summary>
    /// <param name="type">Type to convert to</param>
    /// <param name="value">Value to convert</param>
    /// <param name="flags">Conversion flags</param>
    /// <returns></returns>
    virtual protected object ConvertValue(Type type, string value, ConfigFlags flags) {
      Trace.Write(string.Format("Parsing value for string '{0}'.", value));
      object o = null;

      /*
      // When the type is NameValueCollection, the value is the name of the config section to return
      if (type == typeof(NameValueCollection))
        o = (NameValueCollection)ConfigurationSettings.GetConfig(value);
      */

      // Try to do any simple type parsing that can be done
      if (o == null) {
        o = Parser.Parse(value, type, ParseFlags.Simple, false);
        // test for special "date only" type
        if (o != null && type == typeof(DateTime)) {
          DateTime dt = (DateTime)o;
          if (HasFlag(flags, ConfigFlags.DateOnly) && (dt != dt.Date))
              throw new ConfigurationErrorsException("Found date and time components when expecting only a date.");
        }
      }

      /*
      // if the type is a strongtype config,  then the value will be the name of the config sectiun
      // and its key/value pairs should match the properties in the specified IStrongTypeConfig class.
      if (o == null && type.GetInterface(typeof(IStrongTypeConfig).FullName) != null) {
        //o = Reflector.GetInstance(type); // this is one of our internal methods
        o = Activator.CreateInstance(type); // here is another way to do the same thing
        object[] arguments = new object[] { value }; // config section
        type.InvokeMember("Initialize", BindingFlags.Default | BindingFlags.InvokeMethod, null, o, arguments);
      }
       */

      // make the last ditch effort to try and find a Parse method using reflection
      if (o == null)
        o = Parser.Parse(value, type, ParseFlags.Invoke);
      return o;
    }

    #endregion

    /// <summary>
    /// Takes an array of args from a command line program and makes a
    /// dictionary that is easier to use in your programs.
    /// </summary>
    /// <param name="args">The arg array</param>
    /// <param name="nullValueArgs">A generic list of any commands that do not take a value (e.g. -verbose). Do not include the dashes.</param>
    /// <example>
    /// string[] args = new string[] { "-site", "http://spdev", "-web", "/", "-verbose", "-immediate" };
    /// List<string>() nvArgs = new List<string>();
    /// nvArgs.Add("verbose");
    /// nvArgs.Add("immediate");
    /// nvArgs.Add("help");
    /// HybridDictionary GetArgs(args, nvArgs);
    /// </example>
    /// <returns></returns>
    protected static HybridDictionary GetArgs(string[] args, List<string> nullValueArgs) {
        HybridDictionary argList = new HybridDictionary();
        for (int i = 0; i < args.Length; i++) {
            string cmd = args[i].ToLower();
            if (argList.Contains(cmd))
                throw new Exception(string.Format("Duplicate argument parameter at '{0}'.", cmd));
            if (cmd.StartsWith("-")) {
                cmd = cmd.Substring(1); // remove the dash before we compare to the list of null value args.
                string value = string.Empty;
                if (!nullValueArgs.Contains(cmd)) {
                    if (i == args.Length - 1 || args[i + 1].StartsWith("-"))
                        throw new Exception(string.Format("Missing parameter value at '{0}'.", cmd));
                    i++; // go to next argument
                    value = args[i];
                }
                argList.Add(cmd, value);
            } else {
                throw new Exception(string.Format("Unknown argument format near '{0}'.", cmd));
            }
        } // foreach
        return argList;
    }

  } // class

} // namespace
