@echo off
setlocal
cd /d "%~dp0"
where msbuild >nul 2>nul
if errorlevel 1 (
    echo MSBuild chua nam trong PATH. Hay chay bang Developer Command Prompt for Visual Studio.
    pause
    exit /b 1
)
msbuild "Tests\ScanAllFilesSelfTest.csproj" /t:Rebuild /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal
if errorlevel 1 (
    echo Build that bai.
    pause
    exit /b 1
)
"Tests\bin\ScanAllFilesSelfTest.exe"
set exitCode=%errorlevel%
pause
exit /b %exitCode%
