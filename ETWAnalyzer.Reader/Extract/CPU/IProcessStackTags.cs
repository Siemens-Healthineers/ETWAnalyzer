//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System.Collections.Generic;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Get summary information per process about CPU consumption of specifically tagged stacks.
    /// This contains full CPU information. No CPU cutoff is performed here. 
    /// </summary>
    public interface IProcessStackTags
    {
        /// <summary>
        /// Get per process stacktag summary
        /// </summary>
        IReadOnlyList<KeyValuePair<ProcessKey, IReadOnlyList<IStackTagDuration>>> Stats { get; }

        /// <summary>
        /// One or more used stacktag files which were used to generate the stacktags
        /// </summary>
        string[] UsedStackTagFiles { get; set; }
    }
}