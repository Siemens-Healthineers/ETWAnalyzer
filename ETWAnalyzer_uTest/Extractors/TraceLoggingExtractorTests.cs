//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.TraceLogging;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.Extractors.TraceLogging;
using ETWAnalyzer.Helper;
using ETWAnalyzer.Infrastructure;
using Microsoft.Windows.EventTracing.Events;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ETWAnalyzer_uTest.Extractors
{
    public class TraceLoggingExtractorTests
    {
        [Fact]
        public void Can_Serialize_Deserialize_TraceLoggingData()
        {
            using var local = TempDir.Create();

            ETWExtract extract = new();
            Guid providerId = new Guid("1B0F1F3F-3DB9-47E2-960F-2B0AC61102E5");
            const string StringValue1 = "Test Value 1";
            const string StringFieldName1 = "StringField1";
            const string IntListName = "IntList";

            const string IntFieldName1 = "Int32Field1"; 


            extract.TraceLogging.EventsByProvider.Add("TestProvider", new TraceLoggingProvider
            {
                ProviderId = providerId,
                ProviderName = "TestProvider",
                EventDescriptors = new Dictionary<int, TraceLoggingEventDescriptor>
                {
                    { 1, new TraceLoggingEventDescriptor
                        {
                            EventId = 1,
                            Name = "TestEvent",
                            FieldNames = new List<string>
                            {
                                StringFieldName1,
                                IntFieldName1
                            },
                            ListNames = new List<string>
                            {
                                IntListName,
                            }
                            
                        }
                    }
                }
            });

            DateTimeOffset KTime = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero);


            const string Stack1 = "Stack 1";

            var stackIdx1 = extract.TraceLogging.Stacks.AddStack(Stack1);

            extract.TraceLogging.EventsByProvider["TestProvider"].Events.Add(new TraceLoggingEvent
            {
                EventId = 1,
                ThreadId = 100,
                TimeStamp = KTime,
                ProcessIdx =  0,
                StackIdx = stackIdx1,
                Fields = new Dictionary<string, string>
                {
                    { StringFieldName1, StringValue1 },
                    { IntFieldName1, "42" }
                },
                Lists = new Dictionary<string, List<string>>
                {
                    { IntListName, new List<string> { "1", "2", "3" } }
                },

            });

            extract.Processes.Add(new ETWProcess
            {
                ProcessID = 1,
                ProcessName = "TestProcess",
                StartTime = new DateTimeOffset(2000,1,1,1,1,1,TimeSpan.Zero),
                EndTime = new DateTimeOffset(2000, 1, 1, 1, 1, 10, TimeSpan.Zero),
            });

            string fileName = Path.Combine(local.Name, "test.json");    

            ExtractSerializer ser = new ExtractSerializer(fileName);
            ser.Serialize(extract);

            IETWExtract deser = ExtractSerializer.DeserializeFile(fileName);
            var traceLog = deser.TraceLogging;

            Assert.Single(traceLog.EventsByProvider);
            Assert.Equal(providerId, traceLog.EventsByProvider["TestProvider"].ProviderId);
            ITraceLoggingProvider testProvider = traceLog.EventsByProvider["TestProvider"];
          
            Assert.Single(testProvider.EventDescriptors);

            // Event Descriptor asserts
            ITraceLoggingEventDescriptor descriptor = testProvider.EventDescriptors[1];

            Assert.Equal(1, descriptor.EventId);

            Assert.Equal("TestEvent", descriptor.Name);
            Assert.Equal(2, descriptor.FieldNames.Count);

            Assert.Equal(StringFieldName1, descriptor.FieldNames[0]);
            Assert.Equal(IntFieldName1, descriptor.FieldNames[1]);

            Assert.Single(descriptor.ListNames);
            Assert.Equal(IntListName, descriptor.ListNames[0]);

            // Event Data asserts
            Assert.Single(testProvider.Events);
            ITraceLoggingEvent traceEv = testProvider.Events[0];

            Assert.Equal(1, traceEv.EventId);
            Assert.Equal(100u, traceEv.ThreadId);
            Assert.Equal(KTime, traceEv.TimeStamp);
            Assert.Equal(1, traceEv.EventId);
            Assert.Equal("TestProcess", traceEv.Process.ProcessName);
            Assert.Equal(Stack1, traceEv.StackTrace);
            Assert.Equal(StringValue1, traceEv.TryGetField(StringFieldName1));
            Assert.Equal("42", traceEv.TryGetField(IntFieldName1));

            Assert.Equal("TestEvent", traceEv.TypeInformation.Name);
            Assert.Equal("TestProvider", traceEv.TypeInformation.ProviderName);
            Assert.Equal(providerId, traceEv.TypeInformation.ProviderGuid);

            IReadOnlyList<string> intList = traceEv.TryGetList(IntListName);

            Assert.Equal(3, intList.Count);
            Assert.Equal(new List<string> { "1", "2", "3" }, intList);
        }

        /// <summary>
        /// Create a mocked primitive field of the given type and value.
        /// </summary>
        static IGenericEventField CreatePrimitiveField(string name, GenericEventFieldType type, object value)
        {
            var field = new Mock<IGenericEventField>();
            field.Setup(x => x.Name).Returns(name);
            field.Setup(x => x.Type).Returns(type);
            switch (type)
            {
                case GenericEventFieldType.Int32:
                    field.Setup(x => x.AsInt32).Returns((int)value);
                    break;
                case GenericEventFieldType.String:
                    field.Setup(x => x.AsString).Returns((string)value);
                    break;
                case GenericEventFieldType.Boolean:
                    field.Setup(x => x.AsBoolean).Returns((bool)value);
                    break;
                default:
                    throw new NotSupportedException($"Test helper does not support {type}");
            }

            return field.Object;
        }

        /// <summary>
        /// Create a mocked list field for a list of Int32 values.
        /// </summary>
        static IGenericEventField CreateInt32ListField(string name, IReadOnlyList<int> values)
        {
            var field = new Mock<IGenericEventField>();
            field.Setup(x => x.Name).Returns(name);
            field.Setup(x => x.Type).Returns(GenericEventFieldType.Int32List);
            field.Setup(x => x.AsInt32List).Returns(values);
            return field.Object;
        }

        /// <summary>
        /// Create a mocked structure field which contains the given child fields.
        /// </summary>
        static IGenericEventField CreateStructureField(string name, params IGenericEventField[] children)
        {
            var field = new Mock<IGenericEventField>();
            field.Setup(x => x.Name).Returns(name);
            field.Setup(x => x.Type).Returns(GenericEventFieldType.Structure);
            field.Setup(x => x.AsStructure).Returns(children);
            return field.Object;
        }

        /// <summary>
        /// Create a mocked structure list field where each structure is described by its child fields.
        /// </summary>
        static IGenericEventField CreateStructureListField(string name, params IReadOnlyList<IGenericEventField>[] structures)
        {
            var field = new Mock<IGenericEventField>();
            field.Setup(x => x.Name).Returns(name);
            field.Setup(x => x.Type).Returns(GenericEventFieldType.StructureList);
            field.Setup(x => x.AsStructureList).Returns(structures);
            return field.Object;
        }

        [Fact]
        public void Structure_Fields_Are_Flattened_With_DottedNames()
        {
            // Event with a top level field and a structure containing a primitive, a list and a nested structure
            var fields = new List<IGenericEventField>
            {
                CreatePrimitiveField("TopField", GenericEventFieldType.String, "top"),
                CreateStructureField("Struct",
                    CreatePrimitiveField("Number", GenericEventFieldType.Int32, 42),
                    CreateInt32ListField("Values", new List<int> { 1, 2, 3 }),
                    CreateStructureField("Inner",
                        CreatePrimitiveField("Flag", GenericEventFieldType.Boolean, true)))
            };

            // Descriptor field/list name collection
            List<string> fieldNames = new();
            List<string> listNames = new();
            TraceLoggingEventExtractor.CollectFieldNames(fields, null, fieldNames, listNames);

            Assert.Equal(new List<string> { "TopField", "Struct.Number", "Struct.Inner.Flag" }, fieldNames);
            Assert.Equal(new List<string> { "Struct.Values" }, listNames);

            // Event value collection
            TraceLoggingEvent ev = new();
            TraceLoggingEventExtractor.AddFields(fields, null, ev, "TestProvider", "TestEvent", 1);

            Assert.Equal("top", ev.TryGetField("TopField"));
            Assert.Equal("42", ev.TryGetField("Struct.Number"));
            Assert.Equal("True", ev.TryGetField("Struct.Inner.Flag"));
            Assert.Equal(new List<string> { "1", "2", "3" }, ev.TryGetList("Struct.Values"));
        }

        [Fact]
        public void StructureList_Is_Serialized_As_Readable_List()
        {
            // Event with a structure list (e.g. telemetry properties) where each structure has Key/Value fields
            var fields = new List<IGenericEventField>
            {
                CreateStructureListField("Properties",
                    new List<IGenericEventField>
                    {
                        CreatePrimitiveField("Key", GenericEventFieldType.String, "HostName"),
                        CreatePrimitiveField("Value", GenericEventFieldType.String, "Dev14"),
                    },
                    new List<IGenericEventField>
                    {
                        CreatePrimitiveField("Key", GenericEventFieldType.String, "IsLoadSuccess"),
                        CreatePrimitiveField("Value", GenericEventFieldType.String, "False"),
                    })
            };

            // Descriptor field/list name collection - a structure list is a list field
            List<string> fieldNames = new();
            List<string> listNames = new();
            TraceLoggingEventExtractor.CollectFieldNames(fields, null, fieldNames, listNames);

            Assert.Empty(fieldNames);
            Assert.Equal(new List<string> { "Properties" }, listNames);

            // Event value collection - each structure is serialized to a readable {Name=Value; ...} string
            TraceLoggingEvent ev = new();
            TraceLoggingEventExtractor.AddFields(fields, null, ev, "TestProvider", "TestEvent", 1);

            Assert.Equal(new List<string>
            {
                "{Key=HostName; Value=Dev14}",
                "{Key=IsLoadSuccess; Value=False}",
            }, ev.TryGetList("Properties"));
        }

    }
}
