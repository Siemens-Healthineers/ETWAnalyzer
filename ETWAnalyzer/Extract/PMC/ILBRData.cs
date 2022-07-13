using System.Collections.Generic;

namespace ETWAnalyzer.Extract.PMC
{
    /// <summary>
    /// Last Branch Record Data which contains method call estimates
    /// </summary>
    public interface ILBRData
    {
        /// <summary>
        /// Get method call data as flat array for each process and caller/caller count object
        /// </summary>
        /// <param name="extract">IETWExtract instance to translate process data</param>
        /// <returns>Flat list of all call counts of all processes</returns>
        IReadOnlyList<IMethodCall> GetMethodCalls(IETWExtract extract);
    }
}