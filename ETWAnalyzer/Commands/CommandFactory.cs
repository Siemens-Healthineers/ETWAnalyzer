//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Commands
{
    /// <summary>
    /// Create from a command line the corresponding concrete command class which parses all arguments for this command
    /// </summary>
    class CommandFactory
    {
        internal const string DumpCommand = "-dump";
        internal const string ExtractCommand = "-extract";
        internal const string ConvertArg = "-convert";
        internal const string AnalyzeArg = "-analyze";
        internal const string LoadSymbolArg = "-loadsymbol";
        internal const string ConsoleArg = "-console";
        internal const string ConvertTimeCommand = "-converttime";

        /// <summary>
        /// Create a command 
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Concrete command instance or Help Command if no command was entered.</returns>
        public static ICommand CreateCommand(string[] args)
        {
            ICommand lret = new HelpCommand(args);

            foreach(var potentialCommand in args.Select(arg => arg.ToLowerInvariant()))
            {
                switch(potentialCommand)
                {
                    case DumpCommand:
                        lret = new DumpCommand(args);
                        break;
                    case ExtractCommand:
                        lret = new ExtractCommand(args);
                        break;
                    case ConvertArg:
                        lret = new ConvertCommand(args);
                        break;
                    case LoadSymbolArg:
                        lret = new LoadSymbolCommand(args);
                        break;
                    case ConsoleArg:
                        lret = new ConsoleCommand(args);
                        break;
                    case ConvertTimeCommand:
                        lret = new ConvertTimeCommand(args);
                        break;  
                    default:
                        break;
                }
            }

            return lret;
        }
    }
}
