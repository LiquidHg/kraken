namespace Kraken.Security.Credentials { // PsUtils
  using Core.Security;
  using System;
  using System.Runtime.InteropServices;
  using System.Security;
  using System.Text;

  [CLSCompliant(false)]
  public class CredUtils {

    /// <summary>
    /// An easy to use interface for saving credentials
    /// to the Windows Cred Manager.
    /// </summary>
    /// <param name="target">Target web site URL or user name</param>
    /// <param name="userName">User name</param>
    /// <param name="secPassword">Password as secure string</param>
    /// <param name="callingApp">Name of the calling app, used in comment</param>
    /// <param name="comment">
    /// If left blank, will auto-generate a comment based on callingApp
    /// </param>
    /// <param name="credType">Defaults to GENERIC</param>
    /// <param name="persist">
    /// Defaults to LOCAL_MACHINE
    /// Caution: use at your own risk if you want to change this value
    /// </param>
    /// <param name="throwOnError">
    /// If true, errors will cause SecurityException
    /// If false, they are swept under the rug
    /// </param>
    /// <returns>True if password was saved, otherwise false</returns>
    public static bool CredWrite(
      string userName,
      SecureString secPassword,
      string target = "",
      string callingApp = "Kraken Security",
      string comment = "",
      CredUtils.CRED_TYPE credType = CredUtils.CRED_TYPE.GENERIC,
      CredUtils.CRED_PERSIST persist = CredUtils.CRED_PERSIST.LOCAL_MACHINE,
      bool throwOnError = true
    //, ITrace trace = null // because Kraken.Security doesn't have access to this
    ) {
      if (string.IsNullOrEmpty(userName))
        throw new ArgumentNullException("userName");
      if (secPassword == null)
        throw new ArgumentNullException("secPassword");
      if (string.IsNullOrEmpty(target)) {
        if (credType == CredUtils.CRED_TYPE.GENERIC
          || credType == CredUtils.CRED_TYPE.GENERIC_CERTIFICATE)
          throw new ArgumentNullException("target");
        target = userName;
      }
      //if (trace == null) trace = NullTrace.Default;
      if (credType != CredUtils.CRED_TYPE.GENERIC && 337 < target.Length)
        throw new NotSupportedException(string.Format("Credential target (url or user) is longer than allowed (max 337 characters). Length={0}", target.Length));
      CredUtils.Credential cred = new CredUtils.Credential() {
        Persist = persist,
        Type = credType,
        UserName = userName,
        AttributeCount = 0,
        //TargetAlias = "", 
        TargetName = target,
      };
      if (string.IsNullOrWhiteSpace(cred.Comment))
        cred.Comment = string.Format("Saved by {0} on {1}", callingApp, DateTime.Now); // computername, username, domain
      // this is the DNS domain name of the machine
      string domainName = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
      if (target == userName
        && (cred.Type == CredUtils.CRED_TYPE.DOMAIN_PASSWORD
        || cred.Type == CredUtils.CRED_TYPE.DOMAIN_CERTIFICATE)) {
        cred.Flags = CredUtils.CRED_FLAGS.USERNAME_TARGET;
      } else {
        cred.Flags = CredUtils.CRED_FLAGS.NONE;
      }
      using (SecureStringMarshaller sm = new SecureStringMarshaller(secPassword)) {
        if (!sm.IsDecrypted)
          sm.Decrypt();
        string password = sm.ToString();
        cred.CredentialBlobSize = (uint)Encoding.Unicode.GetBytes(password).Length;
        cred.CredentialBlob = password;
        // wrtie out and destroy cred before leaving security scope
        int result = 0;
        string err = string.Format("Failed to write to credentials store for target '{0}' using '{0}', '{2}'. ", target, userName, comment);
        try {
          result = CredUtils.CredWrite(cred);
        } catch (Exception ex) {
          //trace.TraceError(err);
          //trace.TraceError(ex);
          if (throwOnError)
            throw new SecurityException(err, ex);
          return false;
        }
        if (result != 0) {
          //trace.TraceError(err);
          if (throwOnError)
            throw new SecurityException(err + "result=" + result.ToString("X"));
          return false;
        }
      } // using scope
      return true;
    }

    /// <summary>
    /// Reads stored credentials for specified user and target.
    /// Calls Win32 CredReadW via [PsUtils.CredMan]::CredRead
    /// </summary>
    /// <param name="userName">
    /// Specified the user name to use with the credential.
    /// If not provided, throw ArgumentNullException
    /// TODO - If not provided, should default to the current user name.
    /// </param>
    /// <param name="target">
    /// Specifies the URI for which the credentials are associated
    /// If none is provided, and the type is not GENERIC or 
    /// CENERIC_CERTIFICATE, the username is used as the target.
    /// </param>
    /// <param name="credType">
    /// Specifies the desired credentials type; defaults to  "CRED_TYPE_GENERIC"
    /// </param>
    /// <param name="throwOnError">
    /// If true, errors will cause SecurityException
    /// If false, they are swept under the rug
    /// </param>
    /// <returns>A SecureString object containing the password, or null.</returns>
    public static SecureString CredRead(
      string userName,
      string target = "",
      CredUtils.CRED_TYPE credType = CredUtils.CRED_TYPE.GENERIC,
      bool throwOnError = true
    ) {
      if (string.IsNullOrEmpty(userName))
        throw new ArgumentNullException("userName");
      if (string.IsNullOrEmpty(target)) {
        if (credType == CredUtils.CRED_TYPE.GENERIC
          || credType == CredUtils.CRED_TYPE.GENERIC_CERTIFICATE)
          throw new ArgumentNullException("target");
        target = userName;
      }
      if (credType != CredUtils.CRED_TYPE.GENERIC && 337 < target.Length)
        throw new NotSupportedException(string.Format("Credential target (url or user) is longer than allowed (max 337 characters). Length={0}", target.Length));
      uint result = 0;
      string err = string.Format("Error reading credentials from store for target='{0}' user='{0}'. ", target, userName);
      SecureString ss = null;
      try {
        CredUtils.Credential cred;
        result = (uint)CredRead(target, credType, out cred);
        using (SecureStringMarshaller sm = new SecureStringMarshaller(cred.CredentialBlob)) {
          ss = sm.SecureData;
        }
      } catch (Exception ex) {
        if (throwOnError)
          throw new SecurityException(err, ex);
        return null;
      }
      switch (result) {
        case 0:
          break; // OK
        case 0x80070490: // Cred not found
          return null;
        default:
          if (throwOnError)
            throw new SecurityException(err + " result=" + result.ToString("X"));
          return null;
      }
      return ss;
    }

    #region Custom API

    public static int CredDelete(string target, CRED_TYPE type) {
      if (!CredDeleteW(target, type, 0)) {
        return Marshal.GetHRForLastWin32Error();
      }
      return 0;
    }

    public static int CredEnum(string Filter, out Credential[] Credentials) {
      int count = 0;
      int Flags = 0x0;
      if (string.IsNullOrEmpty(Filter) ||
          "*" == Filter) {
        Filter = null;
        if (6 <= Environment.OSVersion.Version.Major) {
          Flags = 0x1; //CRED_ENUMERATE_ALL_CREDENTIALS; only valid is OS >= Vista
        }
      }
      IntPtr pCredentials = IntPtr.Zero;
      if (!CredEnumerateW(Filter, Flags, out count, out pCredentials)) {
        Credentials = null;
        return Marshal.GetHRForLastWin32Error();
      }
      CriticalCredentialHandle CredHandle = new CriticalCredentialHandle(pCredentials);
      Credentials = CredHandle.GetCredentials(count);
      return 0;
    }

    public static int CredRead(string target, CRED_TYPE type, out Credential Credential) {
      IntPtr pCredential = IntPtr.Zero;
      Credential = new Credential();
      if (!CredReadW(target, type, 0, out pCredential)) {
        return Marshal.GetHRForLastWin32Error();
      }
      CriticalCredentialHandle CredHandle = new CriticalCredentialHandle(pCredential);
      Credential = CredHandle.GetCredential();
      return 0;
    }

    public static int CredWrite(Credential userCredential) {
      if (!CredWriteW(ref userCredential, 0)) {
        return Marshal.GetHRForLastWin32Error();
      }
      return 0;
    }

    #endregion

    #region Imports
    // DllImport derives from System.Runtime.InteropServices
    [DllImport("Advapi32.dll", SetLastError = true, EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode)]
    private static extern bool CredDeleteW([In] string target, [In] CRED_TYPE type, [In] int reservedFlag);

    [DllImport("Advapi32.dll", SetLastError = true, EntryPoint = "CredEnumerateW", CharSet = CharSet.Unicode)]
    private static extern bool CredEnumerateW([In] string Filter, [In] int Flags, out int Count, out IntPtr CredentialPtr);

    [DllImport("Advapi32.dll", SetLastError = true, EntryPoint = "CredFree")]
    private static extern void CredFree([In] IntPtr cred);

    [DllImport("Advapi32.dll", SetLastError = true, EntryPoint = "CredReadW", CharSet = CharSet.Unicode)]
    private static extern bool CredReadW([In] string target, [In] CRED_TYPE type, [In] int reservedFlag, out IntPtr CredentialPtr);

    [DllImport("Advapi32.dll", SetLastError = true, EntryPoint = "CredWriteW", CharSet = CharSet.Unicode)]
    private static extern bool CredWriteW([In] ref Credential userCredential, [In] UInt32 flags);
    #endregion

    #region Fields
    public enum CRED_FLAGS : uint {
      NONE = 0x0,
      PROMPT_NOW = 0x2,
      USERNAME_TARGET = 0x4
    }

    public enum CRED_ERRORS : uint {
      ERROR_SUCCESS = 0x0,
      ERROR_INVALID_PARAMETER = 0x80070057,
      ERROR_INVALID_FLAGS = 0x800703EC,
      ERROR_NOT_FOUND = 0x80070490,
      ERROR_NO_SUCH_LOGON_SESSION = 0x80070520,
      ERROR_BAD_USERNAME = 0x8007089A
    }

    public enum CRED_PERSIST : uint {
      SESSION = 1,
      LOCAL_MACHINE = 2,
      ENTERPRISE = 3
    }

    public enum CRED_TYPE : uint {
      GENERIC = 1,
      DOMAIN_PASSWORD = 2,
      DOMAIN_CERTIFICATE = 3,
      DOMAIN_VISIBLE_PASSWORD = 4,
      GENERIC_CERTIFICATE = 5,
      DOMAIN_EXTENDED = 6,
      MAXIMUM = 7,      // Maximum supported cred type
      MAXIMUM_EX = (MAXIMUM + 1000),  // Allow new applications to run on old OSes
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Credential {
      public CRED_FLAGS Flags;
      public CRED_TYPE Type;
      public string TargetName;
      public string Comment;
      public DateTime LastWritten;
      public UInt32 CredentialBlobSize;
      public string CredentialBlob;
      public CRED_PERSIST Persist;
      public UInt32 AttributeCount;
      public IntPtr Attributes;
      public string TargetAlias;
      public string UserName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential {
      public CRED_FLAGS Flags;
      public CRED_TYPE Type;
      public IntPtr TargetName;
      public IntPtr Comment;
      public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
      public UInt32 CredentialBlobSize;
      public IntPtr CredentialBlob;
      public UInt32 Persist;
      public UInt32 AttributeCount;
      public IntPtr Attributes;
      public IntPtr TargetAlias;
      public IntPtr UserName;
    }
    #endregion

    #region Child Class
    private class CriticalCredentialHandle : Microsoft.Win32.SafeHandles.CriticalHandleZeroOrMinusOneIsInvalid {
      public CriticalCredentialHandle(IntPtr preexistingHandle) {
        SetHandle(preexistingHandle);
      }

      private Credential XlateNativeCred(IntPtr pCred) {
        NativeCredential ncred = (NativeCredential)Marshal.PtrToStructure(pCred, typeof(NativeCredential));
        Credential cred = new Credential();
        cred.Type = ncred.Type;
        cred.Flags = ncred.Flags;
        cred.Persist = (CRED_PERSIST)ncred.Persist;

        long LastWritten = ncred.LastWritten.dwHighDateTime;
        LastWritten = (LastWritten << 32) + ncred.LastWritten.dwLowDateTime;
        cred.LastWritten = DateTime.FromFileTime(LastWritten);

        cred.UserName = Marshal.PtrToStringUni(ncred.UserName);
        cred.TargetName = Marshal.PtrToStringUni(ncred.TargetName);
        cred.TargetAlias = Marshal.PtrToStringUni(ncred.TargetAlias);
        cred.Comment = Marshal.PtrToStringUni(ncred.Comment);
        cred.CredentialBlobSize = ncred.CredentialBlobSize;
        if (0 < ncred.CredentialBlobSize) {
          cred.CredentialBlob = Marshal.PtrToStringUni(ncred.CredentialBlob, (int)ncred.CredentialBlobSize / 2);
        }
        return cred;
      }

      public Credential GetCredential() {
        if (IsInvalid) {
          throw new InvalidOperationException("Invalid CriticalHandle!");
        }
        Credential cred = XlateNativeCred(handle);
        return cred;
      }

      public Credential[] GetCredentials(int count) {
        if (IsInvalid) {
          throw new InvalidOperationException("Invalid CriticalHandle!");
        }
        Credential[] Credentials = new Credential[count];
        IntPtr pTemp = IntPtr.Zero;
        for (int inx = 0; inx < count; inx++) {
          pTemp = Marshal.ReadIntPtr(handle, inx * IntPtr.Size);
          Credential cred = XlateNativeCred(pTemp);
          Credentials[inx] = cred;
        }
        return Credentials;
      }

      override protected bool ReleaseHandle() {
        if (IsInvalid) {
          return false;
        }
        CredFree(handle);
        SetHandleAsInvalid();
        return true;
      }
    }
    #endregion

    #region Test Harness (of sorts)

    /*
     * Note: the Main() function is primarily for debugging and testing in a Visual 
     * Studio session.  Although it will work from PowerShell, it's not very useful.
     */
    public static void Main() {
      Credential[] Creds = null;
      Credential Cred = new Credential();
      int Rtn = 0;

      Console.WriteLine("Testing CredWrite()");
      Rtn = AddCred();
      if (!CheckError("CredWrite", (CRED_ERRORS)Rtn)) {
        return;
      }
      Console.WriteLine("Testing CredEnum()");
      Rtn = CredEnum(null, out Creds);
      if (!CheckError("CredEnum", (CRED_ERRORS)Rtn)) {
        return;
      }
      Console.WriteLine("Testing CredRead()");
      Rtn = CredRead("Target", CRED_TYPE.GENERIC, out Cred);
      if (!CheckError("CredRead", (CRED_ERRORS)Rtn)) {
        return;
      }
      Console.WriteLine("Testing CredDelete()");
      Rtn = CredDelete("Target", CRED_TYPE.GENERIC);
      if (!CheckError("CredDelete", (CRED_ERRORS)Rtn)) {
        return;
      }
      Console.WriteLine("Testing CredRead() again");
      Rtn = CredRead("Target", CRED_TYPE.GENERIC, out Cred);
      if (!CheckError("CredRead", (CRED_ERRORS)Rtn)) {
        Console.WriteLine("if the error is 'ERROR_NOT_FOUND', this result is OK.");
      }
    }


    private static int AddCred() {
      Credential Cred = new Credential();
      string Password = "Password";
      Cred.Flags = 0;
      Cred.Type = CRED_TYPE.GENERIC;
      Cred.TargetName = "Target";
      Cred.UserName = "UserName";
      Cred.AttributeCount = 0;
      Cred.Persist = CRED_PERSIST.ENTERPRISE;
      Cred.CredentialBlobSize = (uint)Password.Length;
      Cred.CredentialBlob = Password;
      Cred.Comment = "Comment";
      return CredWrite(Cred);
    }

    private static bool CheckError(string TestName, CRED_ERRORS Rtn) {
      switch (Rtn) {
        case CRED_ERRORS.ERROR_SUCCESS:
          Console.WriteLine(string.Format("'{0}' worked", TestName));
          return true;
        case CRED_ERRORS.ERROR_INVALID_FLAGS:
        case CRED_ERRORS.ERROR_INVALID_PARAMETER:
        case CRED_ERRORS.ERROR_NO_SUCH_LOGON_SESSION:
        case CRED_ERRORS.ERROR_NOT_FOUND:
        case CRED_ERRORS.ERROR_BAD_USERNAME:
          Console.WriteLine(string.Format("'{0}' failed; {1}.", TestName, Rtn));
          break;
        default:
          Console.WriteLine(string.Format("'{0}' failed; 0x{1}.", TestName, Rtn.ToString("X")));
          break;
      }
      return false;
    }

    #endregion

  }
}
