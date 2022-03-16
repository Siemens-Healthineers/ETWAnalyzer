# -Dump Memory
- Show total machine memory diff from trace start until trace end. 
<table class=MsoNormalTable border=0 cellspacing=0 cellpadding=0 width=1871
 style='width:1403.05pt;border-collapse:collapse;mso-yfti-tbllook:1184;
 mso-padding-alt:0in 0in 0in 0in'>
 <tr style='mso-yfti-irow:0;mso-yfti-firstrow:yes;mso-yfti-lastrow:yes;
  height:49.7pt'>
  <td width=1871 valign=top style='width:1403.05pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:49.7pt'>
  <p class=MsoNormal style='line-height:normal'><a name="_GoBack"></a><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>C:\&gt;ETWAnalyzer
  %f% -dump memory -<span class=SpellE>totalmemory</span><br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#3A96DD;background:black;mso-highlight:black'>2021-12-07
  10:58:31.270</span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'> Committed:<span style='mso-spacerun:yes'>   </span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#3A96DD;background:black;mso-highlight:black'>14001 </span><span
  class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>CommitDiff</span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>:<span
  style='mso-spacerun:yes'>     </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>-18 MB </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>Active:<span
  style='mso-spacerun:yes'>    </span>9531 MB <span class=SpellE>ActiveDiff</span>:<span
  style='mso-spacerun:yes'>    </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>-130 MB </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>Physical
  Memory: 17120 MB
  CallupAdhocWarmReadingCT_3117msDEFOR09T121SRV.20200717-124447</span></p>
  </td>
 </tr>
</table>


- Show per process memory diff from trace start until trace end.

<table class=MsoNormalTable border=0 cellspacing=0 cellpadding=0 width=1871
 style='width:1403.05pt;border-collapse:collapse;mso-yfti-tbllook:1184;
 mso-padding-alt:0in 0in 0in 0in'>
 <tr style='mso-yfti-irow:0;mso-yfti-firstrow:yes;mso-yfti-lastrow:yes;
  height:49.7pt'>
  <td width=1871 valign=top style='width:1403.05pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:49.7pt'>
  <p class=MsoNormal style='line-height:normal'><a name="_GoBack"></a><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>C:\&gt;ETWAnalyzer
  %f% -dump memory -<span class=SpellE>topn</span> 5 -clip<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#61D6D6;background:black;mso-highlight:black'>CallupAdhocWarmReadingCT_3117msDEFOR09T121SRV.20200717-124447<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#3A96DD;background:black;mso-highlight:black'>2021-12-07
  10:58:31.270 Diff:<span style='mso-spacerun:yes'>    </span>0 </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#C19C00;background:black;mso-highlight:black'>Commit<span
  style='mso-spacerun:yes'>  </span>168 MiB <span class=SpellE>WorkingSet</span><span
  style='mso-spacerun:yes'>  </span>122 MiB </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#3A96DD;background:black;mso-highlight:black'>Shared Commit:<span
  style='mso-spacerun:yes'>   </span>40 MiB<span style='mso-spacerun:yes'> 
  </span></span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#B4009E;background:black;
  mso-highlight:black'>GoogleDriveFS.exe(11340)<span style='mso-spacerun:yes'> 
  </span>--...<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#3A96DD;background:black;mso-highlight:black'>2021-12-07
  10:58:31.270 Diff:<span style='mso-spacerun:yes'>    </span>0</span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#C19C00;background:black;mso-highlight:black'>Commit<span
  style='mso-spacerun:yes'>  </span>231 MiB <span class=SpellE>WorkingSet</span><span
  style='mso-spacerun:yes'>  </span>296 MiB </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#3A96DD;background:black;mso-highlight:black'>Shared Commit:<span
  style='mso-spacerun:yes'>   </span>12 MiB<span style='mso-spacerun:yes'> 
  </span></span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#B4009E;background:black;
  mso-highlight:black'>firefox.exe(20408)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#3A96DD;background:black;mso-highlight:black'>2021-12-07
  10:58:31.270 Diff:<span style='mso-spacerun:yes'>    </span>0 </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#C19C00;background:black;mso-highlight:black'>Commit<span
  style='mso-spacerun:yes'>  </span>241 MiB <span class=SpellE>WorkingSet</span><span
  style='mso-spacerun:yes'>  </span>140 MiB Shared Commit:<span
  style='mso-spacerun:yes'>  </span>162 MiB<span style='mso-spacerun:yes'> 
  </span></span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#B4009E;background:black;
  mso-highlight:black'>firefox.exe(23120)<span style='mso-spacerun:yes'> 
  </span>-content...<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#3A96DD;background:black;mso-highlight:black'>2021-12-07
  10:58:31.270 Diff:<span style='mso-spacerun:yes'>    </span>0 </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#E74856;background:black;mso-highlight:black'>Commit<span
  style='mso-spacerun:yes'>  </span>302 MiB </span><span class=SpellE><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#C19C00;background:black;mso-highlight:black'>WorkingSet</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#C19C00;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>  </span>104 MiB</span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#E74856;background:black;mso-highlight:black'>Shared
  Commit:<span style='mso-spacerun:yes'>  </span>431 MiB<span
  style='mso-spacerun:yes'>  </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#B4009E;background:black;mso-highlight:black'>dwm.exe(10336)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#3A96DD;background:black;mso-highlight:black'>2021-12-07
  10:58:31.270 Diff:<span style='mso-spacerun:yes'>    </span>0</span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>Commit<span
  style='mso-spacerun:yes'>  </span>529 MiB </span><span class=SpellE><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#E74856;background:black;mso-highlight:black'>WorkingSet</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#E74856;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>  </span>406 MiB </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#3A96DD;background:black;mso-highlight:black'>Shared Commit:<span
  style='mso-spacerun:yes'>    </span>2 MiB<span style='mso-spacerun:yes'> 
  </span></span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#B4009E;background:black;
  mso-highlight:black'>MsMpEng.exe(5044)</span></p>
  </td>
 </tr>
</table>

This command is mainly used to check if the machine was tight on memory, or if processes did leak memory during
long running tests. ETWAnalyzer can calculate the total memory growth of processes over a collection of
ETL files. 

One little known memory type in ETW lingo is Shared Commit. This is basically the sum of all memory mapped files
a process has access to. This includes all file mapping objects, and page file allocated memory file mappings.
The value remains large even if you have not mapped any file mapping objects. This is the size of all file
mapping objects the process has access to which are mostly created with a call to [CreateFileMapping](https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createfilemappinga). If multiple processes have mapped the 
same file then each process gets the mapped file size as Shared Commit.

![](Images/WPA_Memory.png)