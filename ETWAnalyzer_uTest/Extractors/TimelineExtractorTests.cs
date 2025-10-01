using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.CPU;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.Extractors.CPU;
using ETWAnalyzer_uTest.TestInfrastructure;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Cpu;
using Microsoft.Windows.EventTracing.Processes;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ETWAnalyzer_uTest.Extractors
{
    public class TimelineExtractorTests
    {
        private ITestOutputHelper myWriter;

        public TimelineExtractorTests(ITestOutputHelper myWriter)
        {
            this.myWriter = myWriter;
        }

        [Fact]
        public void Values_Are_Propagated()
        {
            var duration = TimeSpan.FromSeconds(5.5d);
            var startTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            float interval = 1.0f;

            TimelineExtractor extractor = new(interval, startTime, duration);
            Assert.Equal(duration, extractor.SessionDuration);
            Assert.Equal(startTime, extractor.SessionStart);
            Assert.Equal(interval, extractor.Timeline.ExtractionInveralS);

            Assert.Equal(6, extractor.myTimelineBuckets.Length);
        }

        [Fact]
        public void BucketIndices_AreCorrect()
        {
            var duration = TimeSpan.FromSeconds(5.5d);
            var startTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            float interval = 1.0f;

            TimelineExtractor extractor = new(interval, startTime, duration);

            // Boundary checks
            Assert.Equal(0, extractor.GetBucket(startTime));
            Assert.Equal(5, extractor.GetBucket(startTime+duration));

            Assert.Equal(0, extractor.GetBucket(startTime + TimeSpan.FromSeconds(0.99f)));
            Assert.Equal(1, extractor.GetBucket(startTime + TimeSpan.FromSeconds(1.0f)));
            Assert.Equal(1, extractor.GetBucket(startTime + TimeSpan.FromSeconds(1.01f)));
        }

        [Fact]
        public void TimeLine_Data_IsSummed_Correct()
        {
            var duration = TimeSpan.FromSeconds(5.5d);
            var startTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            float interval = 1.0f;
            StaticTraceProcessorContext.MetaData = new TraceMetaDataMock(startTime);

            TimelineExtractor extractor = new(interval, startTime, duration);

            ProcessKey key = new("tester.exe", 100, startTime);
            extractor.AddSample(key, new DummySample(startTime, 1.0f));

            Assert.Equal(1.0m, extractor.Timeline.ProcessTimeLines[key].CPUMs[0]);
            Assert.Equal(0.0m, extractor.Timeline.ProcessTimeLines[key].CPUMs[1]);

            extractor.AddSample(key, new DummySample(startTime+TimeSpan.FromSeconds(0.5), 1.0f));
            Assert.Equal(2.0m, extractor.Timeline.ProcessTimeLines[key].CPUMs[0]);

            extractor.AddSample(key, new DummySample(startTime + TimeSpan.FromSeconds(1.0f), 1.0f));
            Assert.Equal(1.0m, extractor.Timeline.ProcessTimeLines[key].CPUMs[1]);

        }

        CPUTimeLine GetSampleData(ProcessKey key)
        {
            var duration = TimeSpan.FromSeconds(5.5d);
            var startTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            float interval = 1.0f;

            TimelineExtractor extractor = new(interval, startTime, duration);

            
            extractor.AddSample(key, new DummySample(startTime, 1.0f));
            extractor.AddSample(key, new DummySample(startTime + TimeSpan.FromSeconds(0.5), 1.0f));
            extractor.AddSample(key, new DummySample(startTime + TimeSpan.FromSeconds(1.0f), 1.0f));
            extractor.AddSample(key, new DummySample(startTime + TimeSpan.FromSeconds(2.0f), 4000.0f));
            extractor.AddSample(key, new DummySample(startTime + TimeSpan.FromSeconds(3.0f), 2000.0f));
            extractor.AddSample(key, new DummySample(startTime + TimeSpan.FromSeconds(4.0f), 1000.0f));

            return extractor.Timeline;
        }

        [Fact]
        public void Can_Serialize_Deserialize_Timeline()
        {
            ETWExtract extract = new()
            {
                NumberOfProcessors = 4,
            };

            ProcessKey key = new("tester.exe", 100, DateTimeOffset.MinValue);

            StaticTraceProcessorContext.MetaData = new TraceMetaDataMock(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero));
            extract.CPU = new CPUStats(null, null, null, GetSampleData(key),null, null);

            MemoryStream stream = new();
            ExtractSerializer.Serialize(stream, extract);
            stream.Position = 0;

            using var expprinter = new ExceptionalPrinter(myWriter);
            string serialized = Encoding.UTF8.GetString(stream.ToArray());
            expprinter.Messages.Add($"Serialized Data: {serialized}");

            IETWExtract deserialized = ExtractSerializer.Deserialize<ETWExtract>(stream);


            Assert.Equal(6, deserialized.CPU.TimeLine[key].CPUMs.Count);

            List<KeyValuePair<DateTimeOffset,decimal>> timelineAsMsCPU = deserialized.CPU.TimeLine.GetProcessTimeLineData(extract, key, false);
            for(int i = 0; i < timelineAsMsCPU.Count; i++)
            {
                expprinter.Messages.Add($"timeline[{i} at {timelineAsMsCPU[i].Key}]: {timelineAsMsCPU[i].Value}");
            }

            Assert.Equal(DateTimeOffset.MinValue + TimeSpan.FromSeconds(1.0d), timelineAsMsCPU[0].Key);
            Assert.Equal(DateTimeOffset.MinValue + TimeSpan.FromSeconds(2.0d), timelineAsMsCPU[1].Key);

            Assert.Equal(2,    timelineAsMsCPU[0].Value);
            Assert.Equal(1,    timelineAsMsCPU[1].Value);
            Assert.Equal(4000, timelineAsMsCPU[2].Value);
            Assert.Equal(2000, timelineAsMsCPU[3].Value);
            Assert.Equal(1000, timelineAsMsCPU[4].Value);
            Assert.Equal(0,    timelineAsMsCPU[5].Value);

            List<KeyValuePair<DateTimeOffset, decimal>> timelineAsPercentCPU = deserialized.CPU.TimeLine.GetProcessTimeLineData(extract, key, true);
            for (int i = 0; i < timelineAsPercentCPU.Count; i++)
            {
                expprinter.Messages.Add($"timeline[{i} at {timelineAsPercentCPU[i].Key}]: {timelineAsPercentCPU[i].Value}");
            }

            Assert.Equal(100.0m, timelineAsPercentCPU[2].Value);
            Assert.Equal(50.0m, timelineAsPercentCPU[3].Value);
            Assert.Equal(25.0m, timelineAsPercentCPU[4].Value);
        }
    }


    class DummySample : ICpuSample
    {
        public DummySample(DateTimeOffset time, float weightMs)
        {
            myTimestamp = new Timestamp(time.Ticks);
            myDuration = new Duration((long) (weightMs*1000_000));
        }

        public uint Processor => 0;

        readonly Duration myDuration;
        public Duration Weight => myDuration;

        readonly Timestamp myTimestamp;

        public Timestamp Timestamp => myTimestamp;

        public Address InstructionPointer => throw new NotImplementedException();

        public IStackSnapshot Stack => throw new NotImplementedException();

        public IThread Thread => throw new NotImplementedException();

        public IProcess Process => throw new NotImplementedException();

        public IImage Image => throw new NotImplementedException();

        public bool? IsExecutingDeferredProcedureCall => throw new NotImplementedException();

        public bool? IsExecutingInterruptServicingRoutine => throw new NotImplementedException();

        public int? Priority => throw new NotImplementedException();

        public int? Rank => throw new NotImplementedException();

        public StackFrame TopStackFrame => throw new NotImplementedException();

        public IThread WorkOnBehalfThread => throw new NotImplementedException();

        public IProcess WorkOnBehalfProcess => throw new NotImplementedException();

    }
}
