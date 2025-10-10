//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Infrastructure
{
    class ColumnFormatter<T>
    {
        public Func<T, string> Formatter;
        string myHeader;
        public string Header
        {
            get => myHeader;
            set => myHeader = value;
        }

        public ConsoleColor? Color;

        public ColumnFormatter()
        {
            Header = "";
            Formatter = x => "";
            Color = null;
        }
    }
}
