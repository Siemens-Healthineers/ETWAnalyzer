//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.Infrastructure;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ETWAnalyzer.Commands
{
    /// <summary>
    /// Interactive mode of ETWAnalyzer which allows to use all Dump commands without the need to reload the files again
    /// </summary>
    internal class ConsoleCommand : ArgParser
    {
        public override string Help => "";

        string TimeFormatString = null;
        string TimeDigitString = null;
        string ProcessFmt = null;

        bool ShowFullFileNameFlag { get; set; }
        /// <summary>
        /// Currently loaded files
        /// </summary>
        Lazy<SingleTest>[] myInputFiles;

        class ConsoleHelpCommand(string[] args) : ArgParser(args)
        {
            public static readonly string HelpDir = ".dir [-r] [-od] folder" + Environment.NewLine +
                "   Show files in folder. Use -r for recursive and -od for sorting files by modify time. Default sort order is alphabetic." + Environment.NewLine;

            public static readonly string HelpCd = ".cd folder" + Environment.NewLine +
                "   Change current directory to folder. If no folder is given current directory is printed." + Environment.NewLine;

            public override string Help =>
                HelpCd +
                ".cls"+ Environment.NewLine + 
                "   Clear screen." + Environment.NewLine +
                ".clip [0/1 or true/false]"+ Environment.NewLine +
                "   Enable/disable output clipping to console width. If no argument is given current setting is printed." + Environment.NewLine +
                ".converttime -time ..." + Environment.NewLine +
                "   Convert time/datetime string to ETW session time and back." + Environment.NewLine +
               $".dump xxx [ -fd *usecase1* ]" + Environment.NewLine + 
                "   Query loaded file/s. Options are the same as in -Dump command. e.g. .dump CPU will print CPU metrics." + Environment.NewLine +
                "     -fd *filter*    Filter loaded files which are queried. Filter is applied to full path file name." + Environment.NewLine +
               $"   Allowed values are {DumpCommand.AllDumpCommands}" + Environment.NewLine +
                 HelpDir +
                ".load [-all] [-r or -rec or -recursive] file1.json file2.json .." + Environment.NewLine + 
                "     -all    Fully load all json files during load. By default the files are fully loaded during the dump command." + Environment.NewLine +
                "     -r      Load files recursively." + Environment.NewLine +
                "   Load one or more data files. Use . to load all files in current directory. Previously loaded files are removed." + Environment.NewLine +
                ".load+ file.json" + Environment.NewLine + 
                "   Add file to list of loaded files but keep other files." + Environment.NewLine +  
                ".list" + Environment.NewLine + 
                "   List loaded files" + Environment.NewLine +
                ".processfmt timefmt" + Environment.NewLine +
                "   Display for process start/end marker not +- but actual time and duration." + Environment.NewLine +
                ".quit or .q"+Environment.NewLine + 
                "   Quit ETWAnalyzer" + Environment.NewLine +
                ".unload"+Environment.NewLine + 
                "   Unload all files if no parameter is passed. Otherwise only the passed files are unloaded from the file list." + Environment.NewLine +
                ".sffn" +Environment.NewLine + 
                "   Enable/disable -ShowFullFileName to display full path of output files." + Environment.NewLine +
                ".timedigits n" + Environment.NewLine + 
                "   Set time precision (0-6)." + Environment.NewLine +
                ".timefmt fmt [precision]" + Environment.NewLine + 
               $"   Set time display format ({String.Join(" ", Enum.GetNames(typeof(EventDump.DumpBase.TimeFormats)).Where(x=>x!="None"))}) and precision (0-6) where default is 3." + Environment.NewLine +
                "Pressing Ctrl-C will cancel current command, Ctrl-Break will terminate";

            public override void Parse()
            {
            }

            public override void Run()
            {
                ColorConsole.WriteLine(Help, ConsoleColor.Yellow);
            }
        }


        public override void Run()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

            while (true)
            {
                Console.Write(">");
                string command = Console.ReadLine();

                if( command == null)
                {
                    continue;
                }

                string[] parts = SplitQuotedString(command);  // split into command name and optional argument for command

                if( parts.Length == 0 ) 
                {
                    continue;
                }

                if( RunCommand(parts) == true)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Command syntax is first string is command name and all following arguments are arguments for that command like in the command line
        /// </summary>
        /// <param name="parts"></param>
        bool RunCommand(string[] parts)
        {
            bool bCancel = false;
            string cmd = parts[0].ToLowerInvariant();
            string[] args = parts.Skip(1).ToArray();
            ICommand command = cmd switch
            {
                ".dir" => ShowDirectoryContents(args),  
                ".cd" => ChangeDirectory(args),
                ".load" => Load(args, bKeepOldFiles:false),
                ".load+" => Load(args, bKeepOldFiles:true),
                ".unload" => Unload(args),
                ".cls" => Cls(args),
                ".clip" => Clip(args),
                ".dump" => CreateDumpCommand(args),
                ".converttime" => CreateConvertTimeCommand(args),
                ".exit" => new QuitCommand(args),
                ".list" => ListFiles(args),
                ".processfmt" => SetProcessFmt(args),
                ".sffn" => ShowFullFileName(args),
                ".timedigits" => SetTimeDigits(args),
                ".timefmt" => SetTimeFormat(args),
                ".quit" => new QuitCommand(args),
                ".q" => new QuitCommand(args),
                "q" => new QuitCommand(args),
                ".help" => new ConsoleHelpCommand(args),
                "help" => new ConsoleHelpCommand(args),
                "?" => new ConsoleHelpCommand(args),
                ".?" => new ConsoleHelpCommand(args),
                _ => new InvalidCommandCommand(parts),
            };

            if (command != null)
            {
                try
                {
                    command.Parse();
                    command.Run();
                }
                catch(OperationCanceledException)
                {
                    bCancel = true; 
                }
                catch(OutputCanceledException)
                {
                    ColorConsole.WriteLine("Command was canceled.");
                }
                catch(Exception ex)
                {
                    string eMsg = $"Got error during command execution: {ex.Message}";
                    Logger.Error(eMsg);
                    Logger.Error(ex.ToString());

                    ColorConsole.WriteEmbeddedColorLine(command.Help);
                    ColorConsole.WriteLine(eMsg);
                }
            }

            return bCancel;
        }

        private ICommand Clip(string[] args)
        {
            if( args.Length == 0)
            {
                Console.WriteLine($"Output clipping is set to {ColorConsole.ClipToConsoleWidth}. To Change use .Clip 0/1");
            }
            else
            {
                string flag = args[0];
                bool bFlag = false;

                if (int.TryParse(flag, out int flagValue))
                {
                    bFlag = flagValue == 1;
                }
                else if (bool.TryParse(flag, out bool flagBool))
                {
                    bFlag = flagBool;
                }
                else
                {
                    ColorConsole.WriteError($"Error: Could not parse {flag} as 0/1 or true/false.");
                    return null;
                }

                ColorConsole.WriteLine("Set output clipping to " + bFlag);  
                ColorConsole.ClipToConsoleWidth = bFlag;
            }
            return null;
        }


        private ICommand Cls(string[] args)
        {
            Console.Clear();  // does not work in Windows Terminal with vtm 

            // use as second try virtual terminal sequences
            // https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences
            //#define ESC "\x1b"
            //#define CSI "\x1b["
            //printf(CSI "1;1H");
            //printf(CSI "2J"); // Clear screen

            Console.Write("\x1b[1:1H");
            Console.Write("\x1b[2J");

            return null;
        }

        ICommand ShowFullFileName(string[] args)
        {
            ICommand lret = null;
            ShowFullFileNameFlag = !ShowFullFileNameFlag;
            return lret;
        }

        private ICommand SetTimeDigits(string[] args)
        {
            ICommand lret = null;
            TimeDigitString = null;

            if (args.Length > 0)
            {
                TimeDigitString = args[0];
            }
            return lret;
        }

        private ICommand SetTimeFormat(string[] args)
        {
            ICommand lret = null;
            TimeFormatString = null;
            TimeDigitString = null;

            if ( args.Length > 1 )
            {
                TimeFormatString = args[0]; 

                if( args.Length > 1 )
                {
                    TimeDigitString = args[1];  
                }
            }
            return lret;
        }


        private ICommand SetProcessFmt(string[] args)
        {
            ICommand lret = null;
            ProcessFmt = null;

            if (args.Length > 0)
            {
                ProcessFmt = args[0];
            }

            return lret;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        ICommand ListFiles(string[] args)
        {
            ICommand lret = null;
            string str = "";
            if( myInputFiles == null || myInputFiles.Length == 0 )
            {
                str = "No files are loaded.";
            }
            else
            {
                foreach(var file in myInputFiles.SelectMany(x => x.Value.Files).Select(x => x.FileName))
                {
                    str += file + Environment.NewLine;
                }

            }

            str = str.Trim(Environment.NewLine.ToCharArray());
            Console.WriteLine(str);

            return lret;
        }
        
        /// <summary>
        /// Create dump command from filtered list of arguments.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        DumpCommand CreateDumpCommand(string[] args)
        {
            var argsAndTests = ApplyFileDirFilter(args);
            List<string> filteredArgs = argsAndTests.Item1.ToList();
            if( this.TimeDigitString != null )
            {
                filteredArgs.Add("-TimeDigits");
                filteredArgs.Add(TimeDigitString);
            }

            if( this.TimeFormatString != null ) 
            {
                filteredArgs.Add("-TimeFmt");
                filteredArgs.Add(TimeFormatString);
            }

            if (ProcessFmt != null)
            {
                filteredArgs.Add("-ProcessFmt");
                filteredArgs.Add(ProcessFmt);
            }

            return new DumpCommand(filteredArgs.ToArray(), argsAndTests.Item2)
            {
                ShowFullFileName = ShowFullFileNameFlag,
            };
        }

        ConvertTimeCommand CreateConvertTimeCommand(string[] args)
        {
            var argsAndTests = ApplyFileDirFilter(args);
            List<string> filteredArgs = argsAndTests.Item1.ToList();

            return new ConvertTimeCommand(filteredArgs.ToArray(), argsAndTests.Item2);
        }

        /// <summary>
        /// Apply -fd queries to filter current file list based on full path.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        (string[], Lazy<SingleTest>[]) ApplyFileDirFilter(string[] args)
        {
            List<Lazy<SingleTest>> filteredFiles = new(myInputFiles ?? Enumerable.Empty<Lazy<SingleTest>>());
            List<string> filteredArgs = new();
            for (int i = 0; i < args.Length; i++)
            {
                string lower = args[i].ToLowerInvariant();
                switch (lower)
                {
                    case FileOrDirectoryAlias:
                    case FileOrDirectoryArg:
                        if (i + 1 < args.Length)
                        {
                            string filter = args[i + 1];
                            var match = Matcher.CreateMatcher(filter);
                            filteredFiles = filteredFiles.Where(x => match(x.Value.Files[0].FileName)).ToList();
                            if (filteredFiles.Count == myInputFiles?.Length)
                            {
                                Console.WriteLine($"Warning: Filter did not filter any files.");
                            }
                        }
                        else
                        {
                            ColorConsole.WriteLine($"{FileOrDirectoryAlias}/{FileOrDirectoryArg} needs an argument. No filter is applied. Dumping all files.", ConsoleColor.Red);
                        }
                        i++;
                        break;
                    default:
                        filteredArgs.Add(args[i]);
                        break;
                }
            }

            return (filteredArgs.ToArray(),filteredFiles.ToArray());
        }

        private ICommand ShowDirectoryContents(string[] args)
        {
            ICommand cmd = null;
            string curDir = null;
            try
            {
                bool sortByDate = false;
                bool bRecursive = false;
                Func<string, bool> isArg = x => x.StartsWith('/') || x.StartsWith('-');
                List<string> noSwitches = new List<string>();
                foreach (var arg in args)
                {
                    if (isArg(arg))
                    {
                        switch (arg.Substring(1).ToLowerInvariant())
                        {
                            case "od":
                                sortByDate = true;
                                break;
                            case "r":
                            case "rec":
                            case "recursive":
                                bRecursive = true;
                                break;
                            case "h":
                                ColorConsole.WriteLine(ConsoleHelpCommand.HelpDir);
                                return cmd;
                            default:
                                ColorConsole.WriteLine(ConsoleHelpCommand.HelpDir);
                                ColorConsole.WriteError($"Unknown switch {arg}.");
                                return cmd;
                        }
                    }
                    else
                    {
                        noSwitches.Add(arg);
                    }
                }

                if (noSwitches.Count == 0)
                {
                    curDir = ".";
                }
                else
                {
                    curDir = noSwitches[0];
                }

                var data = new TestRunData(curDir, bRecursive ? SearchOption.AllDirectories: SearchOption.TopDirectoryOnly);

                SortOrders order = sortByDate ? SortOrders.ModifyTime : SortOrders.Name;

                string dirSave = TestRun.GetDirectorySave(Path.GetFullPath(curDir));
                List<FileInfo> inputFiles = GetSorted(Directory.GetDirectories(dirSave), order);

                foreach (var dir in inputFiles)
                {
                    Console.WriteLine($"<DIR> {dir.LastWriteTime,-24} {dir.FullName}");
                }

                List<FileInfo> jsonFiles = GetSorted(data.AllFiles.Where(x => x.JsonExtractFileWhenPresent != null).Select(x => x.JsonExtractFileWhenPresent), order);

                foreach (var json in jsonFiles) 
                {
                    Console.WriteLine($"{json.LastWriteTime,-24} {json.FullName}");
                }
            }
            catch (DirectoryNotFoundException)
            {
                ColorConsole.WriteError($"Directory {curDir} was not found.");
            }

            return cmd;
        }


        private ICommand ChangeDirectory(string[] args)
        {
            ICommand cmd = null;

            if (args.Length == 0)
            {
                // print current directory
                Console.WriteLine($"Current directory is {Directory.GetCurrentDirectory()}");
            } else if (args.Length == 1)
            {
                string dir = args[0];
                try
                {
                    Directory.SetCurrentDirectory(dir);
                }
                catch (DirectoryNotFoundException)
                {
                    ColorConsole.WriteError($"Directory {dir} was not found.");
                }
                catch (UnauthorizedAccessException)
                {
                    ColorConsole.WriteError($"You do not have access to directory {dir}.");
                }
                catch (Exception ex)
                {
                    ColorConsole.WriteError($"Could not change directory to {dir}. Got {ex.GetType().Name}: {ex.Message}");
                }
            }
            else
            {
                ColorConsole.WriteError("Change directory command needs one argument with the new directory name.");
            }

            return cmd;
        }

        enum SortOrders
        {
            Name,
            ModifyTime,
        }

        
        List<FileInfo> GetSorted(IEnumerable<FileInfo> files, SortOrders order)
        {
            List<FileInfo> result;
            HashSet<FileInfo> unique = new HashSet<FileInfo>(files, new FileInfoEqualityComparer());


            if ( order == SortOrders.Name)
            {
                result = unique.OrderBy(x => x.Name).ToList();
            }
            else
            {
                result = unique.OrderBy(x => x.LastWriteTime).ToList();
            }

            return result;
        }

        List<FileInfo> GetSorted(IEnumerable<string> files, SortOrders order)
        {
            return GetSorted(files.Select(x=>new FileInfo(x)), order);
        }

        /// <summary>
        /// Load one or multiple input files
        /// </summary>
        /// <param name="args">Input file query</param>
        /// <param name="bKeepOldFiles">Add to existing do not replace previously loaded files.</param>
        /// <returns>null because it is not a real command.</returns>
        ICommand Load(string[] args, bool bKeepOldFiles)
        {
            ICommand cmd = null;

            bool bFullLoad = false;
            bool bRecursive = false;   

            List<Lazy <SingleTest>> tests  = new();
            foreach (var arg in args)
            {
                if( String.IsNullOrEmpty(arg) )
                {
                    continue;
                }

                switch(arg.ToLowerInvariant())
                {
                    case "-fd":
                        continue;
                    case "-all":
                        bFullLoad = true;
                        continue;
                    case "-rec":
                    case "-recursive":
                    case "-r":
                        bRecursive = true;
                        continue;
                }



                Console.WriteLine($"Loading {arg}");
                try
                {
                    var runs = TestRun.CreateFromDirectory(arg, bRecursive ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly, null);
                    IEnumerable<Lazy<SingleTest>> filesToAdd = runs.SelectMany(x => x.Tests).SelectMany(x => x.Value).Select(x =>
                    {
                        x.KeepExtract = true; // do not unload serialized Extract when test is disposed.
                        ForceDeserializeOnLoadWhenRequested(bFullLoad, x);
                        return new Lazy<SingleTest>(() => x);
                    });
                    tests.AddRange(filesToAdd);
                }
                catch(Exception ex)
                {
                    Logger.Error(ex.ToString());
                    ColorConsole.WriteEmbeddedColorLine($"[red]Error: Could not load file {arg}. Got {ex.GetType().Name}: {ex.Message}[/red]");
                }
            }


            myInputFiles = (bKeepOldFiles && myInputFiles != null) ? myInputFiles.Concat(tests).ToArray() : tests.ToArray();

            if( myInputFiles != null )
            {
                foreach(var test in myInputFiles)
                {
                    foreach(var file in test.Value.Files)
                    {
                        Console.WriteLine($"{file.FileName}");
                    }
                }
                Console.WriteLine($"Loaded {myInputFiles.Length} files.");
            }

            return cmd;
        }

        /// <summary>
        /// Deserialize json file and all dependant json files if requested.
        /// By default the files are loaded when it is accessed.
        /// </summary>
        /// <param name="bFullLoad"></param>
        /// <param name="x"></param>
        private static void ForceDeserializeOnLoadWhenRequested(bool bFullLoad, SingleTest x)
        {
            if (bFullLoad)
            {
                foreach (var file in x.Files)
                {
                    var tmp = file.Extract.CPU;
                    var tmp1 = file.Extract.Modules;
                    var tmp2 = file.Extract.FileIO;
                    var tmp3 = file.Extract.HandleData;
                    var tmp4 = file.Extract.CPU.ExtendedCPUMetrics;
                }
            }
        }


        /// <summary>
        /// Unload all loaded files
        /// </summary>
        /// <param name="filesToUnload">files to unload or if empty all files are unloaded.</param>
        /// <returns>null because it is not a real command</returns>
        ICommand Unload(string[] filesToUnload)
        {
            ICommand cmd = null;
            if (filesToUnload.Length == 0)
            {
                myInputFiles = [];
            }
            else
            {
                List<string> toUnload = new();
                foreach (var arg in filesToUnload)
                {
                    if( !String.IsNullOrEmpty(arg) )
                    {
                        toUnload.Add(arg);
                    }
                }

                if( myInputFiles != null )
                {
                    myInputFiles = myInputFiles.Select(x =>
                    {
                        // remove files to be unloaded and switch over to Json files 
                        var inputFiles = x.Value.Files.Where(f => !toUnload.Contains(f.FileName) && f.JsonExtractFileWhenPresent != null).ToArray();
                        // only consider existing json files
                        var filteredJsonFiles = inputFiles.Select(x => new TestDataFile(x.JsonExtractFileWhenPresent)).ToArray();
                        return filteredJsonFiles.Length > 0 ? new Lazy<SingleTest>( () =>
                        {
                            var test = new SingleTest(filteredJsonFiles)
                            {
                                KeepExtract = true // do not unload
                            };
                            return test;
                        }) : null;
                    }).Where(x => x != null).ToArray();
                }

            }
            
            return cmd;
        }


        /// <summary>
        /// Break current command when Ctrl-C was pressed, but terminate if Ctrl-Break was pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                e.Cancel = true; // do not terminate application but break current command
                ColorConsole.CancelRequested = true;
                ColorConsole.EnableColor = true;  // ColorConsole will disable color on its own to prevent colorizing the console output if command is interrupted 

            }
            else
            {
                // terminate application if Ctrl-Break was pressed
            }
        }

        public override void Parse()
        {
        }

        /// <summary>
        /// Main ctor 
        /// </summary>
        /// <param name="args"></param>
        public ConsoleCommand(string[] args) : base(args)
        {
            // skip -console argument and treat rest as input file names
            string[] fileCandidates = args.Skip(1).Where(x => x.ToLowerInvariant() != "-fd").ToArray();
            if( fileCandidates.Length > 0 ) 
            {
                Load(fileCandidates, false);
            }
        }


        /// <summary>
        /// Split a string with quotes
        /// </summary>
        /// <param name="str">Input string to split</param>
        /// <param name="splitChar"></param>
        /// <param name="quoteChar"></param>
        /// <returns></returns>
        static string[] SplitQuotedString(string str, char splitChar=' ', char quoteChar='"')
        {
            ArgumentNullException.ThrowIfNull(str);

            char prevChar = '\0';
            char nextChar = '\0';
            char currentChar = '\0';

            bool inString = false;

            List<string> lret = new();

            StringBuilder token = new();

            for (int i = 0; i < str.Length; i++)
            {
                currentChar = str[i];

                if (i > 0)
                    prevChar = str[i - 1];
                else
                    prevChar = '\0';

                if (i + 1 < str.Length)
                    nextChar = str[i + 1];
                else
                    nextChar = '\0';

                if (currentChar == quoteChar && (prevChar == '\0' || prevChar == splitChar) && !inString)
                {
                    inString = true;
                    continue;
                }

                if (currentChar == quoteChar && (nextChar == '\0' || nextChar == splitChar) && inString)
                {
                    inString = false;
                    continue;
                }

                if (currentChar == splitChar && !inString)
                {
                    if (token.Length > 0)
                    {
                        lret.Add(token.ToString());
                    }
                    token = token.Remove(0, token.Length);
                    continue;
                }

                token = token.Append(currentChar);

            }

            if( token.Length > 0 )
            {
                lret.Add(token.ToString());
            }

            return lret.ToArray();
        }

        /// <summary>
        /// Prints error when not recognized command is entered.
        /// </summary>
        class InvalidCommandCommand(string[] args) : ArgParser(args)
        {
            public override string Help => throw new NotImplementedException();

            public override void Parse()
            {
            }

            public override void Run()
            {
                ColorConsole.WriteLine($"Command: {String.Join(" ", myInputArguments)} is not a recognized command. Enter .help to get list of valid commands.", ConsoleColor.Red);
            }
        }

        /// <summary>
        /// When we want to quite we throw a <see cref="NotImplementedException"/>
        /// </summary>
        class QuitCommand(string[] args) : ArgParser(args)
        {
            public override string Help => throw new NotImplementedException();

            public override void Parse()
            {
                throw new OperationCanceledException();
            }

            public override void Run()
            {
                throw new OperationCanceledException();
            }
        }

        // Add the missing FileInfoEqualityComparer class definition to resolve the CS0246 error.  
        // This implementation assumes that the equality comparison is based on the file name and last write time.  
        internal class FileInfoEqualityComparer : IEqualityComparer<FileInfo>
        {
            public bool Equals(FileInfo x, FileInfo y)
            {
                if (x == null || y == null)
                {
                    return false;
                }
                return x.FullName.Equals(y.FullName, StringComparison.OrdinalIgnoreCase) &&
                       x.LastWriteTime.Equals(y.LastWriteTime);
            }

            public int GetHashCode(FileInfo obj)
            {
                ArgumentNullException.ThrowIfNull(obj);

                // Replace HashCode.Combine with a manual hash code computation
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + obj.FullName.ToLowerInvariant().GetHashCode();
                    hash = hash * 23 + obj.LastWriteTime.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
