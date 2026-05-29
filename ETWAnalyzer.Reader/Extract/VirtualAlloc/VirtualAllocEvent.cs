//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Common;

namespace ETWAnalyzer.Extract.VirtualAlloc
{
    /// <summary>
    /// Contains data for a single VirtualAlloc or VirtualFree event.
    /// </summary>
    public class VirtualAllocEvent : IVirtualAllocEvent
    {
        /// <summary>
        /// Process index into <see cref="IETWExtract.Processes"/> list.
        /// </summary>
        public ETWProcessIndex ProcessIdx { get; set; }

        /// <summary>
        /// Thread Id which performed the VirtualAlloc/Free call.
        /// </summary>
        public uint ThreadId { get; set; }

        /// <summary>
        /// Base address of the allocated/freed memory region.
        /// </summary>
        public ulong BaseAddress { get; set; }

        /// <summary>
        /// Size of the allocated/freed memory region in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// VirtualAlloc/Free flags (Commit, Reserve, Decommit, Release, ...).
        /// </summary>
        public VirtualAllocFlags Flags { get; set; }

        /// <summary>
        /// Time in seconds since trace start when the event occurred.
        /// </summary>
        public float TimeInSecondsSinceTraceStart { get; set; }

        /// <summary>
        /// Stack index into <see cref="IVirtualAllocData.Stacks"/>. Can be <see cref="StackIdx.None"/> if no stack was captured.
        /// </summary>
        public StackIdx StackIdx { get; set; }
    }
}
