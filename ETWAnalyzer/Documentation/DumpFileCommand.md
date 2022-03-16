# -Dump File
Show all accessed files by a process, regardless if the read/write request was served by a cache. This corresponds from a user mode
point of view roughly to calls to 
- [CreateFile](https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilea)
- [ReadFile](https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfile)
- [WriteFile](https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-writefile)
- [FindFirstFile](https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-findfirstfilew)
- [DeleteFile](https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-deletefilew)
- [SetSecurityInfo](https://docs.microsoft.com/en-us/windows/win32/api/aclapi/nf-aclapi-setsecurityinfo)
- [MoveFile](https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-movefilew)
- [CloseHandle](https://docs.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-closehandle)
 
which are done for file or directory objects. Network share files are also covered.


<table class=MsoNormalTable border=0 cellspacing=0 cellpadding=0 width=2081
 style='width:1560.95pt;border-collapse:collapse;mso-yfti-tbllook:1184;
 mso-padding-alt:0in 0in 0in 0in'>
 <tr style='mso-yfti-irow:0;mso-yfti-firstrow:yes;mso-yfti-lastrow:yes;
  height:28.7pt'>
  <td width=2081 valign=top style='width:1560.95pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:28.7pt'>
  <p class=MsoNormal style='line-height:normal'><a name="_GoBack"></a><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>C:\&gt;ETWAnalyzer
  %f% -dump file<br>
  12/7/2021 10:58:16 AM
  CallupAdhocWarmReadingCT_3117msDEFOR09T121SRV.20200717-124447<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>Read
  (Size, Duration, Count)</span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#F9F1A5;background:black;
  mso-highlight:black'><span style='mso-spacerun:yes'>        </span>Write
  (Size, Duration, Count)<span style='mso-spacerun:yes'>         </span></span><span
  class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#61D6D6;background:black;
  mso-highlight:black'>Open+Close</span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#61D6D6;background:black;mso-highlight:black'> Duration, Open,
  Close<span style='mso-spacerun:yes'>        </span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>Directory
  or File if -<span class=SpellE>dirLevel</span> 100 is used<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>            </span>0 KB<span
  style='mso-spacerun:yes'>    </span>0.00000 s<span
  style='mso-spacerun:yes'>    </span>0 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>w<span
  style='mso-spacerun:yes'>            </span>0 KB<span
  style='mso-spacerun:yes'>    </span>0.00000 s<span
  style='mso-spacerun:yes'>    </span>0<span style='mso-spacerun:yes'>  
  </span></span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#61D6D6;background:black;
  mso-highlight:black'>O+C<span style='mso-spacerun:yes'>    </span>0.00093 s
  Open:<span style='mso-spacerun:yes'>    </span>1 Close:<span
  style='mso-spacerun:yes'>    </span>1 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>G:<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>            </span>0 KB<span
  style='mso-spacerun:yes'>    </span>0.00000 s<span
  style='mso-spacerun:yes'>    </span>0 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>w<span
  style='mso-spacerun:yes'>            </span>0 KB<span
  style='mso-spacerun:yes'>    </span>0.00000 s<span
  style='mso-spacerun:yes'>    </span>0<span style='mso-spacerun:yes'>  
  </span></span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#61D6D6;background:black;
  mso-highlight:black'>O+C<span style='mso-spacerun:yes'>    </span>0.00139 s
  Open:<span style='mso-spacerun:yes'>   </span>80 Close:<span
  style='mso-spacerun:yes'>   </span>80 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>C:<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>           </span><span
  style='mso-spacerun:yes'> </span>0 KB<span style='mso-spacerun:yes'>   
  </span>0.00000 s<span style='mso-spacerun:yes'>    </span>0 </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>w<span
  style='mso-spacerun:yes'>            </span>0 KB<span
  style='mso-spacerun:yes'>    </span>0.00000 s<span
  style='mso-spacerun:yes'>    </span>0<span style='mso-spacerun:yes'>  
  </span></span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#61D6D6;background:black;
  mso-highlight:black'>O+C<span style='mso-spacerun:yes'>    </span>0.00006 s
  Open:<span style='mso-spacerun:yes'>    </span>4 Close:<span
  style='mso-spacerun:yes'>    </span>0<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>            </span>0 KB<span
  style='mso-spacerun:yes'>    </span>0.00000 s<span
  style='mso-spacerun:yes'>    </span>0 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>w<span
  style='mso-spacerun:yes'>            </span>0 KB<span
  style='mso-spacerun:yes'>    </span>0.00000 s<span
  style='mso-spacerun:yes'>    </span>0<span style='mso-spacerun:yes'>  
  </span></span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#61D6D6;background:black;
  mso-highlight:black'>O+C<span style='mso-spacerun:yes'>    </span>0.00030 s
  Open:<span style='mso-spacerun:yes'>   </span>24 Close:<span
  style='mso-spacerun:yes'>   </span>26 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>\<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>            </span>0 KB<span
  style='mso-spacerun:yes'>    </span>0.00000 s<span
  style='mso-spacerun:yes'>    </span>0</span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>w<span
  style='mso-spacerun:yes'>            </span>0 KB<span
  style='mso-spacerun:yes'>    </span>0.00000 s<span
  style='mso-spacerun:yes'>    </span>0<span style='mso-spacerun:yes'>  
  </span></span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#61D6D6;background:black;
  mso-highlight:black'>O+C<span style='mso-spacerun:yes'>    </span>0.00005 s
  Open:<span style='mso-spacerun:yes'>    </span>4 Close:<span
  style='mso-spacerun:yes'>    </span>4 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>D:<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>            </span>0 KB<span
  style='mso-spacerun:yes'>    </span>0.00000 s<span
  style='mso-spacerun:yes'>    </span>0 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>w<span
  style='mso-spacerun:yes'>            </span>0 KB<span
  style='mso-spacerun:yes'>    </span>0.00000 s<span
  style='mso-spacerun:yes'>    </span>0<span style='mso-spacerun:yes'>  
  </span></span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#61D6D6;background:black;
  mso-highlight:black'>O+C<span style='mso-spacerun:yes'>    </span>0.00000 s
  Open:<span style='mso-spacerun:yes'>    </span>1 Close:<span
  style='mso-spacerun:yes'>    </span>0 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>E:<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>       </span>44,494 KB<span
  style='mso-spacerun:yes'>    </span>0.36472 s 11407 </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>w<span
  style='mso-spacerun:yes'>       </span>53,905 KB<span
  style='mso-spacerun:yes'>    </span>0.17390 s 2244<span
  style='mso-spacerun:yes'>   </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#61D6D6;background:black;mso-highlight:black'>O+C<span
  style='mso-spacerun:yes'>    </span>0.17180 s Open: 8234 Close: 6042 </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>C:\<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>       </span>67,451 KB<span
  style='mso-spacerun:yes'>    </span>0.44117 s<span style='mso-spacerun:yes'> 
  </span>214 </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#F9F1A5;background:black;
  mso-highlight:black'>w<span style='mso-spacerun:yes'>      </span>127,589
  KB<span style='mso-spacerun:yes'>    </span>0.59930 s<span
  style='mso-spacerun:yes'>  </span>221<span style='mso-spacerun:yes'>  
  </span></span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#61D6D6;background:black;
  mso-highlight:black'>O+C<span style='mso-spacerun:yes'>    </span>0.02775 s
  Open:<span style='mso-spacerun:yes'>  </span>527 Close:<span
  style='mso-spacerun:yes'>  </span>502 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>D:\<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#E74856;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>      </span>111,945 KB<span
  style='mso-spacerun:yes'>    </span>0.80588 s<span style='mso-spacerun:yes'> 
  </span>11621</span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'> </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#B4009E;background:black;
  mso-highlight:black'>w<span style='mso-spacerun:yes'>  </span><span
  style='mso-spacerun:yes'>    </span>181,494 KB<span
  style='mso-spacerun:yes'>    </span>0.77320 s<span
  style='mso-spacerun:yes'>   </span>2465<span style='mso-spacerun:yes'>  
  </span></span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#F9F1A5;background:black;
  mso-highlight:black'>O+C<span style='mso-spacerun:yes'>    </span>0.20228 s
  Open:<span style='mso-spacerun:yes'>   </span>8875 Close:<span
  style='mso-spacerun:yes'>   </span>6655 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>File Total with 2124
  accessed file. Process Count: 107</span></p>
  </td>
 </tr>
</table>

Details show more data such as the maximum file pointer which can help to estimate who big the file was, and the number of calls
to set file security attributes and file delete and rename operations. 

**Note: The FileIO duration can be hard to interpret if async/overlapped IO is used. The duration will then also include
the queuing time and not the actual disk IO duration.**
![](Images/WPA_FileIO.png)

The data shown by WPA for the C:\ drive must match also with the extracted data:

![](Images/DumpFile_Details.png)

At API level of TraceProcessing there are two sizes exposed. 
1. The buffer size passed to Read/WriteFile
2. The number of bytes returned by ReadFile or number of bytes of buffer written

ETWAnalyzer uses the passed buffer size to these APIs. That can help to find issues with too small buffer
sizes if performance issues are found. Reading from files with too small buffers is a common source of performance bottlenecks.

To see e.g. how many BookShelf files our SerializerTest did access we can use the following query

<table class=MsoNormalTable border=0 cellspacing=0 cellpadding=0 width=2081
 style='width:1560.95pt;border-collapse:collapse;mso-yfti-tbllook:1184;
 mso-padding-alt:0in 0in 0in 0in'>
 <tr style='mso-yfti-irow:0;mso-yfti-firstrow:yes;mso-yfti-lastrow:yes;
  height:28.7pt'>
  <td width=2081 valign=top style='width:1560.95pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:28.7pt'>
  <p class=MsoNormal style='line-height:normal'><a name="_GoBack"></a><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>C:\&gt;ETWAnalyzer
  %f% -dump file -<span class=SpellE>perprocess</span> -<span class=SpellE>pn</span>
  <span class=SpellE>serializertests</span> -<span class=SpellE>dirlevel</span>
  100 -filename *bookshelf* -<span class=SpellE>rfn</span> -clip<br>
  12/7/2021 10:58:16 AM
  CallupAdhocWarmReadingCT_3117msDEFOR09T121SRV.20200717-124447<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>Read
  (Size, Duration, Count)</span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#F9F1A5;background:black;
  mso-highlight:black'><span style='mso-spacerun:yes'>        </span>Write
  (Size, Duration, Count)</span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#61D6D6;background:black;
  mso-highlight:black'><span style='mso-spacerun:yes'>         </span><span
  class=SpellE>Open+Close</span> Duration, Open, Close</span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>        </span>Directory or File if -<span
  class=SpellE>dirLevel</span> 100 i...<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>SerializerTests.exe(22416)
  +-<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>       </span>64,236 KB<span
  style='mso-spacerun:yes'>    </span>0.01788 s<span
  style='mso-spacerun:yes'>    </span>1</span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>w<span
  style='mso-spacerun:yes'>      </span>112,092 KB<span
  style='mso-spacerun:yes'>    </span>0.54470 s<span style='mso-spacerun:yes'> 
  </span>188<span style='mso-spacerun:yes'>  </span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#61D6D6;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'> </span>O+C<span style='mso-spacerun:yes'>   
  </span>0.00555 s Open:<span style='mso-spacerun:yes'>    </span>2 Close:<span
  style='mso-spacerun:yes'>    </span>1 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>Serialized_XmlSerializer_BookShelf_1...<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#E74856;background:black;mso-highlight:black'>r<span
  style='mso-spacerun:yes'>       </span>64,236 KB<span
  style='mso-spacerun:yes'>    </span>0.01788 s<span
  style='mso-spacerun:yes'>      </span>1 </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#B4009E;background:black;mso-highlight:black'>w<span
  style='mso-spacerun:yes'>      </span>112,092 KB<span
  style='mso-spacerun:yes'>    </span>0.54470 s<span
  style='mso-spacerun:yes'>    </span>188<span style='mso-spacerun:yes'>  
  </span></span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#F9F1A5;background:black;
  mso-highlight:black'>O+C<span style='mso-spacerun:yes'>    </span>0.00555 s
  Open: <span style='mso-spacerun:yes'>     </span>2 Close:<span
  style='mso-spacerun:yes'>      </span>1</span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'> File Total with 1
  accessed f...</span></p>
  </td>
 </tr>
</table>

To cope with limited console width *-rfn = -ReverseFileName* can be used to get most data into the limited console
window without word wrapping.
If during regression tests e.g. the read file size changes, or the number of accessed files,
or the file is read multiple times, this data will show what has changed. You can always
export the data into a CSV file to track things further in one or a collection of analyzed files.
For more options please refer to the command line help. 

