using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Kraken.Core.Trusted")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Kraken.Core.Trusted")]
[assembly: AssemblyCopyright("Copyright ©  2013")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("1e6e8c6b-9ab9-4dbc-b8c0-cbdd02407108")]

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