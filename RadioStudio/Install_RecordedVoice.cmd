@echo off
cd /d "%~dp0"
echo Close Humanitarian before applying. Select the folder containing Humanitarian_Unity.exe.
py -3 game_patch.py
pause
