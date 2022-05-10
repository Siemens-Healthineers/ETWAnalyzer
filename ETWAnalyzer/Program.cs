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
        /// <returns>0 on success, > 0 the number of failed items which could not be processed.</returns>
        public static int Main(string[] args)
        {
            int returnCode = 0;
            try
            {
                Logger.Info(String.Format(CultureInfo.InvariantCulture, "Process called with args: {0}", String.Join(" ", args)));
                MainCore(args);
                returnCode = 0;
            }
            catch (Exception ex)
            {
                ColorConsole.ClipToConsoleWidth = false;
                Logger.Error($"Exception caught in main: {ex}");
                if( Program.myProgram.myIsParserError || ex is MissingInputException)
                {
                    ColorConsole.WriteEmbeddedColorLine(Program.myProgram.CurrentCommand.Help);
                }
                ColorConsole.WriteLine(Environment.NewLine + $"Error: {ex.Message} Check {Logger.Instance.LogFolder}\\ETWAnalyzer_Trace.log for full details, or use -debug switch to get full output.", ConsoleColor.Red);
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
            EnsureWPTIsInstalled();

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

        /// <summary>
        /// TraceProcessor installs a MSI which will fail if we spawn child processes. Ensure in main process that the installer
        /// has run.
        /// </summary>
        internal static void EnsureWPTIsInstalled()
        {
            // Ensure that if it is called concurrently we do let the MSI installer run only once or we will get
            // funny errors in concurrent tests or command invocations
            using (Mutex global = new(false, "WPTInstallerMutex"))
            {
                global.WaitOne();

                // Install to C:\Users\<User>\AppData\Local\Microsoft\Windows.EventTracing.Processing\<Version>\x64
                // We need to install the MSI by ourself because TraceProcessing crashes when we compile to a self contained .NET 6.0 application where it
                // cannot find its bin Directory
                string installLocation = ETWAnalyzer.TraceProcessorHelpers.Extensions.GetToolkitPath();
                string extractDestination = Path.ChangeExtension(installLocation, "tmp");
                bool alreadyExits = Directory.Exists(ETWAnalyzer.TraceProcessorHelpers.Extensions.GetToolkitPath());
                if (!alreadyExits)
                {
                    try
                    {
                        string[] installers = new string[] { "WPTx64 (OnecoreUAP)-x86_en-us.msi", "WPTx64 (DesktopEditions)-x86_en-us.msi" };
                        string srcDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                        foreach (var item in installers)
                        {
                            ProcessStartInfo processStartInfo = new()
                            {
                                UseShellExecute = false,
                                FileName = "msiexec.exe",
                                Arguments = "/q /a \"" + Path.Combine(srcDir, item) + "\" TARGETDIR=\"" + extractDestination + "\"",
                            };

                            Console.WriteLine($"Installing {processStartInfo.Arguments}");
                            int exitCode;
                            using (Process process = Process.Start(processStartInfo))
                            {
                                process.WaitForExit();
                                exitCode = process.ExitCode;
                                Console.WriteLine($"Installer finished with code {process.ExitCode}");
                            }
                        }

                        string stagingDirectory = Path.Combine(extractDestination, "staging");
                        RunInRetryLoop(delegate
                        {
                            Directory.Move(Path.Combine(extractDestination, "Windows Kits\\10\\Windows Performance Toolkit"), stagingDirectory);
                        });
                        string[] files = Directory.GetFiles(Path.Combine(extractDestination, "Windows Kits\\10\\Debuggers"), "*", SearchOption.AllDirectories);
                        foreach (string symbolsFile in files)
                        {
                            RunInRetryLoop(delegate
                            {
                                File.Move(symbolsFile, Path.Combine(stagingDirectory, Path.GetFileName(symbolsFile)));
                            });
                        }
                        RunInRetryLoop(delegate
                        {
                            Directory.Move(stagingDirectory, installLocation);
                        });
                    }
                    catch (Exception) // this throws an exception but it will install WPT on first call
                    {
                    }
                    finally
                    {
                        RunInRetryLoop(delegate
                        {
                            Directory.Delete(extractDestination, recursive: true);
                        });
                    }

                    PatchSymCacheDllWithFasterOne(installLocation, alreadyExits);
                }

                global.ReleaseMutex();
            }

        }

        private static void RunInRetryLoop(Action action)
        {
            int num = 0;
            bool flag = false;
            while (!flag)
            {
                try
                {
                    action();
                    flag = true;
                }
                catch (IOException ex)
                {
                    if (ex.HResult != -2147024891 || num == 10)
                    {
                        throw;
                    }
                    num++;
                    Thread.Sleep(100);
                }
            }
        }
        private static void PatchSymCacheDllWithFasterOne(string dest, bool alreadyExits)
        {
            const string dllName = "symcache.dll";

            if (Directory.Exists(dest))
            {
                if (!alreadyExits) // WPT folder was created
                {
                    string destFile = Path.Combine(dest, dllName);
                    string desFileBackup = destFile + "_orig";
                    string src = Path.Combine(Configuration.ConfigFiles.ExeDirectory, dllName);

                    if (File.Exists(src))
                    {
                        Logger.Info($"Replace symcache.dll from {src} to {destFile}");
                        File.Move(destFile, desFileBackup);
                        File.Copy(src, destFile);
                    }
                    else
                    {
                        string eMsg = $"Replacement {dllName} was not found in ETWAnalyzer folder at {src}!";
                        Logger.Error(eMsg);
                        ColorConsole.WriteError(eMsg);
                    }
                }
            }
            else
            {
                string msg = $"WPT local install Directory does after installation not exist! Expected {dest}";
                Logger.Warn(msg);
                Console.WriteLine(msg);
            }
        }
    }
}