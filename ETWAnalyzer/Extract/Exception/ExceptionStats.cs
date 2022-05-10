//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extractors;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Exceptions
{
    /// <summary>
    /// Index based structure which is part of ETWExtract class.
    /// The data is stored by indicies to save space in the serialized JSON file.
    /// </summary>
    public class ExceptionStats : IExceptionStats
    {
        /// <summary>
        /// 
        /// </summary>
        public ExceptionStats()
        { }

        /// <summary>
        /// This Constructor is only for testing usage
        /// </summary>
        /// <param name="exceptionEvents">synthetic generated exceptions</param>
        public ExceptionStats(List<ExceptionEventForQuery> exceptionEvents)
        {
            if (exceptionEvents is null)
            {
                throw new ArgumentNullException(nameof(exceptionEvents));
            }

            myExceptions = exceptionEvents.ToArray();
        }

        /// <summary>
        /// Stacks which contain later down in the object hierarchy type message ... to support a space efficient Json serialiation without too much redundant data.
        /// </summary>
        public ExceptionStackContainer Stacks
        {
            get; set;
        }

        /// <summary>
        /// Exception count
        /// </summary>
        public int Count
        {
            get
            {
                int? length = Exceptions?.Length;
                return length ?? 0;
            }
        }

        internal IProcessExtract myExtract;

        ExceptionEventForQuery[] myExceptions;

        /// <summary>
        /// Return all stored exceptions as plain list with direct object references. 
        /// This is a copy of the actual stored data which is different to support efficient serializaton
        /// </summary>
        /// <remarks>If the used serializer is ever changed we need to exclude this property from serializaton!</remarks>
        [JsonIgnore]
        public ExceptionEventForQuery[] Exceptions
        {
            get
            {
                if (myExceptions == null)
                {
                    List<ExceptionEventForQuery> exceptions = new List<ExceptionEventForQuery>();

                    if (Stacks != null)
                    {
                        foreach (KeyValuePair<string, HashSet<ExceptionMessageAndType>> stackAndMessages in Stacks.Stack2Messages)
                        {
                            foreach (ExceptionMessageAndType container in stackAndMessages.Value)
                            {
                                for (int i = 0; i < container.Processes.Count; i++)
                                {
                                    ExceptionEventForQuery ev = new ExceptionEventForQuery(container.Message,
                                        container.Type,
                                        myExtract.GetProcess(container.Processes[i]),
                                        container.Times[i],
                                        stackAndMessages.Key);

                                    exceptions.Add(ev);
                                }
                            }
                        }
                    }
                    myExceptions = exceptions.ToArray();
                }

                return myExceptions;
            }
        }


        internal void Add(IProcessExtract myProcessExtract, ExceptionRowData exData)
        {
            myExceptions = null; // force update of Exceptions array when new data is added.

            if (Stacks == null)
            {
                Stacks = new ExceptionStackContainer();
            }
            Stacks.Add(myProcessExtract, exData);
        }
    }
}
