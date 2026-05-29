using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.EventDump
{
    /// <summary>
    /// Typed base class which caches its output so we have a chance to later add specific unit tests for important aspects of the dump commands
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract class DumpBase<T> : DumpBase
    {
        public string ETLFile { get; set; }
        public bool UsePrettyProcessName { get; set; }

        protected const string Col_CSVOptions = "CSVOptions";
        protected const string Col_FileVersion = "FileVersion";
        protected const string Col_VersionString = "VersionString";
        protected const string Col_ProductVersion = "ProductVersion";
        protected const string Col_ProductName = "ProductName";
        protected const string Col_Description = "Description";
        protected const string Col_Directory = "Directory";
        protected const string Col_TestCase = "TestCase";
        protected const string Col_Process = "Process";
        protected const string Col_Session = "Session";
        protected const string Col_ProcessName = "ProcessName";
        protected const string Col_CommandLine = "CommandLine";
        protected const string Col_Baseline = "Baseline";
        protected const string Col_FileName = "FileName";
        protected const string Col_SourceJsonFile = "SourceJsonFile";
        protected const string Col_Date = "Date";
        protected const string Col_TestTimeinms = "Test Time in ms";
        protected const string Col_StartTime = "Start Time";
        protected const string Col_Time = "Time";
        protected const string Col_Machine = "Machine";
        protected const string Col_AveragePriority = "Average Priority";


        public override void Execute()
        {
            ExecuteInternal();
        }

        /// <summary>
        /// Execute command and return cached output for unit testing
        /// </summary>
        /// <returns></returns>
        public abstract List<T> ExecuteInternal();
    }
}
