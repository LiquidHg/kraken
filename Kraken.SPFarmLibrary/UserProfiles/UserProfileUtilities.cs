using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Web;

using Microsoft.Office.Server.UserProfiles;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Administration.Claims;

using Kraken.SharePoint.IdentityModel;
using Kraken.SharePoint.Logging;
using Kraken.SharePoint.UserProfiles;
using Kraken.SharePoint.Users;

namespace Kraken.SharePoint.UserProfiles {

  public static class UserProfileUtilities {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="site">SPSite to construct a context, or null to use SPContext.Current</param>
    /// <param name="fakeHttpContext">If true, construct our own HttpContext from the application pool user.</param>
    /// <param name="userName">User name of the profile to load</param>
    /// <param name="success">Tells us if we worked OK or if there was an error; will be true if the user was not found.</param>
    /// <returns></returns>
    /// <remarks>
    /// fakeHttpContext only works if you are running elevated.
    /// If you set fakeHttpContext to true, you need to call RestoreHttpContext when you're done.
    /// </remarks>
    public static UserProfile GetUserProfile(SPSite site, bool fakeHttpContext, string userName, out bool success) {
      success = false;
      KrakenLoggingService.Default.WriteStack(typeof(UserProfileUtilities), MethodBase.GetCurrentMethod().Name, false);
      if (site == null) {
        if (SPContext.Current == null) {
          KrakenLoggingService.Default.Write(string.Format("Abandoned attempt to get user profile because SPConext.Current is NULL for user name '{0}'.", userName), TraceSeverity.Monitorable, EventSeverity.Information, LoggingCategories.KrakenProfiles);
          return null;
        } else
          site = SPContext.Current.Site;
      }
      UserProfile result = null;
      try {
        // TODO do we need to dispose upm or profile?
        UserProfileManager upm = GetUserProfileManager(site, fakeHttpContext);
        if (upm == null) {
          KrakenLoggingService.Default.Write(string.Format("Could not create UserProfileManager from SPServiceContext for user name '{0}'.", userName), TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenProfiles);
          return null;
        }
        UserProfile profile = null;
        try {
          profile = upm.GetUserProfile(userName);
        } catch (UserNotFoundException) {
          KrakenLoggingService.Default.Write(string.Format("Could not get profile with user name '{0}'.", userName), TraceSeverity.Monitorable, EventSeverity.Information, LoggingCategories.KrakenProfiles);
          success = true;
          return null;
        }
        if (profile == null) {
          KrakenLoggingService.Default.Write(string.Format("Could not get profile with user name '{0}'.", userName), TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenProfiles);
          return null;
        }
        result = profile;
        success = true;
        KrakenLoggingService.Default.Write(string.Format("Successfully read user profile with user name '{0}'.", userName), TraceSeverity.Medium, EventSeverity.Information, LoggingCategories.KrakenProfiles);
      } catch (Exception ex) {
        KrakenLoggingService.Default.Write(string.Format("Unexpected error; could not get profile with user name '{0}'.", userName), TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenProfiles);
        KrakenLoggingService.Default.Write(ex, LoggingCategories.KrakenProfiles);
      } finally {
        KrakenLoggingService.Default.WriteStack(typeof(UserProfileUtilities), MethodBase.GetCurrentMethod().Name, true);
      }
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="site">SPSite to construct a context, or null to use SPContext.Current</param>
    /// <param name="fakeHttpContext">If true, construct our own HttpContext from the application pool user.</param>
    /// <param name="groupName">Name of the group in which to find profiles</param>
    /// <param name="success">Tells us if we worked OK or if there was an error; will be true if the user was not found.</param>
    /// <returns></returns>
    /// <remarks>
    /// fakeHttpContext only works if you are running elevated.
    /// If you set fakeHttpContext to true, you need to call RestoreHttpContext when you're done.
    /// </remarks>
    public static UserProfile[] GetUserProfilesInGroup(SPSite site, bool fakeHttpContext, string groupName, out bool success) {
      success = false;
      KrakenLoggingService.Default.Write(string.Format("Entering '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      if (site == null) {
        if (SPContext.Current == null) {
          KrakenLoggingService.Default.Write(string.Format("Abandoned attempt to get user profile because SPConext.Current is NULL for group name '{0}'.", groupName), TraceSeverity.Monitorable, EventSeverity.Information, LoggingCategories.KrakenProfiles);
          return null;
        } else
          site = SPContext.Current.Site;
      }

      List<UserProfile> results = new List<UserProfile>();
      try {
        // TODO do we need to dispose upm or profile?
        UserProfileManager upm = GetUserProfileManager(site, fakeHttpContext);
        if (upm == null) {
          KrakenLoggingService.Default.Write(string.Format("Could not create UserProfileManager from SPServiceContext for group name '{0}'.",
              groupName), TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenProfiles);
          return null;
        }
        SPGroup theGroup = null;
        SPSecurity.RunWithElevatedPrivileges(delegate() {
          foreach (SPGroup g in site.RootWeb.Groups) {
            if (g.Name.ToLowerInvariant() == groupName.ToLowerInvariant()) {
              theGroup = g;
              break;
            }
          }
          if (theGroup == null) {
            foreach (SPGroup g in site.RootWeb.SiteGroups) {
              if (g.Name.ToLowerInvariant() == groupName.ToLowerInvariant()) {
                theGroup = g;
                break;
              }
            }
          }
          if (theGroup == null) {
            KrakenLoggingService.Default.Write(string.Format("Could not get group with name '{0}'.",
                groupName), TraceSeverity.Monitorable, EventSeverity.Information, LoggingCategories.KrakenProfiles);
            //success = true;
            //return results.ToArray();
          } else {
            foreach (SPUser user in theGroup.Users) {
              UserProfile profile = null;
              try {
                profile = upm.GetUserProfile(user.LoginName);
              } catch (UserNotFoundException) {
                KrakenLoggingService.Default.Write(string.Format("Could not get profile for user in group {0} with name '{1}'.",
                    groupName, user.LoginName), TraceSeverity.Monitorable, EventSeverity.Information, LoggingCategories.KrakenProfiles);
              }
              if (profile == null) {
                KrakenLoggingService.Default.Write(string.Format("Could not get profile for user in group {0} with name '{1}'.",
                    groupName, user.LoginName), TraceSeverity.Monitorable, EventSeverity.Information, LoggingCategories.KrakenProfiles);
              } else {
                results.Add(profile);
              }
              //success = true;
              KrakenLoggingService.Default.Write(string.Format("Successfully read user profile with user name '{0}'.",
                  user.LoginName), TraceSeverity.Medium, EventSeverity.Information, LoggingCategories.KrakenProfiles);
            }
          }
        });
        if (theGroup != null)
          success = true;
      } catch (Exception ex) {
        KrakenLoggingService.Default.Write(string.Format("Unexpected error geting profiles in group '{0}'.", groupName), TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenProfiles);
        KrakenLoggingService.Default.Write(ex, LoggingCategories.KrakenProfiles);
      } finally {
        KrakenLoggingService.Default.Write(string.Format("Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      }
      return results.ToArray();
    }

    public static UserProfileManager GetUserProfileManager() {
      return GetUserProfileManager(null, false);
    }
    public static UserProfileManager GetUserProfileManager(SPSite site, bool useFakeHttpContext) {
      KrakenLoggingService.Default.WriteStack(typeof(UserProfileUtilities), MethodBase.GetCurrentMethod().Name, false);
      bool siteWasNullSetFromContext = false;
      if (site == null) {
        if (SPContext.Current == null) {
          KrakenLoggingService.Default.Write(string.Format("Abandoned attempt to get user profile manager because SPContext.Current is NULL."), TraceSeverity.Monitorable, EventSeverity.Information, LoggingCategories.KrakenProfiles);
          return null;
        }
        siteWasNullSetFromContext = true;
        site = SPContext.Current.Site;
      }

      UserProfileManager upm = null;
      try {
        SPServiceContext sc = null;
        if (useFakeHttpContext) {
          HttpContext context = CreateHttpContext(site, true);
          sc = SPServiceContext.GetContext(context);
        } else {
          sc = (siteWasNullSetFromContext) ? SPServiceContext.Current : SPServiceContext.GetContext(site);
        }
        if (sc == null) {
          KrakenLoggingService.Default.Write("Could not create SPServiceContext.", TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenProfiles);
          //KrakenLoggingService.Default.Write(string.Format("Abandoned attempt to get user profile because SPConext.Current is NULL for user name '{0}'.", userName), TraceSeverity.Monitorable, EventSeverity.Warning, LoggingCategories.KrakenProfiles);
          return null;
        }
        upm = new UserProfileManager(sc); // TODO do we need to dispose upm or profile?
        if (upm == null) {
          KrakenLoggingService.Default.Write("Could not create UserProfileManager from SPServiceContext.", TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenProfiles);
          return null;
        }
        if (upm == null)
          return null;
      } catch (UserProfileApplicationNotAvailableException upaEx) {
        KrakenLoggingService.Default.Write(upaEx, LoggingCategories.KrakenProfiles);
        List<string> msgs = new List<string>();
        msgs.Add(string.Format("Diagnostic for the above exception: fakeHttpContext='{0}'; siteWasNullSetFromContext={1}; site.Url='{2}'", useFakeHttpContext, siteWasNullSetFromContext, site.Url));
        msgs.Add("The following ULS log entries contain information and URLs that may help you fix this issue.");
        msgs.Add("Tip 1: Check that the provided site context above is valid.");
        msgs.Add("Tip 2: Ensure the specified account has Full Control permissions to UPA in Service Applications. You should see the identity in an error a few lines further down in the ULS logs.");
        msgs.Add("Tip 3: Verify that my site host is created and working. See: http://www.harbar.net/articles/sp2010ups.aspx for instructions.");
        msgs.Add("Tip 4: For other causes of UserProfileApplicationNotAvailableException see: https://blogs.msdn.microsoft.com/sambetts/2016/01/26/user-profile-application-unavailable-with-userprofileapplicationnotavailableexception/");
        foreach (string msg in msgs) {
          KrakenLoggingService.Default.Write(msg, TraceSeverity.Monitorable, EventSeverity.Warning, LoggingCategories.KrakenProfiles);
        }
      } catch (Exception ex) {
        KrakenLoggingService.Default.Write("Unexpected error creating UserProfileManager.", TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenProfiles);
        KrakenLoggingService.Default.Write(ex, LoggingCategories.KrakenProfiles);
      } finally {
        KrakenLoggingService.Default.WriteStack(typeof(UserProfileUtilities), MethodBase.GetCurrentMethod().Name, true);
      }
      return upm;
    }

    private static bool isHoldHttpContext = false;
    private static HttpContext holdHttpContext = null;
    public static HttpContext CreateHttpContext(SPSite site, bool setCurrent) {
      HttpContext context = new HttpContext(new HttpRequest(string.Empty, site.Url, string.Empty), new HttpResponse(new StringWriter()));
      context.User = new GenericPrincipal(WindowsIdentity.GetCurrent(), new string[0]);
      if (setCurrent) {
        holdHttpContext = HttpContext.Current;
        HttpContext.Current = context;
        isHoldHttpContext = true;
      }
      return context;
    }
    public static void RestoreHttpContext() {
      if (isHoldHttpContext) {
        HttpContext.Current = holdHttpContext;
        holdHttpContext = null;
        isHoldHttpContext = false;
      }
    }

    internal static ProfileBase[] _ResolveUserProfiles(string searchText) {
      KrakenLoggingService.Default.Write(string.Format("Entering '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      ProfileBase[] results = null;
      try {
        SPServiceContext sc = SPServiceContext.Current;
        if (sc == null) {
          KrakenLoggingService.Default.Write(string.Format("Could not create SPServiceContext for search text '{0}'.", searchText), TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenProfiles);
          return null;
        }
        UserProfileManager upm = new UserProfileManager(sc); // TODO do we need to dispose upm or profile?
        if (upm == null) {
          KrakenLoggingService.Default.Write(string.Format("Could not create UserProfileManager from SPServiceContext for search text '{0}'.", searchText), TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenProfiles);
          return null;
        }
        ProfileBase[] profiles = upm.ResolveProfile(searchText);
        if (profiles == null) {
          KrakenLoggingService.Default.Write(string.Format("Could not get profile with search text '{0}'.", searchText), TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenProfiles);
          return null;
        }
        results = profiles;
        KrakenLoggingService.Default.Write(string.Format("Successfully resolved user profiles with search text '{0}'.", searchText), TraceSeverity.Medium, EventSeverity.Information, LoggingCategories.KrakenProfiles);
      } catch (Exception ex) {
        KrakenLoggingService.Default.Write(string.Format("Could not get profile with search text '{0}'.", searchText), TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenProfiles);
        KrakenLoggingService.Default.Write(ex, LoggingCategories.KrakenProfiles);
      }
      KrakenLoggingService.Default.Write(string.Format("Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      return results;
    }

    private static SimpleClaimsDecoder decoder = new SimpleClaimsDecoder(true);

    public static List<string> allProviderIDs;
    public static List<string> GetAllProviderIDs(bool useCachedValue) {
      if (allProviderIDs != null && useCachedValue)
        return allProviderIDs;
      KrakenLoggingService.Default.Write(string.Format("Entering '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      allProviderIDs = new List<string>();
      SPSecurity.RunWithElevatedPrivileges(delegate() {
        SPServiceContext sc = SPServiceContext.Current;
        UserProfileManager upm = null;
        try {
          // TODO do we need to dispose upm?
          upm = new UserProfileManager(sc);
        } catch (Exception ex) {
          KrakenLoggingService.Default.Write(ex);
          return;
        }
        foreach (UserProfile profile in upm) {
          try {
            // TODO Is there a faster way to get all these providers? Or can we at least cahce the results?
            // I bet it would be a lot faster to ask SP what claim providers are configured...
            string provider = profile.TryGetClaimsProvider();
            if (string.IsNullOrEmpty(provider)) {
              string userName = profile.TryGetAccountName();
              EncodedClaimInfo info = decoder.DecodeFully(userName, true);
              // TODO can we proper-case the names??
              provider = info.ProviderName;
            }
            if (string.IsNullOrEmpty(provider))
              provider = "No Provider";
            if (!allProviderIDs.Contains(provider))
              allProviderIDs.Add(provider);
          } catch (Exception ex) {
            KrakenLoggingService.Default.Write(ex);
          }
        }
      });
      KrakenLoggingService.Default.Write(string.Format("Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenProfiles);
      return allProviderIDs;
    }

    private static List<string> allPossibleLoginProvders;
    /// <summary>
    /// Gets a portmonteau of all possible login provider names
    /// which are generated both from configured STS and user profiles
    /// </summary>
    /// <param name="useCachedValue"></param>
    /// <returns></returns>
    public static List<string> GetAllPossibleLoginProvders(bool useCachedValue) {
      if (allPossibleLoginProvders == null) {
        List<string> lowerClaimProviders = UserProfileUtilities.GetAllProviderIDs(useCachedValue);
        allPossibleLoginProvders = SPSecurityTokenServiceManager.Local.GetSTSNames(useCachedValue);
        foreach (string lowerName in lowerClaimProviders) {
          if (!allPossibleLoginProvders.Contains(lowerName, StringComparer.InvariantCultureIgnoreCase))
            allPossibleLoginProvders.Add(lowerName);
        }
      }
      return allPossibleLoginProvders;
    }

    /// <summary>
    /// Compares a lowercase provider name like those embedded in claims
    /// against the list of known proper-case names, and returns a match
    /// if one exists. Returns the original name param if no match is found.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string ProperCaseLoginProviderName(string name) {
      List<string> providers = GetAllPossibleLoginProvders(true);
      foreach (string provider in providers) {
        if (name.Equals(provider, StringComparison.InvariantCultureIgnoreCase))
          return provider; // return a proper cased name
      }
      // fallback on name that was given
      return name;
    }

  }

}
