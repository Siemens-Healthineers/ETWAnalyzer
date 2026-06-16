@echo off
echo Build Targets

setlocal enabledelayedexpansion
set ScriptLocation=%~d0%~p0
set BinFolderNet10SelfContained=!ScriptLocation!..\bin\Release\publishSelfContained
set BinSamplesFolder=!ScriptLocation!..\Samples\bin\x64\Release
set BinNotSelfContained=!ScriptLocation!..\bin\Release\Net10-NotSelfContained

set ObjFolder=!ScriptLocation!\obj
set ObjFolderReader=!ScriptLocation!\..\ETWAnalyzer.Reader\obj

set ScriptPublishProfile=!ScriptLocation!\Properties\PublishProfiles\Net10_SelfContained.pubxml
set ScriptTargetFW=net10.0-windows
set ScriptRID=win-x64

call :CleanTempFiles

rem dotnet build !ScriptLocation!\ETWAnalyzer.csproj /p:Configuration=Release -o !BinNotSelfContained!
dotnet publish !ScriptLocation!\ETWAnalyzer.csproj /p:Configuration=Release -f !ScriptTargetFW! -r !ScriptRID! /p:SelfContained=true /p:PublishReadyToRun=true /p:PublishSingleFile=false /p:PublishDir=!BinFolderNet10SelfContained!

dotnet build !ScriptLocation!\..\ETWAnalyzer.Reader\ETWAnalyzer.Reader.csproj  /p:Configuration=Release -f net48
dotnet pack  !ScriptLocation!\..\ETWAnalyzer.Reader\ETWAnalyzer.Reader.csproj  /p:Configuration=Release 

msbuild /p:Configuration=Release /p:Platform=x64 /p:OutDir=!BinSamplesFolder!\ !ScriptLocation!\..\Samples\EventLeak\EventLeak.vcxproj 

call :DelFile "!BinFolderNet10!"
call :DelFile "!BinFolderNet10SelfContained!"

goto :EOF

:DelFile 
echo Binfolder is %1
cd %1
echo Delete superflous files
del ETWAnalyzer_Trace.log 2> NUL
rd /q /s x86
rd /q /s arm64
exit /B 1

:CleanTempFiles

rd /q /s !BinSamplesFolder! 2> NUL
rd /q /s !ObjFolder! 2> NUL
rd /q /s !ObjFolderReader! 2> NUL
rd /q /s !BinFolderNet10SelfContained! 2> NUL
rd /q /s !BinNotSelfContained! 2> NUL

exit /B 1