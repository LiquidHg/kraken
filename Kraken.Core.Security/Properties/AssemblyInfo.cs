﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Kraken.Security")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Kraken.Security")]
[assembly: AssemblyCopyright("Copyright ©  2013")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Common language specification compliance
[assembly: System.CLSCompliant(true)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("029faee4-dd8b-47e5-98f8-67f571da6c10")]

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

// TODO determine if we need this and address any problems
//[assembly: SecurityCritical]
