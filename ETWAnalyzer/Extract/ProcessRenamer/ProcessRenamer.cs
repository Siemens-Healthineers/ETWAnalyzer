using ETWAnalyzer.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace ETWAnalyzer.Extract
{

    /// <summary>
    /// Rename an executable based on executable name and command line.
    /// This is useful if you have generic container processes (like w3wp.exe) where many instances of them are running which 
    /// run different code.
    /// </summary>
    public class ProcessRenamer : IProcessNameResolver
    {
        List<RenameRule> _ProcessRenamers = new List<RenameRule>();
        readonly Dictionary<Pair, string> _RenameCache = new Dictionary<Pair, string>();
        static readonly XmlSerializer mySerializer = new XmlSerializer(typeof(ProcessRenamer));

        /// <summary>
        /// 
        /// </summary>
        public List<RenameRule> ProcessRenamers
        {
            get { return _ProcessRenamers; }
            set { _ProcessRenamers = value ?? new List<RenameRule>(); }
        }

        static readonly Lazy<ProcessRenamer> myDefault = new Lazy<ProcessRenamer>(LoadProcessRenamer);

        /// <summary>
        /// Process Renamer instance with default settings from supplied config file located beneath the executables Configuration folder.
        /// </summary>
        public static ProcessRenamer Default
        {
            get => myDefault.Value;
        }

        /// <summary>
        /// Load the executable name and command line substring file to rename processes based on their arguments.
        /// </summary>
        static ProcessRenamer LoadProcessRenamer()
        {
            if (String.IsNullOrEmpty(ConfigFiles.ProcessRenameRules) || !File.Exists(ConfigFiles.ProcessRenameRules))
            {
                throw new FileNotFoundException($"Process renaming XML file not found. {ConfigFiles.ProcessRenameRules}");
            }
            else
            {
                using var reader = new StreamReader(ConfigFiles.ProcessRenameRules);
                return (ProcessRenamer)mySerializer.Deserialize(reader);
            }
        }

        struct Pair : IEquatable<Pair>
        {
            public string First { get; private set; }
            public string Second { get; private set; }

            public Pair(string first, string second)
            {
                First = first;
                Second = second;
            }


            public bool Equals(Pair other)
            {
                return String.Equals(First, other.First, StringComparison.Ordinal) &&
                       String.Equals(Second, other.Second, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                if( obj is Pair p)
                {
                    return Equals(p);
                }
                else
                    return false;
            }

            public override int GetHashCode()
            {
                int hashCode = -1350907974;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(First);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Second);
                return hashCode;
            }
        }

        /// <summary>
        /// Rename the process to give the logs a more descriptive name.
        /// </summary>
        /// <param name="exeName">Executable to check</param>
        /// <param name="cmdArgs">Command line arguments of this executable</param>
        /// <returns>exeName if no rename rule exist or descriptive name.</returns>
        public string GetProcessName(string exeName, string cmdArgs)
        {
            string renamed = exeName;
            var key = new Pair(exeName, cmdArgs);

            if (_RenameCache.TryGetValue(key, out string cached))
            {
                renamed = cached;
            }
            else
            {
                foreach (var renameOp in this._ProcessRenamers)
                {
                    renamed = renameOp.Rename(exeName, cmdArgs);
                    if (renamed != exeName)
                    {
                        break;
                    }
                }
                _RenameCache[key] = renamed;
            }
            return renamed;
        }


        /// <summary>
        /// 
        /// </summary>
        public class RenameRule
        {
            string _ExeName;

            /// <summary>
            /// 
            /// </summary>
            public string ExeName
            {
                get { return _ExeName ?? ""; }
                set { _ExeName = value; }
            }

            List<string> _CmdLineSubstrings;

            /// <summary>
            /// 
            /// </summary>
            public List<string> CmdLineSubstrings
            {
                get
                {
                    _CmdLineSubstrings ??= new List<string>();
                    return _CmdLineSubstrings;
                }
                set
                {
                    _CmdLineSubstrings = value ?? new List<string>();
                }
            }

            List<string> _NotCmdLineSubstrings;

            /// <summary>
            /// 
            /// </summary>
            public List<string> NotCmdLineSubstrings
            {
                get
                {
                    _NotCmdLineSubstrings ??= new List<string>();
                    return _NotCmdLineSubstrings;
                }
                set
                {
                    _NotCmdLineSubstrings = value ?? new List<string>();
                }
            }

            string _NewExeName;

            /// <summary>
            /// 
            /// </summary>
            public string NewExeName
            {
                get { return _NewExeName ?? ""; }
                set { _NewExeName = value ?? ""; }
                }

            /// <summary>
            /// 
            /// </summary>
            public RenameRule()
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="exeName"></param>
            /// <param name="cmdLineSubStrings"></param>
            /// <param name="notCmdlineStrings"></param>
            /// <param name="newExeName"></param>
            public RenameRule(string exeName, List<string> cmdLineSubStrings, List<string> notCmdlineStrings, string newExeName)
            {
                ExeName = exeName ?? "";
                CmdLineSubstrings = cmdLineSubStrings;
                NotCmdLineSubstrings = notCmdlineStrings;
                NewExeName = newExeName ?? "";
            }

            /// <summary>
            /// Rename exe where 
            /// </summary>
            /// <param name="exeName"></param>
            /// <param name="cmdLine"></param>
            /// <returns></returns>
            public string Rename(string exeName, string cmdLine)
            {
                string lret = exeName;
                if (ExeName.Equals(exeName, StringComparison.OrdinalIgnoreCase) && cmdLine != null)
                {
                    bool hasMatches = CmdLineSubstrings.All(substr => cmdLine.IndexOf(substr, StringComparison.OrdinalIgnoreCase) != -1);

                    // empty filter counts a no filter
                    if (CmdLineSubstrings.Count == 0)
                    {
                        hasMatches = true;
                    }
                    bool hasNotMatches = NotCmdLineSubstrings.Where(x => !String.IsNullOrEmpty(x)).Any(substr => cmdLine.IndexOf(substr, StringComparison.OrdinalIgnoreCase) != -1);

                    if (hasMatches && !hasNotMatches)
                    {
                        lret = NewExeName;
                    }
                }
                return lret;
            }
        }
    }
}
