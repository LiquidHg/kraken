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

namespace Kraken.SharePoint.Logging {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using Microsoft.SharePoint.Administration;

  /// <summary>
  /// The purpose of this class is to provide reliable access to categories and areas
  /// that can be used for log messages in SharePoint 2010. There is a jegged hierarchical
  /// relationship between categories and areas that makes it risky from a code perspective
  /// to attempt to make a logging call. You choices in solving this are to either have only
  /// a single area, or provide a safety mechaism for referncing objects. This service
  /// provides the safety mechanism.
  /// </summary>
  public class LoggingCategoryProvider {

    public LoggingCategoryProvider() {
    }

    private TraceSeverity defaultTrace = (TraceSeverity)Enum.Parse(typeof(TraceSeverity), "Medium", true); // TODO get constant from configuration
    private EventSeverity defaultEvent = (EventSeverity)Enum.Parse(typeof(EventSeverity), "Information", true); // TODO get constant from configuration
    private IList<SPDiagnosticsCategory> krakenCategories;
    private IList<SPDiagnosticsCategory> customCategories;
    private IList<SPDiagnosticsArea> krakenAreas;
    private static LoggingCategoryProvider defaultCategoryProvider;

    public IList<SPDiagnosticsArea> Areas {
      get {
        EnsureAreas();
        return krakenAreas; 
      }
    }

    public TraceSeverity DefaultTrace {
      get { return defaultTrace; }
      set { defaultTrace = value; }
    }

    public EventSeverity DefaultEvent {
      get { return defaultEvent; }
      set { defaultEvent = value; }
    }

    public static LoggingCategoryProvider DefaultCategoryProvider {
      get {
        if (defaultCategoryProvider == null)
          defaultCategoryProvider = new LoggingCategoryProvider();
        return defaultCategoryProvider;
      }
    }

    public SPDiagnosticsCategory GetCategory(string categoryNaame, string areaName, out bool isCustom) {
      return GetCategory(categoryNaame, areaName, this.DefaultTrace, this.DefaultEvent, out isCustom);
    }
    public SPDiagnosticsCategory GetCategory(string categoryNaame, string areaName, TraceSeverity traceLevel, EventSeverity eventLevel, out bool isCustom) {
      EnsureAreas();
      SPDiagnosticsCategory cat = null;
      SPDiagnosticsArea area = krakenAreas.FirstOrDefault(fa => fa.Name == areaName);
      if (area != null) {
        cat = area.Categories.FirstOrDefault(fc => fc.Name == categoryNaame);
        if (cat != null) {
          isCustom = false;
          return cat;
        }
      }
      // if area is null, we have a pretty good idea already that if area is wrong and we're going just on the category name, then we're probably dealing with something weird
      // if cat is null, but area was found, it really doesn't matter because the structure is not there under the "correct" area object
      isCustom = true;
      return CreateCustomCategory(categoryNaame, areaName, traceLevel, eventLevel);
    }

    /// <summary>
    /// Searches all known logging areas for specified the category.
    /// If not found, throws an error.
    /// </summary>
    /// <param name="categoryId"></param>
    /// <returns></returns>
    public SPDiagnosticsCategory GetCategory(LoggingCategories categoryId) {
      EnsureAreas();
      SPDiagnosticsCategory cat = null;
      foreach (SPDiagnosticsArea area in krakenAreas) {
        cat = area.Categories.FirstOrDefault(fc => fc.Id == (uint)categoryId);
        if (cat != null)
          return cat;
      }
      throw new Exception(string.Format("Could not find SPDiagnosticsCategory by logging category ID. categoryId={0}", categoryId));
    }

    /// <summary>
    /// Retrieves the specified area and attempts to find the specified category
    /// if the area is not found, calls a method that searches all areas for the category.
    /// Throws an error if all else fails.
    /// </summary>
    /// <param name="categoryId"></param>
    /// <param name="areaId"></param>
    /// <returns></returns>
    public SPDiagnosticsCategory GetCategory(LoggingCategories categoryId, LoggingAreas areaId) {
      EnsureAreas();
      SPDiagnosticsCategory cat = null;
      SPDiagnosticsArea area = krakenAreas.FirstOrDefault(fa => fa.Id == (uint)areaId);
      if (area == null) {
        // they provided an area id and clearly they were wrong because it doesn't exist, so try finding this with just teh category id
        // TODO log a performance related warning about this
        return GetCategory(categoryId);
      } else {
        cat = area.Categories.FirstOrDefault(fc => fc.Id == (uint)categoryId);
        if (cat != null)
          return cat;
        else
          throw new Exception(string.Format("Could not find SPDiagnosticsCategory by logging category and area ID. categoryId={0}, areaId={1}", categoryId, areaId));
      }
    }

    private SPDiagnosticsCategory CreateCustomCategory(string categoryNaame, string areaName, TraceSeverity traceLevel, EventSeverity eventLevel) {
      SPDiagnosticsCategory category = new SPDiagnosticsCategory(categoryNaame, traceLevel, eventLevel);
      //SPDiagnosticsArea area = new SPDiagnosticsArea(areaName, new List<SPDiagnosticsCategory> { category });
      return category;
    }

    void EnsureAreas() {
      if (krakenAreas == null) {
        krakenAreas = GenerateAreas();
      }
    }

    protected virtual IList<SPDiagnosticsArea> GenerateAreas() {
      krakenCategories = new List<SPDiagnosticsCategory> {
        new SPDiagnosticsCategory("Unknown", null, TraceSeverity.Medium, EventSeverity.Information, 0, uint.MaxValue, true, true),
        new SPDiagnosticsCategory("Kraken Unknown", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenUnknown, (uint)LoggingCategories.KrakenUnknown),
        new SPDiagnosticsCategory("Feature Framework", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenFeatures, (uint)LoggingCategories.KrakenFeatures),
        new SPDiagnosticsCategory("Logging", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenLogging, (uint)LoggingCategories.KrakenLogging),
        new SPDiagnosticsCategory("Web Parts", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenWebParts, (uint)LoggingCategories.KrakenWebParts),
        new SPDiagnosticsCategory("Branding", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenBranding, (uint)LoggingCategories.KrakenBranding),
        new SPDiagnosticsCategory("Content Types", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenContentTypes, (uint)LoggingCategories.KrakenContentTypes),
        new SPDiagnosticsCategory("Field Types", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenFieldTypes, (uint)LoggingCategories.KrakenFieldTypes),
        new SPDiagnosticsCategory("Security", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenSecurity, (uint)LoggingCategories.KrakenSecurity),
        new SPDiagnosticsCategory("Timer Jobs", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenTimerJobs, (uint)LoggingCategories.KrakenTimerJobs),
        new SPDiagnosticsCategory("Utilities", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenUtilities, (uint)LoggingCategories.KrakenUtilities),
        new SPDiagnosticsCategory("Configuration", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenConfiguration, (uint)LoggingCategories.KrakenConfiguration),
        new SPDiagnosticsCategory("Blog Extensions", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenBlogs, (uint)LoggingCategories.KrakenBlogs),
        new SPDiagnosticsCategory("E-mail Alerts", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenAlerts, (uint)LoggingCategories.KrakenAlerts),
        new SPDiagnosticsCategory("Tagging", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenTagging, (uint)LoggingCategories.KrakenTagging),
        new SPDiagnosticsCategory("Claims Authentication", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenClaims, (uint)LoggingCategories.KrakenClaims),
        new SPDiagnosticsCategory("User Profiles", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.KrakenProfiles, (uint)LoggingCategories.KrakenProfiles)
        // add your own categories as needed like this
        //new SPDiagnosticsCategory("Name", "LocalizedName", TraceSeverity.Medium, EventSeverity.Information, DefaultMessageId0, DefaultCategoryId, IsHiddenfalse, IsShadow),
      };
      SPDiagnosticsArea krakenArea = new SPDiagnosticsArea(
        "Kraken Core",
        (uint)LoggingAreas.Kraken,
        (uint)LoggingAreas.Kraken,
        false,
        krakenCategories
      );

      customCategories = new List<SPDiagnosticsCategory> {
        new SPDiagnosticsCategory("Custom Unknown", null, TraceSeverity.High, EventSeverity.Warning, 0, uint.MaxValue, true, true),
        new SPDiagnosticsCategory("Custom Unexpected", TraceSeverity.Unexpected, EventSeverity.Error, (uint)LoggingCategories.CustomError, (uint)LoggingCategories.CustomError),
        new SPDiagnosticsCategory("Custom Monitorable", TraceSeverity.Monitorable, EventSeverity.Warning, (uint)LoggingCategories.CustomWarning, (uint)LoggingCategories.CustomWarning),
        new SPDiagnosticsCategory("Custom High", TraceSeverity.High, EventSeverity.Information, (uint)LoggingCategories.CustomHigh, (uint)LoggingCategories.CustomHigh),
        new SPDiagnosticsCategory("Custom Medium", TraceSeverity.Medium, EventSeverity.Information, (uint)LoggingCategories.CustomMedium, (uint)LoggingCategories.CustomMedium),
        new SPDiagnosticsCategory("Custom Verbose", TraceSeverity.Verbose, EventSeverity.Verbose, (uint)LoggingCategories.CustomVerbose, (uint)LoggingCategories.CustomVerbose),
      };
      SPDiagnosticsArea customArea = new SPDiagnosticsArea(
        "Custom Code",
        (uint)LoggingAreas.Custom,
        (uint)LoggingAreas.Custom,
        false,
        customCategories
      );
      return new List<SPDiagnosticsArea> { krakenArea, customArea };
    }

  }

}
