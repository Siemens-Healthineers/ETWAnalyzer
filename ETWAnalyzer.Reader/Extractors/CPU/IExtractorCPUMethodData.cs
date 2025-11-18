//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ETWAnalyzer.Extractors.CPU
{
    internal interface IExtractorCPUMethodData
    {
        decimal CpuInMs { get; }
        decimal FirstOccurrenceSeconds { get; set; }
        decimal LastOccurrenceSeconds { get; set; }

        ConcurrentBag<ushort> DepthFromBottom { get; }
        
        HashSet<uint> ThreadIds { get; }
        uint ContextSwitchCount { get; }
        ITimeRangeCalculator WaitTimeRange { get; }
        ITimeRangeCalculator ReadyTimeRange { get; }
    }
}