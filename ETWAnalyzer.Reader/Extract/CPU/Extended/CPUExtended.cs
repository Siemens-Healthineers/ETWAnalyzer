//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Newtonsoft.Json;
using System.Collections.Generic;

namespace ETWAnalyzer.Extract.CPU.Extended
{

    /// <summary>
    /// Contains CPU Frequency and P/E Core metrics
    /// </summary>
    public class CPUExtended : ICPUExtended
    {
        /// <summary>
        /// Sampled CPU Frequency data per core
        /// </summary>
        public Dictionary<CPUNumber, FrequencySource> CPUToFrequencyDurations { get; set; } = new();

        /// <summary>
        /// Extended CPU Data
        /// </summary>
        public List<CPUMethodData> MethodData { get; set; } = new();

        Dictionary<ProcessMethodIdx, ICPUMethodData> myMethodIndexToCPUMethodData;

        /// <summary>
        /// Per method metrics which for core usage and frequencies.  Key is <see cref="MethodIndex"/> enum which is an index to the method name in the <see cref="ICPUPerProcessMethodList.MethodNames"/> array.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyDictionary<ProcessMethodIdx, ICPUMethodData> MethodIndexToCPUMethodData 
        { 
            get
            {
                if( myMethodIndexToCPUMethodData == null)
                {
                    myMethodIndexToCPUMethodData = new();
                    foreach(CPUMethodData methodData in MethodData)
                    {
                        myMethodIndexToCPUMethodData[methodData.Index] = methodData;
                    }
                }

                return (IReadOnlyDictionary <ProcessMethodIdx, ICPUMethodData>) myMethodIndexToCPUMethodData;
            }
        }

        /// <summary>
        /// When true CPU frequency data was collected.
        /// </summary>
        [JsonIgnore]
        public bool HasFrequencyData { get => CPUToFrequencyDurations.Count > 0; }

        /// <summary>
        /// Get for a given core the sampled CPU Frequency at a given time which is present in ETL when Microsoft-Windows-Kernel-Processor-Power provider is enabled.
        /// </summary>
        /// <param name="nCore">Core for which to get the frequency.</param>
        /// <param name="timeS">Time in WPA trace Time in seconds since Session start for which you want to get the current time.</param>
        /// <returns>Average CPU Frequency in MHz which was sampled in 15-30ms time slices.</returns>
        public int GetFrequency(CPUNumber nCore, float timeS)
        {

            int lret = -1;

            if (CPUToFrequencyDurations.TryGetValue(nCore, out FrequencySource duration))
            {
                lret = duration.GetFrequency(timeS);
            }

            return lret;
        }

  

        /// <summary>
        /// Add average CPU frequency which was gathered from Microsoft-Windows-Kernel-Processor-Power ETW provider
        /// </summary>
        /// <param name="nCore">Core to update</param>
        /// <param name="startTimeS">Start time when frequency was measured in WPA session time.</param>
        /// <param name="endTimeS">End time for this frequency in WPA session time.</param>
        /// <param name="averageFrequencyMHz">Average frqeuency for this core.</param>
        internal void AddFrequencyDuration(CPUNumber nCore, float startTimeS, float endTimeS, int averageFrequencyMHz)
        {

            if (!CPUToFrequencyDurations.TryGetValue(nCore, out FrequencySource frequencies))
            {
                frequencies = new();
                CPUToFrequencyDurations[nCore] = frequencies;
            }

            frequencies.Add(startTimeS, endTimeS, averageFrequencyMHz);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="processIndex"></param>
        /// <param name="method"></param>
        /// <param name="data"></param>
        internal void AddMethodCostPerEfficiencyClass(ETWProcessIndex processIndex, MethodIndex method, CPUUsage[] data)
        {
            var newData = new CPUMethodData
            {
                CPUConsumption = data,
                Index = processIndex.Create(method),
            };
            MethodData.Add(newData);
        }



        /// <summary>
        /// After sorting method names we need to remap our method indices.
        /// </summary>
        /// <param name="oldToNewIndex"></param>
        internal void RemapMethodIndicies(Dictionary<MethodIndex, MethodIndex> oldToNewIndex)
        {
            ETWProcessIndex process;
            MethodIndex method;
            foreach (var m in MethodData)
            {
                process = m.Index.ProcessIndex();
                method = m.Index.MethodIndex();
                m.Index =  process.Create( oldToNewIndex[method] );
            }
        }

        internal void AddReadyMetrics(ETWProcessIndex processIdx, MethodIndex methodIndex, ReadyTimes ready)
        {
            ProcessMethodIdx procMethod = processIdx.Create(methodIndex);
            var existing = MethodData.Find(x => x.Index == procMethod);
            if (existing == null)
            {
                existing = new CPUMethodData { Index = procMethod };
                MethodData.Add(existing);   
            }
            existing.ReadyMetrics = ready;
        }
    }

    /// <summary>
    /// Contains extended CPU metrics per method. E.g. P/E Core CPU usage and average frequencies, ... 
    /// This class is serialized to Json file
    /// </summary>
    public class CPUMethodData : ICPUMethodData
    {
        /// <summary>
        /// Index to the method name in <see cref="ICPUPerProcessMethodList.MethodNames"/> and <see cref="IETWExtract.Processes"/> array.
        /// </summary>
        public ProcessMethodIdx Index { get; set; }

        /// <summary>
        /// CPU consumption summed across all cores for all supported CPU Efficiency classes (e.g. P/E Cores).
        /// </summary>
        public CPUUsage[] CPUConsumption { get; set; }

        ICPUUsage[] ICPUMethodData.CPUConsumption => CPUConsumption;

        /// <summary>
        /// Contains Ready time percentiles
        /// </summary>
        public ReadyTimes ReadyMetrics { get; set; }

        /// <summary>
        /// Contains Ready time percentiles
        /// </summary>
        IReadyTimes ICPUMethodData.ReadyMetrics => ReadyMetrics;
    }

    /// <summary>
    /// CPU Number as type safe enum
    /// </summary>
    public enum CPUNumber
    {
        /// <summary>
        /// Not a CPU number
        /// </summary>
        Invalid = -1,
    }

    /// <summary>
    /// Efficiency classes start at zero with the most power efficient core types. The highest number is for the fastest
    /// (e.g. P) cores.
    /// </summary>
    public enum EfficiencyClass
    {
        /// <summary>
        /// Not a CPU efficiency class
        /// </summary>
        Invalid = -1,
    }

    /// <summary>
    /// Combined process and method index. Upper 40 bit are <see cref="ETWProcessIndex"/>, lower 24 bit are <see cref="MethodIndex"/>.
    /// </summary>
    public enum ProcessMethodIdx : long
    {
        /// <summary>
        /// Invalid Process and Method Index
        /// </summary>
        Invalid = -1,
    }

    /// <summary>
    /// Contains extended CPU metrics per method. E.g. P/E Core CPU usage and average frequencies, ... 
    /// </summary>
    public interface ICPUMethodData
    {
        /// <summary>
        /// CPU consumption summed across all cores for all supported CPU Efficiency classes (e.g. P/E Cores).
        /// </summary>
        ICPUUsage[] CPUConsumption { get;  }

        /// <summary>
        /// Percentile Ready Metrics 
        /// </summary>
        IReadyTimes ReadyMetrics { get;  }

        /// <summary>
        /// Combined Index to the method name in <see cref="ICPUPerProcessMethodList.MethodNames"/> and <see cref="IETWExtract.Processes"/> arrays.
        /// </summary>
        ProcessMethodIdx Index { get; }
    }

    /// <summary>
    /// CPU usage for a given method summed accross all cores for all CPUs inside one efficiency class. This is serialized to JSON file
    /// </summary>
    public class CPUUsage : ICPUUsage
    {
        /// <summary>
        /// Metric is for all cores in this core efficiency class.
        /// </summary>
        public EfficiencyClass EfficiencyClass { get; set; }

        /// <summary>
        /// Weighted Average Frequency for all cores in one efficiency class. 
        /// </summary>
        public int AverageMHz { get; set; }

        /// <summary>
        /// Consumed CPU time in ms
        /// </summary>
        public int CPUMs { get; set; }

        /// <summary>
        /// Number of used cores of this efficiency class.
        /// </summary>
        public int UsedCores { get; set; }

        /// <summary>
        /// Average number of enabled CPUs based on CPU Affinity mask for all threads in this process for this method.
        /// </summary>
        public long EnabledCPUsAvg { get; set; }

        /// <summary>
        /// Time in seconds since trace start this method was first seen on any CPU of this efficiency class.
        /// The data is taken from CPU sampling or, if not present, from context switch data which may not be accurate.
        /// You need to know your use case if you can trust these numbers. If the method is very fast ( &lt; 1 ms ) then
        /// you should pick another method which consumes more CPU to get reliable numbers. 
        /// </summary>
        public float FirstS { get; set; }

        /// <summary>
        /// Time in seconds since trace start this method was lat seen on any CPU of this efficiency class. 
        /// The data is taken from CPU sampling or, if not present, from context switch data which may not be accurate.
        /// You need to know your use case if you can trust these numbers. If the method is very fast ( &lt; 1 ms ) then
        /// you should pick another method which consumes more CPU to get reliable numbers. 
        /// </summary>
        public float LastS { get; set; }

#if DEBUG
        /// <summary>
        /// Used for debugging
        /// </summary>
        public string Debug { get; set; }
#endif
    }

    /// <summary>
    /// CPU usage for a given method summed accross all cores for all CPUs inside one efficiency class.
    /// </summary>
    public interface ICPUUsage
    {
        /// <summary>
        /// Weighted Average Frequency for all cores in one efficiency class. 
        /// </summary>
        int AverageMHz {  get;  }

        /// <summary>
        /// Consumed CPU time in ms
        /// </summary>
        int CPUMs {  get;  }

        /// <summary>
        /// Metric is for all cores in this core efficiency class.
        /// </summary>
        EfficiencyClass EfficiencyClass {  get;  }

        /// <summary>
        /// Number of used cores of this efficiency class.
        /// </summary>
        int UsedCores { get; }

        /// <summary>
        /// Average number of enabled CPUs based on CPU Affinity mask for all threads in this process for this method.
        /// </summary>
        public long EnabledCPUsAvg { get; }

        /// <summary>
        /// Time in seconds since trace start this method was first seen on any CPU of this efficiency class.
        /// The data is taken from CPU sampling or, if not present, from context switch data which may not be accurate.
        /// You need to know your use case if you can trust these numbers. If the method is very fast ( &lt; 1 ms ) then
        /// you should pick another method which consumes more CPU to get reliable numbers. 
        /// </summary>
        public float FirstS { get; }

        /// <summary>
        /// Time in seconds since trace start this method was lat seen on any CPU of this efficiency class. 
        /// The data is taken from CPU sampling or, if not present, from context switch data which may not be accurate.
        /// You need to know your use case if you can trust these numbers. If the method is very fast ( &lt; 1 ms ) then
        /// you should pick another method which consumes more CPU to get reliable numbers. 
        /// </summary>
        public float LastS { get; }
    }

    /// <summary>
    /// Contains CPU Frequency and P/E Core metrics
    /// </summary>
    public interface ICPUExtended
    {
        /// <summary>
        /// Per method metrics which for core usage and frequencies.  Key is <see cref="MethodIndex"/> enum which is an index to the method name in the <see cref="ICPUPerProcessMethodList.MethodNames"/> array.
        /// </summary>
        IReadOnlyDictionary<ProcessMethodIdx, ICPUMethodData> MethodIndexToCPUMethodData { get; }

        /// <summary>
        /// Get for a given core the sampled CPU Frequency at a given time which is present in ETL when Microsoft-Windows-Kernel-Processor-Power provider is enabled.
        /// </summary>
        /// <param name="nCore">Core for which to get the frequency.</param>
        /// <param name="timeS">Time in WPA trace Time in seconds since Session start for which you want to get the current time.</param>
        /// <returns>Average CPU Frequency in MHz which was sampled in 15-30ms time slices.</returns>
        int GetFrequency(CPUNumber nCore, float timeS);

        /// <summary>
        /// When true CPU frequency data was collected.
        /// </summary>
        public bool HasFrequencyData { get; }
    }

 
}
