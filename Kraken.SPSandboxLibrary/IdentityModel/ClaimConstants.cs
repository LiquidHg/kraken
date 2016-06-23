using Microsoft.SharePoint.Administration.Claims;
using Microsoft.SharePoint.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.IdentityModel {

    #region Immitation WIF Constants

    /// <summary>
    /// This class is a partial copy of WIF in order to allow
    /// decoding of SharePoint claim strings in sandbox/app
    /// and other limited access code configurations.
    /// </summary>
    /// <remarks>
    /// Please use Microsoft.IdentityModel (WIF) or System.IdentityModel
    /// wherever possible, as this class is intended for only specific 
    /// situations and may not be updated as frequently.
    /// </remarks>
    public static class ClaimValueTypes {
      public const string Base64Binary = "http://www.w3.org/2001/XMLSchema#base64Binary";
      public const string Boolean = "http://www.w3.org/2001/XMLSchema#boolean";
      public const string Date = "http://www.w3.org/2001/XMLSchema#date";
      public const string Datetime = "http://www.w3.org/2001/XMLSchema#dateTime";
      public const string DaytimeDuration = "http://www.w3.org/TR/2002/WD-xquery-operators-20020816#dayTimeDuration";
      public const string Double = "http://www.w3.org/2001/XMLSchema#double";
      public const string DsaKeyValue = "http://www.w3.org/2000/09/xmldsig#DSAKeyValue";
      public const string HexBinary = "http://www.w3.org/2001/XMLSchema#hexBinary";
      public const string Integer = "http://www.w3.org/2001/XMLSchema#integer";
      public const string KeyInfo = "http://www.w3.org/2000/09/xmldsig#KeyInfo";
      public const string Rfc822Name = "urn:oasis:names:tc:xacml:1.0:data-type:rfc822Name";
      public const string RsaKeyValue = "http://www.w3.org/2000/09/xmldsig#RSAKeyValue";
      public const string String = "http://www.w3.org/2001/XMLSchema#string";
      public const string Time = "http://www.w3.org/2001/XMLSchema#time";
      public const string X500Name = "urn:oasis:names:tc:xacml:1.0:data-type:x500Name";
      public const string YearMonthDuration = "http://www.w3.org/TR/2002/WD-xquery-operators-20020816#yearMonthDuration";
    }

    /// <summary>
    /// This class is a partial copy of WIF in order to allow
    /// decoding of SharePoint claim strings in sandbox/app
    /// and other limited access code configurations.
    /// </summary>
    /// <remarks>
    /// Please use Microsoft.IdentityModel (WIF) or System.IdentityModel
    /// wherever possible, as this class is intended for only specific 
    /// situations and may not be updated as frequently.
    /// </remarks>
    public class ClaimTypes {
      public const string Actor = "http://schemas.xmlsoap.org/ws/2009/09/identity/claims/actor";
      public const string Anonymous = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/anonymous";
      public const string Authentication = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication";
      public const string AuthenticationInstant = "http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationinstant";
      public const string AuthenticationMethod = "http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationmethod";
      public const string AuthorizationDecision = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authorizationdecision";
      public const string ClaimType2005Namespace = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims";
      public const string ClaimType2009Namespace = "http://schemas.xmlsoap.org/ws/2009/09/identity/claims";
      public const string ClaimTypeNamespace = "http://schemas.microsoft.com/ws/2008/06/identity/claims";
      public const string CookiePath = "http://schemas.microsoft.com/ws/2008/06/identity/claims/cookiepath";
      public const string Country = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/country";
      public const string DateOfBirth = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dateofbirth";
      public const string DenyOnlyPrimaryGroupSid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarygroupsid";
      public const string DenyOnlyPrimarySid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarysid";
      public const string DenyOnlySid = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/denyonlysid";
      public const string Dns = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dns";
      public const string Dsa = "http://schemas.microsoft.com/ws/2008/06/identity/claims/dsa";
      public const string Email = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
      public const string Expiration = "http://schemas.microsoft.com/ws/2008/06/identity/claims/expiration";
      public const string Expired = "http://schemas.microsoft.com/ws/2008/06/identity/claims/expired";
      public const string Gender = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/gender";
      public const string GivenName = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname";
      public const string GroupSid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid";
      public const string Hash = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/hash";
      public const string HomePhone = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/homephone";
      public const string IsPersistent = "http://schemas.microsoft.com/ws/2008/06/identity/claims/ispersistent";
      public const string Locality = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/locality";
      public const string MobilePhone = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone";
      public const string Name = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
      public const string NameIdentifier = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
      public const string OtherPhone = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/otherphone";
      public const string PostalCode = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/postalcode";
      public const string PPID = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/privatepersonalidentifier";
      public const string PrimaryGroupSid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/primarygroupsid";
      public const string PrimarySid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid";
      public const string Role = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
      public const string Rsa = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/rsa";
      public const string SerialNumber = "http://schemas.microsoft.com/ws/2008/06/identity/claims/serialnumber";
      public const string Sid = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid";
      public const string Spn = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/spn";
      public const string StateOrProvince = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/stateorprovince";
      public const string StreetAddress = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/streetaddress";
      public const string Surname = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname";
      public const string System = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/system";
      public const string Thumbprint = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/thumbprint";
      public const string Upn = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";
      public const string Uri = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/uri";
      public const string UserData = "http://schemas.microsoft.com/ws/2008/06/identity/claims/userdata";
      public const string Version = "http://schemas.microsoft.com/ws/2008/06/identity/claims/version";
      public const string Webpage = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/webpage";
      public const string WindowsAccountName = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsaccountname";
      public const string X500DistinguishedName = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/x500distinguishedname";
    }

    #endregion
    #region ClaimTypes used by SharePoint not included in SPClaimTypes

    public class SPClaimTypesEx {
      public const string ProcessIdentitySid = "http://schemas.microsoft.com/sharepoint/2009/08/claims/processidentitysid";
      public const string ProcessIdentityLogonName = "http://schemas.microsoft.com/sharepoint/2009/08/claims/processidentitylogonname";
    }

    #endregion
}
