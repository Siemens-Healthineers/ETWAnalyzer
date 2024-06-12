Open Source and 3rd Party Software
==================================

Description
-----------
ETWAnalyzer uses open source and 3rd party software listed below.  
With respect to open source and 3rd party software, the applicable license conditions prevail over any other terms and conditions covering ETWAnalyzer.

ColorConsole
------------
* version  
  5
* copyright  
  © 2021 Rick Strahl
* use permission  
  [The content of this file is governed by GitHub terms of service.][ColorConsoleLicense]
* location in this repository  
  [ETWAnalyzer/Infrastructure/ColorConsole/ColorConsole.cs][ColorConsole]
* integration  
  The source code of this software is being consumed.

ExceptionExtractor
------------------
* version  
  1
* copyright  
  © 2022 Alois Kraus
* use permission    
  [The content of this file is governed by GitHub terms of service.][ExceptionExtractorLicense]
* location in this repository  
  [ETWAnalyzer/Extractors/Exception/ExceptionExtractor/ExceptionExtractor.cs][ExceptionExtractor]
* integration  
  The source code of this software is being consumed.

FileStress
----------
* version  
  baae3371d5e39312d6f6d76cc58d1ecc2fa51477
* copyright  
  © 2021 Alois Kraus
* license terms  
  [MIT][FileStressLicense]
* location in this repository  
  [ETWAnalyzer/Infrastructure/CtrlCHandler/CtrlCHandler.cs][FileStress]
* integration  
  The source code of this software is being consumed.

MemAnalyzer
-----------
* version  
  bd6c7bcbcf12143a0740e02d67ed34c56f5dcc3f
* copyright  
  © 2017 Alois Kraus
* license terms  
  [MIT][MemAnalyzerLicense]
* location in this repository  
  [ETWAnalyzer/Extract/ProcessRenamer/ProcessRenamer.cs][MemAnalyzer]
* integration  
  The source code of this software is being consumed.

Microsoft.Windows.EventTracing.Processing
--------------------------------------------
* version  
  1.8.0
* copyright  
  © 2019 Microsoft Corporation
* license terms  
  [Trace Processing EULA][MicrosoftWindowsEventTracingProcessingLicense_akaMS] published by Microsoft Corporation. If the hyperlink doesn't work you may refer to a [local copy of the Trace Processing EULA.][MicrosoftWindowsEventTracingProcessingLicense]
* integration  
  The publicly available [NUGET][MicrosoftWindowsEventTracingProcessing_nugetorg] package is consumed and hence contained in releases.

NewtonSoft Json
---------------
* version  
  13.0.1
* copyrights  
  © 2007-2008 James Newton-King  
  © 1998 Hewlett-Packard Company  
  © 2012 James Kovacs  
  © 2010-2015 James Kovacs, Damian Hickey & Contributors  
  © 2007-2009 Atif Aziz, Joseph Albahari. All rights reserved.
* license terms  
  Main license is [MIT, further licenses][NewtonSoftJsonLicense] apply.
* integration  
  The publicly available [NUGET][NewtonSoftJson_nugetorg] package is consumed and hence contained in releases.

Perfview
--------
* version  
  2.0.74
* copyright  
  © .NET Foundation and Contributors  
* license terms  
  [MIT][PerfviewLicense]
* location in this repository  
  [ETWAnalyzer/Converters][Perfview]
* integration  
  The source code of this software is being consumed.

7-Zip
-----
* version  
  19.00
* copyrights  
  © 1999-2019 Igor Pavlov  
  © 2015-2016 Apple Inc. All rights reserved.  
  © 1991, 1999 Free Software Foundation, Inc.
* license terms  
  Main license is [LGPL-2.1-or-later, further licenses][7-ZipLicense] apply.
* source code  
  [7-Zip source code][7-ZipSourceCode_sourceforgenet] published by Igor Pavlov. If the hyperlink doesn't work you may refer to a [local copy of the 7-Zip source code.][7-ZipSourceCode]
* location in this repository  
  [ETWAnalyzer/7-Zip][7-Zip]
* integration  
  The publicly available binary package is consumed, of which a copy has been archieved locally.  
  ETWAnalyzer neither statically nor dynamically links to 7-Zip, instead ETWAnalyzer starts a new 7z.exe process and provides required command line parameters.

**PInvoke.NET**
-----
* version  
  n.a.
 * copyrights

 * license terms  
   http://www.pinvoke.net/termsofuse.htm
 * source code  
  [Win32ErrorCodes.cs](http://pinvoke.net/default.aspx/Constants/Win32ErrorCodes.html)
 * integration  
   The code was modified and is located at 
   [Win32ErrorCodes.cs](https://github.com/Siemens-Healthineers/ETWAnalyzer/blob/main/ETWAnalyzer/Extract/PInvoke.NET/WinErrorCodes.cs)


**Squid-Box.SevenZipSharp.Lite**
-----
* version  
  1.6.2.24
 * copyrights

 * license terms  
   https://github.com/squid-box/SevenZipSharp?tab=LGPL-3.0-1-ov-file#readme
 * source code  
   https://github.com/squid-box/SevenZipSharp
 * integration  
	The publicly available [Squid-Box.SevenZipSharp] package is consumed and hence contained in releases.


<!-- References -->
[ColorConsole]:                                             <../Infrastructure/ColorConsole>
[ColorConsoleLicense]:                                      <https://docs.github.com/en/github/site-policy/github-terms-of-service#d-user-generated-content>

[ExceptionExtractor]:                                       <../Extractors/Exception/ExceptionExtractor>
[ExceptionExtractorLicense]:                                <https://docs.github.com/en/github/site-policy/github-terms-of-service#d-user-generated-content>

[FileStress]:                                               <../Infrastructure/CtrlCHandler>
[FileStressLicense]:                                        <../Infrastructure/CtrlCHandler/LICENSE>

[MemAnalyzer]:                                              <../Extract/ProcessRenamer>
[MemAnalyzerLicense]:                                       <../Extract/ProcessRenamer/LICENSE>

[MicrosoftWindowsEventTracingProcessing]:                   <../3rdParty/Microsoft.Windows.EventTracing.Processing>
[MicrosoftWindowsEventTracingProcessing_nugetorg]:          <https://www.nuget.org/packages/Microsoft.Windows.EventTracing.Processing.All>
[MicrosoftWindowsEventTracingProcessingLicense]:            <../3rdParty/Microsoft.Windows.EventTracing.Processing/traceprocessing-eula.pdf>
[MicrosoftWindowsEventTracingProcessingLicense_akams]:      <https://aka.ms/TraceProcessingLicense>

[NewtonSoftJson]:                                           <../3rdParty/NewtonSoft.Json>
[NewtonSoftJson_nugetorg]:                                  <https://www.nuget.org/packages/Newtonsoft.Json/>
[NewtonSoftJsonLicense]:                                    <../3rdParty/NewtonSoft.Json/LICENSE>

[Perfview]:                                                 <../Converters>
[PerfviewLicense]:                                          <../Converters/LICENSE>

[7-Zip]:                                                    <../3rdParty/7-Zip>
[7-ZipLicense]:                                             <../3rdParty/7-Zip/LICENSE>
[7-ZipSourceCode]:                                          <../3rdParty/7-Zip/7z1900-src.7z>
[7-ZipSourceCode_sourceforgenet]:                           <https://sourceforge.net/projects/sevenzip/files/7-Zip/19.00/7z1900-src.7z/download>

[Squid-Box.SevenZipSharp]:                                   <https://www.nuget.org/packages/Squid-Box.SevenZipSharp/>
