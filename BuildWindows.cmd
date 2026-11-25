@echo off
setlocal
if not defined UNITY_EDITOR set "UNITY_EDITOR=%ProgramFiles%\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe"
"%UNITY_EDITOR%" -batchmode -quit -projectPath "%~dp0." -buildTarget Win64 -executeMethod BuildNightRoute.Windows -logFile "%~dp0WindowsBuild.log"
exit /b %errorlevel%
