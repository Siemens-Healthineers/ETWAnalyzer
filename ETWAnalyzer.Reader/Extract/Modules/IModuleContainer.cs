//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System.Collections.Generic;

namespace ETWAnalyzer.Extract.Modules
{
    /// <summary>
    /// Contains information about loaded modules from all processes.
    /// </summary>
    public interface IModuleContainer
    {
        /// <summary>
        /// List of loaded modules which contain also the processes which have them loaded. 
        /// </summary>
        IReadOnlyList<ModuleDefinition> Modules { get; }

        /// <summary>
        /// List of not resolved pdbs during extraction
        /// </summary>
        IReadOnlyList<IPdbIdentifier> UnresolvedPdbs { get; }

        /// <summary>
        /// Find Module Definition for a loaded module in a process.
        /// </summary>
        /// <param name="moduleName">Module name</param>
        /// <param name="process">Process in which the module is loaded</param>
        /// <returns>ModuleDefinition when module could be located or null when it could not be found.</returns>
        ModuleDefinition FindModule(string moduleName, ETWProcess process);
    }
}