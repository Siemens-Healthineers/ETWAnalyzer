//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Disk;
using ETWAnalyzer.Extract.Exceptions;
using ETWAnalyzer.Extract.FileIO;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.Extract.ThreadPool;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.Infrastructure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// ETWExtract with properties to save event data
    /// </summary>
    public class ETWExtract : IETWExtract
    {
        /// <summary>
        /// Input ETL file name
        /// </summary>
        public string SourceETLFileName { get; set; }

        /// <summary>
        /// Command line of ETWAnalyzer during extraction
        /// </summary>
        public string UsedExtractOptions { get; set; }

        /// <summary>
        /// OS Name
        /// </summary>
        public string OSName { get; set; }

        /// <summary>
        /// OS Build version
        /// </summary>
        public string OSBuild { get; set; }

        /// <summary>
        /// OS Version as Version object
        /// </summary>
        public Version OSVersion { get; set; }

        /// <summary>
        /// Installed Memory size in MB
        /// </summary>
        public int MemorySizeMB { get; set; }

        /// <summary>
        /// CPU Count including Hyper threaded enabled cores
        /// </summary>
        public int NumberOfProcessors { get; set; }

        /// <summary>
        /// CPU Clock Frequency
        /// </summary>
        public int CPUSpeedMHz { get; set; }


        /// <summary>
        /// CPU Vendor
        /// </summary>
        public string CPUVendor { get; set; }

        /// <summary>
        ///  CPU Name
        /// </summary>
        public string CPUName { get; set; }

        /// <summary>
        /// Nullable value which if present allows to check if CPU has HyperThreading Enabled or not
        /// </summary>
        public bool? CPUHyperThreadingEnabled { get; set; }

        /// <summary>
        /// ETW Session start time
        /// </summary>
        public DateTimeOffset SessionStart { get; set; }

        /// <summary>
        /// ETW Session stop time
        /// </summary>
        public DateTimeOffset SessionEnd { get; set; }

        /// <summary>
        /// Duration of ETW session
        /// </summary>
        [JsonIgnore] // For some reason Json.NET serializes read only properties also
        public TimeSpan SessionDuration { get => SessionEnd - SessionStart; }

        /// <summary>
        /// Extracted ETW Marker events
        /// </summary>
        public List<ETWMark> ETWMarks { get; set; } 

        /// <summary>
        /// Extracted ETW Marker events
        /// </summary>
        IReadOnlyList<ETWMark> IETWExtract.ETWMarks => ETWMarks;

        /// <summary>
        /// Computer Model Name
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Machine Active Directory Domain
        /// </summary>
        public string AdDomain { get; set; }

        /// <summary>
        /// True when machine is joined domain
        /// </summary>
        public bool IsDomainJoined { get; set; }

        /// <summary>
        /// Connected displays to computer
        /// </summary>
        public List<Display> Displays { get; set; } = new List<Display>();  // Full Type Info is needed for serializer!

        /// <summary>
        /// Connected displays to computer
        /// </summary>
        IReadOnlyList<Display> IETWExtract.Displays => Displays;

        /// <summary>
        /// Get Information about all loaded modules of the system
        /// </summary>
        IModuleContainer IETWExtract.Modules => myModuleDeserializer.Value; 

        /// <summary>
        /// Processes with their command line and start time
        /// </summary>
        public List<ETWProcess> Processes { get; set; } = new List<ETWProcess>();

        /// <summary>
        /// Processes with their command line and start time
        /// </summary>
        IReadOnlyList<ETWProcess> IETWExtract.Processes => Processes;

        /// <summary>
        /// CPU Metrics
        /// </summary>
        public CPUStats CPU { get; set; }

        /// <summary>
        /// CPU Metrics
        /// </summary>
        ICPUStats IETWExtract.CPU => CPU;

        /// <summary>
        /// Disk Metrics
        /// </summary>
        public DiskIOData Disk { get; set; }

        /// <summary>
        /// Disk Metrics
        /// </summary>
        IDiskIOData IETWExtract.Disk { get => Disk; }

        ExceptionStats myExceptionStats;

        /// <summary>
        /// Exception Metrics
        /// </summary>
        public ExceptionStats Exceptions
        {
            get
            {
                return myExceptionStats;
            }
            set
            {
                myExceptionStats = value;

                // During deserialization connect the exception extract with the parent class 
                // to enable process extraction and time conversion from ETL relative timestamps to local timestamps
                if (myExceptionStats != null)
                {
                    myExceptionStats.myExtract = this;
                }
            }
        }

        /// <summary>
        /// .NET Exceptions which were recorded
        /// </summary>
        IExceptionStats IETWExtract.Exceptions => Exceptions;

        ModuleContainer myModules;

        /// <summary>
        /// Get Information about all loaded modules of the system
        /// </summary>
        public ModuleContainer Modules 
        {
            get => myModules;
            set
            {
                // During Serialization we set the container instance so we can access shared strings and process data
                if ( value != null)
                {
                    value.Extract = this;
                    foreach(var m in value.Modules)
                    {
                        m.Container = value;
                    }
                }
                myModules = value;
            }
        }


        ThreadPoolStats myThreadPoolStats;

        /// <inheritdoc />
        public ThreadPoolStats ThreadPool
        {
            get
            {
                return myThreadPoolStats;
            }
            set
            {
                myThreadPoolStats = value;
            }
        }
        
        IThreadPoolStats IETWExtract.ThreadPool => ThreadPool;


        /// <summary>
        /// Build version of all module vectors. 
        /// </summary>
        public ModuleVersion[] ModuleVersions
        {
            get; set;
        }

        /// <summary>
        /// Build version of latest recognized module
        /// If it does not match the expected one then the DllToBuildMap.json file needs to be adapted 
        /// to add a dll which identifies the module vector.
        /// </summary>
        public ModuleVersion MainModuleVersion
        {
            get;
            set;
        }


        /// <summary>
        /// System wide memory metrics
        /// </summary>
        public MemoryStats MemoryUsage { get; set; }


        [NonSerialized]
        private Dictionary<string, ETWProcessIndex> myProcessByName;

        /// <summary>
        /// Contains from CPU Sampling data all processes stacktagged from several stack tag files
        /// Each Stacktagfile can define a set of disjoint stacktags e.g. GC/JIT and other or a specific method 
        /// which is applied ot each sample event
        /// </summary>
        public ProcessStackTags SummaryStackTags { get; set; }

        /// <summary>
        /// Contains from CPU Sampling data all processes stacktagged from several stack tag files
        /// Each Stacktagfile can define a set of disjoint stacktags e.g. GC/JIT and other or a specific method 
        /// which is applied ot each sample event
        /// </summary>
        IProcessStackTags IETWExtract.SummaryStackTags => SummaryStackTags;

        /// <summary>
        /// User defined stack tags which can be used to trend e.g. specific methods
        /// </summary>
        public ProcessStackTags SpecialStackTags { get; set; }

        /// <summary>
        /// User defined stack tags which can be used to trend e.g. specific methods
        /// </summary>
        IProcessStackTags IETWExtract.SpecialStackTags => SpecialStackTags;

        /// <summary>
        /// Contains FileIO data of all processes. This contains unlike Disk all File Read/Write/Open/Close operations regardless if the data was read from the physical disk or from the file system cache.
        /// </summary>
        public FileIOData FileIO { get; set; }

        /// <summary>
        /// Contains FileIO data of all processes. This contains unlike Disk all Read/Write operations regardless if the data was read from disk or from the file system cache.
        /// </summary>
        IFileIOData IETWExtract.FileIO { get => myFileIODeserializer.Value; }

        /// <summary>
        /// When File  IO data is accessed via IETWExtract 
        /// </summary>
        readonly Lazy<FileIOData> myFileIODeserializer;

        FileIOData ReadFileIOFromExternalFile()
        {
            FileIOData lret = FileIO;
            if( lret == null && DeserializedFileName != null)
            {
                ExtractSerializer ser = new();
                string file = ser.GetFileNameFor(DeserializedFileName, ExtractSerializer.FileIOPostFix);
                if (File.Exists(file))
                {
                    using var fileStream = new FileStream(file, FileMode.Open);
                    lret = ExtractSerializer.Deserialize<FileIOData>(fileStream);
                }
            }
            return lret;
        }


        /// <summary>
        /// When Module data is accessed via IEtwExtract interface deserialize optional data from external file.
        /// </summary>
        readonly Lazy<ModuleContainer> myModuleDeserializer;


        ModuleContainer ReadModuleInformationFromExternalFile()
        {
            ModuleContainer lret = Modules;
            if (lret == null && DeserializedFileName != null)
            {
                ExtractSerializer ser = new();
                string file = ser.GetFileNameFor(DeserializedFileName, ExtractSerializer.ModulesPostFix);
                if (File.Exists(file))
                {
                    using var fileStream = new FileStream(file, FileMode.Open);
                    lret = ExtractSerializer.Deserialize<ModuleContainer>(fileStream);
                    // Set parent nodes for shared strings and process list of ETWExtract after deserialization
                    lret.Extract = this;
                    foreach(var module in lret.Modules)
                    {
                        module.Container = lret;
                    }
                }
            }

            return lret;
        }


        /// <summary>
        /// When Extract is deserialized from a file we set here the input file name to be able to deserialize other parts later
        /// </summary>
        internal string DeserializedFileName { get; set; }

        /// <summary>
        /// Default ctor
        /// </summary>
        public ETWExtract()
        {
            myFileIODeserializer = new Lazy<FileIOData>(ReadFileIOFromExternalFile);
            myModuleDeserializer = new Lazy<ModuleContainer>(ReadModuleInformationFromExternalFile);
        }

        /// <summary>
        /// Convert a trace relative time which is seconds since trace start to an absolue time
        /// </summary>
        /// <param name="traceSinceStartIns"></param>
        /// <returns></returns>
        public DateTimeOffset ConvertTraceRelativeToAbsoluteTime(float traceSinceStartIns)
        {
            return SessionStart.AddTicks((long)(10_000_000.0d * traceSinceStartIns));
        }

        /// <summary>
        /// Get process by id and start time
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="startTime"></param>
        /// <returns>found process or an exception if it was not found.</returns>
        /// <exception cref="KeyNotFoundException">When process was not found</exception>
        /// <exception cref="InvalidOperationException">When pid was negative</exception>
        /// <exception cref="ArgumentNullException">Pid was 0</exception>
        public ETWProcess GetProcessByPID(int pid, DateTimeOffset startTime)
        {
            ETWProcess process = TryGetProcessByPID(pid, startTime);

            if (process == null)
            {
                throw new KeyNotFoundException($"Process with pid {pid} and startTime {startTime} could not be found in process collection.");
            }

            return process;
        }

        /// <summary>
        /// Non throwing version to get a process id
        /// </summary>
        /// <param name="pid">process id</param>
        /// <param name="startTime">Start Time</param>
        /// <returns>Found process instance or null</returns>
        /// <exception cref="ArgumentNullException">Pid is zero</exception>
        /// <exception cref="InvalidOperationException">Pid is negative.</exception>
        public ETWProcess TryGetProcessByPID(int pid, DateTimeOffset startTime)
        {
            if (pid == 0)
            {
                throw new ArgumentNullException(nameof(pid));
            }

            if (pid < 0)
            {
                throw new InvalidOperationException($"{nameof(pid)} cannot be negative!");
            }

            foreach (ETWProcess process in Processes)
            {
                if (process.ProcessID == pid && process.StartTime == startTime)
                {
                    return process;
                }
            }

            return null;
        }

        /// <summary>
        /// Find a process by its process id and process start time which is a unique tuple
        /// </summary>
        /// <param name="pid">Process Id</param>
        /// <param name="startTime">Process start time</param>
        /// <returns>ETWProcessIndex on success, otherwise a KeyNotFoundException is thrown.</returns>
        /// <exception cref="ArgumentNullException">When pid is 0 which is invalid</exception>
        public ETWProcessIndex GetProcessIndexByPID(int pid, DateTimeOffset startTime)
        {
            if( pid <= 0 )
            {
                throw new ArgumentException($"Pid {pid} was not valid!");
            }

            ETWProcessIndex lret = ETWProcessIndex.Invalid;

            for(int i=0;i<Processes.Count;i++)
            {
                if( Processes[i].ProcessID == pid && Processes[i].StartTime == startTime)
                {
                    lret = (ETWProcessIndex)i;
                }
            }

            if( lret == ETWProcessIndex.Invalid)
            {
                throw new KeyNotFoundException($"Could not locate process with pid {pid} and start Time {startTime}");
            }

            return lret;
        }

        /// <summary>
        /// Lookup a process name of the form "xxxx.exe (dddd)" and return an enum which is actually 
        /// an index to the <see cref="ETWExtract.Processes"/> list to reference the process not by a pointer
        /// but by a number into an array.
        /// </summary>
        /// <param name="processNameandId"></param>
        /// <returns>Enum representing the array index</returns>
        public ETWProcessIndex GetProcessIndex(string processNameandId)
        {
            if (processNameandId is null)
            {
                throw new ArgumentNullException(nameof(processNameandId));
            }
            // When the data is read back from the Json file we get the 
            // list which we use then to construct a lookup table to convert a process Name and Id of wpaexporter 
            // to an index which is used by the extracts to reference a process.
            if (myProcessByName == null)
            {
                myProcessByName = new Dictionary<string, ETWProcessIndex>();
                for (int i = 0; i < Processes.Count; i++)
                {
                    ETWProcess current = Processes[i];
                    // CSV process name format is "process.exe (ddd)"
                    string tmpName = $"{current.ProcessName} ({current.ProcessID})";
                    myProcessByName[tmpName] = (ETWProcessIndex)i;
                }
            }

            // Good error handling to track problems is essential. The exception should tell you what went wrong
            // without the need to debug obvious things.

            if (!myProcessByName.ContainsKey(processNameandId))
            {
                // Process has exited or only the pid but not the name is present anymore. Only true missing processes are errors which indicate a data inconsistency
                if (IsValidProcessNameAndId(processNameandId))
                {
                    throw new KeyNotFoundException($"The process {processNameandId} was not found in ETWExtract. Process count: {myProcessByName.Count}");
                }
            }

            return myProcessByName[processNameandId];
        }


        /// <summary>
        /// Already exited process which contain only partial information are of no interest. These will not be added to extracted data.
        /// </summary>
        /// <param name="processNameandId">Process Name and Id which is originated from ETW</param>
        /// <returns>True if process name is valid or False otherwise.</returns>
        public static bool IsValidProcessNameAndId(string processNameandId)
        {
            return processNameandId.Contains("Unknown")  || processNameandId.StartsWith(" (") ? false : true;
        }

        /// <summary>
        /// Convert a trace time to a local time.
        /// </summary>
        /// <param name="traceTime"></param>
        /// <returns></returns>
        public DateTimeOffset GetLocalTime(double traceTime)
        {
            return SessionStart.AddSeconds(traceTime);
        }


        /// <summary>
        /// Get an ETWProcess instance by index from array
        /// </summary>
        /// <param name="processIndex"></param>
        /// <returns></returns>
        public ETWProcess GetProcess(ETWProcessIndex processIndex)
        {
            ETWProcess process;
            if (processIndex == ETWProcessIndex.Invalid)
            {
                process = new ETWProcess() { CmdLine = nameof(ETWProcessIndex.Invalid), ProcessName = nameof(ETWProcessIndex.Invalid) };
            }
            else
            {
                process = Processes[(int)processIndex];
            }
            return process;
        }
    }
}
