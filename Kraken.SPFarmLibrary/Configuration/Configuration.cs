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
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.SharePoint;
    using Microsoft.SharePoint.Administration;

    using Kraken;
    using Kraken.SharePoint.Security;

    public enum ConfigScope
    {
        Web,
        SiteCollection,
        Farm
    }

    public class ConfigurationReader {

      public bool _throwExceptions = true;
      public bool ThrowExceptions {
        get {
          return _throwExceptions;
        }
        set {
          _throwExceptions = value;
        }
      }

      public bool ReadOptionalConfigSetting(SPWeb web, ConfigScope scope, string setting, Type type, object defaultValue, out object value) {
        string valueText = string.Empty;
        // WARN: beware thread crossing calls here, but it's web process so who cares ^_^
        bool oldThrowExceptions = _throwExceptions;
        _throwExceptions = false;
        bool success = ReadConfigSetting(web, scope, setting, type, out value);
        if (!success) {
          bool oldUnsafe = web.AllowUnsafeUpdates;
          web.AllowUnsafeUpdates = true;

          WriteConfigSetting(web, scope, setting, defaultValue.ToString());
          value = defaultValue;

          web.AllowUnsafeUpdates = oldUnsafe;
        }
        _throwExceptions = oldThrowExceptions;
        return success;
      }

      public bool ReadConfigSetting(SPWeb web, ConfigScope scope, string setting, Type type, out object value) {
        value = null;
        string valueText = string.Empty;
        bool success = ReadConfigSetting(web, scope, setting, out valueText);
        if (success) {
          success = Parser.TryParse(valueText, type, ParseFlags.Simple, out value);
        }
        // TODO: how shall we indicate failure to read configuration values? Throw an exception? Log?
        return success;
      }

        /// <summary>
        /// Read a configuration setting from the specified source.
        /// </summary>
        /// <param name="web">SPWeb object for curretn web</param>
        /// <param name="level">Scope at which value is stored.</param>
        /// <param name="setting">Setting to change (propertyName)</param>
        /// <param name="value">Returned value</param>
        /// <returns>True if setting exists and can be read, false otherwise</returns>
        public bool ReadConfigSetting(SPWeb web,ConfigScope scope, string setting, out string value)
        {
            value = string.Empty;
            string retVal = string.Empty;
            bool returnBool = true;
            Delegation.RunWithElevatedPriviliges(web, delegate
            {
                using (SPSite site = new SPSite(web.Site.ID))
                {
                    using (SPWeb elevatedWeb = site.AllWebs[web.ID])
                    {
                        retVal = string.Empty;
                        switch (scope)
                        {
                            case ConfigScope.Web:
                                try
                                {
                                    retVal = elevatedWeb.Properties[setting];
                                }
                                catch (ArgumentException ex)
                                {
                                    returnBool = false;
                                    if (this._throwExceptions)
                                        throw new Exception(string.Format("Failure to read configuration setting '{0}' from property bag for web '{1}'. ", setting, elevatedWeb.Url), ex);
                                }
                                break;
                            case ConfigScope.SiteCollection:
                                try
                                {
                                    retVal = elevatedWeb.Site.RootWeb.Properties[setting];
                                }
                                catch (ArgumentException ex)
                                {
                                    returnBool = false;
                                    if (this._throwExceptions)
                                        throw new Exception(string.Format("Failure to read configuration setting '{0}' from property bag for web '{1}'. ", setting, elevatedWeb.Site.RootWeb), ex);
                                }
                                break;
                            case ConfigScope.Farm:
                                try
                                {
                                    retVal = (string)SPFarm.Local.Properties[setting];
                                }
                                catch (ArgumentException ex)
                                {
                                    returnBool = false;
                                    if (this._throwExceptions)
                                        throw new Exception(string.Format("Failure to read configuration setting '{0}' from property bag for local farm. ", setting), ex);
                                }
                                break;
                        }
                    }
                }
            });
            value = retVal;
            return returnBool;
        }

        /// <summary>
        /// Writes a config value to the web, site or farm
        /// </summary>
        /// <param name="web">SPWeb object for current web</param>
        /// <param name="scope">Scope to write</param>
        /// <param name="setting">Setting (propertyName)</param>
        /// <param name="value">value to write</param>
        public void WriteConfigSetting(SPWeb web,ConfigScope scope, string setting, string value)
        {
             Delegation.RunWithElevatedPriviliges(web, delegate
            {
                using (SPSite site = new SPSite(web.Site.ID))
                {
                    using (SPWeb elevatedWeb = site.AllWebs[web.ID])
                    {
                        switch (scope)
                        {
                            case ConfigScope.Web:
                                elevatedWeb.AllowUnsafeUpdates = true;
                                if (elevatedWeb.Properties.ContainsKey(setting))
                                {
                                    elevatedWeb.Properties[setting] = value;
                                }
                                else
                                {
                                    elevatedWeb.Properties.Add(setting, value);
                                }
                                elevatedWeb.Properties.Update();
                                elevatedWeb.AllowUnsafeUpdates = false;
                                break;
                            case ConfigScope.SiteCollection:
                                elevatedWeb.Site.RootWeb.AllowUnsafeUpdates = true;
                                elevatedWeb.Site.RootWeb.Properties[setting] = value;
                                elevatedWeb.Site.RootWeb.Properties.Update();
                                break;
                            case ConfigScope.Farm:
                                SPFarm.Local.Properties[setting] = value;
                                SPFarm.Local.Update();
                                break;
                        }
                    }
                }
            });
        }
       
    } // class
} // namespace
