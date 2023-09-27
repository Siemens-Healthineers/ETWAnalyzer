//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// When the ETW trace contains sampled workingset data 
    /// </summary>
    public interface IProcessWorkingSet
    {
        /// <summary>
        /// Committed memory in MiB  = bytes/(1024*1024) rounded to next bigger MiB when greater x.5
        /// </summary>
        ulong CommitInMiB { get; }

        /// <summary>
        /// Process for which the data was gathered.
        /// </summary>
        ProcessKey Process { get; }

        /// <summary>
        /// This is the size of file mapping data e.g. Page file or other file mapped data
        /// in MiB = bytes/(1024*1024) rounded to next bigger MiB when greater x.5
        /// </summary>
        ulong SharedCommitSizeInMiB { get; }

        /// <summary>
        /// Working Set in MiB = bytes/(1024*1024) rounded to next bigger MiB when greater x.5
        /// </summary>
        ulong WorkingSetInMiB { get; }

        /// <summary>
        /// Process private working set in MiB = bytes/(1024*1024) rounded to next bigger MiB when greater x.5
        /// </summary>
        ulong WorkingsetPrivateInMiB { get; }
    }
}