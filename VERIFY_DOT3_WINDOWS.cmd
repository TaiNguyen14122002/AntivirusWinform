@echo off
setlocal
cd /d "%~dp0"
echo [1/3] Visual Studio: Build - Rebuild Solution tu ScanAndRemoveVirus.sln
if not exist "ScanAndRemoveVirus.sln" (echo [FAIL] Khong co solution & exit /b 1)
echo [2/3] Kiem tra thanh phan dot 3
if not exist "Services\ScanScheduleService.cs" (echo [FAIL] Thieu lich quet & exit /b 1)
if not exist "Services\SignatureVerificationService.cs" (echo [FAIL] Thieu quan ly chu ky & exit /b 1)
if not exist "Services\ScanExclusionService.cs" (echo [FAIL] Thieu ngoai le & exit /b 1)
if not exist "Services\SecurityHealthCheckService.cs" (echo [FAIL] Thieu health check & exit /b 1)
echo [3/3] Hay chay migration 004, mo app va thu F02 F07 F09 F10 F11 theo huong dan.
echo Neu muon dung Task Scheduler, ung dung phai duoc mo tu EXE da build.
endlocal
