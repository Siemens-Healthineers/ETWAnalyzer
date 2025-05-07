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
            " ETWAnalyzer [green]-help[/green] [Extract, Dump, Console, Convert, ConvertTime or LoadSymbol]  Get further information about the specific sub command" + Environment.NewLine +
            " ETWAnalyzer [green]-Console[/green] [input files]" + Environment.NewLine +
            "        Interactive mode. Useful if working with bigger data sets without the need to reload data on every query. Enter .help to get more information." + Environment.NewLine +
            " ETWAnalyzer [green]-Convert[/green] -filedir xx.etl -pid dd [-perthread]" + Environment.NewLine +
            "        Convert from an ETL File a process to a speedscope json file." + Environment.NewLine +
           $" ETWAnalyzer [green]-Dump[/green] {DumpCommand.AllDumpCommands}" + Environment.NewLine +
            "        Dump previously extracted data from Json files. Some commands additionally support ETL files (e.g. -dump Process)" + Environment.NewLine +
           $" ETWAnalyzer [green]-Extract[/green] [{ExtractCommand.AllExtractCommands}] -filedir inEtlOrZip [-outDir xxxxx] [-symServer [MS, syngo or NtSymbolPath] [-keepTemp] [-symFolder bbbb]" + Environment.NewLine +
            "        Extract data from etl file/s and write summary data into json files." + Environment.NewLine +
            " ETWAnalyzer [green]-LoadSymbol[/green] -filedir xxx.json -symserver ..." + Environment.NewLine +
            "        Resolve method names from an extracted Json file." + Environment.NewLine +
            " ETWAnalyzer [green]-ConvertTime[/green] -filedir xxx.json -time ..." + Environment.NewLine +
            "        Convert a time string to an ETW session time in seconds." + Environment.NewLine +
            "[yellow]Examples:[/yellow] " + Environment.NewLine +
            "[green]Get more help on specific option[/green]" + Environment.NewLine +
            "    ETWAnalyzer -help [Extract, Dump, Convert or Loadsymbol]" + Environment.NewLine +
            "[green]Extract data from ETL and store it in .json7z file in Extract folder beneath ETL.[/green]" + Environment.NewLine +
            "   ETWAnalyzer -extract All -filedir xxx.etl -symserver  NtSymbolPath" + Environment.NewLine +
            "[green]Dump Process start/stop information to console by reading the compressed json file which was generated in the previous (extract) step.[/green]" + Environment.NewLine +
            "   ETWAnalyzer -dump Process -filedir .\\Extract\\xxx.json7z" 
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
        /// Return default return code as when application terminates.
        /// </summary>
        public int? ReturnCode => null;

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
                string arg = myArgs.Dequeue().ToLower();
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
                    case CommandFactory.LoadSymbolArg:
                        Retriever = () => LoadSymbolCommand.HelpString;
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
