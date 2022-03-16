//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{

    /// <summary>
    /// List of processes with stack tag entries which consume either CPU or have wait times
    /// </summary>
    public class ProcessStackTags : IProcessStackTags
    {
        /// <summary>
        /// Used input stacktag file. Helps to diagnose issues later
        /// </summary>
        public string[] UsedStackTagFiles { get; set; }

        /// <summary>
        /// Processes which contain stacktag data
        /// </summary>
        public List<KeyValuePair<ProcessKey, List<StackTagDuration>>> Stats { get; } = new List<KeyValuePair<ProcessKey, List<StackTagDuration>>>();

        IReadOnlyList<KeyValuePair<ProcessKey, IReadOnlyList<IStackTagDuration>>> StatsReadOnly;

        IReadOnlyList<KeyValuePair<ProcessKey, IReadOnlyList<IStackTagDuration>>> IProcessStackTags.Stats
        {
            get
            {
                if( StatsReadOnly == null && Stats != null)
                {
                    StatsReadOnly = Stats.Select(x => new KeyValuePair<ProcessKey, IReadOnlyList<IStackTagDuration>>(x.Key, new List<IStackTagDuration>(x.Value))).ToList();
                }

                return StatsReadOnly;
            }
        }
    }
}
