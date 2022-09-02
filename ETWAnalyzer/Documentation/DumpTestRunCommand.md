# -Dump TestRun

Show all extracted files of a testrun. 

A TestRun is a collection of test cases which have been executed n times. E.g. you execute during a performance regression
test run the tests
- Open
- Load
- Close

each 10 times with distributed ETW profiling on a Client and Server machine you get 60 ETL files which are compressed.

A new test run is "detected" if between two tests is a time gap > 1h.

The currently supported file naming convention for compressed ETL files is

>TestCase_ddddms_Machine_CLT/SRV_TestStatus-Passed_Date.zip/7z 

the compressed file must contain an equally named ETL file inside the archive.

e.g. P03CTOpen_11299ms_IBDI1VIARELP648_CLT_TestStatus-Passed_20211023-213242 is a valid compressed test case name.

A test measures at least one time value which is a part of the file name which allows you to create a simple
viewer which just needs to parse a directory of file names to visualize the runtime of all executed test cases with 
no external database. Everything is self contained. 
```
Run[462] starts at 12/14/2021 11:30:29 PM duration: 00 00:03:01, TestCases:   4 Tests:   4
Run[463] starts at 12/15/2021 4:02:07 AM duration: 00 00:00:00, TestCases:   1 Tests:   1
Run[464] starts at 12/22/2021 10:50:40 AM duration: 00 00:01:24, TestCases:   2 Tests:   2
Run[465] starts at 12/22/2021 1:31:47 PM duration: 00 00:01:57, TestCases:   3 Tests:   3
Run[466] starts at 12/22/2021 4:54:11 PM duration: 00 00:02:51, TestCases:   4 Tests:   4
Run[467] starts at 12/22/2021 9:15:03 PM duration: 00 00:00:00, TestCases:   1 Tests:   1
Run[468] starts at 1/10/2022 2:27:28 PM duration: 00 00:01:26, TestCases:   2 Tests:   2
Run[469] starts at 1/10/2022 5:06:50 PM duration: 00 00:01:58, TestCases:   3 Tests:   3
Run[470] starts at 1/10/2022 8:36:27 PM duration: 00 00:03:10, TestCases:   4 Tests:   4
Run[471] starts at 1/11/2022 1:33:25 AM duration: 00 00:00:00, TestCases:   1 Tests:   1
======================================
Summary
======================================
Runs: 472 First: 6/17/2021 8:35:00 AM Last: 1/11/2022 1:33:25 AM, Files: 5000000 Extracted: 5000000
Used Machines:
        Host1:  5000000
        Host2:  5000000
Total Tests: 5000000
        P01                            :   100000
        P02                            :   100000
        P03                            :   100000
        P07                            :   100000
```

All other dump commands support filtering for such named tests by
- Selecting only n Tests of a test case per TestRun *-TestsPerRun dd*
- Skipping n Tests *-SkipNTests dd*
- Selecting a specific TestRun Index *-TestRunIndex dd*
- Selecting a number of Testruns since TestRun Index with -TestRunIndex dd *-TestRunCount dd*

If your test data does not follow this naming convention you still can use the filter *-TestCase* filter which will
then match the complete file name. 
See [Filters](Filters.md) how to define multiple filters. 

