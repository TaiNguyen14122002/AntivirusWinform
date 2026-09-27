# Chụp 5 tab ra PNG để đối chiếu giao diện. Dùng --shot (DrawToBitmap) — KHÔNG dùng
# CopyFromScreen/PrintWindow: cửa sổ double-buffered + DWM trả ảnh chồng hình.
#   .\Tests\shots.ps1 after     -> obj\UiVerification\after-<tab>.png
param([string]$prefix = "after")
$root = Split-Path -Parent $PSScriptRoot
$exe  = Join-Path $root "bin\Debug\ScanAndRemoveVirus.exe"
$out  = Join-Path $root "obj\UiVerification"
New-Item -ItemType Directory -Force -Path $out | Out-Null
$env:XVIRUS_DATA_DIR = Join-Path $out "data"   # kho dữ liệu riêng, không đụng máy thật
foreach ($t in "tongquan", "baove", "cachly", "lichsu", "caidat") {
    $png = Join-Path $out "$prefix-$t.png"
    $view = if ($t -eq "tongquan") { $null } else { $t }
    $a = @("--shot", $png)
    if ($view) { $a += @("--shot-view", $view) }
    Start-Process -FilePath $exe -ArgumentList $a -Wait -WindowStyle Hidden
    Write-Output "$prefix-$t.png"
}
