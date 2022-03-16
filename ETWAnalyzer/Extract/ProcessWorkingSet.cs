//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
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
    /// When the ETW trace contains sampled workingsetdata 
    /// </summary>
    public class ProcessWorkingSet
    {
        /// <summary>
        /// 
        /// </summary>
        public ulong WorkingSetInMiB
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public ulong CommitInMiB
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public ulong WorkingsetPrivateInMiB
        {
            get; set;
        }

        /// <summary>
        /// This is the size of file mapping data e.g. Page file or other file mapped data
        /// </summary>
        public ulong SharedCommitSizeInMiB
        {
            get;set;
        }

        /// <summary>
        /// 
        /// </summary>
        public ProcessKey Process
        {
            get; set;
        }

        /// <summary>
        /// 
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
            WorkingSetInMiB = (ulong) workingset.TotalMebibytes;
            WorkingsetPrivateInMiB = (ulong)workingsetPrivate.TotalMebibytes;
            CommitInMiB = (ulong)commit.TotalMebibytes;
            SharedCommitSizeInMiB = (ulong)sharedCommitSize.TotalMebibytes;
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
