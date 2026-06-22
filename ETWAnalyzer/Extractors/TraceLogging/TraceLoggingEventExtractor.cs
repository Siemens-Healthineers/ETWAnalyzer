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
                    if( ev.ProcessId <= 0 )
                    {
                        // We need a valid process id to map the event to a process
                        continue;
                    }

                    if ( data.EventDescriptors.TryGetValue(ev.Id, out TraceLoggingEventDescriptor descriptor) == false)
                    {
                        // Store event type data
                        // list properties are serialized as the list e.g. Strings and a second field e.g. String.Count which we do not need since we know the collection size already
                        // Structure fields are flattened recursively with a dotted prefix e.g. StructName.FieldName
                        List<string> fieldNames = new();
                        List<string> listNames = new();
                        CollectFieldNames(ev.Fields, null, fieldNames, listNames);

                        descriptor = new TraceLoggingEventDescriptor
                        {
                            EventId = ev.Id,
                            Name = ev.TaskName,
                            FieldNames = fieldNames,
                            ListNames = listNames,
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
                    ETWProcessIndex processIdx = results.GetProcessIndexByPidAtTime(ev.ProcessId, ev.Timestamp.ConvertToTime());

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
                        TimeStamp = ev.Timestamp.ConvertToTime(),
                    };

                    AddFields(ev.Fields, null, logEv, providerName, ev.TaskName, ev.Id);

                    data.Events.Add(logEv);
                }

            }

            results.TraceLogging = traceLoggingData;

            ReleaseMemory();
        }

        /// <summary>
        /// Recursively collect the field and list names of an event including the fields of nested structures.
        /// Structure fields are flattened with a dotted prefix e.g. StructName.FieldName so they fit into the
        /// existing flat field/list model.
        /// </summary>
        /// <param name="fields">Top level event fields or nested structure fields.</param>
        /// <param name="prefix">Name prefix of the enclosing structure or null for top level fields.</param>
        /// <param name="fieldNames">Collected primitive field names.</param>
        /// <param name="listNames">Collected list field names.</param>
        internal static void CollectFieldNames(IEnumerable<IGenericEventField> fields, string prefix, List<string> fieldNames, List<string> listNames)
        {
            foreach (var field in fields)
            {
                if (IsCountField(field.Name))
                {
                    continue;
                }

                string name = prefix == null ? field.Name : prefix + "." + field.Name;

                if (myPrimitiveEventTypes.Contains(field.Type))
                {
                    fieldNames.Add(name);
                }
                else if (myListTypes.Contains(field.Type))
                {
                    listNames.Add(name);
                }
                else if (field.Type == GenericEventFieldType.Structure)
                {
                    CollectFieldNames(field.AsStructure, name, fieldNames, listNames);
                }
            }
        }

        /// <summary>
        /// Recursively add the field values of an event including the fields of nested structures.
        /// Structure fields are flattened with a dotted prefix e.g. StructName.FieldName so they fit into the
        /// existing flat field/list model.
        /// </summary>
        /// <param name="fields">Top level event fields or nested structure fields.</param>
        /// <param name="prefix">Name prefix of the enclosing structure or null for top level fields.</param>
        /// <param name="logEv">Event to add the field values to.</param>
        /// <param name="providerName">Provider name used for error messages.</param>
        /// <param name="eventName">Event name used for error messages.</param>
        /// <param name="eventId">Event id used for error messages.</param>
        internal static void AddFields(IEnumerable<IGenericEventField> fields, string prefix, TraceLoggingEvent logEv, string providerName, string eventName, int eventId)
        {
            foreach (var field in fields)
            {
                if (IsCountField(field.Name))
                {
                    // Skip count fields, we do not need them
                    continue;
                }

                string name = prefix == null ? field.Name : prefix + "." + field.Name;

                if (myPrimitiveEventTypes.Contains(field.Type))  // Currently we only support basic type field fields 
                {
                    // stringify field in culture invariant, round tripable format where possible
                    var str = Formatter[field.Type](field.Type, field.AsObject());
                    if (!String.IsNullOrEmpty(str))
                    {
                        logEv.Fields.Add(name, str);
                    }
                }
                else if (myListTypes.Contains(field.Type))
                {
                    logEv.Lists.Add(name, GetListValues(field));
                }
                else if (field.Type == GenericEventFieldType.Structure)
                {
                    // A structure is a nested set of fields. Flatten its primitive/list fields recursively with a dotted prefix.
                    AddFields(field.AsStructure, name, logEv, providerName, eventName, eventId);
                }
                else
                {
                    throw new NotSupportedException(providerName + " event " + eventName + " " + eventId + " field " + field.Name + " has unsupported type " + field.Type);
                }
            }
        }

        /// <summary>
        /// Stringify the values of a list field in a culture invariant, where possible round trip able format.
        /// Structure lists are serialized to a readable {Name=Value; ...} string per structure since the flat
        /// field/list model cannot store a variable number of nested structures otherwise.
        /// </summary>
        /// <param name="field">List field to read the values from.</param>
        /// <returns>List of stringified values or null if the field has no value.</returns>
        internal static List<string> GetListValues(IGenericEventField field)
        {
            return field.Type switch
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
                GenericEventFieldType.StructureList => field.AsStructureList?.Select(x => FormatStructure(x)).ToList(),
                GenericEventFieldType.TimeSpanList => field.AsTimeSpanList?.Select(x => x.ToString()).ToList(),
                GenericEventFieldType.UInt16List => field.AsUInt16List?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                GenericEventFieldType.UInt32List => field.AsUInt32List?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                GenericEventFieldType.UInt64List => field.AsUInt64List?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                _ => null
            };
        }

        /// <summary>
        /// Serialize a single structure (the fields of one structure instance) to a readable string e.g. {Key=xx; Value=yy}.
        /// Used for structure lists where the variable number of structures cannot be flattened into the field/list model.
        /// Nested primitives, lists and structures are rendered recursively.
        /// </summary>
        /// <param name="fields">Fields of one structure instance.</param>
        /// <returns>Readable representation of the structure.</returns>
        internal static string FormatStructure(IEnumerable<IGenericEventField> fields)
        {
            IEnumerable<string> parts = fields
                .Where(field => !IsCountField(field.Name))
                .Select(field => field.Name + "=" + FormatFieldValue(field));
            return "{" + String.Join("; ", parts) + "}";
        }

        /// <summary>
        /// Serialize the value of a single field to a readable string. Primitive types use the round trip able
        /// formatters, lists are rendered as [v1, v2, ...] and nested structures are rendered recursively.
        /// </summary>
        /// <param name="field">Field to serialize.</param>
        /// <returns>Readable representation of the field value.</returns>
        internal static string FormatFieldValue(IGenericEventField field)
        {
            if (myPrimitiveEventTypes.Contains(field.Type))
            {
                return Formatter[field.Type](field.Type, field.AsObject());
            }
            else if (myListTypes.Contains(field.Type))
            {
                return "[" + String.Join(", ", GetListValues(field) ?? new List<string>()) + "]";
            }
            else if (field.Type == GenericEventFieldType.Structure)
            {
                return FormatStructure(field.AsStructure);
            }

            return "";
        }

        private void ReleaseMemory()
        {
            myEvents = null;
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
