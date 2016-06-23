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
    using Microsoft.SharePoint.Utilities;
    using Microsoft.SharePoint.Administration;

    using Kraken.IO;
    using Kraken.SharePoint.Logging;
    using Kraken.Configuration;

    //using Kraken.SharePoint.Utilities;

    public class SPPropertyBagReaderBase : IStrongTypeConfig {

        protected Status _initStatus = Status.NotIntialized;
        public Status InitStatus {
            get { return this._initStatus; }
        }

        protected SPSite _site;
        public SPSite Site {
          get { return _site; }
        }

       protected SPWeb _web;
        public SPWeb Web {
            get { return _web; }
        }

        protected SPFolder _folder;
        public SPFolder Folder {
            get { return _folder; }
        }

        public object Parent {
          get {
            if (_folder != null)
              return _folder;
            if (_web != null)
              return _web;
            return _site;
          }
        }

       /*
        protected SPWebApplication _parentWebApp;
        public SPWebApplication WebApplication {
            get { return _parentWebApp; }
        }

        protected SPFarm _parentFarm;
        public SPFarm Farm {
            get { return _parentFarm; }
        }
       */

        protected SPPropertyBag _properties;
        protected SPPropertyBag Properties {
            get { return _properties; }
        }

      /*
        protected SPFeaturePropertyCollection _xmlProperties;
        protected SPFeaturePropertyCollection XmlProperties {
            get { return _xmlProperties; }
        }
       */

      /*
        protected SPFeature _feature;
        protected SPFeature Feature {
            get {
                if (_feature != null)
                    return _feature;
                return _properties.Feature;
            }
        }
       */

        #region Constructors

        public SPPropertyBagReaderBase(SPPropertyBag propBag) : this(propBag, true) { }
        public SPPropertyBagReaderBase(SPPropertyBag propBag, bool doInit) {
            _properties = propBag;
            if (doInit)
                Initialize();
        }
        public SPPropertyBagReaderBase(SPWeb web) : this(web, true) { }
        public SPPropertyBagReaderBase(SPWeb web, bool doInit) {
          _web = web;
          _properties = web.Properties;
          if (doInit)
            Initialize();
        }
        public SPPropertyBagReaderBase(SPSite site) : this(site, true) { }
        public SPPropertyBagReaderBase(SPSite site, bool doInit) {
          _site = site;
          _properties = site.RootWeb.Properties;
          if (doInit)
            Initialize();
        }

      /*
        public SPPropertyBagReaderBase(SPFolder folder) : this(folder, true) { }
        public SPPropertyBagReaderBase(SPFolder folder, bool doInit) {
          _folder = folder;
          _properties = folder.Properties;
          if (doInit)
            Initialize();
        }

       */
      // TODO figure out how to deal with this folder properties object being a hastable instead of a property bag

        #endregion
        #region IStrongTypeConfig Members

        public void Initialize() {
            Initialize(string.Empty);
        }
        virtual public void Initialize(string configSection) {
            if (this.InitStatus == Status.Initializing)
                throw new InvalidOperationException("Already initializing.");
            _initStatus = Status.Initializing;
            try {
                InitializeInstance();
            } catch {
                _initStatus = Status.InitFailed;
                throw;
            }
            _initStatus = Status.Initialized;
        }

        #endregion

        private void InitializeInstance() {
            Trace.WriteLine("InitializeInstance() invoked.");

            if (_properties == null && _feature == null)
                throw new ArgumentNullException("_properties", "Can not initialize if _properties object is null and _feature is also null.");

            SPFeature feature = this.Feature;
            if (feature == null || feature.Parent == null) {
                Trace.WriteLine("There is no this.Feature or this.Feature.Parent object. Most likely reason is that we are installing or uninstalling and not activating/deactivating.");
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
                Trace.WriteLine("site exists = " + ((_site != null).ToString()));
                Trace.WriteLine("parentWeb exists = " + ((_parentWeb != null).ToString()));
                //Trace.WriteLine("targetWeb exists = " + ((_targetWeb != null).ToString()));
                Trace.WriteLine("rootWeb exists = " + ((_rootWeb != null).ToString()));
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

        private void ReadAndSetFieldOrPropertyValue(SPFeaturePropertyCollection configInfo, object fieldOrProperty) {
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

        private bool IsPropertySet(SPFeaturePropertyCollection configInfo, string propName) {
            return (!(configInfo == null || configInfo[propName] == null));
        }
        private string GetXmlProperty(SPFeaturePropertyCollection configInfo, string propName) {
            if (!IsPropertySet(configInfo, propName))
                return null;
            SPFeatureProperty prop = configInfo[propName];
            return prop.Value;
        }
        protected object GetConfigValue(Type type, SPFeaturePropertyCollection configInfo, string key, bool required, ConfigFlags flags) {
            Trace.Write(string.Format("Reading proeprty with key '{0}'.", key));
            object o = GetXmlProperty(configInfo, key);
            if (required && o == null)
                throw new ConfigurationErrorsException(string.Format("Required configuration setting '{0}' was not found.", key));
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
                            throw new ConfigurationErrorsException(string.Format("An error occured while parsing {0}.", key), ex);
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

    } // class

} // namespace
