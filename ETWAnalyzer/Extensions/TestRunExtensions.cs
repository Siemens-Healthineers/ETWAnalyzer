using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extensions
{
    static class TestRunExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="run"></param>
        /// <param name="dateA"></param>
        /// <param name="dateB"></param>
        /// <returns></returns>
        public static bool IsRunningBetween(this TestRun run, DateTime dateA, DateTime dateB)
        {
            DateTime first = dateA <= dateB ? dateA : dateB;
            DateTime second = dateA <= dateB ? dateB : dateA;
            return run.IsRunningAfter(first) && run.IsRunningPrevious(second);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="run"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static bool IsRunningAfter(this TestRun run, DateTime date) 
            => run.TestRunStart >= date;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="run"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static bool IsRunningPrevious(this TestRun run, DateTime date)
            => run.TestRunEnd <= date;

        public static ModuleVersion GetMainModuleVersion(this TestRun run)
            => HasOnlyEqualModulVersions(run) ? run.AllTestFilesSortedAscendingByTime[0].Extract.MainModuleVersion : null;
        private static bool HasOnlyEqualModulVersions(this TestRun run)
        {
            IReadOnlyCollection<TestDataFile> allTests = run.AllTestFilesSortedAscendingByTime;
            ModuleVersion refModuleVersion = allTests.FirstOrDefault()?.Extract.MainModuleVersion;
            bool onlyEqualModulVersions = !(allTests.Any(t => !t.Extract.MainModuleVersion.Equals(refModuleVersion)));

            if (!onlyEqualModulVersions) throw new Exception($"Invalied TestRun reconstruction - All moduleversions in testrun beginning at {run.TestRunStart} must be equal");
            return allTests.Count > 0 ? onlyEqualModulVersions : false;
        }
    }
}
