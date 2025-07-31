//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Common;
using ETWAnalyzer.Extract.TraceLogging;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Diagnostics.Tracing.Stacks;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Events;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;

namespace ETWAnalyzer.Extractors.TraceLogging
{
    /// <summary>
    /// Extracts TraceLogging events from the ETW trace data.
    /// TraceLogging allows to write manifest free events where the event data is described by the event source itself.
    /// The manifest is emitted during trace rundown or process exit so WPA can parse the data. 
    /// It is possible that multiple processes use different versions of the same event source with wildly varying data. 
    /// https://learn.microsoft.com/en-us/windows/win32/tracelogging/trace-logging-about
    /// </summary>
    internal class TraceLoggingEventExtractor : ExtractorBase
    {
        /// <summary>
        /// Set of event field types which are lists.
        /// </summary>
        static readonly HashSet<GenericEventFieldType> myListTypes = new HashSet<GenericEventFieldType>(
         new GenericEventFieldType[]
         {
                    GenericEventFieldType.BooleanList,
                    GenericEventFieldType.ByteList,
                    GenericEventFieldType.CharList,
                    GenericEventFieldType.DateTimeList,
                    GenericEventFieldType.DoubleList,
                    GenericEventFieldType.GuidList,
                    GenericEventFieldType.Int16List,
                    GenericEventFieldType.Int32List,
                    GenericEventFieldType.Int64List,
                    GenericEventFieldType.IPAddressList,
                    GenericEventFieldType.SByteList,
                    GenericEventFieldType.SecurityIdentifierList,
                    GenericEventFieldType.SingleList,
                    GenericEventFieldType.SocketAddressList,
                    GenericEventFieldType.StringList,
                    GenericEventFieldType.StructureList,
                    GenericEventFieldType.TimeSpanList,
                    GenericEventFieldType.UInt16List,
                    GenericEventFieldType.UInt32List,
                    GenericEventFieldType.UInt64List,
         });

        /// <summary>
        /// Set of event field types which are primitive types (not lists).
        /// </summary>
        static readonly HashSet<GenericEventFieldType> myPrimitiveEventTypes = new HashSet<GenericEventFieldType>(new GenericEventFieldType[]         {
            GenericEventFieldType.Boolean,
            GenericEventFieldType.Byte,
            GenericEventFieldType.Char,
            GenericEventFieldType.DateTime,
            GenericEventFieldType.Double,
            GenericEventFieldType.Guid,
            GenericEventFieldType.Int16,
            GenericEventFieldType.Int32,
            GenericEventFieldType.Int64,
            GenericEventFieldType.IPAddress,
            GenericEventFieldType.SByte,
            GenericEventFieldType.SecurityIdentifier,
            GenericEventFieldType.Single,
            GenericEventFieldType.SocketAddress,
            GenericEventFieldType.String,
            GenericEventFieldType.TimeSpan,
            GenericEventFieldType.UInt16,
            GenericEventFieldType.UInt32,
            GenericEventFieldType.UInt64
        });

        /// <summary>
        /// Dictionary of formatters for each event field type to get locale independent round trip capable strings for primitive types. 
        /// </summary>
#pragma warning disable CA1416 // Validate platform compatibility
        internal static readonly Dictionary<GenericEventFieldType, Func<GenericEventFieldType, object, string>> Formatter =
            new Dictionary<GenericEventFieldType, Func<GenericEventFieldType, object, string>>()
        {
            { GenericEventFieldType.Boolean, (type, value) => ((bool)value).ToString(CultureInfo.InvariantCulture) },
            { GenericEventFieldType.Byte, (type, value) => ((byte)value).ToString(CultureInfo.InvariantCulture) },
            { GenericEventFieldType.Char, (type, value) => ((char)value).ToString(CultureInfo.InvariantCulture) },
            { GenericEventFieldType.DateTime, (type, value) => ((DateTime)value).ToString("o") },
            { GenericEventFieldType.Double, (type, value) => ((double)value).ToString(CultureInfo.InvariantCulture) },
            { GenericEventFieldType.Guid, (type, value) => ((Guid)value).ToString() },
            { GenericEventFieldType.Int16, (type, value) => ((short)value).ToString(CultureInfo.InvariantCulture) },
            { GenericEventFieldType.Int32, (type, value) => ((int)value).ToString(CultureInfo.InvariantCulture) },
            { GenericEventFieldType.Int64, (type, value) => ((long)value).ToString(CultureInfo.InvariantCulture) },
            { GenericEventFieldType.IPAddress, (type, value) => ((IPAddress)value).ToString() },
            { GenericEventFieldType.SByte, (type, value) => ((sbyte)value).ToString(CultureInfo.InvariantCulture) },
            { GenericEventFieldType.SecurityIdentifier, (type, value) => ((SecurityIdentifier)value).Value },
            { GenericEventFieldType.Single, (type, value) => ((float)value).ToString(CultureInfo.InvariantCulture) },
            { GenericEventFieldType.SocketAddress, (type, value) => ((SocketAddress)value).ToString() },
            { GenericEventFieldType.String, (type, value) => (string)value },
            { GenericEventFieldType.TimeSpan, (type, value) => ((TimeSpan)value).ToString() },
            { GenericEventFieldType.UInt16, (type, value) => ((ushort)value).ToString(CultureInfo.InvariantCulture) },
            { GenericEventFieldType.UInt32, (type, value) => ((uint)value).ToString(CultureInfo.InvariantCulture) },
            { GenericEventFieldType.UInt64, (type, value) => ((ulong)value).ToString(CultureInfo.InvariantCulture) }
        };

        /// <summary>
        /// Event data read from ETL file
        /// </summary>
        private IPendingResult<IGenericEventDataSource> myEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceLoggingEventExtractor"/> class.
        /// </summary>
        public TraceLoggingEventExtractor()
        {
        }

        public override void RegisterParsers(ITraceProcessor processor)
        {
            NeedsSymbols = true;
            myEvents = processor.UseGenericEvents();
        }

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            using var logger = new PerfLogger("Extract TraceLogging Events");

            StackPrinter printer = new StackPrinter(StackFormat.DllAndMethod);

            TraceLoggingEventData traceLoggingData = new();

            var byProviderNameGrouped = myEvents.Result.Events.Where(x=>x.IsTraceLogging).GroupBy(x => x.ProviderName);
            foreach(var group in byProviderNameGrouped)
            {
                string providerName = group.Key;

                if ( !traceLoggingData.EventsByProvider.TryGetValue(providerName, out TraceLoggingProvider data) )
                {
                    data = new TraceLoggingProvider
                    {
                        ProviderId = group.First().ProviderId,
                        ProviderName = providerName
                    };
                    traceLoggingData.EventsByProvider.Add(providerName, data);
                }

                foreach (IGenericEvent ev in group)
                {
                    if( data.EventDescriptors.TryGetValue(ev.Id, out TraceLoggingEventDescriptor descriptor) == false)
                    {
                        // Store event type data
                        descriptor = new TraceLoggingEventDescriptor
                        {
                            EventId = ev.Id,
                            Name = ev.TaskName,
                            // list properties are serialized as the list e.g. Strings and a second field e.g. String.Count which we do not need since we know the collection size already
                            FieldNames = ev.Fields.Where(field => myPrimitiveEventTypes.Contains(field.Type) && !IsCountField(field.Name)).Select(field => field.Name).ToList(),
                            ListNames = ev.Fields.Where(field => myListTypes.Contains(field.Type)).Select(field => field.Name).ToList(),
                        };
                        data.EventDescriptors.Add(ev.Id, descriptor);
                    }

                    IStackSnapshot stacktrace = ev.Stack;
                    StackIdx stackIdx = StackIdx.None;
                    if( stacktrace != null )
                    {
                        string stackStr = "";
                        stackStr = printer.Print(stacktrace);
                        stackIdx = traceLoggingData.Stacks.AddStack(stackStr);
                    }


                    if (ev.ProcessId == 0) // some events can have 0 as pid which would throw in GetProcessIndexByPidAtTime
                    {
                        continue;
                    }
                    ETWProcessIndex processIdx = results.GetProcessIndexByPidAtTime(ev.ProcessId, ev.Timestamp.DateTimeOffset);

                    if( processIdx == ETWProcessIndex.Invalid)
                    {
                        continue; // skip events for unknown processes
                    }

                    var logEv = new TraceLoggingEvent
                    {
                        EventId = ev.Id,
                        ThreadId = ev.ThreadId,
                        StackIdx = stackIdx,
                        ProcessIdx = processIdx,
                        TimeStamp = ev.Timestamp.DateTimeOffset,
                    };

                    foreach (var field in ev.Fields)
                    {
                        if(IsCountField(field.Name))
                        {
                            // Skip count fields, we do not need them
                            continue;
                        }   

                        if (myPrimitiveEventTypes.Contains(field.Type))  // Currently we only support basic type field fields 
                        {
                            // stringify field in culture invariant, round tripable format where possible
                            var str = Formatter[field.Type](field.Type, field.AsObject());
                            if (!String.IsNullOrEmpty(str))
                            {
                                logEv.Fields.Add(field.Name, str);
                            }
                        }
                        else if (myListTypes.Contains(field.Type))
                        {
                            List<string> list = field.Type switch
                            {
                                GenericEventFieldType.BooleanList => field.AsBooleanList?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                                GenericEventFieldType.ByteList => field.AsByteList?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                                GenericEventFieldType.CharList => field.AsCharList?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                                GenericEventFieldType.DateTimeList => field.AsDateTimeList?.Select(x => x.ToString("o")).ToList(),
                                GenericEventFieldType.DoubleList => field.AsDoubleList?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                                GenericEventFieldType.GuidList => field.AsGuidList?.Select(x => x.ToString()).ToList(),
                                GenericEventFieldType.Int16List => field.AsInt16List?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                                GenericEventFieldType.Int32List => field.AsInt32List?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                                GenericEventFieldType.Int64List => field.AsInt64List?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                                GenericEventFieldType.IPAddressList => field.AsIPAddressList?.Select(x => x.ToString()).ToList(),
                                GenericEventFieldType.SByteList => field.AsSByteList?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                                GenericEventFieldType.SecurityIdentifierList => field.AsSecurityIdentifierList?.Select(x => x.ToString()).ToList(),
                                GenericEventFieldType.SingleList => field.AsSingleList?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                                GenericEventFieldType.SocketAddressList => field.AsSocketAddressList?.Select(x => x.ToString()).ToList(),
                                GenericEventFieldType.StringList => field.AsStringList?.ToList(),
                                GenericEventFieldType.StructureList => field.AsStructureList?.Select(x => x.ToString()).ToList(),
                                GenericEventFieldType.TimeSpanList => field.AsTimeSpanList?.Select(x => x.ToString()).ToList(),
                                GenericEventFieldType.UInt16List => field.AsUInt16List?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                                GenericEventFieldType.UInt32List => field.AsUInt32List?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                                GenericEventFieldType.UInt64List => field.AsUInt64List?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                                _ => null
                            };

                            logEv.Lists.Add(field.Name, list);
                        }
                        else
                        {
                            throw new NotSupportedException(providerName + " event " + ev.TaskName + " " + ev.Id + " field " + field.Name + " has unsupported type " + field.Type); 
                        }
                    }

                    data.Events.Add(logEv);
                }

            }

            results.TraceLogging = traceLoggingData;

        }

        /// <summary>
        /// Lists in ETW get always a second count field to tell consumers how many items in a list are present.
        /// We do not need this in Json since we get already plain lists back. 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        static bool IsCountField(string fieldName)
        {
            return fieldName.EndsWith(".Count");
        }
    }
}
