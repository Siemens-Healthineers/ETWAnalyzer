//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System.Collections.Generic;
using System.IO;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// System wide memory statistics
    /// </summary>
    public class MemoryStats
    {
        /// <summary>
        /// Machine wide committed memory in MiB at Trace Start
        /// </summary>
        public ulong MachineCommitStartMiB
        {
            get;
        }

        /// <summary>
        /// Machine wide committed memory in MiB at Trace End
        /// </summary>
        public ulong MachineCommitEndMiB
        {
            get;
        }

        /// <summary>
        /// MachineCommitEndMiB-MachineCommitStartMiB Calculated property to make it easier to analyze with text processing tools a bunch of Json files without the need to extract and calculate the diff manually
        /// </summary>
        public long MachineCommitDiffMiB
        {
            get;
        }

        /// <summary>
        /// Machine wide active memory in MiB at Trace Start
        /// </summary>
        public ulong MachineActiveStartMiB
        {
            get;
        }

        /// <summary>
        /// Machine wide active memory in MiB at Trace End
        /// </summary>
        public ulong MachineActiveEndMiB
        {
            get;
        }

        /// <summary>
        /// MachineActiveEndMiB-MachineActiveStartMiB Calculated property to make it easier to analyze with text processing tools a bunch of Json files without the need to extract and calculate the diff manually
        /// </summary>
        public long MachineActiveDiffMiB
        {
            get;
        }

        /// <summary>
        /// Collection of working sets at ETW trace start
        /// </summary>
        public IReadOnlyList<ProcessWorkingSet> WorkingSetsAtStart
        {
            get;set;
        }

        /// <summary>
        /// Collection of working sets at ETW Trace end.
        /// </summary>
        public IReadOnlyList<ProcessWorkingSet> WorkingSetsAtEnd
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="machineCommitStartMiB"></param>
        /// <param name="machineCommitEndMiB"></param>
        /// <param name="machineActiveStartMiB"></param>
        /// <param name="machineActiveEndMiB"></param>
        public MemoryStats(decimal machineCommitStartMiB, decimal machineCommitEndMiB, decimal machineActiveStartMiB, decimal machineActiveEndMiB)
        {
            MachineActiveEndMiB = (ulong) machineActiveEndMiB;
            MachineActiveStartMiB = (ulong) machineActiveStartMiB;
            MachineCommitEndMiB = (ulong) machineCommitEndMiB;
            MachineCommitStartMiB = (ulong) machineCommitStartMiB;
            MachineCommitDiffMiB = (long) MachineCommitEndMiB - (long) MachineCommitStartMiB;
            MachineActiveDiffMiB = (long) MachineActiveEndMiB - (long) MachineActiveStartMiB;
        }
    }


}
