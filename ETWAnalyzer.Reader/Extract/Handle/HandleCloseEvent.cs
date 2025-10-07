//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract.Common;

namespace ETWAnalyzer.Extract.Handle
{
    /// <summary>
    /// Handle close event 
    /// </summary>
    public interface IHandleCloseEvent : IStackEventBase
    {
        /// <summary>
        /// Handle which was closed
        /// </summary>
        ulong HandleValue { get; }
    }


    /// <summary>
    /// Handle close event 
    /// </summary>
    public class HandleCloseEvent : StackEventBase, IHandleCloseEvent
    {
        /// <summary>
        /// Handle which was closed
        /// </summary>
        public ulong HandleValue { get; set; }

        /// <summary>
        /// Needed for de/serialization 
        /// </summary>
        public HandleCloseEvent() : this(0, 0, ETWProcessIndex.Invalid, 0, StackIdx.None)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeNs"></param>
        /// <param name="handleValue"></param>
        /// <param name="processIdx"></param>
        /// <param name="threadId"></param>
        /// <param name="stackIdx"></param>
        public HandleCloseEvent(long timeNs, ulong handleValue, ETWProcessIndex processIdx, uint threadId, StackIdx stackIdx)
            : base(timeNs, processIdx, threadId, stackIdx)
        {
            HandleValue = handleValue;
        }
    }
}
