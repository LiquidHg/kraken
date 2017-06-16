/*
  This file is part of SPARK: SharePoint Application Resource Kit.
  The project is distributed via CodePlex: http://www.codeplex.com/spark/
  Copyright (C) 2003-2009 by Thomas Carpe. http://www.ThomasCarpe.com/

  SPARK is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  SPARK is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with SPARK.  If not, see <http://www.gnu.org/licenses/>.
*/
/*
  DotNet Tools by Thomas Carpe
  Strong Type Config Library by Thomas Carpe and Charlie Hill
  Copyright (C)2006, 2008 Thomas Carpe and Charlie Hill. Some Rights Reserved.
  Contact: dotnet@Kraken.com, chill@chillweb.net
 
  The classes in this file were written jointly and are the mutual property of both authors.
  They are licensed for use under the Creative Commons license. Rights reserved include
  "Share and Share Alike", and "Attribution". You may use this code for commercial purposes
  and derivative works, provided that you maintain this copyright notice.
*/
namespace Kraken.Configuration {

    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Reflection;

    using Kraken;

	/// <summary>
	/// Inspired by the .NET 2.0 configuration provider model,
	/// StrongTypeConfigBase is a class implementing IStrongTypeConfig
	/// that provides all the implementation (guts) for reading from
	/// NameValueCollections, like those generally stored in app.config.
	/// Developers can inherit this class into their own configuration class
	/// with strongly typed properties and add StrongTypeConfigEntryAttribute
	/// to the properties that you want to be able to read from the config file.
	/// </summary>
	public abstract class StrongTypeConfigBase : IStrongTypeConfig {

    #region Configuration

    protected static StrongTypeConfigBase _singletonInstance = null;

    protected string _defaultSectionName = "appSettings";

    private string _configSectionName;
    public string SectionName {
      get { return _configSectionName; }
    }

    protected ConfigurationReaderStatus _initStatus = ConfigurationReaderStatus.NotIntialized;
    public ConfigurationReaderStatus InitStatus {
      get { return this._initStatus; }
    }

    //protected NameValueCollection _config = null;

    #endregion

		#region Constructors

    protected StrongTypeConfigBase() : this(string.Empty, false)  { } // essentially do nothing - needed to support serialization
    protected StrongTypeConfigBase(bool doInit) { 
      // we could've done ": this(_defaultSectionName)" but it would make it harder for inherited classes to override _defaultSectionName
      SetSection(_defaultSectionName);
      if (doInit)
        Initialize();
    } 
    protected StrongTypeConfigBase(string configSectionName) : this(configSectionName, true) { }
    /// <summary>
    /// Developers inheriting this class should provide hooks into at least one of these
    /// consturctors, and must provide a hook into the parameterless constructor if
    /// Serialization of the class is desired.
    /// </summary>
    /// <param name="configSectionName">Name of the configuration section to read</param>
    /// <param name="doInit">A boolean that specified whether to initialize</param>
    protected StrongTypeConfigBase(string configSectionName, bool doInit) {
      SetSection(configSectionName);
      if (doInit)
        Initialize(configSectionName);
    }

		#endregion

		#region IStrongTypeConfig Members

		public void Initialize() {
			Initialize(_defaultSectionName);
		}
		virtual public void Initialize(string configSection) {
      SetSection(configSection);
      if (string.IsNullOrEmpty(configSection)) // in .NET Framework 1.1, use Stringtools.IsNullOrEmpty() instead
        throw new ArgumentNullException("configSection", "You must specify a valid name for this configuration object before initializing.");
      if (this.InitStatus == ConfigurationReaderStatus.Initializing)
				throw new InvalidOperationException("Already initializing.");
      _initStatus = ConfigurationReaderStatus.Initializing;
			try {
				InitializeInstance(configSection); 
			} catch {
				_initStatus = ConfigurationReaderStatus.InitFailed;
				throw;
			}
			_initStatus = ConfigurationReaderStatus.Initialized;
		}

		#endregion
		
		#region Private Methods - Initialization

    private void SetSection(string configSection) {
      if (!string.IsNullOrEmpty(this._configSectionName) && string.Compare(_configSectionName, configSection, true) != 0) // in .NET Framework 1.1 use StringTools.IsNullOrEmpty() instead
        throw new InvalidOperationException("You have already specified a value for SectionName. You can not reassign it.");
      this._configSectionName = configSection;
    }

		private void InitializeInstance(string configSection) {
      NameValueCollection configInfo = GetConfig(configSection);
			if (configInfo == null)
				return;

      // get all the fields for the class
			Type strongTypeConfigType = this.GetType(); 
			BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
			FieldInfo[] fields = strongTypeConfigType.GetFields(flags);
			foreach (FieldInfo field in fields) {
        ReadAndSetFieldOrPropertyValue(configInfo, field);
			} 
			// do the same thing for properties as we did for fields
			PropertyInfo[] props = strongTypeConfigType.GetProperties(flags);
			foreach (PropertyInfo prop in props) {
        ReadAndSetFieldOrPropertyValue(configInfo, prop);
			}
		}

    private void ReadAndSetFieldOrPropertyValue(NameValueCollection configInfo, object fieldOrProperty) {
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

      // read the property for fieldName valString
      Type type = (field == null) ? prop.PropertyType : field.FieldType;
      object value = GetConfigValue(type, configInfo, key, required, configFlags);
      if (value == null)
        return;

      // set the property for fieldName valString
      if (field == null)
        prop.SetValue(this, value, null);
      else
        field.SetValue(this, value);
    }

    /// <summary>
    /// Developers can override this method to extend or change the source for
    /// configuration collections, such as using a database instead of a file.
    /// </summary>
    /// <param name="configSection"></param>
    /// <returns></returns>
    protected virtual NameValueCollection GetConfig(string configSection) {
      NameValueCollection configInfo;
      if (string.Compare(configSection, "appSettings", true) == 0)
          configInfo = ConfigurationManager.AppSettings; //ConfigurationSettings.AppSettings;
      else
          configInfo = ConfigurationManager.GetSection(configSection) as NameValueCollection; //ConfigurationSettings.GetConfig(configSection) as NameValueCollection;
      return configInfo;
    }

		private bool GetStrongTypeConfigEntryAttributeValues(object[] attributes, ref string key, ref bool required, ref ConfigFlags flags)  {
			// There should be only 1 - subsequent fields would overwrite the original valString(s), but they are prohibited by the attrib's configuration
			foreach (Attribute attrib in attributes)  {
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

    protected object GetConfigValue(Type type, NameValueCollection configInfo, string key, bool required, ConfigFlags flags) {
      string value = configInfo[key];
      if (value == null)
        value = string.Empty; 
      if (required && value == string.Empty)
          throw new ConfigurationErrorsException(string.Format("Required configuration setting '{0}' in section {1} was not found.", key, this.SectionName));

      if (!string.IsNullOrEmpty(value)) { // // in .NET Framework 1.1 use StringTools.IsNullOrEmpty() instead
        try {
          return ConvertValue(type, value, flags);
        } catch (Exception ex) {
            throw new ConfigurationErrorsException(string.Format("An error occured while parsing {0} in section {1}.", key, this.SectionName), ex);
        }
      } 
      return null;
    }

    private static bool HasFlag(ConfigFlags var, ConfigFlags flag) {
      return ((int)var & (int)flag) == (int)flag;
    }

		#endregion
		
		#region Private Methods - Type Conversion

		/// <summary>
		/// This will convert a string to the requested type.  It can convert 
		/// the base valString types, enumerations, classes that implement the 
		/// IStrongTypeConfig interface, and any object that has a 
		/// Parse(string valString) method.
		/// </summary>
		/// <param name="type">Type to convert to</param>
		/// <param name="valString">Value to convert</param>
		/// <param name="flags">Conversion flags</param>
		/// <returns></returns>
		virtual protected object ConvertValue(Type type, string value, ConfigFlags flags) {
      object o = null;

      // When the type is NameValueCollection, the valString is the name of the config section to return
      if (type == typeof(NameValueCollection))
          o = ConfigurationManager.GetSection(value) as NameValueCollection; //(NameValueCollection)ConfigurationSettings.GetConfig(valString);

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

      // if the type is a strongtype config,  then the valString will be the name of the config sectiun
      // and its key/valString pairs should match the properties in the specified IStrongTypeConfig class.
      if (o == null && type.GetInterface(typeof(IStrongTypeConfig).FullName) != null) {
        //value = Reflector.GetInstance(type); // this is one of our internal methods
        o = Activator.CreateInstance(type); // here is another way to do the same thing
        object[] arguments = new object[] { value }; // config section
        type.InvokeMember("Initialize", BindingFlags.Default | BindingFlags.InvokeMethod, null, o, arguments);
      }

      // make the last ditch effort to try and find a Parse method using reflection
      if (o == null)
        o = Parser.Parse(value, type, ParseFlags.Invoke);

      return o;
		}

		#endregion

	} // class
} // namespace
