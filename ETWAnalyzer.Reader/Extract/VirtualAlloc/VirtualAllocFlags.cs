//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract.VirtualAlloc
{
    /// <summary>
    /// VirtualAlloc/Free flags as defined by the Windows API.
    /// See https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualalloc
    /// </summary>
    [Flags]
    public enum VirtualAllocFlags : uint
    {
        /// <summary>
        /// No flags set
        /// </summary>
        None = 0,

        /// <summary>
        /// Commit memory
        /// </summary>
        Commit = 0x1000,

        /// <summary>
        /// Reserve memory
        /// </summary>
        Reserve = 0x2000,

        /// <summary>
        /// Decommit memory
        /// </summary>
        Decommit = 0x4000,

        /// <summary>
        /// Release memory
        /// </summary>
        Release = 0x8000,

        /// <summary>
        /// Reset memory
        /// </summary>
        Reset = 0x80000,

        /// <summary>
        /// Allocate at highest possible address
        /// </summary>
        TopDown = 0x100000,

        /// <summary>
        /// Enable write watch
        /// </summary>
        WriteWatch = 0x200000,

        /// <summary>
        /// Physical memory
        /// </summary>
        Physical = 0x400000,

        /// <summary>
        /// Undo a previous reset
        /// </summary>
        ResetUndo = 0x1000000,

        /// <summary>
        /// Use large pages
        /// </summary>
        LargePages = 0x20000000
    }
}
