# -Dump ThreadPool

Dump ocurrences of .NET ThreadPool starvation events. These are useful to determine if
an application could have become slower due to increased async/await or TPL usage. The .NET Framework will add a new 
TPL thread with a 1s delay when the ThreadPool size has reached the core count. Things have
changed with .NET 6.0 but it is still useful to see. 


<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0 width=696
 style='width:521.95pt;border-collapse:collapse;border:none'>
 <tr style='height:119.55pt'>
  <td width=696 valign=top style='width:800.95pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:119.55pt'>
  <p class=MsoNormal><span style='font-size:12.0pt;font-family:"Lucida Console";
  color:#CCCCCC;background:black'>C:\&gt;EtwAnalyzer %f% -dump ThreadPool
  -timefmt s<br>
  TestName_11299ms_Machine_CLT_TestStatus-Passed_20211023-213242<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#F9F1A5;
  background:black'>Agent.Worker.exe(1612) </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#3A96DD;background:black'>spawnclient 2964
  2284<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>Starvation at      4.637  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>ThreadCount:   5 </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#B4009E;background:black'>DiffSinceLast   0.000 s<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#CCCCCC;
  background:black'>        </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#16C60C;background:black'>Starvation at      6.164  </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";color:#E74856;background:
  black'>ThreadCount:   6 </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#B4009E;background:black'>DiffSinceLast   1.528 s</span></p>
  </td>
 </tr>
</table>

To get output you need to record with the .NET ETW provider named Microsoft-Windows-DotNETRuntime with the ThreadingKeyword 0x10000.
