using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.PMC
{
    /// <summary>
    /// Last Branch Record Data which contains method call estimates
    /// </summary>
    public class LBRData : ILBRData
    {
        /// <summary>
        /// Index to Methods Array of LBRData
        /// </summary>
        public enum MethodIdx
        {
            /// <summary>
            /// Index to Methods Array of LBRData
            /// </summary>
            Invalid = -1,
        }

        /// <summary>
        /// Get method call data as flat array for each process and caller/caller count object
        /// </summary>
        /// <param name="extract">IETWExtract instance to translate process data</param>
        /// <returns>Flat list of all call counts of all processes</returns>
        public IReadOnlyList<IMethodCall> GetMethodCalls(IETWExtract extract)
        {
            List<MethodCall> calls = new();
            for (int i = 0; i < MethodCallerIndex.Count; i++)
            {
                MethodCall call = new(Methods[MethodCallerIndex[i]], Methods[CalledMethodIndex[i]], MethodCountIndex[i], extract.GetProcess((ETWProcessIndex)ETWProcessIndex[i]));
                calls.Add(call);
            }

            return calls;
        }

        /// <summary>
        /// List of method names which is used by other data structures
        /// </summary>
        public List<string> Methods { get; set; } = new();

        /// <summary>
        /// List of Method Call costs as string to support easy de/serialization while keeping the serialized Json small
        /// </summary>
        public List<string> Costs
        {
            get
            {
                if( MethodCallerIndex?.Count == 0 ) // Json.NET calls get during deserialization. We need to return null, otherwise set will not be called!
                {
                    return null;
                }

                List<string> costAsString = new();
                for (int i = 0; i < MethodCallerIndex.Count; i++)
                {
                    costAsString.Add($"{MethodCallerIndex[i]} {CalledMethodIndex[i]} {ETWProcessIndex[i]} {MethodCountIndex[i]}");
                }
                return costAsString;
            }
            set
            {
                if (value != null)
                {
                    foreach (string cost in value)
                    {
                        string[] costs = cost.Split(CostSplitter);
                        MethodCallerIndex.Add(int.Parse(costs[0]));
                        CalledMethodIndex.Add(int.Parse(costs[1]));
                        ETWProcessIndex.Add(int.Parse(costs[2]));
                        MethodCountIndex.Add(long.Parse(costs[3]));
                    }
                }
            }
        }

        internal List<int> MethodCallerIndex { get; } = new();
        internal List<int> CalledMethodIndex { get; } = new();
        internal List<int> ETWProcessIndex { get; } = new();
        internal List<long> MethodCountIndex { get; } = new();

        static readonly char[] CostSplitter = new char[] { ' ' };


        MethodIdx GetOrAdd(string method)
        {
            if (!Methods.Contains(method))
            {
                Methods.Add(method);
                return (MethodIdx)(Methods.Count - 1);
            }
            else
            {
                return (MethodIdx)Methods.IndexOf(method);
            }
        }

        internal void SetCount(ETWProcessIndex processIndex, string caller, string method, int count)
        {
            MethodIdx fromIdx = GetOrAdd(caller);
            MethodIdx toIdx = GetOrAdd(method);

            MethodCallerIndex.Add((int)fromIdx);
            CalledMethodIndex.Add((int)toIdx);
            ETWProcessIndex.Add((int)processIndex);
            MethodCountIndex.Add(count);
        }
    }
}
