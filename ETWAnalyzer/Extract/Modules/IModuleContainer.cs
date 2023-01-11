//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Infrastructure;
using System;
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
    }
}