@echo off
echo Build Targets

setlocal enabledelayedexpansion
set ScriptLocation=%~d0%~p0
set BinFolderNet48=!ScriptLocation!..\bin\Release\publishNet48
set BinFolderNet6=!ScriptLocation!..\bin\Release\publishNet6

set ObjFolder=!ScriptLocation!\obj

set ReleaseZipNet6=!BinFolderNet6!\..\ETWAnalyzer_Net6.zip
set ReleaseZipNet48=!BinFolderNet48!\..\ETWAnalyzer_Net48.zip

del !ReleaseZipNet6! 2> NUL
del !ReleaseZipNet48! 2> NUL

rd /q /s !BinFolderNet48!
rd /q /s !BinFolderNet6!
rd /q /s !ObjFolder!

dotnet publish /p:PublishProfile=!ScriptLocation!\ETWAnalyzer\Properties\PublishProfiles\Net60_SelfContained.pubxml /p:Configuration=Release /p:TargetFramework=net6.0-windows /p:PublishDir=!BinFolderNet6!
dotnet publish /p:PublishProfile=!ScriptLocation!\ETWAnalyzer\Properties\PublishProfiles\Net48.pubxml /p:Configuration=Release /p:TargetFramework=net48 /p:PublishDir=!BinFolderNet48!

call :DelFile "!BinFolderNet48!"
call :DelFile "!BinFolderNet6!"

7z a  !ReleaseZipNet6! -r "!BinFolderNet6!\*.*"
7z a  !ReleaseZipNet48! -r "!BinFolderNet48!\*.*"

goto :EOF

:DelFile 
echo Binfolder is %1
cd %1
echo Delete superflous files
del ETWAnalyzer_Trace.log 2> NUL
rd /q /s x86
rd /q /s arm64
exit /B 1
