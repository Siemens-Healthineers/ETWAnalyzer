using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Disk
{
    /// <summary>
    /// Contains Partition size and drive letter
    /// </summary>
    public class DiskPartition : IDiskPartition
    {
        /// <summary>
        /// File system which is used for the partition
        /// </summary>
        public FileSystemFormat FileSystem { get; set; }

        /// <summary>
        /// Drive letter as string
        /// </summary>
        public string Drive { get; set; }

        /// <summary>
        /// Partition size in Gib (= 1024*1024*1024)
        /// </summary>
        public decimal TotalSizeGiB { get; set; }

        /// <summary>
        /// Free size of partition in GiB (= 1024*1024*1024)
        /// </summary>
        public decimal FreeSizeGiB { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Drive} {FileSystem} {TotalSizeGiB:F3} GiB, Free: {FreeSizeGiB:F3} GiB";
        }
    }
}
