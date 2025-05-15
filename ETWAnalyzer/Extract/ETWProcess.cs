//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.ProcessTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Process information
    /// </summary>
    public class ETWProcess : IEquatable<ETWProcess>
    {
        /// <summary>
        /// Process Id
        /// </summary>
        public int ProcessID { get; set; }

        /// <summary>
        /// Process name with exe suffix
        /// </summary>
        public string ProcessName { get; set; }

        /// <summary>
        /// Process name including exe with the pid in parenthesis.
        /// </summary>
        [JsonIgnore]
        public string ProcessWithID { get => GetProcessWithId(false); }

        /// <summary>
        /// Get Process name with exe or renamed process if usePrttyProcessName is supplied
        /// </summary>
        /// <param name="usePrettyProcessName"></param>
        /// <returns></returns>
        public string GetProcessWithId(bool usePrettyProcessName)
        {
            return $"{(usePrettyProcessName ? ProcessNamePretty : ProcessName)}({ProcessID})";
        }

        /// <summary>
        /// Command line contents
        /// </summary>
        public string CmdLine { get; set; }

        /// <summary>
        /// Process start time or default value if not set
        /// </summary>
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// Process end time or DateTimeOffset.MaxValue value process is still running
        /// </summary>
        public DateTimeOffset EndTime { get; set; }

        /// <summary>
        /// User name or SID.
        /// During extraction we try to resolve the user names, but it can fail if extraction is done on a different machine. We then store the SID as fallback.
        /// </summary>
        public string Identity { get; set; }

        /// <summary>
        /// Session Id of process
        /// </summary>
        public int SessionId { get; set; }


        string myProcessNamePretty;

        /// <summary>
        /// Pretty process name 
        /// </summary>
        [JsonIgnore]
        public string ProcessNamePretty
        {
            get
            {
                if (myProcessNamePretty == null)
                {
                    myProcessNamePretty = ProcessRenamer.Default.GetProcessName(ProcessName, CmdLine);
                }

                return myProcessNamePretty;
            }
        }

        /// <summary>
        /// Get Process name with .exe depending on supplied flag
        /// </summary>
        /// <param name="usePretty"></param>
        /// <returns></returns>
        public string GetProcessName(bool usePretty)
        {
            return usePretty ? ProcessNamePretty : ProcessName;
        }

        const string StartTag = " +";
        const string EndTag = " -";
        const string StartEndTag = " +-";
        const string NopTag = "";

        /// <summary>
        /// Get a string to indicate if the process has started or stopped during the trace duration.
        /// </summary>
        [JsonIgnore]
        public string StartStopTags
        {
            get =>
            IsNew switch
            {
                true => HasEnded switch
                {
                    true => StartEndTag,
                    false => StartTag,
                },
                false => HasEnded switch
                {
                    true => EndTag,
                    false => NopTag,
                }
            };
        }

        /// <summary>
        /// Return command line arguments without exe name
        /// </summary>
        [JsonIgnore]
        public string CommandLineNoExe
        {
            get
            {
                string cmd = CmdLine;
                int endIdx = 0;
                const string ExeStr = ".exe";

                if( cmd == null || (endIdx = cmd.IndexOf(ExeStr, StringComparison.OrdinalIgnoreCase)) == -1 )
                {
                    return cmd;
                }

                endIdx += ExeStr.Length;
                if(endIdx < cmd.Length && cmd[endIdx] == '"')
                {
                    endIdx++;
                }

                return cmd.Substring(endIdx);

            }
        }


        /// <summary>
        /// Process has ended during trace session
        /// </summary>
        public bool HasEnded { get; set; }

        /// <summary>
        /// Process has been created during tracing session
        /// </summary>
        public bool IsNew { get; set; }

        /// <summary>
        /// Convert return code if present to a string representation. If the return code is convertible to an NtStatus error code
        /// the enum stringified value is returned. This is useful to detect if a process e.g. crashed due to a Heap corruption, stack overflow , ... 
        /// because the OS returns the exception code as return code for a crashed process.
        /// </summary>
        /// <param name="returnCode">Return code of process</param>
        /// <param name="possibleCrash">set to true if return code is in range between NtStatus.UNSUCCESSFUL and NtStatus.Max_Value which usually represent large negative numbers for specific exception codes. </param>
        /// <returns></returns>
        public static string GetReturnString(int? returnCode, out bool possibleCrash)
        {
            return GetNullableErrorString( (NtStatus ?) returnCode, out possibleCrash);
        }

        /// <summary>
        /// Check process return code if it could originate from a crashed process.
        /// </summary>
        /// <param name="returnCode">Return code of process</param>
        /// <returns>True if return code is in range between NtStatus.UNSUCCESSFUL and NtStatus.Max_Value which usually represent large negative numbers for specific exception codes. False otherwise.</returns>
        public static bool IsPossibleCrash(int? returnCode)
        {
            GetNullableErrorString((NtStatus?)returnCode, out bool possibleCrash);
            return possibleCrash;
        }

        /// <summary>
        /// Convert the return code which can be an NtStatus error code which starts at 0xC0000001 &lt; Max_Value. For these codes we return the stringified 
        /// NtStatus enum value to make it easy to see that a process did crash. 0xFFFFFFFF s printed as integer -1 to prevent confusion about that strange error code with the high value.
        /// </summary>
        /// <param name="value">Nullable NtStatus value</param>
        /// <param name="possibleCrash">set to true if status code is in range between NtStatus.UNSUCCESSFUL and NtStatus.Max_Value which usually represent large negative numbers for specific exception codes. </param>
        /// <returns>When null an empty string is returned, otherwise the stringified NtStatus value or its integer value</returns>
        static string GetNullableErrorString(NtStatus ? value, out bool possibleCrash)
        {
            possibleCrash = false;
            string lret = "";
            if( value != null )
            {
                if((value.Value > NtStatus.SUCCESS && value.Value <= NtStatus.Max_Value))
                {
                    possibleCrash = true;
                    lret = value.Value.ToString();
                }
                else
                {
                    lret = CastToInt(value.Value).ToString(CultureInfo.InvariantCulture);
                }
            }

            return lret;
        }

        static int CastToInt(NtStatus value)
        {
            unchecked
            {
                return (int)value;
            }
        }

        /// <summary>
        /// When proces has exited it contains the return code
        /// Public setter needed by Json.NET
        /// </summary>
        public int? ReturnCode { get; set; }

        /// <summary>
        /// Every process has a prent process id
        /// Public setter needed by Json.NET
        /// </summary>
        public int ParentPid { get; set; }

        /// <summary>
        /// Needed when used in a dictionary or HashSet.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return 17 * 31 + ProcessID;
        }

        /// <summary>
        /// We need to explicitly define which properties are needed to be equal when this 
        /// class is used as key in a Dictionary or HashSet. 
        /// .NET will compare for class types only the pointer value which
        /// would return false if all members contain the same value.
        /// See https://www.c-sharpcorner.com/article/story-of-equality-in-net-part-three/
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ETWProcess other)
        {
            if (other == null )
            {
                return false;
            }

            // Intern process name and command line to make string comparisons much faster by comparing references
            if( (ProcessName != null) && String.IsInterned(ProcessName) == null)
            {
                CmdLine = String.Intern(CmdLine ?? "");
                ProcessName = String.Intern(ProcessName);
            }

            if (ProcessID == other.ProcessID &&
                ProcessName == other.ProcessName &&
                CmdLine == other.CmdLine &&
                StartTime == other.StartTime)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Convert implicitly an ETWProcess to a ProcessKey
        /// </summary>
        /// <param name="p"></param>
        public static implicit operator ProcessKey(ETWProcess p)
        {
            if( p == null || p.ProcessName == null)
            {
                return default;
            }

            return new ProcessKey(p.ProcessName, p.ProcessID, p.StartTime);
        }

        ProcessKey myKey;
        /// <summary>
        /// Convert an ETWProcess to a ProcessKey struct. Warning you should check ProcessName for null before trying method ir it will 
        /// throw an exception. This can happen for events which were collected early or late of the recording where the pid was known,
        /// but no image data event was captured.
        /// </summary>
        /// <returns>ProcessKey instance</returns>
        /// <exception cref="ArgumentNullException">When <see cref="ProcessName"/> was null of the current instance.</exception>
        public ProcessKey ToProcessKey()
        {
            if( myKey == null)
            {
                myKey = new ProcessKey(ProcessName ?? "ExitedProcess", ProcessID, StartTime);
            }

            return myKey;
        }

        internal enum ProcessStates
        {
            None,
            Started,
            OnlyStarted,
            Stopped,
            OnlyStopped,
        }

        internal bool IsMatch(ProcessStates? state)
        {
            return state switch
            {
                null => true,
                ProcessStates.None when (!IsNew && !HasEnded) => true,
                ProcessStates.Started when IsNew => true,
                ProcessStates.Stopped when HasEnded => true,
                ProcessStates.OnlyStopped when HasEnded && !IsNew => true,
                ProcessStates.OnlyStarted when IsNew && !HasEnded => true,
                _ => false,
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{ProcessName}({ProcessID}) New:{IsNew} Ended: {HasEnded}, Parent: {ParentPid} Ret: {ReturnCode} {CmdLine}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ETWProcess);
        }


        /// <summary>
        /// Use this comparer if you want to compare processes by its name and pid only but not include the start time.
        /// This can be needed when you combine several long running ETL files which have in one etl recorded the process start
        /// which would end up in duplicate entries in HashSets of ETWProcesses.
        /// </summary>
        public static IEqualityComparer<ETWProcess> CompareOnlyPidCmdLine
        {
            get;
        } = new PidCmdLineComparer();

        internal class PidCmdLineComparer : IEqualityComparer<ETWProcess>
        {
            public bool Equals(ETWProcess x, ETWProcess y)
            {
                if( Object.ReferenceEquals(x,y) )
                {
                    return true;
                }
                if( x is null)
                {
                    return false;
                }

                return
                    x.ProcessID == y.ProcessID &&
                    x.ProcessName == y.ProcessName &&
                    x.CmdLine == y.CmdLine;
            }

            public int GetHashCode(ETWProcess obj)
            {
                // To combine hash codes from different fields see
                // https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
                int hash = 17 * 31 + obj.ProcessID;
                hash = hash * 31 + (obj.CmdLine ?? "").GetHashCode();
                hash = hash * 31 + obj.ProcessName.GetHashCode();
                return hash;
            }
        }
    }
}
