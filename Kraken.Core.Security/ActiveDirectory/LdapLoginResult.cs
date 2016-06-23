using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.Security.ActiveDirectory {

  public enum LdapLoginResult {
    None = 0,
    UnknownError = 1,
    LoginOK = 0x1,
    ServerUnavailable = 81,
    LocalError = 82,
    UserNotFound = 0x525,
    InvalidCredentials = 0x52e,
    LoginRestrictedTime = 0x530,
    LoginRestrictedWorkstation = 0x531,
    PasswordExpired = 0x532,
    AccountDisabled = 0x533,
    AccountExpired = 0x701,
    PasswordResetRequired = 0x773,
    AccountLocked = 0x775
  }

}
