//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer
{
    internal class AllocationDumper
    {
        private readonly string etlFile;

        public AllocationDumper(string etlFile)
        {
            this.etlFile = etlFile;
        }

        internal void Dump()
        {
        }
    }
}