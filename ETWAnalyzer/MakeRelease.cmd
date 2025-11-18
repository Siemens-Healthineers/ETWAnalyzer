@echo off
echo Build Targets

setlocal enabledelayedexpansion
set ScriptLocation=%~d0%~p0
set BinFolderNet10=!ScriptLocation!..\bin\Release\net10.0-windows\win-x64
set BinSamplesFolder=!ScriptLocation!..\Samples\bin\x64\Release

set ObjFolder=!ScriptLocation!\obj

set ReleaseZipNet10=!ScriptLocation!\..\bin\ETWAnalyzer_Net10.zip
set ReleaseSamples=!ScriptLocation!..\bin\Samples_Binaries.zip

del !ReleaseZipNet10! 2> NUL
del !ReleaseSamples! 2> NUL

rd /q /s !BinFolderNet10!
rd /q /s !BinSamplesFolder!
rd /q /s !ObjFolder!

dotnet publish !ScriptLocation!\ETWAnalyzer.csproj /p:PublishProfile=!ScriptLocation!\ETWAnalyzer\Properties\PublishProfiles\Net10_SelfContained.pubxml /p:Configuration=Release /p:TargetFramework=net10.0-windows /p:PublishDir=!BinFolderNet10!
dotnet build !ScriptLocation!\..\ETWAnalyzer.Reader\ETWAnalyzer.Reader.csproj  /p:Configuration=Release -f net48
dotnet pack  !ScriptLocation!\..\ETWAnalyzer.Reader\ETWAnalyzer.Reader.csproj  /p:Configuration=Release 

msbuild /p:Configuration=Release /p:Platform=x64 /p:OutDir=!BinSamplesFolder! !ScriptLocation!\..\Samples\EventLeak\EventLeak.vcxproj 

call :DelFile "!BinFolderNet10!"

7z a  !ReleaseZipNet10! -r "!BinFolderNet10!\*.*"
7z a  !ReleaseSamples! -r "!BinSamplesFolder!\*.*"

goto :EOF

:DelFile 
echo Binfolder is %1
cd %1
echo Delete superflous files
del ETWAnalyzer_Trace.log 2> NUL
rd /q /s x86
rd /q /s arm64
exit /B 1
