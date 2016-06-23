namespace Kraken.SharePoint.UserProfiles {

  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Security;
  using System.Security.Permissions;
  using System.Text;

  using System.Web;
  using System.Web.UI;
  using System.Web.UI.WebControls;

  using Microsoft.SharePoint;
  //using Microsoft.SharePoint.Portal;
  using Microsoft.SharePoint.Security;
  using Microsoft.Office.Server;
  using Microsoft.Office.Server.UserProfiles;

  using Kraken.SharePoint.Logging;

  /// <summary>
  /// A useful developer code pattern that for provisioning user profile properties.
  /// We took the example from http://www.mylifeinaminute.com/2008/12/16/creating-custom-profile-properties-through-code-c/
  /// and made an abstract class so you can just change the important parts.
  /// </summary>
  [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true), SharePointPermission(SecurityAction.Demand, ObjectModel = true)]
  public abstract class ProfilePropertyCreator {

    /// <summary>
    /// This property caches profile property names
    /// so they can be checked without too many calls
    /// to the privileged UPM / SPM / UPCM objects.
    /// </summary>
    protected List<string> propertyNames = null;

    protected abstract List<string> AllowedPropertyNames {
      get;
    }

    protected KrakenLoggingService log = new KrakenLoggingService();

    protected SPSite targetSite;

    public ProfilePropertyCreator(SPSite site) {
      targetSite = site;
    }

    public bool CheckUserPortalRight() {
      bool flag = true;
      try {
        UserProfile userInfo = null;
        //userInfo = GetUserByAccountName("the account name of login user");
        object obj = userInfo["FirstName"].Value;
        userInfo["FirstName"].Value = obj;
      } catch (Exception ex) {
        string targetSite = ex.TargetSite.ToString();
        if (targetSite.Contains("checkupdatepermissions")) {
          flag = false;
        }
      }
      return flag;
    }


    /// <summary>
    /// Legacy method for the SP2007 method of reading properties
    /// </summary>
    /// <param name="properties"></param>
    private void PopulatePropertyNames(PropertyCollection properties) {
      propertyNames = new List<string>();
      foreach (Property property in properties) {
        propertyNames.Add(property.Name);
      }
    }
    /// <summary>
    /// Copy the names of properties into a local string collection.
    /// </summary>
    /// <param name="properties"></param>
    private void PopulatePropertyNames(ArrayList properties) {
      propertyNames = new List<string>();
      foreach (ProfileSubtypeProperty property in properties) {
        propertyNames.Add(property.Name);
      }
    }

    protected bool PropertyExists(string propertyName) {
      if (propertyNames == null)
        throw new Exception("Can not call this method before PopulatePropertyNames(...)");
      return (propertyNames.Contains(propertyName));
    }

    #region Create Profile Properties

    public void EnsureUserProfileProperties() {
      Func<SPSite, object[], object> saveAction = delegate(SPSite elevatedSite, object[] args) {
        // set up the required profile objects
        ProfileType pType = ProfileType.User;
        ArrayList properties = GetProfileProperties(elevatedSite, pType);
        PopulatePropertyNames(properties);
        // alter the profile config
        CreateProfileProperties(elevatedSite, pType);
        return null;
      };
      ElevatedAction(saveAction, null);
    }

    /// <summary>
    /// Override this method to provision your user profile properties.
    /// </summary>
    /// <param name="site">The site collection to use as the context for the profile service app</param>
    /// <param name="pType">The property type: user, group, or organization</param>
    /// <example>
    /// Developers should implement this class with one or more properties
    /// like the following:
    /// <code>
    /// public override void CreateProfileProperties(SPSite elevatedSite) {
    ///   if (!PropertyExists("Sample1")) {
    ///     bool success = CreateProfileProperty(elevatedSite, ProfileType.User, "Sample1", "Sample 1", PropertyDataType.StringSingleValue, 1000, PrivacyPolicy.Disabled, Privacy.NotSet);
    ///   }
    /// }
    /// </code>
    /// </example>
    protected abstract void CreateProfileProperties(SPSite site, ProfileType pType);

    // TODO implement as a TDD unit test
    public bool SampleCreateProperties(SPSite site, ProfileType pType) {
      if (PropertyExists("Sample1"))
        return true;
      bool success = CreateProfileProperty(site, pType, "Sample1", "Sample 1", PropertyDataType.StringSingleValue, 1000, PrivacyPolicy.Disabled, Privacy.NotSet);
      return success;
    }

    protected bool CreateProfileProperty(SPSite site, ProfileType pType, string name, string displayName, string profileDataType, int size, PrivacyPolicy privacyPolicy, Privacy defaultPrivacy) {
      SPServiceContext context = SPServiceContext.GetContext(site);
      UserProfileConfigManager profileConfigManager = new UserProfileConfigManager(context);
      try {
        ProfilePropertyManager ppm = profileConfigManager.ProfilePropertyManager;

        // create core property
        CorePropertyManager cpm = ppm.GetCoreProperties();
        CoreProperty cp = cpm.Create(false);
        cp.Name = name;
        cp.DisplayName = displayName;
        cp.Type = profileDataType;
        cp.Length = size;
        cpm.Add(cp);

        // create profile type property
        ProfileTypePropertyManager ptpm = ppm.GetProfileTypeProperties(pType);
        ProfileTypeProperty ptp = ptpm.Create(cp);
        ptpm.Add(ptp);

        // create profile subtype property
        ProfileSubtypeManager psm = ProfileSubtypeManager.Get(context);
        ProfileSubtype ps = psm.GetProfileSubtype(ProfileSubtypeManager.GetDefaultProfileName(pType));
        ProfileSubtypePropertyManager pspm = ps.Properties;
        ProfileSubtypeProperty psp = pspm.Create(ptp);

        psp.PrivacyPolicy = privacyPolicy; // PrivacyPolicy.OptIn;
        psp.DefaultPrivacy = defaultPrivacy; // Privacy.Organization;
        pspm.Add(psp);

        return true;
      } catch (DuplicateEntryException e1) {
        log.Write(e1);
        return false;
      } catch (System.Exception e2) {
        log.Write(e2);
        throw;
      }
    }

    #endregion

    protected UserProfile GetUserProfile() {
      return GetUserProfile(this.targetSite, true);
    }
    protected virtual UserProfile GetUserProfile(SPSite site, bool createProfile) {
      UserProfileManager profileManager = GetUserProfileManager(site);
      UserProfile profile = profileManager.GetUserProfile(createProfile);
      return profile;
    }
    protected virtual UserProfile GetUserProfile(SPSite site, Guid profileId) {
      UserProfileManager profileManager = GetUserProfileManager(site);
      UserProfile profile = profileManager.GetUserProfile(profileId);
      return profile;
    }

    protected ProfileSubtypePropertyManager GetProfileSubtypePropertyManager() {
      return GetProfileSubtypePropertyManager(this.targetSite, ProfileType.User);
    }
    protected virtual ProfileSubtypePropertyManager GetProfileSubtypePropertyManager(SPSite site, ProfileType pType) {
      SPServiceContext context = SPServiceContext.GetContext(site);
      ProfileSubtypeManager psm = ProfileSubtypeManager.Get(context);
      ProfileSubtype ps = psm.GetProfileSubtype(ProfileSubtypeManager.GetDefaultProfileName(pType));
      ProfileSubtypePropertyManager pspm = ps.Properties;
      return pspm;
    }

    protected UserProfileManager GetUserProfileManager() {
      return GetUserProfileManager(this.targetSite);
    }
    protected virtual UserProfileManager GetUserProfileManager(SPSite site) {
      SPServiceContext context = SPServiceContext.GetContext(site); // ServerContext.GetContext(site);
      UserProfileManager profileManager = new UserProfileManager(context);
      return profileManager;
    }

    // ArrayList of ProfileSubtypeProperty
    protected ArrayList GetProfileProperties() {
      return GetProfileProperties(targetSite, ProfileType.User);
    }
    protected ArrayList GetProfileProperties(SPSite site, ProfileType pType) { // PropertyCollection
      ArrayList properties = null; //PropertyCollection
      try {
        // assert our rights
        PermissionSet ps = new PermissionSet(PermissionState.Unrestricted);
        ps.Assert();
        // do our stuff
        ProfileSubtypePropertyManager spm = GetProfileSubtypePropertyManager(site, pType);
        properties = spm.PropertiesWithSection; //upm.PropertiesWithSection
      } catch (Exception ex) {
        log.Write("Exception during GetProfileProperties().");
        log.Write(ex);
        throw ex;
      } finally {
        CodeAccessPermission.RevertAssert();
      }
      return properties;
    }

    public static PropertyDataType GetPropertyType(UserProfileConfigManager profileConfigManager, string name) {
      //sample to get a property type "URL"
      PropertyDataTypeCollection pdtc = profileConfigManager.GetPropertyDataTypes();
      IEnumerator enumType = pdtc.GetEnumerator();
      while (enumType.MoveNext()) {
        PropertyDataType ptype = (PropertyDataType)enumType.Current;
        if (string.Compare(name, ptype.Name, true) == 0)
          return ptype;
      }
      return null;
    }

    #region Events for Saving Properties

    protected event ProfilePropertyEventHandler SaveProperties;

    public delegate void ProfilePropertyEventHandler(object sender, ProfilePropertyEventArgs e);

    public class ProfilePropertyEventArgs : EventArgs {

      public ProfilePropertyEventArgs(Dictionary<string, object> properties) {
        this.properties = properties;
      }

      Dictionary<string, object> properties;
      public Dictionary<string, object> Properties {
        get { return properties; }
      }

    }

    public void DefaultSaveSettingsEvent(object sender, ProfilePropertyEventArgs e) {
      if (AllowedPropertyNames == null)
        return;
      UserProfile currentUser = GetUserProfile();
      Guid currentProfileID = currentUser.ID;
      Func<SPSite, object[], object> saveAction = delegate(SPSite elevatedSite, object[] args) {
        UserProfile up = GetUserProfile(elevatedSite, currentProfileID);
        foreach (string propertyName in e.Properties.Keys) {
          object propertyValue = e.Properties[propertyName];
          if (!AllowedPropertyNames.Contains(propertyName))
            throw new Exception(string.Format("Attempt to save unsanctioned property name '{0}'. Property must be added to AllowedPropertyNames before it can be saved to the User Profile.", propertyName));
          UserProfileValueCollection p = up[propertyName];
          p.Value = null;
          p.Clear();
          // there is a weird thing in Value set where it will loop through a
          // collection. Unfortunately that's not what we want for binary arrays.
          if (p.ProfileSubtypeProperty.CoreProperty.Type == PropertyDataType.Binary) // p.Property.Type
            p.Add((byte[])propertyValue);
          else
            p.Value = propertyValue;
          // TODO implement other types here
        } // foreach
        up.Commit();
        return null;
      };
      ElevatedAction(saveAction, null);
    }

    protected void SaveProperties_Elevated(ProfilePropertyEventArgs args) {
      ProfilePropertyEventHandler handler = new ProfilePropertyEventHandler(this.DefaultSaveSettingsEvent);
      SaveProperties -= handler;
      SaveProperties += handler;
      OnSaveProperties(args);
    }

    protected virtual void OnSaveProperties(ProfilePropertyEventArgs args) {
      EnsureUserProfileProperties();
      if (SaveProperties != null) {
        SaveProperties(this, args);
      }
    }

    #endregion

    #region Support Elevation - Safely!!

    protected internal object ElevatedAction(Func<SPSite, object[], object> elevatedAction, object[] args) {
      object result = null;
      HttpContext savedContext = HttpContext.Current;
      bool allowRootUpdates = false, allowSiteUpdates = false;
      try {
        SPSecurity.RunWithElevatedPrivileges(delegate() {
          using (SPSite elevatedSite = new SPSite(targetSite.ID)) {
            bool contextAllowUnsafe = SPContext.Current.Web.AllowUnsafeUpdates;
            // set up access to the site so we can write to it
            HttpContext.Current = null;
            allowSiteUpdates = elevatedSite.AllowUnsafeUpdates;
            elevatedSite.AllowUnsafeUpdates = true;
            allowRootUpdates = elevatedSite.RootWeb.AllowUnsafeUpdates;
            elevatedSite.RootWeb.AllowUnsafeUpdates = true;
            //SPContext.Current.Web.AllowUnsafeUpdates = true;

            result = elevatedAction(elevatedSite, args);

            //elevatedSite.AllowUnsafeUpdates = allowSiteUpdates;
            //elevatedSite.RootWeb.AllowUnsafeUpdates = allowRootUpdates;
          } // using
        }); // delegate
      } catch (Exception ex) {
        log.Write(ex);
        throw;
      } finally {
        HttpContext.Current = savedContext;
        //SPContext.Current.Web.AllowUnsafeUpdates = contextAllowUnsafe;
      }
      return result;
    }

    #endregion

  } // class

} // namespace
