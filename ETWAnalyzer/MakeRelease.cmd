@echo off
echo Build Targets

setlocal enabledelayedexpansion
set ScriptLocation=%~d0%~p0
set BinFolderNet48=!ScriptLocation!..\bin\Release\publishNet48
set BinFolderNet8=!ScriptLocation!..\bin\Release\publishNet8
set BinSamplesFolder=!ScriptLocation!..\Samples\bin\x64\Release

set ObjFolder=!ScriptLocation!\obj

set ReleaseZipNet6=!BinFolderNet8!\..\ETWAnalyzer_Net8.zip
set ReleaseZipNet48=!BinFolderNet48!\..\ETWAnalyzer_Net48.zip
set ReleaseSamples=!ScriptLocation!..\Samples_Binaries.zip

del !ReleaseZipNet6! 2> NUL
del !ReleaseZipNet48! 2> NUL
del !ReleaseSamples! 2> NUL

rd /q /s !BinFolderNet48!
rd /q /s !BinFolderNet8!
rd /q /s !BinSamplesFolder!
rd /q /s !ObjFolder!

dotnet publish !ScriptLocation!\ETWAnalyzer.csproj /p:PublishProfile=!ScriptLocation!\ETWAnalyzer\Properties\PublishProfiles\Net80_SelfContained.pubxml /p:Configuration=Release /p:TargetFramework=net8.0-windows /p:PublishDir=!BinFolderNet8!
dotnet publish !ScriptLocation!\ETWAnalyzer.csproj /p:PublishProfile=!ScriptLocation!\ETWAnalyzer\Properties\PublishProfiles\Net48.pubxml /p:Configuration=Release /p:TargetFramework=net48 /p:PublishDir=!BinFolderNet48!
dotnet pack !ScriptLocation!\ETWAnalyzer.csproj  /p:Configuration=Release 

msbuild /p:Configuration=Release /p:Platform=x64 /p:OutDir=!BinSamplesFolder! !ScriptLocation!\..\Samples\EventLeak\EventLeak.vcxproj 

call :DelFile "!BinFolderNet48!"
call :DelFile "!BinFolderNet8!"

7z a  !ReleaseZipNet6! -r "!BinFolderNet8!\*.*"
7z a  !ReleaseZipNet48! -r "!BinFolderNet48!\*.*"
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
