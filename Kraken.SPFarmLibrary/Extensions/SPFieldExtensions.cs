/*
  This file is part of SPARK: SharePoint Application Resource Kit.
  The project is distributed via CodePlex: http://www.codeplex.com/spark/
  Copyright (C) 2003-2010 by Thomas Carpe. http://www.ThomasCarpe.com/

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

namespace Kraken.SharePoint {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;
  using System.Text;

  using Microsoft.SharePoint;
  using Microsoft.SharePoint.Administration;
  using Kraken.SharePoint.Logging;

  public static class SPFieldExtensions {

    #region TryGetField and Supporting Methods

    private const string NO_FIELD_EXCEPTION_MESSAGE = "Value does not fall within the expected range.";
    private const string NO_FIELD_EXCEPTION_MESSAGEPART_2 = "Invalid field name";
    private const string NO_FIELD_EXCEPTION_MESSAGEPART_3 = "does not exist. It may have been deleted by another user.";

    /// <summary>
    /// Attempts to to get a field by its display name, internal name, or id.
    /// Logs any exceptions, if the field is not found, but returns quietly.
    /// Internal name will take precedence over display name if it is found.
    /// </summary>
    /// <param name="internalNameOrTitle">Name or InternalName of the desired field</param>
    /// <param name="fieldId">Id of the desired field</param>
    /// <param name="fields">Collection of fields to search</param>
    /// <param name="field">An SPField with a matching name or null if not found</param>
    /// <returns>True if successful, false if not</returns>
    public static bool TryGetField(this SPFieldCollection fields, string internalNameOrTitle, out SPField field) {
      field = null;
      try {
        field = fields.GetFieldEx(internalNameOrTitle);
      } catch (ArgumentException ex) {
        if (IsFieldNotFoundException(ex))
          return false;
        throw ex;
      }
      return true;
    }
    /// <param name="fieldId">Id of the desired field</param>
    /// <param name="fields">Collection of fields to search</param>
    /// <param name="field">An SPField with a matching name or null if not found</param>
    /// <returns>True if successful, false if not</returns>
    public static bool TryGetField(this SPFieldCollection fields, Guid fieldId, out SPField field) {
      field = null;
      try {
        field = fields.GetFieldEx(fieldId);
      } catch (ArgumentException ex) {
        if (IsFieldNotFoundException(ex))
          return false;
        throw ex;
      }
      return true;
    }

    public static SPField GetFieldEx(this SPFieldCollection fields, string internalNameOrTitle) {
      bool throwException = true;
      return fields.GetFieldEx(internalNameOrTitle, throwException);
    }
    /// <summary>
    /// Attempts to to get a field by its display name, internal name, or id.
    /// Throws a detailed (and helpful) exception if the field is not found.
    /// Internal name will take precedence over display name if it is found.
    /// </summary>
    /// <param name="internalNameOrTitle">Name or InternalName of the desired field</param>
    /// <param name="fieldId">Id of the desired field</param>
    /// <param name="fields">Collection of fields to search</param>
    /// <returns>An SPField with a matching name or null if not found</returns>
    public static SPField GetFieldEx(this SPFieldCollection fields, string internalNameOrTitle, bool throwException) {
      SPField field = null;
      Exception caughtEx = null;
      bool canHasField = fields.ContainsField(internalNameOrTitle);
      if (canHasField) {
        try {
          field = fields.GetFieldByInternalName(internalNameOrTitle);
        } catch (ArgumentException ex) {
          if (!IsFieldNotFoundException(ex))
            throw ex;
          caughtEx = ex;
        }
        if (field != null)
          return field;
        try {
          field = fields.GetField(internalNameOrTitle);
        } catch (ArgumentException ex) {
          if (!IsFieldNotFoundException(ex))
            throw ex;
          caughtEx = ex;
        }
      }
      // last ditch effort to try static name 
      foreach (SPField searchField in fields) {
        if (string.Equals(searchField.StaticName, internalNameOrTitle, StringComparison.InvariantCultureIgnoreCase)) {
          field = searchField;
          break;
        }
      }
      if (field == null) {
        Exception ex = BuildTryAndGetFieldException(fields, caughtEx, "neither the InternalName, StaticName, or Title", internalNameOrTitle);
        KrakenLoggingService.Default.Write(ex.Message, TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenUtilities);
        // This is what we call a 'vexxing exception'
        //KrakenLoggingService.Default.Write(ex, LoggingCategories.KrakenUtilities);
        if (throwException)
          throw ex;
      }
      return field;
    }

    public static SPField GetFieldEx(this SPFieldCollection fields, Guid fieldId) {
      bool throwException = true;
      return fields.GetFieldEx(fieldId, throwException);
    }

    /// <param name="fieldId">Id of the desired field</param>
    /// <param name="fields">Collection of fields to search</param>
    /// <returns>An SPField with a matching name or null if not found</returns>
    public static SPField GetFieldEx(this SPFieldCollection fields, Guid fieldId, bool throwException) {
      SPField field = null;
      Exception caughtEx = null;
      try {
        field = fields[fieldId];
      } catch (ArgumentException ex) {
        if (!IsFieldNotFoundException(ex)) {
          KrakenLoggingService.Default.Write(
              "FYI: I am throwing an exception becuase IsFieldNotFoundException is false"
              , TraceSeverity.Verbose
              , EventSeverity.Verbose
              , LoggingCategories.KrakenUtilities);
          throw ex;
        }
        caughtEx = ex;
      }
      if (field == null && throwException)
        throw BuildTryAndGetFieldException(fields, caughtEx, "Id", fieldId);
      return field;
    }

    private static bool IsFieldNotFoundException(Exception ex) {
      if (ex == null || ex.GetType() != typeof(ArgumentException))
        return false;
      ArgumentException ax = ex as ArgumentException;
      if (ax != null && !string.IsNullOrEmpty(ax.ParamName) && ax.ParamName.Equals(GETFIELDEX_ARGNAME, StringComparison.Ordinal))
        return true;
      // TODO possible to use ex.ParamName, so that'd be more efficient
      // added for 2010 new exception messages
      if (string.Equals(NO_FIELD_EXCEPTION_MESSAGE, ex.Message, StringComparison.Ordinal))
        return true;
      if (ex.Message.Contains(NO_FIELD_EXCEPTION_MESSAGEPART_2))
        return true;
      if (ex.Message.Contains(NO_FIELD_EXCEPTION_MESSAGEPART_3))
        return true;
      return false;
    }

    private static Exception BuildTryAndGetFieldException(SPFieldCollection fields, Exception ex, string fieldLabel, object fieldNameOrId) {
      string moreDiag = string.Empty;
      if (fields.List != null)
        moreDiag = string.Format("List='{0}' List.ParentWebUrl='{1}'", fields.List.Title, fields.List.ParentWebUrl);
      if (fields.Web != null)
        moreDiag = string.Format("Web.Url='{0}'", fields.Web.Url);
      string msg = string.Format("The field collection does not contain a field with {0} equal to '{1}'. {2}", fieldLabel, fieldNameOrId, moreDiag);
      if (ex == null)
        return new ArgumentException(msg, GETFIELDEX_ARGNAME);
      else
        return new ArgumentException(msg, GETFIELDEX_ARGNAME, ex);
    }

    private const string GETFIELDEX_ARGNAME = "fieldNameOrId";

    #endregion

    // TODO test if this block of code is even needed in SP2010
    #region Reflection Based Field Proeprty Access

    /// <summary>
    /// Uses reflection to access the internal methods of SPField class that can be used to set 
    /// properties, as they are accessible by CAML e.g. &gt;Property Select="PropertyName" /&lt;.
    /// Add calls to this method to your overridden Update() method to allow you Custom Properties
    /// to be read by CAML as regular Properties (a.k.a. Field Attributes).
    /// </summary>
    /// <param name="fieldName">SPField object to which proeprty will be set.</param>
    /// <param name="name">Name of the target property/attribute</param>
    /// <param name="value">Value to be set</param>
    /// <returns>Generally, returns the valueyou passed in on the 'value' parameter</returns>
    public static bool? SetFieldProperty(this SPField field, string name, bool? value) {
      MethodInfo mi = field.GetType().GetMethod("SetFieldAttributeTriValue", BindingFlags.Instance | BindingFlags.NonPublic);
      object[] paramArray = new object[] { name, value };
      object result = mi.Invoke(field, paramArray);
      return (bool?)result;
    }

    public static string SetFieldProperty(this SPField field, string name, string value) {
      MethodInfo mi = field.GetType().GetMethod("SetFieldAttributeValue", BindingFlags.Instance | BindingFlags.NonPublic);
      object[] paramArray = new object[] { name, value };
      object result = mi.Invoke(field, paramArray);
      return (string)result;
    }

    public static bool SetFieldProperty(this SPField field, string name, bool value) {
      MethodInfo mi = field.GetType().GetMethod("SetFieldBoolValue", BindingFlags.Instance | BindingFlags.NonPublic);
      object[] paramArray = new object[] { name, value };
      object result = mi.Invoke(field, paramArray);
      return (bool)result;
    }

    public static int SetFieldProperty(this SPField field, string name, int value) {
      MethodInfo mi = field.GetType().GetMethod("SetFieldIntValue", BindingFlags.Instance | BindingFlags.NonPublic);
      object[] paramArray = new object[] { name, value };
      object result = mi.Invoke(field, paramArray);
      return (int)result;
    }

    #endregion

    public static bool FieldExists(this SPFieldCollection fields, string internalNameOrTitle) {
      return fields.ContainsField(internalNameOrTitle);
    }
    public static bool FieldExists(this SPFieldCollection fields, Guid id) {
      SPField field = null;
      try {
        field = fields[id];
      } catch (ArgumentException ex) {
        if (!IsFieldNotFoundException(ex)) {
          // commented because it is not sandbox safe
          KrakenLoggingService.Default.Write(
              "FYI: I am throwing an exception becuase IsFieldNotFoundException is false"
              , TraceSeverity.Verbose
              , EventSeverity.Verbose
              , LoggingCategories.KrakenUtilities);
          throw ex;
        }
      }
      return (field != null);
    }

    /// <summary>
    /// Because for some reason when you need to set the value of a list item
    /// your *only* choices are its Title (yuck!) or the fieldName Id... &lt;sigh /&gt;
    /// </summary>
    /// <param name="internalNameOrTitle">Name or InternalName of the desired field</param>
    /// <param name="fields">Collection of fields to search</param>
    /// <returns>The field Id, or Guid.Empty if the name does not correspond to a valid field</returns>
    public static Guid GetFieldId(this SPFieldCollection fields, string internalNameOrTitle) {
      SPField field = null;
      bool success = fields.TryGetField(internalNameOrTitle, out field);
      if (!success)
        return Guid.Empty;
      return field.Id;
    }

    /// <summary>
    /// This function takes a list of field (display or internal) names
    /// or field ID guids, and creates a generic list of SPField objects.
    /// The function looks for the field first in the web and then in the 
    /// web.Site.RootWeb. Only fields that are found are added to the list.
    /// </summary>
    /// <param name="web">The web to search for the fields within.</param>
    /// <param name="fields">A list of fieldName internal names, titles, or a list of Ids.</param>
    /// <returns>A generic List of SPField objects that match the names or Ids requested</returns>
    public static List<SPField> MakeFieldsList(this SPWeb web, List<string> fields) {
      List<SPField> fieldsList = new List<SPField>();
      foreach (string fieldName in fields) {
        SPField field = null;
        bool success = web.Fields.TryGetField(fieldName, out field);
        if (!success || field != null)
          success = web.Site.RootWeb.Fields.TryGetField(fieldName, out field);
        if (!success || field == null) {
          KrakenLoggingService.Default.Write(string.Format(
              "Attempt to add field '{0}' to field list for web '{1}'. Most likely that field does not exist in web or site root web.",
              fieldName, web.Url
          ), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenUtilities);
          continue;
        }
        fieldsList.Add(field);
      }
      return fieldsList;
    }
    /// <param name="web">The web to search for the fields within.</param>
    /// <param name="fields">A list of fieldName internal names, titles, or a list of Ids.</param>
    /// <returns>A generic List of SPField objects that match the names or Ids requested</returns>
    public static List<SPField> MakeFieldsList(this SPWeb web, List<Guid> fields) {
      List<SPField> fieldsList = new List<SPField>();
      foreach (Guid fieldId in fields) {
        SPField field = null;
        bool success = web.Fields.TryGetField(fieldId, out field);
        if (!success || field != null)
          success = web.Site.RootWeb.Fields.TryGetField(fieldId, out field);
        if (!success || field == null) {
          KrakenLoggingService.Default.Write(string.Format(
              "Attempt to add field '{0}' to field list for web '{1}'. Most likely that field does not exist in web or site root web.",
              fieldId, web.Url
          ), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenUtilities);
          continue;
        }
        fieldsList.Add(field);
      }
      return fieldsList;
    }

    public static void SetDisplayOnlyFieldProperties(this SPField field, string newFormDisplayMode, string editFormDisplayMode, bool doUpdate) {
      if (string.IsNullOrEmpty(newFormDisplayMode))
        newFormDisplayMode = "Display Only";
      if (string.IsNullOrEmpty(editFormDisplayMode))
        editFormDisplayMode = "Display Only";
      field.SetCustomProperty("NewFormDisplayMode", newFormDisplayMode);
      field.SetCustomProperty("EditFormDisplayMode", editFormDisplayMode);
      if (doUpdate)
        field.Update();
    }

    /// <summary>
    /// Checks for a dependent lookup field against the specified primary lookup field
    /// with a LookupField property that matches the one provided.
    /// </summary>
    /// <param name="primaryLookup"></param>
    /// <param name="lookupField"></param>
    /// <returns></returns>
    public static bool DependentLookupExists(this SPFieldLookup primaryLookup, string lookupField) {
      List<string> dependentNames = primaryLookup.GetDependentLookupInternalNames();
      // it turns out that we don't get to [easily] choose our own internal name when the dependent lookup is created, 
      // so instead we have to go through each dependent field to determine if it is the one we plan on creating.
      foreach (string dependentName in dependentNames) {
        // While we could under certain circumstances actually go back and check to ensure the specifed
        // lookup field exists, it is difficult to do in this scape since primaryLookup.LookupList
        // returns only a string and not a reference to the source list. We'll leave this safety check
        // for another day.
        SPFieldLookup possiblyDupeCol = (SPFieldLookup)primaryLookup.ParentList.Fields.GetFieldByInternalName(dependentName);
        if (possiblyDupeCol.LookupField == lookupField)
          return true;
      }
      return false;
    }








    // Neat idea from...
    // http://www.directsharepoint.com/2011/02/replicating-dependent-lookup-columns.html
    // not currently used, but maybe we can find a place for it
    private static void CreateDependentLookUpColumns(SPWeb web, SPFieldLookup spPrimaryField, string additionalConfigXml, SPContentType objSPContentType) {
      System.Xml.XmlDocument objAdditionalConfigXmlDoc = new System.Xml.XmlDocument();
      //objAdditionalConfigXmlDoc.LoadXml(additionalConfigXml);
      System.Xml.XmlNode additionalConfigXmlNode = null;
      System.Xml.XmlNode configDataNode = null;
      System.Xml.XmlNode dependentColumnsNode = null;
      Boolean dependentColumnsExist = false;
      if (objAdditionalConfigXmlDoc != null) {
        additionalConfigXmlNode = objAdditionalConfigXmlDoc.DocumentElement;
        if (additionalConfigXmlNode != null) {
          if (additionalConfigXmlNode.HasChildNodes) {
            configDataNode = additionalConfigXmlNode.SelectSingleNode("child::*[name()='ColumnConfigData']");
            if (configDataNode != null) {
              dependentColumnsNode = configDataNode.SelectSingleNode("child::*[name()='DependentLookupColumnNames']");
              if (dependentColumnsNode != null) {
                if (dependentColumnsNode.HasChildNodes) {
                  dependentColumnsExist = true;
                }
              }
            }
          }
        }
      }
      if (spPrimaryField != null && dependentColumnsExist) {
        if (!spPrimaryField.IsDependentLookup) {
          foreach (System.Xml.XmlNode dependentColumnNode in dependentColumnsNode.ChildNodes) {
            string strDepColName = dependentColumnNode.InnerText;
            if (!string.IsNullOrEmpty(strDepColName)) {
              strDepColName = Microsoft.SharePoint.Utilities.SPHttpUtility.HtmlDecode(strDepColName, true);
            }
            string displayName = spPrimaryField.Title + ":" + strDepColName;
            if (displayName.Length > 255) {
              displayName = displayName.Substring(0, 255);
            }
            SPFieldLookup field = (SPFieldLookup)web.Fields.CreateNewField(SPFieldType.Lookup.ToString(), displayName);
            if (field == null) {
              continue;
            }
            field.LookupList = spPrimaryField.LookupList;
            field.LookupWebId = spPrimaryField.LookupWebId;
            field.LookupField = dependentColumnNode.Attributes.GetNamedItem("Key").Value;
            field.PrimaryFieldId = spPrimaryField.Id.ToString();
            field.ReadOnlyField = true;
            field.AllowMultipleValues = spPrimaryField.AllowMultipleValues;
            field.UnlimitedLengthInDocumentLibrary = spPrimaryField.UnlimitedLengthInDocumentLibrary;
            if (web.RegionalSettings.IsRightToLeft) {
              field.Direction = spPrimaryField.Direction;
            }
            field.Group = spPrimaryField.Group;
            string strName = web.Fields.Add(field);
            if (objSPContentType != null) {
              objSPContentType.FieldLinks.Add(new SPFieldLink(web.Fields.GetFieldByInternalName(strName)));
              objSPContentType.Update();
            }
          }
        }

      }
    }

  }  // class

} // namespace
