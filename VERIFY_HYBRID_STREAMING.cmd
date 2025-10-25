@echo off
setlocal
cd /d "%~dp0"
where msbuild >nul 2>nul
if errorlevel 1 (
  echo MSBuild chua co trong PATH. Hay mo "Developer Command Prompt for VS" va chay lai.
  exit /b 1
)
msbuild "Tests\HybridHashSelfTest.csproj" /p:Configuration=Release /t:Rebuild /v:minimal
if errorlevel 1 exit /b 1
"Tests\bin\HybridHashSelfTest.exe"
exit /b %ERRORLEVEL%
