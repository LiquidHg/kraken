using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Kraken Security Library")]
[assembly: AssemblyDescription("")]
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

// Common language specification compliance
[assembly: System.CLSCompliant(true)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("029faee4-dd8b-47e5-98f8-67f571da6c10")]

#if DOTNET_V35
[assembly: AssemblyVersion("3.5.*")]
#elif DOTNET_V4 && !DOTNET_V45
[assembly: AssemblyVersion("4.0.*")]
#else
[assembly: AssemblyVersion("4.5.*")]
#endif

// TODO determine if we need this and address any problems
//[assembly: SecurityCritical]
