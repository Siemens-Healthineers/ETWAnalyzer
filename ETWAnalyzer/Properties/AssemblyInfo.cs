//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyCopyright("Copyright ©  2022-2026")]
[assembly: NeutralResourcesLanguage("en")]

#if NET8_0_OR_GREATER
[assembly: SupportedOSPlatform("windows")]
#endif

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("b163151c-42dc-44da-a153-fc833ee4c9e2")]
[assembly: InternalsVisibleTo("ETWAnalyzer_uTest")]
[assembly: InternalsVisibleTo("ETWAnalyzer_iTest")]
[assembly: InternalsVisibleTo("LogTester")]
[assembly: InternalsVisibleTo("ETWAnalyzer.McpServer")]
[assembly: InternalsVisibleTo("ProfilingDataManager")]
