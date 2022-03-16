# -Dump Process
Print process names and command lines of all processes. Additionally the start/end times, return code and parent process id are printed.
This command works with ETL files and extracted Json files.


By default all processes with command line are printed. Processes are sorted by executable and process start 
time. This view is useful to quickly identify processes which are frequently started by e.g. a timer based event.

<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0 width=2033
 style='width:1524.55pt;border-collapse:collapse;border:none'>
 <tr style='height:237.5pt'>
  <td width=2033 valign=top style='width:1524.55pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:237.5pt'>
  <p class=MsoNormal style='line-height:normal'><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>c:\Temp\Extract&gt;EtwAnalyzer
  -fd ZScaler_Download_Slow_100KB_Over100MBit_MouseLags -Dump Process -clip<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#61D6D6;
  background:black'>ZScaler_Download_Slow_100KB_Over100MBit_MouseLags<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>13316  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop:                         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:         
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'> RCode:     Parent: 13376 AdobeCollabSync.exe   --type=collab-renderer
  --proc=13376<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>13376  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop:                         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:          
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RCode:     Parent: 10696 AdobeCollabSync.exe<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>9948   </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop:                         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:           </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode:     Parent:  1040 ApplicationFrameHost.exe  -Embedding<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>4708   </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop:                         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:          
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RCode:     Parent:   908 armsvc.exe<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>27708  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#3A96DD;background:black'>2022-02-04 09:55:58.946 Duration:           </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode:   0 Parent:  4024 audiodg.exe  0x700 0x704<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>18808  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop:                         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:         
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'> RCode:     Parent:  1852 cardoscp.exe<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>13708  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop:                         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:          
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RCode:     Parent:   908 CcmExec.exe<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>1172   </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop:                         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:          
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RCode:     Parent: 22408 chrome.exe  --type=renderer
  --display-capture-permissions-policy-allowed --allow-sync-xhr-in-...<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>3156   </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop:                         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:          
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RCode:     Parent: 22408 chrome.exe  --type=renderer
  --display-capture-permissions-policy-allowed --allow-sync-xhr-in-...<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>3324   </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop:                         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:          
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RCode:     Parent: 22408 chrome.exe  --type=utility
  --utility-sub-type=audio.mojom.AudioService --field-trial-handle=1...<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>7804   </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop:                         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:          
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RCode:     Parent: 22408 chrome.exe  --type=renderer
  --display-capture-permissions-policy-allowed --allow-sync-xhr-in-...<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>9736   </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop:                         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:          
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RCode:     Parent: 22408 chrome.exe  --type=gpu-process
  --field-trial-handle=1684,9513942465996862471,9744407511422669...<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>10080  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop:                         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:           </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode:     Parent: 22408 chrome.exe  --type=renderer
  --extension-process --display-capture-permissions-policy-allowed ...<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>10168  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop:                         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:          
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RCode:     Parent: 22408 chrome.exe  --type=renderer
  --extension-process --display-capture-permissions-policy-allowed ...</span></p>
  </td>
 </tr>
</table>

Start/stop times are printed in a fixed locale independent time format which defaults to the .NET format string "yyyy-MM-dd HH:mm:ss.fff".
ETWAnalyzer supports custom time formats for all commands which print time based values.
The common command line switch is -TimeFmt 

To print time in seconds since trace start to correlate with WPA time from the Windows Performance Analyzer you can use *-TimeFmt s*

<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0 width=1327
 style='width:995.55pt;border-collapse:collapse;border:none'>
 <tr style='height:89.8pt'>
  <td width=1327 valign=top style='width:995.55pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:89.8pt'>
  <p class=MsoNormal style='line-height:normal'><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>C&gt;EtwAnalyzer
  -fd ZScaler_Download_Slow_100KB_Over100MBit_MouseLags -Dump Process 
  -TimeFmt s -NewProcess 1 -NoCmdLine<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#61D6D6;
  background:black'>ZScaler_Download_Slow_100KB_Over100MBit_MouseLags<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>28360  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:    </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>9.738 </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>Stop:   </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>17.386
  Duration:       8 s </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>RCode:   0 Parent:  1040 dllhost.exe<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>18756  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:    </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>9.952 </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>Stop:   </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>17.385
  Duration:       7 s </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>RCode:   0 Parent:  1040 dllhost.exe<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>21896  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>  13.507 </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>Stop:   </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>43.767
  Duration:      30 s </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>RCode:   0 Parent: 13404 Teams.exe<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>17336  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:   </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>51.064 </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>Stop:          </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:          
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RCode:     Parent: 13404 Teams.exe</span></p>
  </td>
 </tr>
</table>

## General options available in (nearly) all commands to ETWAnalyzer

The following options are common to other -dump commands where they can be used also, not only for -dump Process.

### Time Formatting
**-TimeFmt** formats time in custom ways. There are other options like *-ProcessFmt* which prints in other commands the start/stop times besides the process name, or *-FirstLastDuration* of -Dump CPU which supports the same enumeration values. The names
are consistent with the names WPA is using. In WPA you can configure every column which displays a time value to
- Time since trace start in seconds (Default)
- Local Time
- UTC

By default WPA will show time as time in seconds since trace start which is for ETWAnalyzer *-timefmt s*. 


![alt text](Images\WPA_TimeFormat.png "WPA Time Format")


Supported values for *-TimeFmt* are:

| -TimeFmt timefmt      | Description |
| ----------- | ----------- |
| s or second      | Print as time in seconds since trace start. This is the time WPA is showing in the UI.       |
| Local     | Print time as local time on which the data was recorded. This is usually the time customers report when something did fail.  
| LocalTime | Same as Local but without date string. |
| UTC|  Print time in UTC (Universal Coordinated Time).|
| UTCTime| Same as UTC but without date string. |
| Here|  Print time as local time in the current system time zone.|
| HereTime|  Same as Here but without date string. |

**Examples**

| -TimeFmt timefmt      | Description |
| ----------- | ----------- |
| s or second      | 51.064|
| Local     | 2022-02-04 09:56:34.670  
| LocalTime | 09:56:34.670 |
| UTC|  2022-02-04 08:56:34.670|
| UTCTime| 08:56:34.670|


## Process Selection/Filters

You can filter by process lifetime with *-NewProcess*. The supported values are:

| -NewProcess      | Description |
| ----------- | ----------- |
|    0   | All processes which have been running from trace start-end.   |
|    1   | Processes which have been started and potentially exited during the trace.   |
|    -1   |  Processes which have exited during the trace but have been potentially also started.  |
|    2   |  Processes which have been started but not stopped during the trace.  |
|    -2   |  Processes which are stopped but not started during the trace.  |
                                                     
The option *-ProcessName* can filter by process id or process name. *-ProcessName* can be abbreviated with *-pn*.
ETWAnalyzer has a general notation of filters. 

See [Filters](Filters.md) for more information. 


## Command Line Filters

The option *-CmdLine* filters for substrings of command line arguments to any process. To filter for e.g. all chrome instances which have -renderer in their command line would be 
> -CmdLine "\*--type=renderer\*" -ProcessName Chrome 

See [Filters](Filters.md) for more information.

## -PlainProcessNames

ETWAnalyzer is used at large scale software projects where many generic process names are present. The same is true on any Windows machine where you have 
a myriad of svchost.exe instances running different services. To make life easier ETWAnalyzer supports in the folder Configuration/ProcessRenameRules.xml
a xml file which is used to rename processes based on executable name and command line parameters.
Below is an example how a typical process rename file looks like:
```
<?xml version="1.0"?>
<ProcessRenamer xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <ProcessRenamers>
    <RenameRule>
      <ExeName>svchost.exe</ExeName>
      <CmdLineSubstrings>
        <string>-s EventLog</string>
      </CmdLineSubstrings>
      <NewExeName>svchost Event Log Service</NewExeName>
    </RenameRule>
    <RenameRule>
      <ExeName>svchost.exe</ExeName>
      <CmdLineSubstrings>
        <string>-s TermService</string>
      </CmdLineSubstrings>
      <NewExeName>svchost RDP Service</NewExeName>
    </RenameRule>
    <RenameRule>
      <ExeName>svchost.exe</ExeName>
      <CmdLineSubstrings>
        <string>-s Winmgmt</string>
      </CmdLineSubstrings>
      <NewExeName>svchost WMI Service</NewExeName>
    </RenameRule>
  </ProcessRenamers>
</ProcessRenamer>
```
The end result is a renamed svhost process name which has a descriptive name. 

<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0 width=1561
 style='width:1170.55pt;border-collapse:collapse;border:none'>
 <tr style='height:74.7pt'>
  <td width=1561 valign=top style='width:1170.55pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:74.7pt'>
  <p class=MsoNormal style='line-height:normal'><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>C&gt;EtwAnalyzer
  -fd test.json -dump process -pn &quot;svchost *&quot;<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#61D6D6;
  background:black'>test<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>2404   </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:           
               Stop:                         </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:          
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RCode:     Parent:  1728 svchost Event Log Service  -k
  LocalServiceNetworkRestricted -p -s EventLog<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>4652   </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop:                         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>Duration:          
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RCode:     Parent:  1728 svchost WMI Service  -k netsvcs -p
  -s Winmgmt</span></p>
  </td>
 </tr>
</table>

If you are for some reason confused or do not need this feature you can add *-PlainProcessNames* to disable automatic process renaming.

# -Dump Process Specific Command line options
##  -Crash
This option will check if any process in the ETW trace did crash. One way to detect crashed processes is to check if WerFault.exe was invoked for a process. Whenever a process crashes 
Windows error Reporting will be called back to create potentially a memory dump of the process. 
There is also another not widely known strategy. If a process dies due to an unhandled exception the Exception code is the return value of the crashed process which is 
a [NTStatus](https://www.osr.com/blog/2020/04/23/ntstatus-to-win32-error-code-mappings/) value. This value is similar to a Win32 error code but these
codes are used at the other side in the Kernel, although some mappings for most codes exist.
ETWAnalzyer employs both techniques to detect potentially crashed process and with unusual NTStatus return code.

<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0 width=1327
 style='width:995.55pt;border-collapse:collapse;border:none'>
 <tr style='height:89.8pt'>
  <td width=1327 valign=top style='width:995.55pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:89.8pt'>
  <p class=MsoNormal style='line-height:normal'><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>C&gt;EtwAnalyzer
  -dump Process -crash -NoCmdLine</span></p>
  <p class=MsoNormal style='line-height:normal'><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#61D6D6;background:black'>10_52_12.18LongTrace<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>3536   </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start:                        
  Stop: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#3A96DD;background:black'>2021-12-02 10:51:31.636 Duration:           </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode: HEAP_CORRUPTION Parent: 46844 Job.exe</span></p>
  <p class=MsoNormal style='line-height:normal'><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#61D6D6;background:black'>12_07_10.18LongTrace<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>112936 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>2021-12-02 12:04:13.428 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Stop: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;background:
  black'>2021-12-02 12:04:47.876 Duration:      34 s </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode: CONTROL_C_EXIT Parent: 49968 Test.exe<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>149744 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>2021-12-02 11:43:52.898 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Stop: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;background:
  black'>2021-12-02 11:43:54.676 Duration:       2 s </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode:   0 Parent: 144368 WerFault.exe<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>86848  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>2021-12-02 11:43:54.690 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Stop: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;background:
  black'>2021-12-02 11:44:10.002 Duration:      15 s </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode:   0 Parent: 136816 WerFault.exe</span></p>
  </td>
 </tr>
</table>

Here we find that Job.exe did crash with a Heap corruption which did trigger no WerFault, because a custom error handler was installed. 
The process Test.exe did exit because with was terminated by the user via Ctrl-C. 

This option is most useful if you have some long term ETW Traces at hand to record a long running test for the complete session duration 
with lightweight profiling. A combination of file based traces where every 10 minutes a new file is started where process start/stop and 
10 ms CPU sampling is enabled along with Disk IO has proven to be a good compromise between the amount of data gathered and what issues one 
still can detect even if no Context Switch data or high CPU sampling data is present. 

## -SortBy
To change sorting by process names the following values are supported

| -SortBy      | Description |
| -----------  | ----------- |
|    Default   | Sort by process name and start time   |
|   Time       | Sort by process start/end time. It displays processes in 3 groups: Running, Ended, Started where the processes are sorted accordingly. |  

## -MinMaxDuration
Filter for processes with specific runtime range. The time is entered in seconds with your current locale dependent decimal point character.

## -Merge
This option can be used to calculate process lifetime over a collection of ETW trace files which did cover a longer test 
run where the data did not fit into one ETL file. The example below e.g. filters out all svchost processes which 
did run for longer than 10 minutes.
The process start/stop times in ETW files which is missing the start/stop event receives the values gathered by earlier or later ETW files
so one can see what the true run time was. 

<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0 width=1470
 style='width:1102.55pt;border-collapse:collapse;border:none'>
 <tr style='height:69.0pt'>
  <td width=1470 valign=top style='width:1102.55pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:69.0pt'>
  <p class=MsoNormal style='line-height:normal'><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>C&gt;EtwAnalyzer
  -dump process -Merge -MinMaxDuration 600 -pn svchost<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#61D6D6;
  background:black'>_9_10_59.14LongTrace<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>58068  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>2021-12-02 09:08:01.770 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Stop: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;background:
  black'>2021-12-02 09:20:37.747 Duration:    13 min </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode:     Parent:  2328 svchost.exe  -k netsvcs -p -s BITS<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#61D6D6;
  background:black'>_9_25_20.16LongTrace<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>58068  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>2021-12-02 09:08:01.770 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Stop: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;background:
  black'>2021-12-02 09:20:37.747 Duration:    13 min </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode:   0 Parent:  2328 svchost.exe  -k netsvcs -p -s BITS<br>
  PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>116160 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>2021-12-02 09:25:01.466 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Stop: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;background:
  black'>2021-12-02 09:35:10.734 Duration:    10 min </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode:     Parent:  2328 svchost.exe  -k netsvcs -p -s BITS<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#61D6D6;
  background:black'>_9_39_46.18LongTrace<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>116160 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>2021-12-02 09:25:01.466 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Stop: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;background:
  black'>2021-12-02 09:35:10.734 Duration:    10 min </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode:   0 Parent:  2328 svchost.exe  -k netsvcs -p -s BITS<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#61D6D6;
  background:black'>_9_46_58.10LongTrace<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>98888  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>2021-12-02 09:41:59.503 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Stop: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;background:
  black'>2021-12-02 09:52:10.412 Duration:    10 min </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode:     Parent:  2328 svchost.exe  -k netsvcs -p -s BITS<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#61D6D6;
  background:black'>_9_54_11.15LongTrace<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>98888  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>2021-12-02 09:41:59.503 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Stop: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;background:
  black'>2021-12-02 09:52:10.412 Duration:    10 min </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode:   0 Parent:  2328 svchost.exe  -k netsvcs -p -s BITS<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#61D6D6;
  background:black'>10_01_24.17LongTrace<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>75816  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>2021-12-02 09:57:29.839 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Stop: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;background:
  black'>2021-12-02 10:10:29.785 Duration:    13 min </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode:     Parent:  2328 svchost.exe  -k netsvcs -p -s BITS<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#61D6D6;
  background:black'>10_15_51.18LongTrace<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>PID: </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>75816  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Start: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;background:
  black'>2021-12-02 09:57:29.839 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>Stop: </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;background:
  black'>2021-12-02 10:10:29.785 Duration:    13 min </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>RCode:   0 Parent:  2328 svchost.exe  -k netsvcs -p -s BITS</span></p>
  </td>
 </tr>
</table>

## -ShowFileOnLine  
Do not print for each file the input ETL name, but print the source file name on each output line.
![alt text](Images\ShowFileOnLine.png "Show File On Line")