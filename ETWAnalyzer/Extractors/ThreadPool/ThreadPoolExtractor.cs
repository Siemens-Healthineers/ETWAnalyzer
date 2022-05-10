﻿//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.ThreadPool;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Extractors
{
    class ThreadPoolExtractor : ExtractorBase
    {

        IPendingResult<IGenericEventDataSource> myGenericEvents;

        public override void RegisterParsers(ITraceProcessor processor)
        {

            myGenericEvents = processor.UseGenericEvents();
        }

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            results.ThreadPool = new ThreadPoolStats();
            ExtractThreadPoolStarvarvations(results);
        }

        private void ExtractThreadPoolStarvarvations(ETWExtract results)
        {
            var starvations = myGenericEvents.Result.Events.Where(x => x.ProviderName == Constants.DotNetRuntimeProviderName
                            && x.TaskName == "ThreadPoolWorkerThreadAdjustment"
                            && x.OpcodeName == "Adjustment"
                            && x.Fields.Dictionary["Reason"].EnumValue == "Starvation"
                            );
            
            foreach (var ins in starvations)
            {
                var pk = new ProcessKey(ins.Process.ImageName, ins.Process.Id, ins.Process.CreateTime.HasValue ? ins.Process.CreateTime.Value.DateTimeOffset : default(DateTimeOffset));

                IList<ThreadPoolStarvationInfo> value;
                ThreadPoolStarvationInfo info = new ThreadPoolStarvationInfo()
                {
                    NewWorkerThreadCount = ins.Fields.Dictionary["NewWorkerThreadCount"].AsUInt32,
                    DateTime = ins.Timestamp.DateTimeOffset,
                    TotalSeconds = ins.Timestamp.TotalSeconds
                };
                if (results.ThreadPool.PerProcessThreadPoolStarvations.TryGetValue(pk, out value))
                {
                    value.Add(info);
                }
                else
                {
                    results.ThreadPool.PerProcessThreadPoolStarvations[pk] = new List<ThreadPoolStarvationInfo>() { info };
                }

            }
        }

    }
}
