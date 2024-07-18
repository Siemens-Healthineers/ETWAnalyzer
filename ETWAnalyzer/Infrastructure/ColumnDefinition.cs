//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Infrastructure
{

    /// <summary>
    /// Defines an output column which can contain multiline data.
    /// </summary>
    internal class ColumnDefinition
    {
        /// <summary>
        /// Column title which is printed to console
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Symbolic name which is referenced by -Column property to enabled/disable specific columns
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// String which is printed at each row
        /// </summary>
        public string Prefix { get; set; } = "";

        /// <summary>
        /// Color in which the text is printed
        /// </summary>
        public ConsoleColor? Color { get; set; }

        /// <summary>
        /// When false column will not be printed.
        /// </summary>
        public bool Enabled { get; set; } = true;


        int myDataWidth;
        /// <summary>
        /// Column width excluding extra space to separate columns in output.
        /// When zero then the column data is simply appended which in effect turns wrapping off.
        /// Minimum non zero data width is 3
        /// </summary>
        public int DataWidth 
        { 
            get => myDataWidth;
            set => myDataWidth = value > 0 ? Math.Max(3, value) : Math.Max(0,value); 
        }

    }
}
