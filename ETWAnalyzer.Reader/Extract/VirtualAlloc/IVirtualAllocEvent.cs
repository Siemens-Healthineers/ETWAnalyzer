//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Common;

namespace ETWAnalyzer.Extract.VirtualAlloc
{
    /// <summary>
    /// Contains data for a single VirtualAlloc or VirtualFree event.
    /// </summary>
    public interface IVirtualAllocEvent
    {
        /// <summary>
        /// Process index into <see cref="IETWExtract.Processes"/> list.
        /// </summary>
        ETWProcessIndex ProcessIdx { get; }

        /// <summary>
        /// Thread Id which performed the VirtualAlloc/Free call.
        /// </summary>
        uint ThreadId { get; }

        /// <summary>
        /// Base address of the allocated/freed memory region.
        /// </summary>
        ulong BaseAddress { get; }

        /// <summary>
        /// Size of the allocated/freed memory region in bytes.
        /// </summary>
        long Size { get; }

        /// <summary>
        /// VirtualAlloc/Free flags (Commit, Reserve, Decommit, Release, ...).
        /// </summary>
        VirtualAllocFlags Flags { get; }

        /// <summary>
        /// Time in seconds since trace start when the event occurred.
        /// </summary>
        float TimeInSecondsSinceTraceStart { get; }

        /// <summary>
        /// Stack index into <see cref="IVirtualAllocData.Stacks"/>. Can be <see cref="StackIdx.None"/> if no stack was captured.
        /// </summary>
        StackIdx StackIdx { get; }
    }
}
