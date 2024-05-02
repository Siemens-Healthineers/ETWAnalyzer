//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract.Disk;
using ETWAnalyzer.Extract.Exceptions;
using ETWAnalyzer.Extract.FileIO;
using ETWAnalyzer.Extract.Handle;
using ETWAnalyzer.Extract.Modules;
using ETWAnalyzer.Extract.Network;
using ETWAnalyzer.Extract.PMC;
using ETWAnalyzer.Extract.Power;
using ETWAnalyzer.Extract.ThreadPool;
using ETWAnalyzer.Infrastructure;
using System;
using System.Collections.Generic;

namespace ETWAnalyzer.Extract
{
    /// <summary>
    /// Read only part of ETWExtract which is returned as deserialized Json
    /// </summary>
    public interface IETWExtract : IProcessExtract
    {

        /// <summary>
        /// Machine Active Directory Domain
        /// </summary>
        string AdDomain { get; }

        /// <summary>
        /// Get total CPU per process and a much more detailed summary which includes for all processes all methods by
        /// default with > 10ms CPU (use during extraction -allCPU to turn this off).
        /// </summary>
        ICPUStats CPU { get; }

        /// <summary>
        /// CPU Clock Frequency
        /// </summary>
        int CPUSpeedMHz { get; }

        /// <summary>
        /// Hard Disk IO data per file and totals for all drives
        /// </summary>
        IDiskIOData Disk { get; }

        /// <summary>
        /// Contains FileIO data of all processes. This contains unlike Disk all Read/Write operations regardless if the data was read from disk or from the file system cache.
        /// </summary>
        IFileIOData FileIO { get; }

        /// <summary>
        /// PMC (Performance Monitoring Counter) CPU Data
        /// </summary>
        IPMCData PMC { get; }

        /// <summary>
        /// Computer Displays. Displays outputs of the graphics card which are not attached have 0 resolution and refresh rate.
        /// </summary>
        IReadOnlyList<Display> Displays { get; }

        /// <summary>
        /// Get Information (File Name, File Path, ProductName, ProductVersion, Description, FileVersion)  about all loaded modules of the system and the processes which have them loaded.
        /// </summary>
        IModuleContainer Modules { get; }

        /// <summary>
        /// Extracted ETW Marker events
        /// </summary>
        IReadOnlyList<ETWMark> ETWMarks { get; }

        /// <summary>
        /// Contains a all thrown and rethrown .NET exceptions with stack traces.
        /// Usually this list is filtered during extraction with rules defined in Configuration\ExceptionFilters.xml
        /// To get all exception during extraction use -allExceptions during extraction.
        /// </summary>
        IExceptionStats Exceptions { get; }

        /// <summary>
        /// information on the CLR ThreadPool
        /// </summary>
        IThreadPoolStats ThreadPool { get; }

        /// <summary>
        /// Get Network data
        /// </summary>
        INetwork Network { get; }

        /// <summary>
        /// True when machine is joined domain
        /// </summary>
        bool IsDomainJoined { get; }

        /// <summary>
        /// Build version of latest recognized module
        /// If it does not match the expected one then the DllToBuildMap.json file needs to be adapted 
        /// to add a dll which identifies the module vector.
        /// </summary>
        ModuleVersion MainModuleVersion { get; }

        /// <summary>
        /// Installed Memory size in MB
        /// </summary>
        int MemorySizeMB { get; }

        /// <summary>
        /// System wide memory metrics and per process memory at start and end of trace
        /// </summary>
        IMemoryStats MemoryUsage { get; }

        /// <summary>
        /// Computer Model Name
        /// </summary>
        string Model { get; }

        /// <summary>
        /// Computer Name
        /// </summary>
        string ComputerName { get; }

        /// <summary>
        /// Build version of all module vectors. 
        /// </summary>
        ModuleVersion[] ModuleVersions { get; }

        /// <summary>
        /// CPU Count including Hyper threaded enabled cores
        /// </summary>
        int NumberOfProcessors { get; }

        /// <summary>
        /// CPU Vendor
        /// </summary>
        public string CPUVendor { get; set; }

        /// <summary>
        ///  CPU Name
        /// </summary>
        public string CPUName { get; set; }

        /// <summary>
        /// Nullable value which if present allows to check if CPU has HyperThreading enabled or not
        /// </summary>
        public bool? CPUHyperThreadingEnabled { get; set; }

        /// <summary>
        /// OS Build version
        /// </summary>
        string OSBuild { get; }

        /// <summary>
        /// OS Name
        /// </summary>
        string OSName { get; }

        /// <summary>
        /// OS Build version as Version object
        /// </summary>
        Version OSVersion { get; }

        /// <summary>
        /// Processes with their command line and start time
        /// </summary>
        IReadOnlyList<ETWProcess> Processes { get; }

        /// <summary>
        /// Duration of ETW session
        /// </summary>
        TimeSpan SessionDuration { get; }

        /// <summary>
        /// ETW Session stop time
        /// </summary>
        DateTimeOffset SessionEnd { get; }

        /// <summary>
        /// ETW Session start time
        /// </summary>
        DateTimeOffset SessionStart { get; }

        /// <summary>
        /// System boot time
        /// </summary>
        public DateTimeOffset BootTime { get; }

        /// <summary>
        /// Input ETL file name
        /// </summary>
        string SourceETLFileName { get; }

        /// <summary>
        /// User defined stack tags which can be used to trend e.g. specific methods.
        /// Since Stacktags are not cut off you can here e.g. also trend methods which have &lt; 10ms CPU sample time which would
        /// otherwise not appear in CPU metrics because by default only methods > 10ms CPU are added to keep the output file small.
        /// </summary>
        IProcessStackTags SpecialStackTags { get; }


        /// <summary>
        /// Contains from CPU Sampling and Context Switch data all processes stacktagged from several stack tag files
        /// Each Stacktagfile can define a set of disjoint stacktags e.g. GC/JIT and other or a specific method 
        /// which is applied to each sample event.
        /// </summary>
        IProcessStackTags SummaryStackTags { get; }

        /// <summary>
        /// Commandline of ETWAnalyzer during extraction
        /// </summary>
        string UsedExtractOptions { get; }

        /// <summary>
        /// Get power profile settings.
        /// </summary>
        IReadOnlyList<IPowerConfiguration> PowerConfiguration { get; }

        /// <summary>
        /// Contains ObjectReference and Handle Trace Data
        /// </summary>
        public HandleObjectData HandleData { get; }

        /// <summary>
        /// Convert a trace relative time which is seconds since trace start to an absolue time
        /// </summary>
        /// <param name="traceSinceStartIns"></param>
        /// <returns></returns>
        DateTimeOffset ConvertTraceRelativeToAbsoluteTime(float traceSinceStartIns);
    }
}