//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers.Infrastructure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.FileIO
{
    /// <summary>
    /// Contains File IO data for a process
    /// </summary>
    public class FileIOStatistics
    {
        [Flags]
        internal enum FileOperation
        {
            Invalid = 0,
            Read = 1,
            Write = 2,
            Open = 4,
            Close = 8,
            SetSecurity = 16,
            Delete = 32,
            Rename = 64,
        }

        /// <summary>
        /// File open Count and Duration summed across all threads in a process. Additionally all return codes from all open operations are stored.
        /// The duration can be interesting when it is high and non overlapped IO was used. Overlapped IO/Completion port IO includes queuing time.
        /// </summary>
        public FileOpenOperation Open
        {
            get; set;
        }

        /// <summary>
        /// Close Count and Duration summed across all threads in a process
        /// </summary>
        public FileCloseOperation Close
        {
            get; set;
        }

        /// <summary>
        /// Bytes read of this process for current file
        /// </summary>
        public FileOffsetOperation Read
        {
            get; set;
        }

        /// <summary>
        /// Bytes written by this process for current file
        /// </summary>
        public FileOffsetOperation Write
        {
            get; set;
        }

        /// <summary>
        /// File Set Security Calls
        /// </summary>
        public FileSetSecurityOperation SetSecurity
        {
            get;set;
        }

        /// <summary>
        /// File deletion calls
        /// </summary>
        public FileDeleteOperation Delete
        {
            get;set;
        }

        /// <summary>
        /// File Rename operations
        /// </summary>
        public FileRenameOperation Rename
        {
            get;set;
        }
        
        internal bool HasOperation(FileOperation operation)
        {
            bool lret = false;
            if( (operation & FileOperation.Close) == FileOperation.Close && Close != null)
            {
                lret = true;
            }
            if( (operation & FileOperation.Open) == FileOperation.Open && Open != null)
            {
                lret = true;
            }
            if( (operation & FileOperation.Read) == FileOperation.Read && Read != null)
            {
                lret = true;
            }
            if( (operation & FileOperation.Write) == FileOperation.Write && Write != null)
            {
                lret = true;
            }
            if( (operation & FileOperation.SetSecurity) == FileOperation.SetSecurity && SetSecurity != null)
            {
                lret = true;
            }
            if( (operation & FileOperation.Delete) == FileOperation.Delete && Delete != null )
            {
                lret = true;
            }
            if( (operation & FileOperation.Rename) == FileOperation.Rename && Rename != null )
            {
                lret = true;
            }
            
            return lret;
        }

        internal void Add(FileIOStatistics other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if( other.Write != null)
            {
                if (Write == null)
                {
                    Write = new FileOffsetOperation();
                }

                Merge(Write, other.Write);
            }

            if( other.Read != null)
            {
                if (Read == null)
                {
                    Read = new FileOffsetOperation();
                }

                Merge(Read, other.Read);
            }

            if( other.Open != null)
            {
                if(Open == null)
                {
                    Open = new FileOpenOperation();
                }

                Merge(Open, other.Open);
            }

            if( other.Close != null)
            {
                if( Close == null)
                {
                    Close = new FileCloseOperation();
                }

                Merge(Close, other.Close);
            }

            if( other.SetSecurity != null )
            {
                if( SetSecurity == null )
                {
                    SetSecurity = new FileSetSecurityOperation();
                }

                Merge(SetSecurity, other.SetSecurity);
            }

            if( other.Delete != null)
            {
                if( Delete == null)
                {
                    Delete = new FileDeleteOperation();
                }
                Merge(Delete, other.Delete);
            }

            if( other.Rename != null)
            {
                if( Rename == null)
                {
                    Rename = new FileRenameOperation(); 
                }
                Merge(Rename, other.Rename);
            }
        }

        private void Merge(FileRenameOperation existing, FileRenameOperation toAdd)
        {
            if (toAdd == null)
            {
                throw new ArgumentNullException(nameof(toAdd));
            }

            existing.Count += toAdd.Count;
        }

        private void Merge(FileDeleteOperation existing, FileDeleteOperation toAdd)
        {
            if( toAdd == null )
            {
                throw new ArgumentNullException(nameof(toAdd));
            }

            existing.Count += toAdd.Count;
        }

        private void Merge(FileSetSecurityOperation existing, FileSetSecurityOperation toAdd)
        {
            if( toAdd.Times == null )
            {
                throw new ArgumentException($"{nameof(toAdd.Times)} was null");
            }

            for(int i=0;i<toAdd.Times.Count;i++)
            {
                int ntStatus = 0;
                if( toAdd.NtStatus != null)
                {
                    ntStatus = toAdd.NtStatus[i];
                }
                existing.AddSecurityEvent(toAdd.Times[i], ntStatus);
            }
        }

        void Merge(FileOffsetOperation existing, FileOffsetOperation toAdd)
        {
            existing.MaxFilePosition = Math.Max(existing.MaxFilePosition, toAdd.MaxFilePosition);
            existing.AccessedBytes += toAdd.AccessedBytes;
            existing.Durationus += toAdd.Durationus;
            existing.Count += toAdd.Count;
        }

        void Merge(FileOpenOperation existing, FileOpenOperation toAdd)
        {
            // store only unique return codes
            existing.AddUniqueNotSucceededNtStatus(toAdd.NtStatus);
            existing.Durationus += toAdd.Durationus;
            existing.Count += toAdd.Count;
        }

        private void Merge(FileCloseOperation existing, FileCloseOperation toAdd)
        {
            existing.Durationus += toAdd.Durationus;
            existing.Cleanups += toAdd.Cleanups;
            existing.Count += toAdd.Count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Written: {Write?.AccessedBytes:N0} bytes Read: {Read?.AccessedBytes:N0} bytes Opened: {Open?.Count:N0} Closed: {Close?.Count} Duration: {(Write?.Durationus ?? 0 + Read?.Durationus ?? 0 + Open?.Durationus ?? 0 + Close?.Durationus ?? 0)/1000:N0} ms";
        }
    }
}
