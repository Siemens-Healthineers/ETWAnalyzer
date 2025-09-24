//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract.Common;
using ETWAnalyzer.Extractors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                if (myEventsByProviderItf == null)
                {
                    myEventsByProviderItf = EventsByProvider.ToDictionary(kvp => kvp.Key, kvp =>
                    {
                        foreach(var desc in kvp.Value.EventDescriptors)
                        {
                            desc.Value.Provider = kvp.Value; 
                        }
                        return (ITraceLoggingProvider)kvp.Value;
                    });
                }
                return myEventsByProviderItf;
            }
        }

        /// <summary>
        /// Stack raw data used by events. Only set during extraction later we read data via interface back from an external file upon access.
        /// </summary>
        public StackCollection Stacks { get; set; } = new();

        /// <summary>
        /// Stack trace collection which is linked by StackIdx stored in <see cref="ITraceLoggingProvider.Events"/>
        /// </summary>
        IStackCollection ITraceLoggingData.Stacks => myStackReader.Value;

        /// <summary>
        /// Read stack data from external file upon access
        /// </summary>
        readonly Lazy<StackCollection> myStackReader;

        /// <summary>
        /// Needed by derived classes to deserialize data from further external files.
        /// </summary>
        internal string DeserializedFileName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceLoggingEventData"/> class.
        /// </summary>
        public TraceLoggingEventData()
        {
            myStackReader = new Lazy<StackCollection>(ReadTraceLogStacksFromExternalFile);
        }

        /// <summary>
        /// This is where we deserialize stack data from derived file if it was set in <see cref="DeserializedFileName"/>.
        /// </summary>
        /// <returns></returns>
        StackCollection ReadTraceLogStacksFromExternalFile()
        {
            StackCollection lret = Stacks;
            if (DeserializedFileName != null)
            {
                ExtractSerializer ser = new(DeserializedFileName);
                lret = ser.Deserialize<StackCollection>(ExtractSerializer.TraceLogStackPostFix);
            }

            return lret;
        }
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

        /// <summary>
        /// Stack trace collection which is linked by StackIdx stored in <see cref="ITraceLoggingProvider.Events"/>
        /// </summary>
        IStackCollection Stacks { get; }
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

        /// <summary>
        /// Set during deserialization from an extract file to enable easy access to provider data in object model
        /// </summary>
        internal ITraceLoggingProvider Provider { get; set; }

        /// <summary>
        /// Gets the name of the provider, implemented as explicit interface to ensure that this data is not serialized in the JSON file.
        /// </summary>
        string ITraceLoggingEventDescriptor.ProviderName => Provider.ProviderName;

        /// <summary>
        /// Gets the provider GUID, implemented as explicit interface to ensure that this data is not serialized in the JSON file.
        /// </summary>
        Guid ITraceLoggingEventDescriptor.ProviderGuid => Provider.ProviderId;
    }


    /// <summary>
    /// Describes a TraceLogging event, including its event ID and field map.
    /// </summary>
    public interface ITraceLoggingEventDescriptor
    {
        /// <summary>
        /// Gets the provider name.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Gets the provider GUID.
        /// </summary>
        Guid ProviderGuid { get; }

        /// <summary>
        /// Gets the name of the event.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the event ID.
        /// </summary>
        int EventId { get; }

        /// <summary>
        /// Fields which contain single values
        /// </summary>
        IReadOnlyList<string> FieldNames { get; }

        /// <summary>
        /// Fields which contain lists
        /// </summary>
        IReadOnlyList<string> ListNames { get; }

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
        /// Gets or sets the thread ID associated with the event.
        /// </summary>
        public int ThreadId { get; set; }

        /// <summary>
        /// A dictionary of fields, where the key is the field name and the value is the field value.
        /// </summary>
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// A dictionary of lists, where the key is the list name and the value is a list of strings.
        /// </summary>
        public Dictionary<string, List<string>> Lists { get; set; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// Try to get a field value by its name. Returns null if the field does not exist, or it was never set.
        /// </summary>
        /// <param name="fieldName">Name of field</param>
        /// <returns></returns>
        public string TryGetField(string fieldName)
        {
            Fields.TryGetValue(fieldName, out string value);
            return value;
        }

        /// <summary>
        /// Try to get a list by its name. If the list does not exist, it returns an empty list.
        /// </summary>
        /// <param name="listName">Name of list field.</param>
        /// <returns></returns>
        public IReadOnlyList<string> TryGetList(string listName)
        {
            if (!Lists.TryGetValue(listName, out List<string> list))
            {
                list = new List<string>();
            }

            return list;
        }


        /// <summary>
        /// Stack Index
        /// </summary>
        public StackIdx StackIdx { get; set; } = StackIdx.None;

        string ITraceLoggingEvent.StackTrace
        {
            get
            {
                if (Extract == null)
                {
                    throw new NotSupportedException("Extract is not set. This field will only be set during deserialization when reading from an extract file.");
                }


                if (StackIdx == StackIdx.None)
                {
                    return string.Empty; // No stack trace available
                }
                return Extract.TraceLogging.Stacks.GetStack(StackIdx);
            }
        }


        /// <summary>
        /// Process Index
        /// </summary>
        public ETWProcessIndex ProcessIdx { get; set; } = ETWProcessIndex.Invalid;


        ETWProcess ITraceLoggingEvent.Process
        {
            get
            {
                if( Extract == null )
                {
                    throw new NotSupportedException("Extract is not set. This field will only be set during deserialization when reading from an extract file.");
                }
                return Extract.GetProcess(ProcessIdx);
            }
        }

        /// <summary>
        /// Gets or sets the timestamp of the event.
        /// </summary>
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.MinValue;

        /// <summary>
        /// Set during deserialize
        /// </summary>
        internal TraceLoggingEventDescriptor TypeInformation { get; set; }

        ITraceLoggingEventDescriptor ITraceLoggingEvent.TypeInformation => TypeInformation;

        // set during deserialize
        internal IETWExtract Extract { get; set; }
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
        /// Gets the thread ID associated with the event.
        /// </summary>
        public int ThreadId { get; }

        /// <summary>
        /// Gets the timestamp of the event.
        /// </summary>
        DateTimeOffset TimeStamp { get; }

        /// <summary>
        /// Try to get a list by its name. If the list does not exist, it returns an empty list.
        /// </summary>
        /// <param name="listName">Name of list field.</param>
        /// <returns></returns>
        IReadOnlyList<string> TryGetList(string listName);

        /// <summary>
        /// Try to get a field value by its name. Returns null if the field does not exist, or it was never set.
        /// </summary>
        /// <param name="fieldName">Name of field</param>
        /// <returns></returns>
        string TryGetField(string fieldName);

        /// <summary>
        /// Type descriptor for this event which contains field map and others.
        /// </summary>
        ITraceLoggingEventDescriptor TypeInformation { get; }

        /// <summary>
        /// Stack trace string of event when available.
        /// </summary>
        string StackTrace { get; }

        /// <summary>
        /// Get Stack index of stack trace collection which is unique for all TraceLogging events.
        /// </summary>
        StackIdx StackIdx { get; }

        /// <summary>
        /// Process object which is associated with this event.
        /// </summary>
        ETWProcess Process { get;  }
        
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
