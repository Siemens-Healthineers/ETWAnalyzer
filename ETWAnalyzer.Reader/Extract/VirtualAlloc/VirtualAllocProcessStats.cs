//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace ETWAnalyzer.Extract.VirtualAlloc
{
    /// <summary>
    /// Per process VirtualAlloc statistics aggregated during extraction.
    /// </summary>
    public class VirtualAllocProcessStats : IVirtualAllocProcessStats
    {
        /// <summary>
        /// Process index into <see cref="IETWExtract.Processes"/> list.
        /// </summary>
        public ETWProcessIndex ProcessIdx { get; set; }

        /// <summary>
        /// Total number of VirtualAlloc Commit calls during trace.
        /// </summary>
        public long CommitCount { get; set; }

        /// <summary>
        /// Total committed memory in bytes during trace (sum of all Commit sizes).
        /// </summary>
        public long CommittedSizeInBytes { get; set; }

        /// <summary>
        /// Total number of VirtualFree Decommit/Release calls during trace.
        /// </summary>
        public long FreedCount { get; set; }

        /// <summary>
        /// Total freed memory in bytes during trace (sum of all Decommit/Release sizes).
        /// </summary>
        public long FreedSizeInBytes { get; set; }

        /// <summary>
        /// Maximum VirtualAlloc Commit size in bytes during trace.
        /// </summary>
        public long MaxCommitSizeInBytes { get; set; }

        /// <summary>
        /// Number of VirtualAlloc Commit calls that were not freed during the trace.
        /// </summary>
        public long NotReleasedCommitCount { get; set; }

        /// <summary>
        /// Total size in bytes of committed memory that was not freed during the trace.
        /// </summary>
        public long NotReleasedSizeInBytes { get; set; }
    }
}
