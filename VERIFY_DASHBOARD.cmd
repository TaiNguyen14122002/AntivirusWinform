@echo off
setlocal
cd /d "%~dp0"
where msbuild >nul 2>nul
if errorlevel 1 (
  echo Open Developer Command Prompt for Visual Studio, then run this file again.
  exit /b 1
)
msbuild Tests\DashboardSmoke.csproj /t:Rebuild /p:Configuration=Debug /m
if errorlevel 1 exit /b 1
set XVIRUS_LEGACY_UI_TEST=1
Tests\bin\Debug\dashboard_test.exe "%~dp0DashboardVerification"
if errorlevel 1 exit /b 1
msbuild Tests\Sketch20Smoke.csproj /t:Rebuild /p:Configuration=Debug /m
if errorlevel 1 exit /b 1
set XVIRUS_LEGACY_UI_TEST=0
Tests\bin\Debug\sketch20-test.exe "%~dp020ScreensVerification"
if errorlevel 1 exit /b 1
msbuild Tests\TypographySmoke.csproj /t:Rebuild /p:Configuration=Debug /m
if errorlevel 1 exit /b 1
Tests\bin\Debug\typography-test.exe "%~dp0TypographyEvidence"
exit /b %errorlevel%
