@echo off
setlocal
cd /d "%~dp0"
where msbuild >nul 2>&1
if errorlevel 1 (
    echo Mo file nay bang Developer Command Prompt for Visual Studio.
    exit /b 1
)
msbuild Tests\SqlSignatureProbe.csproj /t:Rebuild /p:Configuration=Release /verbosity:minimal
if errorlevel 1 exit /b 1
Tests\bin\SqlSignatureProbe.exe %*
exit /b %errorlevel%
