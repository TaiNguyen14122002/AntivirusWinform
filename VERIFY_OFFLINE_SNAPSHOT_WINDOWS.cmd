@echo off
setlocal
cd /d "%~dp0"
set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%MSBUILD%" (
  for /f "usebackq delims=" %%I in (`"%MSBUILD%" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD_EXE=%%I"
)
if not defined MSBUILD_EXE set "MSBUILD_EXE=msbuild"
"%MSBUILD_EXE%" "Tests\OfflineSnapshotSelfTest.csproj" /p:Configuration=Debug /t:Rebuild /v:minimal
if errorlevel 1 exit /b 1
"Tests\bin\OfflineSnapshotSelfTest.exe"
exit /b %errorlevel%
