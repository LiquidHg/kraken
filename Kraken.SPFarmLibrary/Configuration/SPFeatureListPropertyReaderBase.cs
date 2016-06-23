/*
  Project Kraken: SPARK for Microsoft SharePoint 2010
  Copyright (C) 2003-2012 Thomas Carpe. <http://www.ThomasCarpe.com/>
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
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using System.Text;

  using Microsoft.SharePoint;
  using Microsoft.SharePoint.Administration;

  using Kraken.SharePoint.Logging;
  using Kraken.Configuration;
  using System.Reflection;

  public class SPFeatureListPropertyReaderBase : SPFeaturePropertyReaderBase {

    #region Constructors

    public SPFeatureListPropertyReaderBase(SPFeature feature) : base(feature) { }
    public SPFeatureListPropertyReaderBase(SPFeature feature, bool doInit) : base(feature, doInit) { }
    public SPFeatureListPropertyReaderBase(SPFeatureReceiverProperties properties) : base(properties) { }
    public SPFeatureListPropertyReaderBase(SPFeatureReceiverProperties properties, bool doInit) : base(properties, doInit) { }

    #endregion

    protected virtual void SetDefaults() {
      // set our default values
      PropertyListLocation = SPPropertyListLocation.Web;
      PropertyListFieldKeyName = "Key";
      PropertyListFieldValueName = "Value";
      ThrowExceptionOnListNotFound = true;
      AutoCreatePropertyList = false;
    }

    public override void Initialize(string configSection) {
      SetDefaults();
      // read from the data source
      base.Initialize(configSection);
    }

    private SPConfigurationListReader cfgListReader;
    
    protected SPWeb GetListParentWeb() {
      switch (PropertyListLocation) {
        case SPPropertyListLocation.RootWeb:
          return this.Site.RootWeb;
        case SPPropertyListLocation.Web:
          return this.Web;
        case SPPropertyListLocation.CurrentRootWeb:
          if (SPContext.Current == null)
            return null;
          return SPContext.Current.Site.RootWeb;
        case SPPropertyListLocation.CurrentWeb:
          if (SPContext.Current == null)
            return null;
          return SPContext.Current.Web;
        default:
          return null;
      }
    }

    private void EnsureListReader() {
      if (cfgListReader != null)
        return;
      cfgListReader = new SPConfigurationListReader() {
        PropertyListName = this.PropertyListName
        , PropertyListFieldKeyName = this.PropertyListFieldKeyName
        , PropertyListFieldValueName = this.PropertyListFieldValueName
        , RunElevated = this.RunElevated
        , ParentWeb = this.RunElevated ? null : GetListParentWeb()
        , ParentWebUrl = this.RunElevated ? GetListParentWeb().Url : string.Empty
      };
    }

    /// <summary>
    /// If set, this property returns the name of the SharePoint list that will override the settings
    /// which are provided in the Feature XML.
    /// </summary>
    //[StrongTypeConfigEntryAttribute(false)]
    public string PropertyListName {
      get;
      set;
    }

    //[StrongTypeConfigEntryAttribute(false)]
    public string PropertyListFieldKeyName {
      get;
      set;
    }

    //[StrongTypeConfigEntryAttribute(false)]
    public string PropertyListFieldValueName {
      get;
      set;
    }

    public SPPropertyListLocation PropertyListLocation {
      get;
      set;
    }

    //[StrongTypeConfigEntryAttribute(false)]
    public bool AutoCreatePropertyList {
      get;
      set;
    }

    public bool ThrowExceptionOnListNotFound {
      get;
      set;
    }

    protected virtual bool RunElevated {
      get { return false; }
    }

    /*
    public SPList PropertyList {
      get {
        EnsureListReader();
        try {
          return cfgListReader.PropertyList;
        } catch (Exception ex) {
          log.Write(ex);
          string errMsg = string.Format(
            "Failed to get property list '{0}' at web '{1}' for feature '{2}'.",
            this.PropertyListName,
            cfgListReader.ParentWeb.Url,
            this.Feature.Definition.Name
          );
          log.Write(errMsg, TraceSeverity.High, EventSeverity.Error);
          if (ThrowExceptionOnListNotFound) {
            throw new Exception(errMsg, ex);
          }
          return null;
        }
      }
    }
     */

    protected override object GetConfigValue(Type type, SPFeaturePropertyCollection configInfo, string key, bool required, ConfigFlags flags) {
      log.Write(string.Format("GetConfigValue() invoked for key='{0}'. ", key), TraceSeverity.Verbose, EventSeverity.Verbose);
      object value = null;
      EnsureListReader();
      if (cfgListReader.RequiredPropertiesSet) {
        log.Write(string.Format("Reading list proeprty with key '{0}'.", key)); // Trace.Write
        value = cfgListReader.GetProperty(key);
        value = ConvertValue(value, type, key, required, flags);
      }
      // go back to the feature def to ge tthe property
      if (value == null) {
        if (configInfo != null) {
          log.Write("List based property not found. Reverting to base class to get config value from Feature.XML. ", TraceSeverity.Verbose, EventSeverity.Verbose);
          value = base.GetConfigValue(type, configInfo, key, required, flags);
        } else {
          log.Write("List based property not found and no Feature.XML property set provided, so nothing to do. ", TraceSeverity.Verbose, EventSeverity.Verbose);
          // TODO throw exception if required = true
        }
      }
      log.Write("GetConfigValue() completed. ", TraceSeverity.Verbose, EventSeverity.Verbose);
      return value;
    }

    protected Dictionary<string, string> GetMappingDictionary(string prefix, bool useDescValueOrder) {
      EnsureListReader();
      return cfgListReader.GetMappingDictionary(prefix, useDescValueOrder);
    }

  }

  public enum SPPropertyListLocation {
    None,
    Web,
    RootWeb,
    CurrentWeb,
    CurrentRootWeb
  }

}
