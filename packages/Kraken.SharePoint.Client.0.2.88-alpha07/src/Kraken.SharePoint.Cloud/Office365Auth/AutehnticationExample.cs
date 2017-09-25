using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;

namespace Kraken.SharePoint.Cloud.Authentication {

  class AutehnticationExample {

    public void LoginToWebs(Uri uri, string userName, string password) {

      SecureString upass = new SecureString();
      foreach (char c in password) {
        upass.AppendChar(c);
      }
      upass.MakeReadOnly();

      // Set up context
      O365ClientContext occ = new O365ClientContext(uri, userName, upass);
      //helper = occ.ClaimsHelper;

      if (occ.Context == null) {
        // failure
      } else {
        // success
      }


    }

  }

}
