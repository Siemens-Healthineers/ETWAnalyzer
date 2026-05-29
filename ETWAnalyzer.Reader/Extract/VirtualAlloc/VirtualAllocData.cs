//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Common;
using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;

namespace ETWAnalyzer.Extract.VirtualAlloc
{
    /// <summary>
    /// Contains VirtualAlloc/Free data with per process events and stack traces stored in an external file.
    /// </summary>
    public class VirtualAllocData : IVirtualAllocData
    {
        /// <summary>
        /// Per process VirtualAlloc/Free events that were not freed during trace lifetime.
        /// </summary>
        public List<VirtualAllocEvent> VirtualAllocEvents { get; set; } = new();

        /// <summary>
        /// Per process aggregated VirtualAlloc statistics.
        /// </summary>
        public List<VirtualAllocProcessStats> PerProcessStats { get; set; } = new();

        /// <summary>
        /// Stacks for VirtualAlloc/Free events.
        /// </summary>
        public StackCollection Stacks { get; set; } = new();

        /// <summary>
        /// Needed to deserialize dependent stack collection file
        /// </summary>
        internal string DeserializedFileName { get; set; }

        IReadOnlyList<IVirtualAllocEvent> IVirtualAllocData.VirtualAllocEvents => VirtualAllocEvents;

        IReadOnlyList<IVirtualAllocProcessStats> IVirtualAllocData.PerProcessStats => PerProcessStats;

        IStackCollection IVirtualAllocData.Stacks => myStackReader.Value;

        readonly Lazy<StackCollection> myStackReader;

        /// <summary>
        /// Default ctor
        /// </summary>
        public VirtualAllocData()
        {
            myStackReader = new Lazy<StackCollection>(ReadVirtualAllocStacksFromExternalFile);
        }

        StackCollection ReadVirtualAllocStacksFromExternalFile()
        {
            StackCollection lret = Stacks;
            if (DeserializedFileName != null)
            {
                ExtractSerializer ser = new(DeserializedFileName);
                lret = ser.Deserialize<StackCollection>(ExtractSerializer.VirtualAllocStackPostFix);
            }

            return lret;
        }
    }
}
