//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Common;
using Microsoft.Windows.EventTracing;

namespace ETWAnalyzer.Extract.Handle
{
    /// <summary>
    /// Handle duplicate event
    /// </summary>
    public interface IHandleDuplicateEvent : IStackEventBase
    {
        /// <summary>
        /// Duplicated handle value
        /// </summary>
        ulong HandleValue { get; }

        /// <summary>
        /// Source handle value
        /// </summary>
        ulong SourceHandleValue { get; }

        /// <summary>
        /// Source process from which handle was taken. To resolve use <see cref="IProcessExtract.GetProcess(ETWProcessIndex)"/>
        /// </summary>
        ETWProcessIndex SourceProcessIdx { get; }
    }

    /// <summary>
    /// Handle duplicate event
    /// </summary>
    public class HandleDuplicateEvent : StackEventBase, IHandleDuplicateEvent
    {
        /// <summary>
        /// Duplicated handle value
        /// </summary>

        public ulong HandleValue { get; set; }

        /// <summary>
        /// Source handle value
        /// </summary>

        public ulong SourceHandleValue { get; set; }

        /// <summary>
        /// Source process from which handle was taken. To resolve use <see cref="IProcessExtract.GetProcess(ETWProcessIndex)"/>
        /// </summary>

        public ETWProcessIndex SourceProcessIdx { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="handleValue"></param>
        /// <param name="sourceHandleValue"></param>
        /// <param name="processIdx"></param>
        /// <param name="sourceProcessIdx"></param>
        /// <param name="threadId"></param>
        /// <param name="stackIdx"></param>
        public HandleDuplicateEvent(TraceTimestamp time, ulong handleValue, ulong sourceHandleValue, ETWProcessIndex processIdx, ETWProcessIndex sourceProcessIdx, int threadId, StackIdx stackIdx)
            : base(time, processIdx, threadId, stackIdx)
        {
            HandleValue = handleValue;
            SourceHandleValue = sourceHandleValue;
            SourceProcessIdx = sourceProcessIdx;
        }


        /// <summary>
        /// 
        /// </summary>
        public HandleDuplicateEvent() : this(default(TraceTimestamp), 0, 0, ETWProcessIndex.Invalid, ETWProcessIndex.Invalid, 0, StackIdx.None)
        { }
    }
}
