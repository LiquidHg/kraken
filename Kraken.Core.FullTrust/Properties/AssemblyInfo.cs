using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Kraken DotNet Core Full Trust")]
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

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("1e6e8c6b-9ab9-4dbc-b8c0-cbdd02407108")]

#if DOTNET_V35
[assembly: AssemblyVersion("3.5.*")]
#elif DOTNET_V4 && !DOTNET_V45
[assembly: AssemblyVersion("4.0.*")]
#else
[assembly: AssemblyVersion("4.5.*")]
#endif
