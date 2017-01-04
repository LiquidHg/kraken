using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Kraken SharePoint Farm Solution Library")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Liquid Mercury Solutions")]
[assembly: AssemblyProduct("Kraken SharePoint Library")]
[assembly: AssemblyCopyright("Copyright ©2007-2016 Thomas Carpe and Liquid Mercury Solutions LLC; All rights reserved. If you need/want rights not granted under LGPL, please contact the copyright holders.")]
[assembly: AssemblyTrademark("Kraken is a trademark of Liquid Mercury Solutions - established Oct. 2009")]
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

#if DOTNET_V35
[assembly: AssemblyVersion("14.0.0.0")]
[assembly: AssemblyFileVersion("14.2.1701.0201")]
#else
[assembly: AssemblyVersion("15.0.0.0")]
[assembly: AssemblyFileVersion("15.2.1701.0201")]
#endif
// Allow sandbox code to call this assembly
//[assembly: AllowPartiallyTrustedCallers()]
