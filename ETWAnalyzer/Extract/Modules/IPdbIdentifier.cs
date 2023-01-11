//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract.Modules
{
    /// <summary>
    /// A pdb on the symbol server is identified by name, id and age.
    /// </summary>
    public interface IPdbIdentifier
    {
        /// <summary>
        /// Number of PDB recompilations since PDB was created or completely rebuilt
        /// </summary>
        int Age { get;  }

        /// <summary>
        /// GUID which should be new for every recompile of the target binary
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Pdb name without path
        /// </summary>
        string Name { get;  }
    }
}