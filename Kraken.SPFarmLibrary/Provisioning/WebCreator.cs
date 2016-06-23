using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kraken.SharePoint.Logging;
using System.Reflection;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint;

namespace Kraken.SharePoint.Provisioning {

    public class WebCreator
    {

        public static SPWeb EnsureWeb(SPWeb web, string errorMessage)
        {
            if (web == null)
            {
                if (SPContext.Current == null)
                {
                    KrakenLoggingService.Default.Write(errorMessage, TraceSeverity.Monitorable, EventSeverity.Information, LoggingCategories.KrakenUnknown);
                    return null;
                }
                else
                    web = SPContext.Current.Web;
            }
            return web;
        }

        public static SPWeb CreateWeb(
          SPWeb web,
          string url,
          string title,
          string description,
          string templateName,
          bool inheritPermissions
        )
        {
            SPWeb newWeb = null;
            KrakenLoggingService.Default.Write(string.Format("Entering '{0}'.", MethodBase.GetCurrentMethod().Name), 
                TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenUnknown);
            web = EnsureWeb(web, string.Format("Can't create web; no SPWeb was provided and SPConext.Current.Web is NULL", ""));
            if (web == null)
                return null;
            try
            {
                uint lcid = (UInt32)System.Globalization.CultureInfo.CurrentCulture.LCID;

                // get custom template from string
                SPWebTemplateCollection templates = web.GetAvailableWebTemplates(lcid);
                SPWebTemplate template = (from t in templates.Cast<SPWebTemplate>()
                                          where t.Name == templateName || t.Title == templateName
                                          select t).FirstOrDefault<SPWebTemplate>();
                if (template == null)
                {
                    throw new Exception(string.Format(
                        "Could not find available web template with name or title '{0}' at web '{1}'.", templateName, web.Url));
                }
                // create the web site
                newWeb = web.Webs.Add(url, title, description, lcid, template, !inheritPermissions, false);
            }
            catch (Exception ex)
            {
                // log the exception
                KrakenLoggingService.Default.Write(ex);
            }
            finally
            {
                // we are using the web from SPContext so no need to Dispose it
            }
            KrakenLoggingService.Default.Write(string.Format("Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), 
                TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenUnknown);
            return newWeb;
        }

        public static bool SetUniquePermissions(
          SPWeb web,
          PermissionType permissionLevel,
          SPGroup existingGroup,
          string newGroupName,
          string newGroupDescription,
          List<SPUser> newGroupMembers,
            //, bool sendUserInvitations not currently implemented
          out List<Exception> exceptions
          )
        {
            KrakenLoggingService.Default.Write(string.Format("Entering '{0}'.", MethodBase.GetCurrentMethod().Name), 
                TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenUnknown);
            exceptions = new List<Exception>();
            if (!web.HasUniqueRoleAssignments)
            {
                exceptions.Add(new Exception(string.Format("This method cannot be called on web sites with inherited permissions. url='{0}'", web.Url)));
                KrakenLoggingService.Default.Write(string.Format("Leaving '{0}'. Tried to call on site with inherited permissions.", MethodBase.GetCurrentMethod().Name),
                    TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenUnknown);
                return false;
            }
            // set the new group's name and description
            if (string.IsNullOrEmpty(newGroupName))
                newGroupName = web.Title + " " + permissionLevel.ToString();
            if (string.IsNullOrEmpty(newGroupDescription))
                newGroupDescription = web.Title + " " + permissionLevel.ToString();
            if (SPContext.Current.Web.UserIsSiteAdmin)  //restrict this action to SCA's
            {
                bool result = false;
                List<Exception> elevatedExceptions = new List<Exception>();
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    web.AllowUnsafeUpdates = true;
                    try
                    {
                        SPGroup group = existingGroup;
                        if (existingGroup == null)
                        {
                            // create the group
                            try
                            {
                                SPMember theNewOwner = SPContext.Current.Web.CurrentUser;

                                web.SiteGroups.Add(newGroupName, theNewOwner, null, newGroupDescription);
                                web.Update();
                            }
                            catch (Exception ex)
                            {
                                KrakenLoggingService.Default.Write(ex); // log the exception
                                elevatedExceptions.Add(ex); // add error to a running list of errors
                                //if (ex.Message.Contains("fall within the expected range"))
                                return;
                                // may have failed because the group already existed; if so, continue
                            }
                            // get the newly created group
                            group = web.SiteGroups[newGroupName];
                            if (group == null)
                            {
                                KrakenLoggingService.Default.Write("Kraken->SharePoint->Provisioning->WebCreator->SetUniquePermissions, group [" +
                                    newGroupName + "] doesn't exist.", TraceSeverity.Monitorable, EventSeverity.Verbose );
                                return;
                            }
                            group.AllowMembersEditMembership = true;
                            group.OnlyAllowMembersViewMembership = false;
                            group.Update();
                            // add users to the new group
                            foreach (SPUser user in newGroupMembers)
                            {
                                try
                                {
                                    group.AddUser(user);
                                }
                                catch (Exception ex)
                                {
                                    KrakenLoggingService.Default.Write(ex); // log the exception
                                    elevatedExceptions.Add(ex); // add error to a running list of errors
                                    // may have failed because the user is already in the group; if so, continue
                                }
                            }
                        }
                        // add the role assignment and binding for the group in the web site
                        // the role definition is the permission set being granted
                        SPRoleAssignment roleAssignment = new SPRoleAssignment(group);
                        SPRoleDefinition roleDefinition = null;
                        // TODO make these configurable somehow
                        if (permissionLevel == PermissionType.Visitors)
                            roleDefinition = web.RoleDefinitions["Read"];
                        if (permissionLevel == PermissionType.Members)
                            roleDefinition = web.RoleDefinitions["Contribute"];
                        if (permissionLevel == PermissionType.Owners)
                            roleDefinition = web.RoleDefinitions["Full Control"];
                        try
                        {
                            roleAssignment.RoleDefinitionBindings.Add(roleDefinition);
                        }
                        catch (Exception ex)
                        {
                            KrakenLoggingService.Default.Write(ex); // log the exception
                            elevatedExceptions.Add(ex); // add error to a running list of errors
                            // may have failed because the role already exists; if so, continue
                        }
                        // update the web site
                        web.RoleAssignments.Add(roleAssignment);
                        web.Update();
                        web.AllowUnsafeUpdates = false;
                        result = true;
                    }
                    catch (Exception ex)
                    {
                        KrakenLoggingService.Default.Write(ex); // log the exception
                        elevatedExceptions.Add(ex); // add error to a running list of errors
                    }
                    finally
                    {
                        web.AllowUnsafeUpdates = false;
                        KrakenLoggingService.Default.Write(string.Format("Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), 
                            TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
                    }
                });
                exceptions.AddRange(elevatedExceptions);
                return result;
            }
            else
            {
                Exception ex = new UnauthorizedAccessException("*User does not have admin rights to manage group unique permissions. " +
                    "(validation check, user= " + SPContext.Current.Web.CurrentUser.Name + ")");
                KrakenLoggingService.Default.Write(ex); // log the exception
                exceptions.Add(ex); // add error to a running list of errors
            }
            return false;
        }

    }

  public enum PermissionType {
    Visitors,
    Members,
    Owners
  }

}
