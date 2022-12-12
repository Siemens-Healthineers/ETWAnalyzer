namespace ETWAnalyzer.Extract.Disk
{
    /// <summary>
    /// Contains Partition size and drive letter
    /// </summary>
    public interface IDiskPartition
    {
        /// <summary>
        /// Drive letter as string
        /// </summary>
        string Drive { get; }

        /// <summary>
        /// File system which is used for the partition
        /// </summary>
        FileSystemFormat FileSystem { get; }

        /// <summary>
        /// Free size of partition in GiB (= 1024*1024*1024)
        /// </summary>
        decimal FreeSizeGiB { get; }

        /// <summary>
        /// Partition size in Gib (= 1024*1024*1024)
        /// </summary>
        decimal TotalSizeGiB { get; }
    }
}