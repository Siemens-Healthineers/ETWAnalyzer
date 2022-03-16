//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.EventDump
{

    /// <summary>
    /// Used by <see cref="DumpCommand"/> what is dumped. The enum name is also used to parse the dump options
    /// </summary>
    public enum DumpCommands
    {
        /// <summary>
        /// Not set
        /// </summary>
        None = 0,

        /// <summary>
        /// Dump managed heap allocations
        /// </summary>
        Allocations,

        /// <summary>
        /// Dump TestRuns from a specific directory
        /// </summary>
        TestRuns = 500,

        /// <summary>
        /// Print all events by count
        /// </summary>
        Stats,

        /// <summary>
        /// Dump Module version of use ETL file
        /// </summary>
        Versions,

        /// <summary>
        /// Dump costs of methods of all files in a directory or an extracted json file
        /// </summary>
        CPU,

        /// <summary>
        /// Dump Exceptions
        /// </summary>
        Exceptions,

        /// <summary>
        /// Dump Memory statistics
        /// </summary>
        Memory,
        /// <summary>
        /// Dump Processes and command lines inside etl trace
        /// </summary>
        Process,

        /// <summary>
        /// Dump Disk IO data
        /// </summary>
        Disk,

        /// <summary>
        /// Dump File IO Data
        /// </summary>
        File,

        /// <summary>
        /// Dump .NET Threadpool starvation events
        /// </summary>
        ThreadPool,

        /// <summary>
        /// Dump ETW Marker events
        /// </summary>
        Mark,
    }
}
