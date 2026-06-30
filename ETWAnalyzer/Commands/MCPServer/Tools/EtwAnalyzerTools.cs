//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

#nullable enable

// MCP tool methods are public for the MCP server runtime and self-documented via [Description] attributes.
// Suppress CS1591 (missing XML doc comment) for this file only.
#pragma warning disable CS1591

using ETWAnalyzer.Commands;
using ETWAnalyzer.EventDump;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            string[] paths = filePaths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return MCPSession.Instance.Load(paths, keepOldFiles, recursive);
        }

        [McpServerTool(Name = "etw_unload")]
        [Description("Unload files from the session. If no files are specified, all files are unloaded.")]
        public static string Unload(
            [Description("Optional: Comma-separated file paths to unload. If empty, all files are unloaded.")] string filePaths = "")
        {
            string[] paths = string.IsNullOrWhiteSpace(filePaths)
                ? Array.Empty<string>()
                : filePaths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return MCPSession.Instance.Unload(paths);
        }

        [McpServerTool(Name = "etw_list")]
        [Description("List all currently loaded ETW files in the session.")]
        public static string ListFiles()
        {
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

        [McpServerTool(Name = "etw_help")]
        [Description("Get help for ETWAnalyzer dump commands. Shows available options and their descriptions and usage examples.")]
        public static string Help(
            [Description("Specific dump command to get help for (e.g. 'CPU', 'Process', 'Memory'). Leave empty for general help.")] string command = "")
        {
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
