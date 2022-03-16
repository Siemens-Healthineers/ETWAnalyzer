//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.Configuration;
using ETWAnalyzer.Converters;
using ETWAnalyzer.Extract;
using ETWAnalyzer.ProcessTools;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Stacks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ETWAnalyzer.Commands
{
    /// <summary>
    /// Processes -convert command line options.Constructed by <see cref="CommandFactory"/> if the arguments contain -convert.
    /// </summary>
    class ConvertCommand : ArgParser
    {
        static internal string HelpString =
            "ETWAnalyzer -convert -filedir xx.etl [-pid ddd or -1] [-perthread] [-symServer MS, syngo or NtSymbolPath] [-debug]" + Environment.NewLine +
            "Convert CPU Sample Profiling data from an  ETL file to a Json file which can be read by SpeedScope." + Environment.NewLine + 
            "See https://www.speedscope.app/ and https://adamsitnik.com/speedscope/ for more information." + Environment.NewLine +
            "  -filedir xxx.etl    Input ETL file." + Environment.NewLine +
            "  -pid dd             Optional. If -1 then all processes are combined into the converted file. Otherwise you need to specify an existing process id." + Environment.NewLine +
            "  -perthread          By default all threads are merged. If used then the profiling data per thread is extracted." + Environment.NewLine + 
            "  -debug              Print exception on console if a command has an error." +Environment.NewLine +
            "  -nocolor            Do not colorize output on shells with different color schemes. Writing console output is also much faster if it is not colorized." + Environment.NewLine +
            "  -symServer [MS, syngo or NtSymbolPath]   Load pdbs from remote symbol server which is stored in the ETWAnalyzer.exe.config file. Known ones are MS and syngo" + Environment.NewLine +
            "                      When NtSymbolPath is used the first remote symbol server from _NT_SYMBOL_PATH environment variable is used" + Environment.NewLine +
           $"                      The config file {ConfigFiles.RequiredPDBs} declares which pdbs, if present in the trace, must have loaded symbols." + Environment.NewLine +
            "                         Symbol loading works in two steps." + Environment.NewLine +
            "                          1. Symbols are loaded from SymCache and local cached pdb files. If an essential configured pdb is missing and -symserver was used we go to step 2 or we are finished." + Environment.NewLine +
            "                          2. Now the remote configured symbol server is added and we try to load pdbs a second time. If that still fails we continue with what we have" 
            ;

        /// <summary>
        /// Input ETL file name
        /// </summary>
        string myEtlFileName;

        /// <summary>
        /// Process id to extract or -1 if all 
        /// </summary>
        int myPid;


        /// <summary>
        /// Magic pid which we use to merge all processes. 
        /// </summary>
        const int AllProcessesPid = -1;

        /// <summary>
        /// 
        /// </summary>
        bool myDebugOutputToConsole;

        /// <summary>
        /// If true all threads separately printed to json. Otherwise all threds are merged
        /// </summary>
        bool myPerThreadFlag;


        public override string Help => HelpString;


        public ConvertCommand(string[] args):base(args)
        {

        }

        public override void Parse()
        {
            while (myInputArguments.Count > 0)
            {
                string curArg = myInputArguments.Dequeue();
                switch (curArg?.ToLowerInvariant())
                {
                    case CommandFactory.ConvertArg:
                        break;
                    case FileOrDirectoryArg:
                        string path = GetNextNonArg("-filedir");
                        myEtlFileName = ArgParser.CheckIfFileOrDirectoryExistsAndExtension(path, EtlExtension, ZipExtension, SevenZipExtension);
                        break;
                    case PidArg:
                        myPid = int.Parse(GetNextNonArg(PidArg), CultureInfo.InvariantCulture);
                        break;
                    case PerThreadArg:
                        myPerThreadFlag = true;
                        break;
                    case SymbolServerArg: // -symserver
                        Symbols.RemoteSymbolServer = ExtractCommand.ParseSymbolServer(GetNextNonArg(SymbolServerArg));
                        break;
                    case DebugArg:    // -debug 
                        myDebugOutputToConsole = true;
                        Program.DebugOutput = true;
                        break;
                    case NoColorArg:
                        ColorConsole.EnableColor = false;
                        break;
                    default:
                        throw new NotSupportedException($"The argument {curArg} was not recognized as valid argument");
                }
            }

            if( myEtlFileName == null || !File.Exists(myEtlFileName))
            {
                throw new NotSupportedException($"You need to enter {FileOrDirectoryArg} with an existing input file.");
            }
        }

        public override void Run()
        {
            TextWriter dbgOutputWriter = myDebugOutputToConsole ? Console.Out : new StringWriter();

            using var log = TraceLog.OpenOrConvert(myEtlFileName, new TraceLogOptions
            {
                ConversionLog = dbgOutputWriter,
            });

            var process = log.Processes.FirstOrDefault(x => x.ProcessID == myPid);
            if (process == null && myPid != AllProcessesPid || myPid == 0)
            {
                PrintError(log);
                return;
            }
            else if (myPid == AllProcessesPid)
            {
                Console.WriteLine("Converting all processes system wide into one file.");
            }
            else
            {
                ColorConsole.WriteEmbeddedColorLine($"Convert process [green]{process.Name}({process.ProcessID})[/green] {process.CommandLine}, PerThread: {myPerThreadFlag}");
            }

            using Microsoft.Diagnostics.Symbols.SymbolReader reader = new(dbgOutputWriter, Symbols.GetCombinedSymbolPath(myEtlFileName))
            {
                SecurityCheck = (x) => true,
            };
            var computer = new SampleProfilerThreadTimeComputer(log, reader);
            MutableTraceEventStackSource stackSource = new(log);

            var eventSource = log.Events.GetSource();
            // A stack source is  list of samples.  We create a sample structure, mutate it and then call AddSample() repeatedly to add samples. 
            var sample = new StackSourceSample(stackSource);

            float sampleRate = 1.0f; // assume 1ms sample rate unless we get a different value from PerfInfoCollectionStart event
            eventSource.Kernel.PerfInfoSample += (Microsoft.Diagnostics.Tracing.Parsers.Kernel.SampledProfileTraceData obj) =>
            {
                CallStackIndex callStackIdx = obj.CallStackIndex();
                if ((obj.ProcessID == myPid || myPid == AllProcessesPid) && callStackIdx != CallStackIndex.Invalid)
                {
                    // Convert the TraceLog call stack to a MutableTraceEventStackSource call stack
                    StackSourceCallStackIndex stackCallStackIndex = stackSource.GetCallStack(callStackIdx, obj);

                    if (myPerThreadFlag)
                    {
                        // Add a pseudo frame on the bottom of the stack
                        // this way we can see the cpu costs per thread in the merged view at the bottom of the Sandwich view
                        StackSourceFrameIndex frameIdxForName = stackSource.Interner.FrameIntern($"Thread  {obj.ThreadID} - Process {obj.ProcessID}");
                        stackCallStackIndex = stackSource.Interner.CallStackIntern(frameIdxForName, stackCallStackIndex);
                    }

                    // create a sample with that stack and add it to the stack source (list of samples)
                    sample.Metric = sampleRate;
                    sample.TimeRelativeMSec = obj.TimeStampRelativeMSec;
                    sample.StackIndex = stackCallStackIndex;
                    stackSource.AddSample(sample);
                }
            };

            // Interval is specified in 100ns steps. 10K is one ms. We need to divide the sample rate
            eventSource.Kernel.PerfInfoCollectionStart += (Microsoft.Diagnostics.Tracing.Parsers.Kernel.SampledProfileIntervalTraceData obj) =>
            {
                sampleRate = obj.NewInterval / 10_000.0f;
            };

            eventSource.Process();
            stackSource.DoneAddingSamples();
            stackSource.LookupWarmSymbols(1, reader);

            string processName = myPid == AllProcessesPid ? "_AllProcesses" : $"_{ process?.Name}_{ myPid}";
            string outFile = Path.Combine(Path.GetDirectoryName(myEtlFileName), Path.GetFileNameWithoutExtension(myEtlFileName) + $"{processName}.speedscope");

            SpeedScopeWriter.WriteStackViewAsJson(stackSource, outFile, myPerThreadFlag);
            ColorConsole.WriteEmbeddedColorLine($"Converted File: [green]{Path.GetFullPath(outFile)}[/green]");
        }

        private void PrintError(TraceLog log)
        {

            Counter<int> perProcessCounter = new();
            foreach (TraceEvent someEvent in log.Events)
            {
                if (someEvent is SampledProfileTraceData sampleEvent)
                {
                    if( sampleEvent.ProcessID != -1 )
                    {
                        perProcessCounter.Increment(sampleEvent.ProcessID);
                    }
                }
            }

            KeyValuePair<int, int>[] pidSampleCounts = perProcessCounter.Counts;

            int getProcessSampleCount(int pid) => pidSampleCounts.Where(x => x.Key == pid).FirstOrDefault().Value;

            // sort processes by name and the by CPU consumption
            foreach (var procLoop in log.Processes.ToLookup(x => x.Name).OrderBy(x=>x.Key))
            {
                foreach(TraceProcess process in procLoop.OrderBy( x=> getProcessSampleCount(x.ProcessID) ))
                {
                    int sampleCount = getProcessSampleCount(process.ProcessID);

                    string name = $"{process.Name}({process.ProcessID})";
                    string ms = $"{sampleCount:N0}";
                    if (!String.IsNullOrEmpty(process.Name) && sampleCount > 0)
                    {
                        Console.WriteLine($"{name,-50} {ms,-6} ms {process.CommandLine}");
                    }
                }
            }

            ColorConsole.WriteError($"Process with ID {myPid} could not be found in trace");
        }
    }
}
