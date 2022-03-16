using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.Exception.ResultPrinter
{
    internal class ConsoleAsExceptionTableConfig
    {
        public const int FirstCellWidth = 12;
        public const int MinWidthForFullModulVersion = 14;
        public const int LineProtrusionLength = 3;
        public const int CellSeparatorCount = 1;

        public static int MaxTableWidth => Console.LargestWindowWidth > 0 ? Console.LargestWindowWidth - LineProtrusionLength : 250;

        public int CountOfLastNRunsToPrint { get; private set; }
        public int CellWidth { get; private set; }
        public int TableWidth => CountOfLastNRunsToPrint * CellWidth + FirstCellWidth;


        public ConsoleAsExceptionTableConfig(int countOfRunsToPrint, string startingModVSubstringIfAlwaysEqual = "")
        {
            CountOfLastNRunsToPrint = CalculateCountOfLastPrintableRuns(countOfRunsToPrint, startingModVSubstringIfAlwaysEqual);
            CellWidth = CalculateOptimizedCellWidth();
        }

        private int CalculateCountOfLastPrintableRuns(int countOfRunsToPrint, string startingModVSubstringIfAlwaysEqual = "")
        {
            int minCellWidth = MinWidthForFullModulVersion - startingModVSubstringIfAlwaysEqual.Length;
            int freeCellSpace = MaxTableWidth - FirstCellWidth;
            int maxCountOfCells = freeCellSpace / minCellWidth;

            return TrysToPrintMoreRunsThanConsoleSpace(maxCountOfCells, countOfRunsToPrint) ? maxCountOfCells : countOfRunsToPrint;
        }
        bool TrysToPrintMoreRunsThanConsoleSpace(int maxCountOfCells, int countOfRunsToPrint) => maxCountOfCells < countOfRunsToPrint;
        private int CalculateOptimizedCellWidth()
        {
            int freeCellSpace = MaxTableWidth - FirstCellWidth;
            return freeCellSpace / CountOfLastNRunsToPrint;
        }
    }
}
