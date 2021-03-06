# -Dump Stats
Print information about the machine where the recording was taken. This command works with extracted and ETL files. 

## ETL File Mode
It will show session start/end time, CPU Speed, OS Version and a list of all recorded events sorted by their size.

```
C>EtwAnalyzer -dump stats -fd c:\temp\ZScaler_Download_Slow_100KB_Over100MBit_MouseLags.etl
Session Start 2/4/2022 9:55:43 AM - End: 2/4/2022 9:58:07 AM, Duration: 00:02:23.7474711, OSVersion: 10.0, PtrSize: 8
Cores: 4 Speed: 2496 MHz
Lost Events: 0
EventTrace/BuildInfo                                    Count:             1 SizeInBytes:             42 Id:       Name: MSNT_SystemTrace                                PGUID: 9e814aad-3204-11d2-9a82-006008a86939 Task: 68fdd900-4a3e-11d1-84f4-0000f80464e3 OpCode:   66 Keywords: 0x0
..
TcpipNblOob                                             Count:        58,444 SizeInBytes:      4,441,744 Id:  1367 Name: Microsoft-Windows-TCPIP                         PGUID: 2f07e2ee-15db-40f1-90ef-9d7ba282188a Task: 00000000-0000-0000-0000-000000000000 OpCode:    0 Keywords: 0x8000000300000000
InetInspect                                             Count:        66,578 SizeInBytes:      1,864,184 Id:  1454 Name: Microsoft-Windows-TCPIP                         PGUID: 2f07e2ee-15db-40f1-90ef-9d7ba282188a Task: 00000000-0000-0000-0000-000000000000 OpCode:    0 Keywords: 0x8000000000000080
CLRMethod/MethodUnloadVerbose                           Count:        78,658 SizeInBytes:     25,036,320 Id:   144 Name: Microsoft-Windows-DotNETRuntime                 PGUID: e13c0d23-ccbc-4e12-931b-d9cc2eee27e4 Task: 3044f61a-99b0-4c21-b203-d39423c73b00 OpCode:   38 Keywords: 0x30
FileIo/FileRundown                                      Count:       126,189 SizeInBytes:     30,905,088 Id:       Name: MSNT_SystemTrace                                PGUID: 9e814aad-3204-11d2-9a82-006008a86939 Task: 90cbdc39-4a3e-11d1-84f4-0000f80464e3 OpCode:   36 Keywords: 0x0
CLRMethodRundown/MethodDCEndVerbose                     Count:       157,316 SizeInBytes:     50,072,640 Id:   144 Name: Microsoft-Windows-DotNETRuntimeRundown          PGUID: a669021c-c450-4609-a035-5af59af4df18 Task: 0bcd91db-f943-454a-a662-6edbcfbb76d2 OpCode:   40 Keywords: 0x30
PerfInfo/SampleProf                                     Count:       166,074 SizeInBytes:      2,657,184 Id:       Name: MSNT_SystemTrace                                PGUID: 9e814aad-3204-11d2-9a82-006008a86939 Task: ce1dbfb4-137e-4da6-87b0-3f59aa102cbc OpCode:   46 Keywords: 0x0
EventID(60003)                                          Count:       166,450 SizeInBytes:      3,329,000 Id: 60003 Name: Microsoft-Windows-Networking-Correlation        PGUID: 83ed54f0-4d48-4e45-b16e-726ffd1fa4af Task: 00000000-0000-0000-0000-000000000000 OpCode:    9 Keywords: 0x8000000000000001
Thread/ReadyThread                                      Count:       489,003 SizeInBytes:      3,912,024 Id:       Name: MSNT_SystemTrace                                PGUID: 9e814aad-3204-11d2-9a82-006008a86939 Task: 3d6fa8d1-fe05-11d0-9dda-00c04fd7ba7c OpCode:   50 Keywords: 0x0
Thread/CSwitch                                          Count:       961,175 SizeInBytes:     23,068,200 Id:       Name: MSNT_SystemTrace                                PGUID: 9e814aad-3204-11d2-9a82-006008a86939 Task: 3d6fa8d1-fe05-11d0-9dda-00c04fd7ba7c OpCode:   36 Keywords: 0x0
StackWalk/Stack                                         Count:     2,076,774 SizeInBytes:    287,564,248 Id:       Name: MSNT_SystemTrace                                PGUID: 9e814aad-3204-11d2-9a82-006008a86939 Task: def2fe46-7bd6-4b80-bd94-f57fe20d0ce3 OpCode:   32 Keywords: 0x0
```

This is similar to what you see in WPA Trace- System Configuration - Trace Statistics
![alt text](Images/WPA_TraceStatistics.png "WPA Trace Statistics")

You would want to use that data to tailor your recording profiles to record just the things you need for a specific issue. It is also useful to check if the events you 
intended to record are part of the trace or if something did go wrong while recording the data.
The event names are not always 100% matching with WPA because ETWAnalyzer uses TraceEvent which powers PerfView to quickly display this data from an ETL file. 

## Extracted Mode

When you use -Dump Stats with extracted Json files you get a different view on the data where information
similar to WPA - Trace - System Configuration - General:


![alt text](Images/WPA_Trace_General.png "WPA Trace General")


You can get the same data also from the command line:

![](Images/DumpStatsCommand.png "Dump Stats")


