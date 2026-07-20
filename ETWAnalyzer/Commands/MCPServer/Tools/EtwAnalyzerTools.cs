//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

#nullable enable

// MCP tool methods are public for the MCP server runtime and self-documented via [Description] attributes.
// Suppress CS1591 (missing XML doc comment) for this file only.
#pragma warning disable CS1591

using ETWAnalyzer.Commands;
using ETWAnalyzer.Configuration;
using ETWAnalyzer.EventDump;
using ETWAnalyzer.Helper;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ETWAnalyzer.Commands.MCPServer.Tools
{
    /// <summary>
    /// MCP tools that operate on loaded ETW data in-process using the same pattern as ConsoleCommand.
    /// Files are loaded once via etw_load and reused across all dump commands.
    /// </summary>
    [McpServerToolType]
    public sealed class EtwAnalyzerTools
    {
        [McpServerTool(Name = "etw_load")]
        [Description("Load one or more Json7z/Json extracted ETW files or directories into the session. " +
                     "Files remain loaded for all subsequent dump commands. Use 'keepOldFiles=true' to add to existing loaded files.")]
        public static string Load(
            [Description("Comma-separated file paths or directory paths to load (e.g. 'C:\\Extract\\test.json7z' or 'C:\\Extract')")] string filePaths,
            [Description("If true, add to existing loaded files instead of replacing them. Default: false")] bool keepOldFiles = false,
            [Description("If true, search directories recursively. Default: false")] bool recursive = false)
        {
            Logger.Info($"MCP etw_load(filePaths='{filePaths}', keepOldFiles={keepOldFiles}, recursive={recursive})");
            string[] paths = filePaths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return MCPSession.Instance.Load(paths, keepOldFiles, recursive);
        }

        [McpServerTool(Name = "etw_unload")]
        [Description("Unload files from the session. If no files are specified, all files are unloaded.")]
        public static string Unload(
            [Description("Optional: Comma-separated file paths to unload. If empty, all files are unloaded.")] string filePaths = "")
        {
            Logger.Info($"MCP etw_unload(filePaths='{filePaths}')");
            string[] paths = string.IsNullOrWhiteSpace(filePaths)
                ? Array.Empty<string>()
                : filePaths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return MCPSession.Instance.Unload(paths);
        }

        [McpServerTool(Name = "etw_list")]
        [Description("List all currently loaded ETW files in the session.")]
        public static string ListFiles()
        {
            Logger.Info("MCP etw_list()");
            return MCPSession.Instance.ListFiles();
        }

        [McpServerTool(Name = "etw_dump_cpu")]
        [Description("Dump CPU consumption data (sampling and context switch) from loaded files. " +
                     "Shows method inclusive CPU time, wait time, and ready time per process. " +
                     "Use -Methods to filter specific methods, -topN to limit processes, -topNMethods to limit methods.")]
        public static string DumpCpu(
            [Description("Arguments for the CPU dump command (e.g. '-Methods *Initialize* -topN 5 -ProcessName myapp.exe')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.CPU, arguments);
        }

        [McpServerTool(Name = "etw_dump_process")]
        [Description("Dump process information (name, pid, command line, start/stop time, return code, parent) from loaded files. " +
                     "Use -SortBy Tree to show process tree, -Crash to show crashed processes.")]
        public static string DumpProcess(
            [Description("Arguments for the Process dump command (e.g. '-ProcessName cmd.exe -SortBy Tree')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.Process, arguments);
        }

        [McpServerTool(Name = "etw_dump_exception")]
        [Description("Dump managed exceptions from loaded files. Shows exception type, message, count per process. " +
                     "Use -Type to filter by exception type, -ShowStack to see stack traces.")]
        public static string DumpException(
            [Description("Arguments for the Exception dump command (e.g. '-Type *TimeoutException* -ShowStack')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.Exception, arguments);
        }

        [McpServerTool(Name = "etw_dump_memory")]
        [Description("Dump memory usage (working set, committed memory) per process from loaded files. " +
                     "Use -SortBy Commit/WorkingSet/Diff to control sorting, -TopN to limit output.")]
        public static string DumpMemory(
            [Description("Arguments for the Memory dump command (e.g. '-TopN 10 -SortBy Commit')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.Memory, arguments);
        }

        [McpServerTool(Name = "etw_dump_disk")]
        [Description("Dump Disk IO data per directory/file/process from loaded files. " +
                     "Use -DirLevel to control directory depth, -PerProcess to see per-process breakdown.")]
        public static string DumpDisk(
            [Description("Arguments for the Disk dump command (e.g. '-DirLevel 3 -PerProcess')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.Disk, arguments);
        }

        [McpServerTool(Name = "etw_dump_file")]
        [Description("Dump File IO data showing read/write sizes, times, and counts from loaded files. " +
                     "Use -PerProcess for per-process view, -FileOperation to filter by operation type.")]
        public static string DumpFile(
            [Description("Arguments for the File dump command (e.g. '-PerProcess -fileName E:\\Store*')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.File, arguments);
        }

        [McpServerTool(Name = "etw_dump_dns")]
        [Description("Dump DNS query latency and results from loaded files. " +
                     "Shows query names, durations, and optional adapter/IP information.")]
        public static string DumpDns(
            [Description("Arguments for the DNS dump command (e.g. '-DnsQueryFilter *google* -MinMaxTime 20ms')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.Dns, arguments);
        }

        [McpServerTool(Name = "etw_dump_tcp")]
        [Description("Dump TCP connection data including sent/received bytes, retransmissions from loaded files.")]
        public static string DumpTcp(
            [Description("Arguments for the TCP dump command (e.g. '-SortBy RetransmissionCount -MinMaxRetransCount 1')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.TCP, arguments);
        }

        [McpServerTool(Name = "etw_dump_marker")]
        [Description("Dump ETW marker events from loaded files. Useful for correlating custom events with trace timelines.")]
        public static string DumpMarker(
            [Description("Arguments for the Marker dump command (e.g. '-MarkerFilter *Start* -ZeroTime marker *_Start')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.Mark, arguments);
        }

        [McpServerTool(Name = "etw_dump_threadpool")]
        [Description("Dump .NET ThreadPool starvation events from loaded files.")]
        public static string DumpThreadPool(
            [Description("Arguments for the ThreadPool dump command")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.ThreadPool, arguments);
        }

        [McpServerTool(Name = "etw_dump_power")]
        [Description("Dump Windows Power Profile settings from loaded files. Use -Diff to compare two files.")]
        public static string DumpPower(
            [Description("Arguments for the Power dump command (e.g. '-details' or '-Diff')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.Power, arguments);
        }

        [McpServerTool(Name = "etw_dump_stats")]
        [Description("Dump ETW trace statistics (OS version, trace time range, event counts) from loaded files.")]
        public static string DumpStats(
            [Description("Arguments for the Stats dump command (e.g. '-Properties *')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.Stats, arguments);
        }

        [McpServerTool(Name = "etw_dump_version")]
        [Description("Dump module/dll version information from loaded files. Use -dll to filter specific modules.")]
        public static string DumpVersion(
            [Description("Arguments for the Version dump command (e.g. '-dll mylib.dll')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.Version, arguments);
        }

        [McpServerTool(Name = "etw_dump_objectref")]
        [Description("Dump Handle/Object reference tracking data (Create/Close/Leak) from loaded files.")]
        public static string DumpObjectRef(
            [Description("Arguments for the ObjectRef dump command (e.g. '-Leak -ShowStack')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.ObjectRef, arguments);
        }

        [McpServerTool(Name = "etw_dump_virtualalloc")]
        [Description("Dump VirtualAlloc memory allocation events from loaded files. Shows committed/freed memory per process.")]
        public static string DumpVirtualAlloc(
            [Description("Arguments for the VirtualAlloc dump command (e.g. '-Details -ShowStack -TopNStacks 10')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.VirtualAlloc, arguments);
        }

        [McpServerTool(Name = "etw_dump_lbr")]
        [Description("Dump Last Branch Record (LBR) CPU profiling data from loaded files.")]
        public static string DumpLbr(
            [Description("Arguments for the LBR dump command (e.g. '-pn myapp -topnmethods 6 -showcaller')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.LBR, arguments);
        }

        [McpServerTool(Name = "etw_dump_pmc")]
        [Description("Dump Performance Monitor Counter (PMC) data from loaded files.")]
        public static string DumpPmc(
            [Description("Arguments for the PMC dump command (e.g. '-pn sorter')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.PMC, arguments);
        }

        [McpServerTool(Name = "etw_dump_tracelog")]
        [Description("Dump Tracelogging events.")]
        public static string DumpTraceLog(
        [Description("Arguments for the tracelog dump command (e.g. '-pn sorter' to limit to a specific process, To dump an overview '-provider *', to dump all events from all providers '-provider *:*')")] string arguments = "")
        {
            return ExecuteDumpCommand(DumpCommands.TraceLog, arguments);
        }

        [McpServerTool(Name = "etw_extract")]
        [Description("Extract ETW data from one or more .etl/.7z/.zip files (or a directory containing them) into Json7z extract files. " +
                     "The generated extract files are written by default to an 'Extract' subfolder besides the input file and can afterwards be loaded with etw_load and analyzed with the etw_dump_* commands. " +
                     "The returned text lists the produced extract file paths.")]
        public static string Extract(
            [Description("Path to the input .etl/.7z/.zip file or a directory containing such files.")] string etlFile,
            [Description("Space separated list of extractors to run e.g. 'All', 'Default' or 'CPU Disk File TCP Stacktag'. Default: 'All'.")] string extractors = "All",
            [Description("Symbol server. Default is Microsoft. There are shortcuts defined in ETWAnalyzer.dll.config und SymbolServerxx xml nodes.")] string symbolServer = "MS",
            [Description("Optional additional -extract arguments e.g. '-keeptemp -outdir C:\\Extract'.")] string arguments = "")
        {
            return ExecuteExtractCommand(etlFile, extractors, symbolServer, null, arguments);
        }

        [McpServerTool(Name = "etw_extract_timerange")]
        [Description("Extract ETW data for one or more trace relative time regions (seconds since trace start). " +
                     "Only the CPU, Disk, File, TCP and Stacktag extractors honor the time region, all other extractors are extracted unfiltered. " +
                     "Each region produces a separate extract file with the region appended to the file name (e.g. xxx_Time_1.0-2.0.json7z) and the extract contains the ExtractStartTime/ExtractEndTime properties. " +
                     "The generated files can be loaded with etw_load and analyzed with the etw_dump_* commands.")]
        public static string ExtractTimeRange(
            [Description("Path to the input .etl/.7z/.zip file or a directory containing such files.")] string etlFile,
            [Description("Space separated start/end pairs in seconds since trace start e.g. '1.0 2.0 3.0 4.0' extracts the regions 1.0-2.0 and 3.0-4.0. An end value prefixed with + is a duration relative to its start, e.g. '1.0 +2' extracts the region 1.0-3.0.")] string regions,
            [Description("Space separated list of extractors to run. Default: 'CPU Disk File TCP Stacktag'.")] string extractors = "CPU Disk File TCP Stacktag",
            [Description("Symbol server. Default is Microsoft. There are shortcuts defined in ETWAnalyzer.dll.config und SymbolServerxx xml nodes.")] string symbolServer = "MS",
            [Description("Optional additional -extract arguments e.g. '-keeptemp -outdir C:\\Extract'.")] string arguments = "")
        {
            return ExecuteExtractCommand(etlFile, extractors, symbolServer, regions, arguments);
        }

        [McpServerTool(Name = "etw_help")]
        [Description("Get help for ETWAnalyzer dump commands. Shows available options and their descriptions and usage examples.")]
        public static string Help(
            [Description("Specific dump command to get help for (e.g. 'CPU', 'Process', 'Memory'). Leave empty for general help.")] string command = "")
        {
            Logger.Info($"MCP etw_help(command='{command}')");
            if (string.IsNullOrWhiteSpace(command))
            {
                return DumpCommand.HelpString;
            }

            string[] args = new[] { "-dump", command, "-help" };
            try
            {
                var cmd = new DumpCommand(args);
                cmd.Parse();
            }
            catch (InvalidOperationException ex) when (ex.Message == "-help")
            {
                return $"See ETWAnalyzer documentation for -dump {command} options.";
            }
            catch (Exception ex)
            {
                return $"Help for '{command}': {ex.Message}";
            }

            return DumpCommand.HelpString;
        }

        /// <summary>
        /// Core execution method that creates a DumpCommand with preloaded data and captures its output.
        /// Mirrors the pattern in ConsoleCommand.CreateDumpCommand().
        /// </summary>
        private static string ExecuteDumpCommand(DumpCommands dumpType, string arguments)
        {
            Logger.Info($"MCP etw_dump_{GetParseString(dumpType)}(arguments='{arguments}')");
            var session = MCPSession.Instance;
            if (session.LoadedFiles == null || session.LoadedFiles.Length == 0)
            {
                return "Error: No files are loaded. Use etw_load to load files first.";
            }

            // Parse arguments respecting quoted strings
            string[] rawArgs = SplitArguments(arguments);

            // Apply -fd filter to narrow loaded files
            var (filteredArgs, filteredFiles) = session.ApplyFileDirFilter(rawArgs);

            // Build final args: -dump <type> <filteredArgs>
            string dumpTypeStr = GetParseString(dumpType);
            string[] cmdArgs = new[] { "-dump", dumpTypeStr }.Concat(filteredArgs).ToArray();

            using var capture = new ConsoleOutputCapture();
            try
            {
                var dumpCommand = new DumpCommand(cmdArgs, filteredFiles);
                dumpCommand.Parse();
                dumpCommand.Run();
            }
            catch (Exception ex)
            {
                string output = capture.GetOutput();
                return string.IsNullOrEmpty(output)
                    ? $"Error executing dump {dumpType}: {ex.Message}"
                    : output + Environment.NewLine + $"Error: {ex.Message}";
            }

            return capture.GetOutput();
        }

        /// <summary>
        /// Builds and runs an ETWAnalyzer -extract command in a separate ETWAnalyzer.exe process and captures its
        /// console output. Used by the etw_extract and etw_extract_timerange tools.
        /// A dedicated process is spawned for every extraction job because the underlying TraceProcessing library
        /// does not support parallel in-process extraction. Since the MCP host may invoke the extract tools
        /// concurrently, running each job in its own process avoids the resulting crashes.
        /// </summary>
        /// <param name="etlFile">Input .etl/.7z/.zip file or directory.</param>
        /// <param name="extractors">Space separated extractor names (e.g. "All" or "CPU Disk File TCP Stacktag").</param>
        /// <param name="symbolServer">Symbol server. Either a predefined name from or the usual symbol server declaration.</param>
        /// <param name="regions">Optional space separated -extractRegion start/end pairs, or null to extract the whole trace.</param>
        /// <param name="arguments">Optional additional -extract arguments.</param>
        /// <returns>Captured console output of the extraction.</returns>
        private static string ExecuteExtractCommand(string etlFile, string extractors, string? symbolServer, string? regions, string arguments)
        {
            Logger.Info($"MCP etw_extract(etlFile='{etlFile}', extractors='{extractors}', symbolServer='{symbolServer}', regions='{regions}', arguments='{arguments}')");
            if (string.IsNullOrWhiteSpace(etlFile))
            {
                return "Error: No input file specified. Provide the path to an .etl/.7z/.zip file or a directory containing such files.";
            }

            var args = new List<string> { "-extract" };
            args.AddRange(SplitArguments(string.IsNullOrWhiteSpace(extractors) ? "All" : extractors));
            args.Add("-filedir");
            args.Add(etlFile);
            args.Add("-allCPU");

            if (!String.IsNullOrEmpty(symbolServer))
            {
                args.Add("-SymServer");
                args.Add(symbolServer);
            }

            if (!string.IsNullOrWhiteSpace(regions))
            {
                args.Add("-extractRegion");
                args.AddRange(SplitArguments(regions));
            }

            args.AddRange(SplitArguments(arguments));

            // Run directly without spawning a controlling process which is only needed if multiple files would be extracted at once.
            if (!args.Any(a => a.Equals("-child", StringComparison.OrdinalIgnoreCase)))
            {
                args.Add("-child");
            }
            if (!args.Any(a => a.Equals("-nocolor", StringComparison.OrdinalIgnoreCase)))
            {
                args.Add("-nocolor");
            }

            // Run the extraction in a dedicated ETWAnalyzer.exe process. The TraceProcessing library does not
            // support parallel in-process extraction and the MCP host may call the extract tools concurrently,
            // so every job must run in its own process.
            string commandLine = string.Join(" ", args.Select(QuoteArgumentIfNeeded));

            try
            {
                var command = new ProcessCommand(ConfigFiles.ETWAnalyzerExe, commandLine);
                ExecResult res = command.Execute(ProcessPriorityClass.BelowNormal);

                Logger.Info($"MCP etw_extract process exited with {res.ReturnCode} (0x{(uint)res.ReturnCode:X})");

                string output = res.AllOutput.TrimEnd(Environment.NewLine.ToCharArray());

                if (res.ReturnCode != 0)
                {
                    return string.IsNullOrWhiteSpace(output)
                        ? $"Error executing extract: ETWAnalyzer.exe exited with return code {res.ReturnCode}."
                        : output + Environment.NewLine + $"Error: ETWAnalyzer.exe exited with return code {res.ReturnCode}.";
                }

                return string.IsNullOrWhiteSpace(output) ? "Extraction completed. No output was produced." : output;
            }
            catch (Exception ex)
            {
                Logger.Error($"MCP etw_extract failed: {ex}");
                return $"Error executing extract: {ex.Message}";
            }
        }

        /// <summary>
        /// Wrap an argument in double quotes when it contains whitespace so it survives the round trip through the
        /// ETWAnalyzer.exe command line.
        /// </summary>
        private static string QuoteArgumentIfNeeded(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                return "\"\"";
            }

            if (argument.IndexOf(' ') < 0 && argument.IndexOf('\t') < 0)
            {
                return argument;
            }

            // Already quoted?
            if (argument.StartsWith("\"", StringComparison.Ordinal) && argument.EndsWith("\"", StringComparison.Ordinal))
            {
                return argument;
            }

            return "\"" + argument + "\"";
        }

        /// <summary>
        /// Maps <see cref="DumpCommands"/> enum values to the lowercase parse token expected by <see cref="DumpCommand.Parse"/>.
        /// </summary>
        private static string GetParseString(DumpCommands command) => command.ToString().ToLowerInvariant();

        /// <summary>
        /// Split arguments string respecting quoted substrings.
        /// </summary>
        private static string[] SplitArguments(string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                return Array.Empty<string>();
            }

            var parts = new System.Collections.Generic.List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < arguments.Length; i++)
            {
                char c = arguments[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (c == ' ' && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }

                current.Append(c);
            }

            if (current.Length > 0)
            {
                parts.Add(current.ToString());
            }

            return parts.ToArray();
        }
    }
}
