using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.IdentityModel {

  public class SimpleClaimsValueTypeEncoding : ClaimsEncodingBase {

    public SimpleClaimsValueTypeEncoding()
      : base(true) {
    }
    internal SimpleClaimsValueTypeEncoding(bool initialize)
      : base(initialize) {
    }

    public override void Initialize() {
      foreach (KeyValuePair<char, string> pair in BasicClaimValueTypes) {
        base.AddValueToScheme(pair.Key, pair.Value);
      }
      base.Initialize();
    }

    private static Dictionary<char, string> _basicClaimValueTypes = null;
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
    public Dictionary<char, string> BasicClaimValueTypes {
      get {
        if (_basicClaimValueTypes == null)
          _basicClaimValueTypes = CreateBasicClaimValueTypes();
        return _basicClaimValueTypes;
      }
    }

    protected virtual Dictionary<char, string> CreateBasicClaimValueTypes() {
      // using direct strings rather than constants because where we are in
      // the SharePoint API (sandbox safe code only) we just can't assume that
      // we have access to stuff like WIF. :-(
      Dictionary<char, string> dictionary = new Dictionary<char, string>();
      dictionary.Add('!', ClaimValueTypes.Base64Binary);
      dictionary.Add('"', ClaimValueTypes.Boolean);
      dictionary.Add('#', ClaimValueTypes.Date);
      dictionary.Add('$', ClaimValueTypes.Datetime);
      dictionary.Add('%', ClaimValueTypes.DaytimeDuration);
      dictionary.Add('&', ClaimValueTypes.Double);
      dictionary.Add('\'', ClaimValueTypes.DsaKeyValue);
      dictionary.Add('(', ClaimValueTypes.HexBinary);
      dictionary.Add(')', ClaimValueTypes.Integer);
      dictionary.Add('*', ClaimValueTypes.KeyInfo);
      dictionary.Add('+', ClaimValueTypes.Rfc822Name);
      dictionary.Add('-', ClaimValueTypes.RsaKeyValue);
      dictionary.Add('.', ClaimValueTypes.String);
      dictionary.Add('/', ClaimValueTypes.Time);
      dictionary.Add('0', ClaimValueTypes.X500Name);
      dictionary.Add('1', ClaimValueTypes.YearMonthDuration);
      return dictionary;
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

  }
}
 
