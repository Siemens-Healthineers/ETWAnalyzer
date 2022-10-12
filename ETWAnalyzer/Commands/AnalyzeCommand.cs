//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers;
using ETWAnalyzer.Analyzers.Exception;
using ETWAnalyzer.Analyzers.Exception.Duration;
using ETWAnalyzer.Analyzers.Exception.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.Analyzers.ExceptionOccurrence;
using ETWAnalyzer.Analyzers.Infrastructure;
using ETWAnalyzer.Extract;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ETWAnalyzer.Commands
{
    /// <summary>
    /// Processes -analyze command line options. Constructed by <see cref="CommandFactory"/> if the arguments contain -analyze.
    /// </summary>
    class AnalyzeCommand : ArgParser
    {
        internal static readonly string HelpStringGeneral =
            "General Analysis-Arguments - Necessary:" + Environment.NewLine +
            "    -analyze         [analyzer]                    analyzer: exceptiondifferencepersistent exceptiondifferencevolatile exceptionoccurrence testcount" + Environment.NewLine +
            "    -filedir         [jsonDir]                     Directory of .json files to analyze" + Environment.NewLine +
            "    -outdir          [resultDir]                   Analysis results added to this directory" + Environment.NewLine + Environment.NewLine +
            "General Arguments - Optional:" + Environment.NewLine +
            "    -computer        [machine1 machine2]           If the test was executed on more than one machine specify the computer to analyze the data for" + Environment.NewLine +
            "    -testcase        [test1 test2]                 Select specific testcases" + Environment.NewLine +
            "    -testrunindex    [startindex endindex]         Define analysis range by testrunindexes" + Environment.NewLine +
            "    -timeRange       [dd.MM.yyyy dd.MM.yyyyy]      Define analysis range by start and end dates" + Environment.NewLine +
            "    -v                                             Enable verbose output of analzyer" + Environment.NewLine +
            "    -recursive                                     Test data is searched recursively below -filedir" + Environment.NewLine +
            new string('=', Console.WindowWidth);

        internal static readonly string HelpStringExceptionDifferenceAnalyzers =
            "Description - ExceptionDifferenceAnalyzers:         exceptiondifferencepersistent, exceptiondifferencevolatilewith used with following Arguments: " + Environment.NewLine +
            "   exceptiondifferencevolatile                      Prints the exception-differences between sequencial testruns to the console" + Environment.NewLine +
            "   exceptiondifferencepersistent                    Prints the exception-differences between sequencial testruns to the console and serializes the differences to one json with relevant differences" + Environment.NewLine +
            "                                                    and one json with irrelevant differences by userdefined rules" + Environment.NewLine + Environment.NewLine +
            "Specific optional-arguments for:                    exceptiondifferencepersistent" + Environment.NewLine +
            "   -exceptionexpircydate       [dd.MM.yyyy]         Defines the excptiondate when relevant exceptions from <AnalyzerName>DetectionRelevantExcep_<TimeStampLastRun> add to " + Environment.NewLine +
            "                            or [now - noOfDays]     StillActive<AnalyzerName>DetectionIrrelevantExcep_<TimeStampLastRun>." + Environment.NewLine +
            "                                                    Exceptions flagged as irrelevant and do not appear as relevant again. Example: relevant: expirydate < (=older) exceptiondate / irrelvant: expricydate > (=newer) exceptiondate" + Environment.NewLine +
            "   -exceptionexpircybyfirstoccurrence               By using this flag, the exceptionexpircydate is compared with the first date the exception occur. The default-case compares the last date the exception occurs" + Environment.NewLine +
            "                                                    with the given expircydate." + Environment.NewLine + Environment.NewLine +
            "Specific optional-arguments for:                    exceptiondifferencevolatile, exceptiondifferencepersistent" + Environment.NewLine +
            "   -onlystillactives                                Only currently active exceptions be noted" + Environment.NewLine + Environment.NewLine +
            "Exception-difference characteristics:               The user can choose between following exception-differences: Outlier, Trend and Sporadic" + Environment.NewLine +
            "Trend:                                              Exceptions of this output are always active over not less than 2 sequencial testruns" + Environment.NewLine +
            "                                                    example for valid exceptions (=e) in following testruns (=TR): TR[n]={e1}, TR[n+1]={e1,e3}, TR[n+2]={e2,e3}, TR[n+3]={e2}" + Environment.NewLine +
            "   -disjointtrends                                  Is disjoint to disjointsporadics and disjointoutliers.      resultsOf(-disjointtrends) = resultsOf(-disjointTrendsConsistentModVDiff) combined " + Environment.NewLine +
            "                                                    with resultsOf(-disjointTrendsInconsistentModVDiff)" + Environment.NewLine +
            "   -disjointtrendsconsistentmodvdiff                Is disjoint to disjointtrendsinconsistentmodvdiff.          The state changes(starting/ending) of the trend depend 100% on the Modulversion" + Environment.NewLine +
            "   -disjointtrendsinconsistentmodvdiff              Is disjoint to disjointtrendsconsistentmodvdiff.            The state changes(starting/ending) of the trend depend 0% on the Modulversion" + Environment.NewLine +
            "Outlier:                                            Exceptions of this output are always only active in one testrun without closest neighbours " + Environment.NewLine +
            "                                                    example for vaild exceptions (=e) in following testruns (=TR): TR[n]={e1}, TR[n+1]={e2,e3}, TR[n+2]={e1}, TR[n+3]={e3}" + Environment.NewLine +
            "   -disjointoutliers                                Is disjoint to disjointsporadics and disjointtrends.        resultsOf(disjointoutliers) = resultsOf(-disjointoutliersconsistentmodvdiff) combined " + Environment.NewLine +
            "                                                                                                                with resultsOf(-disjointoutliersinconsistentmodvdiff)" + Environment.NewLine +
            "   -disjointoutliersconsistentmodvdiff              Is disjoint to disjointoutliersinconsistentmodvdiff.        The state changes(outlier) of the outliers depend 100% on the Modulversion" + Environment.NewLine +
            "   -disjointoutliersinconsistentmodvdiff            Is disjoint to disjointoutliersconsistentmodvdiff           The state changes(outlier) of the outliers depend 0% on the Modulversion" + Environment.NewLine +
            "Sproadic:                                           Exception of this output are active over not less than 2 sequencial testruns and are active in one testrun without closest neighbours. " + Environment.NewLine +
            "                                                    In other words, the exceptions must have outlier and trend-characteristics." + Environment.NewLine +
            "                                                    example for valid exceptions (=e) in following testruns (=TR): TR[n]={e1,e3}, TR[n+1]={e2,e3}, TR[n+2]={}, TR[n+3]={e2,e3}, TR[n+3]={e1,e2}" + Environment.NewLine +
            "   -disjointsporadics                               Is disjoint to disjointtrends and disjointoutliers.         resultsOf(-disjointsporadics) = resultsOf(-disjointsporadicsconsistentmodvdiff ) combined" + Environment.NewLine +
            "                                                                                                                with resultsOf(-disjointsporadicsinconsistentmodvdiff)" + Environment.NewLine +
            "   -disjointsporadicsconsistentmodvdiff             Is disjoint to disjointsporadicsinconsistentmodvdiff.       The state changes(starting/ending/outlier) of the sporadics depend 100% on the Modulversion" + Environment.NewLine +
            "   -disjointsporadicsinconsistentmodvdiff           Is disjoint to disjointsporadicsconsistentmodvdiff.         The state changes(starting/ending/outlier) of the sporadics depend 0% on the Modulversion" + Environment.NewLine +
            new string('=', Console.WindowWidth);

        internal static readonly string HelpStringExceptionOccAnalyzer =
            "Description - ExceptionOccurrenceAnalyzer:          exceptionoccurrence used with General Analyis-Arguments" + Environment.NewLine +
            "   exceptionoccurrence                              Prints the absolute count of duplicates for each exception with the exception-sourcefiles (=files with the first time appearing exception in a testrun)" + Environment.NewLine +
            new string('=', Console.WindowWidth);

        internal static readonly string HelpStringGeneralExceptionOutputArguments =
            "Output Analysis-arguments for Exceptionanalyzers    Usage:             [-printfullmessage OR -printflatmessage] AND [-printfullstack OR -printflatstack] " + Environment.NewLine +
            "                                                    Valid Analyzers:   exceptiondifferencepersistent exceptiondifferencevolatile exceptionoccurrence" + Environment.NewLine +
            "   -printfullmessage                                Prints the full message of every exception in the results." + Environment.NewLine +
            "   -printflatmessage                                Prints the flat message (cleaned from variable substrings - used for to find equal exceptions) of every exception in the results." + Environment.NewLine +
            "   -printfullstack                                  Prints the full stack of every exception in the results." + Environment.NewLine +
            "   -printflatstack                                  Prints the flat stack (cleaned from irrelevant substrings - starting by execution and ending by exception throw - used for to find equal exceptions)" + Environment.NewLine +
            "                                                    of every exception in the results." + Environment.NewLine +
            new string('=', Console.WindowWidth);

        internal static readonly string HelpStringTestCountAnalyzer =
            "Description - TestCountAnalyzer:                    testcount used with General Analysis-Arguments" + Environment.NewLine +
            "   testcount                                        Prints the testcount-difference starting test and the testcout-difference ending test" + Environment.NewLine +
            new string('=', Console.WindowWidth);

        internal static readonly string HelpStringExceptionByDurationAnomalieAnalyzer =
            "Description - ExceptionByDurationAnomalieAnalyzer: exceptionbydurationanomalie used with General-Analysis-Arguments" + Environment.NewLine +
            "   exceptionbydurationanomalie                     Detects the duration anomalie of tests by an algorithm" + Environment.NewLine +
            "                                                   Quartildistance = MedianAt75% - MedianAt25%" + Environment.NewLine +
            "                                                   UpperThreshold = MedianAt75% + factor * Quartildistance        factor default is 1.5" + Environment.NewLine +
            "                                                   LowerThreshold = MedianAt25% - factor * Quartildistance" + Environment.NewLine +
            "                                                   Anomalie: duration < LowerThreshold or duration > UpperThreshold" + Environment.NewLine +
            "   -factor [factor]                                To change the Threshold values" + Environment.NewLine +
            new string('=', Console.WindowWidth);

        internal static readonly string HelpStringUseCaseExamples =
            "Analyzer Usecases:" + Environment.NewLine +
            "ExceptionDifferenceAnalyzer:                        exceptiondifferencevolatile" + Environment.NewLine + Environment.NewLine +

            "Default case - Prints sourcefiles, exceptiontype, cleaned-stack and cleaned-message of sporadic, trend and outlier exception characteristics of all runs in inputfolder:" + Environment.NewLine +
            @"ETWAnalyzer.exe -analyze exceptionDifferencevolatile -filedir C:\jsonDir" + Environment.NewLine +

            "Prints Sourcefiles and exceptiontype of sporadic exception characteristics between testrun 10 and 20:" + Environment.NewLine +
           @"   ETWAnalyzer.exe -analyze exceptionDifferencevolatile -filedir C:\jsonDir -outdir C:\outDir -testrunindex 10 20 -disjointsporadics" + Environment.NewLine + Environment.NewLine +

            "Prints Sourcefiles and exceptiontype of sporadic and trend exception characteristics between testrun 10 and 20:" + Environment.NewLine +
           @"   ETWAnalyzer.exe -analyze exceptionDifferencevolatile -filedir C:\jsonDir -outdir C:\outDir -testrunindex 10 20 -disjointsporadics -disjointtrends" + Environment.NewLine + Environment.NewLine +

            "Prints Sourcefiles and exceptiontype of sporadic, trend and outlier exception characteristics between testrun 10 and 20:" + Environment.NewLine +
           @"   ETWAnalyzer.exe -analyze exceptionDifferencevolatile -filedir C:\jsonDir -outdir C:\outDir -testrunindex 10 20 -disjointsporadics -disjointtrends -disjointoutliers" + Environment.NewLine + Environment.NewLine +

            "Prints sourcefiles, exceptiontype, stack and message of sporadic, trend and outlier exception characteristics between testrun 10 and 20:" + Environment.NewLine +
           @"   ETWAnalyzer.exe -analyze exceptionDifferencevolatile -filedir C:\jsonDir -outdir C:\outDir -testrunindex 10 20 -disjointsporadics -disjointtrends -disjointoutliers -printfullmessage -printfullstack" + Environment.NewLine + Environment.NewLine +

            "Prints sourcefiles, exceptiontype, cleaned-stack and cleaned-message of sporadic, trend and outlier exception characteristics between testrun 10 and 20:" + Environment.NewLine +
           @"   ETWAnalyzer.exe -analyze exceptionDifferencevolatile -filedir C:\jsonDir -outdir C:\outDir -testrunindex 10 20 -disjointsporadics -disjointtrends -disjointoutliers -printflatmessage -printflatstack" + Environment.NewLine + Environment.NewLine +

            "Prints sourcefiles, exceptiontype, cleaned-stack and cleaned-message of sporadic, trend and outlier exception characteristics between testrun 10 and 20 (only exception which are active in testrun 20):" + Environment.NewLine +
           @"   ETWAnalyzer.exe -analyze exceptionDifferencevolatile -filedir C:\jsonDir -outdir C:\outDir -testrunindex 10 20 -disjointsporadics -disjointtrends -disjointoutliers -printflatmessage -printflatstack -onlystillactives" + Environment.NewLine + Environment.NewLine +

            "Prints sourcefiles, exceptiontype, cleaned-stack and cleaned-message of trend exception characteristics (only exceptions which states(active/inactive) correlate 100% with alternating modulversion) " + Environment.NewLine +
            "between testrun 10 and 20 (only exception which are active in testrun 20):" + Environment.NewLine +
           @"   ETWAnalyzer.exe -analyze exceptionDifferencevolatile -filedir C:\jsonDir -outdir C:\outDir -testrunindex 10 20 -disjointtrendsconsistentmodvdiff -printflatmessage -printflatstack -onlystillactives" + Environment.NewLine + Environment.NewLine +

            "Prints sourcefiles, exceptiontype, cleaned-stack and cleaned-message of trend exception characteristics (only exceptions which states(active/inactive) do not correlate with alternating modulversion)" + Environment.NewLine +
            "between testrun 10 and 20 (only exception which are active in testrun 20):" + Environment.NewLine +
           @"   ETWAnalyzer.exe -analyze exceptionDifferencevolatile -filedir C:\jsonDir -outdir C:\outDir -testrunindex 10 20 -disjointtrendsinconsistentmodvdiff -printflatmessage -printflatstack -onlystillactives" + Environment.NewLine + Environment.NewLine +

            "ExceptionDifferenceAnalyzer:                        exceptiondifferencepersistent (supports the same Arguments as above and additional arguments below)" + Environment.NewLine + Environment.NewLine +

            "Prints sourcefiles, exceptiontype, cleaned-stack and cleaned-message of sporadic, trend and outlier exception characteristics between testrun 10 and 20 to console and json-files" + Environment.NewLine +
            "json with irrelevant exceptions:   exceptions which are at least active since the 15.11.2021" + Environment.NewLine +
            "json with relevant exceptions:     all other still active exceptions which are not already flaged as irrelevant before" + Environment.NewLine +
           @"   ETWAnalyzer.exe -analyze exceptionDifferencepersistent -filedir C:\jsonDir -outdir C:\outDir -testrunindex 10 20 -disjointsporadics -disjointtrends -disjointoutliers -printflatmessage -printflatstack -onlystillactives" + Environment.NewLine +
            "   -exceptionexpircydate 15.11.2021" + Environment.NewLine + Environment.NewLine +

            "ExceptionOccurrenceAnalyzer:                        exceptionoccurrence (prints the absoute count of duplicates for each exception with the exception-sourcefiles(=files with the first time appearing exception in a testrun))" + Environment.NewLine + Environment.NewLine +

            "Prints sourcefiles, exceptiontype, cleaned-stack, cleaned-message and number of duplicates exceptions between testrun 10 and 20:" + Environment.NewLine +
           @"   ETWAnalyzer.exe -analyze exceptionoccurrence -filedir C:\jsonDir -outdir C:\outDir -testrunindex 10 20 -printflatmessage -printflatstack" + Environment.NewLine + Environment.NewLine +

           "Prints sourcefiles, exceptiontype, stack, message and number of duplicates exceptions between testrun 10 and 20:" + Environment.NewLine +
           @"   ETWAnalyzer.exe -analyze exceptionoccurrence -filedir C:\jsonDir -outdir C:\outDir -testrunindex 10 20 -printfullmessage -printfullstack" + Environment.NewLine + Environment.NewLine +

            "TestCountAnalyzer:                                  testcount" + Environment.NewLine + Environment.NewLine +

            "Prints the sourcefiles where testcountdifferences start or end" + Environment.NewLine +
           @"   ETWAnalyzer.exe -analyze testcount -filedir C:\jsonDir -outdir C:\outDir -testrunindex 10 20" + Environment.NewLine + Environment.NewLine +
            "ExceptionAnomalieByDurationAnalyzer:                exceptionanomaliebyduration" + Environment.NewLine + Environment.NewLine +
            "Prints the anomalie-testsource with including exceptiontype,cleaned-stack,cleaned-message between testrun 10 and 20" + Environment.NewLine +
           @"   ETWAnalyzer.exe -analyze testcount -filedir C:\jsonDir -testrunindex 10 20" + Environment.NewLine +
            new string('=', Console.WindowWidth);

        internal const string TestRunIndexArg = "-testrunindex";
        internal const string ComputerArg = "-computer";
        internal const string TestCaseArg = "-testcase";
        internal const string TimeRangeArg = "-timerange";
        internal const string ExceptionExpircyDateArg = "-exceptionexpircydate";
        internal const string IrrelevantMeasuredFromFirstExceptionOccArg = "-exceptionexpircybyfirstoccurrence";
        internal const string DisjointTrendsArg = "-disjointtrends";
        internal const string DisjointTrendsConsistentModVDiffArg = "-disjointtrendsconsistentmodvdiff";
        internal const string DisjointTrendsInconsistentModVDiffArg = "-disjointtrendsinconsistentmodvdiff";
        internal const string DisjointOutliersArg = "-disjointoutliers";
        internal const string DisjointOutliersConsistentModVDiffArg = "-disjointoutliersconsistentmodvdiff";
        internal const string DisjointOutliersInconsistentModVDiffArg = "-disjointoutliersinconsistentmodvdiff";
        internal const string DisjointSporadicsArg = "-disjointsporadics";
        internal const string DisjointSporadicsConsistentModVDiffArg = "-disjointsporadicsconsistentmodvdiff";
        internal const string DisjointSporadicsInconsistentModVDiffArg = "-disjointsporadicsinconsistentmodvdiff";
        internal const string PrintFullStackArg = "-printfullstack";
        internal const string PrintFlatStackArg = "-printflatstack";
        internal const string PrintFullMsgArg = "-printfullmessage";
        internal const string PrintFlatMsgArg = "-printflatmessage";
        internal const string FactorMulipliedWithQuartilDistanceArg = "-factor";
        internal const string OnlyStillActivesArg = "-onlystillactives";


        public bool DisablePrint { get; set; }

        /// <summary>
        /// Default Helpstring which prints all analyzer help
        /// </summary>
        public static readonly string HelpString = string.Concat(HelpStringGeneral, HelpStringExceptionDifferenceAnalyzers, HelpStringExceptionOccAnalyzer, HelpStringGeneralExceptionOutputArguments, HelpStringTestCountAnalyzer,HelpStringExceptionByDurationAnomalieAnalyzer, HelpStringUseCaseExamples);
        public override string Help 
            => Analyzers.Count == 1 ? GetAnalyzerSpecificHelp() : HelpString;

        private string GetAnalyzerSpecificHelp()
        {
            string currHelp = "";
            
            switch (Analyzers.First())
            {
                case ExceptionDifferencePersistentAnalyzer exceptionDifferenceAnalyzer:
                    currHelp = HelpStringGeneral + HelpStringExceptionDifferenceAnalyzers + HelpStringGeneralExceptionOutputArguments + HelpStringUseCaseExamples; break;
                case ExceptionOccurrenceAnalyzer exceptionOccAnalyzer:
                    currHelp = HelpStringGeneral + HelpStringExceptionOccAnalyzer + HelpStringGeneralExceptionOutputArguments + HelpStringUseCaseExamples; break;
                case TestCountAnalyzer testCountAnalyzer:
                    currHelp = HelpStringGeneral + HelpStringTestCountAnalyzer + HelpStringUseCaseExamples; break;
                case ExceptionByDurationAnomalieAnalyzer exceptionByDurationAnomalieAnalyzer:
                    currHelp = HelpStringGeneral + HelpStringExceptionByDurationAnomalieAnalyzer + HelpStringUseCaseExamples;break;
                case FreshBackendAnalyzer e: break;
                default:
                    break;
            }
            return currHelp;
        }

        /// <summary>
        /// Contains extraction options that are linked to create new instance of the analyzers
        /// </summary>
        readonly Dictionary<AnalyzeOptions, Func<AnalyzerBase>> myAnalyzerFactory = new()
        {
            { AnalyzeOptions.ExceptionOccurrence, () => new ExceptionOccurrenceAnalyzer() },
            { AnalyzeOptions.ExceptionDifferenceVolatile, () => new ExceptionDifferenceVolatileAnalyzer() },
            { AnalyzeOptions.ExceptionDifferencePersistent, () => new ExceptionDifferencePersistentAnalyzer() },
            { AnalyzeOptions.ExceptionByDurationAnomalie , () => new ExceptionByDurationAnomalieAnalyzer()},
            { AnalyzeOptions.TestCount, () => new TestCountAnalyzer() },
            { AnalyzeOptions.FreshBackend, () => new FreshBackendAnalyzer() },
        };

        /// <summary>
        /// The -analyze xxxx argument can be one of the following lower cased values
        /// </summary>
        public enum AnalyzeOptions
        {
            ExceptionOccurrence,
            ExceptionDifferenceVolatile,
            ExceptionDifferencePersistent,
            ExceptionByDurationAnomalie,
            FreshBackend,
            TestCount,
            Disk,
            CPU,
            Memory,
            Exception,
        }

        /// <summary>
        /// Input/Output arguments from -extract / -analyse command
        /// Can be Disk,CPU,Memory,VirtualAlloc,Exception / exceptionOccurrence,TestCount  where on the command line each one is
        /// separated with a comma
        /// </summary>
        List<string> myProcessingActionList;

        readonly Dictionary<string, AnalyzeOptions> myAnalyzingEnumStringMap = Enum.GetValues(typeof(AnalyzeOptions))
                                                                          .Cast<AnalyzeOptions>()
                                                                          .ToDictionary(x => x.ToString().ToLowerInvariant());
        public List<string> TestCaseNames { get; private set; } = new();

        public List<AnalyzerBase> Analyzers { get; } = new List<AnalyzerBase>();

        public TestRunData TestRunData { get; private set; }

        public string InputFileOrDirectory { get; private set; }
        public string OutDir { get; private set; }
        public SearchOption SearchOption { get; private set; } = SearchOption.TopDirectoryOnly;
        public List<string> ComputerNames { get; private set; } = new();

        int myIndexDiff = -1;
        int myStartIndex;
        public int StartIndex
        {
            private set { myStartIndex = value; }
            get { return myIndexDiff > 0 ? EndIndex - myIndexDiff : myStartIndex; }
        }

        private int myEndIndex = -1;
        public int EndIndex
        {
            set => myEndIndex = value; 
            get { return myEndIndex == -1 ? TestRunData.Runs.Count - 1 : myEndIndex; }
        }
        private int? myRunsStartIdx = null;
        public int RunsStartIdx
        { 
            set => myRunsStartIdx = value; 
            get => myRunsStartIdx ??= 0; 
        }
        private int? myRunsEndIdx = null;
        public int RunsEndIdx
        { 
            set => myRunsEndIdx = value; 
            get => myRunsEndIdx ??= TestRunData.Runs.Count - 1; 
        }

        /// <summary>
        /// Time Range where test runs are selected, By default the range is from DateTime.MinValue - DateTime.MaxValue
        /// </summary>
        public KeyValuePair<DateTime, DateTime> StartAndStopDates
        {
            get;
            private set;
        } = new KeyValuePair<DateTime, DateTime>(DateTime.MinValue, DateTime.MaxValue);

        private KeyValuePair<DateTime, DateTime>? myStartStop = null;
        public KeyValuePair<DateTime, DateTime> StartStop
        {
            set => myStartStop = value; 
            get => myStartStop ??= new KeyValuePair<DateTime, DateTime>(TestRunData.Runs[RunsStartIdx].TestRunStart, TestRunData.Runs[RunsEndIdx].TestRunEnd);
        }
        public bool VerboseOutput { get; private set; }
        public bool IsPrintingFullMsg { get; set; }
        public bool IsPrintingFlatMsg { get; set; } = true;
        public bool IsPrintingFullStack { get; set; }
        public bool IsPrintingFlatStack { get; set; } = true;
        public bool IsStillActiveExceptionDetector { get; set; }
        public DateTime ExceptionExpircyDate { get; private set; } = DateTime.Now - TimeSpan.FromDays(14);
        public bool IsIrrelevantMeasuredFromFirstExceptionOcc { get; set; }
        public List<string> ExceptionCharacteristicStrings { get; private set; } = new List<string>();
        private void AddOnlyExceptionsOfCharacteristic(string characteristicArg)
        {
            string characteristic = characteristicArg.Substring(1);
            if(ExceptionCharacteristicStrings.Any(c => c.Contains(characteristic)))
            {
                throw new ArgumentException("Only one Trend-, Sporadic- and Outliercharacteristic can be selected. If the consistent and inconsistent modulversion differences are interesting: Select for example -disjointtrends (= disjointtrendsconsistentmodvdiff U disjointtrendsinconsistentmodvdiff)");
            }
            ExceptionCharacteristicStrings.Add(characteristic);
        }
        public double FactorMultipliedWithQuartilDistance { get; private set; } = 1.5;
        public AnalyzeCommand(string[] args) : base(args) { }

        public override void Parse()
        {
            while (myInputArguments.Count > 0)
            {
                string arg = myInputArguments.Dequeue();
                switch (arg = arg.ToLowerInvariant())
                {
                    case CommandFactory.AnalyzeArg: // -analyze xxxx
                        myProcessingActionList = GetArgList(CommandFactory.AnalyzeArg);
                        ParseActionList();
                        break;
                    case FileOrDirectoryArg:
                        string path = GetNextNonArg(FileOrDirectoryArg);
                        InputFileOrDirectory = ArgParser.CheckIfFileOrDirectoryExistsAndExtension(path, JsonExtension); break;
                    // All optional Arguments
                    case OutDirArg:
                        path = GetNextNonArg(OutDirArg);
                        OutDir = ArgParser.CheckIfFileOrDirectoryExistsAndExtension(path);
                        break;
                    case "-verbose":
                        VerboseOutput = true;
                        break;
                    case RecursiveArg:
                        SearchOption = SearchOption.AllDirectories;
                        break;
                    case TestRunIndexArg:
                        List<string> indexData = GetArgList(TestRunIndexArg);
                        SetStartAndEndIndex(indexData);
                        break;
                    case ComputerArg:
                        ComputerNames = GetArgList(ComputerArg);
                        break;
                    case TestCaseArg:
                        TestCaseNames = GetArgList(TestCaseArg);
                        break;
                    case TimeRangeArg:
                        StartAndStopDates = ConvertStringListToDate(GetArgList(TimeRangeArg));
                        break;
                    case DebugArg:    // -debug 
                        Program.DebugOutput = true;
                        break;
                    case NoColorArg:
                        ColorConsole.EnableColor = false;
                        break;
                    case ExceptionExpircyDateArg:
                        List<string> expiryData = GetArgList(ExceptionExpircyDateArg);
                        SetExpircyDate(expiryData);
                        break;
                    case IrrelevantMeasuredFromFirstExceptionOccArg:
                        IsIrrelevantMeasuredFromFirstExceptionOcc = true;
                        break;
                    case PrintFullMsgArg:
                        IsPrintingFullMsg = true;
                        break;
                    case PrintFullStackArg:
                        IsPrintingFullStack = true;
                        break;
                    case PrintFlatMsgArg:
                        IsPrintingFlatMsg = true;
                        break;
                    case PrintFlatStackArg:
                        IsPrintingFlatStack = true;
                        break;
                    case OnlyStillActivesArg:
                        IsStillActiveExceptionDetector = true;
                        break;
                    case DisjointOutliersArg:
                    case DisjointOutliersConsistentModVDiffArg:
                    case DisjointOutliersInconsistentModVDiffArg:
                    case DisjointSporadicsArg:
                    case DisjointSporadicsConsistentModVDiffArg:
                    case DisjointSporadicsInconsistentModVDiffArg:
                    case DisjointTrendsArg:
                    case DisjointTrendsConsistentModVDiffArg:
                    case DisjointTrendsInconsistentModVDiffArg:
                        AddOnlyExceptionsOfCharacteristic(arg);
                        break;
                    case FactorMulipliedWithQuartilDistanceArg:
                        FactorMultipliedWithQuartilDistance = Convert.ToDouble(GetNextNonArg(FactorMulipliedWithQuartilDistanceArg));
                        break;
                    default:
                        throw new NotSupportedException($"Analyzer argument {arg} is not valid.");
                }
            }
            if (InputFileOrDirectory == null ) throw new NullReferenceException("Input directory or file must be set");

        }
        private void SetExpircyDate(List<string> expiryData)
        {
            ExceptionExpircyDate = expiryData.Count switch
            {
                1 => DateTime.Parse(expiryData[0], new CultureInfo("de")),
                3 => (expiryData[0] == "now" && expiryData[1] == "-") ? DateTime.Now - TimeSpan.FromDays(int.Parse(expiryData[2], CultureInfo.InvariantCulture)) : throw new ArgumentException($"Invalid Input. Valid: dd.MM.yyyy or now - days"),
                _ => throw new ArgumentException($"Invalid Input. Valid: dd.MM.yyyy or now - days"),
            };
        }


        /// <summary>
        /// Excludes all unrelevant tests - setted by command line
        /// Every file belongs to an analysis result. This result contains the results of all analyzers.
        /// </summary>
        public override void Run()
        {
            SetPersistentAnalyzerFlags();

            TestRunData = new TestRunData(InputFileOrDirectory, SearchOption, OutDir);

            var (allSingleTests, testruns) = GenerateRelevantAnalysisData();

            TestAnalysisResultCollection results = new();

            Analyze(allSingleTests, results);
            Analyze(testruns, results);

            if ( DisablePrint )
            {
                PrintAnalysis();
            }

        }

        private void SetPersistentAnalyzerFlags()
            => Analyzers.ForEach(a => a.TakePersistentFlagsFrom(this));
        

        private void Analyze(List<SingleTest> allSingleTests,TestAnalysisResultCollection results)
        {
            for (int i = 0; i < allSingleTests.Count; i++)
            {
                foreach (var analyzer in Analyzers)
                {
                    if (IsLastElement(i, allSingleTests)) 
                    {
                        analyzer.IsLastElement = true;
                    }
                    analyzer.AnalyzeTestsByTime(results, allSingleTests[i].Backend, allSingleTests[i].Frontend);
                    analyzer.IsLastElement = false;
                }
            }
        }
        private void Analyze(TestRun[] testruns, TestAnalysisResultCollection results)
        {
            for (int i = 0; i < testruns.Length; i++)
            {
                foreach (var analyzer in Analyzers)
                {
                    if (IsLastElement(i, testruns))
                    {
                        analyzer.IsLastElement = true;
                    }
                    analyzer.AnalyzeTestRun(results, testruns[i]);
                    analyzer.IsLastElement = false;
                }
            }
        }
        private bool IsLastElement(int currIdx, ICollection sourceCollection) => currIdx == sourceCollection.Count - 1;
        private void PrintAnalysis()
        {
            Analyzers.ForEach(analyzer => analyzer.Print());
        }

        /// <summary>
        /// Parsing extraction options
        /// </summary>
        public void ParseActionList()
        {
            string processingAction = null;
            bool wrongCommand = true;
            foreach (var iterateActions in myProcessingActionList)
            {
                wrongCommand = true;
                processingAction = iterateActions.ToLowerInvariant();

                if (myAnalyzingEnumStringMap.TryGetValue(processingAction, out AnalyzeOptions enumActionAnalyze))
                {
                    AddAnalyzingAction(enumActionAnalyze);
                    wrongCommand = false;
                }

                if (processingAction == "samplescreenshot")
                {
                    wrongCommand = false;
                }

                if (wrongCommand)
                {
                    throw new ArgumentException($"Wrong action command: {processingAction}");
                }
            }
        }

        private void AddAnalyzingAction(AnalyzeOptions option)
        {
            Analyzers.Add(myAnalyzerFactory[option]());
        }

        /// <summary>
        /// Vaild non Arguments after -testrunindex:
        /// testrunindex such as 3                                  -> analyzes: testrun[3]
        /// testrunstartindex testrunendindex such as 1 5           -> analyzes: testrun[1] to testrun[5]
        /// lasttestrunindx - lastrunstoAnalyze such as last - 5    -> analyzes: testrun[testrun.Count-1 -5] to testrun[testrun.Count-1]
        /// </summary>
        /// <param name="indexData">non args after -testrunindex</param>
        private void SetStartAndEndIndex(List<string> indexData)
        {
            switch (indexData.Count)
            {
                case 1:
                    if (!Int32.TryParse(indexData[0], out int indexA))
                    {
                        throw new InvalidCastException("Integer expected.");
                    }
                    RunsStartIdx = RunsEndIdx = indexA;
                    break;
                case 2:
                    if (!Int32.TryParse(indexData[0], out indexA) || !Int32.TryParse(indexData[1], out int indexB))
                    {
                        throw new InvalidCastException("Integer expected.");
                    }
                    RunsStartIdx = Math.Min(indexA, indexB);
                    RunsEndIdx = Math.Max(indexA, indexB);
                    break;
                case 3:
                    if (indexData[0] == "last" && indexData[1] == "-" && Int32.TryParse(indexData[2], out indexA))
                    {
                        RunsStartIdx = RunsEndIdx - indexA;
                    }
                    break;
                default:
                    throw new InvalidDataException("Valid Args: startindex endindex or endindex - x or startindex");
            }
        }

        /// <summary>
        /// Converts a List of strings into a List of Date
        /// </summary>
        /// <param name="inputList"></param>
        /// <returns>Timerange of data to extract/analyze</returns>
        static public KeyValuePair<DateTime, DateTime> ConvertStringListToDate(List<string> inputList)
        {
            List<DateTime> tempList = new()
            {
                DateTime.MinValue,
                DateTime.MaxValue
            };

            string[] tempRange = inputList[0].Split(' ');

            if (tempRange.Length == 1 && !tempRange[0].Contains("."))
            {
                try
                {
                    double elapsedDays = int.Parse(inputList[0], CultureInfo.InvariantCulture);
                    tempList[0] = DateTime.Now.Subtract(TimeSpan.FromDays(elapsedDays));
                }
                catch (FormatException)
                {
                    throw new FormatException($"Argument: {inputList[0]} should be an integer.");
                }
            }
            else
            {
                for (int i = 0; i < tempRange.Length; i++)
                {
                    tempList[i] = DateTime.Parse(tempRange[i], new CultureInfo("de"));  // use German culture to parse dates even if English Locale is set!
                }
            }

            tempList.Sort();

            return new KeyValuePair<DateTime, DateTime>(tempList[0], tempList[1]);
        }



        /// <summary>
        /// Exclude unrelevant singletests
        /// </summary>
        /// <returns>filtered singletest list</returns>
        Tuple<List<SingleTest>, TestRun[]> GenerateRelevantAnalysisData()
        {
            TestRun[] matchingTestRuns = TestRunData.RunsIncluding(TestCaseNames, ComputerNames, StartStop);

            List<SingleTest> matchingSingleTests = TestRun.ExistingSingleTestsIncludeComputerAndTestNameAndDateFilter(TestCaseNames, ComputerNames, StartStop, matchingTestRuns);

            return Tuple.Create(matchingSingleTests, matchingTestRuns);
        }
    }
}
