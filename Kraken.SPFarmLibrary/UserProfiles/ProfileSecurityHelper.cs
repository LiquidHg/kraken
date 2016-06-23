using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;

using Microsoft.SharePoint.Security;
using Microsoft.Office.Server.UserProfiles;

namespace Kraken.SharePoint.UserProfiles {

  [Serializable]
  [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true), SharePointPermission(SecurityAction.Demand, ObjectModel = true)]
  class ProfileSecurityExample {

    public void YourMethods() {
      try {
        PermissionSet ps = new PermissionSet(PermissionState.Unrestricted);
        ps.Assert();
        //Put your code here
      } catch (Exception) {
        //throw something
        throw;
      } finally {
        CodeAccessPermission.RevertAssert();
      }
    }

  }
}

