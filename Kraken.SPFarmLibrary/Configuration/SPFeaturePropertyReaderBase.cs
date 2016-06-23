/*
  Project Kraken: SPARK for Microsoft SharePoint 2010
  Copyright (C) 2003-2011 Thomas Carpe. <http://www.ThomasCarpe.com/>
  Maintained by: <http://www.LiquidMercurySolutions.com/>

  This file is part of SPARK: SharePoint Application Resource Kit.
  SPARK projects are distributed via CodePlex: <http://www.codeplex.com/spark/>

  You may use this code for commercial purposes and derivative works, 
  provided that you maintain all copyright notices.

  SPARK is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version. You should have received a copy of
  the GNU General Public License along with SPARK.  If not, see
  <http://www.gnu.org/licenses/>.

  SPARK is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.
  
  We worked hard on all SPARK code, and we don't make any profit from
  sharing it with the world. Please do us a favor amd give us credit
  where credit is due, by leaving this notice unchanged. We all stand
  on the backs of giants. Wherever we have used someone else's code or
  blog article as the basis of our work, we have provided references
  to our source.
*/

namespace Kraken.SharePoint.Configuration {

  using System;
  using System.Configuration;
  using System.Diagnostics;
  using System.Collections.Generic;
  using System.Collections.Specialized;
  using System.Reflection;
  using System.Text;

  using Microsoft.SharePoint;
  using Microsoft.SharePoint.Administration;

  //using Kraken.IO;
  using Kraken.SharePoint.Logging;
  using Kraken.Configuration;

  //using Kraken.SharePoint.Utilities;

  public class SPFeaturePropertyReaderBase : IStrongTypeConfig {

    protected KrakenLoggingService log;

    protected ConfigurationReaderStatus _initStatus = ConfigurationReaderStatus.NotIntialized;
    public ConfigurationReaderStatus InitStatus {
      get { return this._initStatus; }
    }

    protected SPSite _site;
    public SPSite Site {
      get { return _site; }
    }

    protected SPWeb _parentWeb;
    public SPWeb Web {
      get { return _parentWeb; }
    }

    protected SPWeb _rootWeb;
    public SPWeb RootWeb {
      get { return _rootWeb; }
    }

    protected SPWebApplication _parentWebApp;
    public SPWebApplication WebApplication {
      get { return _parentWebApp; }
    }

    protected SPFarm _parentFarm;
    public SPFarm Farm {
      get { return _parentFarm; }
    }

    protected SPFeatureReceiverProperties _properties;
    protected SPFeatureReceiverProperties Properties {
      get { return _properties; }
    }

    protected SPFeaturePropertyCollection _xmlProperties;
    protected SPFeaturePropertyCollection XmlProperties {
      get { return _xmlProperties; }
    }

    protected SPFeature _feature;
    protected SPFeature Feature {
      get {
        if (_feature != null)
          return _feature;
        return _properties.Feature;
      }
    }

    #region Constructors

    public SPFeaturePropertyReaderBase(SPFeature feature) : this(feature, true) { }
    public SPFeaturePropertyReaderBase(SPFeature feature, bool doInit) {
      EnsureLogService();
      _feature = feature;
      _properties = null; // new SPFeatureReceiverProperties();
      _xmlProperties = feature.Definition.Properties;
      if (doInit)
        Initialize();
    }

    public SPFeaturePropertyReaderBase(SPFeatureReceiverProperties properties) : this(properties, true) { }
    public SPFeaturePropertyReaderBase(SPFeatureReceiverProperties properties, bool doInit) {
      EnsureLogService();
      _properties = properties;
      _feature = properties.Feature;
      _xmlProperties = properties.Definition.Properties;
      if (doInit)
        Initialize();
    }

    #endregion
    #region IStrongTypeConfig Members

    private void EnsureLogService() {
      if (log == null) {
        log = new KrakenLoggingService();
        log.DefaultCategory = LoggingCategories.KrakenLogging;
      }
    }

    public void Initialize() {
      EnsureLogService();
      log.Write("Initialize() invoked.");
      Initialize(string.Empty);
      log.Write("Initialize() completed.");
    }
    virtual public void Initialize(string configSection) {
      EnsureLogService();
      log.Write(string.Format("Initialize() invoked for configSection='{0}'.", configSection));
      if (this.InitStatus == ConfigurationReaderStatus.Initializing)
        throw new InvalidOperationException("Already initializing.");
      _initStatus = ConfigurationReaderStatus.Initializing;
      _initStatus = (InitializeInstance()) ? ConfigurationReaderStatus.Initialized : ConfigurationReaderStatus.InitFailed;
      log.Write("Initialize() completed.");
    }

    #endregion

    private bool InitializeInstance() {
      try {
        EnsureLogService();
        log.Write("InitializeInstance() invoked."); // Trace.WriteLine
        if (_properties == null && _feature == null)
          throw new ArgumentNullException("_properties", "Can not initialize if _properties object is null and _feature is also null.");

        SPFeature feature = this.Feature;
        if (feature == null || feature.Parent == null) {
          log.Write("There is no this.Feature or this.Feature.Parent object. Most likely reason is that we are installing or uninstalling and not activating/deactivating."); // Trace.WriteLine
        } else {
          // TODO use reflect to loop properties with attributes
          _parentFarm = feature.Parent as SPFarm;
          _parentWebApp = feature.Parent as SPWebApplication;
          _site = feature.Parent as SPSite;
          _parentWeb = feature.Parent as SPWeb;
          if (_parentWeb != null) {
            if (_site == null)
              _site = _parentWeb.Site;
          }
          if (_site != null) {
            _rootWeb = _site.RootWeb;
            if (_parentWebApp == null)
              _parentWebApp = _site.WebApplication;
          }
          if (_parentWebApp != null && _parentFarm == null) {
            _parentFarm = _parentWebApp.Farm;
          }

          // TODO implement target Web property here!
          log.Write("site exists = " + ((_site != null).ToString())); // was Trace.WriteLine
          log.Write("parentWeb exists = " + ((_parentWeb != null).ToString())); // was Trace.WriteLine
          //Trace.WriteLine("targetWeb exists = " + ((_targetWeb != null).ToString()));
          log.Write("rootWeb exists = " + ((_rootWeb != null).ToString())); // was Trace.WriteLine
        }

        if (_xmlProperties == null)
          throw new ArgumentNullException("_xmlProperties", "Can not initialize if _xmlProperties object is null.");

        // get all the fields for the class
        Type strongTypeConfigType = this.GetType();
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        FieldInfo[] fields = strongTypeConfigType.GetFields(flags);
        foreach (FieldInfo field in fields) {
          ReadAndSetFieldOrPropertyValue(_xmlProperties, field);
        }
        // do the same thing for properties as we did for fields
        PropertyInfo[] props = strongTypeConfigType.GetProperties(flags);
        foreach (PropertyInfo prop in props) {
          ReadAndSetFieldOrPropertyValue(_xmlProperties, prop);
        }

        log.Write("InitializeInstance() completed."); // was Trace.WriteLine

      } catch (Exception ex) {
        log.Write(ex);
        throw;
      } finally {
        _initStatus = ConfigurationReaderStatus.InitFailed;
      }
      return true;
    }

    #region Basically Unchanged from StrongTypeConfigBase

    private bool GetStrongTypeConfigEntryAttributeValues(object[] attributes, ref string key, ref bool required, ref ConfigFlags flags) {
      // There should be only 1 - subsequent fields would overwrite the original valString(s), but they are prohibited by the attrib's configuration
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

    private void ReadAndSetFieldOrPropertyValue(SPFeaturePropertyCollection configInfo, object fieldOrProperty) {
      log.Write(string.Format("ReadAndSetFieldOrPropertyValue() invoked for property '{0}'.", fieldOrProperty));
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
      if (value == null) {
        log.Write(string.Format("Using default value for field or property {0}.", key)); // Trace.Write
        return;
      }

      // set the property for fieldName valString
      log.Write(string.Format("Setting field or property: {0}={1}.", key, value.ToString())); // Trace.Write
      if (field == null)
        prop.SetValue(this, value, null);
      else
        field.SetValue(this, value);
      log.Write("ReadAndSetFieldOrPropertyValue() completed.");
    }

    private bool IsPropertySet(SPFeaturePropertyCollection configInfo, string propName) {
      return (!(configInfo == null || configInfo[propName] == null));
    }
    private string GetXmlProperty(SPFeaturePropertyCollection configInfo, string propName) {
      log.Write("GetXmlProperty() invoked.", TraceSeverity.Verbose, EventSeverity.Verbose);
      if (!IsPropertySet(configInfo, propName))
        return null;
      SPFeatureProperty prop = configInfo[propName];
      log.Write("GetXmlProperty() completed.", TraceSeverity.Verbose, EventSeverity.Verbose);
      return prop.Value;
    }

    /// <summary>
    /// This property should be overridden where the developer wants to extend how proeprties are read.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="configInfo"></param>
    /// <param name="key"></param>
    /// <param name="required"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    protected virtual object GetConfigValue(Type type, SPFeaturePropertyCollection configInfo, string key, bool required, ConfigFlags flags) {
      log.Write(string.Format("GetConfigValue() invoked. Reading proeprty with key '{0}'.", key)); // Trace.Write
      object value = GetXmlProperty(configInfo, key);
      value = ConvertValue(value, type, key, required, flags);
      log.Write("GetConfigValue() completed.");
      return value;
    }

    protected object ConvertValue(object value, Type type, string key, bool required, ConfigFlags flags) {
      log.Write(string.Format("ConvertValue() invoked for key '{0}'", key));
      // First, check for required property is missing.
      if (required && value == null)
        throw new ConfigurationErrorsException(string.Format("Required configuration setting '{0}' was not found.", key));
      // optional property null, no issue
      if (value == null)
        return null;
      // compare the property's type ('type') against the type of the object ('propType') we read from the source
      Type propType = value.GetType();
      if (propType == type) // these types match no parsing is needed
        return value;
      // if the object type isn't string, we can't parse it
      if (propType != typeof(string))
        throw new ArgumentException(string.Format("Type mismatch; property key '{0}' was type '{1}' and should be '{2}'.", key, propType, type));
      // convert to string - logic here is a bit muddy
      string valString = (value == null) ? string.Empty : value.ToString();
      if (string.IsNullOrEmpty(valString))
        return null;
      object result = null;
      log.Write("ConvertValue() completed [almost].");
      try {
        result = ConvertValueString(type, valString, flags);
      } catch (Exception ex) {
        throw new ConfigurationErrorsException(string.Format("An error occurred while parsing '{0}'. Error was: {1}", key, ex.Message), ex);
      }
      return result;
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
    virtual protected object ConvertValueString(Type type, string value, ConfigFlags flags) {
      log.Write(string.Format("ConvertValueString() invoked. Parsing value for string '{0}'.", value), TraceSeverity.Verbose, EventSeverity.Verbose); // Trace.Write
      object o = null;

      /*
      // When the type is NameValueCollection, the valString is the name of the config section to return
      if (type == typeof(NameValueCollection))
        value = (NameValueCollection)ConfigurationSettings.GetConfig(valString);
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
      // if the type is a strongtype config,  then the valString will be the name of the config sectiun
      // and its propertyName/valString pairs should match the properties in the specified IStrongTypeConfig class.
      if (value == null && type.GetInterface(typeof(IStrongTypeConfig).FullName) != null) {
        //value = Reflector.GetInstance(type); // this is one of our internal methods
        value = Activator.CreateInstance(type); // here is another way to do the same thing
        object[] arguments = new object[] { valString }; // config section
        type.InvokeMember("Initialize", BindingFlags.Default | BindingFlags.InvokeMethod, null, value, arguments);
      }
       */

      // make the last ditch effort to try and find a Parse method using reflection
      if (o == null)
        o = Parser.Parse(value, type, ParseFlags.Invoke);
      log.Write("ConvertValueString() completed.", TraceSeverity.Verbose, EventSeverity.Verbose);
      return o;
    }

    #endregion

  } // class

} // namespace

// TODO phase this out completely
namespace Kraken.SharePoint.Receivers {

  using System;
  using Microsoft.SharePoint;
  using Kraken.SharePoint.Configuration;

  [Obsolete("This class was moved to Kraken.SharePoint.Configuration.SPFeaturePropertyReaderBase")]
  public class SPFeaturePropertyReaderBase : Kraken.SharePoint.Configuration.SPFeaturePropertyReaderBase {

    public SPFeaturePropertyReaderBase(SPFeature feature) : base(feature) { }
    public SPFeaturePropertyReaderBase(SPFeature feature, bool doInit) : base(feature, doInit) { }
    public SPFeaturePropertyReaderBase(SPFeatureReceiverProperties properties) : base(properties) { }
    public SPFeaturePropertyReaderBase(SPFeatureReceiverProperties properties, bool doInit) : base(properties, doInit) { }

  } // class

} // namespace
