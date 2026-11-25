@echo off
setlocal
if not defined UE_ROOT set "UE_ROOT=%ProgramFiles%\Epic Games\UE_5.5"
call "%UE_ROOT%\Engine\Build\BatchFiles\RunUAT.bat" BuildCookRun -project="%~dp0Humanitarian_UE5.uproject" -noP4 -platform=Win64 -clientconfig=Development -build -cook -map=/Engine/Maps/Entry -stage -pak -archive -archivedirectory="%~dp0Build" -prereqs -utf8output
exit /b %errorlevel%
