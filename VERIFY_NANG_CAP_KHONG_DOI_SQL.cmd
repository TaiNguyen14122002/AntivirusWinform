@echo off
setlocal
cd /d "%~dp0"
echo [1/2] Build ma nguon WinForms .NET Framework 4.7.2...
where msbuild >nul 2>nul
if errorlevel 1 (
  echo Khong co MSBuild. Mo Developer Command Prompt for Visual Studio.
  exit /b 1
)
msbuild "ScanAndRemoveVirus.csproj" /t:Rebuild /p:Configuration=Debug /v:minimal
if errorlevel 1 exit /b 1
echo [2/2] Kiem tra ZIP/TAR/GZ bang tep thu vo hai...
call VERIFY_ARCHIVE_SCAN_WINDOWS.cmd
if errorlevel 1 exit /b 1
echo Build + archive selftest da hoan tat.
echo CHUA kiem thu SQL Server, cach ly, offline queue va WinTrust tren Windows that.
exit /b 0
