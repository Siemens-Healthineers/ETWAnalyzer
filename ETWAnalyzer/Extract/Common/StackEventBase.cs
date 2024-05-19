﻿//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Microsoft.Windows.EventTracing;
using Newtonsoft.Json;
using System;

namespace ETWAnalyzer.Extract.Common
{
    /// <summary>
    /// Base interface for stack based events which contain a timestamp, process, thread and stack
    /// </summary>
    public interface IStackEventBase
    {
        /// <summary>
        /// Index to process object. Use <see cref="IProcessExtract.GetProcess(ETWProcessIndex)"/> to resolve.
        /// </summary>
        ETWProcessIndex ProcessIdx { get; }

        /// <summary>
        /// Index to stack trace string. Use <see cref="IStackCollection.GetStack(StackIdx)"/> to resolve.
        /// </summary>
        StackIdx StackIdx { get; }

        /// <summary>
        /// Thread id of logging thrad.
        /// </summary>
        int ThreadId { get; }

        /// <summary>
        /// Event time
        /// </summary>
        long TimeNs { get; }

        /// <summary>
        /// Get local time as DateTimeOffset
        /// </summary>
        /// <param name="traceStart">Trace start time</param>
        /// <returns>local time</returns>
        DateTimeOffset GetTime(DateTimeOffset traceStart);

    }

    /// <summary>
    /// Base class for stack based events which contain a timestamp, process, thread and stack
    /// </summary>
    public class StackEventBase : IStackEventBase
    {
        /// <summary>
        /// Time
        /// </summary>
        public DateTimeOffset GetTime(DateTimeOffset traceStart)
        {
            return traceStart.AddTicks(TimeNs / 100);
        }

        /// <summary>
        /// Time since trace start in nanoseconds.
        /// </summary>
        public long TimeNs { get; set; }

        /// <summary>
        /// Stack Index
        /// </summary>
        public StackIdx StackIdx { get; set; }

        /// <summary>
        /// Process Index
        /// </summary>
        public ETWProcessIndex ProcessIdx { get; set; }

        /// <summary>
        /// Thread Id
        /// </summary>
        public int ThreadId { get; set; }

        /// <summary>
        /// Used by serializer to construct a valid instance
        /// </summary>
        /// <param name="time"></param>
        /// <param name="processIdx"></param>
        /// <param name="threadId"></param>
        /// <param name="stackIdx"></param>
        public StackEventBase(TraceTimestamp time, ETWProcessIndex processIdx, int threadId, StackIdx stackIdx)
        {
            if (time.HasValue)
            {
                TimeNs = time.Nanoseconds;
            }
            StackIdx = stackIdx;
            ProcessIdx = processIdx;
            ThreadId = threadId;
            StackIdx = stackIdx;
        }
    }
}
