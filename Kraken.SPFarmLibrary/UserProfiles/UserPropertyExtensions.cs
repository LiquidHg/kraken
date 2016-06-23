using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.Office.Server.UserProfiles;

using Kraken.SharePoint.Users;
using Kraken.SharePoint.IdentityModel;
using Kraken.SharePoint.Logging;

namespace Kraken.SharePoint.UserProfiles {

  public static class UserPropertyExtensions {

    /// <summary>
    /// Tries to get the user profile from a user name. If it fails, tries to strip out everything before the final |
    /// </summary>
    /// <param name="upm"></param>
    /// <param name="loginName"></param>
    /// <returns></returns>
    public static UserProfile TryGetUserProfile(this UserProfileManager upm, string loginName) {
      KrakenLoggingService.Default.Write(string.Format("Entering '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      UserProfile result = null;
      try {
          result = upm.GetUserProfile(loginName);
      } catch (UserNotFoundException) {
          if (loginName.Contains("|")) {
              string[] pieces = loginName.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
              result = upm.GetUserProfile(pieces[pieces.GetUpperBound(0)]);
          } else {
            KrakenLoggingService.Default.Write(string.Format("User '{0}' not found.", loginName), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
          }
      } catch (Exception ex2) {
        KrakenLoggingService.Default.Write(ex2, LoggingCategories.KrakenProfiles);
      } finally {
        KrakenLoggingService.Default.Write(string.Format("Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      }
      return result;
    }

    public static string TryGetUserProfileProperty(this UserProfile profile, string propertyName) {
      string value = default(string);
      try {
        if (profile == null) throw new ArgumentNullException("profile");
        if (profile[propertyName] == null)
          return value;
        if (profile[propertyName].Value == null)
          return value;
        if (profile[propertyName].Count == 1)
        {
            value = profile[propertyName].Value.ToString();
        }
        else if (profile[propertyName].Count > 1)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in profile[propertyName])
            {
                sb.Append(s + "; ");
            }
            value = sb.ToString().Trim();
            if (value.EndsWith(";"))
                value = value.Remove(value.Length - 1);
        }
      } catch (Exception ex) {
        // TODO do something else here?
        value = "#ERROR#: " + ex.Message;
      }
      return value;
    }

    /// <summary>
    /// Attempts to get the long form account name from a userAsAdmin's profile.
    /// </summary>
    /// <param name="profile"></param>
    /// <returns></returns>
    public static string TryGetAccountName(this UserProfile profile) {
      //string userName = profile.TryGetUserProfileProperty(PropertyConstants.ClaimID); // commented because often completely empty
      //userName = profile.TryGetUserProfileProperty(PropertyConstants.UserName); // commented because it is only the short name (part after the domain)
      string userName = profile.TryGetUserProfileProperty(PropertyConstants.AccountName);
      // TODO determined if PropertyConstants.SAMUserName and PropertyConstants.UserName are similar enough to use this way
#if DOTNET_V35
      if (string.IsNullOrEmpty(userName))
        userName = profile.TryGetUserProfileProperty(PropertyConstants.SAMUserName);
#else
      if (string.IsNullOrEmpty(userName))
        userName = profile.TryGetUserProfileProperty(PropertyConstants.UserName);
#endif
      return userName;
    }

    /// <summary>
    /// Attempts to get the claims provider for a user's profile.
    /// Actually, never seems to return any values.
    /// </summary>
    /// <param name="profile"></param>
    /// <returns></returns>
    public static string TryGetClaimsProvider(this UserProfile profile) {
      string provider = profile.TryGetUserProfileProperty(PropertyConstants.ClaimProviderID);
      // these two constants have the same value
      //if (string.IsNullOrEmpty(provider))
      //  provider = profile.TryGetUserProfileProperty(PropertyConstants.SPSClaimProviderID);
      return provider;
    }

    private static KrakenLoggingService uls = new KrakenLoggingService();

    private static SimpleClaimsDecoder decoder = new SimpleClaimsDecoder(true);

    public static string TryGetAuthProvider(this UserProfile profile, bool useStsDataToNormalize) {
      uls.DefaultCategory = LoggingCategories.KrakenClaims;
      uls.Entering(MethodBase.GetCurrentMethod());
      string userName = profile.TryGetAccountName();
      uls.Write(string.Format("userName = {0}", userName), TraceSeverity.Verbose, EventSeverity.Verbose);
      if (string.IsNullOrEmpty(userName))
        return default(string);
      EncodedClaimInfo info = decoder.DecodeFully(userName);
      string provider = info.ProviderName;
      if (useStsDataToNormalize)
        provider = UserProfileUtilities.ProperCaseLoginProviderName(provider);
      return provider;
    }

    // TODO does SharePoint's user profile library provide any functions for doing this sort of thing?

    /// <summary>
    /// Attempt to get the display name from profile Title property;
    /// failing that, attempts to combine first and last name.
    /// </summary>
    /// <param name="profile"></param>
    /// <returns></returns>
    public static string TryGetDisplayName(this UserProfile profile, ProfileDisplayNameFormatType displayNameFormat, bool useStsDataToNormalize) {
      string name = profile.TryGetUserProfileProperty(PropertyConstants.PreferredName);
      if (string.IsNullOrEmpty(name)) {
        name = string.Format(
          "{0} {1}",
          profile.TryGetUserProfileProperty(PropertyConstants.FirstName),
          profile.TryGetUserProfileProperty(PropertyConstants.LastName)
        ).Trim();
      } else {
        // check if preferred name is the long form account name, and shorten it.
        if (string.Equals(name, TryGetAccountName(profile), StringComparison.InvariantCultureIgnoreCase)) {
          name = profile.TryGetUserProfileProperty(PropertyConstants.UserName);
        }
      }
      if (string.IsNullOrEmpty(name))
        name = profile.TryGetUserProfileProperty(PropertyConstants.UserName); // short userAsAdmin name (no domain or provider)
      if (displayNameFormat == ProfileDisplayNameFormatType.NameOnly)
        return name;
      // append userAsAdmin name to display name
      name = string.Format(
        "{0} [{1}]",
        name,
        (displayNameFormat == ProfileDisplayNameFormatType.NameWithFullClaimUserName)
        ? profile.TryGetUserProfileProperty(PropertyConstants.UserName)
        : TryGetAuthProvider(profile, useStsDataToNormalize)
      ).Trim();
      return name;
    }

    /// <summary>
    /// Copies profile infomation to teh userAsAdmin/userAsAdmin information list of a SharePoint site collection.
    /// Only performs a copy if the profile.PrefferedName does not match the userAsAdmin.Name
    /// </summary>
    /// <param name="profile"></param>
    /// <param name="userAsAdmin"></param>
    /// <returns>Returns true if the userAsAdmin was updated</returns>
    public static bool SyncProfileToUserInfo(this UserProfile profile, SPUser user, bool useStsDataToNormalize) {
      KrakenLoggingService.Default.Write(string.Format("Entering '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      string name = profile.TryGetDisplayName(ProfileDisplayNameFormatType.NameOnly, useStsDataToNormalize);
      // About the only way to tell if other properties need to be edited is to compare names
      if (name.Equals(user.Name, StringComparison.InvariantCultureIgnoreCase)) {
        KrakenLoggingService.Default.Write(string.Format("Leaving '{0}' without any action because Name properties match in profile and user info.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
        return false;
      }
      try {
        SPSecurity.RunWithElevatedPrivileges(delegate() {
          string loginName = user.LoginName;
          SPWeb web = user.ParentWeb;
          SPUser userAsAdmin = web.TryGetSiteUser(loginName, profile.TryGetAccountName());
          string value;

          // all other attributes must be set using the UserInfo list
          userAsAdmin.ParentWeb.AllowUnsafeUpdates = true;
          SPList userList = web.SiteUserInfoList;
          SPListItem userItem = userList.Items.GetItemById(userAsAdmin.ID);
          value = profile.TryGetUserProfileProperty(PropertyConstants.AboutMe);
          if (string.IsNullOrEmpty(value))
            userItem[UserInfoFieldConstants.AboutMe] = value;
          value = profile.TryGetUserProfileProperty(PropertyConstants.Department);
          if (string.IsNullOrEmpty(value))
            userItem[UserInfoFieldConstants.Department] = value;
          value = profile.TryGetUserProfileProperty(PropertyConstants.JobTitle);
          if (string.IsNullOrEmpty(value))
            userItem[UserInfoFieldConstants.JobTitle] = value;
          //userItem[UserInfoFieldConstants.Picture] = value;
          value = profile.TryGetUserProfileProperty(PropertyConstants.SipAddress);
          if (string.IsNullOrEmpty(value))
            userItem[UserInfoFieldConstants.SIPAddress] = value;
          userItem.Update();
          //userAsAdmin.ParentWeb.AllowUnsafeUpdates = false;

          // These are the ones that can be done in a more conventional way
          value = profile.TryGetUserProfileProperty(PropertyConstants.WorkEmail);
          if (string.IsNullOrEmpty(value)) userAsAdmin.Email = value;
          userAsAdmin.Name = name;
          userAsAdmin.Update();
        }); // elevate
      } catch (Exception ex) {
        KrakenLoggingService.Default.Write(string.Format("Could not save SPUser changes for user name '{0}'.", user.LoginName), TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenUnknown);
        KrakenLoggingService.Default.Write(ex, LoggingCategories.KrakenUnknown);
        return false;
      }
      KrakenLoggingService.Default.Write(string.Format("Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      return true;
    }

  } // class

  public enum ProfileDisplayNameFormatType {
    NameOnly,
    NameWithProvider,
    NameWithShortUserName,
    NameWithFullClaimUserName
  }

} // namespace
