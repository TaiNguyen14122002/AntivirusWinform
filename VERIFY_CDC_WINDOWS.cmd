@echo off
setlocal
where msbuild >nul 2>&1
if errorlevel 1 (
 echo Use the Developer Command Prompt for Visual Studio with .NET Framework 4.7.2 targeting pack.
 exit /b 1
)
msbuild Tests\CdcSelfTest.csproj /t:Rebuild /p:Configuration=Release /verbosity:minimal || exit /b 1
Tests\bin\CdcSelfTest.exe
exit /b %errorlevel%
