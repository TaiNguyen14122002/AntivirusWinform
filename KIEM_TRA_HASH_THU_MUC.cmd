@echo off
setlocal
cd /d "%~dp0"
echo =========================================================
echo  KIEM TRA HASH TUNG TEP TRONG THU MUC VOI AntivirusDB
 echo =========================================================
set /p "SCAN_FOLDER=Nhap duong dan thu muc ban da quet (khong can dau ngoac kep): "
if not defined SCAN_FOLDER (
  echo Chua nhap thu muc.
  pause
  exit /b 1
)
set "SCAN_FOLDER=%SCAN_FOLDER:"=%"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\KiemTraHashThuMuc.ps1" -Folder "%SCAN_FOLDER%"
set "STATUS=%ERRORLEVEL%"
echo.
if not "%STATUS%"=="0" echo CHU Y: Cong cu gap loi. Kiem tra server localhost\SQLEXPRESS, database AntivirusDB, quyen SQL.
pause
exit /b %STATUS%
