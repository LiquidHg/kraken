using Microsoft.SharePoint.Administration.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.IdentityModel {

  /// <summary>
  /// This class provides a mechanism to break apart
  /// the component peices of an encoded claim string
  /// so they can be analyzed and acted upon.
  /// </summary>
  public class EncodedClaimInfo {

    public EncodedClaimInfo() {
      ProviderType = SPClaimsAuthProviderTypes.None;
      IssuerType = SPOriginalIssuerType.Unknown;
      ClaimPrefixType = SPClaimPrefixTypes.None;
      IsLoginProviderNameDecorative = false;
    }

    /// <summary>
    /// Authentication provider type
    /// </summary>
    public SPClaimsAuthProviderTypes ProviderType { get; internal set; }
    
    // TODO is it possible that the namespace for this is not allowed in many cases
    /// <summary>
    /// The original issuer type
    /// </summary>
    /// <remarks>
    /// This set method left as public so we can override its value elsewhere
    /// </remarks>
    public SPOriginalIssuerType IssuerType { get; set; }

    /// <summary>
    /// This is the encoded portion of the claim string before the first pipe character
    /// </summary>
    public string EncodedClaimPrefix { get; internal set; }

    /// <summary>
    /// If the value represents a decoded claim, this is the claim type
    /// </summary>
    public string ClaimType { get; internal set; }

    public string ValueType { get; internal set; }

    /// <summary>
    /// This is the original encoded (non-decoded) claim value
    /// </summary>
    public string EncodedValue { get; internal set; }

    /// <summary>
    /// The STS login provider name or a standard provider such as Windows or Forms
    /// </summary>
    /// <remarks>
    /// Left this proprerty set method as public so we can override it later with display name
    /// </remarks>
    public string ProviderName { get; set; }

    /// <summary>
    /// True if LoginProviderName is there just for show.
    /// If false, LoginProviderName represents a trusted issuer.
    /// </summary>
    public bool IsLoginProviderNameDecorative { get; set; } 

    /// <summary>
    /// Represents the user's domain qualified login name or other claim value
    /// </summary>
    public string Value { get; internal set; }

    /// <summary>
    /// Represents the portion of the login name that is the user's
    /// name without the domain, similar to SAMAccountName.
    /// </summary>
    public string UnqualifiedLoginName { get; internal set; }

    /// <summary>
    /// Represents the portion of the login name that is the user's
    /// windows domain or UPN suffix.
    /// </summary>
    public string LoginDomainName { get; internal set; }

    /// <summary>
    /// Additional data as needed
    /// </summary>
    public string OtherData { get; set; }

    /// <summary>
    /// Based on the prefix i: c: etc
    /// </summary>
    public SPClaimPrefixTypes ClaimPrefixType { get; internal set; }

    public static SPClaimsAuthProviderTypes GetClaimsAuthType(char providerCode) {
      switch (providerCode) {
        case 'f':
          return SPClaimsAuthProviderTypes.FormsProvider;
        case 'm':
          return SPClaimsAuthProviderTypes.MembershipProvider;
        case 'r':
          return SPClaimsAuthProviderTypes.RoleProvider;
        case 's':
          return SPClaimsAuthProviderTypes.SecurityTokenService;
        case 't':
          return SPClaimsAuthProviderTypes.TrustedProvider;
        case 'w':
          return SPClaimsAuthProviderTypes.WindowsClaims;
        case 'c':
          return SPClaimsAuthProviderTypes.ClaimsOther;
        default:
          return SPClaimsAuthProviderTypes.Unknown;
      }
    }
    public static SPOriginalIssuerType GetOriginalIssuerType(char providerCode) {
      switch (providerCode) {
        case 'f':
        case 'm':
        case 'r':
          return SPOriginalIssuerType.Forms;
        case 's':
          return SPOriginalIssuerType.SecurityTokenService;
        case 't':
          return SPOriginalIssuerType.TrustedProvider;
        case 'w':
          return SPOriginalIssuerType.Windows;
        case 'c':
          return SPOriginalIssuerType.ClaimProvider;
        default:
          return SPOriginalIssuerType.Unknown;
      }
    }

    public static SPClaimPrefixTypes GetClaimsPrefixType(string prefix) {
      if (string.IsNullOrEmpty(prefix))
        return SPClaimPrefixTypes.None;
      switch (prefix[0]) {
        case 'c':
          return SPClaimPrefixTypes.Claim;
        case 'i':
          return SPClaimPrefixTypes.Identity;
        default:
          return SPClaimPrefixTypes.None;
      }
    }

  }

}
