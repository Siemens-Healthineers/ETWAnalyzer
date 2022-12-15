using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Disk
{
    /// <summary>
    /// Defines how partion is formatted
    /// </summary>
    public enum FileSystemFormat
    {
        /// <summary>
        /// 
        /// </summary>
        Ntfs,

        /// <summary>
        /// Not formatted
        /// </summary>
        Raw,

        /// <summary>
        /// 
        /// </summary>
        Fat,

        /// <summary>
        /// 
        /// </summary>
        Fat32,

        /// <summary>
        /// 
        /// </summary>
        ExFat,

        /// <summary>
        /// 
        /// </summary>
        Cdfs,

        /// <summary>
        ///        
        /// </summary>
        Udf,

        /// <summary>
        /// 
        /// </summary>
        Csvfs,

        /// <summary>
        /// 
        /// </summary>
        ReFS
    }
}
