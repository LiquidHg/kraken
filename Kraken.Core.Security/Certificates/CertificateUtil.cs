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
  using System.Collections.Generic;

  public class GetCertificateOptions {
    public bool ValidOnly = false;
    public bool ConvertInMemoryToMy = true;
    public bool SharePointStoreHack = true;
  }

  public static class CertificateUtil {

    //private static bool VerboseLogs = false; // make true to get more detail

    private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public static string NormalizeThumbprint(string thumbprint) {
      // removes any characters that would not be basic hex digits
      thumbprint = Regex.Replace(thumbprint, @"[^\da-zA-z]", string.Empty).ToUpper();
      if (thumbprint.Length != 40)
        return string.Empty;
      return thumbprint;
    }

    public static X509Certificate2 GetCertificate(StoreName store, StoreLocation location, string subjectNameOrThumbprint, GetCertificateOptions options = null) {
      return GetCertificate(store.ToString(), location, subjectNameOrThumbprint, options);
    }
    public static X509Certificate2 GetCertificate(string storeName, StoreLocation location, string subjectNameOrThumbprint, GetCertificateOptions options = null) {
      if (options == null)
        options = new GetCertificateOptions();
      if (string.IsNullOrEmpty(subjectNameOrThumbprint))
        throw new ArgumentNullException("subjectNameOrThumbprint");
      if (options.ConvertInMemoryToMy) {
        // Certificates passed by Beowulf use this store name to indicate they were received by a KeyDescription and may not exist locally
        if (storeName == "InMemory")
          storeName = StoreName.My.ToString();
      }
      string tooManyFoundMessage = string.Format("Multiple certificates found for subject name or thumbprint '{0}' in store '{1}' at '{2}'.", subjectNameOrThumbprint, storeName, location);
      string notFoundMessage = string.Format("No certificate was found for subject name or thumbprint '{0}' in store '{1}' at '{2}'.", subjectNameOrThumbprint, storeName, location);
      X509Store store = new X509Store(storeName, location);
      try {
        store.Open(OpenFlags.ReadOnly);
        var certificates = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, subjectNameOrThumbprint, options.ValidOnly);
        if (certificates.Count != 0) {
          if (certificates.Count > 1)
            throw new InvalidOperationException(tooManyFoundMessage);
          return new X509Certificate2(certificates[0]);
        } else {
          var thumbprint = Regex.Replace(subjectNameOrThumbprint, @"[^\da-zA-z]", string.Empty).ToUpper();
          if (thumbprint.Length == 40) {
            certificates = store.Certificates.Find(X509FindType.FindByThumbprint, subjectNameOrThumbprint, options.ValidOnly);
            if (certificates.Count != 0) {
              if (certificates.Count > 1)
                throw new InvalidOperationException(tooManyFoundMessage);
              return new X509Certificate2(certificates[0]);
            }
          }
        }
        // HACK If you can't find the cert in My, try the SharePoint store
        if (options.SharePointStoreHack) {
          X509Certificate2 cert = null;
          try {
            storeName = "SharePoint";
            cert = GetCertificate(storeName, location, subjectNameOrThumbprint, options);
          } catch { }
          if (cert != null)
            return cert;
        }
        throw new KeyNotFoundException(notFoundMessage);
      } finally {
        store.Close();
      }
    }

    // TODO put some additional security around this code
    public static bool AddCertificateToLocalStore(X509Certificate2 certificate, StoreName storeName = StoreName.My, StoreLocation location = StoreLocation.LocalMachine) {
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