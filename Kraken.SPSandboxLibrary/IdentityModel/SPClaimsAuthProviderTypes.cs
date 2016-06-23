using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.IdentityModel {

  /// <summary>
  /// Possible options for the first character in an encoded claim string
  /// This class is similar to SPOriginalIssuerType with some additional values.
  /// </summary>
  public enum SPClaimPrefixTypes {

    /// <summary>
    /// Not specified / Unitialized value
    /// </summary>
    None,

    /// <summary>
    /// There is no initial decoration
    /// </summary>
    Undecorated,

    /// <summary>
    /// Represents encoding starting with c:
    /// </summary>
    Claim,

    /// <summary>
    /// Represents encoding starting with i:
    /// </summary>
    Identity
  }

  public enum SPClaimsAuthProviderTypes {

    /// <summary>
    /// Not specified / Unitialized value
    /// </summary>
    None,

    /// <summary>
    /// Encoded value is 'w'
    /// </summary>
    SecurityTokenService,

    /// <summary>
    /// Represents windows auth pre-dating the use of encoded claims
    /// </summary>
    WindowsClassic,

    /// <summary>
    /// Encoded value is 'w'
    /// </summary>
    WindowsClaims,

    /// <summary>
    /// Encoded value is 'm'; this one is never used in SharePoint
    /// </summary>
    MembershipProvider,

    /// <summary>
    /// Encoded value is 'r'; this one is never used in SharePoint
    /// </summary>
    RoleProvider,

    /// <summary>
    /// Encoded value is 'f'
    /// </summary>
    FormsProvider,

    /// <summary>
    /// Encoded value is 't'
    /// </summary>
    TrustedProvider,

    /// <summary>
    /// Encoded value is 'c'
    /// </summary>
    ClaimsOther,

    /// <summary>
    /// Means we tried to decode it and found it was an unrecognized character
    /// </summary>
    Unknown

  } // class

} // namespace
