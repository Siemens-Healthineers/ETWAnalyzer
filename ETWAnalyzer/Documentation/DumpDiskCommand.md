# -Dump Disk

Dump Disk IO metrics where the hard disk did actually some work. Unlike FileIO DiskIO normally contains much less data 
because if you read the same file again, the second time, the read will be served from the file system cache. If your
disk is some tiered storage like a SAN or a RAID then you still might get to see some cache effects due to e.g. RAID controller
cache effects.
DiskIO is not easy to attribute to a specific process, because many optimizations are in place. If you open a file and then read
a few KB the OS will prefetch data by doing some read ahead caching which can result that an application is not paying of the 
IO because it did parse the data so slow that the OS had plenty of time to prefetch data. See [this article of my old blog](https://web.archive.org/web/20141230191825/http://geekswithblogs.net/akraus1/archive/2014/12/14/160652.aspx).

![WPA_DiskIO](Images/WPA_DiskIO.png)

ETWAnalyzer can show aggregates per directory which is configurable via -DirLevel, Read/Write throughput for one, or if *-Merge* is 
used a collection of files to check e.g. average throughput over an extended test run.

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
  %f% -dump disk<br>
  12/7/2021 10:58:16 AM
  CallupAdhocWarmReadingCT_3117msDEFOR09T121SRV.20200717-124447<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>Read<span
  style='mso-spacerun:yes'>                                </span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>Write<span
  style='mso-spacerun:yes'>                               </span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#61D6D6;background:black;mso-highlight:black'>Flush<span
  style='mso-spacerun:yes'>        </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>Directory or File if -<span
  class=SpellE>dirLevel</span> 100 is used<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>    </span>0.00016 s<span
  style='mso-spacerun:yes'>       </span>0 MB<span style='mso-spacerun:yes'>  
  </span>97 MB/s</span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'> </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#F9F1A5;background:black;
  mso-highlight:black'>w<span style='mso-spacerun:yes'>    </span>0.00004
  s<span style='mso-spacerun:yes'>       </span>0 MB<span
  style='mso-spacerun:yes'>   </span>88 MB/s </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#61D6D6;background:black;mso-highlight:black'>f<span
  style='mso-spacerun:yes'>    </span>0.000 s </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>Unknown (0x0)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>    </span>0.00000 s<span
  style='mso-spacerun:yes'>       </span>0 MB<span style='mso-spacerun:yes'>   
  </span>0 MB/s </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#F9F1A5;background:black;
  mso-highlight:black'>w<span style='mso-spacerun:yes'>    </span>0.00000
  s<span style='mso-spacerun:yes'>       </span>0 MB<span
  style='mso-spacerun:yes'>    </span>0 MB/s </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#61D6D6;background:black;mso-highlight:black'>f<span
  style='mso-spacerun:yes'>    </span>0.034 s </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>Id0 Flush<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>    </span>0.00000 s<span
  style='mso-spacerun:yes'>       </span>0 MB<span style='mso-spacerun:yes'>   
  </span>0 MB/s </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#F9F1A5;background:black;
  mso-highlight:black'>w<span style='mso-spacerun:yes'>    </span>0.00000
  s<span style='mso-spacerun:yes'>       </span>0 MB<span
  style='mso-spacerun:yes'>    </span>0 MB/s </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#61D6D6;background:black;mso-highlight:black'>f<span
  style='mso-spacerun:yes'>    </span>0.069 s </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>Id1 Flush<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>    </span>0.25641 s<span
  style='mso-spacerun:yes'>      </span>18 MB<span style='mso-spacerun:yes'>  
  </span>68 MB/s </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#F9F1A5;background:black;
  mso-highlight:black'>w<span style='mso-spacerun:yes'>    </span>0.08024
  s<span style='mso-spacerun:yes'>      </span>28 MB<span
  style='mso-spacerun:yes'>  </span>344 MB/s </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#61D6D6;background:black;mso-highlight:black'>f<span
  style='mso-spacerun:yes'>    </span>0.000 s </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>C:\<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>    </span>0.38667 s<span
  style='mso-spacerun:yes'>       </span>3 MB<span style='mso-spacerun:yes'>   
  </span>7 MB/s </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#F9F1A5;background:black;
  mso-highlight:black'>w<span style='mso-spacerun:yes'>    </span>0.30650
  s<span style='mso-spacerun:yes'>      </span>62 MB<span
  style='mso-spacerun:yes'>  </span>201 MB/s </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#61D6D6;background:black;mso-highlight:black'>f<span
  style='mso-spacerun:yes'>    </span>0.000 s </span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>D:\</span></p>
  </td>
 </tr>
</table>

You can also get per file metrics by using *-DirLevel 100*. If the output does not suit your needs you can export the data
to a CSV file and analyze it further with Excel or R.

