using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Kraken.SharePoint")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Liquid Mercury Solutions / Colossus Consulting LLC")]
[assembly: AssemblyProduct("Kraken.SharePoint.SandboxSafe")]
[assembly: AssemblyCopyright("Copyright ©2003,2007,2010,2013 Colossus Consulting LLC. All Rights Reserved.")]
[assembly: AssemblyTrademark("Liquid Mercury Solutions, Kraken, and Beowulf are Trademarks of Colossus Consulting LLC")]
[assembly: AssemblyCulture("")]

/*
 * This has been removed to prevent "Attempt by security transparent method X to access security critical method Y failed"
// added to allow sandboxed code to make use of this assembly in the user code service
[assembly: AllowPartiallyTrustedCallers()]
 */

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("ec689a42-1c8e-40cd-8df7-22a015875809")]

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
[assembly: AssemblyVersion("15.0.0.0")]
//[assembly: AssemblyFileVersion("15.0.0.0")]
#else
    [assembly: AssemblyVersion("14.0.0.0")]
    //[assembly: AssemblyFileVersion("14.0.0.0")]
#endif
// Allow sandbox code to call this assembly
//[assembly: AllowPartiallyTrustedCallers()]
