@echo off
echo Build Targets

setlocal enabledelayedexpansion
set ScriptLocation=%~d0%~p0
set BinFolder48=!ScriptLocation!..\bin\Release\publishNet48
set BinFolderNet6=!ScriptLocation!..\bin\Release\publishNet6

dotnet publish /p:PublishProfile=!ScriptLocation!\ETWAnalyzer\Properties\PublishProfiles\Net60_SelfContained.pubxml /p:TargetFramework=net6.0-windows /p:PublishDir=!BinFolderNet6!
dotnet publish /p:PublishProfile=!ScriptLocation!\ETWAnalyzer\Properties\PublishProfiles\Net48.pubxml /p:TargetFramework=net48 /p:PublishDir=!BinFolder48!

call :DelFile "!BinFolder48!"
call :DelFile "!BinFolderNet6!"

goto :EOF

:DelFile 
echo Binfolder is %1
cd %1
echo Delete superflous files
del ETWAnalyzer_Trace.log 2> NUL
del "WPTx86 (DesktopEditions)-x86_en-us.msi"
del "WPTx86 (OnecoreUAP)-x86_en-us.msi"
rd /q /s x86
exit /B 1
