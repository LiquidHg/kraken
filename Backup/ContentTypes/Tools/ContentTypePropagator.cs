// <copyright file="ContentTypePropogator.cs" company="Colossus Consulting LLC">
// Copyright (c)2003-2010. All Right Reserved.
// </copyright>
// <author>Thomas Carpe</author>
// <email>spark@thomascarpe.com</email>
// <date>2010-03-01</date>
// <summary></summary>

namespace Kraken.SharePoint.ContentTypes {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Text;

    using Microsoft.SharePoint;
  using Microsoft.SharePoint.Administration;
  using Microsoft.SharePoint.StsAdmin;

  using Kraken.SharePoint.Logging;

  public class ContentTypePropagator { 

    KrakenLoggingService uls = KrakenLoggingService.CreateNew(LoggingCategories.KrakenContentTypes);

        public void RefreshListContentTypes(
            SPSite site,
            List<string> contentTypeNames,
            bool updateFields,
            bool removeFields,
            bool processSubWebs,
            bool forceUpdate
        ) {
            SPWeb web = site.RootWeb;
            RefreshListContentTypes(web, contentTypeNames, updateFields, removeFields, processSubWebs, forceUpdate);
        }

        public void RefreshListContentTypes(
            SPWeb web,
            List<string> contentTypeNames,
            bool updateFields,
            bool removeFields,
            bool processSubWebs,
            bool forceUpdate
        ) {
            //Get the source site content type
            List<SPContentType> sourceContentTypes = new List<SPContentType>();
            foreach (string contentTypeName in contentTypeNames) {
                SPContentType sourceContentType = web.AvailableContentTypes[contentTypeName];
                if (sourceContentType == null)
                    throw new ArgumentException(string.Format("Unable to find contenttype named '{0}'.", contentTypeName));
                sourceContentTypes.Add(sourceContentType);
            }
            RefreshListContentTypes(web, sourceContentTypes, updateFields, removeFields, processSubWebs, forceUpdate);
        }

        public void RefreshListContentTypes(
            SPSite site,
            List<SPContentType> sourceContentTypes,
            bool updateFields,
            bool removeFields,
            bool processSubWebs,
            bool forceUpdate
        ) {
            SPWeb web = site.RootWeb;
            RefreshListContentTypes(web, sourceContentTypes, updateFields, removeFields, processSubWebs, forceUpdate);
        }

        /// <summary>
        /// Go through a web, all lists and sync with the source content
        /// type. Go recursively through all sub webs.
        /// </summary>
        /// <param name="contentTypeName">Name of the site content type</param>
        /// <param name="web">web to process list's within</param>
        /// <param name="site">site to get the content type from</param>
        /// <param name="sourceContentTypes">Source (site) content type</param>
        /// <param name="updateFields"></param>
        /// <param name="removeFields">
        /// WARNING: This option has not been adequately tested 
        /// (though what could go wrong? ... )
        /// </param>
        /// <param name="processSubWebs">Set to true to recurse through all subwebs</param>
        /// <param name="forceUpdate">If true, will perform an update even if the schema Xml is the same</param>
        public void RefreshListContentTypes(
            SPWeb web,
            List<SPContentType> sourceContentTypes,
            bool updateFields,
            bool removeFields,
            bool processSubWebs,
            bool forceUpdate
        ) {
            // Do work on lists on this web
            uls.Write(string.Format("Processing web '{0}' for ContentType update.", web.Url), TraceSeverity.Medium, EventSeverity.Information);

            // Grab the lists first, to avoid messing up an enumeration while looping through it.
            List<Guid> lists = new List<Guid>();
            foreach (SPList list in web.Lists)
                lists.Add(list.ID);
            foreach (Guid listId in lists) {
                SPList list = web.Lists[listId];
                if (list.ContentTypesEnabled) {
                  uls.Write(string.Format("Processing list: {0}/{1}", list.ParentWebUrl, list.Title), TraceSeverity.Medium, EventSeverity.Information);

                    foreach (SPContentType sourceContentType in sourceContentTypes) {
                        string contentTypeName = sourceContentType.Name;
                        SPContentType listContentType = null;
                        try {
                            listContentType = list.ContentTypes[contentTypeName];
                        } catch (NullReferenceException ex) {
                            // not really sure why this might happen but we should log it.
                            string msg = string.Format("Failed to get content type collection for web '{0}'.", web.Url);
                            uls.Write(msg, TraceSeverity.Unexpected, EventSeverity.Error);
                            uls.Write(ex);
                        }
                        if (listContentType != null) {
                          uls.Write(string.Format("Processing content type on list: {0}", list), TraceSeverity.Medium, EventSeverity.Information);

                            bool removeDuplicateFieldLinks = true;
                            if (removeDuplicateFieldLinks) {
                                List<SPFieldLink> listFieldLinksToRemove = new List<SPFieldLink>();
                                List<Guid> alreadyExists = new List<Guid>();
                                foreach (SPFieldLink listFieldLink in listContentType.FieldLinks) {
                                    if (alreadyExists.Contains(listFieldLink.Id)) {
                                        // add the field link reference to the collection of objects we are going to remove from fields list collection...
                                        listFieldLinksToRemove.Add(listFieldLink);
                                    } else {
                                        // first time we are seeing this field, add it to the list of existing field links
                                        alreadyExists.Add(listFieldLink.Id);
                                    }
                                } // foreach
                                foreach (SPFieldLink listFieldLink in listFieldLinksToRemove) {
                                    uls.Write(string.Format(
                                        "Removing field '{0}' from ContentType {1} on: {2}/{3}.",
                                       listFieldLink.DisplayName,
                                       contentTypeName,
                                       list.ParentWebUrl,
                                       list.Title), TraceSeverity.Verbose, EventSeverity.Verbose);
                                    // TODO not sure if maybe at this point did we just remove an object or did we remove ALL objects
                                    listContentType.FieldLinks.Delete(listFieldLink.Id);
                                    listContentType.Update();
                                } // foreach
                            }
                            
                            if (updateFields) {
                                UpdateListFields(list, listContentType, sourceContentType, forceUpdate);
                            }

                            /*
                            // Find/add the fields to add
                            foreach (SPFieldLink sourceFieldLink in sourceContentTypes.FieldLinks) {
                                if (!SPFieldTools.FieldExists(sourceContentTypes.Fields, sourceFieldLink.Id)) {
                                    uls.Write(string.Format(
                                      "Failed to add fieldName {0} on list {1}/{2}. Field does not exist (in .Fields[]) on source content type '{3}'.",
                                      sourceFieldLink.DisplayName,
                                      list.ParentWebUrl,
                                      list.Title,
                                      contentTypeName
                                    ));
                                    continue;
                                }
                                if (SPFieldTools.FieldExists(listContentType.Fields, sourceFieldLink.Id)) {
                                    // Performs double update, just to be safe (but slow)
                                    // delete it
                                    uls.Write(string.Format(
                                       "Deleting fieldName '{0}' to ContentType on '{1}/{2}'.",
                                       sourceFieldLink.DisplayName, list.ParentWebUrl, list.Title
                                    ), TraceSeverity.Verbose);
                                    if (listContentType.FieldLinks[sourceFieldLink.Id] != null) {
                                        listContentType.FieldLinks.Delete(sourceFieldLink.Id);
                                        listContentType.Update();
                                    }
                                    uls.Write(string.Format(
                                       "Adding fieldName '{0}' to ContentType on '{1}/{2}'.",
                                       sourceFieldLink.DisplayName, list.ParentWebUrl, list.Title
                                    ), TraceSeverity.Verbose);
                                    // re-add it
                                    listContentType.FieldLinks.Add(new SPFieldLink(sourceContentTypes.Fields[sourceFieldLink.Id]));
                                    listContentType.Update();
                                }
                            } // foreach
                            */

                            if (removeFields) { // Find the fields to delete 
                                // Copy collection to avoid modifying enumeration
                                // as we go through it
                                List<SPFieldLink> listFieldLinks = new List<SPFieldLink>();
                                foreach (SPFieldLink listFieldLink in listContentType.FieldLinks) {
                                    if (!sourceContentType.Fields.FieldExists(listFieldLink.Id)) {
                                        listFieldLinks.Add(listFieldLink);
                                    }
                                } // foreach
                                foreach (SPFieldLink listFieldLink in listFieldLinks) {
                                    uls.Write(string.Format(
                                        "Removing field '{0}' from ContentType {1} on: {2}/{3}.",
                                        listFieldLink.DisplayName,
                                        contentTypeName,
                                        list.ParentWebUrl,
                                        list.Title), TraceSeverity.Verbose, EventSeverity.Verbose);
                                    listContentType.FieldLinks.Delete(listFieldLink.Id);
                                    listContentType.Update();
                                } // foreach
                            } // if removeFields
                        } // if targetListContentType != null
                    } //foreach 
                } // if list.ContentTypesEnabled
            } // fn
            //Process sub webs
            if (processSubWebs) {
                foreach (SPWeb subWeb in web.Webs) {
                    RefreshListContentTypes(subWeb, sourceContentTypes, updateFields, removeFields, processSubWebs, forceUpdate);
                    subWeb.Dispose();
                }
            }
        }

        /// <summary>
        /// Updates the fields of the list content type (targetListContentType) with the
        /// fields found on the source content type (courceCT).
        /// </summary>
        /// <param name="list"></param>
        /// <param name="targetListContentType"></param>
        /// <param name="sourceContentTypes"></param>
        /// <param name="forceUpdate">If true, will perform an update even if the schema Xml is the same</param>
        private void UpdateListFields(
            SPList list,
            SPContentType targetListContentType,
            SPContentType sourceContentType,
            bool forceUpdate
        ) {
          uls.Write(string.Format("Starting to update fields for list {0}/{1}.", list.ParentWebUrl, list.Title), TraceSeverity.Medium, EventSeverity.Information);
            foreach (SPFieldLink sourceFieldLink in sourceContentType.FieldLinks) {
                //has the fieldName changed? If not, continue.
                SPFieldLink listLink = targetListContentType.FieldLinks[sourceFieldLink.Id];
                if (listLink != null && !forceUpdate && listLink.SchemaXml == sourceFieldLink.SchemaXml) {
                    uls.Write(string.Format(
                      "Doing nothing to field '{0}' from contenttype on '{1}/{2}'. List content type field link is not different than site content type field link.",
                      sourceFieldLink.Name,
                      list.ParentWebUrl,
                      list.Title
                    ), TraceSeverity.Verbose, EventSeverity.Verbose);
                    continue;
                }
                if (!sourceContentType.Fields.FieldExists(sourceFieldLink.Id)) {
                    uls.Write(string.Format(
                        "Doing nothing to field '{0}' from ContentType on '{1}/{2}'. Field does not exist (in .Fields[]) on source content type.",
                        sourceFieldLink.DisplayName,
                        list.ParentWebUrl,
                        list.Title
                    ), TraceSeverity.Verbose, EventSeverity.Verbose);
                    continue;
                }
                if (targetListContentType.FieldLinks[sourceFieldLink.Id] != null) {
                    uls.Write(string.Format(
                        "Deleting field link '{0}' from ContentType on: '{1}/{2}'.",
                        sourceFieldLink.Name,
                        list.ParentWebUrl,
                        list.Title
                    ), TraceSeverity.Monitorable, EventSeverity.Information);
                    targetListContentType.FieldLinks.Delete(sourceFieldLink.Id);
                    targetListContentType.Update();
                }
                uls.Write(string.Format("Adding field '{0}' from ContentType on : '{1}/{2}'.",
                    sourceFieldLink.Name,
                    list.ParentWebUrl,
                    list.Title
                ), TraceSeverity.Verbose, EventSeverity.Verbose);
                SPField field = sourceContentType.Fields[sourceFieldLink.Id];
                SPFieldLink link = new SPFieldLink(field);
                // found this was creating duplicate field links somehow
                /*
                if (!sourceFieldLink.Name.Equals(sourceFieldLink.DisplayName, StringComparison.CurrentCulture))
                    link.DisplayName = sourceContentType.FieldLinks[sourceFieldLink.Id].DisplayName;
                else */
                // Set displayname, not set by previus operation
                link.DisplayName = field.Title;
                targetListContentType.FieldLinks.Add(link);
                targetListContentType.Update();
            } // foreach
            uls.Write("Done updating fields.", TraceSeverity.Medium, EventSeverity.Information);
        }

    } // class

} // namespace
