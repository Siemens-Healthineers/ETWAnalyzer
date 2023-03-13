//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.ProcessTools;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace ETWAnalyzer
{
    class Program
    {
        /// <summary>
        /// Subfolder name of output (-outdir) directory where the extracted Json files are extracted to
        /// </summary>
        public const string ExtractFolder = "Extract";

        /// <summary>
        /// Property to store debug flag
        /// </summary>
        public static bool DebugOutput { get; set; }

        /// <summary>
        /// Currently running instance to detect in main current argument to display common relative help messages
        /// </summary>
        static internal Program myProgram;

        /// <summary>
        /// Current top level command which is now executing
        /// </summary>
        public ICommand CurrentCommand { get; set; } = new HelpCommand(Array.Empty<string>());

        /// <summary>
        /// Command line input arguments
        /// </summary>
        readonly string[] myInputArgs;


        /// <summary>
        /// Error happened during parse. In that case we print context specific help
        /// </summary>
        bool myIsParserError;


        /// <summary>
        /// Validating necessary arguments to parse their correctness
        /// </summary>
        /// <param name="args"></param>
        public Program(string[] args)
        {
            myInputArgs = args;
        }

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">passed command line arguments to executable.</param>
        /// <returns>0 on success, > 0 the number of failed items which could not be processed. -1 on Exception.</returns>
        public static int Main(string[] args)
        {
            int returnCode = 0;
            try
            {
                Logger.Info(String.Format(CultureInfo.InvariantCulture, "Process called with args: {0}", String.Join(" ", args)));
                MainCore(args);

                // when return code is set by command return it. 
                if (myProgram?.CurrentCommand?.ReturnCode != null)
                {
                    returnCode = myProgram.CurrentCommand.ReturnCode.Value;
                }
            }
            catch (Exception ex)
            {
                ColorConsole.ClipToConsoleWidth = false;
                Logger.Error($"Exception caught in main: {ex}");
                if( Program.myProgram.myIsParserError || ex is MissingInputException)
                {
                    ColorConsole.WriteEmbeddedColorLine(Program.myProgram.CurrentCommand.Help);
                }

                if (ex.Message != ArgParser.HelpArg) // .e.g -dump cpu -help is not an error
                {
                    ColorConsole.WriteLine(Environment.NewLine + $"Error: {ex.Message} Check {Logger.Instance.LogFolder}\\ETWAnalyzer_Trace.log for full details, or use -debug switch to get full output.", ConsoleColor.Red);
                }

                if (Program.DebugOutput)
                {
                    Console.WriteLine(ex);
                }

                returnCode = -1;
            }

            Logger.Info($"Return code: {returnCode}");
            return returnCode;
        }


        /// <summary>
        /// Main method without any try/catch handler which is the main entry point for integration tests
        /// </summary>
        /// <param name="args"></param>
        internal static Program MainCore(string[] args)
        {
            myProgram = new Program(args);

            // parse cmd line and execute action if any or help is printed
            myProgram.Execute();

            return myProgram; // return for unit tests to check values
        }


        /// <summary>
        /// Executes the given Command Action
        /// </summary>
        internal void Execute()
        {
            CurrentCommand = CommandFactory.CreateCommand(myInputArgs);
            try
            {
                CurrentCommand.Parse();
            }
            catch(Exception)
            {
                myIsParserError = true;
                throw;
            }
            CurrentCommand.Run();

        }
    }
}