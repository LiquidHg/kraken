// <copyright file="SPContentTypeTools.cs" company="Colossus Consulting LLC">
// Copyright (c)2003-2010. All Right Reserved.
// </copyright>
// <author>Thomas Carpe</author>
// <email>spark@thomascarpe.com</email>
// <date>2010-03-01</date>
// <summary></summary>


namespace Kraken.SharePoint {

  using System;
  using System.Reflection;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using Microsoft.SharePoint;

  public static class SPContentTypeExtensions {

    /// <summary>
    /// Creates a new content type with the specified fields.
    /// </summary>
    /// <param name="web">The web to create the content type within</param>
    /// <param name="contentTypeName">The name of the new content type to create</param>
    /// <param name="parentTypeName">The name of the parent content type</param>
    /// <param name="groupName">The group name for the new content type</param>
    /// <param name="fieldsToAdd">A list of fields, fieldName names, fieldName internal names, or fieldName ids.</param>
    public static void CreateContentType(this SPWeb web, string contentTypeName, string parentTypeName, string groupName, List<string> fieldsToAdd) {
      List<SPField> fields = web.MakeFieldsList(fieldsToAdd);
      CreateContentType(web, contentTypeName, parentTypeName, groupName, fields);
    }
    public static void CreateContentType(this SPWeb web, string contentTypeName, string parentTypeName, string groupName, List<Guid> fieldsToAdd) {
      List<SPField> fields = web.MakeFieldsList(fieldsToAdd);
      CreateContentType(web, contentTypeName, parentTypeName, groupName, fields);
    }
    public static void CreateContentType(this SPWeb web, string contentTypeName, string parentTypeName, string groupName, List<SPField> fieldsToAdd) {
      if (web == null)
        throw new ArgumentNullException("This method expects a valid SPWeb object.", "web");
      if (string.IsNullOrEmpty(contentTypeName))
        throw new ArgumentNullException("This method requires a content type name.", "contentTypeName");
      if (string.IsNullOrEmpty(parentTypeName))
        throw new ArgumentNullException("This method requires a parent content type name.", "parentTypeName");
      if (string.IsNullOrEmpty(groupName))
        throw new ArgumentNullException("This method requires a parent content type group name.", "groupName");
      if (fieldsToAdd == null || fieldsToAdd.Count <= 0)
        throw new ArgumentNullException("This method expects a valid list with at least one SPField object.", "fieldsToAdd");

      // TODO we might need to do a try catch here...
      SPContentType parentCType = web.AvailableContentTypes[parentTypeName];
      if (parentCType == null)
        throw new Exception(string.Format("Could not find specvified content type name '{0}' to use as parent type for new content type '{1}' in web '{2}'.", parentTypeName, contentTypeName, web.Url));
      SPContentType existingCType = web.AvailableContentTypes[contentTypeName];
      if (existingCType != null)
        throw new Exception(string.Format("There is already a content typed with name '{0}' in web '{1}'.", contentTypeName, web.Url));

      SPContentType newContentType = new SPContentType(parentCType, web.ContentTypes, contentTypeName);
      newContentType.Group = groupName;
      foreach (SPField field in fieldsToAdd) {
        SPFieldLink fieldLink = new SPFieldLink(field);
        // because we know for a fact that there is a qurik/bug in sSharePoint that sets this property as the fieldName's internal name
        fieldLink.DisplayName = field.Title;
        // TODO determine if we need to do any of these, keeping in mind we can always use the web service mathod to update them also
        //fieldLink.ReadOnly = fieldName.ReadOnlyField;
        //fieldLink.Required = fieldName.Required;
        //fieldLink.Hidden = fieldName.Hidden;
        newContentType.FieldLinks.Add(fieldLink);
      }
      newContentType.Update();
    }

    /// <summary>
    /// This overload uses reflection to create a content type from Xml.
    /// </summary>
    /// <param name="web"></param>
    /// <param name="xml"></param>
    public static void CreateContentType(this SPWeb web, System.Xml.XmlNode xml) {
      if (web == null)
        throw new ArgumentNullException("This method expects a valid SPWeb object.", "web");
      // TODO check to make sure node is of type ContentType
      string contentTypeName = xml.Attributes["Name"].Value;
      SPContentType existingCType = web.AvailableContentTypes[contentTypeName];
      if (existingCType != null)
        throw new Exception(string.Format("There is already a content typed with name '{0}' in web '{1}'.", contentTypeName, web.Url));
      SPContentType newContentType = typeof(SPContentType).GetInstance() as SPContentType;
      if (newContentType != null) {
        System.Xml.XmlNodeReader xrdr = new System.Xml.XmlNodeReader(xml);
        object result = newContentType.InvokeMember("Load", new object[] { xrdr });
      }
      web.ContentTypes.Add(newContentType);
      //newContentType.Update(); // this probably is not neededd
    }

  } // class
} // namespace
