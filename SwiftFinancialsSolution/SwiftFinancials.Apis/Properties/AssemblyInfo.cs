using Microsoft.Owin;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("SwiftFinancials.Apis")]
[assembly: AssemblyCulture("")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("dd914358-6174-429f-a445-82d58c61bbc1")]

[assembly: OwinStartupAttribute(typeof(SwiftFinancials.Apis.Startup))]
