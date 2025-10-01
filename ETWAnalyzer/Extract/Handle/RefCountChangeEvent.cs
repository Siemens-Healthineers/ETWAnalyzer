//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Common;
using Microsoft.Windows.EventTracing;
using System;

namespace ETWAnalyzer.Extract.Handle
{
    /// <summary>
    /// Handle reference count change event. 
    /// </summary>
    public interface IRefCountChangeEvent : IStackEventBase
    {
        /// <summary>
        /// Number by which the ref count is increased or decreased (value is negative)
        /// </summary>
        int RefCountChange { get; }
    }

    /// <summary>
    /// Handle reference count change event. 
    /// </summary>
    public class RefCountChangeEvent : StackEventBase, IRefCountChangeEvent
    {
        /// <summary>
        /// Number by which the ref count is increased or decreased (value is negative)
        /// </summary>
        public int RefCountChange { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="refCountChange"></param>
        /// <param name="processIdx"></param>
        /// <param name="threadId"></param>
        /// <param name="stackIdx"></param>
        public RefCountChangeEvent(Timestamp time, int refCountChange, ETWProcessIndex processIdx, uint threadId, StackIdx stackIdx)
            : base(time, processIdx, threadId, stackIdx)
        {
            RefCountChange = refCountChange;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="refCountChange"></param>
        /// <param name="process"></param>
        /// <param name="threadId"></param>
        public RefCountChangeEvent(Timestamp time, int refCountChange, ETWProcessIndex process, uint threadId)
            : this(time, refCountChange, process, threadId, StackIdx.None)
        { }

        /// <summary>
        /// 
        /// </summary>
        public RefCountChangeEvent() : this(default(Timestamp), 0, ETWProcessIndex.Invalid, 0)
        { }
    }
}
