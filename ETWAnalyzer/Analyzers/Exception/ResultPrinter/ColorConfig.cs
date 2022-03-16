using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.Exception.ResultPrinter
{
    static class ColorConfig
    {
        public const ConsoleColor ColorOutliers = ConsoleColor.Yellow;
        public const ConsoleColor ColorTrends = ConsoleColor.DarkCyan;
        public const ConsoleColor ColorHeadings = ConsoleColor.White;
        public const ConsoleColor ColorRelevantProcesses = ConsoleColor.Magenta;
        public const ConsoleColor ColorExceptionTyp = ConsoleColor.Green;
        public const ConsoleColor ColorExceptionMsg = ConsoleColor.Cyan;
        public const ConsoleColor ColorExceptionStack = ConsoleColor.White;
    }
}
