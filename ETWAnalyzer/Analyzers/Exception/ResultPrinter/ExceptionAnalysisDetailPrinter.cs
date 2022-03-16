using ETWAnalyzer.Analyzers.Exception.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.Extract;
using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ETWAnalyzer.Analyzers.Exception.ResultPrinter
{
    internal class ExceptionAnalysisDetailPrinter
    {
        private const string myExceptionIDTitle = "\n\t\tException-ID:\t";
        private const string myExceptionStackTitle = "\t\t\tException-Stack:\t";
        private const string myExceptionMsgTitle = "\t\t\tException-Message:\t";
        private const string myExceptionTypeTitle = "\t\t\tException-Type:\t";
        private const string myExceptionSoucesTitle = "\t\t\tSource-File(s):\t";
        private const string myExceptionProcessTitle = "\n\tProcess:\t";
        private const string myExceptionTestCaseTitle = "\nTestCase:";
        private Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> RelevantExceptionData { get; set; }
        private bool IsPrintingAnyOutliers { get; set; }
        private bool IsPrintingAnySporadics { get; set; }
        private bool IsPrintingAnyTrends { get; set; }
        private bool IsPrintingAnyExceptionMessage
            => Analyzer.IsPrintingFlatMsgFlag || Analyzer.IsPrintingFullMsgFlag;
        private bool IsPrintingAnyExceptionStack
            => Analyzer.IsPrintingFlatStackFlag || Analyzer.IsPrintingFullStackFlag;
        private ExceptionAnalyzerBase Analyzer { get; set; }

        private Func<KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>, string> myAdditionalInfoPerExceptionID;
        private Func<ExceptionSourceFileWithNextNeighboursModuleVersion, string> myFormattedExceptionSourceDescription;


        public ExceptionAnalysisDetailPrinter(Dictionary<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> relevantExceptionData, ExceptionAnalyzerBase analyzer)
        {
            RelevantExceptionData = relevantExceptionData;
            Analyzer = analyzer;
            SetAnalyzerSpecificFlags(analyzer);
        }
        private void SetAnalyzerSpecificFlags(ExceptionAnalyzerBase analyzer)
        {
            if (analyzer == null) throw new ArgumentException($"{analyzer} cannot be null");
            if (analyzer is ExceptionDifferenceVolatileAnalyzer)
            {
                var specificAnalyzer = (ExceptionDifferenceVolatileAnalyzer)analyzer;
                IsPrintingAnyOutliers = specificAnalyzer.SelectedCharacteristicsFlag.Any(x => x == ExceptionCharacteristic.DisjointOutliers || x == ExceptionCharacteristic.DisjointOutliersConsistentModVDiff || x == ExceptionCharacteristic.DisjointOutliersInconsistentModVDiff);
                IsPrintingAnySporadics = specificAnalyzer.SelectedCharacteristicsFlag.Any(x => x == ExceptionCharacteristic.DisjointSporadics || x == ExceptionCharacteristic.DisjointSporadicsConsistentModVDiff || x == ExceptionCharacteristic.DisjointSporadicsInconsistentModVDiff);
                IsPrintingAnyTrends = specificAnalyzer.SelectedCharacteristicsFlag.Any(x => x == ExceptionCharacteristic.DisjointTrends || x == ExceptionCharacteristic.DisjointTrendsConsistentModVDiff || x == ExceptionCharacteristic.DisjointTrendsInconsistentModVDiff);
            }
        }
        public void SetFormattedExceptionSourceDescription(Func<ExceptionSourceFileWithNextNeighboursModuleVersion, string> func)
            => myFormattedExceptionSourceDescription = func;

        /// <summary>
        /// Enables to add additional information as a string behind the id output which is separated by '\t'
        /// example: "Exception-ID:   726825528               {func return substring}"
        /// </summary>
        /// <param name="func"></param>
        public void SetAdditionalInfoPerExceptionID(Func<KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>, string> func)
            => myAdditionalInfoPerExceptionID = func;
        
        /// <summary>
        /// Prints the detailed detection of every exception
        /// Output contains: Testcase, Process, Exception-ID, Sourcecluster, Modulversiondifferences, type, flat-/ stack, flat-/message
        /// </summary>
        public void PrintDetailedExceptionDetection()
        {
            foreach (var testWithExceptions in RelevantExceptionData)
            {
                PrintTestCaseHeadLine(testWithExceptions);

                var groupedByProcesses = testWithExceptions.Value.OrderBy(x => x.Key.ID).GroupBy(x => x.Key.ProcessNamePretty).OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.ToList());
                foreach (var processWithExceptions in groupedByProcesses)
                {
                    PrintProcessHeadLine(processWithExceptions.Key);
                    processWithExceptions.Value.ForEach(exceptionWithSources => PrintExceptionAssignedDetails(exceptionWithSources));
                }
            }
            PrintAssignmentStructureOverview();
        }

        private void PrintTestCaseHeadLine(KeyValuePair<string, Dictionary<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]>> testWithExceptions)
        {
            Console.WriteLine(Environment.NewLine);
            ConsolePrinter.PrintLine(ConsoleAsExceptionTableConfig.MaxTableWidth);
            PrintTestCaseHeadLine(testWithExceptions.Key);
        }

        private void PrintExceptionAssignedDetails(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> exceptionWithSources)
        {
            PrintExceptionIDWithOptionalSubstringInfo(exceptionWithSources);
            PrintExceptionSourcesWithNextModuleVersions(exceptionWithSources);
            PrintExceptionTypeHeadLine(exceptionWithSources);
            PrintExceptionMsgIfSelected(exceptionWithSources);
            PrintExceptionStackIfSelected(exceptionWithSources);
        }

        private void PrintExceptionIDWithOptionalSubstringInfo(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> exceptionWithSources)
            => PrintExceptionIDHeadLine($"{exceptionWithSources.Key.ID}\t{ myAdditionalInfoPerExceptionID?.Invoke(exceptionWithSources)}");
        
        private void PrintExceptionSourcesWithNextModuleVersions(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> exceptionWithSources)
        {
            PrintExceptionSourceTitle();
            exceptionWithSources.Value.ToList().ForEach(source => PrintClusterWithModuleVersions(source));
        }
        private void PrintClusterWithModuleVersions(ExceptionSourceFileWithNextNeighboursModuleVersion source)
        {
            string sourceWithModulV = $"{myFormattedExceptionSourceDescription?.Invoke(source)}";
            sourceWithModulV += $"{source.SourceOfActiveException.PerformedAt}: {Path.GetFileName(source.SourceOfActiveException.FileName)}";
            sourceWithModulV += "\tMod.V.: - ";

            var (previousModIfRelevant, currentMod, followingModIfRelevant) = source.GetResponsibleModulVersionsForAlternatingExceptionState();
            sourceWithModulV += GetFormatedModuleVersionsString(previousModIfRelevant, currentMod, followingModIfRelevant);

            ConsoleColor color = GetClusterMappedColor(source.ExceptionStatePersistenceDependingCluster);

            PrintCluster(sourceWithModulV, color);
        }

        private string GetFormatedModuleVersionsString(ModuleVersion previous, ModuleVersion current, ModuleVersion following)
        {
            string formatedModV = previous != null ? $"Prev.:{previous}".PadRight(25, ' ') : "";
            formatedModV += $"Sou.:{current}".PadRight(25, ' ');
            return formatedModV += following != null ? $"Fol.:{following}" : "";
        }
        private ConsoleColor GetClusterMappedColor(ExceptionCluster? cluster)
            => ExceptionCluster.OutlierException.Equals(cluster) ? ColorConfig.ColorOutliers : ColorConfig.ColorTrends;
        private void PrintExceptionTypeHeadLine(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> exceptionWithSources)
            => PrintExceptionTypeHeadLine(exceptionWithSources.Key.Type);

        private void PrintExceptionMsgIfSelected(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> exceptionWithSources)
            => PrintExceptionMsgIfSelected(GetSelectedMsg(exceptionWithSources));

        private string GetSelectedMsg(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> exceptionWithSources)
            => Analyzer.IsPrintingFullMsgFlag ? exceptionWithSources.Key.Message : exceptionWithSources.Key.FlatMessage;

        private void PrintExceptionStackIfSelected(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> exceptionWithSources)
            => PrintExceptionStackIfSelected(GetSelectedStack(exceptionWithSources));

        private string GetSelectedStack(KeyValuePair<ExceptionKeyEvent, ExceptionSourceFileWithNextNeighboursModuleVersion[]> exceptionWithSources)
            => Analyzer.IsPrintingFullStackFlag ? exceptionWithSources.Key.Stack : exceptionWithSources.Key.FlatStack;


        /// <summary>
        /// Prints the structure of exception attributes
        /// </summary>
        void PrintAssignmentStructureOverview()
        {
            Console.WriteLine(Environment.NewLine);
            ConsolePrinter.PrintLine(ConsoleAsExceptionTableConfig.MaxTableWidth);
            Console.WriteLine("\nSelected Result Structure:");

            PrintTestCaseHeadLine("<TestCase>");
            PrintProcessHeadLine("<ProcessNamePretty>");
            PrintExceptionIDHeadLine("<ID> <Optionalinfo>");
            PrintExceptionSourceTitle();

            if (IsPrintingAnySporadics || IsPrintingAnyOutliers)    PrintCluster("<Outlier-Sourcefile with ModulVersionchanges>", ColorConfig.ColorOutliers);
            if (IsPrintingAnySporadics || IsPrintingAnyTrends)      PrintCluster("<Trend-Sourcefile with ModulVersionchanges>", ColorConfig.ColorTrends);

            PrintExceptionTypeHeadLine("<Exceptiontype>");
            PrintExceptionMsgIfSelected("<Exceptionmessage>");
            PrintExceptionStackIfSelected("<Exceptionstack>");
        }
        
        private void PrintTestCaseHeadLine(string testCase) => ColorConsole.WriteLine(myExceptionTestCaseTitle + testCase, ColorConfig.ColorHeadings);
        private void PrintProcessHeadLine(string processName) => ColorConsole.WriteLine(myExceptionProcessTitle + processName, ColorConfig.ColorRelevantProcesses);
        private void PrintExceptionIDHeadLine(string idWithOptionalInfo) => ColorConsole.WriteLine(myExceptionIDTitle + idWithOptionalInfo, ColorConfig.ColorHeadings);
        private void PrintExceptionSourceTitle() => ColorConsole.WriteLine(myExceptionSoucesTitle, ColorConfig.ColorHeadings);
        private void PrintCluster(string cluster, ConsoleColor color) => ColorConsole.WriteLine("\t\t\t\t" + cluster, color);


        private void PrintExceptionTypeHeadLine(string type)
        {
            PrintExceptionTypeTitle();
            PrintExceptionType(type);
        }
        private void PrintExceptionTypeTitle() => ColorConsole.WriteLine(myExceptionTypeTitle, ColorConfig.ColorExceptionTyp);
        private void PrintExceptionType(string type) => ColorConsole.WriteLine("\t\t\t\t" + type, ColorConfig.ColorExceptionTyp);

        private void PrintExceptionMsgIfSelected(string message)
        {
            if (IsPrintingAnyExceptionMessage)
            {
                PrintExceptionMsgTitle();
                PrintExceptionMessage(message);
            }
        }
        private void PrintExceptionMsgTitle() => ColorConsole.WriteLine(myExceptionMsgTitle, ColorConfig.ColorExceptionMsg);
        private void PrintExceptionMessage(string message)
            => ConsolePrinter.PrintIndentedCollection(message.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries), "\t\t\t\t", ColorConfig.ColorExceptionMsg);

        private void PrintExceptionStackIfSelected(string stack)
        {
            if (IsPrintingAnyExceptionStack)
            {
                PrintExceptionStackTitle();
                PrintExceptionStack(stack);
            }
        }
        private void PrintExceptionStackTitle() => ColorConsole.WriteLine(myExceptionStackTitle, ColorConfig.ColorExceptionStack);
        private void PrintExceptionStack(string stack)
            => ConsolePrinter.PrintIndentedCollection(stack.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries), "\t\t\t\t", ColorConfig.ColorExceptionStack);
    }
}
