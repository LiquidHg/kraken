//-----------------------------------------------------------------------
// <copyright file="ActiveDirectoryUserValidation.cs" company="Liquid Mercury Solutions">
//     Copyright (c) Liquid Mercury Solutions. All rights reserved.
// </copyright>
// <summary>
// Authenicates a user against Windows Active Directory
// </summary>
//-----------------------------------------------------------------------
namespace Kraken.Security.ActiveDirectory {

  using System;
  using System.Collections.Generic;
  using System.Configuration;
  using System.DirectoryServices;
  using System.Linq;
  using System.Text;
  using System.Security;
  using System.Security.Principal;
  using System.Reflection;

  using Kraken.Core.Security;
  using Kraken.Security.ActiveDirectory;

#if !DOTNET_V35
  using System.Runtime.Caching;
#endif

  // Why were these commented out????
  // Was it because Kraken.Logging is in the full trust core??
  using log4net;

  // TODO library class - kraken probably
  public static class LdapExtensions {

    private static bool VerboseLogs = false; // make true to get more detail

    private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public static T GetValue<T>(this DirectoryEntry entry, string propertyName) {
      PropertyValueCollection properties = entry.Properties[propertyName];
      if (properties == null || properties.Count == 0)
        return default(T);
      return GetValue<T>(properties);
    }

    public static T GetValue<T>(this PropertyValueCollection properties) {
      if (typeof(T) == typeof(DateTime)) {
        long value;
        try {
          value = (long)(properties[0]);
        } catch {
          // COM objects that represent date time such as pwdLastSet
          value = ConvertADSLargeIntegerToInt64(properties[0]);
        }
        object dt = DateTime.FromFileTimeUtc(value);
        return (T)dt;
      } else if (typeof(T) == typeof(TimeSpan)) {
        long value;
        try {
          value = (long)(properties[0]);
        } catch {
          // COM objects that represent date time such as pwdLastSet
          value = ConvertADSLargeIntegerToInt64(properties[0]);
        }
        object ts = TimeSpan.FromTicks(value);
        return (T)ts;
      } else {
        object value = null;
        // HACK always gets the last value rather than the first one
        foreach (object tmpValue in properties) {
          value = tmpValue;
        }
        return (T)value;
      }
    }

    public static Int64 ConvertADSLargeIntegerToInt64(object adsLargeInteger) {
      var highPart = (Int32)adsLargeInteger.GetType().InvokeMember("HighPart", System.Reflection.BindingFlags.GetProperty, null, adsLargeInteger, null);
      var lowPart = (Int32)adsLargeInteger.GetType().InvokeMember("LowPart", System.Reflection.BindingFlags.GetProperty, null, adsLargeInteger, null);
      return highPart * ((Int64)UInt32.MaxValue + 1) + lowPart;
    }

    // TODO make me a Kraken library function
    public static bool HasParent(this DirectoryEntry entry) {
      try {
        return (entry.Parent != null);
      } catch {
        return false;
      }
    }

    // TODO cache this in memory - its too much to go back to LDAP over and over!
    public static string GetFQADDomainName(this DirectoryEntry entry) {
      if (entry == null || entry.Parent == null) return string.Empty;
      // move up the stack until we hit the domain dns
      while (entry.HasParent() && entry.GetValue<string>("objectClass") != "domainDNS") {
        entry = entry.Parent;
      }
      string dn = entry.GetValue<string>("distinguishedName");
      string[] parts = dn.Replace("DC=", string.Empty).Split(new char[] { ',' });
      return string.Join(".", parts);
    }

    public static string GetNetbiosDomainName(this DirectoryEntry entry, string ldapServerDNSName) {
      if (VerboseLogs)
        Log.Entering(MethodBase.GetCurrentMethod());
      string loginName = entry.GetWindowsLoginName(ldapServerDNSName);
      if (VerboseLogs)
        Log.DebugFormat("loginName = '{0}'", loginName);
      string[] parts = loginName.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
      if (parts != null && parts.Length == 2) {
        if (VerboseLogs)
          Log.Leaving(MethodBase.GetCurrentMethod(), string.Format("parts[0] = '{0}'", parts[0]));
        return parts[0];
      }
      if (VerboseLogs)
        Log.Leaving(MethodBase.GetCurrentMethod(), "Returning empty handed.");
      return string.Empty;
    }

    // TODO until we find a better way to make this work, allow the caller to specify which method(s) should be used and in what order.
    public static string GetWindowsLoginName(this DirectoryEntry entry, string ldapServerDNSName) {
      if (ldapServerDNSName.StartsWith("LDAP://"))
        ldapServerDNSName = ldapServerDNSName.Substring(7);
      if (VerboseLogs)
        Log.Entering(MethodBase.GetCurrentMethod());
      string loginName = string.Empty;
      // This method takes longer, fails in certain cases
      // and definitely always assigns the currently mapped domain
      // for this LDAP directory, even if the user comes from another
      // domain. Therefore use with care.
      if (string.IsNullOrEmpty(loginName)) {
        if (VerboseLogs)
          Log.Debug("Couldn't convert SID to windowsloginname; falling back on FQDN and LDAP Configuration method.");
        string fqdn = entry.GetFQADDomainName();
        if (VerboseLogs)
          Log.DebugFormat("fqdn = '{0}'", fqdn);
        // get qualified NetBIOS style login name
        string domain = LdapExtensions.GetNetbiosDomainName(fqdn, ldapServerDNSName);
        if (VerboseLogs)
          Log.DebugFormat("NT domain = '{0}'", domain);
        if (!string.IsNullOrEmpty(domain)) {
          string samAccountName = entry.GetValue<string>("samaccountname");
          if (VerboseLogs)
            Log.DebugFormat("samAccountName = '{0}'", samAccountName);
          loginName = domain + "\\" + samAccountName;
        }
      }
      // This method is more elegant and faster, but has the shortcoming
      // that it will return the current computer's domain instead of the 
      // domain associated with the user SID. Still working on a fix, but
      // this is the best option in a single domain enviroment.
      if (string.IsNullOrEmpty(loginName)) {
        if (VerboseLogs)
          Log.Debug("Getting SID...");
        byte[] sidData = entry.GetValue<byte[]>("objectsid");
        if (sidData != null && sidData.Length > 0) {
          SecurityIdentifier sid = new SecurityIdentifier(sidData, 0);
          if (VerboseLogs)
            Log.Debug("Tranlating SID to NTAccount");
          NTAccount nt = (NTAccount)sid.Translate(typeof(NTAccount));
          if (VerboseLogs)
            Log.Debug("Getting account login name");
          loginName = nt.Value;
        } else {
          if (VerboseLogs)
            Log.Debug("Couldn't read objectsid as byte[]");
        }
      }
      if (VerboseLogs)
        Log.DebugFormat("loginName = '{0}'", loginName);
      if (VerboseLogs)
        Log.Leaving(MethodBase.GetCurrentMethod());
      return loginName;
    }

    // TODO cache this in memory - its too much to go back to LDAP over and over!
    public static string GetNetbiosDomainName(string fqADDomainName, string ldapServerDNSName) {
      if (ldapServerDNSName.StartsWith("LDAP://"))
        ldapServerDNSName = ldapServerDNSName.Substring(7);
      if (VerboseLogs)
        Log.Entering(MethodBase.GetCurrentMethod());
      // we found that calls to the sub-domain controller were failing when we use fqADDomainName instead of ldapServerDNSName; this should be fixed if possible to reduce number of input params
      string ldapPath = "LDAP://CN=Partitions,CN=Configuration,DC=" + ldapServerDNSName.Replace(".", ",DC=");
      if (VerboseLogs)
        Log.DebugFormat("ldapPath={0}", ldapPath);
      //ldapPath = ldapPath.Insert(7, "CN=Partitions,CN=Configuration,DC=");
      string netBiosName = string.Empty;
      // turn fully qualified domain into partition path
      using (DirectoryEntry root = new DirectoryEntry(ldapPath)) {
        using (DirectorySearcher searcher = new DirectorySearcher(root)) {
          searcher.ReferralChasing = ReferralChasingOption.All;
          searcher.Filter = string.Format("(|(msDS-DnsRootAlias={0})(dnsRoot={0}))", fqADDomainName);
          if (VerboseLogs)
            Log.DebugFormat("searcher.Filter={0}", searcher.Filter);
          searcher.PropertiesToLoad.Add("nETBIOSName");
          try {
            SearchResult result = searcher.FindOne();
            if (result == null) {
              Exception ex = new NotSupportedException(string.Format("Attempt to get the NetBIOS (Windows NT) Domain Name from Active Directory has failed to produce any results for the specific FQDN '{0}'. Please check configuration and AD security and try again.", fqADDomainName));
              Log.Error("ERROR NO NT DOMAIN", ex);
            } else {
              if (VerboseLogs)
                Log.Debug("Found a result.");
              ResultPropertyValueCollection values = result.Properties["nETBIOSName"];
              string keys = string.Empty;
              if (values != null && values.Count > 0) {
                netBiosName = values[0].ToString();
              } else {
                Log.Error("Unexpected result: result.Properties['nETBIOSName'] is null or does not exist!");
                foreach (string key in result.Properties.PropertyNames) {
                  keys += "," + keys;
                }
                if (!string.IsNullOrEmpty(keys))
                  keys = keys.Substring(1);
                if (VerboseLogs)
                  Log.DebugFormat("result.Properties.PropertyNames: {0}", keys);
                if (VerboseLogs)
                  Log.Debug("Trying direct read of LDAP entry instead.");
                DirectoryEntry entry = result.GetDirectoryEntry();
                netBiosName = entry.GetValue<string>("nETBIOSName");
              }
              if (VerboseLogs)
                Log.DebugFormat("netBiosName = '{0}'", netBiosName);
            }
          } catch (Exception ex) {
            Log.Error("ERR NT DOMAIN", ex);
          } finally {
            searcher.Dispose();
            root.Dispose();
            /*
            if (result != null) {
              result.Dispose();
              result = null;
            }
             */
          } // finally
        }
      }
      if (VerboseLogs)
        Log.Leaving(MethodBase.GetCurrentMethod());
      return netBiosName;
    }

    // The cache method implemented here only works in .NET 4.0
    // TODO implement a cache method for legacy versions
    public static IList<string> GetAllDomainNames(string ldapRootPath) {
      if (string.IsNullOrEmpty(ldapRootPath))
        throw new ArgumentNullException("ldapRootPath");

#if !DOTNET_V35
      const string cashKey = "domainNames";
      ObjectCache cache = MemoryCache.Default;
      List<string> domainNames = cache[cashKey] as List<string>;
      CacheItemPolicy policy = new CacheItemPolicy();
      policy.AbsoluteExpiration = DateTimeOffset.Now.AddHours(1.0);
#else
      List<string> domainNames = null;
#endif
      if (domainNames == null) {
        domainNames = new List<string>();

        using (DirectoryEntry root = new DirectoryEntry(ldapRootPath.Insert("LDAP://".Length, "CN=Partitions,CN=Configuration,DC=").Replace(".", ",DC=")))
        using (DirectorySearcher searcher = new DirectorySearcher(root)) {
          searcher.Filter = "nETBIOSName=*";
          searcher.PropertiesToLoad.Add("cn");

          using (SearchResultCollection results = searcher.FindAll()) {
            foreach (SearchResult result in results) {
              domainNames.Add(result.Properties["cn"][0].ToString());
            }
          }
        }

#if !DOTNET_V35
        cache.Set(cashKey, domainNames, policy);
#endif //!DOTNET_V35
      }

      return domainNames;
    }

  }
}
