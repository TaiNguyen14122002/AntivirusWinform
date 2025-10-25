@echo off
setlocal
cd /d "%~dp0"
where msbuild >nul 2>&1
if errorlevel 1 (
  echo Chay file nay bang Developer Command Prompt for Visual Studio.
  exit /b 2
)
msbuild "Tests\BehavioralActivitySelfTest.csproj" /t:Rebuild /p:Configuration=Release /v:minimal
if errorlevel 1 exit /b 1
"Tests\bin\Release\BehavioralActivitySelfTest.exe"
exit /b %errorlevel%
