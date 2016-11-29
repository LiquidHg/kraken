using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Security.Cryptography;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Kraken.Core.Security.Unsafe;
using System.Diagnostics;

namespace Kraken.Core.Security {

  // TODO further secure the methods of this class from unsafe calling and reflection.

  internal sealed class SafeBuffer : SafeHandle {

    private sealed class UnsafeNativeMethods {

      private UnsafeNativeMethods() { }

      [SecurityPermissionAttribute(SecurityAction.LinkDemand, UnmanagedCode = true)]
      [DllImport("Kernel32.dll", SetLastError = true)]
      //[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
      [return: MarshalAs(UnmanagedType.U1)] // test this attribute, might be Bool
      public static extern bool CloseHandle(IntPtr hObject);
    }

    private bool useUnicode;
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public bool EnableUnicode {
      get { return useUnicode; }
      set {
        if (!this.IsInvalid)
          throw new InvalidOperationException("Can't change state of EnableUnicode property when there is dcrypted data alkready in the buffer.");
        useUnicode = value;
      }
    }

    internal SafeBuffer() : base(IntPtr.Zero, true) { }

    public void Decrypt(SecureString secureData) {
      if (secureData == null)
        throw new ArgumentNullException("secureData");
      if (secureData.Length == 0)
        return;
      if (this.IsInvalid) {
        if (this.EnableUnicode) {
          this.SetHandle(Marshal.SecureStringToGlobalAllocUnicode(secureData));
        } else {
          this.SetHandle(Marshal.SecureStringToBSTR(secureData));
        }
      }
    }

    [SecurityPermissionAttribute(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public unsafe char* ToPointer() {
      return (char*)DangerousGetHandle().ToPointer();
    }
    /*
    public unsafe IntPtr ToIntPointer() {
      return DangerousGetHandle();
    }
     */

    public override bool IsInvalid {
      get {
        return IntPtr.Zero.Equals(this.handle);
      }
    }

    [SecurityPermissionAttribute(SecurityAction.LinkDemand, UnmanagedCode = true)]
    protected override bool ReleaseHandle() {
      if (!this.IsInvalid) {
        if (this.EnableUnicode) {
          Marshal.ZeroFreeGlobalAllocUnicode(handle);
        } else {
          Marshal.ZeroFreeBSTR(handle);
        }
      }
      handle = IntPtr.Zero; // SetHandleAsInvalid();
      return UnsafeNativeMethods.CloseHandle(handle);
    }

  } // class

  // TODO add additional assertions to make it harder for limited CAS policies to use this code
  // TODO add additional secret code to make it harder for unknown callers to use this code
  // TODO provide additional guidance to developers about how to safely use this code

  /// <summary>
  /// This class encapsulates the marshalling and other logic required
  /// to encode and decode SecureString data. Use the methods of this
  /// class sparingly, as they result in security vulnerabilitoes.
  /// 
  /// WARN making this a public class will allow other callers to use it
  /// to easily decypher secures trings stored in your application.
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
  public class SecureStringMarshaller : IDisposable {

    #region Constructor / Destructor

    /// <summary>
    /// The destructor (Finalize) ensures that Dispose is called and
    /// that any insecure text is cleared from memory. If you planned
    /// to do something with the clear text, you should complete it
    /// before this method runs.
    /// </summary>
    ~SecureStringMarshaller() {
      Dispose(false);
    }

    /// <summary>
    /// Create a new (empty) instance of SecureString that you can populate later.
    /// </summary>
    public SecureStringMarshaller() {
      ssSecureData = new SecureString();
    }

    /// <summary>
    /// Provide an existing SecureString to work with the marshaller.
    /// This is the most secure constructor.
    /// </summary>
    /// <param name="strSecureString"></param>
    public SecureStringMarshaller(SecureString secureString) {
      ssSecureData = secureString;
    }

    /// <summary>
    /// Copies a managed string into the SecureString and optionally marks
    /// the object as read only so it can't be altered later.
    /// 
    /// WARN This managed string will remain in memory until collected by the GC.
    /// You can use StringTools.RandomizeAndZero to clear its contents.
    /// </summary>
    /// <param name="strInsecureString">The insecure managed string to import and copy.</param>
    /// <param name="markReadOnly">If true, the secure string will be locked.</param>
    public SecureStringMarshaller(string insecureText, bool markReadOnly) {
      // TODO test this to see what has become of the string from the internal call
      ssSecureData = ConvertToSecureString(insecureText, markReadOnly, false);
    }
    public SecureStringMarshaller(string insecureText) : this(insecureText, true) { }

    /// <summary>
    /// Reads a stream reader into the Secure String. This can be useful for copying
    /// data directly from a decryption stream. The stream is consumed as the data is
    /// read into it; you will not be abke to view the plaintext again, except through
    /// the methods of this class, as the data will have moved.
    /// <param name="objReader">The target stream</param>
    /// <param name="markReadOnly">If true, the secure string will be locked.</param>
    public SecureStringMarshaller(StreamReader objReader, bool markReadOnly) {
      ssSecureData = ConvertToSecureString(objReader, markReadOnly);
    }
    public SecureStringMarshaller(StreamReader objReader) : this(objReader, true) { }

    /// <summary>
    /// Sets up the marshaller and imports an unmanaged strings into the SecureString.
    /// This is the most direct method to secure text from unmanaged code since it
    /// is supported directly be the SecureString class. For best security, consider
    /// using StringTools.RandomizeAndZero to scramble the value of the unmanaged
    /// string after you've passed it into the SecureString.
    /// </summary>
    /// <param name="clearText">An unamanged string in plain text</param>
    /// <param name="length">the length of clearText</param>
    /// <param name="markReadOnly">If true, the secure string will be locked.</param>
    [CLSCompliant(false)]
    public unsafe SecureStringMarshaller(char* clearText, int length, bool makeReadOnly) {
      ssSecureData = ConvertToSecureString(clearText, length, makeReadOnly);
    }
    [CLSCompliant(false)]
    public unsafe SecureStringMarshaller(char* clearText, int length) : this(clearText, length, true) { }

    #endregion

    #region Private Properties

    /// <summary>
    /// The target SecureString that will be decrypted by the marshaller
    /// </summary>
    private SecureString ssSecureData;

    /// <summary>
    /// Pointer to the unmanaged BSTR created by Decrypt();
    /// </summary>
    private SafeBuffer bufferPointer = new SafeBuffer();

    /// <summary>
    /// Used to store reference to string created by ToString()
    /// </summary>
    private ArrayList arrClearText = new ArrayList(ClearTextStringCapacity);

    /// <summary>
    /// It is a good idea to define this explicitly, becuase when
    /// arrClearText exceeds it, it is very likely the whole colletion
    /// will be memory-copied into a larger array.
    /// </summary>
    private const int ClearTextStringCapacity = 10;

    #endregion

    #region Public Properties

    /// <summary>
    /// The SecureString class that is created and populated by the constructor.
    /// </summary>
    public SecureString SecureData {
      get { return ssSecureData; }
    }

    /// <summary>
    /// Converts the decryption buffer into a managed string. If decryption has
    /// not been done on the SecureString, it is done at this time. For the most
    /// security, avoid modifying the string after it is created, since a reference
    /// to the string is saved in the class. This string will be scrambled when the
    /// class is Disposed. Be advsied that the use of this method is a potential
    /// security risk, since copies of the string could be made by other code or
    /// the GC, and this method is open to reflection as well.
    /// </summary>
    public override string ToString() {
#if AutoDecryptSecureStrings
        if (!IsDecrypted)
            Decrypt();
#else
      if (!IsDecrypted)
        throw new SecureStringMarshallerException("ExceptionMessageSecureStringCantReadEncryptedData");
          //Colossus.Resources.ExceptionMessageSecureStringCantReadEncryptedData);
#endif
      return BufferConvertToString();
    }

    /// <summary>
    /// Encode the buffer into a byte array of the appropriate length
    /// </summary>
    /// <param name="encoding">The encoding method to use with the string.</param>
    /// <param name="protectionScope">If specified, protects the return value using ProtectedData.</param>
    /// <param name="optionalEntropy">Optional: optionalEntropy to be used with ProtectedData.</param>
    /// <returns>The value protected with ProtectedData so it can't be rad on the stack.</returns>
    [DataProtectionPermission(SecurityAction.Demand, ProtectData = true)]
    public unsafe byte[] EncodeToByteArray(Encoding encoding, DataProtectionScope protectionScope, byte[] optionalEntropy) {
      int arrayLength = encoding.GetByteCount(Buffer, SecureData.Length);
      byte[] output = new byte[arrayLength];
      byte[] protectedOutput;
      fixed (byte* bytPtr = output) { // plaintext is readable, fix it so it can't be moved by GC
        encoding.GetBytes(Buffer, SecureData.Length, bytPtr, arrayLength);
        protectedOutput = ProtectedData.Protect(output, optionalEntropy, protectionScope);
        SecureStringTools.RandomizeAndZero(ref output);
      }
      return protectedOutput;
    }
    /// <summary>
    /// Encode the buffer into a byte array of the appropriate length
    /// </summary>
    /// <param name="encoding">The encoding method to use with the string.</param>
    /// <param name="protectionScope">If specified, protects the return value using ProtectedMemory.</param>
    /// <returns>The value protected with ProtectedMemory so it can't be rad on the stack.</returns>
    [DataProtectionPermission(SecurityAction.Demand, ProtectData = true)]
    public unsafe byte[] EncodeToByteArray(Encoding encoding, MemoryProtectionScope protectionScope) {
      int arrayLength = encoding.GetByteCount(Buffer, SecureData.Length);
      byte[] output = new byte[arrayLength];
      fixed (byte* bytPtr = output) { // plaintext is readable, fix it so it can't be moved by GC
        encoding.GetBytes(Buffer, SecureData.Length, bytPtr, arrayLength);
        ProtectedMemory.Protect(output, protectionScope);
      }
      return output;
    }

    /// <summary>
    /// Encode the buffer into a byte array of the appropriate length
    /// </summary>
    /// <param name="encoding">The encoding method to use with the string.</param>
    /// <returns>The encoded string, returned in clear text.</returns>C:\Projects\Tools\Xml\
    public unsafe byte[] EncodeToByteArray(Encoding encoding) {
      int arrayLength = encoding.GetByteCount(Buffer, SecureData.Length);
      byte[] output = new byte[arrayLength];
      fixed (byte* bytPtr = output) {
        encoding.GetBytes(Buffer, SecureData.Length, bytPtr, arrayLength);
      }
      return output;
    }

    /// <summary>
    /// Indicates that decryption has been performed and there is text in the buffer.
    /// </summary>
    // HACK made public instead of internal
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
    public bool IsDecrypted {
      get { return (bufferPointer != null && !bufferPointer.IsInvalid); }
    }

    public static bool IsNullOrEmpty(SecureString ss) {
      if (ss == null) return true;
      return (ss.Length <= 0);
    }

    #endregion

    #region Decryption Buffer - unmanaged character array

    /// <summary>
    /// Returns the unmanaged pointer to the descryption buffer.
    /// Unlike ToString this property does not perform any automatic decryption.
    /// WARNING this method is not safe and uses GDangerousGetHandle().ToPointer()
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands"), CLSCompliant(false)]
    public unsafe char* Buffer {
      get { return bufferPointer.ToPointer(); }
    }
    /*
    public unsafe IntPtr BufferAsIntPtr {
      get { return bufferPointer.ToIntPtr(); }
    }
     */

    /// <summary>
    /// Decrypts the secure string to a buffer in global memroy that is not subject to garbage collection.
    /// Managed libraries should access the ClearText property after calling this method.
    /// </summary>
    /// <returns>A char pointer that can be passed to unmanaged code.</returns>
    // HACK made public instead of internal
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
    [DataProtectionPermission(SecurityAction.Demand, UnprotectData = true)]
    // LinkDemand for SecurityPermissionAttribute.UnmanagedCode permission is not
    // needed, because this method only accepts SecureString data and EnsurePermission
    // is called to make sure we are allowed to decrypt it.
    public void Decrypt() { // char*
      EnsurePermissions(); // also covered by the attribute above
      this.BufferClear(); // ensure any previouis buffer is erased
      if (this.SecureData != null && this.SecureData.Length > 0)
        bufferPointer.Decrypt(this.SecureData);
      //return Buffer;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
    [DataProtectionPermission(SecurityAction.Demand, UnprotectData = true)]
    [CLSCompliant(false)]
    public unsafe char* DecryptGetBuffer() {
      Decrypt();
      return Buffer;
    }

    // keeps a reference to the string so we can scramble it when we dispose
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
    private unsafe string BufferConvertToString() {
      if (!IsDecrypted)
        throw new SecureStringMarshallerException("ExceptionMessageSecureStringCantReadEncryptedData");
      //Colossus.Resources.ExceptionMessageSecureStringCantReadEncryptedData
      if (arrClearText.Count > 0) {
        string strClearText = (string)arrClearText[0];
        if (!VerifyString(strClearText))
          throw new SecurityException("ExceptionMessageSecureStringBufferIntegrityFailure");
            //Colossus.Resources.ExceptionMessageSecureStringBufferIntegrityFailure
        // TODO something has obviously changed, perhaps we ought to change it back?
        // we've obviously done this before, so let's take the first string off the pile
        return strClearText;
      } else {
        // make a new string to follow and destroy later
        string strClearText = new string(Buffer);
        arrClearText.Add(strClearText);
        return strClearText;
      }
    }

    /// <summary>
    /// This method is provided as a means for destorying references to string that may
    /// contain copies of the plain text returned by this object's ToString method. Note
    /// that this will not prevent GC copies from being made by managed strings, but may
    /// reduce the overall number of strings that remain in memory after the marshaller is
    /// destroyed.
    /// </summary>
    /// <param name="strText"></param>
    [DataProtectionPermission(SecurityAction.Demand, UnprotectData = true)]
    // to ensure that we can't use the collection to wipe out random string data,
    // make sure the caller had the ability to decrypt the data in the first place.
    public void MarkStringForDisposal(ref string text) {
      EnsurePermissions();
      if (text == null)
        Debug.Write("WARN: marked a null string object for randomization!");
      else
        arrClearText.Add(text);
    }
    [DataProtectionPermission(SecurityAction.Demand, UnprotectData = true)]
    // to ensure that we can't use the collection to wipe out random string data,
    // make sure the caller had the ability to decrypt the data in the first place.
    [CLSCompliant(false)]
    public void MarkStringForDisposal(string text) {
      EnsurePermissions();
      if (string.IsNullOrEmpty(text))
        Debug.Write("WARN: marked a null string object for randomization!");
      else
        arrClearText.Add(text);
    }

    /// <summary>
    /// Tests to ensure that the contents of a string match the contents of the decryption
    /// buffer. This is useful when tracking the string, since in most cases it should not
    /// be different.
    /// </summary>
    /// <param name="strTest"></param>
    /// <returns></returns>
    private unsafe bool VerifyString(string strTest) {
      return SecureStringTools.IsStringContentEqualCharArray(strTest, Buffer);
    }

    /// <summary>
    /// Deallocates the buffer and scrambles any managed string created by ToString().
    /// Note that copies of the managed string may survive this procedure.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
    [DataProtectionPermission(SecurityAction.Demand, UnprotectData = true)]
    // to ensure that we can't use the collection to wipe out random string data,
    // make sure the caller had the ability to decrypt the data in the first place.
    private unsafe void BufferClear() {
      EnsurePermissions();
      if (bufferPointer != null) {
        bufferPointer.SetHandleAsInvalid();
      }
      // suspected not to be working because strClearText is not mutex
      // but we *may* be okay if the string is not altered in the two calls to the array
      for (int i = 0; i < arrClearText.Count; i++) {
        int stringLength = ((string)arrClearText[i]).Length;
        if (arrClearText[i] != null && !string.IsNullOrEmpty((string)arrClearText[i])) {
          fixed (char* stringFixed = ((string)arrClearText[i])) {
            SecureStringTools.RandomizeAndZero(stringFixed, stringLength);
          }
        }
      }
      arrClearText.Clear();
    }

    #endregion

    #region Static Import Routines

    /// <summary>
    /// Converts a string into a sSecure string.
    /// </summary>
    /// <param name="insecureText">The string data to import</param>
    /// <param name="makeReadOnly">Default is true, makes the SecureString read-only</param>
    /// <param name="destroySource">Default is false, scrambles the source data string</param>
    /// <returns>A SecureString with the string data encrypted in it</returns>
    /// <remarks>
    /// WARN You already have an insecure string at this point, so it may be too little too late.
    /// </remarks>
    [DataProtectionPermission(SecurityAction.Demand, ProtectData = true)]
    public static unsafe SecureString ConvertToSecureString(string insecureText, bool makeReadOnly, bool destroySource) {
      SecureString ssSecureData;
      fixed (char* pfixed = insecureText) {
        ssSecureData = ConvertToSecureString(pfixed, insecureText.Length, makeReadOnly);
        // TODO will clearing the string here have an effect on the caller?
        if (destroySource)
          SecureStringTools.RandomizeAndZero(ref insecureText);
      }
      return ssSecureData;
    }
    public static unsafe SecureString ConvertToSecureString(string insecureText, bool destroySource) {
      return ConvertToSecureString(insecureText, true, destroySource);
    }
    public static unsafe SecureString ConvertToSecureString(string insecureText) {
      return ConvertToSecureString(insecureText, true, false);
    }

    [DataProtectionPermission(SecurityAction.Demand, ProtectData = true)]
    [CLSCompliant(false)]
    public static unsafe SecureString ConvertToSecureString(char* insecureText, int length, bool makeReadOnly) {
      //if (ssSecureData != null)
      //  throw new System.Security.SecurityException("Attempted to overwrite existing SecureString class. This may be a substitution attack.");
      SecureString ssSecureData = new SecureString(insecureText, length);
      if (makeReadOnly)
        ssSecureData.MakeReadOnly();
      return ssSecureData;
    }

    /// <summary>
    /// This will read (and consume) the entire stream into the secure string.
    /// </summary>
    /// <param name="objReader"></param>
    [DataProtectionPermission(SecurityAction.Demand, ProtectData = true)]
    public static SecureString ConvertToSecureString(StreamReader objReader, bool markReadOnly) {
      //if (ssSecureData != null)
      //  throw new System.Security.SecurityException("Attempted to overwrite existing SecureString class. This may be a substitution attack.");
      SecureString ssSecureData = new SecureString();
      while (!objReader.EndOfStream) {
        int intCh = objReader.Read();
        ssSecureData.AppendChar((char)intCh);
      }
      if (markReadOnly)
        ssSecureData.MakeReadOnly();
      return ssSecureData;
    }

    #endregion

    #region IDisposable Members

    /// <summary>
    /// Implement IDisposable: Clears the buffer and collection of clear
    /// </summary>
    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private bool _isDisposed;

    /// <summary>
    /// Implement IDisposable: Clears the buffer and collection of clear
    /// text strings referenced in using the buffer in the .NET framework.
    /// You need to make use of these strings before this method is called.
    /// </summary>
    /// <param name="disposing">Has no effect at this time.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
    protected virtual void Dispose(bool disposing) {
      if (!_isDisposed) {
        if (disposing) {
          // Free other state (managed objects)
        }
        // Free unmanaged objects
        BufferClear();
        bufferPointer.Dispose();
        bufferPointer = null;
        // Set large fields to null.
        this.arrClearText = null;
        _isDisposed = true;
      }
    }

    #endregion

    /// <summary>
    /// Demands UnprotectData permission in order to allow decryption
    /// to occur. Throws a SecurityException if the operation fails.
    /// </summary>
    private static void EnsurePermissions() {
      // create a permission set to allow us to decrypt the data
      // Not sure if UnprotectData is the permission we really wish to Demand.
      DataProtectionPermission decryptPermission = new DataProtectionPermission(DataProtectionPermissionFlags.UnprotectData);
      try {
        decryptPermission.Demand();
      } catch (SecurityException securityEx) {
        throw new SecureStringMarshallerException("ExceptionMessageDPAPIPermissionDenied", securityEx);
        //Colossus.Resources.ExceptionMessageDPAPIPermissionDenied
      }
    }

  } // class SecureStringMarshaller

  [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly"), Serializable]
  public class SecureStringMarshallerException : Exception {

    public SecureStringMarshallerException() { }
    public SecureStringMarshallerException(string message) : base(message) { }
    public SecureStringMarshallerException(string message, Exception innerException) : base(message, innerException) { }
    protected SecureStringMarshallerException(SerializationInfo info, StreamingContext context) : base(info, context) { }

  }

} // namespace