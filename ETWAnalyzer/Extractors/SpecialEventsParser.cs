//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Network;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ETWAnalyzer.Extractors
{

    /// <summary>
    /// Parses various key events like boot time, NIC information, etc. from ETL files that are not directly exposed by the TraceProcessing library.
    /// </summary>
    internal class SpecialEventsParser
    {
        public DateTime? BootTimeUTC { get; private set; }
        public uint BufferSize { get; private set; }
        public byte MajorVersion { get; private set; }
        public byte MinorVersion { get; private set; }
        public byte SubVersion { get; private set; }
        public byte SubMinorVersion { get; private set; }
        public uint ProviderVersion { get; private set; }
        public uint TimerResolution { get; private set; }
        public uint MaximumFileSizeMB { get; private set; }
        public LogFileModeEnum LogFileMode { get; private set; }
        public uint BuffersWritten { get; private set; }
        public uint EventsLost { get; private set; }
        public ulong LoggerName { get; private set; }
        public ulong LogFileName { get; private set; }
        public uint BuffersLost { get; private set; }

        public List<NetworkInterface> NetworkInterfaces
        {
            get;
            private set;
        } = new();

        public string LogFileModeNice
        {
            get
            {
                List<string> values = new List<string>();
                for (int i=0;i<32;i++)
                {
                    uint v = (uint) (1 << i);
                    LogFileModeEnum definedValue = (LogFileModeEnum)v & LogFileModeEnum.All;
                    if ( definedValue != LogFileModeEnum.None)
                    {
                        if( (definedValue & LogFileMode) != LogFileModeEnum.None)
                        {
                            values.Add(definedValue.ToString());
                        }
                    }
                    else
                    {
                        if( ( v & (uint) LogFileMode ) != 0 ) // undefined value set not covered by enum
                        {
                            values.Add( "0x" + v.ToString("X"));
                        }
                    }
                    
                }
                return string.Join(",", values);
            }
        }

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/etw/systemconfig
        /// </summary>
        static readonly Guid mySystemConfigGuid = new Guid("01853a65-418f-4f36-aefc-dc0f1d2fd235");

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/etw/eventtraceevent
        /// </summary>
        static readonly Guid myEventTraceEventGuid = new Guid("68fdd900-4a3e-11d1-84f4-0000f80464e3");

        /// <summary>
        /// Get events out of ETL which are not exposed by TraceProcessing Library, such as boot time
        /// </summary>
        /// <param name="processor"></param>
        /// <exception cref="InvalidOperationException"></exception>
        internal void RegisterSpecialEvents(ITraceProcessor processor)
        {
            // Get Boot Time data. Code by https://stackoverflow.com/questions/61996791/expose-boottime-in-traceprocessor
            // We cannot use IGenericEvent because these are logged classic events 
            processor.Use(new[] { myEventTraceEventGuid, mySystemConfigGuid }, e =>
            {
                if (e.Event.ProviderId == myEventTraceEventGuid)
                {
                    ParseTraceEventHeader(e.Event);
                }else if( e.Event.ProviderId == mySystemConfigGuid)
                {
                    ParseSystemConfig(e.Event);
                }
            });
        }

        /// <summary>
        /// Parse the SystemConfig events to extract network interface information.
        /// </summary>
        /// <param name="e"></param>
        private void ParseSystemConfig(TraceEvent e)
        {
            if (e.Version != 2)
            {
                return;
            }

            var classic = e.AsClassicEvent;

            /* https://learn.microsoft.com/en-us/windows/win32/etw/systemconfig-nic
              [EventType(13), EventTypeName("NIC")]
                class SystemConfig_NIC : SystemConfig
                {
                  uint64 PhysicalAddr;
                  uint32 PhysicalAddrLen;
                  uint32 Ipv4Index;
                  uint32 Ipv6Index;
                  string NICDescription;
                  string IpAddresses;
                  string DnsServerAddresses;
                };
            */

            const int SystemConfig_NIC = 13;

            if ( classic.Id == SystemConfig_NIC)
            {
                var span = e.Data;
                var ctx = new ParseContext()
                {
                    Data = span,
                    Offset = 0
                };

                ulong physicalAddr = ctx.ParseUInt64();
                UInt32 physicalAddrLen = ctx.ParseUInt32();
                // Convert PhysicalAddr to IPv4 or IPv6 string representation
                string macAddres = string.Empty;
                if (physicalAddrLen == 0) // No address set
                {
                    macAddres = "0:0:0:0:0:0";
                }
                else
                {
                    macAddres = BitConverter.ToString(BitConverter.GetBytes(physicalAddr), 0, (int)physicalAddrLen);
                }


                UInt32 ipv4Index = ctx.ParseUInt32();
                UInt32 ipv6Index = ctx.ParseUInt32();
                string nicDescription = ctx.ReadNullTerminatedUTF16String();
                string ipAddresses = ctx.ReadNullTerminatedUTF16String();
                string dnsServerAddresses = ctx.ReadNullTerminatedUTF16String();

                var itf = new NetworkInterface(
                    ipAddresses,
                    dnsServerAddresses,
                    nicDescription,
                    macAddres,
                    (int)ipv4Index,
                    (int)ipv6Index
                );

                if( !NetworkInterfaces.Contains(itf))
                {
                    NetworkInterfaces.Add(itf);
                }   
            }
        }



     
        private void ParseTraceEventHeader(TraceEvent e)
        {
            if (e.Id != 0 || e.Version != 2)
            {
                return;
            }

            var data = e.Data;
            long rawBootTime;

            if (e.Is32Bit)
            {
                if (data.Length < Marshal.SizeOf<NativeTraceHeaderEvent32>())
                {
                    throw new InvalidOperationException("Invalid 32-bit trace header event.");
                }

                // FYI - Inefficient / lots of copies, but doesn't require compiling with /unsafe.
                IntPtr pointer = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data.ToArray(), 0, pointer, data.Length);
                NativeTraceHeaderEvent32 typedData = Marshal.PtrToStructure<NativeTraceHeaderEvent32>(pointer);
                Marshal.FreeHGlobal(pointer);
                rawBootTime = typedData.BootTime;

                BufferSize = typedData.BufferSize;
                BuffersLost = typedData.BuffersLost;
                BuffersWritten = typedData.BuffersWritten;
                EventsLost = typedData.EventsLost;
                LogFileMode = (LogFileModeEnum)typedData.LogFileMode;
                LogFileName = typedData.LogFileName;
                LoggerName = typedData.LoggerName;
                MajorVersion = typedData.MajorVersion;
                MinorVersion = typedData.MinorVersion;
                SubVersion = typedData.SubVersion;
                SubMinorVersion = typedData.SubMinorVersion;
                MaximumFileSizeMB = typedData.MaximumFileSize;
                ProviderVersion = typedData.ProviderVersion;
                TimerResolution = typedData.TimerResolution;
            }
            else
            {
                if (data.Length < Marshal.SizeOf<NativeTraceHeaderEvent64>())
                {
                    throw new InvalidOperationException("Invalid 64-bit trace header event.");
                }

                // FYI - Inefficient / lots of copies, but doesn't require compiling with /unsafe.
                IntPtr pointer = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data.ToArray(), 0, pointer, data.Length);
                NativeTraceHeaderEvent64 typedData = Marshal.PtrToStructure<NativeTraceHeaderEvent64>(pointer);
                Marshal.FreeHGlobal(pointer);
                rawBootTime = typedData.BootTime;

                BufferSize = typedData.BufferSize;
                BuffersLost = typedData.BuffersLost;
                BuffersWritten = typedData.BuffersWritten;
                EventsLost = typedData.EventsLost;
                LogFileMode = (LogFileModeEnum)typedData.LogFileMode;
                LogFileName = typedData.LogFileName;
                LoggerName = typedData.LoggerName;
                MajorVersion = typedData.MajorVersion;
                MinorVersion = typedData.MinorVersion;
                SubVersion = typedData.SubVersion;
                SubMinorVersion = typedData.SubMinorVersion;
                MaximumFileSizeMB = typedData.MaximumFileSize;
                ProviderVersion = typedData.ProviderVersion;
                TimerResolution = typedData.TimerResolution;
            }

            // See https://docs.microsoft.com/en-us/windows/win32/api/evntrace/ns-evntrace-trace_logfile_header:
            // BootTime is ticks since midnight, January 1, 1601 and is apparently UTC (despite documentation to the
            // contrary).
            DateTime epoch = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            BootTimeUTC = epoch.AddTicks(rawBootTime);
        }

    }
}
