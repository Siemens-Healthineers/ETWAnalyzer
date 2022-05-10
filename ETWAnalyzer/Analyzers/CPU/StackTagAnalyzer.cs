//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.CPU
{
    class StackTagAnalyzer : AnalyzerBase
    {
        public override void AnalyzeTestRun(TestAnalysisResultCollection issues, TestRun run)
        {
            throw new NotImplementedException();
        }

        public override void AnalyzeTestsByTime(TestAnalysisResultCollection issues, TestDataFile backend, TestDataFile frontend)
        {
            throw new NotImplementedException();
        }

        public override void Print()
        {
            throw new NotImplementedException();
        }

        public override void TakePersistentFlagsFrom(AnalyzeCommand analyzeCommand)
        {
            throw new NotImplementedException();
        }
    }
}
