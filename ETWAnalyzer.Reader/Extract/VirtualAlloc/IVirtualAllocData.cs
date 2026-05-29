//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Common;
using System.Collections.Generic;

namespace ETWAnalyzer.Extract.VirtualAlloc
{
    /// <summary>
    /// Contains per process VirtualAlloc/Free data with stack traces stored in an external file.
    /// Only allocations that were not freed during the trace are stored with stack traces.
    /// </summary>
    public interface IVirtualAllocData
    {
        /// <summary>
        /// VirtualAlloc Commit events that were not freed (Decommit/Release) during trace lifetime. These are potential memory leaks.
        /// </summary>
        IReadOnlyList<IVirtualAllocEvent> VirtualAllocEvents { get; }

        /// <summary>
        /// Per process aggregated VirtualAlloc statistics (total allocated/freed sizes, counts, and leaked metrics).
        /// </summary>
        IReadOnlyList<IVirtualAllocProcessStats> PerProcessStats { get; }

        /// <summary>
        /// Stacks used by <see cref="VirtualAllocEvents"/> which are referenced by StackIdx values
        /// </summary>
        IStackCollection Stacks { get; }
    }
}
