using System.Reflection;
using System.Runtime.CompilerServices;

// Information about this assembly is defined by the following attributes. 
// Change them to the values specific to your project.
[assembly: AssemblyTitle("CFAN")]
[assembly: AssemblyDescription("Cool Factorio Archive Network")]
[assembly: AssemblyConfiguration ("")]
[assembly: AssemblyCompany("https://github.com/FACTORIO-CFAN/CFAN")]
[assembly: AssemblyProduct ("")]
[assembly: AssemblyCopyright("MIT")]
[assembly: AssemblyTrademark ("")]
[assembly: AssemblyCulture ("")]
// The assembly version has the format "{Major}.{Minor}.{Build}.{Revision}".
// The form "{Major}.{Minor}.*" will automatically update the build and revision,
// and "{Major}.{Minor}.{Build}.*" will update just the revision.

// We don't use AssemblyVersions, since the build system adds them for us,
// based upon git tags and commit numbers.
//[assembly: AssemblyVersion ("0.0.1.*")]

// The following attributes are used to specify the signing key for the assembly, 
// if desired. See the Mono documentation for more information about signing.
//[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile("")]

// Tests can see our internals.
[assembly: InternalsVisibleTo("Tests")]

// NB: The CKAN build/release process appends an AssemblyInformationalVersion to this file.
