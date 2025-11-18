using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Disk
{
    /// <summary>
    /// Contains disk layout information and partition data
    /// </summary>
    public class DiskLayout : IDiskLayout
    {
        /// <summary>
        ///  Disk Type (SSD or HDD)
        /// </summary>
        public DiskTypes Type { get; set; }

        /// <summary>
        /// Model Name
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Partitions
        /// </summary>
        public List<DiskPartition> Partitions { get; set; } = new();

        /// <summary>
        /// Partitions
        /// </summary>
        IReadOnlyList<IDiskPartition> IDiskLayout.Partitions
        {
            get { return Partitions; }
        }

        /// <summary>
        /// Capacity in GiB = 1024*1024*1024 bytes
        /// </summary>
        public decimal CapacityGiB { get; set; }

        /// <summary>
        /// Cylinder count
        /// </summary>
        public long CylinderCount { get; set; }


        /// <summary>
        /// Tracks per Cylinder
        /// </summary>
        public int TracksPerCylinder { get; set; }

        /// <summary>
        /// Sectors per Track
        /// </summary>
        public int SectorsPerTrack { get; set; }

        /// <summary>
        /// Size per sector in bytes
        /// </summary>
        public long SectorSizeBytes { get; set; }

        /// <summary>
        /// When write cache is enabled writes are faster but it can cause data loss during an unexpected power off event
        /// </summary>
        public bool IsWriteCachingEnabled { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Model}, Capacity: {CapacityGiB:F3} GiB, SectorSizeBytes: {SectorSizeBytes}, Partitions: {Partitions.Count}";
        }
    }
}
