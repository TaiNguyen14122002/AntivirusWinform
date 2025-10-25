@echo off
setlocal
cd /d "%~dp0"
set "scanner_msbuild="
for /f "delims=" %%B in ('where msbuild 2^>nul') do if not defined scanner_msbuild set "scanner_msbuild=%%B"
set "scanner_vswhere=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%scanner_vswhere%" set "scanner_vswhere=C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
if not defined scanner_msbuild if exist "%scanner_vswhere%" (
  for /f "delims=" %%B in ('"%scanner_vswhere%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe') do set "scanner_msbuild=%%B"
)
if not defined scanner_msbuild (
  echo Install .NET desktop development and .NET Framework 4.7.2 targeting pack in Visual Studio.
  exit /b 2
)
"%scanner_msbuild%" Tests\WindowsUiEvidence.csproj /t:Rebuild /p:Configuration=Release /m
if errorlevel 1 exit /b 2
Tests\bin\Release\windows-ui-evidence.exe "%~dp0WindowsEvidence" "%~dp0UI_Preview\PhacThao_Goc" > "%~dp0WindowsEvidence.log" 2>&1
set scanner_result=%errorlevel%
type "%~dp0WindowsEvidence.log"
echo Evidence is in WindowsEvidence. Current Windows DPI only; display settings were not changed.
exit /b %scanner_result%
