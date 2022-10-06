using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors.Dns
{
    class QueryState
    {
        public ETWProcessIndex ProcessIndex { get; set; }
        public DateTimeOffset Start { get; set; }
        public TimeSpan Duration { get; set; }
        public bool TimedOut { get; set; }

        public List<string> DnsServerList { get; set; } = new List<string>();
        public string DnsServer { get; internal set; }
        public string AdapterName { get; internal set; }
    }
}
