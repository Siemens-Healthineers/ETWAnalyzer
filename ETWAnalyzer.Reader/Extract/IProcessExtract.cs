//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// This is a trick to create a compact serialized data format where the enum is an index to the <see cref="ETWExtract.Processes"/> list.
    /// <see cref="IProcessExtract.GetProcessIndex(string)"/> is here to allow a lookup and to store in other tables not objects but only an integer. 
    /// </summary>
    public enum ETWProcessIndex
    {
        /// <summary>
        /// Invalid entry
        /// </summary>
        Invalid = -1,
    }

    /// <summary>
    /// Decouple ETWExtract from utility functions. The extract classes should not need to 
    /// know the concrete type of their containing class to reduce coupling when things are changed.
    /// </summary>
    public interface IProcessExtract
    {
        /// <summary>
        /// The different extracts (Exception, CPU, Memory ...) contain processes in the CSV extract.
        /// We unify in the <see cref="ETWExtract"/> class all these references to a specific process 
        /// by a single integer which is used as array index of the <see cref="ETWExtract.Processes"/> 
        /// list. That solves several problem with renaming processes in different places in different extracts
        /// and potential inconsistencies by referencing from the extracts always the same <see cref="ETWProcess"/>
        /// instance. 
        /// </summary>
        ETWProcessIndex GetProcessIndex(string processNameandId);

        /// <summary>
        /// Retrieve process from process index
        /// </summary>
        /// <param name="processIndex">Value returned by <see cref="GetProcessIndex(string)"/></param>
        /// <returns>ETWProcess instance</returns>
        /// <exception cref="ArgumentException">Invalid value entered.</exception>
        /// <exception cref="IndexOutOfRangeException">Index did point outside the process array.</exception>
        ETWProcess GetProcess(ETWProcessIndex processIndex);

        /// <summary>
        /// Convert a trace time message to a trace local time
        /// </summary>
        /// <param name="traceTime"></param>
        /// <returns></returns>
        DateTimeOffset GetLocalTime(double traceTime);

        /// <summary>
        /// Find a process by its process id and process start time which is a unique tuple
        /// </summary>
        /// <param name="pid">Process Id</param>
        /// <param name="startTime">Process start time</param>
        /// <returns>Found process object or a KeyNotFoundException is thrown</returns>
        /// <exception cref="ArgumentNullException">When pid is 0 which is invalid</exception>
        /// <exception cref="KeyNotFoundException">Process could not be found with given <paramref name="pid"/> and <paramref name="startTime"/></exception>
        ETWProcess GetProcessByPID(uint pid, DateTimeOffset startTime);


        /// <summary>
        /// Find a process by its process id and process start time which is a unique tuple
        /// </summary>
        /// <param name="pid">Process Id</param>
        /// <param name="startTime">Process start time</param>
        /// <returns>ETWProcessIndex on success, otherwise a KeyNotFoundException is thrown.</returns>
        /// <exception cref="ArgumentNullException">When pid is 0 which is invalid</exception>
        /// <exception cref="KeyNotFoundException">Process could not be found with given <paramref name="pid"/> and <paramref name="startTime"/></exception>
        ETWProcessIndex GetProcessIndexByPID(uint pid, DateTimeOffset startTime);


        /// <summary>
        /// Get process index at a given time. This is useful to correlate ETW events which log a pid at a given time so we can find the corresponding process later.
        /// </summary>
        /// <param name="pid">Process Id</param>
        /// <param name="timeWhenProcessDidExist">Time when process was running.</param>
        /// <returns>EtwProcessIndex.Invalid or a valid index on success. It does not throw if nothing could be found.</returns>
        /// <exception cref="ArgumentException">When pid is 0 or smaller.</exception>
        public ETWProcessIndex GetProcessIndexByPidAtTime(uint pid, DateTimeOffset timeWhenProcessDidExist);

        /// <summary>
        /// Non throwing version
        /// </summary>
        /// <param name="pid">process id</param>
        /// <param name="startTime">Start Time</param>
        /// <returns>Found process instance or null</returns>
        /// <exception cref="ArgumentNullException">Pid is zero</exception>
        /// <exception cref="InvalidOperationException">Pid is negative.</exception>
        ETWProcess TryGetProcessByPID(uint pid, DateTimeOffset startTime);
    }
}
