// <copyright file="SPContentTypeFeatureTools.cs" company="Colossus Consulting LLC">
// Copyright (c)2003-2010. All Right Reserved.
// </copyright>
// <author>Thomas Carpe</author>
// <email>spark@thomascarpe.com</email>
// <date>2010-03-01</date>
// <summary></summary>

namespace Kraken.SharePoint.ContentTypes {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Xml;

    using Microsoft.SharePoint;
    using Microsoft.SharePoint.Utilities;

  using Kraken.SharePoint;
  using Kraken.SharePoint.Configuration;
  using Kraken.SharePoint.Logging;
  using Kraken.SharePoint.Services;

#if LegacyXmlContentTypeFunctions

    /// <summary>
    /// This class contains some methods that are useful when creating advanced Features
    /// that deploy site columns and content types. Here are some things it solves:
    /// 1) Updates the ghosted and unghosted version of a content type when the feature is activated.
    /// 2) Allows you to make a Web scoped content type feature based on an element file.
    /// 3) Propagates changes to existing list content types upon activation using a timer job.
    /// </summary>
    //[Obsolete("SPContentTypeFeatureTools is obsolete. Please user SPContentTypeFeatureToolsX to take advantage of new framework features in System.Xml.Linq.")]
    public class SPContentTypeFeatureTools {

        #region RefreshListContentTypes Event

        public static event ListContentTypeRefreshEventHandler RefreshListContentTypes;

        public static void RemoveAllRefreshListContentTypes() {
            RefreshListContentTypes = null;
        }

        //RefreshListContentTypes += new ListContentTypeRefreshEventHandler(Default_RefreshListContentTypes);

        [Obsolete("SPContentTypeFeatureTools is obsolete. Please user SPContentTypeFeatureToolsX to take advantage of new framework features in System.Xml.Linq.")]
        public static void OnRefreshListContentTypes(SPWeb web, string contentTypeName) {
            if (RefreshListContentTypes != null) {
                ListContentTypeRefreshEventArgs e = new ListContentTypeRefreshEventArgs(new List<string>(){ contentTypeName });
                RefreshListContentTypes(web, e);
            }
        }

        [Obsolete("SPContentTypeFeatureTools is obsolete. Please user SPContentTypeFeatureToolsX to take advantage of new framework features in System.Xml.Linq.")]
        public static void DoRefreshListContentTypes(object web, ListContentTypeRefreshEventArgs args) {
            SPWeb targetWeb = web as SPWeb;
            if (web == null)
                throw new ArgumentNullException("Expecting a valid object of type SPWeb.", "web");
            // instantiated now, used later in the loop
            ContentTypePropagator cta = new ContentTypePropagator();
            //cta.Logging += new LoggingEventHandler(BehemothLoggingService.Default.Log);
            cta.RefreshListContentTypes(
                targetWeb.Site, // TODO make an overload that support both 'web' and ctName string
                args.ContentTypeNames,
                args.UpdateFields, 
                args.RemoveFields,
                args.RecurseSubWebs,
                args.ForceUpdate
            );
        }

        #endregion

        #region Reading of Feature Element XML

        public static XmlNodeList GetSiteColumnDefs(XmlDocument doc) {
            XmlNodeList elemsList = doc.GetElementsByTagName("Elements");
            if (elemsList != null && elemsList.Count > 0) {
                XmlElement elems = elemsList[0] as XmlElement;
                if (elems != null) {
                    XmlNodeList fields = elems.GetElementsByTagName("Field");
                    return fields;
                }
            }
            return null;
        }

        public static XmlNodeList GetContentTypeDefs(XmlDocument doc) {
            XmlNodeList elemsList = doc.GetElementsByTagName("Elements");
            if (elemsList != null && elemsList.Count > 0) {
                XmlElement elems = elemsList[0] as XmlElement;
                if (elems != null) {
                    XmlNodeList cTypes = elems.GetElementsByTagName("ContentType");
                    return cTypes;
                }
            }
            return null;
        }

        #endregion

        #region Content Types

        /// <summary>
        /// 
        /// </summary>
        /// <param name="web">
        /// Web you want to create content types for, 
        /// or use SPSite.RootWeb for site collection level.
        /// </param>
        /// <param name="elementFeatureAndFile"></param>
        [Obsolete("SPContentTypeFeatureTools is obsolete. Please user SPContentTypeFeatureToolsX to take advantage of new framework features in System.Xml.Linq.")]
        [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage", Justification = "This method has also been marked as obsolete.")]
        public static void EnsureContentTypes(SPWeb web, string elementFeatureAndFile) {
            XmlDocument elementDoc = SPFeatureXmlTools.GetConfigFile(elementFeatureAndFile);
            EnsureContentTypes(web, elementDoc);
        }

        [Obsolete("SPContentTypeFeatureTools is obsolete. Please user SPContentTypeFeatureToolsX to take advantage of new framework features in System.Xml.Linq.")]
        public static void EnsureContentTypes(SPWeb web, XmlDocument elementDoc) {

            // get all ContentType nodes in /ContentTypes/ContentType
            // attribs available are... 
            //  Name (CT Display Name, is also in element file)
            //  ID (the bizarre "octet" 0x0000 format, is also in element file)
            //  Group (string like "_Hidden", is also in element file)
            //  Description (string, is also in element file)
            //  NewDocumentControl (string)
            //  Scope (a SP web address)
            //  Version (a whole number from 0 to ...)
            //  RequireClientRenderingOnNew (TRUE or FALSE)
            XmlDocument currentContentTypeDefs = SPWebServicesClientFactory.GetContentTypes(web);

            XmlNodeList cTypes = GetContentTypeDefs(elementDoc);
            // loop through element file

            foreach (XmlNode cType in cTypes) {
                // for currentContentTypesDoc find /ContentTypes/ContentType[@ID='']
                XmlElement cTypeElement = cType as XmlElement;
                string cTypeID = cTypeElement.Attributes["ID"].Value;
                string cTypeName = cTypeElement.Attributes["Name"].Value;
                string queryExistingCypeByID = string.Format("/ContentTypes/ContentType[@ID='{0}']", cTypeID);
                // TODO: we could do this by name and group too to prevent weird conflcits...
                XmlNodeList qryExistingCType = currentContentTypeDefs.SelectNodes(queryExistingCypeByID); // nsmgr
                bool cTypeExists = (qryExistingCType != null && qryExistingCType.Count > 0);
                if (cTypeExists) {
                    // count up the fields in the current content type
                    // seperate into new fields and updated fields
                    // call UpdateContentType
                    XmlNode existingCType = qryExistingCType[0];

                    XmlDocument currentWebCTypeDefDoc = SPWebServicesClientFactory.GetContentType(web, cTypeID);
                    XmlNode currentWebCTypeDef = currentWebCTypeDefDoc.SelectSingleNode("ContentType");

                    XmlDocument doc = new XmlDocument();
                    XmlNode cTypeProperties = doc.ImportNode(cType, false);
                    // do we need to remove stuff here???

                    XmlNode newFields = BuildContentTypesWebServiceFieldsNode(currentWebCTypeDef, cType, BuildWebServiceFieldsNodeType.NewFields);
                    XmlNode updateFields = BuildContentTypesWebServiceFieldsNode(currentWebCTypeDef, cType, BuildWebServiceFieldsNodeType.ExstingFields);
                    XmlNode deleteFields = null; // TODO: implement me - maybe

                    XmlNode result = SPWebServicesClientFactory.UpdateContentType(web, cTypeID, cTypeProperties, newFields, updateFields, deleteFields);
                    // TODO parse result, ensure success...

                    // Now we have updated the content type. If that succeeded, update the list ct's too.
                    OnRefreshListContentTypes(web, cTypeName);
                    // TODO It's kind of a hack to have this sitting in here with all the XML and web service stuff plus, it's slow.

                } else { // if not found...
                    // TODO use the API to create the content types first, then re-enter this method

                    // call CreateContentType
                    // convert cType to a fields list
                    // string displayName = cType.Attributes["Name"].Value;
                    // AppendFieldXml(qryElementField, fieldsDoc, fields, methodNumber++);
                    // pass it into the web service
                    // ... 
                    // uh-oh... uuurrrrtttt! (that's a braking/record scratching sound, btw))
                    // Actually none of this will work because you can't set the content type ID this way...
                    throw new NotSupportedException("Creation of content types through the web service is not supported due to inability to pass ID as a parameter during creation. Ensure that you have included an element file with the desired content type data, and that the element file matches the file passed to this reciever.");
                }
            }

        }

        private static XmlNode BuildContentTypesWebServiceFieldsNode(
            XmlNode currentWebCTypeDef,
            XmlNode featureElementCTypeDef,
            BuildWebServiceFieldsNodeType typeOfFields
        ) {
            int methodNumber = (int)typeOfFields;
            // XmlNamespaceManager nsmgr, was removed because we stripped xmlns from the Xml source
            XmlDocument fieldsDoc = new XmlDocument();
            /*
            //was removed because we stripped xmlns from the Xml source
            XmlNamespaceManager nsmgr2 = new XmlNamespaceManager(fieldsDoc.NameTable);
            nsmgr2.AddNamespace("", MOSS_SOAP_NAMESPACE);
             */
            XmlNode fields = fieldsDoc.CreateElement("Fields");
            fieldsDoc.AppendChild(fields);

            // determine if the fieldName in the element list is already in the web
            XmlNodeList elementFields = featureElementCTypeDef.SelectNodes(".//FieldRef");
            foreach (XmlNode qryElementField in elementFields) {
                // cycle through all the fields in the element file...
                string qryFieldId = qryElementField.Attributes["ID"].Value;
                string queryExistingFieldById = string.Format(".//Field[@ID='{0}']", qryFieldId);
                XmlNodeList qryExistingField = currentWebCTypeDef.SelectNodes(queryExistingFieldById); // nsmgr
                // fieldName was found on the web's fieldName list...
                bool existingField = (qryExistingField != null && qryExistingField.Count > 0);
                if (existingField && typeOfFields == BuildWebServiceFieldsNodeType.ExstingFields)
                    AppendFieldXml(qryElementField, fieldsDoc, fields, methodNumber);
                if (!existingField && typeOfFields == BuildWebServiceFieldsNodeType.NewFields)
                    AppendFieldXml(qryElementField, fieldsDoc, fields, methodNumber);
            }
            return fields;
            // GC will come by for fieldsDoc ... eventually... I hope... :-S
        }

        #endregion

        #region Site Columns Stuff

        /// <summary>
        /// Calls a web service to ensure that site columns exist.
        /// </summary>
        /// <param name="web">
        /// Web you want to create site columns for, 
        /// or use SPSite.RootWeb for site collection level.
        /// </param>
        /// <param name="elementFeatureAndFile"></param>
        /// <returns></returns>
        [Obsolete("SPContentTypeFeatureTools is obsolete. Please user SPContentTypeFeatureToolsX to take advantage of new framework features in System.Xml.Linq.")]
        [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage", Justification = "This method has also been marked as obsolete.")]
        public static void EnsureSiteColumns(SPWeb web, string elementFeatureAndFile) {
            XmlDocument elementDoc = SPFeatureXmlTools.GetConfigFile(elementFeatureAndFile);
            EnsureColumns(web, elementDoc);
        }

        /// <summary>
        /// Given an element file and a web, ensures the fields have been created.
        /// Uses web service, rather than provisioning directly through a feature,
        /// which allows for some interesting "hacks".
        /// </summary>
        /// <param name="web">The target web</param>
        /// <param name="elementDoc">XmlDocument of the element.xml file with Feature nodes</param>
        [Obsolete("SPContentTypeFeatureTools is obsolete. Please user SPContentTypeFeatureToolsX to take advantage of new framework features in System.Xml.Linq.")]
        [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage", Justification = "This method has also been marked as obsolete.")]
        public static void EnsureColumns(SPWeb web, XmlDocument elementDoc) {
            XmlDocument currentFieldsDoc = SPWebServicesClientFactory.GetColumns(web);
            XmlNode newColumns = BuildSiteColumnsWebServiceFieldsNode(currentFieldsDoc, elementDoc, BuildWebServiceFieldsNodeType.NewFields);
            XmlNode updateColumns = BuildSiteColumnsWebServiceFieldsNode(currentFieldsDoc, elementDoc, BuildWebServiceFieldsNodeType.ExstingFields);
            XmlNode deleteColumns = null; // BuildFieldsNode(xmlDoc, deleteFieldsXQuery, false);
            XmlNode result = SPWebServicesClientFactory.UpdateColumns(web, newColumns, updateColumns, deleteColumns);
        }

        /// <summary>
        /// Builds an individual fieldName node that will be used as an argument for UpdateColumns
        /// web method. Creates either either new or existing Fields, based delta of fields between
        /// an element manifest and current site columns.
        /// </summary>
        /// <param name="currentWebFieldDefs">The fields xml that was returned by GetColumns web method.</param>
        /// <param name="featureElementsFieldDef">The xml document with the Field element definition</param>
        /// <param name="typeOfFields">Determines whether you want to add fields that already exist, or fields that are new.</param>
        /// <returns></returns>
        private static XmlNode BuildSiteColumnsWebServiceFieldsNode(
            XmlDocument currentWebFieldDefs,
            XmlDocument featureElementsFieldDef,
            BuildWebServiceFieldsNodeType typeOfFields
        ) {
            int methodNumber = 0;
            // XmlNamespaceManager nsmgr, was removed because we stripped xmlns from the Xml source
            XmlDocument fieldsDoc = new XmlDocument();
            /*
            //was removed because we stripped xmlns from the Xml source
            XmlNamespaceManager nsmgr2 = new XmlNamespaceManager(fieldsDoc.NameTable);
            nsmgr2.AddNamespace("", MOSS_SOAP_NAMESPACE);
             */
            XmlNode fields = fieldsDoc.CreateElement("Fields");
            fieldsDoc.AppendChild(fields);

            // determine if the fieldName in the element list is already in the web
            // I find this code so damn frustrating to get working!!!
            /* 
            string queryAllElementFieldsDefs = "/Elements/Field";
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(featureElementsFieldDef.NameTable);
            nsmgr.AddNamespace(string.Empty, MOSS_SOAP_NAMESPACE);
            XmlNodeList qryElementFields = featureElementsFieldDef.SelectNodes(queryAllElementFieldsDefs, nsmgr); 
            */
            // ... just do this instead 
            XmlNodeList qryElementFields = GetSiteColumnDefs(featureElementsFieldDef);
            foreach (XmlNode qryElementField in qryElementFields) {
                // cycle through all the fields in the element file...
                string qryFieldName = qryElementField.Attributes["Name"].Value;
                string queryExistingFieldByName = string.Format("/Fields/Field[@Name='{0}']", qryFieldName); // + " or @DisplayName='{0}']"
                XmlNodeList qryExistingField = currentWebFieldDefs.SelectNodes(queryExistingFieldByName); // nsmgr
                // fieldName was found on the web's fieldName list...
                bool existingField = (qryExistingField != null && qryExistingField.Count > 0);
                if (existingField && typeOfFields == BuildWebServiceFieldsNodeType.ExstingFields)
                    AppendFieldXml(qryElementField, fieldsDoc, fields, methodNumber++);
                if (!existingField && typeOfFields == BuildWebServiceFieldsNodeType.NewFields)
                    AppendFieldXml(qryElementField, fieldsDoc, fields, methodNumber++);
            }
            return fields;
            // GC will come by for fieldsDoc ... eventually... I hope... :-S
        }

        #endregion

        #region Applies to both Site Columns and Content Types

        /// <summary>
        /// Creates a single fieldName node for use in calling the UpdateColumns web service.
        /// </summary>
        /// <param name="fieldsDoc">XmlDocument that contains targetFieldsNode</param>
        /// <param name="targetFieldsNode">the Fields node that the fieldName will be appended into</param>
        /// <param name="methodNumber">A method number, caller must increment for each node</param>
        /// <param name="sourceField">The xml fieldName that will be imported (copied) to make the Field node</param>
        private static void AppendFieldXml(
            XmlNode sourceField,
            XmlDocument fieldsDoc,
            XmlNode targetFieldsNode,
            int methodNumber
        ) {
            if (fieldsDoc == null)
                throw new XmlException("fieldsDoc was null!");
            if (targetFieldsNode == null || targetFieldsNode.Name != "Fields")
                throw new XmlException("targetFieldsNode has an unexpected value. You can only supply a Fields node.");
            if (sourceField == null || (sourceField.Name != "Field" && sourceField.Name != "FieldRef"))
                throw new XmlException("sourceField has an unexpected value. You can only append Field or FieldRef elements to this node list.");
            bool newIdGuid = false; // elements coming froma file already have a deterministic ID
            bool removeAttribs = true; // these attribs always cause trouble... I think...

            XmlElement method = fieldsDoc.CreateElement("Method");
            method.SetAttribute("ID", methodNumber.ToString());
            targetFieldsNode.AppendChild(method);

            // use ImportNode instead of CloneNode when you are copying between Xml documents
            XmlElement field = null;
            if (sourceField.Name == "Field")
                field = fieldsDoc.ImportNode(sourceField, true) as XmlElement;
            else if (sourceField.Name == "FieldRef") {
                // this is provided to support content type definitions
                field = fieldsDoc.CreateElement("Field");
                if (sourceField.Attributes["ID"] != null)
                    field.SetAttribute("ID", sourceField.Attributes["ID"].Value);
                if (sourceField.Attributes["Name"] != null)
                    field.SetAttribute("Name", sourceField.Attributes["Name"].Value);
                if (sourceField.Attributes["Required"] != null)
                    field.SetAttribute("Required", sourceField.Attributes["Required"].Value);
                if (sourceField.Attributes["Hidden"] != null)
                    field.SetAttribute("Hidden", sourceField.Attributes["Hidden"].Value);
                if (sourceField.Attributes["ReadOnly"] != null)
                    field.SetAttribute("ReadOnly", sourceField.Attributes["ReadOnly"].Value);
            }
            if (field == null || field.Name != "Field")
                throw new XmlException("xml node import returned an unexpected result. This node list should contain only Field elements.");
            // I thought you had to remove ID from the newFields, but I guess not...
            // see: http://sharepointandstuff.blogspot.com/2009/01/how-to-addupdatedelete-site-columns-by.html
            if (newIdGuid)
                field.SetAttribute("ID", Guid.NewGuid().ToString());
            if (removeAttribs) {
                field.RemoveAttribute("Version"); // removed because of problem described in http://www.sharepointblogs.com/abdrasin/archive/2007/10/31/site-column-update-error-the-object-has-been-updated-by-another-user-since-it-was-last-fetched.aspx
                field.RemoveAttribute("StaticName"); // not sure if I should've removed this or not
                field.RemoveAttribute("SourceID"); // not sure if I should've removed this or not
            }
            method.AppendChild(field);
        }

        /*
        private static void RemovePrefixes(XmlNode node) {
            node.Prefix = "";
            foreach (XmlNode subNode in node.ChildNodes) {
                RemovePrefixes(subNode);
            }
        }

        /// <summary>
        /// I just get so sick of this crap that comes back from web services
        /// with namespaces on it that make it next to impossible to get the
        /// XPath syntax correct! This method strips those namespaces out.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        private static XmlDocument CreateCleanXmlDocument(XmlNode xml) {
            XmlDocument xmlDoc = new XmlDocument();
            string rawXml = xml.OuterXml;
            string strippedXml = rawXml.Replace("xmlns=\"" + MOSS_SOAP_NAMESPACE + "\"", string.Empty);
            strippedXml = strippedXml.Replace("xmlns=\"" + MOSS_NAMESPACE + "\"", string.Empty);
            // end get...
            xmlDoc.LoadXml(XML_DOC_HEAD + strippedXml);
            return xmlDoc;
        }
         */

        #endregion

    }

    /*
    public enum BuildWebServiceFieldsNodeType {
        NewFields = 1,
        ExstingFields = 2,
        DeleteFields = 3
    } */

#endif

} // namespace
