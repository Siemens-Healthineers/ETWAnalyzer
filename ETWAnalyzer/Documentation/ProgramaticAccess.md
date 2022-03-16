# Programatic Access to Extracted Data

The IETWExtract interface gives you access to all deserialized data of an extracted Json file in a cohesive object
oriented object model. 
Since ETWAnalyzer was built initially for mass data analysis of automated tests you need to start with a TestRunData instance
which gets as input a directory or a Json file. 
From there you can then query the TestRuns or all contained files. 
Each TestDataFile has if it adheres to the naming convention the 
- TestCase Name
- Measured Duration
- Time when it was taken
- Machine Name
- Client/Server 
- Test Status

This is all encoded into the file name which is very fast to query without the need to open any json file. 
When you want to examine the contents of a Json file you can access of a TestDataFile its Extract property which 
will deserialize the Json file.

```
using ETWAnalyzer.Extract;
using ETWAnalyzer.ProcessTools;
using System.Collections.Generic;
using System.Linq;

namespace AutoQuery
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TestRunData data = new TestRunData(args[0]); // read file or a directory with tests
            foreach(TestDataFile test in data.AllFiles)  
            {
                IETWExtract extract = test.Extract; // read Json data
                foreach(KeyValuePair<ProcessKey,uint> topn in extract.CPU.PerProcessCPUConsumptionInMs.Take(10))
                {
                    ColorConsole.WriteEmbeddedColorLine($"[green]{topn.Value,-6} ms[/green] [yellow]{topn.Key,-50}[/yellow]");
                }

            }
        }
    }
}
```

This very simple application gives you already the top 10 CPU consumers of a file or a directory of files:

<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0
 style='border-collapse:collapse;border:none'>
 <tr style='height:143.25pt'>
  <td width=994 valign=top style='width:745.7pt;border:solid windowtext 1.0pt;
  background:black;padding:0in 5.4pt 0in 5.4pt;height:143.25pt'>
  <p class=MsoNormal style='line-height:normal'><span style='font-size:12.0pt;
  font-family:"Lucida Console";color:#CCCCCC;background:black'>c:\Temp\Extract&gt;AutoQuery.exe
  SlowDownload100PercentCPU_4.json<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;
  background:black'>114419 ms </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#F9F1A5;background:black'>System(4)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;
  background:black'>17350  ms </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#F9F1A5;background:black'>ETWController.exe(10460)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;
  background:black'>15954  ms </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#F9F1A5;background:black'>explorer.exe(11048)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;
  background:black'>15055  ms </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#F9F1A5;background:black'>ZSATunnel.exe(21740)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;
  background:black'>8079   ms </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#F9F1A5;background:black'>dwm.exe(1496)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;
  background:black'>6653   ms </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#F9F1A5;background:black'>Taskmgr.exe(8380)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;
  background:black'>6062   ms </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#F9F1A5;background:black'>svchost.exe(2176)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;
  background:black'>5141   ms </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#F9F1A5;background:black'>devenv.exe(6864)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;
  background:black'>4747   ms </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#F9F1A5;background:black'>svchost.exe(5140)<br>
  </span><span style='font-size:12.0pt;font-family:"Lucida Console";color:#16C60C;
  background:black'>4573   ms </span><span style='font-size:12.0pt;font-family:
  "Lucida Console";color:#F9F1A5;background:black'>devenv.exe(3748)</span></p>
  </td>
 </tr>
</table>

Below is the class Diagram of IETWExtract

![](Images/IEtwExtract.png)

## Scalable Json 

The main goal of ETWAnalyzer is to make profiling data queryable in a quick manner. On the other
hand many additional extractors can be added in the future. If the queried data is small it can 
be directly added to the main Json extract file. But if the data can be large, such as FILEIO data
which records every touched file operation it can become pretty large. To prevent that performance hit
a simple way out was chosen. 
During serialization for potentially large data extracts a different file is used which contains
the suffix *_Derived_xxxx.json*

![](Images/ExtractedDataFiles.png)

These derived files are accessed only when you touch e.g. the FileIO data property. That way
we can evolve over time many more extractors with different goals without sacrificing query speed for 
common things. 
