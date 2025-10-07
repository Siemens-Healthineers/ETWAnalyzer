//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// ETW Logger Mode defined in https://learn.microsoft.com/en-us/windows/win32/etw/logging-mode-constants
    /// </summary>
    [Flags]
    public enum LogFileModeEnum : uint
    {

        /// <summary>
        /// Logfile is off
        /// </summary>
        None = 0x00000000,

        /// <summary>
        /// Log sequentially
        /// </summary>
        FILE_MODE_SEQUENTIAL = 0x00000001,

        /// <summary>
        /// Log in circular manner
        /// </summary>
        FILE_MODE_CIRCULAR = 0x00000002,

        /// <summary>
        /// Append sequential log
        /// </summary>
        FILE_MODE_APPEND = 0x00000004,

        /// <summary>
        /// Auto-switch log file
        /// </summary>
        FILE_MODE_NEWFILE = 0x00000008,

        /// <summary>
        /// Pre-allocate mode
        /// </summary>
        FILE_MODE_PREALLOCATE = 0x00000020,

        /// <summary>
        /// Session cannot be stopped Autologger only 
        /// </summary>
        NONSTOPPABLE_MODE = 0x00000040,

        /// <summary>
        /// Real time mode on
        /// </summary>
        REAL_TIME_MODE = 0x00000100,

        /// <summary>
        /// Delay opening file
        /// </summary>
        DELAY_OPEN_FILE_MODE = 0x00000200,

        /// <summary>
        /// Buffering mode only
        /// </summary>
        BUFFERING_MODE = 0x00000400,

        /// <summary>
        /// Process Private Logger
        /// </summary>
        PRIVATE_LOGGER_MODE = 0x00000800,

        /// <summary>
        /// Add a logfile header
        /// </summary>
        ADD_HEADER_MODE = 0x00001000,

        /// <summary>
        /// Use KBytes as file size unit
        /// </summary>
        USE_KBYTES_FOR_SIZE = 0x00002000,

        /// <summary>
        /// Use global sequence no.
        /// </summary>
        USE_GLOBAL_SEQUENCE = 0x00004000,

        /// <summary>
        /// Use local sequence no.
        /// </summary>
        USE_LOCAL_SEQUENCE = 0x00008000,

        /// <summary>
        /// Relogger
        /// </summary>
        RELOG_MODE = 0x00010000,

        /// <summary>
        /// Reserved
        /// </summary>
        MODE_RESERVED = 0x00100000,

        /// <summary>
        /// Use pageable buffers  
        /// </summary>
        USE_PAGED_MEMORY = 0x01000000,

        /// <summary>
        /// 
        /// </summary>
        SYSTEM_LOGGER_MODE = 0x02000000,

        /// <summary>
        /// 
        /// </summary>
        INDEPENDENT_SESSION_MODE = 0x8000000,

        /// <summary>
        /// 
        /// </summary>
        NO_PER_PROCESSOR_BUFFERING  = 0x10000000,

        /// <summary>
        /// 
        /// </summary>
        ADDTO_TRIAGE_DUMP = 0x80000000,

        /// <summary>
        /// All defined values
        /// </summary>
        All = FILE_MODE_SEQUENTIAL | FILE_MODE_CIRCULAR | FILE_MODE_APPEND | FILE_MODE_NEWFILE | FILE_MODE_PREALLOCATE | NONSTOPPABLE_MODE | REAL_TIME_MODE | 
              DELAY_OPEN_FILE_MODE | BUFFERING_MODE | PRIVATE_LOGGER_MODE | ADD_HEADER_MODE | USE_KBYTES_FOR_SIZE | USE_GLOBAL_SEQUENCE | USE_LOCAL_SEQUENCE |
              RELOG_MODE | MODE_RESERVED | USE_PAGED_MEMORY | SYSTEM_LOGGER_MODE | INDEPENDENT_SESSION_MODE | NO_PER_PROCESSOR_BUFFERING |  ADDTO_TRIAGE_DUMP 
               ,
              
    }

    /// <summary>
    /// 
    /// </summary>
    public class TraceHeader
    {
        /// <summary>
        /// 
        /// </summary>
        public uint BufferSize { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public byte MajorVersion { get; set; }
        /// <summary>
        /// /
        /// </summary>
        public byte MinorVersion { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public byte SubVersion { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public byte SubMinorVersion { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public uint ProviderVersion { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public uint TimerResolution { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public uint MaximumFileSizeMB { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string LogFileMode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public uint BuffersWritten { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public uint EventsLost { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public uint BuffersLost { get; set; }
    }
}
