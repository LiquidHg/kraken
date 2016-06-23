using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Cloud.Authentication {

  public enum SharePointAuthenticationType {
    None,
    CurrentWindowsUser,
    SpecifyWindowsUser,
    FormsBasedLogin,
    Office365Login
  }

}
