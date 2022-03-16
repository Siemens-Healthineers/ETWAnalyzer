using ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.Extensions;
using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.Exception.ResultPrinter
{
    internal class ExceptionDifferenceAnalysisOverviewPrinter
    {
        private ConsoleAsExceptionTableConfig myConfig;
        private ConsoleAsExceptionTableConfig Config => myConfig ??= new ConsoleAsExceptionTableConfig(AnalyzedRuns.Count, IsEqualModulVerStart ? StartingVersionSubstring : "");
        private List<TestRun> AnalyzedRuns { get; }
        private string StartingVersionSubstring { get; set; }
        private Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> RelevantExceptionData { get; set; }


        public ExceptionDifferenceAnalysisOverviewPrinter(Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> relevantExceptionData, List<TestRun> analyzedRuns)
        {
            RelevantExceptionData = relevantExceptionData;
            AnalyzedRuns = analyzedRuns;
            StartingVersionSubstring = GetVersionStartingSubstring();
        }

        private string GetVersionStartingSubstring()
            => new Regex(@"\d*\.\d*\.").Match(AnalyzedRuns[0].GetMainModuleVersion().Version).Value;
        private bool IsEqualModulVerStart
             => AnalyzedRuns.All(x => x.GetMainModuleVersion().Version.StartsWith(StartingVersionSubstring));
        /// <summary>
        /// Prints a testrun assigned table of exception with their states
        /// alternating states marked by the specific cluster (Outlier, Sporadic, Trend)
        /// </summary>
        public void PrintOverviewTableExceptionDetection()
        {
            Console.WriteLine("\n");
            ConsolePrinter.PrintLine(Config.TableWidth + ConsoleAsExceptionTableConfig.CellSeparatorCount);

            foreach (var testCaseWithExceptions in RelevantExceptionData)
            {
                PrintTestCaseAssignedHead(testCaseWithExceptions);

                var grouptByProcesses = testCaseWithExceptions.Value.OrderBy(x => x.Key.ID)
                                                                    .GroupBy(x => x.Key.ProcessNamePretty)
                                                                    .OrderBy(x => x.Key)
                                                                    .ToDictionary(x => x.Key, y => y.ToList());

                foreach (var exceptionWithSources in grouptByProcesses)
                {
                    PrintProcessWithExceptionCount(exceptionWithSources);
                    exceptionWithSources.Value.ForEach(exceptionWithSource => PrintExceptionClustersInCells(exceptionWithSource));
                }
            }
        }

        /// <summary>
        /// Prints the complete headline included the specific testcase with the count of detected exceptions
        /// </summary>
        /// <param name="testCaseWithExceptions"></param>
        void PrintTestCaseAssignedHead(KeyValuePair<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> testCaseWithExceptions)
        {
            PrintHeadLineForTestruns();
            ConsolePrinter.PrintCell("Excep. ID", ConsoleAsExceptionTableConfig.FirstCellWidth, ColorConfig.ColorHeadings);
            ConsolePrinter.PrintRow(Config.TableWidth - ConsoleAsExceptionTableConfig.FirstCellWidth, ColorConfig.ColorHeadings, $"Test: {testCaseWithExceptions.Key} ({testCaseWithExceptions.Value.Count} Exceptions detected)");
            ConsolePrinter.PrintLine(Config.TableWidth + ConsoleAsExceptionTableConfig.CellSeparatorCount);
        }

        /// <summary>
        /// Prints the Headline of the overview table with testruns, dates, times and versions
        /// </summary>
        void PrintHeadLineForTestruns()
        {
            ConsolePrinter.PrintLine(Config.TableWidth);

            List<List<string>> linesWithCells = CreateContentForHeadLines();
            int idxOfFirstPrintAbleSourceRun = GetIdxOfFirstPrintAbleSourceRun();
            int idxOfLastPrintAbleRun = idxOfFirstPrintAbleSourceRun + Config.CountOfLastNRunsToPrint;

            const int lineCountToPrint = 4;
            for (int idxLine = 0; idxLine < lineCountToPrint; idxLine++)
            {
                for (int idxofSourceRun = idxOfFirstPrintAbleSourceRun; idxofSourceRun < idxOfLastPrintAbleRun; idxofSourceRun++)
                {
                    linesWithCells[idxLine].Add(GetCellHeadFromSourceRuns(idxLine, idxofSourceRun));
                }
                PrintEachHeadLine(linesWithCells[idxLine]);
            }
            ConsolePrinter.PrintLine(Config.TableWidth);
        }
        private List<List<string>> CreateContentForHeadLines()
        {
            return new List<List<string>>()
            {
                new List<string>() { "Runs:" },
                new List<string>() { "Date:" },
                new List<string>() { "Time:" },
                new List<string> { IsEqualModulVerStart ? $"Mod.V.:{StartingVersionSubstring}" : "Mod.Ver.:" }
            };
        }
        private int GetIdxOfFirstPrintAbleSourceRun()
            => AnalyzedRuns[0].Parent.Runs.ToList().FindIndex(x => x.TestRunStart == AnalyzedRuns[AnalyzedRuns.Count - Config.CountOfLastNRunsToPrint].TestRunStart);
        private string GetCellHeadFromSourceRuns(int idxLine, int idxSourceRun)
        {
            var sourceRun = AnalyzedRuns[0].Parent.Runs[idxSourceRun];
            return idxLine switch
            {
                0 => $"Run[{idxSourceRun}]",
                1 => sourceRun.TestRunStart.ToString("dd.MM.yy", CultureInfo.CurrentCulture),
                2 => sourceRun.TestRunStart.ToString("hh:mm:ss", CultureInfo.CurrentCulture),
                3 => GetVaryModulVersionSubstring(sourceRun.GetMainModuleVersion().Version),
                _ => throw new ArgumentException("Invalid line index Parameter"),
            };
        }
        private void PrintEachHeadLine(List<string> headLine)
        {
            ConsolePrinter.PrintCell(headLine[0], ConsoleAsExceptionTableConfig.FirstCellWidth, ColorConfig.ColorHeadings);
            ConsolePrinter.PrintRow(Config.CellWidth, ColorConfig.ColorHeadings, headLine.Skip(1).ToArray());
        }

        private string GetVaryModulVersionSubstring(string moduleVer)
        {
            if (!IsEqualModulVerStart) return moduleVer;

            int idxFirstPoint = moduleVer.IndexOf('.');
            int idxSecondPoint = moduleVer.IndexOf('.', idxFirstPoint + 1);
            return moduleVer.Substring(idxSecondPoint + 1);
        }

        /// <summary>
        /// Prints the process with the count of the process assigned detected exceptions
        /// </summary>
        /// <param name="exceptionWithSources"></param>
        void PrintProcessWithExceptionCount(KeyValuePair<string, List<KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>>> exceptionWithSources)
        {
            ConsolePrinter.PrintCell("", ConsoleAsExceptionTableConfig.FirstCellWidth);
            ConsolePrinter.PrintRow(Config.TableWidth - ConsoleAsExceptionTableConfig.FirstCellWidth, ColorConfig.ColorRelevantProcesses, $"Process: {exceptionWithSources.Key} ({exceptionWithSources.Value.Count} Exceptions detected)");
            ConsolePrinter.PrintLine(Config.TableWidth + ConsoleAsExceptionTableConfig.CellSeparatorCount);
        }

        /// <summary>
        /// Prints the contents of the cells
        /// Cluster-content: Outlier, Starting, Ending
        /// Interpolation-content: =======
        /// </summary>
        /// <param name="exceptionWithSource"></param>
        void PrintExceptionClustersInCells(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> exceptionWithSource)
        {
            ConsolePrinter.PrintCell(exceptionWithSource.Key.ID.ToString(), ConsoleAsExceptionTableConfig.FirstCellWidth);

            string gapInterpolationString = GetInterpolationStringToTheFirstPrintableExceptionCluster(exceptionWithSource);

            for (int currRunIdxOfPrintableRuns = GetIdxOfFirstPrintAbleAnalyzedRun(); currRunIdxOfPrintableRuns < AnalyzedRuns.Count; currRunIdxOfPrintableRuns++)
            {
                ExceptionSourceFileWithNextNeighboursModuleVersion detectedSourceBelongsToRun = GetSourceBelongsToRun(AnalyzedRuns[currRunIdxOfPrintableRuns], exceptionWithSource.Value);
                if (detectedSourceBelongsToRun != null)
                {
                    gapInterpolationString = detectedSourceBelongsToRun.IsExceptionCluster(ExceptionCluster.StartingException) ? new string('=', Config.CellWidth - ConsoleAsExceptionTableConfig.CellSeparatorCount) : " ";
                }
                string cellContent = detectedSourceBelongsToRun == null ? gapInterpolationString : detectedSourceBelongsToRun.ClusterDefiningSubstring;
                ConsoleColor contentColor = GetClusterMappedColor(detectedSourceBelongsToRun?.ExceptionStatePersistenceDependingCluster);
                bool isLastElement = currRunIdxOfPrintableRuns == AnalyzedRuns.Count - 1;
                ConsolePrinter.PrintCell(cellContent, Config.CellWidth, contentColor, isLastElement);
            }
            ConsolePrinter.PrintLine(Config.TableWidth + ConsoleAsExceptionTableConfig.CellSeparatorCount);
        }
        /// <summary>
        /// Defines the Interpolation content to the first visualisable cluster content
        /// </summary>
        /// <param name="exceptionWithSource"></param>
        /// <returns>interpolation content: "========" or "         "</returns>
        private string GetInterpolationStringToTheFirstPrintableExceptionCluster(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> exceptionWithSource)
        {
            ExceptionSourceFileWithNextNeighboursModuleVersion lastExceptionSourcePreviousPrinableConsoleSpace = exceptionWithSource.Value.LastOrDefault(x => AnalyzedRuns[GetIdxOfFirstPrintAbleAnalyzedRun()].IsRunningAfter(x.SourceOfActiveException.PerformedAt));
            ExceptionSourceFileWithNextNeighboursModuleVersion firstExceptionSourceAfterPrintableConsoleSpace = exceptionWithSource.Value.FirstOrDefault(x => AnalyzedRuns[GetIdxOfFirstPrintAbleAnalyzedRun()].IsRunningAfter(x.SourceOfActiveException.PerformedAt));

            return lastExceptionSourcePreviousPrinableConsoleSpace?.ExceptionStatePersistenceDependingCluster == ExceptionCluster.StartingException ||
                   firstExceptionSourceAfterPrintableConsoleSpace?.ExceptionStatePersistenceDependingCluster == ExceptionCluster.EndingException ?
                   new string('=', Config.CellWidth - ConsoleAsExceptionTableConfig.CellSeparatorCount) : " ";
        }

        private int GetIdxOfFirstPrintAbleAnalyzedRun()
            => AnalyzedRuns.Count - Config.CountOfLastNRunsToPrint;
        private ExceptionSourceFileWithNextNeighboursModuleVersion GetSourceBelongsToRun(TestRun run, ExceptionSourceFileWithNextNeighboursModuleVersion[] sources)
            => sources.FirstOrDefault(x => run.TestRunStart.Equals(x.SourceOfActiveException.ParentTest.Parent.TestRunStart));
        private ConsoleColor GetClusterMappedColor(ExceptionCluster? cluster)
            => ExceptionCluster.OutlierException.Equals(cluster) ? ColorConfig.ColorOutliers : ColorConfig.ColorTrends;
    }
}
