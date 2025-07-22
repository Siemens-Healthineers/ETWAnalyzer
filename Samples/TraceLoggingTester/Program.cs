//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;

namespace TraceLoggingTester
{
    /// <summary>
    /// - Start profiling with WPR (Windows Performance Recorder) using a TraceLogging profile
    /// - Generate some tracelogging events and stop profiling.
    /// - Stop profiling and if an input argument is given use that as output file otherwise write to c:\temp\TraceLoggingTester.etl
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            string outFile = args.FirstOrDefault() ?? "C:\\temp\\TraceLoggingTester.etl";
            var p = Process.Start("wpr", $"-start {GetExeDirectory()}\\TraceLoggingProfile.wprp");

            p.WaitForExit();
            Console.WriteLine($"WPR started. Exit Code: {p.ExitCode}");


            const int NEvents = 10;
            Console.WriteLine($"TraceLogging: Writing {NEvents} events.");
            for (int i = 0; i < NEvents; i++)
            {

                TestSource.Log.MarkString("Hello, ETW!");
                TestSource.Log.MarkStringInteger("Hello, ETW with number", 42);
                TestSource.Log.MarkBoolByteCharDoubleInt16Int32Int64SByteUInt16UInt32UInt64(true,
                    (byte) i,
                    'A',
                    3.14,
                    -32768,
                    2147483647,
                    9223372036854775807L,
                    -128,
                    65535,
                    4294967295U,
                    18446744073709551615UL);
                TestSource.Log.WriteIntList(new[] { i, 2, 3, 4 });
                TestSource.Log.WriteDateTime(new DateTime(2000, 1, 1));
                TestSource.Log.WriteTimeSpan(TimeSpan.FromSeconds(42));
                TestSource.Log.WriteGuid(Guid.Parse("00000000-0000-0000-0000-000000000001"));

                /*  Not supported data types by .NET EventSourced API SocketAddress, IPAddress and SecurityIdentifier
                    But they are supported by TraceProcesing library
                  
                TestSource.Log.WriteSocketAddress(new SocketAddress(AddressFamily.InterNetwork, 6));
                // Set the bytes for the SocketAddress manually
                var sa = new SocketAddress(AddressFamily.InterNetwork, 6);
                sa[2] = 192;
                sa[3] = 168;
                sa[4] = 1;
                sa[5] = 1;
                sa[0] = 0;
                sa[1] = 0;
                TestSource.Log.WriteSocketAddress(sa);
                
                TestSource.Log.WriteIPAddress(IPAddress.Parse("127.0.0.1"));
                */
            }
            var p2 = Process.Start("wpr", $"-stop {outFile}");
            p2.WaitForExit();

            Console.WriteLine($"WPR stopped. Exit Code: {p2.ExitCode}, Output file: {outFile}");
        }

        static string GetExeDirectory()
        {
            return Path.GetDirectoryName(Environment.ProcessPath) ?? string.Empty;
        }
    }

    /// <summary>
    /// This is a test event source for ETW tracing using TraceLogging with various data types.
    /// </summary>
    [EventSource(Name = "CSharpTraceLoggingEventSource")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class TestSource : EventSource
    {
        /// <summary>
        /// API:No
        /// </summary>
        public TestSource() : base(EventSourceSettings.EtwSelfDescribingEventFormat| EventSourceSettings.ThrowOnEventWriteErrors)
        { }

        /// <summary>
        /// API:No
        /// Singleton instance of the EtwPerfMark event source. This is used to write marker strings to ETW.
        /// </summary>
        public static TestSource Log { get; } = new TestSource();

        public void MarkString(string input)
        {
            WriteEvent(1, input);
        }

        public void MarkStringInteger(string input, int number1)
        {
            WriteEvent(2, input, number1);
        }

        public void MarkBoolByteCharDoubleInt16Int32Int64SByteUInt16UInt32UInt64(
            bool boolean,
            byte b,
            char c,
            double d,
            short i16,
            int i32,
            long i64,
            sbyte sb,
            ushort u16,
            uint u32,
            ulong u64)
        {
            WriteEvent(3, boolean, b, c, d, i16, i32, i64, sb, u16, u32, u64);
        }

        public void WriteGuid(Guid guid)
        {
            WriteEvent(4, guid);
        }   

        public void WriteDateTime(DateTime dateTime)
        {
            WriteEvent(5, dateTime);
        }

        public void WriteTimeSpan(TimeSpan timeSpan)
        {
            WriteEvent(6, timeSpan);
        }

        public void WriteIntList(int[] numbers)
        {
            WriteEvent(7, numbers);
        }

        /* The API supports only anonymous types or types decorated with the EventDataAttribute. Non-compliant type: IPAddress dataType.
        public void WriteIPAddress(IPAddress address)
        {
            WriteEvent(8, address);
        }
        */

        /* It is not possible to write SecurityIdentifier directly using TraceLogging due to limitations in the EventSource API.
         * 
         * System.Diagnostics.Tracing.EventSourceException: An error occurred when writing to a listener.
             ---> System.ArgumentException: The API supports only anonymous types or types decorated with the EventDataAttribute. Non-compliant type: SecurityIdentifier dataType.
               at System.Diagnostics.Tracing.Statics.CreateDefaultTypeInfo(Type dataType, List`1 recursionCheck)
               at System.Diagnostics.Tracing.TraceLoggingTypeInfo.GetInstance(Type type, List`1 recursionCheck)
               at System.Diagnostics.Tracing.TraceLoggingEventTypes.MakeArray(ParameterInfo[] paramInfos)

        public unsafe void WriteSecurityIdentifier(SecurityIdentifier sid)
        {
           // WriteEvent(9, sid.ToString());
        }   
        */


        /* 
         It is not possible to write as SocketAddress directly using TraceLogging due to limitations in the EventSource API.
         System.Diagnostics.Tracing.EventSourceException: An error occurred when writing to a listener.
         ---> System.ArgumentException: The API supports only anonymous types or types decorated with the EventDataAttribute. Non-compliant type: SocketAddress dataType.
           at System.Diagnostics.Tracing.Statics.CreateDefaultTypeInfo(Type dataType, List`1 recursionCheck)
           at System.Diagnostics.Tracing.TraceLoggingTypeInfo.GetInstance(Type type, List`1 recursionCheck)

        public void WriteSocketAddress(SocketAddress socketAddress)
        {
            WriteEvent(10, socketAddress);
        }
         */
    }
}
