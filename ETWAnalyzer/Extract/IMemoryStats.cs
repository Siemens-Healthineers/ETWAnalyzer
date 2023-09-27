//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System.Collections.Generic;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// System wide memory statistics
    /// </summary>
    public interface IMemoryStats
    {
        /// <summary>
        /// MachineActiveEndMiB-MachineActiveStartMiB Calculated property to make it easier to analyze with text processing tools a bunch of Json files without the need to extract and calculate the diff manually
        /// </summary>
        long MachineActiveDiffMiB { get; }

        /// <summary>
        /// Machine wide active memory in MiB at Trace End
        /// </summary>
        ulong MachineActiveEndMiB { get; }

        /// <summary>
        /// Machine wide active memory in MiB at Trace Start
        /// </summary>
        ulong MachineActiveStartMiB { get; }

        /// <summary>
        /// MachineCommitEndMiB-MachineCommitStartMiB Calculated property to make it easier to analyze with text processing tools a bunch of Json files without the need to extract and calculate the diff manually
        /// </summary>
        long MachineCommitDiffMiB { get; }

        /// <summary>
        /// Machine wide committed memory in MiB at Trace End
        /// </summary>
        ulong MachineCommitEndMiB { get; }

        /// <summary>
        /// Machine wide committed memory in MiB at Trace Start
        /// </summary>
        ulong MachineCommitStartMiB { get; }

        /// <summary>
        /// Collection of working sets at ETW Trace end.
        /// </summary>
        IReadOnlyList<IProcessWorkingSet> WorkingSetsAtEnd { get; }

        /// <summary>
        /// Collection of working sets at ETW trace start
        /// </summary>
        IReadOnlyList<IProcessWorkingSet> WorkingSetsAtStart { get; }
    }
}