using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using log4net;

namespace Kraken.Security.Web {
	public static class WebSslUtility {

		private static string allowedSSLCertificateNames = string.Empty;

		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public static void AttachSslBypassEvents(Uri targetUri, string allowedCerts = "") {
#if DEBUG
			if (targetUri.Scheme.ToLower() == "https"
				&& !string.IsNullOrEmpty(allowedCerts)) {
					allowedSSLCertificateNames = allowedCerts;
				if (ServicePointManager.ServerCertificateValidationCallback == null)
					ServicePointManager.ServerCertificateValidationCallback += AllowDevelopmentCertificates;
#else
			Log.InfoFormat("AttachSslBypassEvents for url '{0}' and certs '{1}' disabled in release mode. This method is only effective in DEBUG mode and has been disaabled for security reasons. ", targetUri, allowedCerts);
#endif
			}
		}

#if DEBUG
		public static bool AllowDevelopmentCertificates(
			Object sender,
			X509Certificate certificate,
			X509Chain chain,
			SslPolicyErrors sslPolicyErrors) {

			Log.Entering(MethodBase.GetCurrentMethod());
			if (certificate == null)
				Log.Error("No certificate.");
			Log.InfoFormat("Checking certiticate with subject '{0}' and thumbprint '{1}'.", certificate.Subject, "not known");
			bool nameMismatch = ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) > 0);
			bool notAvailable = ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) > 0);
			bool chainError = ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) > 0);
			Log.DebugFormat("SslPolicyErrors.RemoteCertificateNameMismatch = '{0}'", nameMismatch);
			Log.DebugFormat("SslPolicyErrors.RemoteCertificateNotAvailable = '{0}'", notAvailable);
			Log.DebugFormat("SslPolicyErrors.RemoteCertificateChainErrors = '{0}'", chainError);
			if (sslPolicyErrors == SslPolicyErrors.None) {
				Log.Leaving(MethodBase.GetCurrentMethod(), "returning true");
				return true;
			}

			if (nameMismatch || chainError) {
				string bypassCertNames = allowedSSLCertificateNames;
				Log.DebugFormat("bypassCertNames = '{0}'", bypassCertNames);
				if (bypassCertNames == "*")
					return true;

				string[] allowed = bypassCertNames.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string allow in allowed) {
					// == "CN=127.0.0.1, O=TESTING ONLY, OU=Windows Azure"
					if (certificate.Subject.Contains(allow)) {
						Log.InfoFormat("Bypassing certiticate containing '{0}'.", allow);
						return true;
					}
				}
			}
			Log.Leaving(MethodBase.GetCurrentMethod(), "returning false");

			throw new System.Security.SecurityException(string.Format("Problem with certificate prevented SSL/TLS connection. sslPolicyErrors={0}, subject='{1}'", sslPolicyErrors, certificate.Subject));
			//return false;
		}
#endif

	}
}
