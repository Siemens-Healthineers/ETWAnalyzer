# Data Extraction

ETWAnalyzer extracts from one or a folder of .etl files Json extracts which are put by default in a subfolder named Extract below the source ETL files. 
It can also work with .7z/.zip files if the contained .etl file has the same name as the enclosing .7z/.zip file. 

The basic syntax is
```
ETWAnalyzer -extract all -fd c:\issue1\xxx.etl -symserver ms
```
Besides ```All``` the following extractors are available:

| Extractor   |  Description |
| -------     | --------     |
|  All        | Include all extractors|
|  CPU        | CPU consumption of all proceses part of the recording. CPU Sampling (PROFILE) and/or Context Switch tracing (CSWITCH) data with stacks must be present.|
|  Default    | Include all extractors except File|
|  Disk       | Disk IO summary and a per file summary of read/write/flush disk service times. DISK_IO data must be present in trace to get this data.|
|  DNS        | Extract DNS Queries. You need to enable ETW provider Microsoft-Windows-DNS-Client.|
|  Exception  | Get all .NET Exception Messages, Type and their call stacks when present with Process,ThreadId and TimeStamp|
|             | To get call stacks you need symbols. See below -symServer section. The Microsoft-Windows-DotNETRuntime ETW provider with ExceptionKeyword 0x8000 and stacks must be present.|
|  File       | Open/Close/Read/Write summary of all accessed files per process|
|             | The ETL file must contain FILEIO data.|
|  Frequency  | Extract CPU Frequency data when present from enabled Microsoft-Windows-Kernel-Processor-Power and Microsoft-Windows-Kernel-Power (capture state data from both providers is also needed) ETW providers.|
|  Memory     | Get workingset/committed memory machine wide and of all processes at trace start and a second time at trace end. MEMINFO_WS must be present.|
|  Module     | Dump all loaded modules with file path and version. LOADER data must be present in trace.|
|  ObjectRef  | Extract all Handle (Create/Duplicate/Close) with kernel provider OB_HANDLE, Object (AddRef/ReleaseRef) with kernel provider OB_OBJECT and File map/unmap events with provider VAMAP.
|  PMC        | Extract Performance Monitoring Counters and Last Branch Record CPU traces. You need to enable PMC/LBR ETW Tracing during the recording to get data.|
|  Power      | Extract Power profile data when present from Microsoft-Windows-Kernel-Power provider (capture state is needed to get power profile data).|
|  Stacktag   | Get from all processes the CPU call stack summary by the WPA stacktag names|
|             | To work properly you need symbols. See below -symServer section|
|             |  Json Nodes: SummaryStackTags-UsedStackTagFiles,Stats...|
|             |               This uses default.stacktags and GCAndJit.stacktags. For each process the GC and JIT overhead is printed extra while the default stacktags contain implicitly GC and JIT also.|
|             |  Json Nodes: SpecialStackTags-UsedStackTagFiles,Stats...|
|             |               There you can configure with the ETWAnalyzer\Configuration\Special.stacktags to trend e.g. specific methods over one or more testruns to find regression issues or when an issue did start occurring.|
|  TCP        | Extract TCP statistic per connection. You need to enable the provider Microsoft-Windows-TCPIP.  |
|  ThreadPool | Extract relevant data from .NET Runtime ThreadPool if available. ThreadingKeyword 0x10000 needs to be set for the Microsoft-Windows-DotNETRuntime ETW Provider during recording.|
|             | Json Nodes: ThreadPool-PerProcessThreadPoolStarvations|


If you extract from 7z archives the files are uncompressed in-place and deleted after extraction. If you want to leave the decompressed files on disk after extraction 
you can add the ```-keep``` command line option. 

## Using Multiple Extractors
You can use multiple extractors by listing their names, separated by spaces, in the command line. If you don't specify a symbol server, ETWAnalyzer will use the already downloaded and cached symbols. This can speed up 
extraction if you're certain that no new symbols will be resolved.
```
ETWAnalyzer -extract CPU TCP DNS -fd c:\issue1\xxx.etl
```

## Using your own Symbol Server

To use custom symbol servers/directories you can specifcy one or more with the -symserver option. The symbol cache folder is by
default C:\Symbols. If you want to download the symbol to a different local folder use the ```-SymCache``` option.

```
ETWAnalyzer -extract all -fd c:\issue1\xxx.etl -symserver https://msdl.microsoft.com/download/symbols;https://chromium-browser-symsrv.commondatastorage.googleapis.com -symcache d:\OtherSymbolStore
```
There are some shortcuts configured which you can use with ```-SymServer```

| Shortcut | Value   |
| --------      | ------- |
| NtSymbolPath  | Value from environent variable _NT_SYMBOL_PATH is used.                    |
| MS            | ```SRV*C:\Symbols*https://msdl.microsoft.com/download/symbols```                |
| Google        | ```SRV*C:\Symbols*https://chromium-browser-symsrv.commondatastorage.googleapis.com``` |



## File Compression
Starting from version v3.0.0.8, ETWAnalyzer compresses the extracted JSON files into a single .json7z 7z archive. 
If you prefer to have the output in uncompressed .json files, use the `-NoCompress` option with the `-Extract` command. 
To make the JSON files more readable, you can add the `-Indent` option, which will format the JSON with proper indentation.


# Common Issues
## High Memory Consumption
ETWAnalyzer loads symbols which needs several GB of memory. E.g. the chrome pdb alone is over 2 GB in size. But the highest memory costs arise from parsing context switch events which 
can easily exceed 30 GB of private bytes for one instance if you have a large (ca. 5 GB) .etl file. To speed things up ETWAnalyzer uses Server GC which is a lot faster but also 
more memory hungry. If you run out of memory due to too many concurrent extraction jobs you can limit the number of concurrent extractions (by default up to 5 depending on core count) 
with ```-nthreads 1``` to one. If you do not need context switch data (thread waits ...) you can also omit it by adding ```-NoCSwitch```. 

## Incremental Update/Retrying Extraction
When you extract data again the json files are overwritten. If you want to extract just the missing files because you have added new input data, or your previous extract job
did skip some files due to OutOfMemoryExceptions you can use ```-NoOverwrite```.

## Corrupt VMWare ETW Data
Sometimes you get from VMWare machines data where the CPU sampling data contains invalid CPU performance counter data which prevent WPA from loading the data just as it is the 
case with ETWAnalyzer. If you see during extraction or from WPA the message 

```
An exception occurred while processing the events: 0xD000003E. 
```
This happens only for CPU sampling data. If you have collected Context Switch data you can still process the data by adding the flag ```-NoSampling``` which will then extract just the 
context switch data, when present. That way you can get at least some insights from such VMs. 

