using Kraken.Core.Security;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.Protocols;
using System.DirectoryServices.ActiveDirectory;
using SDP = System.DirectoryServices.Protocols;
//using ActiveDs;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.DirectoryServices.AccountManagement;

namespace Kraken.Security.ActiveDirectory {

  public static class LdapFunctions {

    /*
    public static bool LogonUser2(
      string ldapPath,
      string userName, // includes domain or UPN suffix
      //string domain,
      SecureString password
    ) {
      // TODO provide user and password to make the connection to ldap
      bool isValid = false;
      PrincipalContext pc = new PrincipalContext(ContextType.Domain, ldapPath);
      using (SecureStringMarshaller sm = new SecureStringMarshaller(password)) {
        if (!sm.IsDecrypted)
          sm.Decrypt();
        isValid = pc.ValidateCredentials(userName, sm.ToString(), ContextOptions.Negotiate);
      }
      return isValid;
    }
     */

    // TODO support using a privileged account to make the connection to LDAP server in case it is locked down
    public static LdapLoginResult LogonUser(
      string ldapPath,
      string userName, // includes domain or UPN suffix
      //string domain,
      SecureString password
    ) {
      if (string.IsNullOrEmpty(ldapPath))
        throw new ArgumentNullException("ldapPath");
      if (string.IsNullOrEmpty(userName))
        throw new ArgumentNullException("userName");
      if (password == null)
        throw new ArgumentNullException("password");

      //Uri ldapUri = new Uri(ldapServer);
      string ldapServer = ldapPath; bool useSsl = false;
      if (ldapServer.StartsWith("ldap://", StringComparison.InvariantCultureIgnoreCase))
        ldapServer = ldapServer.Substring(7);
      if (ldapServer.StartsWith("ldaps://", StringComparison.InvariantCultureIgnoreCase)) {
        ldapServer = ldapServer.Substring(8);
        useSsl = true;
      }
      // TODO extract the port # from the URL or use defaults

      try {
        /*
        DirectoryEntry entry = GetUser(ldapPath, userName);
        if (entry == null)
          return LdapLoginResult.UserNotFound;
        string dn = entry.GetValue<string>("distinguishedName");
         */

        //using (SecureStringMarshaller sm = new SecureStringMarshaller(password)) {
        //if (!sm.IsDecrypted)
        //  sm.Decrypt();
        //}
        // HACK hard coded ports only work in default AD configuration
        LdapDirectoryIdentifier ldp = new LdapDirectoryIdentifier(ldapServer, useSsl ? 686 : 389);
        LdapConnection connection = new LdapConnection(ldp);
#if DOTNET_V35
        NetworkCredential credential = null;
        using (SecureStringMarshaller sm = new SecureStringMarshaller(password)) {
          if (!sm.IsDecrypted)
            sm.Decrypt();
          credential = new NetworkCredential(userName, sm.ToString());
        }
#else
          NetworkCredential credential = new NetworkCredential(userName, password);
#endif
          connection.Credential = credential;
        // HACK while the password is still encrypted, it would be good to implement SSL
          connection.AuthType = AuthType.Basic; // the wrong authtype can cause false negatives because of problems on re-binding
          //connection.AuthType = AuthType.Negotiate;
          if (useSsl) {
            connection.SessionOptions.SecureSocketLayer = true;
            // HACK we will want to enable only in config options
            connection.SessionOptions.VerifyServerCertificate = new VerifyServerCertificateCallback((con, cer) => true);
            // HACK for cases where we need client validation on non-Basic auth
            connection.SessionOptions.QueryClientCertificate = new QueryClientCertificateCallback((con, cer) => null);
          }
          connection.Bind();
          //connection.SendRequest(new SearchRequest(dn, "(objectClass=*)", SDP.SearchScope.Subtree, null));
          return LdapLoginResult.LoginOK;
        //} // using
      } catch (LdapException lexc) {
        if (lexc.ErrorCode == 81)
          return LdapLoginResult.ServerUnavailable;
        if (lexc.ErrorCode == 82)
          return LdapLoginResult.LocalError;
        if (!string.IsNullOrEmpty(lexc.ServerErrorMessage)) {
          string[] errorParts = lexc.ServerErrorMessage.Split(new char[] { ',' });
          string errCode = errorParts[errorParts.Length - 2];
          errCode = errCode.Replace("data", string.Empty);
          uint errNum = 0;
          if (uint.TryParse(errCode, NumberStyles.HexNumber, null, out errNum)) {
            switch (errNum) {
              case 0x525: // user not found ​(1317)
                return LdapLoginResult.UserNotFound;
              case 0x52e: // invalid credentials ​(1326)
                return LdapLoginResult.InvalidCredentials;
              case 0x530: // not permitted to logon at this time​ (1328)
                return LdapLoginResult.LoginRestrictedTime;
              case 0x531: //​ not permitted to logon at this workstation​ (1329)
                return LdapLoginResult.LoginRestrictedWorkstation;
              case 0x532: //​ password expired ​(1330)
                return LdapLoginResult.PasswordExpired;
              case 0x533: // account disabled ​(1331) 
                return LdapLoginResult.AccountDisabled;
              case 0x701: // account expired ​(1793)
                return LdapLoginResult.AccountExpired;
              case 0x773: // user must reset password (1907)
                return LdapLoginResult.PasswordResetRequired;
              case 0x775: //​ user account locked (1909) */
                return LdapLoginResult.AccountLocked;
            }
          }
        }
        //lexc.ErrorCode;
        return LdapLoginResult.UnknownError;
      }
      /*
       * 81 ldap server unavailable
       * 49 supplied credential invalid
       */
    }

    public const string InvalidCredentialsMessage = "The provided username or password is invalid.";
    /// <summary>
    /// Returns a message that can be sent to the user, based on system security settings.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="msgObfuscation"></param>
    /// <returns></returns>
    public static string GetLoginResultMessage(LdapLoginResult result, MessageObfuscationLevel msgObfuscation) {
      switch (result) {
        case LdapLoginResult.AccountDisabled:
          return (msgObfuscation > MessageObfuscationLevel.None) ? InvalidCredentialsMessage : "Account is disabled.";
        case LdapLoginResult.AccountExpired:
          return (msgObfuscation > MessageObfuscationLevel.None) ? InvalidCredentialsMessage : "Account is expired.";
        case LdapLoginResult.AccountLocked:
          return (msgObfuscation >= MessageObfuscationLevel.Medium) ? InvalidCredentialsMessage : "Account has been locked due to excessive login attempts. Please try again later.";
        case LdapLoginResult.InvalidCredentials:
          return InvalidCredentialsMessage;
        case LdapLoginResult.LoginOK:
          return "Login successful.";
        case LdapLoginResult.LoginRestrictedTime:
          return (msgObfuscation >= MessageObfuscationLevel.Medium) ? InvalidCredentialsMessage : "Account is restricted and cannot login at during this time period. Please try again later.";
        case LdapLoginResult.LoginRestrictedWorkstation:
          return (msgObfuscation >= MessageObfuscationLevel.Medium) ? InvalidCredentialsMessage : "Account is restricted and cannot login on this machine.";
        case LdapLoginResult.None:
          return "No login result.";
        case LdapLoginResult.PasswordExpired:
          return (msgObfuscation >= MessageObfuscationLevel.Maximum) ? InvalidCredentialsMessage : "Your password has expired. Please update your password before attempting to login.";
        case LdapLoginResult.PasswordResetRequired:
          return (msgObfuscation >= MessageObfuscationLevel.Maximum) ? InvalidCredentialsMessage : "Your password requires a reset. Please update your password before attempting to login.";
        case LdapLoginResult.ServerUnavailable:
          return "The login server is unavailable. Please try again later. If the problem persists, please contact the system administrator.";
        case LdapLoginResult.UserNotFound:
          return (msgObfuscation > MessageObfuscationLevel.None) ? InvalidCredentialsMessage : "User does not exist.";
        default:
          return "Unrecognized result"; 
      }
    }

    // TODO implement the following encoding for ldap query values
    /*
     * ( 	\28 	  	
     * ) 	\29 	  	
     * & 	\26 	  	
     * | 	\7c 	  	
     * = 	\3d
     * > 	\3e 	  	
     * < 	\3c 	  	
     * ~ 	\7e 	  	
     * * 	\2a 	  	
     * / 	\2f
     * \ 	\5c
     */

    // Thanks to SelfADSI for the help in getting these worked out
    // http://www.selfadsi.org/ldap-filter.htm

    private const string LDAPQ_OBJCLASS_USER = "(&(objectClass=user)(!(objectClass=contact)))"; // All users excluding contacts
    private const string LDAPQ_SECURITY_GROUP = "(groupType:1.2.840.113556.1.4.803:=2147483648)"; // 'All security enabled groups
    // (groupType=2147483656) // All universal security groups
    // (groupType:1.2.840.113556.1.4.803:=8) // All universal groups
    // (objectClass=group) // All groups
    // (objectClass=contact) // All contacts

    public static string GetObjectTypeClause(LdapSearchOptions options) {
      // TODO write up a handy LDAP query string appending utility method
      // TODO support more possible options than just users and groups
      string objectTypeClause = string.Empty;
      if (0 < (options & LdapSearchOptions.IncludeUsers))
        objectTypeClause = LDAPQ_OBJCLASS_USER;
      if (0 < (options & LdapSearchOptions.IncludeGroups)) {
        if (string.IsNullOrEmpty(objectTypeClause))
          objectTypeClause = LDAPQ_SECURITY_GROUP;
        else
          objectTypeClause = string.Format("(|{0}{1})", objectTypeClause, LDAPQ_SECURITY_GROUP);
      }
      return objectTypeClause;
    }

    public const string NTDomainNotSpecified = "NA";

    /// <summary>
    /// This method will return the user information from active directory for producing claims
    /// </summary>
    /// <param name="userName">Network user login id</param>
    /// <returns>dictionary with key pair token name values</returns>
    public static DirectoryEntry GetUser(
      string ldapPath,
      string userName,
      LdapSearchOptions options
      = (LdapSearchOptions.IncludeUsers | LdapSearchOptions.EnableReferralChasing)
    ) {
      if (string.IsNullOrEmpty(ldapPath))
        throw new ArgumentNullException("ldapPath");
      if (string.IsNullOrEmpty(userName))
        throw new ArgumentNullException("userName");
      // TODO certainly if we have a mapping of domain prefixes and fq domains,
      // then we probably don't need ldapPath at this point
      //Log.Entering(MethodBase.GetCurrentMethod());
      if (string.IsNullOrEmpty(userName))
        throw new ArgumentNullException("userName");
      DirectoryEntry dirEntry = new DirectoryEntry(ldapPath);
      DirectorySearcher userSearch = new DirectorySearcher(dirEntry);
      // TODO I can think of a lot of legit cases where we want to connect first to the right LDAP server and turn referral chaing off, or otherwise find a way to get the correct NT account.
      if (0 < (options & LdapSearchOptions.EnableReferralChasing))
        userSearch.ReferralChasing = ReferralChasingOption.All;

      ADUserInfo userInfo = new ADUserInfo(userName);
      string objectTypeClause = GetObjectTypeClause(options);
      if (userInfo.Domain == NTDomainNotSpecified)
        userSearch.Filter = string.Format("(&{0}(userPrincipalName={1}))", objectTypeClause, userInfo.UserName);
      else
        userSearch.Filter = string.Format("(&{0}(sAMAccountName={1}))", objectTypeClause, userInfo.UserName);
      //Log.DebugFormat("Attempting search with '{0}'", userSearch.Filter);
      SearchResult userResult = userSearch.FindOne();
      if (userResult != null)
        return userResult.GetDirectoryEntry();
      return null;
    }

    // like http://houseofderek.blogspot.com/2008/07/password-expiration-email-utility.html
    // and http://stackoverflow.com/questions/3764327/active-directory-user-password-expiration-date-net-ou-group-policy
    // and http://stackoverflow.com/questions/14402344/active-directory-password-expiration-date
    public static int GetPasswordExpireDays(DirectoryEntry domainEntry, DirectoryEntry userEntry) {
      TimeSpan tsMaxPasswordAge = GetMaxPasswordAge(domainEntry);
      int maxPwdDays = tsMaxPasswordAge.Days;

      int userAccountControl = Convert.ToInt32(userEntry.Properties["userAccountcontrol"].Value);
      // Password never expires
      if ((userAccountControl & (int)ADUserAccountControlFlags.ADS_DONT_EXPIRE_PASSWORD) > 0)
        return -1;

      //ActiveDs.IADsUser native = (IADsUser)UserEntry.NativeObject;
      DateTime passwordLastChanged = new DateTime(9999, 1, 1);
      try {
        passwordLastChanged = userEntry.GetValue<DateTime>("pwdLastSet");
      } catch {
        return PasswordExpiryStatusCodes.NoCheckPwdLastSetFailure1;
      }
      // Password last changed date is not set
      if (passwordLastChanged.Year == 9999)
        return PasswordExpiryStatusCodes.NoCheckPwdLastSetFailure2;

      DateTime expireDate = passwordLastChanged.AddDays(maxPwdDays);
      TimeSpan ts = expireDate - DateTime.Now;
      int daysUntilPwdExpired = ts.Days;
      //int daysUntilPwdExpired = maxPwdDays - DateTime.Today.Subtract(passwordLastChanged.Days);
      return daysUntilPwdExpired;
    }

    public static TimeSpan GetMaxPasswordAge(DirectoryEntry domainEntry) {
      //using (Domain d = Domain.GetCurrentDomain())
      //using (DirectoryEntry domain = d.GetDirectoryEntry()) {
      string filter = "maxPwdAge=*"; // "(objectClass=*)"
      DirectorySearcher ds = new DirectorySearcher(
        domainEntry,
        filter,
        null,
        System.DirectoryServices.SearchScope.Base
      );
      SearchResult sr = ds.FindOne();
      TimeSpan maxPwdAge = TimeSpan.MinValue;
      if (sr.Properties.Contains("maxPwdAge")) {
        //maxPwdAge = sr.GetValue<DateTime>("pwdLastSet");
        long maxPwdAgeTicks = (long)sr.Properties["maxPwdAge"][0];
        maxPwdAge = TimeSpan.FromTicks(maxPwdAgeTicks);
      }
      return maxPwdAge.Duration();
    }

  } // class

  public class PasswordExpiryStatusCodes {
    public const int NotChecked = -1;
    public const int PasswordNeverExpires = -2;
    public const int NoCheckPwdLastSetFailure1 = -3;
    public const int NoCheckPwdLastSetFailure2 = -4;
    public const int NoCheckUserEntryFailure = -5;
    public const int NoCheckOtherReason = -6;
  }

}
