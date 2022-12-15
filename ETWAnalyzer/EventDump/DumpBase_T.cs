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

        protected string Col_CSVOptions = "CSVOptions";
        protected string Col_FileVersion = "FileVersion";
        protected string Col_VersionString = "VersionString";
        protected string Col_ProductVersion = "ProductVersion";
        protected string Col_ProductName = "ProductName";
        protected string Col_Description = "Description";
        protected string Col_Directory = "Directory";
        protected string Col_TestCase = "TestCase";
        protected string Col_Process = "Process";
        protected string Col_ProcessName = "ProcessName";
        protected string Col_CommandLine = "CommandLine";
        protected string Col_Baseline = "Baseline";
        protected string Col_FileName = "FileName";
        protected string Col_SourceJsonFile = "SourceJsonFile";
        protected string Col_Date = "Date";
        protected string Col_TestTimeinms = "Test Time in ms";
        protected string Col_StartTime = "Start Time";
        protected string Col_Time = "Time";
        protected string Col_Machine = "Machine";


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
