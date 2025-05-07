//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.EventDump;
using ETWAnalyzer.Extract;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace ETWAnalyzer.Commands
{
    internal class ConvertTimeCommand : ArgParser
    {

        public override string Help => "ETWAnalyzer -convertTime -filedir/fd Extract\\ or xx.json7z -time \"...\"" + Environment.NewLine +
        "Convert a time string to an ETW session time in seconds. The file query must match exactly one file." + Environment.NewLine +
        "[yellow]Examples:[/yellow] " + Environment.NewLine +
        "[green]Convert a time string to an ETW session time in seconds.[/green]" + Environment.NewLine +
        "   ETWAnalyzer -convertTime -fd issue.json7z -time \"2025-04-28 16:37:42.972\"";



        bool myDebugOutputToConsole;
        string myFileDirQuery;
        List<TestDataFile> myTests;
        private string myTimeString;


        public ConvertTimeCommand(string[] args) : this(args, null)
        { }

        public ConvertTimeCommand(string[] args, Lazy<SingleTest>[] preloadedData) : base(args)
        {
            if (preloadedData?.Length == 1)
            {
                myTests = preloadedData.SelectMany(test => test.Value.Files).ToList();
            }
        }


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

        DateTimeOffset? GetDateTime()
        {
            DateTimeOffset dateTime;
            foreach (var fmt in DumpFileDirBase<ConvertTimeCommand>.DateTimeFormatStrings.Reverse())
            {
                if (DateTimeOffset.TryParseExact(myTimeString, fmt, null, DateTimeStyles.AssumeLocal, out dateTime))
                {
                    if (myDebugOutputToConsole)
                    {
                        Console.WriteLine("Could decode DateTime with fmt string {fmt})");
                    }

                    return dateTime;
                }
            }

            if(DateTimeOffset.TryParseExact(myTimeString, "O", null, DateTimeStyles.AssumeLocal, out dateTime))  // try round trip time format
            {
                return dateTime;
            }

            if(DateTimeOffset.TryParseExact(myTimeString, "yyyy-MM-ddTHH:mm:ss.fffK", null, DateTimeStyles.AssumeLocal, out dateTime))  // used in many log files
            {
                return dateTime;    
            }


            return null;
        }


        public override void Run()
        {
            DateTimeOffset? dateTime = GetDateTime();

            if (myTests == null)
            {
                TestRunData runData = new(myFileDirQuery, SearchOption.TopDirectoryOnly);

                if (dateTime == null)
                {
                    if (runData.AllFiles.Count > 1)
                    {
                        throw new NotSupportedException($"Time conversion is only useful for a single input file. Your query did match {runData.AllFiles.Count} files.");
                    }
                }

                if (runData.AllFiles.Count == 0)
                {
                    throw new NotSupportedException($"No input files were found for query {myFileDirQuery}");
                }


                myTests = runData.AllFiles.Select(file => file).ToList();  
            }


            if (dateTime != null)
            {
                foreach (var test in myTests)
                {
                    if (dateTime >= test.Extract.SessionStart && dateTime <= test.Extract.SessionEnd)
                    {
                        string timeStr = (dateTime.Value - test.Extract.SessionStart).TotalSeconds.ToString("F6", CultureInfo.InvariantCulture);
                        Console.WriteLine($"{timeStr} seconds since session start for file {test.FileName}");
                        return;
                    }
                }
            }
            else
            {
                var extract = myTests.First().Extract; // When we have just a time it makes only sense to convert for a single etl file. 

                foreach (var fmt in DumpFileDirBase<ConvertTimeCommand>.TimeFormatStrings.Reverse()) // try to parse with most digits first 
                {
                    DateTimeOffset timeWithOffset = default;
                    if (DateTime.TryParseExact(myTimeString, fmt, null, DateTimeStyles.AssumeLocal, out DateTime time) ||
                        DateTimeOffset.TryParseExact(myTimeString, fmt+"K", null, DateTimeStyles.RoundtripKind, out timeWithOffset))  // try also with time zone offset
                    {
                        // only one of the two parse calls can have succeeded. The other value is then zero and will not make it wrong.
                        var date = extract.SessionStart.Date + time.TimeOfDay + timeWithOffset.TimeOfDay;  

                        dateTime = new DateTimeOffset(date, timeWithOffset.Offset);
                        if (myDebugOutputToConsole)
                        {
                            Console.WriteLine("Could decode time with fmt string {fmt})");
                        }
                        string timeStr = (dateTime.Value - extract.SessionStart).TotalSeconds.ToString("F6", CultureInfo.InvariantCulture);
                        Console.WriteLine($"{timeStr} seconds since session start for file {myTests.First().FileName}");
                        return;
                    }
                }
            }

            Console.WriteLine($"Could not decode time string {myTimeString} with any of the known formats e.g. {DumpFileDirBase<ConvertTimeCommand>.DateTimeFormatStrings.Last()} or {DumpFileDirBase<ConvertTimeCommand>.TimeFormatStrings.Last()}.");
        }
    }
}
