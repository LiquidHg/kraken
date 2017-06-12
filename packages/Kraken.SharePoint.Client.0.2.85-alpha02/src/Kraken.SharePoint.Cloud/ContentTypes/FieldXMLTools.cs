using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Kraken.Xml.Linq;
using Kraken.SharePoint.Cloud;

namespace Kraken.SharePoint.Cloud.Fields {

  public class FieldXMLTools {

    /// <summary>
    /// Creates a single fieldName node for use in calling the UpdateColumns web service.
    /// </summary>
    /// <param name="fieldsDoc">XmlDocument that contains targetFieldsNode</param>
    /// <param name="targetFieldsNode">the Fields node that the fieldName will be appended into</param>
    /// <param name="methodNumber">A method number, caller must increment for each node</param>
    /// <param name="sourceField">The xml fieldName that will be imported (copied) to make the Field node</param>
    public static void AppendFieldXml(
        XElement targetFieldsNode,
        XElement sourceField,
        int methodNumber
    ) {
      if (targetFieldsNode == null || targetFieldsNode.Name.LocalName != "Fields")
        throw new Exception("targetFieldsNode has an unexpected value. You can only supply a Fields node.");
      string sourceFieldName = sourceField == null ? string.Empty : sourceField.Name.LocalName;
      if (!sourceFieldName.Equals("Field", StringComparison.InvariantCulture)
          && !sourceFieldName.Equals("FieldRef", StringComparison.InvariantCulture))
        throw new Exception("sourceField has an unexpected value. You can only append Field or FieldRef elements to this node list.");
      bool newIdGuid = false; // elements coming froma file already have a deterministic ID
      bool removeAttribs = true; // these attribs always cause trouble... I think...

      XElement method = new XElement("Method");
      method.SetAttributeValue("ID", methodNumber.ToString());
      targetFieldsNode.Add(method);

      // use ImportNode instead of CloneNode when you are copying between Xml documents
      XElement field = null;
      if (sourceFieldName.Equals("Field", StringComparison.InvariantCulture)) {
        field = new XElement(sourceField);
      } else if (sourceFieldName.Equals("FieldRef", StringComparison.InvariantCulture)) {
        // this is provided to support content type definitions
        field = new XElement("Field");
        if (sourceField.Attribute("ID") != null)
          field.SetAttributeValue("ID", sourceField.Attribute("ID").Value);
        if (sourceField.Attribute("Name") != null)
          field.SetAttributeValue("Name", sourceField.Attribute("Name").Value);
        if (sourceField.Attribute("Required") != null)
          field.SetAttributeValue("Required", sourceField.Attribute("Required").Value);
        if (sourceField.Attribute("Hidden") != null)
          field.SetAttributeValue("Hidden", sourceField.Attribute("Hidden").Value);
        if (sourceField.Attribute("ReadOnly") != null)
          field.SetAttributeValue("ReadOnly", sourceField.Attribute("ReadOnly").Value);
      }
      if (field == null || field.Name.LocalName != "Field")
        throw new Exception("xml node import returned an unexpected result. This node list should contain only Field elements.");
      // I thought you had to remove ID from the newFields, but I guess not...
      // see: http://sharepointandstuff.blogspot.com/2009/01/how-to-addupdatedelete-site-columns-by.html
      if (newIdGuid)
        field.SetAttributeValue("ID", Guid.NewGuid().ToString());
      if (removeAttribs) {
        if (field.Attribute("Version") != null) {
          // removed because of problem described in http://www.sharepointblogs.com/abdrasin/archive/2007/10/31/site-column-update-error-the-object-has-been-updated-by-another-user-since-it-was-last-fetched.aspx
          field.Attribute("Version").Remove();
        }
        // not sure if I should've removed these or not
        if (field.Attribute("StaticName") != null)
          field.Attribute("StaticName").Remove();
        if (field.Attribute("SourceID") != null)
          field.Attribute("SourceID").Remove();
      }
      method.Add(field);
    }

    /// <summary>
    /// Builds an individual fieldName node that will be used as an argument for UpdateColumns
    /// or similar web methods. Creates either either new or existing Fields, based delta of fields
    /// between an element manifest and current site columns.
    /// </summary>
    /// <param name="typeOfFields">Determines whether you want to add fields that already exist, or fields that are new.</param>
    /// <param name="currentFields">The fields xml that was returned by GetColumns web method.</param>
    /// <param name="targetFields">The xml document with the Field element definition</param>
    /// <param name="attributeMatch">"Name" or "ID" site columns use name, content types use ID</param>
    /// <param name="attributeMatch">"Field" or "FieldRef" site columns use Field, content types use FieldRef</param>
    /// <returns></returns>
    public static XElement BuildWebServiceDeltaFieldsNode(
      BuildWebServiceFieldsNodeType typeOfFields,
      XElement currentFields,
      XElement targetFields,
      string fieldElementMatch,
      string attributeMatch,
      bool alternativeMethodNumber,
      bool returnNullWhenEmpty = true
    ) {
      XElement fields = new XElement("Fields");
      int methodNumber = (alternativeMethodNumber) ? (int)typeOfFields : 0;

      // determine if the fieldName in the target element list is already in the existing object
      List<XElement> qryElementFields = targetFields.GetAllElementsOfType(fieldElementMatch);
      bool didSomething = false;
      if (currentFields == null) {
        if (typeOfFields != BuildWebServiceFieldsNodeType.NewFields)
          throw new ArgumentNullException("currentFields", "You must provide a value for currentWebCTypeDef when using typeOfFields other than 'NewFields'.");
        foreach (XElement qryElementField in qryElementFields) {
          AppendFieldXml(fields, qryElementField, methodNumber);
          didSomething = true;
          if (!alternativeMethodNumber) methodNumber++;
        }
      } else {
        foreach (XElement qryElementField in qryElementFields) {
          // cycle through all the fields in the element file...
          string qryFieldNameOrId = qryElementField.Attribute(attributeMatch).Value;
          //string queryExistingFieldByName = string.Format("/Fields/Field[@Name='{0}']", qryFieldName); // + " or @DisplayName='{0}']"
          //XmlNodeList qryExistingField = currentWebFieldDefs.SelectNodes(queryExistingFieldByName); // nsmgr
          List<XElement> qryExistingField = (
              from XElement field in currentFields.Descendants()
              where field.Name.LocalName.Equals("Field", StringComparison.InvariantCulture) && field.Attribute(attributeMatch).Value == qryFieldNameOrId
              select field
          ).ToList();
          // the field was found on the Fields list...
          bool existingField = (qryExistingField != null && qryExistingField.Count > 0);
          if (existingField && (typeOfFields == BuildWebServiceFieldsNodeType.ExstingFields || typeOfFields == BuildWebServiceFieldsNodeType.DeleteFields)) {
            AppendFieldXml(fields, qryElementField, methodNumber);
            didSomething = true;
            if (!alternativeMethodNumber) methodNumber++;
          }
          if (!existingField && typeOfFields == BuildWebServiceFieldsNodeType.NewFields) {
            AppendFieldXml(fields, qryElementField, methodNumber);
            didSomething = true;
            if (!alternativeMethodNumber) methodNumber++;
          }
        }
      }
      if (!didSomething && returnNullWhenEmpty)
        return null;
      return fields;
    }

    public static XElement BuildContentTypesWebServiceFieldsNode(
      XElement currentSiteColumnsDef,
      XElement featureElementSiteColumnsDef,
      BuildWebServiceFieldsNodeType typeOfFields
    ) {
      return BuildWebServiceDeltaFieldsNode(
        typeOfFields, currentSiteColumnsDef, featureElementSiteColumnsDef, "FieldRef", "ID", true);
    }
    public static XElement BuildSiteColumnsWebServiceFieldsNode(
      XElement currentSiteColumnsDef,
      XElement featureElementSiteColumnsDef,
      BuildWebServiceFieldsNodeType typeOfFields
    ) {
      return BuildWebServiceDeltaFieldsNode(
        typeOfFields, currentSiteColumnsDef, featureElementSiteColumnsDef, "Field", "Name", false);
    }

    /// <summary>
    /// Create a FieldRef node from a Field element within a content type definition.
    /// Might also work with list schema, but hasn't been tested with it yet.
    /// </summary>
    /// <param name="field">The field definition to make a field ref for</param>
    /// <param name="parentContentType">The parent content type, used for determining inheritence</param>
    /// <param name="includeFieldRefsOfParent">flag to enable inclusion of parent fields</param>
    /// <param name="includeFieldRefsWithoutID">flag to enable inclusion of system fields without an ID</param>
    /// <returns></returns>
#if DOTNET_V35
    public static XElement CreateFieldRefFromField(XElement field, XElement parentContentType) {
      return CreateFieldRefFromField(field, parentContentType, false, false);
    }
    public static XElement CreateFieldRefFromField(
      XElement field, XElement parentContentType, bool includeFieldRefsOfParent, bool includeFieldRefsWithoutID
    ) {
#else
    public static XElement CreateFieldRefFromField(
      XElement field,
      XElement parentContentType,
      bool includeFieldRefsOfParent = false,
      bool includeFieldRefsWithoutID = false
    ) {
#endif
      XAttribute ID = null, Name = null;
      try {
        XElement fieldRef = new XElement("FieldRef");
        fieldRef = fieldRef.StripSchema();

        ID = field.TryCloneAttribute("ID", fieldRef);
        Name = field.TryCloneAttribute("Name", fieldRef);
        // get the parent content type and skip any child fields that are not needed in this definition...
        bool isInheritedField = IsFieldInherited(parentContentType, Name);

        // skip any in parent or any without an ID as specified
        if ((ID == null && !includeFieldRefsWithoutID) || (isInheritedField && !includeFieldRefsOfParent))
          return null;

        // the complete list of available attributes is here: http://msdn.microsoft.com/en-us/library/aa543225%28v=office.12%29.aspx
        XAttribute DisplayName = field.TryCloneAttribute("DisplayName", fieldRef);
        XAttribute DefaultValue = field.TryCloneAttribute("DefaultValue", fieldRef);
        XAttribute Description = field.TryCloneAttribute("Description", fieldRef);
        XAttribute Hidden = field.TryCloneAttribute("Hidden", fieldRef);
        XAttribute ReadOnly = field.TryCloneAttribute("ReadOnly", fieldRef);
        XAttribute ReadOnlyClient = field.TryCloneAttribute("ReadOnlyClient", fieldRef);
        XAttribute Required = field.TryCloneAttribute("Required", fieldRef);
        XAttribute ShowInDisplayForm = field.TryCloneAttribute("ShowInDisplayForm", fieldRef);
        XAttribute ShowInEditForm = field.TryCloneAttribute("ShowInEditForm", fieldRef);
        XAttribute ShowInListSettings = field.TryCloneAttribute("ShowInListSettings", fieldRef);
        XAttribute ShowInNewForm = field.TryCloneAttribute("ShowInNewForm", fieldRef);
        // TODO refine the set of properties to only those that are supported and useful
        return fieldRef;

      } catch (Exception ex) {
        XElement error = new XElement("FieldRefError");
        string name = "not yet implemented";
        error.Add(new XAttribute("message", string.Format("Error attempting to add field reference Name={0} ID={1}", name, ID == null ? "unknown ID" : ID.Value)));
        error.Add(new XAttribute("exception", string.Format("{0}", ex.Message)));
        return error;
      }
    }

    /// <summary>
    /// Returns true if the specified field internal name is a match for the
    /// internal name of a field included in the provided parent content type
    /// </summary>
    /// <param name="parentContentType"></param>
    /// <param name="Name"></param>
    /// <returns></returns>
    private static bool IsFieldInherited(XElement parentContentType, XAttribute Name) {
      if (parentContentType == null)
        throw new ArgumentNullException("parentContentType");
      if (Name == null || string.IsNullOrEmpty(Name.Value))
        return false;
      XElement parentFieldsNode = parentContentType.Descendants().Where(x => x.Name.LocalName == "Fields").FirstOrDefault<XElement>();
      if (parentFieldsNode == null)
        return false; // TODO is this actually an error condition???
      XElement parentField = (from node in parentFieldsNode.Descendants()
                              where node.Name.LocalName == "Field" && node.Attribute("Name") != null
                              && (node.Attribute("Name") == null ? string.Empty : node.Attribute("Name").Value) == Name.Value
                              select node).FirstOrDefault<XElement>();
      // TODO more logic here to check if certain properties are exactly as they are in the parent definition...
      return (parentField != null);
    }

    /// <summary>
    /// Some field attributes used in XML promoted properties are left in field defintiions
    /// even though they are useless and potentially cause errors. This method removes them
    /// when they have no value. 
    /// </summary>
    /// <remarks>
    /// More info at http://msdn.microsoft.com/en-us/library/aa543481.aspx
    /// </remarks>
    /// <param name="container"></param>
    public static void TrimFieldXmlAttributes(XElement container) {
      if (container == null)
        throw new ArgumentNullException("container");
      List<XElement> fields = (from f in container.Descendants()
                               where f.Name.LocalName == "Field"
                               select f).ToList<XElement>();
      foreach (XElement field in fields) {
        if (string.IsNullOrEmpty(field.TryGetAttributeValue("PITarget", string.Empty)))
          field.TryRemoveAttribute("PITarget");
        if (string.IsNullOrEmpty(field.TryGetAttributeValue("PrimaryPITarget", string.Empty)))
          field.TryRemoveAttribute("PrimaryPITarget");
        if (string.IsNullOrEmpty(field.TryGetAttributeValue("PIAttribute", string.Empty)))
          field.TryRemoveAttribute("PIAttribute");
        if (string.IsNullOrEmpty(field.TryGetAttributeValue("PrimaryPIAttribute", string.Empty)))
          field.TryRemoveAttribute("PrimaryPIAttribute");
        if (string.IsNullOrEmpty(field.TryGetAttributeValue("Aggregation", string.Empty)))
          field.TryRemoveAttribute("Aggregation");
        if (string.IsNullOrEmpty(field.TryGetAttributeValue("Node", string.Empty)))
          field.TryRemoveAttribute("Node");
      }
    }

    public static void TrimFieldAttributes(SiteColumnExportOptions options, XElement container) {
      if (container == null)
        throw new ArgumentNullException("container");
      List<XElement> fields = (from f in container.Descendants()
                               where f.Name.LocalName == "Field"
                               select f).ToList<XElement>();
      foreach (XElement field in fields) {

        // TODO allow configuration or custom source schema ID
        if (options.RemoveSourceId)
          field.TryRemoveAttribute("SourceID");
        if (options.RemoveWebId)
          field.TryRemoveAttribute("WebId");
        if (options.RemoveStaticName)
          field.TryRemoveAttribute("StaticName");
        if (options.RemoveVersion)
          field.TryRemoveAttribute("Version");
        // adds the overwrite attribute if specified
        if (options.AddOverwrite && field.Attribute("Overwrite") == null) {
          XAttribute overwrite = new XAttribute("Overwrite", true.ToString().ToUpper());
          field.Add(overwrite);
        }

      }
    }

  }

  public enum BuildWebServiceFieldsNodeType {
    NewFields = 1,
    ExstingFields = 2,
    DeleteFields = 3
  }

}
