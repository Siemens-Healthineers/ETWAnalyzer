## Automated Profiling
You can record ETW data during performance regression tests. If the files name adheres to a specific file format it is recognized
by ETWAnalyzer. The compressed ETL data file name encodes
- Test Case Name
- Measured Time of test
- Computer where data was recorded
- Time when it was recorded

After extraction you can query by test case name, computer, and time range to quickly check in thousands of profiled test cases if an issue did occur only once or 
every time or it happens only since a specific build version. Doing such analysis manually was possible only with huge efforts by opening each ETL file in WPA and
check if the problem was present or not. Now it is a matter of seconds

The query below shows e.g. that with a specific software version (8.0.2102.1303)  the method *Business.Init* no longer shows up which
did consume before 1,2s of CPU time and did cause ca. 50ms of wait time. The displayed CPU and Wait times are summed for all threads within one process. This is the main reason why
the extracted Json files are much smaller than the original etl files.


<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0
 style='border-collapse:collapse;border:none'>
 <tr style='height:301.5pt'>
  <td width=1516 valign=top style='width:1137.35pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:301.5pt'>
  <p class=MsoNormal style='line-height:normal'><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>c:\Extract\VB60&gt;EtwAnalyzer
  -dump CPU -fd . -methods *Business.Init*;RtlUserThreadStart -pn Vortal
  -ShowOnMethod -testcase CallupAdhocColdReadingCR<br>
        </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>CPU ms     </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>Wait ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>Method</span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#3A96DD;background:black'> </span></p>
  <p class=MsoNormal style='line-height:normal'><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>2/10/2021
  10:04:17 AM CallupAdhocColdReadingCR_16351msFO9DE01T0162PC.20210210-100501 </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>8.0.2102.903 <br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>1285 ms       </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>61 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>Business.Init </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#C19C00;background:black'>Vortal(3144)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>    </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>32839 ms  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>1534595 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RtlUserThreadStart </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#C19C00;background:black'>Vortal(3144)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;
  background:black'>2/11/2021 1:01:58 AM CallupAdhocColdReadingCR_16451msFO9DE01T0162PC.20210211-010241
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>8.0.2102.1001 <br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>1304 ms       </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>75 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>Business.Init </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#C19C00;background:black'>Vortal(5648)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>    </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>30577 ms  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>1439256 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RtlUserThreadStart </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#C19C00;background:black'>Vortal(5648)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;
  background:black'>2/11/2021 1:02:24 PM CallupAdhocColdReadingCR_16565msFO9DE01T0162PC.20210211-130308
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>8.0.2102.1101 <br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>1288 ms       </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>82 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>Business.Init </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#C19C00;background:black'>Vortal(8292)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>    </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>30812 ms  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>1431427 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RtlUserThreadStart </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#C19C00;background:black'>Vortal(8292)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;
  background:black'>2/12/2021 9:40:00 AM
  CallupAdhocColdReadingCR_16577msFO9DE01T0162PC.20210212-094044 </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>8.0.2102.1201 <br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>1326 ms       </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>64 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>Business.Init </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#C19C00;background:black'>Vortal(9192)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>    </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>30793 ms  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>1462563 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RtlUserThreadStart </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#C19C00;background:black'>Vortal(9192)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;
  background:black'>2/13/2021 5:26:53 AM
  CallupAdhocColdReadingCR_16115msFO9DE01T0162PC.20210213-052736 </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>8.0.2102.1202 <br>
       </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>1283 ms       </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";color:#F9F1A5;background:black'>60 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>Business.Init </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#C19C00;background:black'>Vortal(5228)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>    </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>32569 ms  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>1372803 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RtlUserThreadStart </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#C19C00;background:black'>Vortal(5228)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;
  background:black'>2/14/2021 2:08:10 AM
  CallupAdhocColdReadingCR_14812msFO9DE01T0162PC.20210214-020852 </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>8.0.2102.1303 <br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>31238 ms  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>1408137 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RtlUserThreadStart </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#C19C00;background:black'>Vortal(3288)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;
  background:black'>2/14/2021 10:43:06 PM
  CallupAdhocColdReadingCR_15183msFO9DE01T0162PC.20210214-224348 </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>8.0.2102.1401 <br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>31211 ms  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>1389349 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RtlUserThreadStart </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#C19C00;background:black'>Vortal(32)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;
  background:black'>2/15/2021 7:27:53 PM
  CallupAdhocColdReadingCR_14697msFO9DE01T0162PC.20210215-192835 </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>8.0.2102.1504 <br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>30932 ms  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>1364505 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RtlUserThreadStart </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#C19C00;background:black'>Vortal(7036)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#3A96DD;
  background:black'>2/16/2021 4:08:51 PM CallupAdhocColdReadingCR_14831msFO9DE01T0162PC.20210216-160933
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>8.0.2102.1601 <br>
      </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>29401 ms  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#F9F1A5;background:black'>1397893 ms </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>RtlUserThreadStart </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#C19C00;background:black'>Vortal(1480)</span></p>
  </td>
 </tr>
</table>

Besides CPU/Wait times you can query for any method of system wide profiling data. Stacktag files make it easy to query e.g. for Virus Scanner, GC, JIT compilation
overhead and many other things which are otherwise difficult to see.

