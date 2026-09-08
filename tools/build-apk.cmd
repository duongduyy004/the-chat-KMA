@echo off
rem Runs tools/build-apk.ps1 regardless of the machine PowerShell execution policy.
rem Forwards every argument: build-apk.cmd -Abi x86_64
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-apk.ps1" %*
exit /b %ERRORLEVEL%
