using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

using Kraken.SharePoint.Logging;
using Kraken.SharePoint.Users;

namespace Kraken.SharePoint {
  public static class SPUserExtensions {

    public static SPUser TryGetSiteUser(this SPWeb web, string loginName, string alternateLoginName) {
      KrakenLoggingService.Default.Write(string.Format("Entering '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      SPUser user = web.TryGetSiteUser(loginName);
      if (user == null)
        user = web.TryGetSiteUser(alternateLoginName);
      KrakenLoggingService.Default.Write(string.Format("Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      return user;
    }
    public static SPUser TryGetSiteUser(this SPWeb web, string loginName) {
      KrakenLoggingService.Default.Write(string.Format("Entering '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      KrakenLoggingService.Default.Write(string.Format("loginName = '{0}'", loginName), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      SPUser user = null;
      try {
        user = web.SiteUsers[loginName];
      } catch (Exception ex) {
        // decreased the severity of this message as it doesn't necessarily indicate an error
        Kraken.SharePoint.Logging.KrakenLoggingService.Default.Write(string.Format(
          "{0}: user '{1}' did not exist in the site collection '{2}'. {3}",
          MethodBase.GetCurrentMethod(),
          loginName,
          web.Site.Url,
          ex.Message
        ), TraceSeverity.Monitorable, EventSeverity.Verbose, Logging.LoggingCategories.KrakenSecurity);
      }
      KrakenLoggingService.Default.Write(string.Format("Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      return user;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// You might need to run this elevated if you want it to work properly.
    /// </remarks>
    /// <param name="user"></param>
    /// <param name="overwriteExisting"></param>
    /// <param name="properties"></param>
    public static void UpdateUserInfo(this SPUser user, bool overwriteExisting, Dictionary<string, string> properties) {
      // TODO make me even more secure if possible
      if (SPContext.Current != null && SPContext.Current.Web != null && SPContext.Current.Web.CurrentUser.ID != user.ID)
        throw new NotSupportedException("For security reasons, you may not call this method on anyone other than the current user.");
      List<string> allowedProperties = new List<string>() {
        UserInfoFieldConstants.AboutMe,
        UserInfoFieldConstants.Department,
        UserInfoFieldConstants.EMail,
        UserInfoFieldConstants.JobTitle,
        UserInfoFieldConstants.Name,
        UserInfoFieldConstants.SIPAddress
        //UserInfoFieldConstants.Picture
      };
      try {
          SPUser userAsAdmin = user.ParentWeb.TryGetSiteUser(user.LoginName);
          SPWeb web = userAsAdmin.ParentWeb;
          SPList userList = web.SiteUserInfoList;
          SPListItem userItem = userList.Items.GetItemById(userAsAdmin.ID);
          web.AllowUnsafeUpdates = true;
          foreach (string field in properties.Keys) {
            if (!allowedProperties.Contains(field))
              throw new NotSupportedException(string.Format("Ability to set user info property '{0}' has been restricted. It is not int he whitelist.", field));
            if (overwriteExisting || string.IsNullOrEmpty(userItem[field] as string))
              userItem[field] = properties[field];
          }
          userItem.Update();
          //web.AllowUnsafeUpdates = false;
      } catch (Exception ex) {
        KrakenLoggingService.Default.Write(string.Format("Could not save SPUser changes for user name '{0}'.", user.LoginName), TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenUnknown);
        KrakenLoggingService.Default.Write(ex, LoggingCategories.KrakenUnknown);
      }
    }


  }
}
