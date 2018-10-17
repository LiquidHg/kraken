using Microsoft.SharePoint.Administration.Claims;
using Microsoft.SharePoint.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.IdentityModel {

  /// <summary>
  /// This class provides a rudimentary interface for encoding
  /// and decoding SharePoint user-claim strings, including the
  /// provider name and (in certain cases) the windows domain.
  /// 
  /// It is not intended as a comprehensive solution such as the
  /// SPClaimEncodingManager class, which can handle things such
  /// as custom claim types (with the funny accented "g" character.
  /// However, it should work in many of the typical cases.
  /// </summary>
  /// <remarks>
  /// Parts of this class are lifted directly out of the
  /// internal sealed implementation of SPClaimEncodingManager
  /// and its supporting classes. So, thanks Microsoft for
  /// filling a treasure chest full of useful stuff and then
  /// burying it in a deep hole. We're glad we found the pirate map.
  /// </remarks>
  public class SimpleClaimsDecoder {

    public SimpleClaimsDecoder(bool initialize) {
      if (initialize)
        this.Initialize(true);
    }

    /// <summary>
    /// When true, functions will try wherever possible to use
    /// indirect methods to call on SPClaimsEncodingManager
    /// since most of these methods are indirectly accessible.
    /// </summary>
    public bool UseSharePointClaimsEncodingManager { get; set; }

    public const char ClaimEncodingSeperator = '|';
    public const char WindowsNTDomainDelimiter = '\\';
    public const char WindowsUPNDelimiter = '@';
    public const int MaxEncodedClaimSize = 500;
    // not really needed since we don't do any persistence here
    //internal const string ObjectName = "SimpleClaimsDecoder";

    /// <summary>
    /// Indicates that Initialize() has been called.
    /// </summary>
    /// <remarks>
    /// Developers should set this field to true in your
    /// implementation of overridden Initialize().
    /// </remarks>
    protected bool m_Initialized;

    protected SimpleClaimsValueTypeEncoding m_ClaimValueTypeEncoding;
    protected SimpleClaimsTypeEncoding m_ClaimTypeEncoding;

    /// <summary>
    /// ClaimsType encoding dictionary
    /// </summary>
    /// <remarks>
    /// Perhaps it is not in our best interest, but left this property public
    /// and we trust the developers will not add items to the dictionary willy-nilly.
    /// </remarks>
    public SimpleClaimsTypeEncoding ClaimsTypeEncodingObject {
      get {
        return this.m_ClaimTypeEncoding;
      }
    }

    /// <summary>
    /// ClaimsValueType encoding dictionary
    /// </summary>
    /// <remarks>
    /// Perhaps it is not in our best interest, but left this property public
    /// and we trust the developers will not add items to the dictionary willy-nilly.
    /// </remarks>
    public SimpleClaimsValueTypeEncoding ClaimsValueTypeEncodingObject {
      get {
        return this.m_ClaimValueTypeEncoding;
      }
    }

    #region Static Methods

    /* not implemented here, but there is one in Beowulf.SharePoint.Claims.IdentityModel
    public static SPClaimEncodingManager Local { get; }
    */

    /// <summary>
    /// Determines how much "room" if left in a claim string
    /// for other stuff after the prefix is accounted for.
    /// </summary>
    /// <param name="claimOriginalIssuerIndex"></param>
    /// <param name="claimOriginalIssuerEncoded"></param>
    /// <returns></returns>
    public static int CalculateMaximumEncodedClaimValueSize(char claimOriginalIssuerIndex, string claimOriginalIssuerEncoded) {
      int num = 2;
      if (('w' == claimOriginalIssuerIndex) || ('s' == claimOriginalIssuerIndex))
        num = 1;
      return ((0x1ee - num) - claimOriginalIssuerEncoded.Length);
    }

    /// <summary>
    /// Returns decoded string from DecodeForFormsClaimsSafety
    /// ensuring that URL encoded characters are decoded.
    /// </summary>
    /// <param name="encodedValue"></param>
    /// <returns></returns>
    private static string DecodeClaimValue(string encodedValue) {
      return ClaimsEncodingBase.DecodeForFormsClaimsSafety(encodedValue);
    }

    /// <summary>
    /// A passthrough function to SPOriginalIssuers.Format
    /// </summary>
    /// <param name="originalIssuerIndex"></param>
    /// <param name="originalIssuerEncoded"></param>
    /// <returns></returns>
    private static string DecodeOriginalIssuer(char originalIssuerIndex, string originalIssuerEncoded) {
      return SPOriginalIssuers.Format((SPOriginalIssuerType)originalIssuerIndex, originalIssuerEncoded);
    }

    /// <summary>
    /// Returns decoded string from EncodeForFormsClaimsSafety
    /// ensuring that certain characers will be URL encoded.
    /// Throws exception if the encoded string is &gt; maxLength.
    /// </summary>
    /// <param name="claimValue">claim value to encode</param>
    /// <param name="maxLength">max allowed length of resulting encoded string</param>
    /// <returns></returns>
    private static string EncodeClaimValue(string claimValue, int maxLength) {
      string str = ClaimsEncodingBase.EncodeForFormsClaimsSafety(claimValue);
      if (str.Length > maxLength)
        throw new ArgumentException(null, "claimValue");
      return str;
    }

    /// <summary>
    /// Outputs character code and encoded string for issuer such as Windows, Forms, TrustedIssuer, or STS.
    /// </summary>
    /// <param name="originalIssuer">One of several supported login providers</param>
    /// <param name="originalIssuerIndex">The character code for originalIssuer</param>
    /// <param name="originalIssuerEncoded">An encoded version of originalIssuer</param>
    private static void EncodeOriginalIssuer(string originalIssuer, out char originalIssuerIndex, out string originalIssuerEncoded) {
      SPOriginalIssuerType issuerType = SPOriginalIssuers.GetIssuerType(originalIssuer);
      originalIssuerEncoded = string.Empty;
      switch (issuerType) {
        case SPOriginalIssuerType.SecurityTokenService:
        case SPOriginalIssuerType.Windows:
          originalIssuerEncoded = string.Empty;
          break;

        case SPOriginalIssuerType.TrustedProvider:
        case SPOriginalIssuerType.ClaimProvider:
        case SPOriginalIssuerType.Forms:
          originalIssuerEncoded = SPOriginalIssuers.GetIssuerIdentifier(originalIssuer);
          break;

        default:
          throw new ArgumentException(null, "originalIssuer");
      }
      originalIssuerIndex = (char)((ushort)issuerType);
    }

    /// <summary>
    /// Encodes a string using EncodeForFormsClaimsSafety and returns true
    /// only if the resulting string is less than or equal to maxLength.
    /// </summary>
    /// <param name="claimValue"></param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    private static bool IsClaimValueEncodable(string claimValue, int maxLength) {
      bool flag = true;
      if (ClaimsEncodingBase.EncodeForFormsClaimsSafety(claimValue).Length > maxLength) {
        flag = false;
      }
      return flag;
    }

    /// <summary>
    /// Attempts to extract the provider name from a potential claim value, which
    /// should either be 'c' or 'i'. Then tries to get everything after the ':'.
    /// If either operation fails, returns false, otherwise true.
    /// </summary>
    /// <param name="value">The value to test</param>
    /// <returns></returns>
    public static bool IsEncodedClaim(string value) {
      return IsEncodedClaim(value, true);
    }
    public static bool IsEncodedClaim(string value, bool useClaimProviderManager) {
      if (useClaimProviderManager)
        return SPClaimProviderManager.IsEncodedClaim(value);
      bool flag = true;
      if (value == null)
        throw new ArgumentNullException("value");
      // GetProviderName returns just the character(s) before the first ':'
      string providerName = SPUtility.GetProviderName(value);
      if (!string.Equals(providerName, "c", StringComparison.OrdinalIgnoreCase) && !string.Equals(providerName, "i", StringComparison.OrdinalIgnoreCase))
        return false;
      // GetAccountName returns everything after the first ':' otherwise ""
      if (string.IsNullOrEmpty(SPUtility.GetAccountName(value)))
        flag = false;
      return flag;
    }

    /// <summary>
    /// Similar to IsEncodedClaim but only checks to ensure the
    /// string begins with "i:".
    /// </summary>
    /// <param name="value">The value to test</param>
    /// <returns></returns>
    internal static bool IsEncodedClaimUser(string value) {
      bool flag = false;
      if (string.Equals(SPUtility.GetProviderName(value), "i", StringComparison.OrdinalIgnoreCase))
        flag = true;
      return flag;
    }

    /// <summary>
    /// Checks character and returns false for ',' ':' ';' '|' and control 
    /// chacters like newline and carriage return as well as a series of characters
    /// checks by SPClaim.IsClaimXmlSafeCharacter. The logic is pretty complex.
    /// </summary>
    /// <param name="character"></param>
    /// <returns></returns>
    internal static bool IsFormsEncodedClaimSafeCharacter(char character) {
      bool flag = SPClaim.IsClaimXmlSafeCharacter(character);
      return (
        (!flag 
          || (character != ':' && character != ','
            && character != ';' && character != '|'
            && !char.IsControl(character))
        )
        && flag
      );
    }

    /// <summary>
    /// A pass through function to SPOriginalIssuers.IsValidIssuer
    /// </summary>
    /// <param name="originalIssuer"></param>
    /// <returns></returns>
    private static bool IsOriginalIssuerEncodable(string originalIssuer) {
      return SPOriginalIssuers.IsValidIssuer(originalIssuer);
    }

    #endregion
    #region Refactored Methods 
    // (These were all static in SPClaimEncodingManager but that doesn't make sense in this implementation)
    // Those methods that used Local should probably not be a static method

    public void AddEncodingForClaimType(char encodingCharacter, string claimType) {
      if (claimType == null)
        throw new ArgumentNullException("claimType");
      if (string.IsNullOrEmpty(claimType))
        throw new ArgumentException("claimType");
      this.ClaimsTypeEncodingObject.AddEncodingForClaimType(encodingCharacter, claimType);
    }

    /// <summary>
    /// Crate a role claim for forms authentication. Format user/claimValue with 
    /// EncodeClaimIntoFormsSuffix, runs through SPUtility.FormatAccountName and
    /// and prefixes it with "i:"
    /// </summary>
    /// <param name="claimType"></param>
    /// <param name="claimValue"></param>
    /// <param name="claimValueType"></param>
    /// <param name="claimOriginalIssuer"></param>
    /// <returns></returns>
    public string ConvertClaimToMembership(string claimType, string claimValue, string claimValueType, string claimOriginalIssuer) {
      string str = null;
      string user = this.EncodeClaimIntoFormsSuffix(claimType, claimValue, claimValueType, claimOriginalIssuer);
      if (user != null)
        str = SPUtility.FormatAccountName("i", user);
      return str;
    }

    /// <summary>
    /// Crate a role claim for forms authentication. Format user/claimValue with 
    /// EncodeClaimIntoFormsSuffix, runs through SPUtility.FormatAccountName and
    /// and prefixes it with "c:"
    /// </summary>
    /// <param name="claimType"></param>
    /// <param name="claimValue"></param>
    /// <param name="claimValueType"></param>
    /// <param name="claimOriginalIssuer"></param>
    /// <returns></returns>
    public string ConvertClaimToRole(string claimType, string claimValue, string claimValueType, string claimOriginalIssuer) {
      string str = null;
      string user = this.EncodeClaimIntoFormsSuffix(claimType, claimValue, claimValueType, claimOriginalIssuer);
      if (user != null)
        str = SPUtility.FormatAccountName("c", user);
      return str;
    }

    /// <summary>
    /// Decode a claim value using SPUtility.GetAccountName and
    /// DecodeClaimFromFormsSuffix. First trims off the leading
    /// i: or c: (but will fail if they aren't there), then decodes.
    /// </summary>
    /// <remarks>
    /// You can use IsEncodedClaim to determine if you want to call
    /// DecodeClaim or DecodeClaimFromFormsSuffix.
    /// </remarks>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual SPClaim DecodeClaim(string value) {
      if (value == null)
        throw new ArgumentNullException("value");
      if (!SimpleClaimsDecoder.IsEncodedClaim(value, this.UseSharePointClaimsEncodingManager))
        throw new ArgumentException(null, "value");
      string accountName = SPUtility.GetAccountName(value);
      if (string.IsNullOrEmpty(accountName)) {
        throw new ArgumentNullException("accountName", string.Format("SPUtility.GetAccountName(value) for value = '{0}' returned an empty string.", value));
      }
      return this.DecodeClaimFromFormsSuffix(accountName);
    }

    // TODO there is probably some backdoor way into SPClaimEncodingManager

    /// <summary>
    /// Accepts the encoded string that is after the ":" and converts
    /// it to SPClaim object.
    /// </summary>
    /// <param name="encodedValue"></param>
    /// <returns></returns>
    public virtual SPClaim DecodeClaimFromFormsSuffix(string encodedValue) {
      if (encodedValue == null)
        throw new ArgumentNullException("encodedValue");
      if (encodedValue.Length < 6)
        throw new ArgumentException(null, "encodedValue[length]");
      if (encodedValue[4] != '|')
        throw new ArgumentException(null, "encodedValue[4]");
      string trustedProviderAndLoginName = encodedValue.Substring(5);
      if (string.IsNullOrEmpty(trustedProviderAndLoginName))
        throw new ArgumentException(null, "encodedValue[provider]");
      // so we know that we have a claim string without the i: or c: in front
      // and it has the required number of characters and then the |
      int index = trustedProviderAndLoginName.IndexOf('|');
      string originalIssuerEncoded = null;
      // now we look for the pipe the seperates the trusted provider from the login name
      string loginName = trustedProviderAndLoginName;
      char claimTypeChar = encodedValue[1];
      char claimValueTypeChar = encodedValue[2];
      char providerChar = encodedValue[3];
      if (index != -1) { // there is a pipe
        // set originalIssuerEncoded to just what appears before the |
        originalIssuerEncoded = trustedProviderAndLoginName.Substring(0, index);
        // set loginName to just what appears after the |
        loginName = trustedProviderAndLoginName.Substring(index + 1);
      } else if ((providerChar != 'w') && (providerChar != 's')) {
        // if there was no pipe then it had better be a windows name or STS token
        throw new ArgumentException(null, "encodedValue[ws]");
      }

      string type = string.Empty;
      try {
        type = this.ClaimsTypeEncodingObject.DecodeValue(claimTypeChar);
      } catch (ArgumentException) {
        // TODO log error if possible
        // 99% of the time if this fails it would be due to 'custom' claim types
        // in SharePoint that deviate from the standard defined constants...
        // You know, like your Windows user name, because in a Microsoft product, that's custom!
        type = ClaimTypes.WindowsAccountName;
        // TODO check upstrean to see if we screw a bunch of things up by doin gthis.
        // I don't see how we could though, it threw an error and crashed before.
      }
      string valueType = this.ClaimsValueTypeEncodingObject.DecodeValue(claimValueTypeChar);
      string originalIssuer = DecodeOriginalIssuer(providerChar, originalIssuerEncoded);
      return new SPClaim(type, DecodeClaimValue(loginName), valueType, originalIssuer);
    }

    public string EncodeClaim(SPClaim claim) {
      return EncodeClaim(claim, this.UseSharePointClaimsEncodingManager);
    }
    /// <summary>
    /// Encodes a claim to an SP encoded claim string
    /// </summary>
    /// <param name="claim">The claim to encode</param>
    /// <param name="useEncodingManager">
    /// If true, use claim.ToEncodedString which passes this on to SPClaimsEncodingManager
    /// otherwise uses our internal reverse engineered logic from the same class.
    /// </param>
    /// <returns></returns>
    private string EncodeClaim(SPClaim claim, bool useEncodingManager) {
      if (claim == null)
        throw new ArgumentNullException("claim");
      if (useEncodingManager)
        return claim.ToEncodedString(); //  this one will pass things to the sealed method in SPClaimsEncodingManager
      else
        return this.EncodeClaim(claim.ClaimType, claim.Value, claim.ValueType, claim.OriginalIssuer);
    }

    public string EncodeClaim(string claimType, string claimValue, string claimValueType, string claimOriginalIssuer) {
      if (claimType == null)
        throw new ArgumentNullException("claimType");
      if (SPClaimTypes.Equals(SPClaimTypes.UserIdentifier, claimType))
        throw new ArgumentException(null, "claimType");
      if (SPClaimTypes.Equals(ClaimTypes.Name, claimType))
        throw new ArgumentException(null, "claimType");
      if (claimValue == null)
        throw new ArgumentNullException("claimType");
      if (claimValueType == null)
        throw new ArgumentNullException("claimType");
      if (claimOriginalIssuer == null)
        throw new ArgumentNullException("claimType");
      if (this.IsIdentityClaim(claimType, claimOriginalIssuer))
        return this.ConvertClaimToMembership(claimType, claimValue, claimValueType, claimOriginalIssuer);
      return this.ConvertClaimToRole(claimType, claimValue, claimValueType, claimOriginalIssuer);
    }

    /// <summary>
    /// This method is a simple test that returns true for the following claim types:
    /// Upn, WindowsAccountName, UserIdentifier, UserLogonName. 
    /// </summary>
    /// <remarks>
    /// This method will work in many common scenarios, but note that this class doesn't
    /// interact with SPClaimProviderManager, so if you map something like e-mail address or a
    /// custom claim type to IdentityClaim, this logic will be broken in this base class.
    /// 
    /// Dervied classes should use SPClaimProviderManager.IsIdentityClaim where possible.
    /// Ohhh, so sorry, but that's an internal static method! Well, I guess we'll just have
    /// to reverse engineer it.
    /// </remarks>
    /// <param name="claimType"></param>
    /// <param name="claimOriginalIssuer"></param>
    /// <returns></returns>
    public virtual bool IsIdentityClaim(string claimType, string claimOriginalIssuer) {
      if (SPClaimTypes.Equals(ClaimTypes.Upn, claimType)
        || SPClaimTypes.Equals(ClaimTypes.WindowsAccountName, claimType)
        || SPClaimTypes.Equals(SPClaimTypes.UserIdentifier, claimType)
        || SPClaimTypes.Equals(SPClaimTypes.UserLogonName, claimType))
        return true;
      return false;
    }

    public string EncodeClaimIntoFormsSuffix(string claimType, string claimValue, string claimValueType, string claimOriginalIssuer) {
      if (claimType == null)
        throw new ArgumentNullException("claimType");
      if (claimValue == null)
        throw new ArgumentNullException("claimValue");
      if (claimValueType == null)
        throw new ArgumentNullException("claimValueType");
      if (claimOriginalIssuer == null)
        throw new ArgumentNullException("claimOriginalIssuer");
      int num = -1;
      int num2 = -1;

      if (-1 == (num = this.ClaimsTypeEncodingObject.EncodeValue(claimType)))
        throw new ArgumentException(null, "claimType");
      if (-1 == (num2 = this.ClaimsValueTypeEncodingObject.EncodeValue(claimValueType)))
        throw new ArgumentException(null, "claimValueType");
      int num3 = 0x30;
      char originalIssuerIndex = '0';
      string originalIssuerEncoded = null;
      EncodeOriginalIssuer(claimOriginalIssuer, out originalIssuerIndex, out originalIssuerEncoded);
      int maxLength = CalculateMaximumEncodedClaimValueSize(originalIssuerIndex, originalIssuerEncoded);
      string str3 = EncodeClaimValue(claimValue, maxLength);
      StringBuilder builder = new StringBuilder("");
      builder.Append((char)num3);
      builder.Append((char)num);
      builder.Append((char)num2);
      builder.Append(originalIssuerIndex);
      if (!string.IsNullOrEmpty(originalIssuerEncoded)) {
        builder.Append('|');
        builder.Append(originalIssuerEncoded);
      }
      builder.Append('|');
      builder.Append(str3);
      return builder.ToString().ToLowerInvariant();
    }

    public virtual IEnumerable<KeyValuePair<char, string>> GetClaimTypeEncodings() {
      return this.ClaimsTypeEncodingObject.GetEncodings();
    }

    public string GetClaimTypeForEncoding(char encodingValue) {
      return this.ClaimsTypeEncodingObject.DecodeValue(encodingValue);
    }

    public char GetEncodingForClaimType(string claimType) {
      if (claimType == null)
        throw new ArgumentNullException("claimType");
      if (string.IsNullOrEmpty(claimType))
        throw new ArgumentException("claimType");
      int num = this.ClaimsTypeEncodingObject.EncodeValue(claimType);
      if (-1 == num)
        throw new ArgumentException(null, "claimType");
      return (char)num;
    }

    public bool IsClaimEncodable(SPClaim claim) {
      if (claim == null)
        throw new ArgumentNullException("claim");
      return this.IsClaimEncodable(claim.ClaimType, claim.Value, claim.ValueType, claim.OriginalIssuer);
    }

    public bool IsClaimEncodable(string claimType, string claimValue, string claimValueType, string claimOriginalIssuer) {
      char ch;
      string str;
      if (claimType == null)
        throw new ArgumentNullException("claimType");
      if (claimValue == null)
        throw new ArgumentNullException("claimValue");
      if (claimValueType == null)
        throw new ArgumentNullException("claimValueType");
      if (claimOriginalIssuer == null)
        throw new ArgumentNullException("claimOriginalIssuer");
      if (SPClaimTypes.Equals(SPClaimTypes.UserIdentifier, claimType))
        return false;
      if (SPClaimTypes.Equals(ClaimTypes.Name, claimType))
        return false;
      if (((-1 == this.ClaimsTypeEncodingObject.EncodeValue(claimType)) || (-1 == this.ClaimsValueTypeEncodingObject.EncodeValue(claimValueType))) || !IsOriginalIssuerEncodable(claimOriginalIssuer))
        return false;
      EncodeOriginalIssuer(claimOriginalIssuer, out ch, out str);
      int maxLength = CalculateMaximumEncodedClaimValueSize(ch, str);
      return IsClaimValueEncodable(claimValue, maxLength);
    }

    #endregion

    /// <summary>
    /// Populates the dictionaries for claim types and claimvalue types.
    /// </summary>
    /// <remarks>
    /// This should be overriden in derived classes to support
    /// getting values from SPClaimProviderManager.
    /// </remarks>
    public virtual void Initialize(bool refresh) {
      m_ClaimValueTypeEncoding = new SimpleClaimsValueTypeEncoding();
      m_ClaimTypeEncoding = new SimpleClaimsTypeEncoding();
      this.m_Initialized = true;
      if (refresh)
        Refresh();
    }

    /// <summary>
    /// Rebuilds the list of claim types and claim value types.
    /// </summary>
    /// <remarks>
    /// In derived classes, this should pull claims from SPClaimProviderManager.
    /// </remarks>
    public virtual void Refresh() { // SPClaimProviderManager claimProviderManager
      //if (!this.m_Initialized)
      //  throw new InvalidOperationException("You must Initialize() before calling Refresh().");
      this.m_ClaimTypeEncoding.Refresh(); // claimProviderManager
      this.m_ClaimValueTypeEncoding.Refresh(); // claimProviderManager
      //this.Update();
    }

    /// <summary>
    /// Unlike other methods in this class, this method will
    /// extract all the detailed information that it can get about 
    /// a given claim or user login name.
    /// </summary>
    /// <param name="encodedClaimOrUserName">An encoded claim string or windows login name</param>
    /// <returns></returns>
    public virtual EncodedClaimInfo DecodeFully(string encodedClaimOrUserName, bool isCleanUserName = false) {
      if (string.IsNullOrEmpty(encodedClaimOrUserName))
        throw new ArgumentNullException("encodedClaimOrUserName");
      EncodedClaimInfo info = new EncodedClaimInfo();
      info.EncodedValue = encodedClaimOrUserName;
      SPClaim claim = null; bool checkWindowsClassic = false;

      // if there is no pipe, assume it is a windows name and not an encoded claim
      if (encodedClaimOrUserName.IndexOf(ClaimEncodingSeperator) == -1) {
        checkWindowsClassic = true;
      } else if (IsEncodedClaim(encodedClaimOrUserName, this.UseSharePointClaimsEncodingManager)) {
        // TODO is there some other SP backdoor to acces the version of this in SPClaimEncodingManager?
        claim = DecodeClaim(encodedClaimOrUserName);
        string prefix = SPUtility.GetProviderName(encodedClaimOrUserName);
        if (!string.IsNullOrEmpty(prefix))
          info.ClaimPrefixType = EncodedClaimInfo.GetClaimsPrefixType(prefix);
      } else {
        try {
          info.ClaimPrefixType = SPClaimPrefixTypes.Undecorated;
          claim = DecodeClaimFromFormsSuffix(encodedClaimOrUserName);
        } catch (ArgumentException ex) {
          info.ClaimPrefixType = SPClaimPrefixTypes.None;
          // if this wasn't a propertly formatted claim, then maybe its a classic windows account
          if (ex.ParamName == "encodedValue")
            checkWindowsClassic = true;
        }
      }

      char providerChar = '?';
      string[] parts = encodedClaimOrUserName.Split(new char[] {ClaimEncodingSeperator});
      info.EncodedClaimPrefix = default(string);
      if (claim != null) {
        info.EncodedClaimPrefix = parts[0];
        // "i:0#.w|" or "0e.t|"
        if (info.EncodedClaimPrefix.Length == 4) {
          info.ClaimPrefixType = SPClaimPrefixTypes.Undecorated;
          providerChar = encodedClaimOrUserName[3];
        } else {
          switch(info.EncodedClaimPrefix[0]) {
            case 'i':
              info.ClaimPrefixType = SPClaimPrefixTypes.Identity;
              break;
            case 'c':
              info.ClaimPrefixType = SPClaimPrefixTypes.Claim;
              break;
            default:
              info.ClaimPrefixType = SPClaimPrefixTypes.None;
              break;
          }
          providerChar = encodedClaimOrUserName[5];
        }
        info.ProviderType = EncodedClaimInfo.GetClaimsAuthType(providerChar);
        // TODO it would be nice if someplace in SharePoint API maybe we find something liek the below
        info.IssuerType = EncodedClaimInfo.GetOriginalIssuerType(providerChar); //(SPOriginalIssuerType)providerChar;
        string issuer = claim.OriginalIssuer;
        // the above will often be a qualified string, 
        // so split it and take the second half
        // this has the effect of "unqualifying" it
        int seperator = issuer.IndexOf(':');
        if (seperator != -1)
          issuer = issuer.Substring(seperator + 1);
        info.ProviderName = issuer; // parts[1];
        info.Value = claim.Value; // parts[2];
        info.ClaimType = claim.ClaimType; // this was decoded from EncodedClaimPrefix
        info.ValueType = claim.ValueType; // this was decoded from EncodedClaimPrefix
      }
      // has no pipe but has a backslash
      if (checkWindowsClassic && encodedClaimOrUserName.IndexOf(ClaimEncodingSeperator) == -1 && encodedClaimOrUserName.IndexOf(WindowsNTDomainDelimiter) != -1) {
        info.ProviderType = SPClaimsAuthProviderTypes.WindowsClassic;
        info.Value = encodedClaimOrUserName;
      }
      if (string.IsNullOrEmpty(info.ProviderName)) {
        info.ProviderName = GetLoginProviderName(info.ProviderType, providerChar);
        info.IsLoginProviderNameDecorative = true;
      }

      /*
      // This is a safety and probably is not needed
      if (info.IssuerType == SPOriginalIssuerType.Unknown && !string.IsNullOrEmpty(info.ProviderName) && !info.IsLoginProviderNameDecorative)
        info.IssuerType = SPOriginalIssuerType.TrustedProvider;
      */

      // domain username speration - for convenience
      if (info.ProviderType == SPClaimsAuthProviderTypes.WindowsClassic || 
        this.IsIdentityClaim(info.ClaimType, info.ProviderName)) {
        if (!string.IsNullOrEmpty(info.Value)) {
          if (info.Value.IndexOf(WindowsNTDomainDelimiter) != -1) {
            string[] userParts = info.Value.Split(new char[] { WindowsNTDomainDelimiter },StringSplitOptions.RemoveEmptyEntries);
            if (userParts.Length == 2) {
              info.LoginDomainName = userParts[0];
              info.UnqualifiedLoginName = userParts[1];
            }
          } else if (info.Value.IndexOf(WindowsUPNDelimiter) != -1) {
            string[] userParts = info.Value.Split(new char[] { WindowsUPNDelimiter }, StringSplitOptions.RemoveEmptyEntries);
            if (userParts.Length == 2) {
              info.UnqualifiedLoginName = userParts[0];
              info.LoginDomainName = userParts[1];
            }
          }
        }
      }
      return info;
    }

    // TODO make this globally configurable or a resource
    public const string WindowsUserProviderName = "Windows Users";
    public const string FBAMembershipProviderName = "Membership";
    public const string SecurityTokenServiceProviderName = "SharePoint STS";

    private static string GetLoginProviderName(SPClaimsAuthProviderTypes providerType, char providerChar) {
      switch (providerType) {
        case (SPClaimsAuthProviderTypes.TrustedProvider):
          return string.Empty;
        case (SPClaimsAuthProviderTypes.FormsProvider):
        case (SPClaimsAuthProviderTypes.MembershipProvider):
        case (SPClaimsAuthProviderTypes.RoleProvider):
          return FBAMembershipProviderName;
        case (SPClaimsAuthProviderTypes.WindowsClassic):
        case (SPClaimsAuthProviderTypes.WindowsClaims):
          return WindowsUserProviderName;
        case (SPClaimsAuthProviderTypes.SecurityTokenService):
          return SecurityTokenServiceProviderName;
        default:
          return string.Format("Unknown Provider '{0}'", providerChar);
      }
    }

  } // classic

} // namespace
