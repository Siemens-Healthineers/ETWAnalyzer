using System;
using System.Collections.Generic;

namespace ETWAnalyzer.Extract.CPU
{
    /// <summary>
    /// Contains extracted CPU timeline data which can be used to e.g. graph summary data
    /// </summary>
    public interface ICPUTimeLine
    {
        /// <summary>
        /// Calculate CPU per process with this interval. The interval is set during exctation with -timeline dd where dd is seconds.
        /// </summary>
        float ExtractionInveralS { get;  }

        /// <summary>
        /// Get raw timeline data for a process. To gt a list of timepoints use the method <see cref="GetProcessTimeLineData(IETWExtract, ProcessKey, bool)"/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Raw timeline data which contains a flat array of CPU usage, but no timeoints.</returns>
        IProcessTimeLine this[ProcessKey key] { get; }

        /// <summary>
        /// Get from a process a list of timepoints and the CPU consumption.
        /// </summary>
        /// <param name="extract">ETW Extract is needed to determine when the timepoints are starting. The timeline tarts with <see cref="IETWExtract.SessionStart"/></param>
        /// <param name="key">Process key which is used to query other processes.</param>
        /// <param name="calculatePercentCPU">When true the returned list contains % CPU instead of CPU ms per interval.</param>
        /// <returns>List of timepoints and aggregated CPU consumption in ms since last timepoint. </returns>
        List<KeyValuePair<DateTimeOffset, decimal>> GetProcessTimeLineData(IETWExtract extract, ProcessKey key, bool calculatePercentCPU);
    }
}