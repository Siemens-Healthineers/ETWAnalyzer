# Contributing

## Code of Conduct

Any contribution is considered very valuable and is highly appreciated. However, the ETWAnalyzer project requires those to comply with following conditions:  
- [GitHub terms of service][GitHubTermsOfService]
- Your contribution and all parts thereof are licensed under MIT license only.
- If the contribution contains any 3rd party software, all their licenses must be compatible with the MIT license. Furthermore, all license obligations must be complied with, which applies particularly but not only to maintaining copyrights, providing acknowledgments, annotations in source code, providing the original license texts and source code.
- No part of the contribution is protected by any patent, neither at the time of contribution nor in future.

## Get Started
If you miss an extractor, or a specific dump command you will want to compile ETWAnalyzer.

You need VS 2022 17.1.0 or later because ETWAnalyzer compiles to .NET 6.0 with full source code
embedded in PDBs. You can debug right away without any additional source code compilations.
As target platforms are .NET 6.0 and .NET 4.8 supported.

> Hint if you debug extraction and you wonder why your breakpoints are never hit: ETWAnalyzer starts a child process to do the actual extraction. Add -child to the -extract command line to get the expected breakpoints.

## Debugging Self Contained .NET 6.0 Applications
This is currently a [pain](https://docs.microsoft.com/en-us/dotnet/core/deploying/single-file/overview) and not really supported by Visual Studio.
The best thing is to compile ETWAnalyzer and not use the the single file deployment. The .NET 6.0 single file deployment merges all dlls
into one giant image. All infrastructure like GC, JIT, other managed code is part of ETWAnalyzer.exe which makes profiling pretty hard, because
stacktags which contain the image name no longer work. 

The only way to get call stacks of single file apps is to take a memory dump and load that into Visual Studio, or Windbg. 

You can also use .NET 4.8 and debug as usual. That will always work.

## Source Code Structure
| Directory   | Contains    |
| ----------- | ----------- |
| Extractors  | Code used to extract data from ETL files | 
| Extract     | ETWExtract class which contains the deserialized Json object model | 
| EventDump   | -Dump xxx commands |
| Commands    | Command line parsing logic to create specific commands such as -extract, -dump ...  |


<!-- References -->
[GitHubTermsOfService]:     <https://docs.github.com/en/github/site-policy/github-terms-of-service#d-user-generated-content>
