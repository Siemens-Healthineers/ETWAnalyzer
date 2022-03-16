# ETWAnalyzer

## License
ETWAnalyzer main license is [MIT][ETWAnalyzerLicense].

## Open Source and 3rd Party Software
ETWAnalyzer uses Open Source and 3rd Party Software listed in [ReadMe.oss][ETWAnalyzerOSS].

## What is it? 
ETWAnalyzer can extract summary information into Json files from ETL files which are produced by the builtin profiling infrastructure of Windows [Event Tracing for Windows (ETW)](https://docs.microsoft.com/en-us/windows/win32/etw/about-event-tracing). 
The profiling data can be collected with e.g. [wpr](https://docs.microsoft.com/en-us/windows-hardware/test/wpt/windows-performance-recorder). The command line version of WPRUI is already installed on your machine if you are using Windows 10 or later. 
These ETL files are usually large (multi GB) and load slow into analysis tools such as 
[Windows Performance Analyzer (WPA)](https://docs.microsoft.com/en-us/windows-hardware/test/wpt/windows-performance-analyzer#:~:text=Included%20in%20the%20Windows%20Assessment,run%20in%20the%20Assessment%20Platform.) 
or [PerfView](https://github.com/microsoft/perfview). The design goal of ETWAnalyzer is to make the huge ETL files small by keeping just the data that is necessary to identify performance bottlenenecks or regression issues in 
one or a collection of thousands of ETL files. 

After the extraction ETWAnalyzer has many query commands to make the data much more accessible. It can query one or many files where the output is either printed to console, 
or written to a CSV file for further analysis. 

The Json files can also be accessed via a C# interface [**IETWExtract**](ETWAnalyzer\Documentation\ProgramaticAccess.md) which enables you to write custom analyzers.

Json files are much faster to query than the input ETL files without any slow symbol server lookups.
It is based on years of field experience with ETW file analysis to keep the extracted Json file size as small as possible while maximizing the insights you can get of the extracted files.
An ETW Json file is typically a few MB while the input .etl file including PDBs is hundreds of MB. 

## Contributing
Persons who want to contribute, miss specific data, or want to add your specific dump command? Check out [Contributing](ETWAnalyzer/Documentation/Contributing.md) to get started.

## Data Extraction
Data extraction is done for one or a directory of ETL files. Zipped ETL files are extracted. By default 75% of all cores are used.
Normally you would want to use all builtin extractors which include 


| Extractor  | What is extracted from ETL Into Json? |
| ------------- | ------------- |
| All  | Include all extractors  |
| Default  | Include all extractors except File  |
| CPU|CPU consumption of all proceses part of the recording. CPU Sampling (PROFILE) and/or Context Switch tracing (CSWITCH) data with stacks must be present. |
| Memory| Get workingset/committed memory machine wide and of all processes at trace start and a second time at trace end. MEMINFO_WS must be present. |
| Exception|Get all .NET Exception Messages, Type and their call stacks when present with Process,ThreadId and TimeStamp. To get call stacks you need symbols. See below -symServer section. The Microsoft-Windows-DotNETRuntime ETW provider with ExceptionKeyword 0x8000 and stacks must be present. |
| Disk| Disk IO summary and a per file summary of read/write/flush disk service times. DISK_IO data must be present in trace to get this data.|
| Module| Dump all loaded modules with file path and version. LOADER data must be present in trace. |
| File| Open/Close/Read/Write summary of all accessed files per process. The ETL file must contain FILEIO data.|
| Stacktag | Get from all processes the CPU call stack summary by the WPA stacktag names. To work properly you need symbols. See below -symServer section |

### Example

The following command extracts everything, using Microsoft symbols from a single ETL file. The option -AllCPU will include also methods with < 10 ms CPU time which 
are normally not relevant for performance regression issues to keep the file size as small as possible. 

<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0
 style='border-collapse:collapse;border:none'>
 <tr style='height:79.85pt'>
  <td width=1177 valign=top style='width:882.5pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;'>
  <p class=MsoNormal style='line-height:normal'><a name="_GoBack"></a><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>c:\&gt;EtwAnalyzer -extract All -filedir
  c:\temp\ZScaler_Download_Slow_100KB_Over100MBit_MouseLags.etl -symserver ms
  -AllCPU<br>
  1 - files found to extract.<br>
  Success Extraction of
  c:\temp\Extract\ZScaler_Download_Slow_100KB_Over100MBit_MouseLags.json<br>
  Extracted 1/1 - Failed 0 files.<br>
  Extracted: 1 files in 00 00:02:06, Failed Files 0</span></p>
  </td>
 </tr>
</table>

## Querying the Data
After extraction you have from the over 600 MB input file a small ca. 6 MB file in the output folder. 

![alt text](ETWAnalyzer\Documentation\Images\ExtractedDataFiles.png "Extracted Data Files")

ETWanalyzer will query all files in the current directory if you do not use -filedir/-fd xxx.  
The first query would be to check on which machine with how much memory, CPU and Windows version it was running. 

<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0
 style='border-collapse:collapse;border:none'>
 <tr style='height:79.85pt'>
  <td width=1177 valign=top style='width:882.5pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:79.85pt'>
  <p class=MsoNormal style='line-height:normal'><a name="_GoBack"></a><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>c:\&gt;EtwAnalyzer -dump stats -filedir c:\temp\extract\ZScaler_Download_Slow_100KB_Over100MBit_MouseLags<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;
  background:black'>c:\temp\extract\ZScaler_Download_Slow_100KB_Over100MBit_MouseLags.json<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#F9F1A5;background:black'>TestCase            </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: ZScaler_Download_Slow_100KB_Over100MBit_MouseLags<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>PerformedAt         </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: 2/4/2022 9:55:43 AM<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>Source              </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>:
  c:\temp\extract\ZScaler_Download_Slow_100KB_Over100MBit_MouseLags.json<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>SourceETLFileName   </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: c:\temp\ZScaler_Download_Slow_100KB_Over100MBit_MouseLags.etl<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>OSName              </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: Windows 10 Enterprise<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>OSBuild             </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: 19h1_release<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>OSVersion           </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: 10.0.18363<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>MemorySizeMB        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: 34234<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>NumberOfProcessors  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: 4<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>CPUSpeedMHz         </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: 2496<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>CPUVendor           </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: GenuineIntel<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>CPUName             </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: Intel(R) Core(TM) i5-6300U CPU @ 2.40GHz<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>HyperThreading      </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: True<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>SessionStart        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: 2/4/2022 9:55:43 AM +01:00<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>SessionEnd          </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: 2/4/2022 9:58:07 AM +01:00<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>SessionDurationS    </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: 143<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>Model               </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: HP ProBook 640 G2<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>AdDomain            </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: corp.net<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>IsDomainJoined      </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: True<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>Displays            </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>: Horizontal: 3840~0~0 Vertical: 2160~0~0 MemoryMiB: 1024~1024~1024 Name:
  Intel(R) HD Graphics 520~Intel(R) HD Graphics 520~Intel(R) HD Graphics 520</span></p>
  </td>
 </tr>
</table>

If you have a directory of files you can limit the output to specific properties with e.g. -Properties MemorySizeMB,OSName,NumberOfProcessors,CPUSpeedMHz,CPUVendor,CPUName to
get a quick overview of the machine specs of a bunch of extracted ETL files. 

Now we want to get an overview what the CPU consumption was of the top 9 CPU consumers of that file
<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0
 style='border-collapse:collapse;border:none'>
 <tr style='height:79.85pt'>
  <td width=1177 valign=top style='width:882.5pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:79.85pt'>
  <p class=MsoNormal style='line-height:normal'><a name="_GoBack"></a><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>c:\&gt;EtwAnalyzer -filedir
  c:\temp\extract\ZScaler_Download_Slow_100KB_Over100MBit_MouseLags -dump CPU -topN 9<br>
  2/4/2022 9:55:43 AM    ZScaler_Download_Slow_100KB_Over100MBit_MouseLags<br>
          </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>4477    ms </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>WPRUI.exe(29260)    
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;
  background:black'>wprui<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>4566    ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>MsSense.exe(5520)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>5620    ms</span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;background:
  black'> </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#F9F1A5;background:black'>ZSATunnel.exe(10832)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>5684    ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>Taskmgr.exe(9400)     </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#3A96DD;background:black'>/2<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>5955    ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>devenv.exe(6764)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>10756   ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>dwm.exe(1428)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>14619   ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>explorer.exe(10696)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>15170   ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>ETWController.exe(18908)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>63540   ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>System(4)</span></p>
  </td>
 </tr>
</table>

The mouse was hanging while I was downloading data. At the same time the CPU was fully utilized on my quad core notebook.
The by far highest CPU consumer was the Windows Kernel which sits in the System process. Lets pick that one and print the top 50 methods.

<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0
 style='border-collapse:collapse;border:none'>
 <tr style='height:79.85pt'>
  <td width=1398 valign=top style='width:1048.25pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:79.85pt'>
  <p class=MsoNormal style='line-height:normal'><a name="_GoBack"></a><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>c:\&gt;EtwAnalyzer -filedir
  c:\temp\extract\ZScaler_Download_Slow_100KB_Over100MBit_MouseLags -dump cpu
  -processName System -methods * -topnmethods 50<br>
        </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>CPU ms     </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>Wait ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>Method<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;
  background:black'>2/4/2022 9:55:43 AM
  ZScaler_Download_Slow_100KB_Over100MBit_MouseLags<br>
     </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>System(4)<br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>1603 ms</span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>RtlEqualSid<br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>1660 ms        </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>0 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>TcpInspectReceive<br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>1895 ms        </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>0 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>TcpDeliverDataToClient<br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>1896 ms        </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>0 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>TcpDeliverReceive<br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>4795 ms        </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>0 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>KxWaitForSpinLockAndAcquire<br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>4801 ms        </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>0 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>KeAcquireSpinLockAtDpcLevel<br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>4886 ms        </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>0 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>IpNlpSendDatagrams<br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>6639 ms </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>       0 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>TcpTcbCarefulDatagram<br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>9558 ms        </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>0 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>MatchValues<br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>9770 ms        </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>0 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>TcpTcbSend<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>14979 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>IpNlpFastSendDatagram<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>19086 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>IppInspectLocalDatagramsOut<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>19845 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>TcpTcbHeaderSend<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>19847 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>IppSendDatagramsCommon<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>23857 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>InetInspectReceiveTcpDatagram<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>31317 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>TcpTcbReceive<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>34487 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>TcpMatchReceive<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>42063 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>IndexListClassify<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>42717 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>AleInspectTcpDatagram<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>42728 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>WfpAlepReauthorizeOutboundConnection<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>42738 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>WfpAleReauthorizeOutboundConnection<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>42749 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>WfpAleReauthorizeConnection<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>43131 ms</span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>KfdClassify<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>49756 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>TcpReceive<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>49757 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>TcpNlClientReceiveDatagrams<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>49830 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>IppProcessDeliverList<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>49848 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>IppReceiveHeaderBatch<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>49883 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>IppFlcReceivePacketsCore<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>49938 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>IppIndicatePrevalidatedPacketsToIpsServiceChain<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>49944 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>IpFlcReceivePreValidatedPackets<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50009 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms</span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'> FlReceiveNetBufferListChainCalloutRoutine<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50027 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>FlReceiveNetBufferListChain<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50133 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>VmsMpNicPvtReceiveRssProcessNblGroup<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50156 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>ndisMIndicateNetBufferListsToOpen<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50164 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>VmsVrssDpc<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50166 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>ndisMTopReceiveNetBufferLists<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50336 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>NdisMIndicateReceiveNetBufferLists<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50338 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>ndisCallReceiveHandler<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50341 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>ndisInvokeNextReceiveHandler<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50361 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>KiExecuteAllDpcs<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50377 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>KzLowerIrql<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50397 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>KxRetireDpcList<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50397 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>KiRetireDpcList<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50399 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>KiDispatchInterruptContinue<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50441 ms       </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>75 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>KeExpandKernelStackAndCalloutEx<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50441 ms       </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>75 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>KeExpandKernelStackAndCalloutInternal<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50475 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>KiDpcInterrupt<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>50476 ms   </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>167607 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>bridge.sys<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>54480 ms  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>2655067 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>KiStartSystemThread<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>54480 ms  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>2655067 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>PspSystemThreadStartup</span></p>
  </td>
 </tr>
</table>

To understand the data you need to know that ETWAnalyzer keeps for every method in a process the method inclusive times 
for CPU and Wait summed accross all threads. CPU timing is extracted from CPU sampling data. Wait times are determined from Context Switch data which signal the time a
method was moved off a CPU due to a blocking OS call. That is the reason why Main or other entry point methods for a thread have the highest CPU
consumption but are not the performance bottleneck. The actual issue is in one of the methods which consume CPU which is not directly visible
in the extracted data.
To get an overview for a new issues one turns over to WPA to see time dependencies which can only visually be analyzed.

Once the issue is understood you can create [WPA stacktags](https://docs.microsoft.com/en-us/windows-hardware/test/wpt/stack-tags) of past issues to see if the same issue 
in the other file also appears without the need to drill deep into the call stacks. In this case it is obvious that we have again a Firewall problem where each network packet 
gets an expensive check in WfpAlepReauthorizeOutboundConnection which traverses long (many firewall rules?) lists. 

![alt text](ETWAnalyzer\Documentation\Images\WPA_HighCPUAnalysis.png "WPA Analysis")

Bad things keep coming back. After having identified a pattern in WPA we can check other ETW files either manually or we query the data with ETWAnalyzer and save a 
lot of time. Since WPA and ETWAnalyzer support stacktags we can dump the top 10 stacktags for the System process. 
After adding "-stacktags *" to the command line we get all stacktags. If you add "-stacktags * -methods *" all methods and stacktags are printed together in one list.

**Note: There is only one filter named -topNMethods which filters for the overall top CPU consumers for methods and stacktags.**

<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0
 style='border-collapse:collapse;border:none'>
 <tr style='height:79.85pt'>
  <td width=1398 valign=top style='width:1048.25pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:79.85pt'>
  <p class=MsoNormal style='line-height:normal'><a name="_GoBack"></a><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>c:\&gt;EtwAnalyzer -filedir
  c:\temp\extract\ZScaler_Download_Slow_100KB_Over100MBit_MouseLags -dump cpu
  -processName System -stacktags * -topnmethods 10<br>
        </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>CPU ms     </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>Wait ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>Method<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;
  background:black'>2/4/2022 9:55:43 AM
  ZScaler_Download_Slow_100KB_Over100MBit_MouseLags<br>
     </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>System(4)<br>
         </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>24 ms    </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>15450 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>Waits\Normal Waits\OS Wait<br>
         </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>25 ms        </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>0 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>Waits\File System Callback - possible Virus Scanner<br>
         </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>60 ms </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>   54417 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>Waits\Normal Waits\GatherMappedPages-Timer<br>
         </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>88 ms   </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>264875 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>Antivirus - Windows Defender<br>
        </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>110 ms     </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>3094 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>Windows\Write Modified Data To Disk<br>
        </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>431 ms        </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>0 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>Windows\WorkingSetTrim<br>
        </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>714 ms        </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>0 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>Tracing Overhead\ETW Stackwalks<br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>1125 ms    </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>45753 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;background:
  black'>Windows\Zero Page Thread<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>17625 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>Other<br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>43315 ms        </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:
  black'>0 ms</span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'> Windows\Windows Firewall</span></p>
  </td>
 </tr>
</table>

From the stacktag CPU consumption we find as top match "Windows\Windows Firewall" which proves that we have hit the same issue again. 

From the WPA stacktrace we know that these calls are executed as [DPCs (Deferred Procedure Calls)](https://en.wikipedia.org/wiki/Deferred_Procedure_Call) which are normally issued for longer running tasks from 
an interrupt handler. The DPCs for all network packets consume up to all 4 cores on my machine which compete with the mouse interrupt handling
resulting in a slow, sluggish system. It is time to call Microsoft Support to ask where this is coming from. To check which dll version 
we are running one would turn over to WPA and open the Images graph to find which dlls in which version are loaded. 
There you can find the exact patch level of the OS or your application dlls which are loaded by a process.

ETWAnalyzer amends version information to CPU data if you add to **-dump CPU** *-ShowModuleInfo* or *-smi*. That will show besides the method names module version data. 
If you are not using a console with a high console buffer width the output becomes unreadable due to word wrapping. To better support non wraping consoles
you can add -Clip to all commands of ETWAnalyzer to prevent wraparound of output. Besides the version we would need the dll which is by default
not printed. But you can add *-IncludeDll* or *-id* to get besides method names also the dll name. 

![alt text](ETWAnalyzer\Documentation\Images\ETWAnalyzer_ClippedOutput.png "Clipped Output")

That small intro showed some of the key features of ETWAnalyzer. With this tool it is easy to detect patterns in thousands of ETL files which
was an impossible task with other publicly available tools. If you have performance trending tests it makes a lot of sense to run them with ETW profiling enabled 
so you can later find systematic deviations with a simple query. Issues which were before that tool 
too much work to track down are now a simple query. If your test is e.g. 3 out of 30 times 20% slower you can query all tests for common patterns to 
see if e.g. a running Windows Installer did have an effect to your test execution time or if that did occur in other fast tests as well. 

The currently supported dump commands are
- [CPU](ETWAnalyzer/Documentation/DumpCPUCommand.md) 
- [Disk](ETWAnalyzer/Documentation/DumpDiskCommand.md) 
- [File](ETWAnalyzer/Documentation/DumpFileCommand.md) 
- [Stats](ETWAnalyzer/Documentation/StatsCommand.md)
- [Process](ETWAnalyzer/Documentation/DumpProcessCommand.md) 
- [Memory](ETWAnalyzer/Documentation/DumpMemoryCommand.md) 
- [Version](ETWAnalyzer/Documentation/DumpVersionCommand.md)
- [Exception](ETWAnalyzer/Documentation/DumpExceptionCommand.md)
- [ThreadPool](ETWAnalyzer/Documentation/DumpThreadPoolCommand.md) 
- [Mark](ETWAnalyzer/Documentation/DumpMarkCommand.md)
- [TestRun](ETWAnalyzer/Documentation/DumpTestRunCommand.md)

which all support -filedir and an extensive command line help what you can dump from the extracted data. 



<!-- References -->
[ETWAnalyzerLicense]:                                       <LICENSE>
[ETWAnalyzerOSS]:                                           <ETWAnalyzer/3rdParty/ReadMeOSS.md>
