//// SPDX-FileCopyrightText:  © 2026 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;

namespace ETWAnalyzer.McpServer
{
    /// <summary>
    /// Stateful session that mirrors the ConsoleCommand pattern.
    /// Files are loaded once and reused across multiple MCP tool calls.
    /// </summary>
    internal class EtwSession
    {
        private static readonly object myLock = new();
        private static EtwSession? myInstance;

        /// <summary>
        /// Singleton instance shared across all MCP tool calls.
        /// </summary>
        public static EtwSession Instance
        {
            get
            {
                if (myInstance == null)
                {
                    lock (myLock)
                    {
                        myInstance ??= new EtwSession();
                    }
                }
                return myInstance;
            }
        }

        /// <summary>
        /// Currently loaded files, same pattern as ConsoleCommand.myInputFiles
        /// </summary>
        private Lazy<SingleTest>[]? myInputFiles;

        /// <summary>
        /// Get the currently loaded tests for use with DumpCommand preloaded data.
        /// </summary>
        public Lazy<SingleTest>[]? LoadedFiles => myInputFiles;

        private EtwSession()
        {
            ColorConsole.EnableColor = false; // Disable color output for MCP server context to render faster
        }

        /// <summary>
        /// Load one or multiple input files. Mirrors ConsoleCommand.Load().
        /// </summary>
        /// <param name="filePaths">File or directory paths to load.</param>
        /// <param name="keepOldFiles">If true, add to existing loaded files.</param>
        /// <param name="recursive">If true, search directories recursively.</param>
        /// <returns>Summary of loaded files.</returns>
        public string Load(string[] filePaths, bool keepOldFiles, bool recursive)
        {
            List<Lazy<SingleTest>> tests = new();
            List<string> messages = new();

            foreach (var arg in filePaths)
            {
                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                messages.Add($"Loading {arg}");
                try
                {
                    var runs = TestRun.CreateFromDirectory(arg, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly, null);
                    IEnumerable<SingleTest> filesToAdd = runs.SelectMany(x => x.Tests).SelectMany(x => x.Value);

                    HashSet<string> extractedUniqueFiles = filesToAdd.SelectMany(x => x.Files).Select(x => x.JsonExtractFileWhenPresent).Where(x => x != null).ToHashSet();
                    var jsonTests = extractedUniqueFiles.Select(jsonFileName =>
                    {
                        var test = new SingleTest(new TestDataFile[] { new TestDataFile(jsonFileName) });
                        test.KeepExtract = true;
                        return new Lazy<SingleTest>(() => test);
                    });

                    tests.AddRange(jsonTests);
                }
                catch (Exception ex)
                {
                    messages.Add($"Error: Could not load file {arg}. Got {ex.GetType().Name}: {ex.Message}");
                }
            }

            myInputFiles = (keepOldFiles && myInputFiles != null) ? myInputFiles.Concat(tests).ToArray() : tests.ToArray();

            if (myInputFiles != null)
            {
                foreach (var test in myInputFiles)
                {
                    foreach (var file in test.Value.Files)
                    {
                        messages.Add(file.FileName);
                    }
                }
                messages.Add($"Loaded {myInputFiles.Length} files.");
            }

            return string.Join(Environment.NewLine, messages);
        }

        /// <summary>
        /// Unload files. Mirrors ConsoleCommand.Unload().
        /// </summary>
        /// <param name="filesToUnload">Specific files to unload, or empty to unload all.</param>
        /// <returns>Summary message.</returns>
        public string Unload(string[] filesToUnload)
        {
            if (filesToUnload == null || filesToUnload.Length == 0)
            {
                int count = myInputFiles?.Length ?? 0;
                myInputFiles = [];
                return $"Unloaded all {count} files.";
            }
            else
            {
                List<string> toUnload = filesToUnload.Where(x => !string.IsNullOrEmpty(x)).ToList();

                if (myInputFiles != null)
                {
                    int before = myInputFiles.Length;
                    myInputFiles = myInputFiles.Select(x =>
                    {
                        var inputFiles = x.Value.Files.Where(f => !toUnload.Contains(f.FileName) && f.JsonExtractFileWhenPresent != null).ToArray();
                        var filteredJsonFiles = inputFiles.Select(f => new TestDataFile(f.JsonExtractFileWhenPresent)).ToArray();
                        return filteredJsonFiles.Length > 0 ? new Lazy<SingleTest>(() =>
                        {
                            var test = new SingleTest(filteredJsonFiles)
                            {
                                KeepExtract = true
                            };
                            return test;
                        }) : null;
                    }).Where(x => x != null).Cast<Lazy<SingleTest>>().ToArray();

                    return $"Unloaded {before - myInputFiles.Length} files. {myInputFiles.Length} files remaining.";
                }

                return "No files were loaded.";
            }
        }

        /// <summary>
        /// List currently loaded files. Mirrors ConsoleCommand.ListFiles().
        /// </summary>
        /// <returns>File listing or message indicating no files loaded.</returns>
        public string ListFiles()
        {
            if (myInputFiles == null || myInputFiles.Length == 0)
            {
                return "No files are loaded.";
            }

            var files = myInputFiles.SelectMany(x => x.Value.Files).Select(x => x.FileName).ToList();
            files.Add($"Total: {myInputFiles.Length} files loaded.");
            return string.Join(Environment.NewLine, files);
        }

        /// <summary>
        /// Apply -fd filter to the loaded files, same pattern as ConsoleCommand.ApplyFileDirFilter().
        /// </summary>
        /// <param name="args">Command arguments that may contain -fd filter.</param>
        /// <returns>Filtered args (without -fd) and filtered files.</returns>
        public (string[] FilteredArgs, Lazy<SingleTest>[] FilteredFiles) ApplyFileDirFilter(string[] args)
        {
            List<Lazy<SingleTest>> filteredFiles = new(myInputFiles ?? Enumerable.Empty<Lazy<SingleTest>>());
            List<string> filteredArgs = new();

            for (int i = 0; i < args.Length; i++)
            {
                string lower = args[i].ToLowerInvariant();
                switch (lower)
                {
                    case "-fd":
                    case "-filedir":
                        if (i + 1 < args.Length)
                        {
                            string filter = args[i + 1];
                            var match = Matcher.CreateMatcher(filter);
                            filteredFiles = filteredFiles.Where(x => match(x.Value.Files[0].FileName)).ToList();
                        }
                        i++;
                        break;
                    default:
                        filteredArgs.Add(args[i]);
                        break;
                }
            }

            return (filteredArgs.ToArray(), filteredFiles.ToArray());
        }
    }
}
