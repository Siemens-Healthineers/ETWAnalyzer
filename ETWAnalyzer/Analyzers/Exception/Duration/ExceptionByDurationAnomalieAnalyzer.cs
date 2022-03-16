using ETWAnalyzer.Analyzers.Exception.ResultPrinter;
using ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Analyzers.Exception.Duration
{
    class ExceptionByDurationAnomalieAnalyzer : ExceptionAnalyzerBase
    {
        private double FactorMultipliedWithQuartilDistanceFlag { get; set; } = 1.5;

        private Dictionary<string, ValueAnomalieDetector<string>> TestWithDurationAnomalieDetectors { get; set; } = new();

        private Dictionary<string, Dictionary<string, double>> TestCasesWithTestsAndDurations { get; set; } = new();
        public Dictionary<string, List<TestDataFile>> DetectedLowerValueAnomalieSources { get; private set; } = new();
        public Dictionary<string, List<TestDataFile>> DetectedUpperValueAnomalieSources { get; private set; } = new();
        public Dictionary<string,List<TestDataFile>> DetectedAnomalieSources { get; private set; } = new();
        public Dictionary<string, UniqueExceptionsWithSourceFiles> TestCaseWithUniqueExceptionsOfLowerDurationAnomalie { get; private set; } = new();
        public Dictionary<string, UniqueExceptionsWithSourceFiles> TestCaseWithUniqueExceptionsOfUpperDurationAnomalie { get; private set; } = new();

        public override void AnalyzeTestRun(TestAnalysisResultCollection issues, TestRun run)
        {
            TestRunsForAnalysis.Add(GenerateReducedDeepCopyOfSourceRun(run));
            if (IsLastElement) Analyze();
        }
        public override void TakePersistentFlagsFrom(AnalyzeCommand analyzeCommand)
        {
            base.TakePersistentFlagsFrom(analyzeCommand);
            FactorMultipliedWithQuartilDistanceFlag = analyzeCommand.FactorMultipliedWithQuartilDistance;
        }
        private void Analyze()
        {
            SetTestsWithDuration();
            SetDetectedSourcesOfAnomalieInDuration();
            DetectExceptionsInSourcesOfDurationAnomalie();
        }
        private void SetTestsWithDuration()
        {
            Dictionary<string, IEnumerable<SingleTest>> testCaseWithAllTestsToAnalyze = TestRunsForAnalysis .SelectMany(t => t.Tests)
                                                                                                            .GroupBy(g => g.Key)
                                                                                                            .ToDictionary(k => k.Key, v => v.SelectMany(x => x.Value));
            foreach (var testsOfTestCase in testCaseWithAllTestsToAnalyze)
            {
                TestCasesWithTestsAndDurations.Add(testsOfTestCase.Key, testsOfTestCase.Value.SelectMany(x => x.Files).ToDictionary(x => x.FileName, x => (double)x.DurationInMs));
            }
        }
        private void SetDetectedSourcesOfAnomalieInDuration()
        {
            foreach (var testsWithDurationOfTestCase in TestCasesWithTestsAndDurations)
            {
                ValueAnomalieDetector<string> valueAnomalieDetector = new(testsWithDurationOfTestCase.Value, FactorMultipliedWithQuartilDistanceFlag);
                TestWithDurationAnomalieDetectors.Add(testsWithDurationOfTestCase.Key, valueAnomalieDetector);

                InitAnomalieDetectionForTestCase(testsWithDurationOfTestCase.Key);

                var testdataFilesOfTestCase = TestRunsForAnalysis.SelectMany(x => x.Tests[testsWithDurationOfTestCase.Key]).SelectMany(x => x.Files);
                foreach (var testdataFileOfTestCase in testdataFilesOfTestCase)
                {
                    if(valueAnomalieDetector.IsKeyOfLowValueAnomalie(testdataFileOfTestCase.FileName))
                    {
                        AddTestAsLowValueAnomalie(testsWithDurationOfTestCase.Key, testdataFileOfTestCase);
                    }
                    else if(valueAnomalieDetector.IsKeyOfHighValueAnomalie(testdataFileOfTestCase.FileName))
                    {
                        AddTestAsHighValueAnomalie(testsWithDurationOfTestCase.Key, testdataFileOfTestCase);
                    }
                }
            }
        }
        private void InitAnomalieDetectionForTestCase(string testcasename)
        {
            DetectedLowerValueAnomalieSources.Add(testcasename, new List<TestDataFile>());
            DetectedUpperValueAnomalieSources.Add(testcasename, new List<TestDataFile>());
            DetectedAnomalieSources.Add(testcasename, new List<TestDataFile>());
        }

        private void AddTestAsLowValueAnomalie(string testcasename,TestDataFile test)
        {
            DetectedLowerValueAnomalieSources[testcasename].Add(test);
            DetectedAnomalieSources[testcasename].Add(test);
        }
        private void AddTestAsHighValueAnomalie(string testcasename, TestDataFile test)
        {
            DetectedUpperValueAnomalieSources[testcasename].Add(test);
            DetectedAnomalieSources[testcasename].Add(test);
        }
        private void DetectExceptionsInSourcesOfDurationAnomalie()
        {
            DetectExceptionsInSourcesOfLowDurationAnomalie();
            DetectExceptionsInSourcesOfHighDurationAnomalie();
        }
        private void DetectExceptionsInSourcesOfLowDurationAnomalie()
            => DetectExceptionsInSourcesOfDurationAnomalie(DetectedLowerValueAnomalieSources, TestCaseWithUniqueExceptionsOfLowerDurationAnomalie);
        private void DetectExceptionsInSourcesOfHighDurationAnomalie()
            => DetectExceptionsInSourcesOfDurationAnomalie(DetectedUpperValueAnomalieSources, TestCaseWithUniqueExceptionsOfUpperDurationAnomalie);
        private void DetectExceptionsInSourcesOfDurationAnomalie(Dictionary<string, List<TestDataFile>> detectedAnomalieSources, Dictionary<string, UniqueExceptionsWithSourceFiles> addDetection)
            => detectedAnomalieSources.ToList().ForEach(testWithsources => addDetection.Add(testWithsources.Key, new UniqueExceptionsWithSourceFiles(testWithsources.Value)));
        public override void Print()
        {
            PrintLowAndHighDurationAnomalieTests();
            PrintLowAndHighDurationAnomalieTestsWithExceptionDetails();
        }

        private void PrintLowAndHighDurationAnomalieTests()
        {
            foreach (var detector in TestWithDurationAnomalieDetectors)
            {
                ColorConsole.WriteLine('\n'+detector.Key, ColorConfig.ColorHeadings);
                PrintTests(detector.Value.SourceIdentificationWithDetectedLowerAnomalieValues, "Tests with high duration Anomalie");
                PrintTests(detector.Value.SourceIdentificationWithDetectedHighAnomalieValues, "Tests with low duration Anomalie");
            }
        }

        private void PrintTests(Dictionary<string,double> testAsKeyWithAnomalie,string headLine)
        {
            ColorConsole.WriteLine('\n' + headLine + '\n', ColorConfig.ColorHeadings);
            testAsKeyWithAnomalie.Values.ToList().ForEach(x => Console.WriteLine(x));
        }

        private void PrintLowAndHighDurationAnomalieTestsWithExceptionDetails()
        {
            PrintTestsWithExceptionDetails(TestCaseWithUniqueExceptionsOfLowerDurationAnomalie, PrintLowValueAnomalie);
            PrintTestsWithExceptionDetails(TestCaseWithUniqueExceptionsOfUpperDurationAnomalie, PrintHighValueAnomalie);
        }
        private void PrintTestsWithExceptionDetails(Dictionary<string, UniqueExceptionsWithSourceFiles> testCaseWithUniqueExceptionsOfDurationAnomalie, Func<KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>, string> anomaliePrintingFunc)
        {
            var converted = testCaseWithUniqueExceptionsOfDurationAnomalie.ToDictionary(k => k.Key, v => v.Value.ExceptionsWithSources.ToDictionary(k => k.Key, v => new ExceptionSourceFileWithNextNeighboursModuleVersion[] { v.Value }));
            ExceptionAnalysisDetailPrinter printer = new(converted, this);
            printer.SetAdditionalInfoPerExceptionID(anomaliePrintingFunc);
            printer.PrintDetailedExceptionDetection();
        }
        private string PrintHighValueAnomalie(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> arg)
            => "Duration has exceeded the upper threshold";
        private string PrintLowValueAnomalie(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> arg)
            => "Duration has exceeded the lower threshold";

        public override void AnalyzeTestsByTime(TestAnalysisResultCollection issues, TestDataFile backend, TestDataFile frontend) { }

    }
}
