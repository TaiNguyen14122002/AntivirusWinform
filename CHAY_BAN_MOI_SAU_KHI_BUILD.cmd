@echo off
setlocal
cd /d "%~dp0"
if exist "bin\Debug\ScanAndRemoveVirus.exe" (
  echo Dang mo ban Debug moi nhat sau Build...
  start "" "bin\Debug\ScanAndRemoveVirus.exe"
  exit /b 0
)
echo Khong tim thay EXE da Build: bin\Debug\ScanAndRemoveVirus.exe
echo Mo ScanAndRemoveVirus.sln, chon Debug x64, Clean Solution, Rebuild Solution.
pause
exit /b 1
