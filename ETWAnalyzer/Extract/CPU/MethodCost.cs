//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{

    /// <summary>
    /// Index to Methods list. To enable easier reading we strongly type the enum value to mark it as index to the Methods list in arguments to other methods
    /// </summary>
    public enum MethodIndex
    {
        /// <summary>
        /// Dummy index value. In reality the value is an index to a list
        /// </summary>
        Invalid = -1
    }


    /// <summary>
    /// Contains method CPU consumption, wait time, first/last occurrence number of threads on which it was executed in a given process
    /// </summary>
    public class MethodCost
    {
        /// <summary>
        /// Array index to MethodList array
        /// </summary>
        public MethodIndex MethodIdx
        {
            get;
            internal set;
        }

        /// <summary>
        /// Set later after data has been deserialized so we can get the actual method name from the object model
        /// </summary>
        internal List<string> MethodList
        {
            get;
            set;
        }

        /// <summary>
        /// Get actual method name as string
        /// </summary>
        public string Method
        {
            get => MethodList[(int)MethodIdx];
        }

        string myModule;

        /// <summary>
        /// Get Module Name
        /// </summary>
        [JsonIgnore]
        public string Module
        {
            get
            {
                if( myModule == null )
                {
                    myModule = "";
                    string method = Method ?? "";

                    int idx = method.IndexOf("!");
                    if( idx > 0 )
                    {
                        myModule = method.Substring(0, idx);
                    }
                }

                return myModule;
            }
        }

        /// <summary>
        /// CPU consumption of that method in ms from CPU Sample profiling data
        /// </summary>
        public uint CPUMs
        {
            get; private set;
        }

        /// <summary>
        /// Context switch data how long all threads did wait in this method
        /// </summary>
        public uint WaitMs
        {
            get; private set;
        }

        /// <summary>
        /// First occurrence of that method which is seconds since trace start
        /// </summary>
        public float FirstOccurenceInSecond
        {
            get; private set;
        }

        /// <summary>
        /// Last occurrence of that method which is seconds since tract start
        /// </summary>
        public float LastOccurenceInSecond
        {
            get; private set;
        }

        /// <summary>
        /// Number of different threads that method was executed during the trace recording
        /// </summary>
        public int Threads
        {
            get; private set;
        }

        /// <summary>
        /// Averaged value how many stack frames are below for given method. This allows to define a distance
        /// metric to show methods with the lowest Depths but highest CPU diff which are most probably responsible
        /// for a degradation
        /// </summary>
        public int DepthFromBottom
        {
            get; private set;
        }

        /// <summary>
        /// Aggregated Ready time in milliseconds across all threads. Overlapping ready times are counted only once.
        /// If e.g. two threads wait from 1 - 2 and 1.5 - 2.5 we return 1.5 as the total ready time. Overlapping waits are counted only once.
        /// </summary>
        public uint ReadyMs
        {
            get; private set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodIdx"></param>
        /// <param name="cpuMs"></param>
        /// <param name="waitMs"></param>
        /// <param name="firstOccurrence"></param>
        /// <param name="lastOccurrence"></param>
        /// <param name="threadCount"></param>
        /// <param name="depthFromBottom"></param>
        /// <param name="readyMs"></param>
        public MethodCost(MethodIndex methodIdx, uint cpuMs, uint waitMs, decimal firstOccurrence, decimal lastOccurrence, int threadCount, int depthFromBottom, uint readyMs)
        {
            MethodIdx = methodIdx;
            CPUMs = cpuMs;
            WaitMs = waitMs;
            FirstOccurenceInSecond = (float)firstOccurrence;
            LastOccurenceInSecond = (float)lastOccurrence;
            Threads = threadCount;
            DepthFromBottom = depthFromBottom;
            ReadyMs = readyMs;
        }

        /// <summary>
        /// Default ctor
        /// </summary>
        private MethodCost()
        {
        }

        /// <summary>
        /// Deserialize from a string Method index and CPU cost
        /// </summary>
        /// <param name="cost">Serialized string</param>
        /// <returns>Deserialized MethodCost instance</returns>
        internal static MethodCost FromString(string cost)
        {
            if (String.IsNullOrEmpty(cost))
            {
                throw new ArgumentException($"{nameof(cost)} was null or empty.");
            }

            int startIdx = 0;
            Parts part = Parts.MethodIdx;

            MethodCost deser = new MethodCost();

            for(int i=0;i<cost.Length;i++)
            {
                if( cost[i] == ' ')
                {
                    ParseCost(cost, startIdx, i-startIdx, part, deser);
                    part = (Parts)(((int)part) + 1);
                    startIdx = i + 1;
                }
                else if( i == cost.Length - 1)
                {
                    ParseCost(cost, startIdx, i+1 - startIdx, part, deser);
                }
            }

            return deser;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ParseCost(string cost, int startIdx, int len, Parts part, MethodCost deser)
        {
            switch (part)
            {
                case Parts.MethodIdx:
                    deser.MethodIdx = (MethodIndex)ParseInt(cost, startIdx, len);
                    break;
                case Parts.CPUMs:
                    deser.CPUMs = ParseUInt(cost, startIdx, len);
                    break;
                case Parts.WaitMs:
                    deser.WaitMs = ParseUInt(cost, startIdx, len);
                    break;
                case Parts.FirstOccurence:
                    deser.FirstOccurenceInSecond = Single.Parse(cost.Substring(startIdx, len), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                    break;
                case Parts.LastOccurence:
                    deser.LastOccurenceInSecond = Single.Parse(cost.Substring(startIdx, len), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                    break;
                case Parts.ThreadCount:
                    deser.Threads = ParseInt(cost, startIdx, len);
                    break;
                case Parts.DepthFromBottom:
                    deser.DepthFromBottom = ParseInt(cost, startIdx, len);
                    break;
                case Parts.ReadyMs:
                    deser.ReadyMs = ParseUInt(cost, startIdx, len);
                    break;
                default:
                    throw new InvalidOperationException("Invalid Part of MethodCost reached");
            }

            
        }

        enum Parts
        {
            MethodIdx,
            CPUMs,
            WaitMs,
            FirstOccurence,
            LastOccurence,
            ThreadCount,
            DepthFromBottom,
            ReadyMs,
        }

        /// <summary>
        /// Faster parser for integers containing no thousand separators
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int ParseInt(string s)
        {
            return ParseInt(s, 0, s.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int ParseInt(string s, int startIdx, int len)
        {
            int lret = 0;
            int end = startIdx + len;
            for (int i = startIdx; i < end; i++)
            {
                lret = lret * 10 + (s[i] - '0');
            }
            return lret;
        }

        /// <summary>
        /// Faster parser for unsigned integers containing no thousand separators
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static uint ParseUInt(string s)
        {
            return ParseUInt(s, 0, s.Length);
        }

        static uint ParseUInt(string s, int startIdx, int len)
        {
            long lret = 0;
            int end = startIdx + len;
            for (int i = startIdx; i < end; i++)
            {
                lret = lret * 10 + (s[i] - '0');
            }

            return (uint)lret;
        }

        /// <summary>
        /// Serialize MethodCost to a string which is the counterpart for <see cref="FromString(string)"/>
        /// </summary>
        /// <returns></returns>
        public string ToStringForSerialize()
        {
            // Use 1/10ms precision for method first/last timings
            string first = FirstOccurenceInSecond.ToString("F4", CultureInfo.InvariantCulture);
            string last = LastOccurenceInSecond.ToString("F4", CultureInfo.InvariantCulture);

            return $"{MethodIdx} {CPUMs.ToString(CultureInfo.InvariantCulture)} {WaitMs.ToString(CultureInfo.InvariantCulture)} {first} {last} {Threads} {DepthFromBottom} {ReadyMs}";
        }

        /// <summary>
        /// Used during serialization to provide a compact extensible serialization format
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Method} CPU: {CPUMs:N0}ms Wait: {WaitMs:N0}ms Ready: {ReadyMs}ms  First: {FirstOccurenceInSecond}s Last: {LastOccurenceInSecond}s Threads: {Threads} Depth: {DepthFromBottom}";
        }
    }
}
