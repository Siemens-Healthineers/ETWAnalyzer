using System;
using System.Collections.Generic;

namespace ETWAnalyzer.Extract.CPU
{
    /// <summary>
    /// Contains CPU timeline data for all processes from ETW Session start until end. CPU time is summed for each <see cref="CPUTimeLine.ExtractionInveralS"/>
    /// to allow other tools to e.g. chart this data.
    /// </summary>
    public class CPUTimeLine : ICPUTimeLine
    {
        /// <summary>
        /// CPU is summed for each process from with this interval.
        /// When interval is 0.0f no timeline data was extracted.
        /// </summary>
        public float ExtractionInveralS
        {
            get; set;
        }

        /// <summary>
        /// Per process CPU timelines
        /// </summary>
        public Dictionary<ProcessKey, ProcessTimeLine> ProcessTimeLines
        {
            get;
            set;
        } = new Dictionary<ProcessKey, ProcessTimeLine>();


        /// <summary>
        /// Get raw timeline data for a process. To gt a list of timepoints use the method <see cref="GetProcessTimeLineData(IETWExtract, ProcessKey, bool)"/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Raw timeline data which contains a flat array of CPU usage, but no timepoints.</returns>
        public IProcessTimeLine this[ProcessKey key] { get => ProcessTimeLines[key]; }

        /// <summary>
        /// Construct a CPUTimeLine object with given start time and interval
        /// </summary>
        /// <param name="extractionInveralS"></param>
        public CPUTimeLine(float extractionInveralS)
        {
            ExtractionInveralS = extractionInveralS;
        }


        /// <summary>
        /// Get from a process a list of timepoints and the CPU consumption.
        /// </summary>
        /// <param name="extract">ETW Extract is needed to determine when the timepoints are starting. The timeline tarts with <see cref="IETWExtract.SessionStart"/></param>
        /// <param name="key">Process key which is used to query other processes.</param>
        /// <param name="calculatePercentCPU">When true the returned list contains % CPU instead of CPU ms per interval.</param>
        /// <returns>List of timepoints and aggregated CPU consumption in ms since last timepoint. </returns>
        public List<KeyValuePair<DateTimeOffset, decimal>> GetProcessTimeLineData(IETWExtract extract, ProcessKey key, bool calculatePercentCPU)
        {
            ProcessTimeLine timeline = ProcessTimeLines[key];
            DateTimeOffset start = extract.SessionStart;
            List <KeyValuePair<DateTimeOffset, decimal>> lret = new();

            decimal totalCPUPerInterval = extract.NumberOfProcessors * ((decimal)ExtractionInveralS * 1000.0m);

            for (int i=0;i<timeline.CPUMs.Count;i++)
            {
                decimal cpuValue = calculatePercentCPU ? timeline.CPUMs[i]*100.0m/totalCPUPerInterval : timeline.CPUMs[i];
                lret.Add( new KeyValuePair<DateTimeOffset, decimal>(start + TimeSpan.FromSeconds((i + 1) * ExtractionInveralS), cpuValue) );
            }

            return lret;
        }
    }
}
