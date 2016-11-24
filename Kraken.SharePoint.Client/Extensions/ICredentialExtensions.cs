namespace Microsoft.SharePoint.Client {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net;
  using System.Text;

  public static class KrakenICredentialExtensions {

    public static string GetUserName(this ICredentials cred) {
      if (cred == null)
        return string.Empty;
#if !DOTNET_V35
      SharePointOnlineCredentials spCred = cred as SharePointOnlineCredentials;
      if (spCred != null)
        return GetUserName(spCred);
#endif
      NetworkCredential netCred = cred as NetworkCredential;
      if (netCred != null)
        return GetUserName(netCred);
      throw new NotSupportedException(string.Format("Derivation of user name from credential object of type {0} is not supported.", cred.GetType().FullName));
    }
#if !DOTNET_V35
    private static string GetUserName(SharePointOnlineCredentials cred) {
      return cred.UserName;
    }
#endif
    private static string GetUserName(NetworkCredential cred) {
      return cred.UserName;
    }

  } // class

}
