# -Dump CPU
Print CPU consumption of processes as total or by method. 

The data in the extracted JSON file is stored for all processes in method granularity. During extraction the default setting is to skip all 
methods with a CPU consumption < 10ms. If you want to see all methods like you do in WPA you need to add *-allCPU* during extraction.

The picture below illustrates what data is stored per method for all processes of the ETL file: 

![alt text](Images\CPUTimeFirstLast.png "CPU Time First Last")

The extracted data contains for each method in a process
- CPU usage from sampling data summed across all threads
- Wait time from Context Switch data summed across all threads
- Number of threads this method was seen
- First time this method was seen on any thread
- Last time this method was seen on any thread
- Average stack depth

To get an overview you would start with -dump CPU and a file name without further options. Since file names tend to be long you simplify
the command line by putting the file name into an environment variable in your favorite shell. We can then dump the top 5 CPU consuming processes
with the following command:

<table class=MsoNormalTable border=0 cellspacing=0 cellpadding=0
 style='border-collapse:collapse;mso-yfti-tbllook:1184;mso-padding-alt:0in 0in 0in 0in'>
 <tr style='mso-yfti-irow:0;mso-yfti-firstrow:yes;mso-yfti-lastrow:yes;
  height:84.0pt'>
  <td width=1290 valign=top style='width:967.45pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:84.0pt'>
  <p class=MsoNormal style='line-height:normal'><a name="_GoBack"></a><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>C:\&gt;set
  f=-<span class=SpellE>fd</span>
  C:\Temp\Extract\CallupAdhocWarmReadingCT_3117msDEFOR09T121SRV.20200717-124447.json<o:p></o:p></span></p>
  <p class=MsoNormal style='line-height:normal'><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>C:\&gt;ETWAnalyzer %f%
  -dump CPU -<span class=SpellE>topn</span> 5<br>
  12/7/2021 10:58:16 <span class=GramE>AM<span style='mso-spacerun:yes'> 
  </span>CallupAdhocWarmReadingCT</span>_3117msDEFOR09T121SRV.20200717-124447<br>
  <span style='mso-spacerun:yes'>        </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>981<span
  style='mso-spacerun:yes'>     </span><span class=SpellE>ms</span> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>dwm.exe(10336)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>        </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>1469<span
  style='mso-spacerun:yes'>    </span><span class=SpellE>ms</span> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>MsMpEng.exe(5044)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>        </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>2493<span
  style='mso-spacerun:yes'>    </span><span class=SpellE>ms</span> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>System(4)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>        </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>3202<span
  style='mso-spacerun:yes'>    </span><span class=SpellE>ms</span> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>ETWController.exe(15944)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>        </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>3430<span
  style='mso-spacerun:yes'>    </span><span class=SpellE>ms</span> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>SerializerTests.exe(22416)
  +- </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#3A96DD;background:black;
  mso-highlight:black'>SerializerTests</span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#3A96DD;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>  </span>-Runs 1 -N 1000000 -test combined
  -serializer <span class=SpellE>XmlSerializer</span></span></p>
  </td>
 </tr>
</table>

Did you notice the +- after the process SerializerTests? These characters signal that the process was started + and has ended - during 
the recording. The other processes did run since trace start. You can print also the process start time instead of the +- signs to analyze 
startup/shutdown performance issues with the *-ProcessFmt* option:

<table class=MsoNormalTable border=0 cellspacing=0 cellpadding=0
 style='border-collapse:collapse;mso-yfti-tbllook:1184;mso-padding-alt:0in 0in 0in 0in'>
 <tr style='mso-yfti-irow:0;mso-yfti-firstrow:yes;mso-yfti-lastrow:yes;
  height:48.55pt'>
  <td width=1327 valign=top style='width:995.4pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:48.55pt'>
  <p class=MsoNormal style='line-height:normal'><a name="_GoBack"></a><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>C:\&gt;ETWAnalyzer
  %f% -dump CPU -topn 1 -processfmt s<br>
  12/7/2021 10:58:16 AM<span style='mso-spacerun:yes'> 
  </span>CallupAdhocWarmReadingCT_3117msDEFOR09T121SRV.20200717-124447<br>
  <span style='mso-spacerun:yes'>        </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>3430<span
  style='mso-spacerun:yes'>    </span>ms </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>SerializerTests.exe(22416)
  +3.469 - 7.991</span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#3A96DD;background:black;
  mso-highlight:black'> SerializerTests<span style='mso-spacerun:yes'> 
  </span>-Runs 1 -N 1000000 -test combined -serializer XmlSerializer</span></p>
  </td>
 </tr>
</table>

The blue part after the process name is its command line. If you are using custom color schemes in your shell you can add *-nocolor* to omit coloring output.
If you are experiencing word wrapping in your shell you can add *-clip* to prevent word wrapping while omitting the additional output which did clutter
your console. This is very useful to copy the output into an Email to show the issue a colleague.

Lets look inside the top 20 methods of SerializerTests

<table class=MsoNormalTable border=0 cellspacing=0 cellpadding=0
 style='border-collapse:collapse;mso-yfti-tbllook:1184;mso-padding-alt:0in 0in 0in 0in'>
 <tr style='mso-yfti-irow:0;mso-yfti-firstrow:yes;mso-yfti-lastrow:yes;
  height:48.55pt'>
  <td width=1327 valign=top style='width:995.4pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:48.55pt'>
  <p class=MsoNormal style='line-height:normal'><a name="_GoBack"></a><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>C:\&gt;ETWAnalyzer
  %f% -dump CPU -<span class=SpellE>pn</span> <span class=SpellE>SerializerTests</span>
  -methods Serializer* -<span class=SpellE>topnmethods</span> 20<br>
  <span style='mso-spacerun:yes'>   </span><span
  style='mso-spacerun:yes'>   </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>CPU <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>     </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>Wait <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>Method<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#3A96DD;background:black;mso-highlight:black'>12/7/2021
  10:58:16 AM CallupAdhocWarmReadingCT_3117msDEFOR09T121SRV.20200717-124447<br>
  <span style='mso-spacerun:yes'>   </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.exe(22416)
  +- </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#3A96DD;background:black;
  mso-highlight:black'>SerializerTests</span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#3A96DD;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>  </span>-Runs 1 -N 1000000 -test combined
  -serializer <span class=SpellE>XmlSerializer</span><br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>       </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>28 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>       </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>10 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.Serializers.XmlSerializer`1+&lt;&gt;c[System.__Canon].&lt;.ctor&gt;b__0_0<br>
  <span style='mso-spacerun:yes'>       </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>40 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>819 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.TestBase`2[System.__Canon,System.__Canon].ReadMemoryStreamFromDisk<br>
  <span style='mso-spacerun:yes'>       </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>58 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>251 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.TestBase`2[System.__Canon,System.__Canon].SaveMemoryStreamToDisk<br>
  <span style='mso-spacerun:yes'>      </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>211 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>       </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>63 <span class=SpellE>ms</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'> <span
  class=SpellE>SerializerTests.Program</span>.&lt;Data&gt;b__29_0<br>
  <span style='mso-spacerun:yes'>      </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>218 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>       </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>64 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>SerializerTests.Program.Data</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>      </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>218 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>       </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>64 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.TestBase`2[System.__Canon,System.__Canon].get_TestData<br>
  <span style='mso-spacerun:yes'>      </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>720 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>   </span><span style='mso-spacerun:yes'>    </span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>11 <span
  class=SpellE>ms</span> </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>SerializerTests.TestBase`2[System.__Canon,System.__Canon].&lt;TestSerialize&gt;b__37_0<br>
  <span style='mso-spacerun:yes'>      </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>720 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>      </span>11 <span class=SpellE>ms</span> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.Serializers.XmlSerializer`1[System.__Canon].Serialize<br>
  <span style='mso-spacerun:yes'>      </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>720 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>       </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>11 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.TestBase`2[System.__Canon,System.__Canon].TestSerializeOnly<br>
  <span style='mso-spacerun:yes'>     </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>1055 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>327 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.TestBase`2[System.__Canon,System.__Canon].TestSerialize<br>
  <span style='mso-spacerun:yes'>     </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>1729 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>       </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>79 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.Serializers.XmlSerializer`1[System.__Canon].Deserialize<br>
  <span style='mso-spacerun:yes'>     </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>1731 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>       </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>79 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.TestBase`2+&lt;&gt;c__DisplayClass39_0[System.__Canon,System.__Canon].&lt;TestDeserialize&gt;b__0<br>
  <span style='mso-spacerun:yes'>     </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>1731 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>       </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>79 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.TestBase`2[System.__Canon,System.__Canon].TestDeserializeOnlyAndTouch<br>
  <span style='mso-spacerun:yes'>    </span><span
  style='mso-spacerun:yes'> </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>1835 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>899 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.TestBase`2[System.__Canon,System.__Canon].TestDeserialize<br>
  <span style='mso-spacerun:yes'>     </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>2571 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>      </span>91 <span class=SpellE>ms</span> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.TestBase`2[<span
  class=SpellE>System.__Canon,System.__Canon</span>].Test<br>
  <span style='mso-spacerun:yes'>     </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>2898 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>     </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>1227 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>SerializerTests.Test_O_N_Behavior.TestCombined</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'> </span><span style='mso-spacerun:yes'>   
  </span></span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#16C60C;background:black;
  mso-highlight:black'>2899 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>     </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>1227 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>SerializerTests.Program.Combined</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>     </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>2954 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>     </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>1228 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>SerializerTests.Program.Run</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>     </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>2962 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>     </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>1228 <span class=SpellE>ms</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'> <span
  class=SpellE>SerializerTests.Program.Main</span><br>
  <span style='mso-spacerun:yes'>     </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>3013 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>     </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>1289 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.exe</span></p>
  </td>
 </tr>
</table>

The dump command supports many additional options to add context data depending on what you are after:

![alt text](Images\DumpCPU_All.png "Dump CPU All")

The CPU consumption and wait time of a method in a process are summed over all threads. This is the main reason 
why the extracted Json data is so much smaller. For performance regression issues this data is in most cases sufficient
to track changes down to method level.

For each method the number of threads on which it was seen is recorded and can be added via the *-threadcount* option.

## First And Last Method Duration
With *-FirstLastDuration [timefmt] [timefmt]* or *-fld* you can display when a method was seen first and last in a trace file. 
See first picture how this relates to the data seen in WPA. For methods ETWAnalyzer keeps the aggregated data
across all threads. The First time is therefore the time a method was seen first on any thread. The method Last time
is the time a method was seen last in CPU sampling or Context Switch data. The difference Last-First is called FirstLastDuration.
If a method was executed in a process once on one thread it should closely match the method runtime. 
The same is true if a method was executed once on many threads e.g. DownloadAsync and it did produce data when it did start and finish you 
can get a good approximation how long the download was running. 

That aggregated data can give good insights, but you should be have checked in WPA if your assumptions about when this method is called
are true. The First/Last method timepoints are not useful when a method is called many times during the trace. E.g. a Render call 
will be called for many frames, but if the animation was paused in between then no render calls were made the FirstLastDuration would be misleading.
But if the method is called all the time it would simply be the time from trace start until trace end. Another source of error is if the method
is barely above your CPU sample rate (by default ETW samples with 1kHz = 1000 Samples/s). You can miss the First/Last method time and get 
unreasonable results. 

You can configure how the method First/Last time is printed. That allows you to correlate the
data gathered with ETW with log files to check if first invocation of a method e.g. DeleteDatabase relates with the errors seen in the 
logs shortly after the method was seen first in ETW. 

That query would be 

>*-Dump CPU -FirstLastDuration Local -method \*DeleteDataBase\**

The time format values are described in [-Dump Process](DumpProcessCommand.md).

## Stacktags

Additionally stacktag data which is supported by WPA in the form of a stacktag file is also extracted. During extraction the stacktag files
- Configuration/default.stacktags
- Configuration/Special.stacktags
are used.

Stacktags are a powerful way to gain insights into a system, because you can rename ("tag") specific methods and give them a descriptive name. 
If you group by Stack Tags in WPA you do not need to unfold the stack trace to the deepest methods just to find e.g. the virus scanner 
interfering again with your test. 


![alt text](Images\StackTags.png "StackTags")


In the trace above we e.g. see that the Windows Defender stacktag is a few ms of CPU but in CPU Usage (Precise) which is the Context Switch data
view we see that we were blocked over 819ms summed across all threads. A nice property of stacktags is that they add up. The way this works 
can be explained how profiling works. Whenever a CPU sample interrupt is fired with 1000 sample interrupts/s for all running threads a stacktrace is
taken. If the process did fully utilize a CPU then we have got 1000 stack traces in 1s. If the process was blocked then we would get 0 stack 
traces. 

| Stack1| Stack2 | Stack3 | Stack4 | Total CPU in ms
| ----------- | ----------- | -----------| ----------- |
| Main() | Main()  | Main() | Main() | 4 |
| F1() | F1() | F1() | F1() | 4 |
| A()|  A()| B() | B()| 4|
| C()| C()| D()| D()| 4|
| F()| F()| | | 2|
| F2()| | | | 1|

Summation Stacktrace

| Level 0| Level 1 | Level 3 | CPU in ms
| ----------- | ----------- |  ----------- |
| Main() |  | | 4 |
| F1() | | |  4 |
| | ->A()|  | 2|
| |  ->C()| |  2|
| | ->F()|  | 2|
| | | ->F2()| 1|
| | ->B() | | 2|
| | ->D() | |  2|

When you give method D a stacktag and method A then WPA assigns for the complete stack just one stacktag, where the deepest method 
stacktag wins. As a consequence you cannot count CPU twice when multiple stacktags compete for a given stacktrace.
This is a big advantage when summing up stacktags. We get always meaningful total numbers and never count things twice as it is the 
case with normal methods, because we store only method inclusive CPU and Wait times. 

We can with properly declared stacktags nicely tell how much impact the Virus scanner had on our test case

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
  %f% -dump CPU<span style='mso-spacerun:yes'>  </span>-<span class=SpellE>stacktags</span>
  *virus* -<span class=SpellE>minmaxwaitms</span> 100 -<span class=SpellE>showtotal</span>
  method<br>
  <span style='mso-spacerun:yes'>      </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>CPU <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>     </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>Wait <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>Method<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#3A96DD;background:black;mso-highlight:black'>12/7/2021
  10:58:16 AM CallupAdhocWarmReadingCT_3117msDEFOR09T121SRV.20200717-124447 </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>CPU
  1363 <span class=SpellE>ms</span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'> Wait </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>126780 <span
  class=SpellE>ms</span> </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#B4009E;background:black;
  mso-highlight:black'>Total 128143 <span class=SpellE>ms</span><br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#3A96DD;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>   </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.exe(22416)
  +- </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#16C60C;background:black;
  mso-highlight:black'>CPU 5 <span class=SpellE>ms</span> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'>Wait:
  820 <span class=SpellE>ms</span> </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#B4009E;background:black;mso-highlight:black'>Total: 825 <span
  class=SpellE>ms</span> </span><span class=SpellE><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#3A96DD;background:black;mso-highlight:black'>SerializerTests</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#3A96DD;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>  </span>-Runs 1 -N 1000000 -test combined
  -serializer <span class=SpellE>XmlSerializer</span><br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>        </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>5 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>820 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>Antivirus
  - Windows Defender<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#3A96DD;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>   </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>System(4) </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>CPU 14 <span
  class=SpellE>ms</span> </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#F9F1A5;background:black;
  mso-highlight:black'>Wait: 113691 <span class=SpellE>ms</span> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#B4009E;background:black;mso-highlight:black'>Total:
  113705 <span class=SpellE>ms</span><br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>       </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>14 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>   </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>113691 <span
  class=SpellE>ms</span> </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>Antivirus - Windows Defender<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#3A96DD;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>   </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>MsMpEng.exe(5044) </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#16C60C;background:black;mso-highlight:black'>CPU
  1344 <span class=SpellE>ms</span> </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>Wait: 12269 <span
  class=SpellE>ms</span> </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#B4009E;background:black;
  mso-highlight:black'>Total: 13613 <span class=SpellE>ms</span><br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>     </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>1344 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>   </span>12269 <span class=SpellE>ms</span> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>Antivirus
  - Windows Defender<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#B4009E;background:black;mso-highlight:black'>Total
  128,143 <span class=SpellE>ms</span> </span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>CPU 1,363 <span
  class=SpellE>ms</span> </span><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#F9F1A5;background:black;
  mso-highlight:black'>Wait 126,780 <span class=SpellE>ms</span></span></p>
  </td>
 </tr>
</table>

We find that our SerializerTest.exe was blocked 820 ms by Windows Defender. 
Because we know that this test runs single threaded we have therefore a true test 
slowdown and we can trust this number. 

To identify the region where Defender did intercept our calls we can use WPA, or we can interleave method names and Stacktags in one common output. 

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
  %f% -dump CPU<span style='mso-spacerun:yes'>  </span>-<span class=SpellE>stacktags</span>
  *virus* -<span class=SpellE>minmaxwaitms</span> 500-900 -methods * -<span
  class=SpellE>pn</span> <span class=SpellE>SerializerTests</span> -<span
  class=SpellE>SortBy</span> Wait<br>
  <span style='mso-spacerun:yes'>      </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>CPU <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>     </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>Wait <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>Method<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#3A96DD;background:black;mso-highlight:black'>12/7/2021
  10:58:16 AM CallupAdhocWarmReadingCT_3117msDEFOR09T121SRV.20200717-124447<br>
  <span style='mso-spacerun:yes'>   </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.exe(22416)
  +- </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#3A96DD;background:black;
  mso-highlight:black'>SerializerTests</span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#3A96DD;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>  </span>-Runs 1 -N 1000000 -test combined
  -serializer <span class=SpellE>XmlSerializer</span><br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>        </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>0 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>     </span>768 <span class=SpellE>ms</span> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.TestBase`2[System.__Canon,System.__Canon].SerializeDuration<br>
  <span style='mso-spacerun:yes'>       </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>15 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>819 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>IopCreateFile</span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>       </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>12 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>819 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>NtCreateFile</span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>       </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>12 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>819 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>NtCreateFile</span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>        </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>4 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>819 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>WdFilter.sys<br>
  <span style='mso-spacerun:yes'>        </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>5 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>819 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>FltpPerformPostCallbacksWorker</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>       </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>12 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>819 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>CreateFileInternal</span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>       </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>12 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>819 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>CreateFileW</span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>        </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>3 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>819 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>FltpPassThroughCompletionWorker</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>       </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>40 <span class=SpellE>ms</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>819 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.TestBase`2[System.__Canon,System.__Canon].ReadMemoryStreamFromDisk<br>
  <span style='mso-spacerun:yes'>        </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>0 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>819 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>FltCancellableWaitForSingleObject</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>        </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>0 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>819 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>FsRtlCancellableWaitForSingleObject</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>        </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>0 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>819 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>FsRtlCancellableWaitForMultipleObjects</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>        </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>5 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#F9F1A5;background:black;mso-highlight:black'><span
  style='mso-spacerun:yes'>     </span>820 <span class=SpellE>ms</span> </span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>Antivirus
  - Windows Defender<br>
  <span style='mso-spacerun:yes'>       </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>29 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>830 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>ObOpenObjectByNameEx</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>       </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>29 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>830 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>ObpLookupObjectName</span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>       </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>25 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>830 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>IopParseDevice</span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>       </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>17 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>830 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>IoCallDriverWithTracing</span></span><span
  style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>       </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>17 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>830 <span class=SpellE>ms</span>
  </span><span class=SpellE><span style='font-size:12.0pt;font-family:"Lucida Console";
  mso-fareast-font-family:"Times New Roman";color:#CCCCCC;background:black;
  mso-highlight:black'>FltpCreate</span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#CCCCCC;background:black;mso-highlight:black'><br>
  <span style='mso-spacerun:yes'>     </span></span><span style='font-size:
  12.0pt;font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#16C60C;background:black;mso-highlight:black'>1835 <span class=SpellE>ms</span><span
  style='mso-spacerun:yes'>      </span></span><span style='font-size:12.0pt;
  font-family:"Lucida Console";mso-fareast-font-family:"Times New Roman";
  color:#F9F1A5;background:black;mso-highlight:black'>899 <span class=SpellE>ms</span>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";mso-fareast-font-family:
  "Times New Roman";color:#CCCCCC;background:black;mso-highlight:black'>SerializerTests.TestBase`2[System.__Canon,System.__Canon].TestDeserialize</span></p>
  </td>
 </tr>
</table>

We dump all methods which have a wait time between 500-900ms, then we sort by wait time ascending. 
By comparing the numbers it looks like the CreateFile calls are scanned by Defender which is contributing
to the pretty high wait times in the CreateFile method calls. Microsoft does not supply pdbs to Windows Defender,
but there is a list of pretty much all Filter Driver altitudes published by Microsoft which is a collection
of the Who’s Who of AV device drivers. ETWAnalyzer can use that to our advantage and print module infos for well known
driver names:

![alt text](Images/WindowsDefender_SMI.png)

WdFilter.sys is one the Windows Filter drivers used by Defender to intercept central operations such as process creation, file 
access and other things. The yellow string Microsoft FSFILTER... is from the well known list of AV vendors. This makes it much 
easier to measure the impact of AV solutions. 

