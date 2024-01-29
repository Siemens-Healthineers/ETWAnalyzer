//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.CPU;
using ETWAnalyzer.Extract.CPU.Extended;
using ETWAnalyzer.Extractors;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Part of ETWExtract which contains per process CPU consumption metrics. This is serialized to Json file.
    /// </summary>
    public class CPUStats : ICPUStats
    {
        /// <summary>
        /// Simple stat which contains the total CPU in ms per process
        /// </summary>
        public Dictionary<ProcessKey, uint> PerProcessCPUConsumptionInMs
        {
            get;
        } = new();

        /// <summary>
        /// Simple stat which contains the total CPU in ms per process
        /// </summary>
        IReadOnlyDictionary<ProcessKey, uint> ICPUStats.PerProcessCPUConsumptionInMs => PerProcessCPUConsumptionInMs;

        /// <summary>
        /// Average process priority of all threads taken from CPU sampling data if present.
        /// </summary>
        public Dictionary<ETWProcessIndex, float> PerProcessAvgCPUPriority
        {
            get;
        } = new();

        IReadOnlyDictionary<ETWProcessIndex, float> ICPUStats.PerProcessAvgCPUPriority => PerProcessAvgCPUPriority;

        /// <summary>
        /// Contains methods which have CPU/Wait > 10ms (default) 
        /// </summary>
        public CPUPerProcessMethodList PerProcessMethodCostsInclusive
        {
            get;
        } = new CPUPerProcessMethodList();

        /// <summary>
        /// Lists all methods which have > 10ms (default) 
        /// </summary>
        ICPUPerProcessMethodList ICPUStats.PerProcessMethodCostsInclusive => PerProcessMethodCostsInclusive;

        /// <summary>
        /// CPU Frequency Metrics
        /// </summary>
        public CPUExtended ExtendedCPUMetrics { get; set; }

        /// <summary>
        /// CPU Frequency Metrics
        /// </summary>
        ICPUExtended ICPUStats.ExtendedCPUMetrics => myLazyCPU.Value;

        Lazy<CPUExtended> myLazyCPU;


        /// <summary>
        /// Per core CPU Information
        /// </summary>
        public Dictionary<CPUNumber, CPUTopology> Topology { get; set; } = new();

        /// <summary>
        /// Returns true if CPU data is set and CPU has E-Cores
        /// </summary>
        [JsonIgnore]
        public bool HasECores
        {
            get => Topology != null && Topology.Count > 0 && Topology.Values.Any(x => x.EfficiencyClass > 0);
        }

        IReadOnlyDictionary<CPUNumber, ICPUTopology> myReadOnly;

        /// <summary>
        /// CPU Information
        /// </summary>
        IReadOnlyDictionary<CPUNumber, ICPUTopology> ICPUStats.Topology
        {
            get
            {
                if( myReadOnly == null )
                {
                    // We need to make a copy because we cannot cast the dictionary to the interface
                    var local = new Dictionary<CPUNumber, ICPUTopology>();
                    myReadOnly = local;
                    if (this.Topology != null)
                    {
                        foreach (var topology in ((CPUStats)this).Topology)
                        {
                            local[topology.Key] = topology.Value;
                        }
                    }
                }

                return myReadOnly;
            }
        }

        /// <summary>
        /// When -timeline was used during extraction we generate CPU timeline data.
        /// </summary>
        public CPUTimeLine TimeLine
        {
            get;
        } = new CPUTimeLine(0.0f);

        /// <summary>
        /// When -timeline was used during extraction we generate CPU timeline data.
        /// </summary>
        ICPUTimeLine ICPUStats.TimeLine => TimeLine;

        /// <summary>
        /// Needed to deserialize dependant Json files on access
        /// </summary>
        internal string DeserializedFileName { get;  set; }



        /// <summary>
        /// Ctor which fills the data. This is also used by Json.NET during deserialization.
        /// </summary>
        /// <param name="perProcessCPUConsumptionInMs"></param>
        /// <param name="perProcessAvgCPUPriority"></param>
        /// <param name="perProcessMethodCostsInclusive"></param>
        /// <param name="timeLine"></param>
        /// <param name="cpuInfos">CPU informations</param>
        /// <param name="extendedMetrics">Extended metrics</param>
        public CPUStats(Dictionary<ProcessKey, uint> perProcessCPUConsumptionInMs, Dictionary<ETWProcessIndex, float> perProcessAvgCPUPriority, CPUPerProcessMethodList perProcessMethodCostsInclusive, CPUTimeLine timeLine, Dictionary<CPUNumber, CPUTopology> cpuInfos, CPUExtended extendedMetrics)
        {
            PerProcessCPUConsumptionInMs = perProcessCPUConsumptionInMs;
            PerProcessAvgCPUPriority = perProcessAvgCPUPriority;   
            PerProcessMethodCostsInclusive = perProcessMethodCostsInclusive;
            TimeLine = timeLine;
            Topology = cpuInfos;
            ExtendedCPUMetrics = extendedMetrics;
            myLazyCPU = new Lazy<CPUExtended>(() =>
            {
                CPUExtended lret = null;
                if (DeserializedFileName != null)
                {
                    ExtractSerializer ser = new();
                    string file = ser.GetFileNameFor(DeserializedFileName, ExtractSerializer.ExtendedCPUPostFix);
                    if (File.Exists(file))
                    {
                        using var fileStream = ETWExtract.OpenFileReadOnly(file);
                        lret = ExtractSerializer.Deserialize<CPUExtended>(fileStream);
                    }
                }
                
                if( lret == null )
                {
                    lret = new();
                }

                return lret;
            });
        }
    }
}
