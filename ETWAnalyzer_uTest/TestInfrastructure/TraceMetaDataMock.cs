//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Metadata;
using System;

namespace ETWAnalyzer_uTest.TestInfrastructure
{
    internal class TraceMetaDataMock : ITraceMetadata
    {
        public Version OSVersion => throw new NotImplementedException();

        public bool Is32Bit => throw new NotImplementedException();

        public FrequencyValue ProcessorSpeed => throw new NotImplementedException();

        public TraceClockType ClockType => throw new NotImplementedException();

        public FrequencyValue PerformanceCounterFrequency => throw new NotImplementedException();

        public FrequencyValue ProcessorUsageTimerFrequency => throw new NotImplementedException();

        public Timestamp FirstAnalyzerDisplayedEventTime => throw new NotImplementedException();

        public Timestamp LastEventTime => throw new NotImplementedException();

        public Duration AnalyzerDisplayedDuration => throw new NotImplementedException();

        public long LostBufferCount => throw new NotImplementedException();

        public long LostEventCount => throw new NotImplementedException();

        public string TracePath => throw new NotImplementedException();

        public DateTimeOffset StartTime { get; set; } = DateTimeOffset.MinValue;

        public DateTimeOffset StopTime => throw new NotImplementedException();

        public uint ProcessorCount => throw new NotImplementedException();

        public uint KernelEventVersion => throw new NotImplementedException();

        public DateTimeOffset GetWallClock(Timestamp timestamp)
        {
            return StartTime + TimeSpan.FromTicks(timestamp.Nanoseconds - StartTime.Ticks);
        }

        public TraceMetaDataMock(DateTimeOffset baseValue)
        {
            StartTime = baseValue;
        }

        public TraceMetaDataMock()
        {
        }
    }
}
