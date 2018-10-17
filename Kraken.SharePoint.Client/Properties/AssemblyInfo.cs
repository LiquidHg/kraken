using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if DEBUG
[assembly: AssemblyTitle("Kraken SharePoint Client (Debug Build)")]
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyTitle("Kraken SharePoint Client")]
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyInformationalVersion("0.1")]
[assembly: AssemblyDescription("Kraken is a set of open source libraries for SharePoint development. Kraken code is battle tested. It's been around since long ago; we started calling it Kraken in 2010 and older versions were known as Behemoth (SP2007) a.k.a. SPARK. The library includes code for full trust, sandbox solutions, client applications (CSOM), and most recently provider hosted apps.")]
[assembly: AssemblyCompany("Liquid Mercury Solutions")]
[assembly: AssemblyProduct("Kraken Tools for SharePoint Developers")]
[assembly: AssemblyCopyright("Copyright ©2007-2016 Thomas Carpe and Liquid Mercury Solutions LLC. All Rights Reserved.")]
[assembly: AssemblyTrademark("The terms 'Kraken' and 'Liquid Mercury' are trademarks of Liquid Mercury Solutions; first used in business in 2009. If you work for Octopus Deploy, let's be friends and share nicely!")]
//[assembly: AssemblyCulture("en-US")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d12fb4f4-ac1b-466a-b4a0-22ab719750a0")]

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
  [assembly: AssemblyVersion("15.0.*")]
  //[assembly: AssemblyFileVersion("15.0.1611.0")]
#else
  [assembly: AssemblyVersion("14.0.*")]
  //[assembly: AssemblyFileVersion("14.0.1611.0")]
#endif