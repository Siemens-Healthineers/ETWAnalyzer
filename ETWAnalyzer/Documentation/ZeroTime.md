# ZeroTime

![](Images/EventHorizon20190410-78m.png)

Image Source (https://eventhorizontelescope.org/press-release-april-10-2019-astronomers-capture-first-image-black-hole)

A common issue is to track performance relative a key event in the ETL file. Common scenarios are e.g. 
- UI visible after button click event was called
- UI visible after UI process has started
- New methods executed shortly before process has terminated (e.g. crashed)
- Exceptions that did happen after shutdown of application as requested
- Correlate distributed ETW traces
- ...

You can calculate relative timings manually but it is much easier to get that backed into your query. 

## Which events can be ZeroTime markers?
- First time a method was seen in one or any process
- Last time a method was seen in one or any process
- ETW Marker message
- Process start/end 

## How to move time?
The option *-ZeroTime/zt* allows you to do that. If the filter is not unique you can additionally add *-ZeroProcessName/zpn* to limit the zero time
definition to a specific process. 

| ZeroTime Option| Meaning |
| -----------  | ----------- 
| Marker       filter   |  Select a specific ETW Marker message e.g \*TestStart\* as zero time
| First        filter   |  Select first occurrence of method e.g. \*OnClick\* as zero time|
| Last         filter   |  Select last occurrence of method e.g. \*OnClick\* as zero time
| ProcessStart [CmdLine]|  Select the process start time as zero time. Needs -ZeroProcessName to filter for process name. The CmdLine filter is optional.
| ProcessEnd   [CmdLine]|  Select the process exit time as zero time. Needs -ZeroProcessName to filter for process name. The CmdLine filter is optional.

The following commands support -ZeroTime 
- -Dump CPU used with *-FirstLastDuration s s* to view shifted First/Last timings. 
- -Dump Process used with *-TimeFmt s* to view shifted process start/stop timings.
- -Dump Exception used with *-TimeFmt s* to view shifted exception times
- -Dump Mark

## Examples

### First Occurrence of Method as Zero
The following example selects the method *TestDeserialize* as zero timepoint. The headlines of First and Last have got a \* 
to indicate that the times are shifted.
To get meaningful output we sort by *First* and select all methods which were seen between the time interval [0;2]s and
have at least 50ms of CPU time to reduce the output. 

The -0.000 time is a rounding error because we just show ms and a time of e.g. -0.0004 
is rounded down to -0.000. Floating point numbers have a -0 and a +0. 
![](Images/DumpCPU_ZeroTime.png "Dump CPU ZeroTime")

The same data will also show up if it is exported to a CSV file with the -csv option.

### Process Start/Stop relative to marker method
Another example is to visualize shutdown behavior of a multi process application relative to the method *ShowShutdownWindow*

>ETWAnalyzer.exe -dump process -zerotime first *ShowShutdownWindow*  -timefmt s -csv Shutdown.csv

With an Excel Pivot Chart you get a nice visualization when processes did terminate relative to the shutdown trigger method along with their parent processes.

![](Images/ZeroShutdownMetric.png)

### Multi File correlation

You can also correlate multiple ETL files which were recorded from different machines at the same time.
Normally you have some key method which starts something in the frontend and some other method on the backend which 
is called shortly afterwards. You can then define two zero markers for multiple files where only the first matching 
marker will be applied. 

>ETWAnalyzer -dump CPU -ZeroTime First \*FrontendClick\*;\*BackendStartDownload\* -ZeroProcessName FE.exe;BE.exe -timefmt s -ProcessName FE.exe;BE.exe 

Or if you did emit marker events you can use them as well das zero point

>ETWAnalyzer -dump CPU -ZeroTime Marker \*FrontendClick\*;\*BackendStartDownload\*  -timefmt s -ProcessName FE.exe;BE.exe 

This works in principle with any number of ETW traces from any number of machines if you can identify by manual ETW analysis correlation events.
