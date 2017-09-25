using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

// example of how to target .NET framework 3.5 vs 4.0
#if DOTNET_V4
#else
#endif

namespace Kraken.Security.ActiveDirectory {

  public class ADUserInfo {

    /// <summary>
    /// domain name of the user
    /// </summary>
    private string domainName;

    /// <summary>
    /// domain user name
    /// </summary>
    private string userName;

    /*
    /// <summary>
    /// domain active directory
    /// </summary>
    private string activeDirectoryLDAP;
     */

    /// <summary>
    /// Initializes a new instance of the ADUserInfo class
    /// </summary>
    public ADUserInfo() {
    }

    /// <summary>
    /// Initializes a new instance of the ADUserInfo class
    /// </summary>
    /// <param name="userName">name of the logged in user</param>
    public ADUserInfo(string userName, string domain = "") {
      if (userName.IndexOf('\\') > 0) {
        this.domainName = userName.Substring(0, userName.IndexOf('\\'));
        this.userName = userName.Substring(userName.IndexOf('\\') + 1, userName.Length - (this.domainName.Length + 1));
      } else {
        if (string.IsNullOrEmpty(domain))
          throw new Exception("If creating ADUserInfo without specifying a domain in the user's name (e.g. 'DOMAIN\\user') you must specify a domain. In some cases, you can simply pass ActiveDirectorySTSConfiguration.DefaultDomain into this method.");
        //ConfigInformation.DefaultDomain;
        this.domainName = domain;
        this.userName = userName;
      }
      //this.activeDirectoryLDAP = "LDAP://" + this.domainName;
    }

    /// <summary>
    /// Gets the domain user name
    /// </summary>
    public string UserName {
      get { return this.userName; }
    }

    /// <summary>
    /// Gets the domain name
    /// </summary>
    public string Domain {
      get { return this.domainName; }
    }

    /*
    /// <summary>
    /// Gets the domain active directory
    /// </summary>
    public string ActiveDirectoryLDAP {
      get { return this.activeDirectoryLDAP; }
    }
     */

  }
}
