//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Common;

namespace ETWAnalyzer.Extract.Handle
{
    /// <summary>
    /// Handle create event
    /// </summary>
    public interface IHandleCreateEvent : IStackEventBase
    {
        /// <summary>
        /// Handle value returned by Create... call
        /// </summary>
        ulong HandleValue { get; }
    }

    /// <summary>
    /// Handle create event
    /// </summary>
    public class HandleCreateEvent : StackEventBase, IHandleCreateEvent
    {
        /// <summary>
        /// Handle value returned by Create... call
        /// </summary>
        public ulong HandleValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeNs"></param>
        /// <param name="handleValue"></param>
        /// <param name="processIdx"></param>
        /// <param name="threadId"></param>
        /// <param name="stackIdx"></param>
        public HandleCreateEvent(long timeNs, ulong handleValue, ETWProcessIndex processIdx, uint threadId, StackIdx stackIdx)
            : base(timeNs, processIdx, threadId, stackIdx)
        {
            HandleValue = handleValue;
        }

        /// <summary>
        /// 
        /// </summary>
        public HandleCreateEvent() : this(0, 0, ETWProcessIndex.Invalid, 0, StackIdx.None)
        { }

    }
}
