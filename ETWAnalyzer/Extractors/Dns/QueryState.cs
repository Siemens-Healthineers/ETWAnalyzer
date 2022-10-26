//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;

namespace ETWAnalyzer.Extractors.Dns
{
    /// <summary>
    /// State object to capture DNS query state while collecting key events
    /// </summary>
    class QueryState
    {
        public ETWProcessIndex ProcessIndex { get; set; }
        public DateTimeOffset Start { get; set; }
        public TimeSpan Duration { get; set; }
        public bool TimedOut { get; set; }

        public List<string> DnsServerList { get; set; } = new List<string>();
        public string TimedOutServer { get; internal set; }
        public string AdapterName { get; internal set; }
    }
}
