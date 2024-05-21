﻿//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using ETWAnalyzer.ProcessTools;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
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

        class ConsoleHelpCommand : ArgParser
        {
            public override string Help =>
               $".dump xxx                          Query loaded file/s. Options are the same as in -Dump command. e.g. .dump CPU will print CPU metrics. Allowed values are {DumpCommand.AllDumpCommands}" + Environment.NewLine +
                ".load file1.json file2.json ...    Load one or more data files. Use . to load all files in current directory." + Environment.NewLine +
                ".list                              List loaded files" + Environment.NewLine +
                ".quit or .q                        Quit ETWAnalyzer" + Environment.NewLine +
                ".unload                            Unload all files if no parameter is passed. Otherwise only the passed files are unloaded from the file list." + Environment.NewLine +
                ".sffn                              Enable/disable -ShowFullFileName to display full path of output files." + Environment.NewLine +
                "Pressing Ctrl-C will cancel current command, Ctrl-Break will terminate";

            public override void Parse()
            {
            }

            public override void Run()
            {
                ColorConsole.WriteLine(Help, ConsoleColor.Yellow);
            }

            public ConsoleHelpCommand(string[] args) : base(args)
            { }
        }

        /// <summary>
        /// Currently loaded files
        /// </summary>
        Lazy<SingleTest>[] myInputFiles;


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

        bool ShowFullFileNameFlag { get; set; }

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
                ".load" => Load(args),
                ".unload" => Unload(args),
                ".list" => ListFiles(args),
                ".dump" => new DumpCommand(args, myInputFiles)
                {
                    ShowFullFileName = ShowFullFileNameFlag,
                },
                ".sffn" => ShowFullFileName(args),
                ".quit" => new QuitCommand(args),
                ".q" => new QuitCommand(args),
                "q" => new QuitCommand(args),
                ".exit" => new QuitCommand(args),
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


        ICommand ShowFullFileName(string[] args)
        {
            ICommand lret = null;
            ShowFullFileNameFlag = !ShowFullFileNameFlag;
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
        /// Load one or multiple input files
        /// </summary>
        /// <param name="args">Input file query</param>
        /// <returns>null because it is not a real command.</returns>
        ICommand Load(string[] args)
        {
            ICommand cmd = null;

            List<Lazy <SingleTest>> tests  = new();
            foreach (var arg in args)
            {
                if( String.IsNullOrEmpty(arg) )
                {
                    continue;
                }
                Console.WriteLine($"Loading {arg}");
                var runs = TestRun.CreateFromDirectory(arg, System.IO.SearchOption.TopDirectoryOnly, null);
                IEnumerable<Lazy<SingleTest>> filesToAdd = runs.SelectMany(x => x.Tests).SelectMany(x => x.Value).Select(x =>
                {
                    x.KeepExtract = true; // do not unload serialized Extract when test is disposed.
                    return new Lazy<SingleTest>(() => x);
                });
                tests.AddRange(filesToAdd);
            }

            myInputFiles = tests.ToArray();

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
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

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
        class InvalidCommandCommand : ArgParser
        {
            public override string Help => throw new NotImplementedException();

            public override void Parse()
            {
            }

            public override void Run()
            {
                ColorConsole.WriteLine($"Command: {String.Join(" ", myInputArguments)} is not a recognized command. Enter .help to get list of valid commands.", ConsoleColor.Red);
            }

            public InvalidCommandCommand(string[] args) : base(args)
            { }
        }

        /// <summary>
        /// When we want to quite we throw a <see cref="NotImplementedException"/>
        /// </summary>
        class QuitCommand : ArgParser
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

            public QuitCommand(string[] args) : base(args) { }
        }
    }
}
