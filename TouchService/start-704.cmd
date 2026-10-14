@echo off
setlocal
cd /d "%~dp0"
start "TouchService 704" "%~dp0publish\win-x64\TouchService.exe" --console
