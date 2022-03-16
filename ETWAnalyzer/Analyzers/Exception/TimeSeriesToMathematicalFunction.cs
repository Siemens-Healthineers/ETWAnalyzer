//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer
{
    class TimeSeriesToMathematicalFunctionAdapter
    {
        public static List<Point> GenerateTimeAndValueDiscretFunction(ExceptionSourceFileWithNextNeighboursModuleVersion[] testsWithException, List<TestRun> runsOfTimeSeries)
        {
            List<Point> function = new();
            int sourceIdx = 0, x = 0, y = 0;

            foreach (var run in runsOfTimeSeries)
            {
                y = 0;
                if (sourceIdx < testsWithException.Length && runsOfTimeSeries[x] == testsWithException[sourceIdx].SourceOfActiveException.ParentTest.Parent)
                {
                    y = 1;
                    sourceIdx++;
                }
                function.Add(new Point(x++, y));
            }
            return function;
        }

        public static List<Point> GenerateTimeAndValueDiscretFunctionFromDifferentiatedExceptionData(ExceptionSourceFileWithNextNeighboursModuleVersion[] testsWithException, List<TestRun> runsOfTimeSeries)
        {
            List<Point> function = new();

            int sourceIdx = 0, x = 0;
            int y = testsWithException.First().IsExceptionCluster(ExceptionCluster.EndingException) ? 1 : 0;

            foreach (var run in runsOfTimeSeries)
            {
                if (sourceIdx < testsWithException.Length && runsOfTimeSeries[x] == testsWithException[sourceIdx].SourceOfActiveException.ParentTest.Parent)
                {
                    function.Add(new Point(x++,1));
                    y = testsWithException[sourceIdx++].IsExceptionCluster(ExceptionCluster.StartingException) ? 1 : 0;
                }
                else
                {
                    function.Add(new Point(x++, y));
                }
            }

            return function;
        }

    }
}
