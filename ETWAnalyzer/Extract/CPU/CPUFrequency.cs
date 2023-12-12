using ETWAnalyzer.Extractors.CPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.CPU
{

    public class CPUInfo
    {
        public int NominalFrequencyMHz { get; set; }
        public int RelativePerformancePercentage { get; internal set; }
        public int EfficiencyClass { get; internal set; }
    }

    public enum CPUNumber
    {
        Invalid = -1,
    }

    public class CPUFrequency
    {
        public Dictionary<CPUNumber, CPUInfo> CPUInfos { get; set; } = new();

        public Dictionary<CPUNumber, CPUFrequencyDuration[]> FrequencyData {get;set; } = new ();

    }
}
