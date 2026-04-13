@echo off
setlocal
cd /d "%~dp0"
echo Codex History Migration
echo 1. Export (source machine)
echo 2. Import (target machine)
set /p choice=Select 1 or 2: 
if "%choice%"=="1" goto do_export
if "%choice%"=="2" goto do_import
echo Invalid selection.
pause
exit /b 1
:do_export
call "%~dp0export.cmd"
pause
exit /b %errorlevel%
:do_import
call "%~dp0import.cmd"
pause
exit /b %errorlevel%
