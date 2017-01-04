using Microsoft.SharePoint.Administration.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.IdentityModel {

  public class SimpleClaimsTypeEncoding : ClaimsEncodingBase {

    public SimpleClaimsTypeEncoding()
      : base(true) {
    }
    internal SimpleClaimsTypeEncoding(bool initialize)
      : base(initialize) {
    }

    private static Dictionary<char, string> _basicClaimTypes = null;
    /// <summary>
    /// A list of the supported claim value type encoding characters.
    /// </summary>
    /// <remarks>
    /// This list is current as of the June 2012 CU for SP2010 and may
    /// be different in SP2013.
    /// 
    /// It is important to understand that because the MS implementation
    /// of ClaimTypeValueEncoding refers to objects in the configuration
    /// database, it is entirely possible for additional supported claim 
    /// value types to be added into the system that we can't add here
    /// because these are simply hard coded.
    /// </remarks>
    public Dictionary<char, string> BasicClaimTypes {
      get {
        if (_basicClaimTypes == null)
          _basicClaimTypes = CreateBasicClaimTypes();
        return _basicClaimTypes;
      }
    }

    protected virtual Dictionary<char, string> CreateBasicClaimTypes() {
      Dictionary<char, string> dictionary = new Dictionary<char, string>();
      dictionary.Add('!', SPClaimTypes.IdentityProvider);
      dictionary.Add('"', SPClaimTypes.UserIdentifier);
      dictionary.Add('#', SPClaimTypes.UserLogonName);
      dictionary.Add('$', SPClaimTypes.DistributionListClaimType);
      dictionary.Add('%', SPClaimTypes.FarmId);
      dictionary.Add('&', SPClaimTypesEx.ProcessIdentitySid);
      dictionary.Add('\'', SPClaimTypesEx.ProcessIdentityLogonName);
      dictionary.Add('(', SPClaimTypes.IsAuthenticated);
      dictionary.Add(')', ClaimTypes.PrimarySid);
      dictionary.Add('*', ClaimTypes.PrimaryGroupSid);
      dictionary.Add('+', ClaimTypes.GroupSid);
      dictionary.Add('-', ClaimTypes.Role);
      dictionary.Add('.', ClaimTypes.Anonymous);
      dictionary.Add('/', ClaimTypes.Authentication);
      dictionary.Add('0', ClaimTypes.AuthorizationDecision);
      dictionary.Add('1', ClaimTypes.Country);
      dictionary.Add('2', ClaimTypes.DateOfBirth);
      dictionary.Add('3', ClaimTypes.DenyOnlySid);
      dictionary.Add('4', ClaimTypes.Dns);
      dictionary.Add('5', ClaimTypes.Email);
      dictionary.Add('6', ClaimTypes.Gender);
      dictionary.Add('7', ClaimTypes.GivenName);
      dictionary.Add('8', ClaimTypes.Hash);
      dictionary.Add('9', ClaimTypes.HomePhone);
      dictionary.Add('<', ClaimTypes.Locality);
      dictionary.Add('=', ClaimTypes.MobilePhone);
      dictionary.Add('>', ClaimTypes.Name);
      dictionary.Add('?', ClaimTypes.NameIdentifier);
      dictionary.Add('@', ClaimTypes.OtherPhone);
      dictionary.Add('[', ClaimTypes.PostalCode);
      dictionary.Add('\\', ClaimTypes.PPID);
      dictionary.Add(']', ClaimTypes.Rsa);
      dictionary.Add('^', ClaimTypes.Sid);
      dictionary.Add('_', ClaimTypes.Spn);
      dictionary.Add('`', ClaimTypes.StateOrProvince);
      dictionary.Add('a', ClaimTypes.StreetAddress);
      dictionary.Add('b', ClaimTypes.Surname);
      dictionary.Add('c', ClaimTypes.System);
      dictionary.Add('d', ClaimTypes.Thumbprint);
      dictionary.Add('e', ClaimTypes.Upn);
      dictionary.Add('f', ClaimTypes.Uri);
      dictionary.Add('g', ClaimTypes.Webpage);
      dictionary.Add('h', SPClaimTypes.ProviderUserKey);
      // Added to support the first custom ID claim that is sent from SharePoint
      // usually this is either samaccountname or something similar, because
      // practically anything else you can use as an ID is defined in the list above.
      dictionary.Add('ǵ', ClaimTypes.WindowsAccountName);
      return dictionary;
    }

    public void AddEncodingForClaimType(char encodingCharacter, string claimType) {
      if (claimType == null)
        throw new ArgumentNullException("claimType");
      if (string.IsNullOrEmpty(claimType))
        throw new ArgumentException(null, "claimType");
      if (this.IsEncodingCharacterAssigned(encodingCharacter))
        throw new ArgumentException(null, "encodingCharacter");
      if (!this.IsEncodingCharacterValid(encodingCharacter))
        throw new ArgumentException(null, "encodingCharacter");
      base.AddValueToScheme(encodingCharacter, claimType);
    }

    public override void Initialize() {
      foreach (KeyValuePair<char, string> pair in BasicClaimTypes) {
        base.AddValueToScheme(pair.Key, pair.Value);
      }
      base.Initialize();
    }

    public bool IsClaimTypeRegistered(string claimType) {
      if (claimType == null)
        throw new ArgumentNullException("claimType");
      if (string.IsNullOrEmpty(claimType))
        throw new ArgumentException(null, "claimType");
      return base.IsValueInScheme(claimType);
    }

    public bool IsEncodingCharacterAssigned(char encodingCharacter) {
      return base.IsKeyInScheme(encodingCharacter);
    }

    public bool IsEncodingCharacterValid(char encodingCharacter) {
      return base.IsValidKey(encodingCharacter);
    }

    /// <summary>
    /// Not implemented in this class. Has no effect.
    /// </summary>
    /// <remarks>
    /// intended to replace public override void Refresh(SPClaimProviderManager manager)
    /// </remarks>
    public override void Refresh(bool force = false) {
      return;
    }

  } // class
} // namespace