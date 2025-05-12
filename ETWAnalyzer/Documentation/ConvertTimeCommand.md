# -ConvertTime 
Convert a time or DateTime string to ETW session time to allow correlation of key events from different data sources. It is common to e.g. search 
for key frames in screenshots which are timestamped from e.g. ETWController or events coming from log files to filter events based on the time stamp.
ETWAnalyzer prints out time strings in human readable strings with ```-timefmt Local/UTC/Here``` date and ```timedigits xx```

- yyyy-MM-dd HH:mm:ss
- yyyy-MM-dd HH:mm:ss.f
- yyyy-MM-dd HH:mm:ss.ff
- yyyy-MM-dd HH:mm:ss.fff
- yyyy-MM-dd HH:mm:ss.ffff
- yyyy-MM-dd HH:mm:ss.fffff
- yyyy-MM-dd HH:mm:ss.fffff

Time is printed in these formats with ```-timefmt LocalTime/UTCTime/HereTime``` and ```timedigits xx```
- HH:mm:ss
- HH:mm:ss.f
- HH:mm:ss.ff
- HH:mm:ss.fff
- HH:mm:ss.ffff
- HH:mm:ss.fffff
- HH:mm:ss.ffffff

Additionally the following time formats are supported

- yyyy-MM-ddTHH:mm:ss.fffK
- [Round Trip](https://learn.microsoft.com/en-us/dotnet/standard/base-types/how-to-round-trip-date-and-time-values) Time format from DateTimeOffset O

## Examples

With -Dump TCP the TCP times by default are printed in like "2025-05-06 11:05:29.116". To get the ETW session time you can use in 
console mode you can use 
```
.converttime -time "2025-05-06 11:05:29.116" 
12.344417 seconds since session start for file 11_10_17.17LongTrace
```

You can also convert from an ETW session time to local time for one or all currently loaded files
```
.converttime -time 10.0s  -fd *15_18_01.13LongTrace*
2025-05-06 15:13:10.369185 for file 15_18_01.13LongTrace.json7z
```

This can be used to get all loaded ETW session start times with high precision compared to the ```-dump stats``` command.
```
>.converttime -time 0                                       
2025-05-06 10:59:54.426914 for file 11_04_55.13LongTrace.json7z
2025-05-06 11:05:16.771583 for file 11_10_17.17LongTrace.json7z
2025-05-06 11:10:37.138413 for file 11_15_37.17LongTrace.json7z
2025-05-06 11:15:58.801786 for file 11_20_59.14LongTrace.json7z
2025-05-06 11:21:21.476221 for file 11_26_22.20LongTrace.json7z
2025-05-06 11:26:48.582591 for file 11_31_49.14LongTrace.json7z
2025-05-06 11:32:11.700472 for file 11_37_12.19LongTrace.json7z
...

>.dump stats -properties SessionStart 
5/6/2025 10:59:54 AM   11_04_55.13LongTrace 9.5.2501.2904 Falcon
        SessionStart        : 5/6/2025 10:59:54 AM +02:00
5/6/2025 11:05:16 AM   11_10_17.17LongTrace 9.5.2501.2904 Falcon
        SessionStart        : 5/6/2025 11:05:16 AM +02:00
```