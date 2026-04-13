@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0export.ps1"
exit /b %errorlevel%
