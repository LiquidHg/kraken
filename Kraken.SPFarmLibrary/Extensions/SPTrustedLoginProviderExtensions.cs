/*
  Project Kraken: SPARK for Microsoft SharePoint 2010
  Copyright (C) 2003-2011 Thomas Carpe. <http://www.ThomasCarpe.com/>
  Maintained by: <http://www.LiquidMercurySolutions.com/>

  This file is part of SPARK: SharePoint Application Resource Kit.
  SPARK projects are distributed via CodePlex: <http://www.codeplex.com/spark/>

  You may use this code for commercial purposes and derivative works, 
  provided that you maintain all copyright notices.

  SPARK is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version. You should have received a copy of
  the GNU General Public License along with SPARK.  If not, see
  <http://www.gnu.org/licenses/>.

  SPARK is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.
  
  We worked hard on all SPARK code, and we don't make any profit from
  sharing it with the world. Please do us a favor amd give us credit
  where credit is due, by leaving this notice unchanged. We all stand
  on the backs of giants. Wherever we have used someone else's code or
  blog article as the basis of our work, we have provided references
  to our source.
*/

namespace Kraken.SharePoint {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Reflection;

  using Microsoft.SharePoint.Administration;
  using Microsoft.SharePoint.Administration.Claims;

  using Kraken.SharePoint.Logging;

  public static class SPTrustedLoginProviderExtensions {

    /// <summary>
    /// Attempts to overcome a limitation in the SharePoint API which prevents
    /// us from setting ClaimProviderName back to empty string when we no longer
    /// want to use a claim provider coupled to the login provider.
    /// </summary>
    /// <param name="sts"></param>
    /// <param name="doUpadte"></param>
    /// <returns></returns>
    public static bool TryResetClaimProvider(this SPTrustedLoginProvider sts, bool doUpadte) {
      try {
        sts.SetFieldOrProperty("m_ClaimProviderName", false, string.Empty);
        sts.SetFieldOrProperty("m_ClaimProvider", false, null);
        if (doUpadte)
          sts.Update(true);
        return true;
      } catch (Exception ex) {
        KrakenLoggingService.Default.Write(ex);
      }
      return false;
    }

    public static List<string> allSTSNames;
    public static List<string> GetSTSNames(this SPSecurityTokenServiceManager stsm, bool useCachedValue) {
      if (allSTSNames != null && useCachedValue)
        return allSTSNames;
      KrakenLoggingService.Default.Write(string.Format("Entering '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenSecurity);
      try {
        var stsNames = from s in stsm.TrustedLoginProviders
                       select s.Name;
        allSTSNames = stsNames.ToList<string>();
        return allSTSNames;
      } catch (Exception ex) {
        KrakenLoggingService.Default.Write(ex);
        return null;
      } finally {
        KrakenLoggingService.Default.Write(string.Format("Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenSecurity);
      }
    }

    /// <summary>
    /// Get the first TrustedLoginProvider associated with current claim provider
    /// LIMITATION: The same claim provider (uniquely identified by its name) cannot be associated to several TrustedLoginProvider because there is no way to determine what TrustedLoginProvider is currently calling the claim provider during search and resolution
    /// </summary>
    /// <param name="claimProviderName">The internal name of the claims provider</param>
    /// <returns></returns>
    public static SPTrustedLoginProvider GetFirstAssociatedSTS(this SPSecurityTokenServiceManager stsm, string claimProviderName) {
      KrakenLoggingService.Default.Write(string.Format("Entering '{0}' providerName='{1}'.", MethodBase.GetCurrentMethod().Name, claimProviderName), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenSecurity);
      var sts = stsm.TrustedLoginProviders.Where(x => x.ClaimProviderName == claimProviderName);
      if (sts != null && sts.Count() == 1) {
        KrakenLoggingService.Default.Write(string.Format("Found STS; Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenSecurity);
        return sts.First(); // This is what we are hoping for
      }
      string message = (sts != null && sts.Count() > 1)
        ? string.Format("SPClaimProvider (claim provider with internal name == '{0}') is associated to several SPTrustedLoginProvider (aka SPTrustedIdentityTokenIssuer), which is not supported because there is no way to determine which SPTrustedLoginProvider is currently calling the claim provider during Search or Resolve.", claimProviderName)
        : string.Format("No SPTrustedLoginProvider (aka SPTrustedIdentityTokenIssuer) was found with ClaimProviderName (SPClaimProvider object with internal name) == '{0}'; Claim Provider cannot create permissions for a trust if it is not associated to it. Use PowerShell cmdlet Get-SPTrustedIdentityTokenIssuer or other means to create association.", claimProviderName);
      KrakenLoggingService.Default.Write(message, TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenSecurity);
      return null;
    }
    public static SPTrustedLoginProvider GetFirstAssociatedSTS(this SPSecurityTokenServiceManager stsm, SPClaimProvider provider) {
      string providerName = provider.Name;
      return stsm.GetFirstAssociatedSTS(providerName);
    }

    /// <summary>
    /// Get all  TrustedLoginProvider associated with current claim provider
    /// </summary>
    /// <param name="claimProviderName">The internal name of the claims provider</param>
    /// <returns></returns>
    public static List<SPTrustedLoginProvider> GetAllAssociatedSTS(this SPSecurityTokenServiceManager stsm, string claimProviderName) {
      KrakenLoggingService.Default.Write(string.Format("Entering '{0}' providerName='{1}'.", MethodBase.GetCurrentMethod().Name, claimProviderName), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenSecurity);
      var sts = stsm.TrustedLoginProviders.Where(x => x.ClaimProviderName == claimProviderName);
      if (sts != null && sts.Count() > 0) {
        KrakenLoggingService.Default.Write(string.Format("Found one or more STS; Leaving '{0}'.", MethodBase.GetCurrentMethod().Name), TraceSeverity.Verbose, EventSeverity.Verbose, LoggingCategories.KrakenSecurity);
        return sts.ToList<SPTrustedLoginProvider>(); // This is what we are hoping for
      }
      string message = string.Format("Claim provider with internal name '{0}' is not associated with any SPTrustedLoginProvider, and it cannot create permissions for a trust if it is not associated to it.\r\nUse PowerShell cmdlet Get-SPTrustedIdentityTokenIssuer to create association.", claimProviderName);
      KrakenLoggingService.Default.Write(message, TraceSeverity.Unexpected, EventSeverity.Error, LoggingCategories.KrakenSecurity);
      return null;
    }
    public static List<SPTrustedLoginProvider> GetAllAssociatedSTS(this SPSecurityTokenServiceManager stsm, SPClaimProvider provider) {
      string providerName = provider.Name;
      return stsm.GetAllAssociatedSTS(providerName);
    }

  } // class
} // namespace
