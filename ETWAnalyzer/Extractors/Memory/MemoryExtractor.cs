//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Extractors
{
    class MemoryExtractor : ExtractorBase
    {
        IPendingResult<IWorkingSetDataSource> myWorkingSet;
        IPendingResult<IMemoryUtilizationDataSource> myMemoryUtilization;

        public MemoryExtractor()
        {
        }

        public override void RegisterParsers(ITraceProcessor processor)
        {
            myWorkingSet = processor.UseWorkingSetData();
            myMemoryUtilization = processor.UseMemoryUtilizationData();
        }

        public override void Extract(ITraceProcessor processor, ETWExtract results)
        {
            AnalyzeMemoryUtilization(myMemoryUtilization.Result, results);
            AnalyzeWorkingSets(myWorkingSet.Result, results);
        }

        private void AnalyzeWorkingSets(IWorkingSetDataSource workingSets, ETWExtract results)
        {
            IWorkingSetSnapshot first = workingSets?.Snapshots.FirstOrDefault();
            if( first == null)
            {
                Console.WriteLine("Warning: No Working Set snapshot data present in trace!");
                return;
            }
            results.MemoryUsage.WorkingSetsAtStart = ExtractWorkingSets(first);

            IWorkingSetSnapshot last = workingSets.Snapshots.Last();
            results.MemoryUsage.WorkingSetsAtEnd = ExtractWorkingSets(last);
        }

        IReadOnlyList<ProcessWorkingSet> ExtractWorkingSets(IWorkingSetSnapshot workingsets)
        {
            List<ProcessWorkingSet> lret = new();
            // IWorkingSetEntry.SystemCategoryName is set when Process is null. These values are of no interest except for kernel devs so we can safely skip them
            // SystemCacheWs: 130.66 MiB 130.66 MiB 130.66 MiB
            // PagedPoolWs: 288.46 MiB 288.46 MiB 296.56 MiB
            // SystemPteWs: 8.39 MiB 8.39 MiB 8.39 MiB
            foreach (IWorkingSetEntry entry in workingsets.Entries.Where(x => x.Process != null && x.Process.ImageName != null))
            {
                lret.Add(new ProcessWorkingSet(entry.Process, entry.CommitSize, entry.WorkingSetSize, entry.PrivateWorkingSetSize, entry.SharedCommitSize));
            }

            return lret.OrderByDescending(x => x.CommitInMiB).ToArray();
        }

        private void AnalyzeMemoryUtilization(IMemoryUtilizationDataSource result, ETWExtract results)
        {
            IMemoryUtilizationSnapshot firstMemUntilization = result?.Snapshots.FirstOrDefault();
            if (firstMemUntilization == null)
            {
                Console.WriteLine("Warning: No Memory Utilization snapshot data present in trace!");
                return;
            }
            IMemoryUtilizationSnapshot lastMemoryUtilization = result.Snapshots.LastOrDefault();
            results.MemoryUsage = new MemoryStats(firstMemUntilization.CommitSize.TotalMebibytes, lastMemoryUtilization.CommitSize.TotalMebibytes,
                                                  firstMemUntilization.InUseListSize.TotalMebibytes, lastMemoryUtilization.InUseListSize.TotalMebibytes);

        }
    }
}
