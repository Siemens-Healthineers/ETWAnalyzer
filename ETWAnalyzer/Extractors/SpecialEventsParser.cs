//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extract;
using ETWAnalyzer.Extract.Network;
using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Streaming;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ETWAnalyzer.Extractors
{

    /// <summary>
    /// Parses various key events like boot time, NIC information, etc. from ETL files that are not directly exposed by the TraceProcessing library.
    /// </summary>
    internal class SpecialEventsParser : IUnparsedEventConsumer
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

        public uint ProcessorGroups { get; private set; } 

        public string WinSatOSName { get; private set; }
        public string WinSatProcessorName { get; private set; } 

        public List<NetworkInterface> NetworkInterfaces
        {
            get;
            private set;
        } = new();


        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/etw/systemconfig
        /// </summary>
        static readonly Guid mySystemConfigGuid = new Guid("01853a65-418f-4f36-aefc-dc0f1d2fd235");

        /// <summary>
        /// WinSATAssessment Provider
        /// </summary>
        static readonly Guid myWinSatGuid = new Guid("ed54dff8-c409-4cf6-bf83-05e1e61a09c4");

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/etw/eventtraceevent
        /// </summary>
        static readonly Guid myEventTraceEventGuid = new Guid("68fdd900-4a3e-11d1-84f4-0000f80464e3");



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
        /// Get events out of ETL which are not exposed by TraceProcessing Library, such as boot time
        /// </summary>
        /// <param name="processor"></param>
        /// <exception cref="InvalidOperationException"></exception>
        internal void RegisterSpecialEvents(ITraceProcessor processor)
        {
            // Get Boot Time data. Code by https://stackoverflow.com/questions/61996791/expose-boottime-in-traceprocessor
            // We cannot use IGenericEvent because these are logged classic events 
            processor.UseStreaming().UseUnparsedEvents(this, new[] { myEventTraceEventGuid, mySystemConfigGuid, myWinSatGuid });
        }

        public void Process(TraceEvent eventData)
        {
            if (eventData.ProviderId == myEventTraceEventGuid)
            {
                ParseTraceEventHeader(eventData);
            }
            else if (eventData.ProviderId == mySystemConfigGuid)
            {
                ParseSystemConfig(eventData);
            }
            else if (eventData.ProviderId == myWinSatGuid)
            {
                ParseWinSatConfig(eventData);
            }
        }

        public void ProcessFailure(FailureInfo failureInfo)
        {
            failureInfo.ThrowAndLogParseFailure();
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

            const int SystemConfig_NIC = 13;
            const int SystemConfig_ProcessorGroup = 26;

            var span = e.Data;
            var ctx = new ParseContext()
            {
                Data = span,
                Offset = 0
            };


            switch (e.Id)
            {
                case SystemConfig_ProcessorGroup:
                    ProcessorGroups = ctx.ParseUInt32();
                    break;
                case SystemConfig_NIC:
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

                    if (!NetworkInterfaces.Contains(itf))
                    {
                        NetworkInterfaces.Add(itf);
                    }
                    break;
                default:
                    break;
            }
        }

        private void ParseWinSatConfig(TraceEvent e)
        {
            var classic = e.AsClassicEvent;
            const int WinSat_SystemConfig = 37;

            var span = e.Data;
            var ctx = new ParseContext()
            {
                Data = span,
                Offset = 0
            };

            switch (e.Id)
            {
                case WinSat_SystemConfig:
                    string winSatStr = GetWinSATXml(ref ctx);
                    WinSatOSName = GetOSNameFromWinSat(winSatStr);
                    WinSatProcessorName = GetProcessorNameFromWinSat(winSatStr);
                    break;
                default:
                    break;
            }

        }

        /// <summary>
        /// Get From WinSAT String the real OS Name which is retrieved from  KernelTraceControl/WinSat/SystemConfig ETW Event
        /// </summary>
        /// <param name="xmlStr">WinSAT data</param>
        /// <returns></returns>
        string GetOSNameFromWinSat(string xmlStr)
        {
            if( String.IsNullOrEmpty( xmlStr ) )
            {
                return null;
            }

            // <OSName><![CDATA[Windows Server 2019 Standard]] ></OSName>
            string lret = null;

            string startOSName = "<OSName>";
            // string endOSName = "</OSName>";
            string cDataStart = "![CDATA[";
            int startIdx = xmlStr.IndexOf(startOSName);
            if( startIdx > -1 )
            {
                startIdx = xmlStr.IndexOf(cDataStart, startIdx);
                if (startIdx > -1)
                {
                    startIdx += cDataStart.Length;
                    int endIdx = xmlStr.IndexOf("]", startIdx);
                    if (endIdx > -1)
                    {
                        lret = xmlStr.Substring(startIdx, endIdx - startIdx);
                    }
                }
            }

            return lret;
        }

        string GetProcessorNameFromWinSat(string xmlStr)
        {
            // <ProcessorName>
            string startNode = "<ProcessorName>";
            string endNode = "</ProcessorName>";
            int startIdx = xmlStr.IndexOf(startNode);
            if( startIdx  > 0 )
            {
                startIdx += startNode.Length;
                int endIdx = xmlStr.IndexOf(endNode, startIdx);
                if( endIdx > startIdx )
                {
                    return xmlStr.Substring(startIdx, endIdx - startIdx);
                }
            }

            return null;
        }

        /// <summary>
        /// The ETW event KernelTraceControl/WinSat/SystemConfig contains compressed XML data. Decompress it and prettify it. 
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>XML data or empty string.</returns>
        private unsafe string GetWinSATXml(ref ParseContext ctx)
        {
            ctx.Offset = 4;
            uint uncompressedSize = ctx.ParseUInt32();
            if (0x10000 <= uncompressedSize)
            {
                return "";
            }

            byte[] uncompressedData = new byte[uncompressedSize];
            Span<byte> data = uncompressedData;
            ctx.Offset = 8; // Skip header (State + UncompressedLength)
            int compressedSize = ctx.Data.Length - ctx.Offset; // Compressed size is total size minus header.

            int resultSize = 0;
            int hr = 0;
            fixed (byte* uncompressedPtr = &uncompressedData[0])
            {
                fixed (byte* compressedDataPtr = &ctx.Data[ctx.Offset])
                {
                    hr = RtlDecompressBuffer(
                    COMPRESSION_FORMAT_LZNT1 | COMPRESSION_ENGINE_MAXIMUM,
                    uncompressedPtr,
                    uncompressedSize,
                    compressedDataPtr,
                    compressedSize,
                    out resultSize);
                }
            }

            if (hr == 0 && resultSize == uncompressedSize)
            {
                var indent = 0;
                // PrettyPrint the XML
                StringBuilder sb = new StringBuilder();
                Span<char> uncompressedChars = MemoryMarshal.Cast<byte, char>(data);
                bool noChildren = true;
                for (int i = 0; i < uncompressedChars.Length; i++)
                {
                    char c = uncompressedChars[i];
                    if (c == 0)
                    {
                        break;  // we will assume null termination
                    }
                    if (c == '<')
                    {
                        var c1 = uncompressedChars[i + 1];
                        bool newLine = false;
                        if (c1 == '/')
                        {
                            newLine = !noChildren;
                            noChildren = false;
                        }
                        else if (Char.IsLetter(c1))
                        {
                            noChildren = true;
                            newLine = true;
                            indent++;
                        }
                        if (newLine)
                        {
                            sb.AppendLine();
                            for (int k = 0; k < indent; k++)
                            {
                                sb.Append(' ');
                            }

                        }
                        if (c1 == '/')
                        {
                            --indent;
                        }
                    }
                    sb.Append(c);
                }
                return sb.ToString();
            }

            return "";
        }

        // Used to decompress WinSat data 
        internal const int COMPRESSION_FORMAT_LZNT1 = 0x0002;
        internal const int COMPRESSION_ENGINE_MAXIMUM = 0x0100;
        [DllImport("ntdll.dll")]
        unsafe internal static extern int RtlDecompressBuffer(int CompressionFormat, byte* UncompressedBuffer, uint UncompressedBufferSize, byte* CompressedBuffer, int CompressedBufferSize, out int FinalUncompressedSize);
    }
}
