using ETWAnalyzer.TraceProcessorHelpers;
using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors
{
    internal class SpecialEventsParser
    {
        public DateTime? BootTimeUTC { get; set; }

        /// <summary>
        /// Get events out of ETL which are not exposed by TraceProcessing Library, such as boot time
        /// </summary>
        /// <param name="processor"></param>
        /// <exception cref="InvalidOperationException"></exception>
        internal void RegisterSpecialEvents(ITraceProcessor processor)
        {
            // Get Boot Time data. Code by https://stackoverflow.com/questions/61996791/expose-boottime-in-traceprocessor
            Guid eventTraceProviderId = new Guid("68fdd900-4a3e-11d1-84f4-0000f80464e3");
            processor.Use(new[] { eventTraceProviderId }, e =>
            {
                if (e.Event.Id != 0 || e.Event.Version != 2)
                {
                    return;
                }

                var data = e.Event.Data;
                long rawBootTime;

                if (e.Event.Is32Bit)
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
                }

                // See https://docs.microsoft.com/en-us/windows/win32/api/evntrace/ns-evntrace-trace_logfile_header:
                // BootTime is ticks since midnight, January 1, 1601 and is apparently UTC (despite documentation to the
                // contrary).
                DateTime epoch = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                BootTimeUTC = epoch.AddTicks(rawBootTime);

                e.Cancel();
            });

        }

    }
}
