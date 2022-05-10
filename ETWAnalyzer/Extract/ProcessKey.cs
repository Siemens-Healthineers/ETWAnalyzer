//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Small container which can help to reduce temporary allocations after reading data from serialized json file
    /// </summary>
    public class ProcessKey : IEquatable<ProcessKey>
    {
        /// <summary>
        /// Setters are needed to deserialize data with Json.NET!
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Setters are needed to deserialize data with Json.NET!
        /// </summary>
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// Setters are needed to deserialize data with Json.NET!
        /// </summary>
        public int Pid { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public ProcessKey()
        { }

        /// <summary>
        /// Create ProcessKey instance
        /// </summary>
        /// <param name="processName">Name of process (including exe)</param>
        /// <param name="pid">Process id</param>
        /// <param name="startTime">Process Start time</param>
        public ProcessKey(string processName, int pid, DateTimeOffset startTime)
        {
            Name = processName ?? throw new ArgumentNullException(nameof(processName));
            Pid = pid;
            StartTime = startTime;
        }

        /// <summary>
        /// Convert ProcessKey from a string object implicitly. This is used by Json.NET to convert dictionary keys from a string back to an object
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator ProcessKey(string value)
        {
            return new ProcessKey(value);
        }

        /// <summary>
        /// Create a ProcessKey from a string representation which format is defined by the .ToString method of ProcessKey
        /// </summary>
        /// <param name="serialized">serialized string</param>
        public ProcessKey(string serialized)
        {
            if( String.IsNullOrEmpty(serialized))
            {
                throw new ArgumentException("Input string was null or empty. That is not a valid ProcessKey", nameof(serialized));
            }

            int startPid = serialized.IndexOf('(');
            int stopPid = serialized.IndexOf(')');
            if( startPid == -1 || stopPid == -1 )
            {
                throw new ArgumentException($"Input string {serialized} is not in the right format");
            }

            Name = serialized.Substring(0, startPid);
            string strInt = serialized.Substring(startPid+1, stopPid - startPid - 1);
            Pid = int.Parse(strInt, CultureInfo.InvariantCulture);
            string time = null;
            if( stopPid < serialized.Length-1) // we have a time string
            {
                time = serialized.Substring(stopPid + 1);
                StartTime = DateTimeOffset.Parse(time, null, DateTimeStyles.RoundtripKind);
            }
            else
            {
                StartTime = DateTimeOffset.MinValue;
            }


        }

        /// <summary>
        /// This string is also used by JSON.NET to serialize a ProcessKey as key in a dictionary. The string used here
        /// must be round trip capable with the string ctor of ProcessKey!
        /// </summary>
        /// <returns>String used for debugger display and serialization as key</returns>
        public override string ToString()
        {
            string startTimeStr = "";
            if( StartTime != DateTimeOffset.MinValue )
            {
                // use round trippable format o  See https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-round-trip-date-and-time-values
                startTimeStr = StartTime.ToString("o", CultureInfo.InvariantCulture);
            }
            
            return $"{Name}({Pid.ToString(CultureInfo.InvariantCulture)}){startTimeStr}";
        }

        /// <summary>
        /// Compare all members for equality
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ProcessKey other)
        {
            if (Pid == other?.Pid && Name == other?.Name && StartTime == other?.StartTime)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Compare only Pid and Name because in Stacktags we do not have the process start time present
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if Pid and Name are equal, false otherwise</returns>
        public bool EqualNameAndPid(ProcessKey other)
        {
            if (Pid == other?.Pid && Name == other?.Name)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is ProcessKey && Equals((ProcessKey)obj);
        }

        /// <summary>
        /// /
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 17 * 31 + Name.GetHashCode();
            hash = hash * 31 + StartTime.GetHashCode();
            hash = hash * 31 + Pid;
            return hash;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
        public static bool operator ==(ProcessKey left, ProcessKey right)
        {
            if(Object.ReferenceEquals(left, right))
            {
                return true;
            }

            if( left is object)
            {
                return left.Equals(right);
            } 
            else 
            {
                return right.Equals(left);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(ProcessKey left, ProcessKey right)
        {
            return !(left == right);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public ProcessKey FromString(string serialized)
        {
            return new ProcessKey(serialized);
        }
    }
}
