using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

#if NET8_0_OR_GREATER
[assembly: SupportedOSPlatform("windows")]
#endif 


[assembly: AssemblyCopyright("Copyright ©  2022-2025")]
[assembly: NeutralResourcesLanguage("en")]

[assembly: InternalsVisibleTo("ETWAnalyzer")]
[assembly: InternalsVisibleTo("ETWAnalyzer_uTest")]
[assembly: InternalsVisibleTo("ETWAnalyzer_iTest")]
[assembly: InternalsVisibleTo("ProfilingDataManager")]
                                           
