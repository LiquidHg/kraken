using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;

using Kraken.Core.Security;

namespace Kraken.Security.ActiveDirectory {

  public static class ADFunctions {

    /// <summary>
    /// This method is exported from active directory dll to validate the user network credential
    /// </summary>
    /// <param name="lpszUsername">network username</param>
    /// <param name="lpszDomain">network domain</param>
    /// <param name="lpszPassword">network password</param>
    /// <param name="logonType">network login type</param>
    /// <param name="logonProvider">network login provider</param>
    /// <param name="token">inptr token this can be blank</param>
    /// <returns>returns true if login is valid else false</returns>
    [DllImport("ADVAPI32.dll", EntryPoint = "LogonUserW", SetLastError = true, CharSet = CharSet.Auto)]
    private unsafe static extern bool LogonUser(
                                string lpszUsername,
                                string lpszDomain,
                                char* lpszPassword, // ref IntPtr 
                                int logonType,
                                int logonProvider,
                                ref IntPtr token);

    /// <summary>
    /// Takes a secure string and descrypts it, then passes the login info.
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="domain"></param>
    /// <param name="password"></param>
    /// <param name="logonType"></param>
    /// <param name="logonProvider"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static bool SafeLogonUser(
                                string userName,
                                string domain,
                                SecureString password,
                                int logonType,
                                int logonProvider,
                                ref IntPtr token) {
      // Any API function that will take a SecureString would be prefferable.
      // Unfortunately, the Windows API is full of lots of instances where we have to pass strings!
      // at least we are trying not to contribute to the issue any further ourselves
      // Normally, imported function marshall as strings, but we're passing character
      // pointer instead to ensure that clone copies of the immutable string are not
      // created by our calls. Some other examples of how to use SecureStringMarshaller
      // are as follows:
      // 
      // string password = sm.ToString();
      // sm.MarkStringForDisposal(password); 
      // byte[] pwdArray = sm.EncodeToByteArray(Encoding.Default);
      //
      bool loginValid = false;
      /// It's marked unsafe, but we promise that this method doesn't do anything
      /// weird to the char* that it uses to send the password to the Win32 API.
      unsafe {
        using (SecureStringMarshaller sm = new SecureStringMarshaller(password)) {
          loginValid = LogonUser(userName, domain, sm.DecryptGetBuffer(), logonType, logonProvider, ref token);
        }
      }
      return loginValid;
    }

  } // class

} // namespace
