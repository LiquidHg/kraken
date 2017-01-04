using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Kraken SharePoint Sandbox Solution Library")]
[assembly: AssemblyDescription("This assembly houses code that is designed to be safe to run from SharePoint's Sandbox Code Service.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Liquid Mercury Solutions")]
[assembly: AssemblyProduct("Kraken SharePoint Library")]
[assembly: AssemblyCopyright("Copyright ©2007-2016 Thomas Carpe and Liquid Mercury Solutions LLC; All rights reserved. If you need/want rights not granted under LGPL, please contact the copyright holders.")]
[assembly: AssemblyTrademark("Kraken is a trademark of Liquid Mercury Solutions - established Oct. 2009")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("16663629-810d-4964-8b06-7fc78318d2eb")]

#if DOTNET_V35
[assembly: AssemblyVersion("14.0.0.0")]
[assembly: AssemblyFileVersion("14.2.1701.0201")]
#else
[assembly: AssemblyVersion("15.0.0.0")]
[assembly: AssemblyFileVersion("15.2.1701.0201")]
#endif

// Allow sandbox code to call this assembly
[assembly: AllowPartiallyTrustedCallers()]
