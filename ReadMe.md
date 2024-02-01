# ETWAnalyzer

## License
ETWAnalyzer main license is [MIT][ETWAnalyzerLicense].

## Open Source and 3rd Party Software
ETWAnalyzer uses Open Source and 3rd Party Software listed in [ReadMe.oss][ETWAnalyzerOSS].

## What is it? 
ETWAnalyzer extracts from ETL files summary information into Json files. The ETL files are produced by the builtin profiling infrastructure of Windows [Event Tracing for Windows (ETW)](https://docs.microsoft.com/en-us/windows/win32/etw/about-event-tracing). 
Profiling data can be collected with e.g. [wpr](https://docs.microsoft.com/en-us/windows-hardware/test/wpt/windows-performance-recorder), wprUI, xperf, PerfView, ... . Wpr is part of Windows 10 which enables you to record data on any Windows machine without additional software installation in the field. 
These recorded ETL files are usually large (multi GB) and load slow into analysis tools such as 
[Windows Performance Analyzer (WPA)](https://docs.microsoft.com/en-us/windows-hardware/test/wpt/windows-performance-analyzer#:~:text=Included%20in%20the%20Windows%20Assessment,run%20in%20the%20Assessment%20Platform.) 
or [PerfView](https://github.com/microsoft/perfview). The design goal of ETWAnalyzer is to extract from huge ETL files the smallest data set that is necessary to identify performance bottlenenecks or regression issues in 
one or a collection of thousands of ETL files. 

After extraction ETWAnalyzer has many query commands to make the data much more accessible. It can query one or many files where the output is either printed to console, 
or written to a CSV file for further analysis. 

The Json files can also be accessed via a C# interface [**IETWExtract**](ETWAnalyzer/Documentation/ProgramaticAccess.md) which enables you to write custom analyzers.

Json files are much faster to query than the input ETL files without slow symbol server lookups.
It is based on years of field experience with ETW file analysis to keep the extracted Json file size as small as possible while maximizing the insights you can get of the extracted files.
An ETW Json file is typically a few MB while the input .etl file including PDBs is hundreds of MB. 

## Contributing
You want to contribute, miss specific data, or want to add your specific dump command? Check out [Contributing](ETWAnalyzer/Documentation/Contributing.md) to get started.

## Documentation
See [Documentation Folder in Repo](https://github.com/Siemens-Healthineers/ETWAnalyzer/tree/2.5.10.0/ETWAnalyzer/Documentation)

## Dump Commands
- [CPU](ETWAnalyzer/Documentation/DumpCPUCommand.md) 
- [CPU Extended](ETWAnalyzer/Documentation/DumpCPUExtended.md) 
- [Disk](ETWAnalyzer/Documentation/DumpDiskCommand.md) 
- [Dns](ETWAnalyzer/Documentation/DumpDNSCommand.md)
- [Exception](ETWAnalyzer/Documentation/DumpExceptionCommand.md)
- [File](ETWAnalyzer/Documentation/DumpFileCommand.md) 
- [LBR](ETWAnalyzer/Documentation/DumpLBRCommand.md)
- [Mark](ETWAnalyzer/Documentation/DumpMarkCommand.md)
- [Memory](ETWAnalyzer/Documentation/DumpMemoryCommand.md) 
- [PMC](ETWAnalyzer/Documentation/DumpPMCCommand.md)
- [Power](ETWAnalyzer/Documentation/DumpPower.md)
- [Process](ETWAnalyzer/Documentation/DumpProcessCommand.md)  
- [Stats](ETWAnalyzer/Documentation/StatsCommand.md)
- [TCP](ETWAnalyzer/Documentation/DumpTCPCommand.md)
- [TestRun](ETWAnalyzer/Documentation/DumpTestRunCommand.md)
- [ThreadPool](ETWAnalyzer/Documentation/DumpThreadPoolCommand.md) 
- [Version](ETWAnalyzer/Documentation/DumpVersionCommand.md)

They all support -filedir/-fd and an extensive command line help what you can dump from the extracted data. 


## Data Generation
The easiest way to get ETW data is to install [ETWController](https://github.com/Alois-xx/etwcontroller) which comes with predefined WPR profiles. 
ETWController supports
 - Zipping ETW and NGEN pdbs with 7z.
  - Taking Screenshots which are put besides profiling data which can be viewed in Browser.
 - Capturing Keyboard/Mouse events.
 - Distributed Profiling (Client/Server scenarios).
   
The 7z archives created by ETWController can be directly consumed with ETWAnalyzer. To e.g. unpack all compressed 7z files in a folder generated
by ETWController and keep the files uncompressed so you can analyze further with WPA:
```
ETWAnalyzer -extract all -fd c:\Cases\Failure\*.7z -keepTemp -symserver MS 
```
## Use Cases
 - [Profiler Driven Development](https://aloiskraus.wordpress.com/2022/07/25/pdd-profiler-driven-development/)
 - [Run tests with ETW Profiling](https://github.com/Alois-xx/SerializerTests)
 - [Build Profiling At Scale At Github](ETWAnalyzer/Documentation/BuildProfiling.md)

## Data Extraction
Data extraction is done for one or a directory of ETL files. Zipped ETL files are also extracted. By default 75% of all cores are used.
See [Extract](ETWAnalyzer/Documentation/ExtractCommand.md) for more information.

### Example

The following command extracts everything, using Microsoft symbols from a single ETL file. 

![](ETWAnalyzer/Documentation/Images/ExtractionCommand.png "Extract Command")
The option -AllCPU will include also methods with < 10 ms CPU or Wait time which are normally not relevant for performance regression issues to keep the file size as small as possible. 
You can also extract small Json files without symbol server access and resolve methods later from the Json files. See [Build Profiling At Scale At Github](ETWAnalyzer/Documentation/BuildProfiling.md) for more details.
There is extracted example data located at [Test Data](https://github.com/Siemens-Healthineers/ETWAnalyzer/blob/main/ETWAnalyzer_uTest/TestData/CallupAdhocWarmReadingCT_3117msFO9DE01T0162PC.20200717-124447.json) which you can query at your own. Can you find the performance bug? The curl command downloads the test data from Github. Then you can start working with the data.
```
curl https://raw.githubusercontent.com/Siemens-Healthineers/ETWAnalyzer/main/ETWAnalyzer_uTest/TestData/CallupAdhocWarmReadingCT_3117msFO9DE01T0162PC.20200717-124447.json > c:\Temp\ETWAnalzyerTest.json
set f=-filedir c:\Temp\ETWAnalzyerTest.json
ETWAnalyzer %f% -dump CPU -topN 5
ETWAnalyzer %f% -dump CPU -topN 1 -methods *
ETWAnalyzer %f% -dump CPU -topN 1 -methods * -sortby stackdepth -MinMaxCPUMs 1000
ETWAnalyzer %f% -dump CPU -topN 1 -methods * -sortby stackdepth -MinMaxCPUMs 1000 -includedll
ETWAnalyzer %f% -dump CPU -topN 1 -methods * -sortby stackdepth -MinMaxCPUMs 1000 -includedll -threadcount
ETWAnalyzer %f% -dump CPU -topN 1 -methods * -sortby stackdepth -MinMaxCPUMs 1000 -FirstLastDuration s s
ETWAnalyzer %f% -dump Stats -Properties SessionDurations
``` 

This shows a Microsoft Bug at work while some serialization performance test was executed.

## Querying the Data
After extraction from a > 600 MB input file a small ca. 6 MB file in the output folder. 

![alt text](ETWAnalyzer/Documentation/Images/ExtractedDataFiles.png "Extracted Data Files")

ETWAnalyzer will query all files in the current directory if you do not use -filedir/-fd xxx.  
The first query would be to check on which machine with how much memory, CPU and Windows version it was running. 
```
set f=-fd c:\Temp\C:\Temp\Extract\ZScaler_Download_Slow_100KB_Over100MBit_MouseLags
EtwAnalyzer %f% -dump Stats 
```
![alt text](ETWAnalyzer/Documentation/Images/DumpStatsCommand.png "Dump Stats")

If you have a directory of files you can limit the output to specific properties with e.g. *-Properties MemorySizeMB,OSName,NumberOfProcessors,CPUSpeedMHz,CPUVendor,CPUName* to
get a quick overview of the machine specs of a bunch of extracted ETL files. 

Now we want to get an overview what the CPU consumption was of the top 9 CPU consumers of that file
```
EtwAnalyzer %f% -dump CPU -topN 9 
```
![alt text](ETWAnalyzer/Documentation/Images/DumpCPUTop9.png "Dump CPU Top 9")

The mouse was hanging while I was downloading data. At the same time the CPU was fully utilized on my quad core notebook.
The by far highest CPU consumer was the Windows Kernel which sits in the System process. Lets pick that one and print the top 30 methods.

```
EtwAnalyzer %f% -dump CPU -processName System -TopNMethods 30 
```
![alt text](ETWAnalyzer/Documentation/Images/DumpCPUTop30Methods.png "Dump CPU Top 30 Methods")

To understand the data you need to know that ETWAnalyzer keeps for every method in a process the method inclusive times 
for CPU summed accross all threads. The Wait and Ready time is summed accross all threads but overlapping regions are only counted once.
This means that the maximum Wait/Ready time can get the recording time when at least one thread was always waiting or in the ready queue.
CPU timing is extracted from CPU sampling data. Wait/Ready times are determined from Context Switch data which is mainly generated when the
method was moved off a CPU due to a blocking OS call. That is the reason why Main or other entry point methods for a thread have the highest CPU
consumption but are not the performance bottleneck. The actual issue is in one of the methods which consume CPU which is not directly visible
in the extracted data.
To get an overview for a new issue one turns over to WPA to see time dependencies which can only visually be analyzed.

Once the issue is understood you can create [WPA stacktags](https://docs.microsoft.com/en-us/windows-hardware/test/wpt/stack-tags) of past issues to see if the same issue 
in the other file also appears without the need to drill deep into the call stacks. In this case it is obvious that we have again a Firewall problem where each network packet 
gets an expensive check in WfpAlepReauthorizeOutboundConnection which traverses long (many firewall rules?) lists. 

![alt text](ETWAnalyzer/Documentation/Images/WPA_HighCPUAnalysis.png "WPA Analysis")

Bad things keep coming back. After having identified a pattern in WPA we can check other ETW files either manually or we query the data with ETWAnalyzer and save a 
lot of time. Since WPA and ETWAnalyzer support stacktags we can dump the top 10 stacktags for the System process. 
After adding "-stacktags *" to the command line we get all stacktags. If you add "-stacktags * -methods *" all methods and stacktags are printed together in one list.

**Note: There is only one filter named -topNMethods which filters for the overall top CPU consumers for methods and stacktags.**
```
Etwanalyzer %f% -dump CPU -pn System -StackTags * -TopNMethods 10
```
![alt text](ETWAnalyzer/Documentation/Images/DumpCPUTop10Stacktags.png "Dump CPU Top 10 Stacktags")


From the stacktag CPU consumption we find as top match "Windows\Windows Firewall" which proves that we have hit the same issue again. 

**This is a known Windows 10 Bug which was fixed in the July 2023 KB5028166 Update which can happen in large AD Domains**
```
Windows Filtering Platform forces reauthorization of every packet of an existing session when there is a profile change 
(like change from public profile to domain profile). The authenticate / classification is an expensive operation and 
reauthorization of every packet has a high performance impact on the system.
```

From the WPA stacktrace we know that these calls are executed as [DPCs (Deferred Procedure Calls)](https://en.wikipedia.org/wiki/Deferred_Procedure_Call) which are normally issued for longer running tasks from 
an interrupt handler. The DPCs for all network packets consume up to all 4 cores on my machine which compete with the mouse interrupt handling
resulting in a slow, sluggish system. It is time to call Microsoft Support to ask where this is coming from. To check which dll version 
we are running one would turn over to WPA and open the Images graph to find which dlls in which version are loaded. 
There you can find the exact patch level of the OS or your application dlls which are loaded by a process.

ETWAnalyzer amends version information to CPU data if you add to **-dump CPU** *-ShowModuleInfo* or *-smi*. That will show besides the method names module version data. 
If you are not using a console with a high console buffer width the output becomes unreadable due to word wrapping. To better support non wraping consoles
you can add -Clip to all commands of ETWAnalyzer to prevent wraparound of output. Besides the version we would need the dll which is by default
not printed. But you can add *-IncludeDll* or *-id* to get besides method names also the dll name. To get rid of the Ready time we can add *-NoReady*.

```
Etwanalyzer %f% -dump CPU -pn System -methods * -MinMaxCPUMs 50s-51s -smi -id -clip -NoReady 
```
![alt text](ETWAnalyzer/Documentation/Images/ETWAnalyzer_ClippedOutput.png "Clipped Output") 

That small intro showed some of the key features of ETWAnalyzer. With this tool it is easy to detect patterns in thousands of ETL files which
was an impossible task with other publicly available tools. If you have performance trending tests it makes a lot of sense to run them with ETW profiling enabled 
so you can later find systematic deviations with a simple query. Issues which were before that tool 
too much work to track down are now a simple query. If your test is e.g. 3/30 times 20% slower you can query all tests for common patterns to 
see if e.g. a running Windows Installer did have an effect to your test execution time or if that did occur in other fast tests as well. 



<!-- References -->
[ETWAnalyzerLicense]:                                       <LICENSE>
[ETWAnalyzerOSS]:                                           <ETWAnalyzer/3rdParty/ReadMeOSS.md>
