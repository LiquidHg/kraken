//-----------------------------------------------------------------------------
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
//-----------------------------------------------------------------------------

namespace Kraken.Core.Security.Certificates {

  using System;
  using System.Security.Cryptography.X509Certificates;
  using System.Text.RegularExpressions;

  using log4net;

  /// <summary>
  /// A utility class which helps to retrieve an x509 certificate
  /// </summary>
  public class CertificateUtil {

    private static bool VerboseLogs = false; // make true to get more detail

    private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public static string NormalizeThumbprint(string thumbprint) {
      // removes any characters that would not be basic hex digits
      thumbprint = Regex.Replace(thumbprint, @"[^\da-zA-z]", string.Empty).ToUpper();
      if (thumbprint.Length != 40)
        return string.Empty;
      return thumbprint;
    }

#if DOTNET_V35
    public static X509Certificate2 GetCertificate(StoreName storeName, StoreLocation location, string subjectNameOrThumbprint) {
      return GetCertificate(storeName, location, subjectNameOrThumbprint, false);
    }
    public static X509Certificate2 GetCertificate(StoreName storeName, StoreLocation location, string subjectNameOrThumbprint, bool validOnly) {
#else
    public static X509Certificate2 GetCertificate(StoreName storeName, StoreLocation location, string subjectNameOrThumbprint, bool validOnly = false) {
#endif
      string notFoundMessage = string.Format("No certificate was found for subject name or thumbprint '{0}' in store '{1}' at '{2}'.", subjectNameOrThumbprint, storeName, location);
      string tooManyFoundMessage = string.Format("Multiple certificates found for subject name or thumbprint '{0}' in store '{1}' at '{2}'.", subjectNameOrThumbprint, storeName, location);
      X509Store store = new X509Store(storeName, location);
      X509Certificate2Collection certificates = null;
      try {
        store.Open(OpenFlags.ReadOnly);

        string thumbprint = NormalizeThumbprint(subjectNameOrThumbprint);
        X509FindType findType = !string.IsNullOrEmpty(thumbprint) ? X509FindType.FindByThumbprint : X509FindType.FindBySubjectName;
        var findValue = findType.Equals(X509FindType.FindByThumbprint) ? thumbprint : subjectNameOrThumbprint;

        certificates = store.Certificates.Find(findType, findValue, validOnly);
        if (certificates.Count == 1)
            return new X509Certificate2(certificates[0]);
        if (certificates.Count > 1)
            throw new InvalidOperationException(tooManyFoundMessage);

        // Alernative less strict method to try and get the cert by subject name
        X509Certificate2 result = null;
        // Every time we call store.Certificates property, a new collection will be returned.
        certificates = store.Certificates;
        for (int i = 0; i < certificates.Count; i++) 
        {
            X509Certificate2 cert = certificates[i];
            bool isFinded = false;
            if(findType.Equals(X509FindType.FindByThumbprint))
            {
                isFinded = cert.Thumbprint.Equals(findValue, StringComparison.InvariantCultureIgnoreCase);
            }
            else if(findType.Equals(X509FindType.FindBySubjectName))
            {
                isFinded = cert.SubjectName.Name.Equals(findValue, StringComparison.InvariantCultureIgnoreCase);
            }

            if(isFinded)
            {
               if (result != null)
                    throw new InvalidOperationException(tooManyFoundMessage);
                result = new X509Certificate2(cert);
            }
        }

        if (result == null || (validOnly && !result.Verify()))
            throw new InvalidOperationException(notFoundMessage);
        return result;
      } finally {
        // What is the purpose of having this code here??
        if (certificates != null) {
          for (int i = 0; i < certificates.Count; i++) {
            X509Certificate2 cert = certificates[i];
            cert.Reset();
          }
        }
        store.Close();
      }
    }


/// <summary>
/// Returns certificate from X509Store
/// </summary>
/// <param name="storeName"></param>
/// <param name="location"></param>
/// <param name="subjectNameOrThumbprint"></param>
/// <param name="validOnly"></param>
/// <remarks>
/// Old function GetCertificate runs in a number of cases no correct or slow.
/// For example, in the case of the argument of the form "CN=DAF" cycle was used as X509FindType.FindBySubjectName searched only in form "DAF".
/// Also in the case of the argument of the form "CN=SharePoint Security Token Service Testing 1"
/// it defines how Thumbprint (because after Regex.Replace string has length is 40)
/// </remarks>
/// <returns></returns>
#if DOTNET_V35
    public static X509Certificate2 GetCertificate2(StoreName storeName, StoreLocation location, string subjectNameOrThumbprint) {
      return GetCertificate2(storeName, location, subjectNameOrThumbprint, false);
    }
    public static X509Certificate2 GetCertificate2(StoreName storeName, StoreLocation location, string subjectNameOrThumbprint, bool validOnly)
#else
    public static X509Certificate2 GetCertificate2(StoreName storeName, StoreLocation location, string subjectNameOrThumbprint, bool validOnly = false)
#endif
    {
        if (string.IsNullOrEmpty(subjectNameOrThumbprint))
            throw new ArgumentNullException("subjectNameOrThumbprint");

        string notFoundMessage = string.Format("No certificate was found for subject name or thumbprint '{0}' in store '{1}' at '{2}'.", subjectNameOrThumbprint, storeName, location);

        X509Store store = new X509Store(storeName, location);

        try
        {
            store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, subjectNameOrThumbprint, validOnly);
            if(certificates.Count != 0)
            {
                return new X509Certificate2(certificates[0]);
            }
            else 
            {
                var thumbprint = Regex.Replace(subjectNameOrThumbprint, @"[^\da-zA-z]", string.Empty).ToUpper();
                if (thumbprint.Length == 40)
                {
                    certificates = store.Certificates.Find(X509FindType.FindByThumbprint, subjectNameOrThumbprint, validOnly);
                    if(certificates.Count != 0)
                    {
                        return new X509Certificate2(certificates[0]);
                    }
                }
            }

            throw new InvalidOperationException(notFoundMessage);
        }
        finally
        {
            store.Close();
        }
    }


    // TODO put some additional security around this code
#if DOTNET_V35
    public static bool AddCertificateToLocalStore(X509Certificate2 certificate) {
      return AddCertificateToLocalStore(certificate, StoreName.My, StoreLocation.LocalMachine);
    }
    public static bool AddCertificateToLocalStore(X509Certificate2 certificate, StoreName storeName, StoreLocation location) {
#else
    public static bool AddCertificateToLocalStore(X509Certificate2 certificate, StoreName storeName = StoreName.My, StoreLocation location = StoreLocation.LocalMachine) {
#endif
      bool success = false;
      try {
        X509Store store = new X509Store(storeName, location);
        store.Open(OpenFlags.ReadWrite);
        if (!store.Certificates.Contains(certificate)) {
          store.Add(certificate);
          store.Close();
          success = true;
        }
      } catch (Exception ex) {
        Log.Error("ERROR WRITING CERTIFICATE TO STORE", ex);
      }
      if (!success) {
        Log.Error(string.Format("Certificate with friendly name '{0}' subject '{1}' and thumbprint '{2}' was not added to the local store. Look for an exception in ULS log preceding this message to find the cause.", certificate.FriendlyName, certificate.Subject, certificate.Thumbprint));
      }
      return success;
    }

  } // class
}