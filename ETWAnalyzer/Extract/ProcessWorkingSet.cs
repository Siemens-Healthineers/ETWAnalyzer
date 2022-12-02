//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Processes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// When the ETW trace contains sampled workingset data 
    /// </summary>
    public class ProcessWorkingSet
    {
        /// <summary>
        /// Working Set in MiB = bytes/(1024*1024) rounded to next bigger MiB when greater x.5
        /// </summary>
        public ulong WorkingSetInMiB
        {
            get; set;
        }

        /// <summary>
        /// Committed memory in MiB  = bytes/(1024*1024) rounded to next bigger MiB when greater x.5
        /// </summary>
        public ulong CommitInMiB
        {
            get; set;
        }

        /// <summary>
        /// Process private working set in MiB = bytes/(1024*1024) rounded to next bigger MiB when greater x.5
        /// </summary>
        public ulong WorkingsetPrivateInMiB
        {
            get; set;
        }

        /// <summary>
        /// This is the size of file mapping data e.g. Page file or other file mapped data
        /// in MiB = bytes/(1024*1024) rounded to next bigger MiB when greater x.5
        /// </summary>
        public ulong SharedCommitSizeInMiB
        {
            get;set;
        }

        /// <summary>
        /// Process for which the data was gathered.
        /// </summary>
        public ProcessKey Process
        {
            get; set;
        }

        /// <summary>
        /// Potentially needed by some serializer to create an empty object
        /// </summary>
        public ProcessWorkingSet()
        {

        }

        internal ProcessWorkingSet(IProcess process, DataSize commit, DataSize workingset, DataSize workingsetPrivate, DataSize sharedCommitSize)
        {
            if( process == null )
            {
                throw new ArgumentNullException(nameof(process));
            }
            DateTimeOffset createTime = default;
            if( process.CreateTime.HasValue )
            {
                createTime = process.CreateTime.Value.DateTimeOffset;
            }
            Process = new ProcessKey(process.ImageName, process.Id, createTime);

            // round x.5 to next number to reduce the error
            WorkingSetInMiB =        (ulong) Math.Round(workingset.TotalMebibytes,        0, MidpointRounding.AwayFromZero);
            WorkingsetPrivateInMiB = (ulong) Math.Round(workingsetPrivate.TotalMebibytes, 0, MidpointRounding.AwayFromZero);
            CommitInMiB =            (ulong) Math.Round(commit.TotalMebibytes,            0, MidpointRounding.AwayFromZero);
            SharedCommitSizeInMiB =  (ulong) Math.Round(sharedCommitSize.TotalMebibytes,  0, MidpointRounding.AwayFromZero);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Process.Name}({Process.Pid}) Commit: {CommitInMiB} MiB WorkingSet {WorkingSetInMiB} MiB, WorkingSetPrivate {WorkingsetPrivateInMiB} MiB, SharedCommit {SharedCommitSizeInMiB} MiB";
        }
    }
}
