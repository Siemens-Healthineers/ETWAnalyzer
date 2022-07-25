//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extractors;
using ETWAnalyzer.Extractors.CPU;
using Microsoft.Windows.EventTracing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Store CPU consumption of all methods by process
    /// </summary>
    public class CPUPerProcessMethodList : ICPUPerProcessMethodList
    {
        /// <summary>
        /// List of all methods found in ETL file
        /// </summary>
        public List<string> MethodNames { get; set; } = new List<string>();

        /// <summary>
        /// List of all methods found in ETL file
        /// </summary>
        IReadOnlyList<string> ICPUPerProcessMethodList.MethodNames { get => MethodNames; }


        /// <summary>
        /// If true no CPU threshold of 10ms was applied during extraction
        /// </summary>
        public bool ContainsAllCPUData
        {
            get;
            set;
        }

        /// <summary>
        /// Input ETL file has recorded Context Switch Data
        /// </summary>
        public bool? HasCSwitchData
        {
            get;
            set;
        }

        /// <summary>
        /// Input ETL file has recorded CPU sampling data
        /// </summary>
        public bool? HasCPUSamplingData
        {
            get;
            set;
        }

        /// <summary>
        /// Store per process per method the CPU in ms
        /// Index based structure to support compact serialization
        /// </summary>
        public List<MethodsByProcess> MethodStatsPerProcess
        {
            get;
            set;
        } = new List<MethodsByProcess>();

        /// <summary>
        /// Store per process per method the CPU in ms
        /// Index based structure to support compact serialization
        /// </summary>
        IReadOnlyList<MethodsByProcess> ICPUPerProcessMethodList.MethodStatsPerProcess { get => MethodStatsPerProcess; }

        /// <summary>
        /// For fast lookup and efficient insertion we remember the index in this dictionary
        /// </summary>
        readonly Dictionary<string, int> myMethodChecker = new Dictionary<string, int>();



        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            // Set Methods reference after deserialization to allow reading the 
            // method name in the object model
            foreach (var process in MethodStatsPerProcess)
            {
                foreach (var cost in process.Costs)
                {
                    cost.MethodList = this.MethodNames;
                }
            }
        }

        /// <summary>
        /// Add a method entry for a given process. 
        /// </summary>
        /// <param name="process"></param>
        /// <param name="method"></param>
        /// <param name="cpuData"></param>
        /// <param name="cutOffMs">Do not add method if duration (Wait or CPU) is &lt;= cutOffMs</param>
        internal void AddMethod(ProcessKey process, string method, CpuData cpuData, int cutOffMs)
        {
            uint cpuDurationMs = (uint)Math.Round(cpuData.CpuInMs.TotalMilliseconds);
            uint waitDurationMs = (uint)(cpuData.WaitTimeRange.GetDuration().TotalMilliseconds);

            if (cpuDurationMs  <= cutOffMs && 
                waitDurationMs <= cutOffMs && 
                cutOffMs != 0)
            {
                return;
            }

            MethodsByProcess methodList = MethodStatsPerProcess.FirstOrDefault(x => x.Process == process);
            if (methodList == null)
            {
                methodList = new MethodsByProcess(process);
                MethodStatsPerProcess.Add(methodList);
            }

            long totalStackDepth = cpuData.DepthFromBottom.Aggregate(0L, (sum, value) => sum + value);
            long averageStackDepths = totalStackDepth / (cpuData.DepthFromBottom.Count > 0 ? cpuData.DepthFromBottom.Count : 1);

            var cost = new MethodCost(GetMethodIndex(method), cpuDurationMs, waitDurationMs, cpuData.FirstOccurrenceSeconds, cpuData.LastOccurrenceSeconds, cpuData.ThreadIds.Count,
                                      (int)averageStackDepths, (uint)cpuData.ReadyTimeRange.GetDuration().TotalMilliseconds)
            {
                MethodList = MethodNames
            };
            methodList.Costs.Add(cost);
        }

        /// <summary>
        /// Get Index in Methods array for a given method. If the method is not yet in the list it is added.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        MethodIndex GetMethodIndex(string method)
        {
            if (!myMethodChecker.TryGetValue(method, out int index))
            {
                MethodNames.Add(method);
                index = MethodNames.Count - 1;
                myMethodChecker.Add(method, MethodNames.Count - 1);
            }

            return (MethodIndex)index;
        }

        internal void SortMethodsByNameAndCPU()
        {
            // Sort Method names alphabetically in Json and update MethodIdx of the status
            List<string> sorted = MethodNames.OrderBy(x => x).ToList();

            Dictionary<MethodIndex, MethodIndex> old2NewIndexMap = new Dictionary<MethodIndex, MethodIndex>();

            for (int i = 0; i < sorted.Count; i++)
            {
                string method = sorted[i];
                old2NewIndexMap[(MethodIndex)myMethodChecker[method]] = (MethodIndex)i;
            }

            MethodNames.Clear();
            // keep list because during construction we reference the Methods array
            MethodNames.AddRange(sorted);

            // Now Update stats 
            List<MethodsByProcess> newList = new List<MethodsByProcess>();
            foreach (var process2Method in MethodStatsPerProcess)
            {
                foreach (var method2Duration in process2Method.Costs)
                {
                    method2Duration.MethodIdx = old2NewIndexMap[method2Duration.MethodIdx];
                }
            }


            // Sort processes and methods per processes descending by CPU consumption
            static int SortAscendingByCPU(MethodsByProcess a, MethodsByProcess b)
            {
                ulong cpuA = a.Costs.Aggregate(0uL, (sum, cost) => sum + cost.CPUMs);
                ulong cpuB = b.Costs.Aggregate(0uL, (sum, cost) => sum + cost.CPUMs);
                return cpuB.CompareTo(cpuA);
            }

            MethodStatsPerProcess.Sort(SortAscendingByCPU);

            foreach (var process in MethodStatsPerProcess)
            {
                process.Costs.Sort((a, b) => b.CPUMs.CompareTo(a.CPUMs));
            }
        }


        /// <summary>
        /// String version of object which is used by debugger
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Processes: {MethodStatsPerProcess.Count} MethodNames: {MethodNames.Count}";
        }

    }
}
