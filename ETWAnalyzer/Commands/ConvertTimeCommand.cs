//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.EventDump;
using ETWAnalyzer.Extract;
using ETWAnalyzer.ProcessTools;
using SevenZip;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static ETWAnalyzer.EventDump.DumpBase;
using static System.Net.Mime.MediaTypeNames;

namespace ETWAnalyzer.Commands
{
    internal class ConvertTimeCommand : ArgParser
    {
        public override string Help => "ETWAnalyzer -convertTime -filedir/fd Extract\\ or xx.json7z -time \"...\" [-sffn/ShowFullFileName]" + Environment.NewLine +
        "Convert a time string to an ETW session time in seconds and back." + Environment.NewLine +
        "[yellow]Examples:[/yellow] " + Environment.NewLine +
        "[green]Convert a time string to an ETW session time in seconds.[/green]" + Environment.NewLine +
        "   ETWAnalyzer -convertTime -fd issue.json7z -time \"2025-04-28 16:37:42.972\"" + Environment.NewLine +
        "   ETWAnalyzer -convertTime -fd issue.json7z -time \"2025-04-28T16:37:42.972+2:00\"" + Environment.NewLine +
        "   ETWAnalyzer -convertTime -fd issue.json7z -time -time 13:01:01.000" + Environment.NewLine +
        "   ETWAnalyzer -convertTime -fd issue.json7z -time -time 13.123s";

        bool myDebugOutputToConsole;
        string myFileDirQuery;
        List<TestDataFile> myTests;
        private string myTimeString;
        bool myShowFullFileName;

        public ConvertTimeCommand(string[] args) : this(args, null)
        { }

        public ConvertTimeCommand(string[] args, Lazy<SingleTest>[] preloadedData) : base(args)
        {
            if (preloadedData != null)
            {
                myTests = preloadedData.SelectMany(test => test.Value.Files).ToList();
            }
        }

        readonly DateTimeStyles myDefaultStyle = DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal;

        public override void Parse()
        {
            while (myInputArguments.Count > 0)
            {
                string curArg = myInputArguments.Dequeue();
                switch (curArg?.ToLowerInvariant())
                {
                    case CommandFactory.ConvertTimeCommand:
                        break;
                    case FileOrDirectoryArg:
                    case FileOrDirectoryAlias:
                        myFileDirQuery = GetNextNonArg(FileOrDirectoryArg);
                        break;
                    case "-time":
                        myTimeString = GetNextNonArg("-time");  
                        break;
                    case "-sffn":
                        myShowFullFileName = true;
                        break;
                    case "-showfullfilename":
                        myShowFullFileName = true;
                        break;
                    case DebugArg:    // -debug 
                        myDebugOutputToConsole = true;
                        Program.DebugOutput = true;
                        break;
                    case NoColorArg:
                        ColorConsole.EnableColor = false;
                        break;
                    case HelpArg:
                        throw new InvalidOperationException(HelpArg);
                    default:
                        throw new NotSupportedException($"The argument {curArg} was not recognized as valid argument");
                }
            }

            if (myTests == null && myFileDirQuery == null)
            {
                throw new NotSupportedException($"You need to enter {FileOrDirectoryArg} with an existing input file.");
            }
        }
        
        public override void Run()
        {
            DateTimeOffset? dateTime = GetDateTime();

            if (myTests == null)
            {
                TestRunData runData = new(myFileDirQuery, SearchOption.TopDirectoryOnly);
                myTests = runData.AllFiles.Select(file => file).ToList();  
            }

            if ( myTests.Count == 0 )
            {
                throw new NotSupportedException($"No input files were found for query {myFileDirQuery}");
            }

            bool bSuccess = false;

            if (dateTime != null)
            {
                foreach (var test in myTests)
                {
                    if (dateTime >= test.Extract.SessionStart && dateTime <= test.Extract.SessionEnd)
                    {
                        string timeStr = (dateTime.Value - test.Extract.SessionStart).TotalSeconds.ToString("F6", CultureInfo.InvariantCulture);
                        Console.WriteLine($"{timeStr} seconds since session start for file {GetFileName(test)}");
                        bSuccess = true;
                        break;
                    }
                }
            }
            else
            {
                foreach (var test in myTests)
                {
                    var extract = test.Extract;
                    bSuccess = GetTimeForFile(test, extract) || bSuccess ? true : false;
                }
            }

            if (!bSuccess)
            {
                Console.WriteLine($"Could not decode time string {myTimeString} with any of the known formats e.g. HH:mm:ss, {DumpFileDirBase<ConvertTimeCommand>.DateTimeFormatStrings.Last()} or {DumpFileDirBase<ConvertTimeCommand>.TimeFormatStrings.Last()}.");
            }
        }

        private bool GetTimeForFile( TestDataFile test, IETWExtract extract)
        {
            DateTimeOffset dateTime;

            foreach (var fmt in DumpFileDirBase<ConvertTimeCommand>.TimeFormatStrings.Reverse()) // try to parse with most digits first 
            {
                DateTimeOffset timeWithOffset = default;

                var currentTimeStyle = myDefaultStyle;
                for (int i = 0; i < 2; i++)
                {
                    if (DateTime.TryParseExact(myTimeString, fmt, null, currentTimeStyle, out DateTime time) ||
                        DateTimeOffset.TryParseExact(myTimeString, fmt + "K", null, DateTimeStyles.RoundtripKind | currentTimeStyle, out timeWithOffset))  // try also with time zone offset
                    {
                        // only one of the two parse calls can have succeeded. The other value is then zero and will not make it wrong.
                        var date = extract.SessionStart.Date + time.TimeOfDay + timeWithOffset.TimeOfDay;

                        dateTime = new DateTimeOffset(date, time == default ? timeWithOffset.Offset : DateTimeOffset.Now.Offset);
                        if (myDebugOutputToConsole)
                        {
                            Console.WriteLine("Could decode time with fmt string {fmt})");
                        }

                        if (dateTime > extract.SessionEnd || dateTime < extract.SessionStart)
                        {
                            if (i == 0)  // first try with UTC
                            {
                                Console.WriteLine($"Time value is outside of ETW session time. Assuming UTC time for file {GetFileName(test)}");
                                currentTimeStyle = DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces;
                                continue;
                            }
                            else // then give up
                            {
                                break;
                            }
                        }

                        string timeStr = (dateTime - extract.SessionStart).TotalSeconds.ToString("F6", CultureInfo.InvariantCulture);

                        Console.WriteLine($"{timeStr} seconds since session start for file {GetFileName(test)}");
                        return true;
                    }
                }
            }

            // convert ETW session time in seconds to human readable time format 
            try
            {
                double timInS = ParseDouble(myTimeString.TrimEnd('s'));
                DateTimeOffset time = extract.SessionStart+TimeSpan.FromSeconds(timInS);
                if( time <= extract.SessionEnd || time >= extract.SessionStart)
                {
                    Console.WriteLine($"{time.ToString(DumpFileDirBase<ConvertTimeCommand>.DateTimeFormat6)} for file {GetFileName(test)}");
                    return true;
                }
            }
            catch { }
            { }

            return false;
        }

        DateTimeOffset? GetDateTime()
        {
            DateTimeStyles dtStyle = DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal;

            DateTimeOffset dateTime;
            foreach (var fmt in DumpFileDirBase<ConvertTimeCommand>.DateTimeFormatStrings.Reverse())
            {
                if (DateTimeOffset.TryParseExact(myTimeString, fmt, null, dtStyle, out dateTime))
                {
                    if (myDebugOutputToConsole)
                    {
                        Console.WriteLine("Could decode DateTime with fmt string {fmt})");
                    }

                    return dateTime;
                }
            }

            if (DateTimeOffset.TryParseExact(myTimeString, "O", null, dtStyle, out dateTime))  // try round trip time format
            {
                return dateTime;
            }

            if (DateTimeOffset.TryParseExact(myTimeString, "yyyy-MM-ddTHH:mm:ss.fffK", null, dtStyle, out dateTime))  // used in many log files
            {
                return dateTime;
            }


            return null;
        }

        string GetFileName(TestDataFile testData)
        {
            return myShowFullFileName ? testData.FileName : Path.GetFileName(testData.FileName);
        }
    }
}
