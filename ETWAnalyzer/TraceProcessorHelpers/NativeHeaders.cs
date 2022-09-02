//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.TraceProcessorHelpers
{
    // https://docs.microsoft.com/en-us/windows/win32/api/evntrace/ns-evntrace-trace_logfile_header
    [StructLayout(LayoutKind.Sequential)]
    struct NativeTraceHeaderEvent64
    {
        public uint BufferSize;
        public byte MajorVersion;
        public byte MinorVersion;
        public byte SubVersion;
        public byte SubMinorVersion;
        public uint ProviderVersion;
        public uint NumberOfProcessors;
        public long EndTime;
        public uint TimerResolution;
        public uint MaximumFileSize;
        public uint LogFileMode;
        public uint BuffersWritten;
        public uint StartBuffers;
        public uint PointerSize;
        public uint EventsLost;
        public uint CpuSpeedInMHz;
        public ulong LoggerName;
        public ulong LogFileName;
        public NativeTimeZoneInformation TimeZone;
        public long BootTime;
        public long PerfFreq;
        public long StartTime;
        public uint ReservedFlags;
        public uint BuffersLost;
    }

    // https://docs.microsoft.com/en-us/windows/win32/api/evntrace/ns-evntrace-trace_logfile_header
    [StructLayout(LayoutKind.Sequential)]
    struct NativeTraceHeaderEvent32
    {
        public uint BufferSize;
        public byte MajorVersion;
        public byte MinorVersion;
        public byte SubVersion;
        public byte SubMinorVersion;
        public uint ProviderVersion;
        public uint NumberOfProcessors;
        public long EndTime;
        public uint TimerResolution;
        public uint MaximumFileSize;
        public uint LogFileMode;
        public uint BuffersWritten;
        public uint StartBuffers;
        public uint PointerSize;
        public uint EventsLost;
        public uint CpuSpeedInMHz;
        public uint LoggerName;
        public uint LogFileName;
        public NativeTimeZoneInformation TimeZone;
        public long BootTime;
        public long PerfFreq;
        public long StartTime;
        public uint ReservedFlags;
        public uint BuffersLost;
    }

    // https://docs.microsoft.com/en-us/windows/win32/api/timezoneapi/ns-timezoneapi-time_zone_information
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct NativeTimeZoneInformation
    {
        public int Bias;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] StandardName;

        public NativeSystemTime StandardDate;

        public int StandardBias;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] DaylightName;

        public NativeSystemTime DaylightDate;

        public int DaylightBias;
    }

    // https://docs.microsoft.com/en-us/windows/win32/api/minwinbase/ns-minwinbase-systemtime
    [StructLayout(LayoutKind.Sequential)]
    struct NativeSystemTime
    {
        public short wYear;
        public short wMonth;
        public short wDayOfWeek;
        public short wDay;
        public short wHour;
        public short wMinute;
        public short wSecond;
        public short wMilliseconds;
    }
}
