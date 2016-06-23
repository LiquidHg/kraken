using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.Security.ActiveDirectory {

  [Flags()]
  public enum LdapSearchOptions {
    None = 0x0,
    EnableReferralChasing = 0x01,
    IncludeUsers = 0x02,
    IncludeGroups = 0x04,
    IncludeUsersAndGroups = 0x06,
    IncludeContacts = 0x08,
    IncludeDistributionGroups = 0x10
  }

  public enum MessageObfuscationLevel {
    None = 0,
    Minimal = 1,
    Medium = 2,
    Maximum = 3
  }

  [Flags()]
  public enum ADUserAccountControlFlags {
    ADS_SCRIPT = 0x0001,
    ADS_ACCOUNTDISABLE = 0x0002,
    ADS_HOMEDIR_REQUIRED = 0x0008,
    ADS_LOCKOUT = 0x0010,
    ADS_PASSWD_NOTREQD = 0x0020,
    ADS_PASSWD_CANT_CHANGE = 0x0040,
    ADS_ENCRYPTED_TEXT_PWD_ALLOWED = 0x0080,
    ADS_TEMP_DUPLICATE_ACCOUNT = 0x0100,
    ADS_NORMAL_ACCOUNT = 0x0200,
    ADS_INTERDOMAIN_TRUST_ACCOUNT = 0x0800,
    ADS_WORKSTATION_TRUST_ACCOUNT = 0x1000,
    ADS_SERVER_TRUST_ACCOUNT = 0x2000,
    ADS_DONT_EXPIRE_PASSWORD = 0x10000,
    ADS_MNS_LOGON_ACCOUNT = 0x20000,
    ADS_SMARTCARD_REQUIRED = 0x40000,
    ADS_TRUSTED_FOR_DELEGATION = 0x80000,
    ADS_NOT_DELEGATED = 0x100000,
    ADS_USE_DES_KEY_ONLY = 0x200000,
    ADS_DONT_REQ_PREAUTH = 0x400000,
    ADS_PASSWORD_EXPIRED = 0x800000,
    ADS_TRUSTED_TO_AUTH_FOR_DELEGATION = 0x1000000
  } // class

}
