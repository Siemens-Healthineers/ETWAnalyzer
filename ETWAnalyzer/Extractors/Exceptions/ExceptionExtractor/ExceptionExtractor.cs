//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Configuration;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Exceptions;
using ETWAnalyzer.Extractors.Exceptions;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Events;
using Microsoft.Windows.EventTracing.Streaming;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;

namespace ETWAnalyzer.Extractors
{
    class ExceptionExtractor : ExtractorBase, IUnparsedEventConsumer
    {
        /// <summary>
        /// Disable Exception filter to extract all exceptions
        /// </summary>
        public bool DisableExceptionFilter { get; internal set; }

        /// <summary>
        /// When no stack trace data is present this default string is used to group by stacks
        /// </summary>
        public const string NoStackString = "No Stack";

        // let exception stack trace start only after this method
        const string MarkerFrame = "IL_Throw";

        readonly ExceptionFilters myExceptionFilters;
        IPendingResult<IGenericEventDataSource> myGenericEvents;
        IPendingResult<IStackDataSource> myStackSource;

        /// <summary>
        /// Filters by a given Filter
        /// </summary>
        /// <param name="exceptionFilters"></param>
        public ExceptionExtractor(ExceptionFilters exceptionFilters)
        {
            this.myExceptionFilters = exceptionFilters;
        }

        /// <summary>
        /// Default Constructor filters by defined ExceptionFilteringRules
        /// </summary>
        public ExceptionExtractor()
        {
            this.myExceptionFilters = ExceptionFilters();
        }

        /// <summary>
        /// Reads Filterrules from ExceptionFilters.xml
        /// </summary>
        /// <returns></returns>
        internal static ExceptionFilters ExceptionFilters()
        {
            ExceptionFilters filter;

            XmlSerializer ser = new XmlSerializer(typeof(ExceptionFilters));
            using (var inFile = File.OpenRead(ConfigFiles.ExceptionFilteringRules))
            {
                 filter = (ExceptionFilters)ser.Deserialize(inFile);
            }
            return filter;
        }

        public override void RegisterParsers(ITraceProcessor processor)
        {
            NeedsSymbols = true;
            myGenericEvents = processor.UseGenericEvents();
            // as long as https://stackoverflow.com/questions/63464266/missing-stack-frames-from-clrstackwalk-event is not solved
            processor.UseUnparsedEvents(this, new Guid[] { DotNetETWConstants.DotNetRuntimeGuid });
            // we need to parse the raw events
            myStackSource = processor.UseStacks();
        }

        readonly List<StackEvent> StackEvents = new List<StackEvent>();

        class StackEvent
        {
            public Timestamp TimeStamp;
            public IReadOnlyList<Address> Stack;
        }

        /// <summary>
        /// Set to true
        /// </summary>
        bool myNeedsStack;

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            using var logger = new PerfLogger("Extract Exception");

            StackPrinter printer = new StackPrinter(StackFormat.MethodsOnly);
            ExceptionStats exStats = new ExceptionStats();
            ExceptionRowData prevRow = null;

            void AddPrevRow()
            {
                if (prevRow != null)
                {
                    if (DisableExceptionFilter || myExceptionFilters.IsRelevantException(prevRow.ProcessNameAndPid, prevRow.ExceptionType, prevRow.ExceptionMessage, prevRow.Stack) )
                    {
                        exStats.Add(results, prevRow);
                    }

                    prevRow = null;
                }
            }

            // filter for exception and stackwalk events and then group by thread id to ensure 
            // that when we have exception happening at the same time in different threads we do not by accident 
            // use the wrong stackwalk event. With grouping by thread we eliminate that entire problem
            foreach (var eventsPerThread in myGenericEvents.Result.Events.Where(x => 
                                                                                x.ProviderName == DotNetETWConstants.DotNetRuntimeProviderName && 
                                                                                (x.Id == DotNetETWConstants.ExceptionEventId || x.Id == DotNetETWConstants.ClrStackWalkEventId) )
                                                                          .GroupBy(x=>x.ThreadId))
            {
                foreach (IGenericEvent ev in eventsPerThread)
                {
                    bool bInvalidEvent = false;

                    // if we have got no stacktrace from ETW check for additional CLR Stack walk events which can be enabled for the .NET Runtime
                    if (ev.Id == DotNetETWConstants.ClrStackWalkEventId && prevRow != null && prevRow.Stack == NoStackString)
                    {
                        StackEvent stackEv = StackEvents.Find(x => x.TimeStamp == ev.Timestamp);
                        if (stackEv != default(StackEvent))
                        {
                            bool markerFound = false;
                            StringBuilder sb = new StringBuilder();
                            foreach (Address stackAdr in stackEv.Stack)
                            {
                                if (ev?.Process?.Images != null)
                                {
                                    IStackSymbol stackSymbol = null;
                                    try
                                    {
                                        stackSymbol = ev.Process.GetSymbolForAddress(stackAdr); // might fail for pdbs like msedge.dll where the pdb format is not supported by TracePrcessing V2 currently. https://github.com/microsoft/eventtracing-processing-samples/issues/12
                                    }
                                    catch(Exception)
                                    { }

                                    if (markerFound)
                                    {
                                        string method = printer.GetPrettyMethod(stackSymbol);
                                        sb.AppendLine(method);
                                    }

                                    if (stackSymbol?.FunctionName == MarkerFrame)
                                    {
                                        markerFound = true;
                                    }
                                }
                                else
                                {
                                    // Already exited process where we have no process information about images left
                                    // skip that exception
                                    bInvalidEvent = true;
                                }
                            }

                            string stackStr = sb.ToString();
                            prevRow.Stack = String.IsNullOrEmpty(stackStr) ? NoStackString : stackStr;
                        }

                        if (bInvalidEvent) // delete event data from already exited process
                        {
                            prevRow = null;
                        }
                        AddPrevRow();
                    }
                    else if (ev.Id == DotNetETWConstants.ExceptionEventId)
                    {
                        AddPrevRow();

                        IStackSnapshot snapshot = myStackSource.Result.GetStack(ev.Timestamp, ev.ThreadId);
                        string callstack = snapshot == null ? NoStackString : printer.Print(snapshot);

                        prevRow = new ExceptionRowData
                        {
                            ExceptionMessage = ev.Fields[1].AsString,
                            ExceptionType = ev.Fields[0].AsString,
                            Stack = callstack,
                            ThreadId = ev.ThreadId,
                            TimeInSec = ev.Timestamp.ConvertToTime(),
                            ProcessNameAndPid = $"{ev?.Process?.ImageName} ({ev.ProcessId})",
                        };
                    }
                }
            }

            AddPrevRow();

            results.Exceptions = exStats;

            ReleaseMemory();
        }

        private void ReleaseMemory()
        {
            // Null out all members to help GC
            myGenericEvents = null;
            myStackSource = null;
            StackEvents.Clear();
        }

        /// <summary>
        /// Copyright by Alois Kraus 2020
        /// https://stackoverflow.com/questions/63464266/missing-stack-frames-from-clrstackwalk-event/63625162#comment112605938_63625162
        /// </summary>
        /// <param name="ev"></param>
        public void Process(TraceEvent ev)
        {
            if (ev.ProviderId == DotNetETWConstants.DotNetRuntimeGuid)
            {
                if (ev.Id == DotNetETWConstants.ExceptionEventId)
                {
                    myNeedsStack = true;
                }

                // potentially every exception event is followed by a stackwalk event
                if (myNeedsStack && ev.Id == DotNetETWConstants.ClrStackWalkEventId)
                {
                    myNeedsStack = false;

                    StackEvent stackEv = new StackEvent()
                    {
                        TimeStamp = ev.Timestamp,
                    };

                    ReadOnlySpan<byte> frameData = ev.Data.Slice(8);
                    List<Address> addresses = new();
                    stackEv.Stack = addresses;

                    if (ev.Is32Bit)
                    {
                        ReadOnlySpan<int> ints  = MemoryMarshal.Cast<byte, int>(frameData);
                            
                        foreach(var intAdr in ints)
                        {
                            addresses.Add(new Address(intAdr));
                        }
                    }
                    else
                    {
                        ReadOnlySpan<long> longs = MemoryMarshal.Cast<byte, long>(frameData);
                        foreach(var longAdr in longs)
                        {
                            addresses.Add(new Address(longAdr));
                        }
                    }

                    StackEvents.Add(stackEv);
                }
            }

        }

        public void ProcessFailure(FailureInfo failureInfo)
        {
            failureInfo.ThrowAndLogParseFailure();
        }
    }
}
