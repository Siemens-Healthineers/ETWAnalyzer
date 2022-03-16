//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Analyzers.Exception;
using ETWAnalyzer.Analyzers.Exception.ResultPrinter;
using ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace ETWAnalyzer.Analyzers.ExceptionOccurrence
{
    class ExceptionOccurrenceAnalyzer : ExceptionAnalyzerBase
    {
        public Dictionary<string, Dictionary<ExceptionKeyEvent, List<ExceptionSourceFileWithNextNeighboursModuleVersion>>> ExceptionOrderedByOccurrenceCountWithSources { get ; private set; } = new();
        List<TestSpecificCollectionOfUniqueExceptionsWithSource> RawExceptionsWithSources { get; set; } = new();
        TestRunConfiguration TestRunConfiguration { get; } = new();

        public override void AnalyzeTestRun(TestAnalysisResultCollection issues, TestRun run)
        {
            TestRun currRun =  GenerateReducedDeepCopyOfSourceRun(run);
            RawExceptionsWithSources.Add(new TestSpecificCollectionOfUniqueExceptionsWithSource(currRun));

            if(IsLastElement)
            {
                DetermineOccurrence();
            }
        }

        private void DetermineOccurrence()
        {
            foreach (var testCase in TestRunConfiguration.ExpectedRun.TestCases)
            {
                ExceptionOrderedByOccurrenceCountWithSources.Add(testCase.TestCaseName, new Dictionary<ExceptionKeyEvent, List<ExceptionSourceFileWithNextNeighboursModuleVersion>>());
                Dictionary<ExceptionKeyEvent, List<ExceptionSourceFileWithNextNeighboursModuleVersion>> exceptionsOfTestCase = ExceptionOrderedByOccurrenceCountWithSources[testCase.TestCaseName];

                foreach (var exceptionsWithSource in RawExceptionsWithSources)
                {
                    if(exceptionsWithSource.TestSpecificExceptionsWithSourceFile.TryGetValue(testCase.TestCaseName,out UniqueExceptionsWithSourceFiles uniqueExceptions))
                    {
                        foreach (var exceptionWithSource in uniqueExceptions.ExceptionsWithSources)
                        {
                            if(exceptionsOfTestCase.TryGetValue(exceptionWithSource.Key, out List<ExceptionSourceFileWithNextNeighboursModuleVersion> detectedSources))
                            {
                                detectedSources.Add(exceptionWithSource.Value);
                                exceptionsOfTestCase.FirstOrDefault(x => x.Key.Equals(exceptionWithSource.Key)).Key.Occurrence += exceptionWithSource.Key.Occurrence;
                            }
                            else
                            {
                                exceptionsOfTestCase.Add(exceptionWithSource.Key, new List<ExceptionSourceFileWithNextNeighboursModuleVersion>() { exceptionWithSource.Value });
                            }
                        }
                    }
                }
                ExceptionOrderedByOccurrenceCountWithSources[testCase.TestCaseName] = exceptionsOfTestCase.OrderBy(x => x.Key.Occurrence).ToDictionary(k=>k.Key,v=>v.Value);
            }
        }

        public override void Print()
        {
            Console.WriteLine(Environment.NewLine);
            int maxWidth = Console.LargestWindowWidth > 0 ? Console.LargestWindowWidth - 5 : 250;
            ConsolePrinter.PrintLine(maxWidth);

            foreach (var testCasesWithExceptions in ExceptionOrderedByOccurrenceCountWithSources)
            {
                ColorConsole.WriteLine("\nTestCase: " + testCasesWithExceptions.Key, ConsoleColor.White);
                var groupedByProcesses = testCasesWithExceptions.Value.OrderBy(x => x.Key.ID).GroupBy(x => x.Key.ProcessNamePretty).OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.ToList());
                var groupedByRelevantProcesses = groupedByProcesses.Where(x => RelevantProcessNames.Contains(x.Key));
                foreach (var processWithExceptions in groupedByRelevantProcesses)
                {
                    ColorConsole.WriteLine("\n\tProcess:\t" + processWithExceptions.Key, ConsoleColor.Magenta);
                    var sortedByOccurrence = processWithExceptions.Value.OrderBy(x => x.Key.Occurrence);
                    foreach (var exceptionWithSources in sortedByOccurrence)
                    {
                        ColorConsole.WriteLine($"\n\t\tException-ID:\t{exceptionWithSources.Key.ID}\tOccurrence: {exceptionWithSources.Key.Occurrence}");
                        ColorConsole.WriteLine("\t\t\tSource-File(s):\t", ConsoleColor.White);
                        foreach (var source in exceptionWithSources.Value)
                        {
                            ColorConsole.WriteLine($"\t\t\t\t{Path.GetFileName(source.SourceOfActiveException.FileName)}", ConsoleColor.White);
                        }
                        ColorConsole.WriteLine("\t\t\tException-Type:\t", ConsoleColor.Green);
                        ColorConsole.WriteLine("\t\t\t\t" + exceptionWithSources.Key.Type, ConsoleColor.Green);
                        if (IsPrintingFlatMsgFlag || IsPrintingFullMsgFlag)
                        {
                            ColorConsole.WriteLine($"\t\t\tException-{(IsPrintingFlatMsgFlag? "Flatmessage":"Message")}\t", ConsoleColor.Cyan);
                            string msg = IsPrintingFullMsgFlag ? exceptionWithSources.Key.Message : exceptionWithSources.Key.FlatMessage;
                            ConsolePrinter.PrintIndentedCollection(msg.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries), "\t\t\t\t", ConsoleColor.Cyan);
                        }
                        if (IsPrintingFlatStackFlag || IsPrintingFullStackFlag)
                        {
                            ColorConsole.WriteLine($"\t\t\tException-{(IsPrintingFlatStackFlag? "Flatstack":"Stack")}:\t", ConsoleColor.White);
                            string stack = IsPrintingFullStackFlag ? exceptionWithSources.Key.Stack : exceptionWithSources.Key.FlatStack;
                            ConsolePrinter.PrintIndentedCollection(stack.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries), "\t\t\t\t", ConsoleColor.White);
                        }
                    }
                }

            }
        }


        public override void AnalyzeTestsByTime(TestAnalysisResultCollection issues, TestDataFile backend, TestDataFile frontend) { }

        public override void TakePersistentFlagsFrom(AnalyzeCommand analyzeCommand)
        {
            VerboseOutput = analyzeCommand.VerboseOutput;
        }
    }
}
