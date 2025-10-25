@echo off
setlocal
cd /d "%~dp0"
echo Verifying safe ZIP scanning with a Windows C# build...
msbuild "Tests\ArchiveScanSelfTest.csproj" /t:Rebuild /p:Configuration=Debug /v:minimal
if errorlevel 1 (
 echo Build failed. Use Developer Command Prompt for Visual Studio.
 exit /b 1
)
"Tests\bin\Debug\ArchiveScanSelfTest.exe"
exit /b %errorlevel%
