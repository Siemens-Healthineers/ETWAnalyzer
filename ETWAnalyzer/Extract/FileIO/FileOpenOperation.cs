//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.FileIO
{
    /// <summary>
    /// Open Operation
    /// </summary>
    public class FileOpenOperation
    {
        /// <summary>
        /// NTSTATUS Error Codes return by CreateFile.  Only failed unique return codes are saved. When the List is null all calls were successful.
        /// </summary>
        public List<int> NtStatus { get; set; }

        /// <summary>
        /// IO Duration summed across all threads in microseconds rounded to nearest value away from 0.
        /// </summary>
        public long Durationus { get; set; }

        /// <summary>
        /// Number of times the file was opened
        /// </summary>
        public long Count { get; set; }

        internal void AddUniqueNotSucceededNtStatus(List<int> ntStatusList)
        {
            if(ntStatusList != null)
            {
                foreach(var nt in ntStatusList)
                {
                    AddUniqueNotSucceededNtStatus(nt);
                }

            }
        }

        internal void AddUniqueNotSucceededNtStatus(int ntStatus)
        {
            if (ntStatus != 0)
            {
                if (NtStatus == null)
                {
                    NtStatus = new List<int>();
                }

                AddUniqueNtStatus(NtStatus, ntStatus);
            }
        }

        internal static void AddUniqueNtStatus(List<int> stati, int ntStatus)
        {
            if( !stati.Contains(ntStatus))
            {
                stati.Add(ntStatus);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string stati = String.Join(",", NtStatus?.Select(x => ((NtStatus)x).ToString()) ?? new string[] { "" } );

            return $"Count: {Count} Duration: {Durationus / 1000:N0} ms, Stati: {stati}";
        }
         
    }
}
