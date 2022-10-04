using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer_uTest.TestInfrastructure
{
    /// <summary>
    /// Enscapsualates data with some cached output which is meant to be written to console or log 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataOutput<T>
    {
        /// <summary>
        /// Wraped output
        /// </summary>
        public T Data { get; }

        /// <summary>
        /// Cached ouptut data
        /// </summary>
        public string Output { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="output"></param>
        public DataOutput(T data, string output)
        {
            Data = data;
            Output = output;
        }

    }
}
