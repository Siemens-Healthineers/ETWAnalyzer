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
    /// Set File Security Events
    /// </summary>
    public class FileSetSecurityOperation
    {
        /// <summary>
        /// Time when File Security was set
        /// </summary>
        public List<DateTimeOffset> Times { get; set; } = new List<DateTimeOffset>();

        /// <summary>
        /// Return code what was the result. Only failed unique return codes are saved. When the List is null all calls were successful.
        /// </summary>
        public List<int> NtStatus { get; set; }

        /// <summary>
        /// Add a Security event this object. To save space in NtStatus array in serialized Json we store only failed NtStatus values ( NtStatus != 0 ).
        /// </summary>
        /// <param name="time"></param>
        /// <param name="ntStatus"></param>
        public void AddSecurityEvent(DateTimeOffset time, int ntStatus)
        {
            Times.Add(time);

            if( ntStatus != 0)
            {
                if( NtStatus == null)
                {
                    NtStatus = new List<int>();
                }

                FileOpenOperation.AddUniqueNtStatus(NtStatus, ntStatus);
            }
            
        }
    }
}
