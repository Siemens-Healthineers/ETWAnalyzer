using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.Exception.ResultPrinter
{
    static class ConsolePrinter
    {
        private const int CellSeparatorCount = 1;
        /// <summary>
        /// Prints an indented collection of strings to console
        /// </summary>
        /// <param name="collection">collection to print</param>
        /// <param name="indentation"></param>
        /// <param name="color">output writing color</param>
        public static void PrintIndentedCollection(string[] collection, string indentation, ConsoleColor color)
            => collection.ToList().ForEach(line => ColorConsole.WriteLine(indentation + line, color));

        /// <summary>
        /// Prints a content in a cell
        /// </summary>
        /// <param name="cellContent">string to print</param>
        /// <param name="width">with of the cell</param>
        /// <param name="color">color of the content</param>
        /// <param name="isLastColumn">adds a new line</param>
        public static void PrintCell(string cellContent, int width, ConsoleColor? color = ConsoleColor.White, bool isLastColumn = false)
        {
            Console.Write("|");
            ColorConsole.Write(AlignCenter(cellContent, width - CellSeparatorCount), color);
            if (isLastColumn)
            {
                Console.Write("|\n");
            }
        }

        /// <summary>
        /// Prints the horizontal separators between the contents
        /// </summary>
        /// <param name="tableWidth"></param>
        /// <param name="color"></param>
        public static void PrintLine(int tableWidth, ConsoleColor? color = ConsoleColor.Gray)
        {
            ColorConsole.WriteLine(new string('-', tableWidth), color);
        }
        /// <summary>
        /// Prints the vertikal separators between the contents
        /// </summary>
        /// <param name="width"></param>
        /// <param name="color"></param>
        /// <param name="columns"></param>
        public static void PrintRow(int width, ConsoleColor? color, params string[] columns)
        {
            Console.Write("|");
            foreach (var c in columns)
            {
                ColorConsole.Write(AlignCenter(c, width - CellSeparatorCount), color);
                Console.Write("|");
            }
            Console.Write("\n");
        }

        /// <summary>
        /// Generates a center aligned string, surrounded by gap filling chars
        /// </summary>
        /// <param name="text">text to aligne in the center</param>
        /// <param name="width">width of the complete string</param>
        /// <param name="filler">fills the empty gaps</param>
        /// <returns>center aligned string</returns>
        public static string AlignCenter(string text, int width, char filler = ' ')
        {
            text = text.Length > width ? text.Substring(0, width - 3) + "..." : text;
            return String.IsNullOrEmpty(text)
                   ? new string(filler, width)
                   : text.PadRight(width - (width - text.Length) / 2).PadLeft(width);
        }

    }
}
