//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace ETWAnalyzer.Extract.VirtualAlloc
{
    /// <summary>
    /// Per process VirtualAlloc statistics aggregated during extraction.
    /// </summary>
    public interface IVirtualAllocProcessStats
    {
        /// <summary>
        /// Process index into <see cref="IETWExtract.Processes"/> list.
        /// </summary>
        ETWProcessIndex ProcessIdx { get; }

        /// <summary>
        /// Total number of VirtualAlloc Commit calls during trace.
        /// </summary>
        long CommitCount { get; }

        /// <summary>
        /// Total committed memory in bytes during trace (sum of all Commit sizes).
        /// </summary>
        long CommittedSizeInBytes { get; }

        /// <summary>
        /// Total number of VirtualFree Decommit/Release calls during trace.
        /// </summary>
        long FreedCount { get; }

        /// <summary>
        /// Total freed memory in bytes during trace (sum of all Decommit/Release sizes).
        /// </summary>
        long FreedSizeInBytes { get; }

        /// <summary>
        /// Maximum VirtualAlloc Commit size in bytes during trace.
        /// </summary>
        long MaxCommitSizeInBytes { get; }

        /// <summary>
        /// Number of VirtualAlloc Commit calls that were not freed during the trace.
        /// </summary>
        long NotReleasedCommitCount { get; }

        /// <summary>
        /// Total size in bytes of committed memory that was not freed during the trace.
        /// </summary>
        long NotReleasedSizeInBytes { get; }
    }
}
