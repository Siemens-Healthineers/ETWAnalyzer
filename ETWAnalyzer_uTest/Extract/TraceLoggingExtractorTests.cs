//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.TraceLogging;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ETWAnalyzer_uTest.Extract
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


            extract.TraceLogging.EventsByProvider["TestProvider"].Events.Add(new TraceLoggingEvent
            {
                EventId = 1,
                TimeStamp = KTime,
                Process = (ETWProcessIndex) 0,
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
            var testProvider = traceLog.EventsByProvider["TestProvider"];
          
            Assert.Single(testProvider.EventDescriptors);

            var descriptor = testProvider.EventDescriptors[1];

            Assert.Equal(1, descriptor.EventId);
            Assert.Equal("TestEvent", descriptor.Name);
            Assert.Equal(2, descriptor.FieldNames.Count);

            Assert.Equal(StringFieldName1, descriptor.FieldNames[0]);
            Assert.Equal(IntFieldName1, descriptor.FieldNames[1]);

            Assert.Single(descriptor.ListNames);
            Assert.Equal(IntListName, descriptor.ListNames[0]);

            Assert.Single(testProvider.Events);
            var traceEv = testProvider.Events[0];

            Assert.Equal(1, traceEv.EventId);
            Assert.Equal(KTime, traceEv.TimeStamp);
            Assert.Equal(1, traceEv.EventId);
            Assert.Equal(0, (int) traceEv.Process);
            Assert.Equal(StringValue1, traceEv.Fields[StringFieldName1]);
            Assert.Equal("42", traceEv.Fields[IntFieldName1]);

            var intList = traceEv.Lists[IntListName];

            Assert.Equal(3, intList.Count);
            Assert.Equal(new List<string> { "1", "2", "3" }, intList);
        }

    }
}
