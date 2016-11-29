using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Kraken.Core")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Liquid Mercury Solutions / Colossus Consulting LLC")]
[assembly: AssemblyProduct("Kraken.SharePoint.SandboxSafe")]
[assembly: AssemblyCopyright("Copyright ©2003,2007,2010,2013 Colossus Consulting LLC. All Rights Reserved.")]
[assembly: AssemblyTrademark("Liquid Mercury Solutions, Kraken, and Beowulf are Trademarks of Colossus Consulting LLC")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("db831c83-b282-45e1-9e68-840106dc067d")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
#if DOTNET_V45
[assembly: AssemblyVersion("4.5.*")]
[assembly: AssemblyFileVersion("4.5.1611.0")]
#else
#if DOTNET_V4
    [assembly: AssemblyVersion("4.0.*")]
    [assembly: AssemblyFileVersion("4.0.1611.0")]
#else
    [assembly: AssemblyVersion("3.5.*")]
    [assembly: AssemblyFileVersion("3.5.1611.0")]
#endif
#endif

// added to allow sandboxed code to make use of this assembly in the user code service
// was causing issues elsewhere and thus we commented it; move the offending assembly to a new DLL
[assembly: AllowPartiallyTrustedCallers()]
[assembly: SecurityTransparent]
// done to preven issues with the json serializer
/* SecurityRules were new in .NET Framework 4.0 */
#if DOTNET_V4
[assembly: SecurityRules(System.Security.SecurityRuleSet.Level1)]
#endif