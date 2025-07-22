//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Extract.TraceLogging
{

    /// <summary>
    /// Extracted data from manifest free (TraceLogging) events. Contains event values, process and stacks 
    /// </summary>
    public class TraceLoggingEventData : ITraceLoggingData
    {
        /// <summary>
        /// Dictionary of TraceLogging providers, indexed by provider name.
        /// </summary>
        public Dictionary<string, TraceLoggingProvider> EventsByProvider { get; set; } = new Dictionary<string, TraceLoggingProvider>();

        IReadOnlyDictionary<string, ITraceLoggingProvider> myEventsByProviderItf = null;
        IReadOnlyDictionary<string, ITraceLoggingProvider> ITraceLoggingData.EventsByProvider
        {
            get
            {
                myEventsByProviderItf ??= EventsByProvider.ToDictionary(kvp => kvp.Key, kvp => (ITraceLoggingProvider)kvp.Value);
                return myEventsByProviderItf;
            }
        }

        internal string DeserializedFileName { get; set; }
    }

    /// <summary>
    /// Extracted data from manifest free (TraceLogging) events. Contains event values, process and stacks 
    /// </summary>
    public interface ITraceLoggingData
    {
        /// <summary>
        /// Dictionary of TraceLogging providers, indexed by provider name.
        /// </summary>
        public IReadOnlyDictionary<string, ITraceLoggingProvider> EventsByProvider { get; }
    }

    /// <summary>
    /// Describes a TraceLogging event, including its event ID and field map.
    /// </summary>
    public class TraceLoggingEventDescriptor : ITraceLoggingEventDescriptor
    {
        /// <summary>
        /// Event Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the event ID.
        /// </summary>
        public int EventId { get; set; }

        /// <summary>
        /// Fields which contain single values
        /// </summary>
        public List<string> FieldNames { get; set; } = new List<string>();

        IReadOnlyList<string> ITraceLoggingEventDescriptor.FieldNames => FieldNames;

        /// <summary>
        /// Fields which contain lists
        /// </summary>
        public List<string> ListNames { get; set; } = new List<string>();

        IReadOnlyList<string> ITraceLoggingEventDescriptor.ListNames => ListNames;
    }


    /// <summary>
    /// Describes a TraceLogging event, including its event ID and field map.
    /// </summary>
    public interface ITraceLoggingEventDescriptor
    {
        /// <summary>
        /// Gets the name of the event.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the event ID.
        /// </summary>
        public int EventId { get; }

        /// <summary>
        /// Fields which contain single values
        /// </summary>
        public IReadOnlyList<string> FieldNames { get; }

        /// <summary>
        /// Fields which contain lists
        /// </summary>
        public IReadOnlyList<string> ListNames { get; }

    }


    /// <summary>
    /// Represents a TraceLogging event with its associated fields, lists, stack index, process index, and timestamp.
    /// </summary>
    public class TraceLoggingEvent : ITraceLoggingEvent
    {
        /// <summary>
        /// Gets or sets the event ID.
        /// </summary>
        public int EventId { get; set; }

        /// <summary>
        /// A dictionary of fields, where the key is the field name and the value is the field value.
        /// </summary>
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// A dictionary of lists, where the key is the list name and the value is a list of strings.
        /// </summary>
        public Dictionary<string, List<string>> Lists { get; set; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// Stack Index
        /// </summary>
        public StackIdx StackIdx { get; set; } = StackIdx.None;

        /// <summary>
        /// Process Index
        /// </summary>
        public ETWProcessIndex Process { get; set; } = ETWProcessIndex.Invalid;

        /// <summary>
        /// Gets or sets the timestamp of the event.
        /// </summary>
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.MinValue;

        IReadOnlyDictionary<string, string> ITraceLoggingEvent.Fields => Fields;

        IReadOnlyDictionary<string, IReadOnlyList<string>> myLists;
        IReadOnlyDictionary<string, IReadOnlyList<string>> ITraceLoggingEvent.Lists
        {
            get
            {
                if (myLists == null)
                {
                    myLists = Lists.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value);
                }
                return myLists; 
            }
        }
    }

    /// <summary>
    /// Represents a TraceLogging event with its associated fields, lists, stack index, process index, and timestamp.
    /// </summary>
    public interface ITraceLoggingEvent
    {
        /// <summary>
        /// Gets the event ID.
        /// </summary>
        int EventId { get; }

        /// <summary>
        /// A dictionary of fields, where the key is the field name and the value is the field value.
        /// </summary>
        IReadOnlyDictionary<string, string> Fields { get; }

        /// <summary>
        /// A dictionary of lists, where the key is the list name and the value is a list of strings.
        /// </summary>
        IReadOnlyDictionary<string, IReadOnlyList<string>> Lists { get; }

        /// <summary>
        /// Process Index
        /// </summary>
        ETWProcessIndex Process { get; }

        /// <summary>
        /// Stack Index
        /// </summary>
        StackIdx StackIdx { get; }

        /// <summary>
        /// Gets the timestamp of the event.
        /// </summary>
        DateTimeOffset TimeStamp { get; }
    }

    /// <summary>
    /// Represents a TraceLogging provider, which contains event descriptors and traced events.
    /// </summary>
    public class TraceLoggingProvider : ITraceLoggingProvider
    {
        /// <summary>
        /// Gets or sets the provider GUID
        /// </summary>
        public Guid ProviderId { get; set; }

        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        public string ProviderName { get; set; }

        /// <summary>
        /// A dictionary of event descriptors, where the key is the event ID and the value is the event descriptor.
        /// </summary>
        public Dictionary<int, TraceLoggingEventDescriptor> EventDescriptors { get; set; } = new();

        IReadOnlyDictionary<int, ITraceLoggingEventDescriptor> myEventDescriptorsItf = null;
        IReadOnlyDictionary<int, ITraceLoggingEventDescriptor> ITraceLoggingProvider.EventDescriptors
        {
            get
            {
                myEventDescriptorsItf ??= EventDescriptors.ToDictionary(kvp => kvp.Key, kvp => (ITraceLoggingEventDescriptor)kvp.Value);
                return myEventDescriptorsItf;
            }
        }

        /// <summary>
        /// List of traced events for this provider 
        /// </summary>
        public List<TraceLoggingEvent> Events { get; set; } = new List<TraceLoggingEvent>();

        IReadOnlyList<ITraceLoggingEvent> ITraceLoggingProvider.Events => Events;
    }

    /// <summary>
    /// Represents a TraceLogging provider, which contains event descriptors and traced events.
    /// </summary>
    public interface ITraceLoggingProvider
    {
        /// <summary>
        /// Gets the provider GUID.
        /// </summary>
        Guid ProviderId { get; }

        /// <summary>
        /// Gets the provider name.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// A dictionary of event descriptors, where the key is the event ID and the value is the event descriptor.
        /// </summary>
        IReadOnlyDictionary<int, ITraceLoggingEventDescriptor> EventDescriptors { get; }

        /// <summary>
        /// List of traced events for this provider 
        /// </summary>
        IReadOnlyList<ITraceLoggingEvent> Events { get; }
    }


}
