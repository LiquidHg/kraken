using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Users {

  public static class SPUserTools {

    /// <summary>
    /// Finds a user in SPWeb.SiteUsers, adds it to the collection
    /// </summary>
    /// <param name="searchText">Text to search for in the user's name, logon, and email</param>
    /// <returns>Collection of users found</returns>
    public static List<SPUser> SearchForAndResolveUser(string searchText) {
      List<SPUser> users = new List<SPUser>();
      if (SPContext.Current == null || SPContext.Current.Web == null)
        return users;
      // TODO find a more performance optimized way of doing this
      foreach (SPUser user in SPContext.Current.Web.SiteUsers) {
        if (user.Name.ToLower().Contains(searchText)
          || user.LoginName.ToLower().Contains(searchText)
          || user.Email.ToLower().Contains(searchText)) {
          bool alreadyExists = false;
          foreach (SPUser existingUser in users) {
            if (user.LoginName.Equals(existingUser.LoginName.ToString(), StringComparison.InvariantCultureIgnoreCase)) {
              alreadyExists = true;
              break;
            }
          }
          if (!alreadyExists) {
            users.Add(user);
          }
        }
      }
      return users;
    }

  }

}

