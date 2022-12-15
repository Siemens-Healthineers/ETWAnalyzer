using System.Collections.Generic;

namespace ETWAnalyzer.Extract.Disk
{
    /// <summary>
    /// Contains disk layout information and partition data
    /// </summary>
    public interface IDiskLayout
    {
        /// <summary>
        /// Capacity in GiB = 1024*1024*1024 bytes
        /// </summary>
        decimal CapacityGiB { get; }

        /// <summary>
        /// Cylinder count
        /// </summary>
        long CylinderCount { get; }

        /// <summary>
        /// When write cache is enabled writes are faster but it can cause data loss during an unexpected power off event
        /// </summary>
        bool IsWriteCachingEnabled { get; }

        /// <summary>
        /// Model Name
        /// </summary>
        string Model { get; }

        /// <summary>
        /// Partitions
        /// </summary>
        IReadOnlyList<IDiskPartition> Partitions { get; }

        /// <summary>
        /// Size per sector in bytes
        /// </summary>

        long SectorSizeBytes { get; }

        /// <summary>
        /// Sectors per Track
        /// </summary>
        int SectorsPerTrack { get; }

        /// <summary>
        /// Tracks per Cylinder
        /// </summary>
        int TracksPerCylinder { get; }

        /// <summary>
        ///  Disk Type (SSD or HDD)
        /// </summary>
        DiskTypes Type { get; }
    }
}