//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.ProcessTools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.Infrastructure
{
    /// <summary>
    /// The MultiLineFormatter class is responsible for printing multiline column data to the console with proper wrapping of output. 
    /// It is used to format and display tabular data in a visually appealing way.
    /// </summary>
    /// <remarks>
    /// The class has the following key features:
    /// 	Columns Property: This property represents the columns that will be printed. It is an array of ColumnDefinition objects.
    /// 	Constructor: The class has a constructor that accepts a variable number of ColumnDefinition objects.These objects define the properties of each column, such as the title, data width, and color.
    /// 	Print Methods: The class provides two overloaded Print methods.The first method is used to print a line of data, and the second method is used to print column titles.Both methods accept a boolean parameter addNewline to indicate whether a new line should be added after printing.
    /// Overall, the MultiLineFormatter class provides a convenient way to format and print multiline column data to the console, making it easier to display tabular data in a readable format.
    /// </remarks>
    internal class MultiLineFormatter
    {
        /// <summary>
        /// Columns which are printed.
        /// </summary>
        public ColumnDefinition[] Columns { get; set; }

        /// <summary>
        /// Used for unit testing 
        /// </summary>
        internal Action<string, ConsoleColor?> Printer = ColorConsole.Write;

        /// <summary>
        /// Create a formatter with a given set of columns
        /// </summary>
        /// <param name="columns"></param>
        public MultiLineFormatter(params ColumnDefinition[] columns)
        {
            Columns = columns;
        }

        /// <summary>
        /// Get wrapped data with proper alignment. Headers are left aligned, while row data is right aligned to get proper
        /// number alignment.
        /// </summary>
        /// <param name="lineNo">Starts with 0</param>
        /// <param name="isHeader">When header is true data is left aligned.</param>
        /// <param name="columnData">Row data to print. The count of columnData elements must match the number of (including disabled) columns.</param>
        /// <returns>List of column data to print with their column definitions. If line contains no data the list is empty.</returns>
        /// <exception cref="NotSupportedException"></exception>
        private List<KeyValuePair<string,ColumnDefinition>> GetCombinedFormattedLine(int lineNo, bool isHeader, params string[] columnData)
        {
            List<KeyValuePair<string, ColumnDefinition>> columns = new();
            
            bool allEmpty = true;
            int idx = 0;
            foreach(ColumnDefinition curColumn in Columns)
            {
                if( columnData.Length <= idx )
                {
                    throw new NotSupportedException($"Data is missing for column {curColumn.Title}.");
                }

                string nextData = curColumn.Enabled ? GetLine(curColumn, columnData[idx], lineNo, false) : null;
                if( nextData != null)
                {
                    columns.Add(new KeyValuePair<string, ColumnDefinition>(nextData, curColumn));
                    allEmpty = (allEmpty && nextData == "") ? true : false;
                }

                idx++;
            }

            if( !allEmpty)
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    var current = columns[i];

                    if (current.Value.DataWidth > 0)
                    {
                        columns[i] = new KeyValuePair<string, ColumnDefinition>(columns[i].Key.WithWidth(isHeader ? (-1) * current.Value.DataWidth : current.Value.DataWidth) + " ", current.Value);
                    }
                }
            }

            if (allEmpty)
            {
                columns.Clear();
            }

            return columns;
        }

        /// <summary>
        /// Print a line of data
        /// </summary>
        /// <param name="addNewline">Add after line was printed a new line</param>
        /// <param name="columnData">Data to print.</param>
        public void Print(bool addNewline, params string[] columnData)
        {
            Print(columnData, false);
            if(addNewline)
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Print column titles to console
        /// </summary>
        /// <param name="bPrintNewLine">By default a new line is printed after headers were printed.</param>
        public void PrintHeader(bool bPrintNewLine=true)
        {
            string[] headers = Columns.Select(x => x.Enabled ? x.Title : null).ToArray();
            Print(headers, true);
            if( bPrintNewLine )
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnData"></param>
        /// <param name="isHeader"></param>
        void Print(string[] columnData, bool isHeader)
        {
            int lineNo = 0;
            while (true)
            {
                List<KeyValuePair<string, ColumnDefinition>> columns = GetCombinedFormattedLine(lineNo, isHeader, columnData.ToArray());
                

                if (columns.Count  == 0 )
                {
                    break;
                }

                if (lineNo > 0)
                {
                    Console.WriteLine();
                }

                foreach (var col in columns)
                {
                    Printer(col.Key, col.Value.Color);
                }
                lineNo++;
            }
        }

        string GetLine(ColumnDefinition column, string str, int lineNo, bool isHeader)
        {
            if (!column.Enabled)
            {
                return null;
            }

            if( String.IsNullOrEmpty(str) )
            {
                return "";
            }

            string line = (lineNo == 0 && column.DataWidth == 0) ? str : "";
            if (column.DataWidth > 0)
            {
                int startIdx = lineNo * column.DataWidth;
                int len = startIdx + column.DataWidth <= str.Length ? column.DataWidth : str.Length - startIdx;
                line = startIdx >= str.Length ? "" : str.Substring(startIdx, len);
            }
            return line;
        }
    }
}
