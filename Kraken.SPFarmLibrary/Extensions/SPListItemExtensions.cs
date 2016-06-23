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

namespace Kraken.SharePoint {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using Microsoft.SharePoint;
  using Microsoft.SharePoint.Administration;
  using Microsoft.SharePoint.Utilities;

  using Kraken.SharePoint.Logging;

  public static class SPListItemExtensions {

    public static object GetValue(this SPListItem item, string fieldInternalNameOrTitle) {
      object result;
      if (!item.TryGetValue(fieldInternalNameOrTitle, out result))
        throw new Exception(string.Format("Could not get value from item {0}, list {1}, field {2}.", item.ID, item.ParentList.Title, fieldInternalNameOrTitle));
      return result;
    }
    public static string GetValueAsString(this SPListItem item, string fieldInternalNameOrTitle) {
      return item.GetValue(fieldInternalNameOrTitle).ToString();
    }
    public static object GetValue(this SPListItem item, Guid fieldId) {
      object result;
      if (!item.TryGetValue(fieldId, out result))
        throw new Exception(string.Format("Could not get value from item {0}, list {1}, field {2}.", item.ID, item.ParentList.Title, fieldId));
      return result;
    }
    public static string GetValueAsString(this SPListItem item, Guid fieldId) {
      return item.GetValue(fieldId).ToString();
    }

    public static bool TrySetValue<T>(this SPListItem item, string fieldInternalNameOrTitle, T value) {
      Guid fieldId = item.Fields.GetFieldId(fieldInternalNameOrTitle);
      return item.TrySetValue(fieldId, fieldInternalNameOrTitle + "{" + fieldId.ToString() + "}", value);
    }
    public static bool TrySetValue<T>(this SPListItem item, Guid fieldId, T value) {
      return item.TrySetValue<T>(fieldId, string.Empty, value);
    }
    public static bool TrySetValue<T>(this SPListItem item, Guid fieldId, string fieldTag, T value) {
      try {
        if (fieldId == Guid.Empty)
          return false;
        item[fieldId] = value;
        return true;
      } catch (Exception ex) { // TODO narrow this down to a specific except if possible
        KrakenLoggingService.Default.Write(string.Format(
            "Expected (suppressed) exception thrown trying to get SPListItem value for field '{0}' in list '{1}/{2}'. Exception: {3}",
            string.IsNullOrEmpty(fieldTag) ? fieldId.ToString() : fieldTag,
            item.ParentList.ParentWebUrl,
            item.ParentList.Title,
            ex.Message
            ), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenUtilities);
        //BehemothLoggingService.Default.Write(ex);
        return false;
      }
    }


    /// <summary>
    /// Safely gets the value of a list item field value.
    /// </summary>
    /// <param name="fieldInternalNameOrTitle">
    /// The field internal name or title for the desired field
    /// (internal name takes precedence, if found)</param>
    /// <typeparam name="T">The type of the desired object, see <ref="value"/></typeparam>
    /// <param name="item">SPListItem that contains the desired field value</param>
    /// <param name="value">
    /// For simple fields, this is usually something like string or int.
    /// For complex fields and custom field values, it may a subclass of SPField.
    /// </param>
    /// <returns>True for success and false for failure</returns>
    public static bool TryGetValue<T>(this SPListItem item, string fieldInternalNameOrTitle, out T value) {
      value = default(T);
      Guid fieldId = item.Fields.GetFieldId(fieldInternalNameOrTitle);
      return item.TryGetValue<T>(fieldId, fieldInternalNameOrTitle + "{" + fieldId.ToString() + "}", out value);
    }
    /// <param name="fieldId">The field Id for the desired field</param>
    /// <typeparam name="T">The type of the desired object, see <ref="value"/></typeparam>
    /// <param name="item">SPListItem that contains the desired field value</param>
    /// <param name="value">
    /// For simple fields, this is usually something like string or int.
    /// For complex fields and custom field values, it may a subclass of SPField.
    /// </param>
    /// <returns>True for success and false for failure</returns>
    public static bool TryGetValue<T>(this SPListItem item, Guid fieldId, out T value) {
      return item.TryGetValue<T>(fieldId, string.Empty, out value);
    }

    private static bool TryGetValue<T>(this SPListItem item, Guid fieldId, string fieldTag, out T value) {
      value = default(T);
      try {
        if (fieldId == Guid.Empty)
          return false;
        object rawValue = item[fieldId];
        if (rawValue != null) {
          value = (T)rawValue;
          return true;
        } else
          return false;
      } catch (Exception ex) { // TODO narrow this down to a specific except if possible
        KrakenLoggingService.Default.Write(string.Format(
            "Expected exception thrown trying to get SPListItem value for field '{0}' in list '{1}/{2}'. Exception: {3}",
            string.IsNullOrEmpty(fieldTag) ? fieldId.ToString() : fieldTag,
            item.ParentList.ParentWebUrl,
            item.ParentList.Title,
            ex.Message
            ), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenUtilities);
        //BehemothLoggingService.Default.Write(ex);
        return false;
      }
    }

    public static bool TryGetValueAsString(this SPListItem item, string fieldName, out string value) {
      value = null;
      SPField field = null;
      bool success = item.Fields.TryGetField(fieldName, out field);
      if (!success || field == null)
        return false;
      object fieldValue = null;
      if (!item.TryGetValue<object>(fieldName, out fieldValue))
        return false;
      try {
        value = field.GetFieldValueAsText(fieldValue);
        return true;
      } catch (System.Exception ex) { // TODO narrow this down to a specific except if possible
        string message = string.Format(
                "Expected exception thrown trying to get SPListItem value for field '{0}' in list '{1}/{2}'. Exception: {3}.",
                fieldName,
                item.ParentList.ParentWebUrl,
                item.ParentList.Title,
                ex.Message
              );
        //throw new Exception(message, ex);
        KrakenLoggingService.Default.Write(message, TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenUtilities);
        //BehemothLoggingService.Default.Write(ex);
        return false;
      }
    }

  } // class
} // namespace
