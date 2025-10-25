@echo off
setlocal
cd /d "%~dp0"
echo Checking new Admin modules ...
if not exist "Control\AdminSecurity.cs" (echo Missing AdminSecurity.cs & exit /b 1)
if not exist "Control\AdminAudit.cs" (echo Missing AdminAudit.cs & exit /b 1)
if not exist "Control\UcQuanTri.cs" (echo Missing UcQuanTri.cs & exit /b 1)
where msbuild >nul 2>nul
if errorlevel 1 (
  echo MSBuild not on PATH. Open ScanAndRemoveVirus.sln in Visual Studio and Rebuild Solution.
  exit /b 0
)
msbuild "ScanAndRemoveVirus.sln" /t:Rebuild /p:Configuration=Debug /p:Platform="Any CPU" /verbosity:minimal
exit /b %errorlevel%
