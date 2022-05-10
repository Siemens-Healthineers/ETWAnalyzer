//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Contains for a given process all method costs such as CPU/Wait/First/Last call time ...
    /// </summary>
    public class MethodsByProcess : IMethodsByProcess
    {
        /// <summary>
        /// set needed by Json.Net
        /// </summary>
        public ProcessKey Process
        {
            get;
            set;
        }

        /// <summary>
        /// Get costs for each methods. Costs mean here CPU consumption, wait time, first/last call time based on CPU sampling data (this is NOT method call count!) and other stats
        /// </summary>
        [JsonIgnore]
        public List<MethodCost> Costs
        {
            get;                     // After deserializing the data we get back objects
            private set;
        } = new List<MethodCost>();


        /// <summary>
        /// Get costs for each methods. Costs mean here CPU consumption, wait time, first/last call time based on CPU sampling data (this is NOT method call count!) and other stats
        /// </summary>
        IReadOnlyList<MethodCost> IMethodsByProcess.Costs => Costs;


        /// <summary>
        /// Used by Json.NET To serialize/deserialize list in stringified form so we can easily extend MethodCost without adding much overhead in the serialized output such as property names ...
        /// </summary>
        public List<string> CostsAsString
        {
            get => Costs.Select(x => x.ToStringForSerialize()).ToList();
            set => Costs = value.Select(x => MethodCost.FromString(x)).ToList();
        }


        /// <summary>
        /// ctor used during inseration of data
        /// </summary>
        /// <param name="process"></param>
        public MethodsByProcess(ProcessKey process)
        {
            Process = process;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Process} with {Costs.Count} methods";
        }
    }
}
