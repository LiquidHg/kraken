using System;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.SharePoint;

using Kraken.SharePoint.Logging;
using Microsoft.SharePoint.Administration;

namespace Kraken.SharePoint {

  public static class SPListExtensions {

    public static SPList GetSPListFromAbsoluteUrl(string absoluteUrl, bool failQuietly) {
      try {
        //SPSite theSite = theContext.Site;
        using (SPSite theSite = new SPSite(absoluteUrl)) {
          using (SPWeb theWeb = theSite.OpenWeb()) {
            string listUrl = absoluteUrl.Replace(theWeb.Url, "").Replace("/", "").ToLowerInvariant();
            SPList theList = (from SPList x in theWeb.Lists
                              where x.RootFolder.Url.ToLowerInvariant() == listUrl
                              select x).FirstOrDefault<SPList>();
            return theList;
          }
        }
      } catch (Exception ex) {
        if (failQuietly) {
          KrakenLoggingService.Default.Write(string.Format(
            "Leaving {0}::GetSPListFromAbsoluteUrl -> Error: Could not get SPList from URL[{1}] ], was told to fail quietly. Exception message is {2}",
            typeof(SPListExtensions).GetType().Name, absoluteUrl, ex.Message),
            TraceSeverity.Verbose, EventSeverity.Verbose);
        } else {
          KrakenLoggingService.Default.Write(string.Format(
            "Leaving {0}::GetSPListFromAbsoluteUrl -> Error: Could not get SPList from URL[{1}].",
            typeof(SPListExtensions).GetType().Name, absoluteUrl),
            TraceSeverity.Unexpected, EventSeverity.Error);
          KrakenLoggingService.Default.Write(ex);
        }
        return null;
      }
    }

  }
}
