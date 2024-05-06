using ETWAnalyzer.Extract.Common;
using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public HandleCloseEvent() : this(default(TraceTimestamp), 0, ETWProcessIndex.Invalid, 0, StackIdx.None)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="handleValue"></param>
        /// <param name="processIdx"></param>
        /// <param name="threadId"></param>
        /// <param name="stackIdx"></param>
        public HandleCloseEvent(TraceTimestamp time, ulong handleValue, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
            : base(time, processIdx, threadId, stackIdx)
        {
            HandleValue = handleValue;
        }
    }
}
