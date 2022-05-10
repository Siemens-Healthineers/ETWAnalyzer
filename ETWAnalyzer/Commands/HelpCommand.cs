//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace ETWAnalyzer.Commands
{
    /// <summary>
    /// Help is the default command which prints general help or help for a sub command.
    /// </summary>
    class HelpCommand : ICommand
    {
        private static readonly string HelpString =
            $"ETWAnalyzer {FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule.FileName).FileVersion}" + Environment.NewLine +
            " ETWAnalyzer [green]-help[/green] [extract, dump, convert or analyze]  Get further information about the specific sub command" + Environment.NewLine +
            " ETWAnalyzer [green]-extract[/green] [All or Disk CPU Memory Exception Stacktag] -filedir inEtlOrZip [-outDir xxxxx] [-symServer [MS, syngo or NtSymbolPath] [-keepTemp] [-symFolder bbbb]" + Environment.NewLine +
            " ETWAnalyzer [green]-dump[/green] [Stats,Process,CPU,Memory,Disk,File,ThreadPool,Exception,Mark,TestRun,Version]" + Environment.NewLine +
            " ETWAnalyzer [green]-convert[/green] -filedir xx.etl -pid dd [-perthread]" + Environment.NewLine +
            " ETWAnalyzer [green]-analyze[/green] is WIP" + Environment.NewLine +
            "  -extract   Extract specific aspects from etl files into json files" + Environment.NewLine +
            "  -dump      Dump previously extracted data from Json files. Some commands additionally support ETL files (e.g. -dump Process)" + Environment.NewLine +
            "  -convert   Convert from an ETL File a process to a speedscope json file." + Environment.NewLine +
            "  -analyze   Analyzes for specifics from the previous extract step" + Environment.NewLine +
            "[yellow]Examples:[/yellow] " + Environment.NewLine +
            "[green]Get more help on specific option[/green]" + Environment.NewLine +
            "    ETWAnalyzer -help [extract, dump, or convert]" + Environment.NewLine +
            "[green]Extract data from ETL and store it in Json file in extract folder beneath ETL[/green]" + Environment.NewLine +
            "   ETWAnalyzer -extract All -filedir xxx.etl -symserver  NtSymbolPath" + Environment.NewLine +
            "[green]Dump Process start/stop information to console by reading the Json file which was generated in the previous (extract) step.[/green]" + Environment.NewLine +
            "   ETWAnalyzer -dump Process -filedir .\\Extract\\xxx.json" 
            ;

        /// <summary>
        /// Help string is command defendant which is set here
        /// </summary>
        Func<string> Retriever = () => HelpString;

        /// <summary>
        /// Get Help for current command or general help if no or an invalid command was entered.
        /// </summary>
        public string Help => Retriever();

        /// <summary>
        /// Command line arguments
        /// </summary>
        readonly Queue<string> myArgs;

        /// <summary>
        /// Construct help command from command line help
        /// </summary>
        /// <param name="args"></param>
        public HelpCommand(string[] args)
        {
            myArgs = new Queue<string>(args);
        }

        /// <summary>
        /// Find command specific help
        /// </summary>
        public void Parse()
        {
            while(myArgs.Count>0)
            {
                string arg = myArgs.Dequeue();
                switch (AddPrefix(arg))
                {
                    case CommandFactory.ExtractCommand:
                        Retriever = () => ExtractCommand.HelpString;
                        break;
                    case CommandFactory.DumpCommand:
                        Retriever = () => DumpCommand.HelpString;
                        break;
                    case CommandFactory.AnalyzeArg:
                        Retriever = () => AnalyzeCommand.HelpString;
                        break;
                    case CommandFactory.ConvertArg:
                        Retriever = () => ConvertCommand.HelpString;
                        break;
                    case ArgParser.NoColorArg:
                        ColorConsole.EnableColor = false;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Print Help
        /// </summary>
        public void Run()
        {
            ColorConsole.WriteEmbeddedColorLine(Help);
        }

        static string AddPrefix(string arg)
        {
            return arg.StartsWith("-", StringComparison.Ordinal) ? arg : "-" + arg;
        }
    }
}
