# -Dump Mark

WPA has an ETW Marker graph which is useful to navigate to central timeopints of an instrumented test case.
You can write your own marker events via the undocumented API [EtwSetMark](https://web.archive.org/web/20170921050719/http://geekswithblogs.net/akraus1/archive/2015/09/26/167117.aspx)
which needs a ETW Tracing session id (0 is the default kernel session of the NT Kernel Logger which is used by xperf) and the string.


<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0 width=1804
 style='width:1352.65pt;border-collapse:collapse;border:none'>
 <tr style='height:79.1pt'>
  <td width=1804 valign=top style='width:1352.65pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:79.1pt'>
  <p class=MsoNormal><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>C:\&gt;EtwAnalyzer %f% -dump mark -timefmt s
  -MarkerFilter !*Screenshot*;!*One*;!*Claim*<br>
  TestCase_11299ms_Machine_CLT_TestStatus-Passed_20211023-213242 </span></p>
  <p class=MsoNormal><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>         </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#16C60C;background:black'>2.034  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:    2.034 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>CorrelationMarker_SimplifiedProfiling_Start<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>         </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>2.036  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:    2.036 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>Start by
  ProfilingGuard<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>         </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>6.348  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:    6.348 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>Operation
  CreateFindingsAssistant   started<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>         </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>6.677  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:    6.677 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>Operation
  CreateFindingsAssistant   Stopped Duration: 0.329 s<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>         </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>9.595  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:    9.595 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>Operation
  OpenFindingsAssistant   started<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>         </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>9.674  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:    9.674 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>Operation
  OpenFindingsAssistant   Stopped Duration: 0.079 s<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>10.628  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   10.628 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: patient
  browser disappeared<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>12.827  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   12.827 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_6_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.26-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>12.843  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   12.843 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_7_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.27-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>12.976  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   12.976 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_5_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.25-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>13.076  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   13.076 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_8_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.28-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>13.176  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   13.176 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_1_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.21-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>13.243  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   13.243 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_2_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.22-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>13.344  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   13.344 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_3_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.23-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>13.693  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   13.693 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_4_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.24-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>13.694  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   13.694 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: All segments
  filled UI_SEGMENT_DISPLAY<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>23.700  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   23.700 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>Stop by
  ProfilingGuard<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>23.970  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   23.970 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>CorrelationMarker_SimplifiedProfiling_Stop<br>
  <br>
  </span></p>
  </td>
 </tr>
</table>

You can also use in other -dump commands which support -ZeroTime one of these markers as zero timepoint to e.g. get all
method timings relative to the e.g. the Profiling_Start marker message which indicates the start of a profiling action.

To see all marker events relatvie to the start marker you can use this command

> EtwAnalyzer -dump mark -timefmt s -ZeroTime marker *Profiling_Start*

where %f% is an environment variable with *set f=-fd xxxx.json* to the actual Json file to shorten the command line.

<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0 width=1804
 style='width:1352.65pt;border-collapse:collapse;border:none'>
 <tr style='height:79.1pt'>
  <td width=1804 valign=top style='width:1352.65pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:79.1pt'>
  <p class=MsoNormal><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>C:\&gt;EtwAnalyzer %f% -dump mark -timefmt s
  -MarkerFilter !*Screenshot*;!*One*;!*Claim* -ZeroTime marker
  *Profiling_Start*<br>
  TestCase_11299ms_Machine_CLT_TestStatus-Passed_20211023-213242 <br>
           </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#16C60C;background:black'>2.034  </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#E74856;background:black'>DiffToZero:   
  0.000 s </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#B4009E;background:black'>CorrelationMarker_SimplifiedProfiling_Start<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>         </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>2.036  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:    0.002 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>Start by
  ProfilingGuard<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>         </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>6.348  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:    4.314 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>Operation
  CreateFindingsAssistant   started<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>         </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>6.677  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:    4.643 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>Operation
  CreateFindingsAssistant   Stopped Duration: 0.329 s<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>         </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>9.595  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:    7.561 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>Operation
  OpenFindingsAssistant   started<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>         </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>9.674  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:    7.640 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>Operation
  OpenFindingsAssistant   Stopped Duration: 0.079 s<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>10.628  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:    8.594 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: patient
  browser disappeared<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>12.827  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   10.792 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_6_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.26-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>12.843  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   10.808 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_7_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.27-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>12.976  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   10.941 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_5_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.25-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>13.076  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   11.041 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_8_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.28-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>13.176  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   11.142 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_1_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.21-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>13.243  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   11.208 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_2_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.22-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>13.344  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   11.310 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment
  Segment_3_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.23-s got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>13.693  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   11.659 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: Segment Segment_4_LayoutSector_08e3c377-bae3-4742-b7c1-17b106c9fae1.24-s
  got visible<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>13.694  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   11.660 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>UI: All segments
  filled UI_SEGMENT_DISPLAY<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>23.700  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   21.665 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>Stop by
  ProfilingGuard<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>23.970  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>DiffToZero:   21.936 s </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#B4009E;background:black'>CorrelationMarker_SimplifiedProfiling_Stop<br>
  <br>
  </span></p>
  </td>
 </tr>
</table>

