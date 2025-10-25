# ============================================================
# Auto-reload cho app WinForms:
#   phát hiện .cs/.resx/.csproj trong project thay đổi
#   -> build Debug -> tắt app cũ -> mở lại bản mới.
#   Build lỗi -> popup thông báo, app nguồn sẽ chạy lại ở lần sửa kế tiếp.
# Chạy:  powershell -ExecutionPolicy Bypass -File Tests\dev-reload.ps1
# ============================================================
param(
    [int]$PollMs = 700,
    [int]$DebounceMs = 1200
)

$RepoRoot  = Split-Path -Parent $PSScriptRoot          # gốc repo (csproj nằm ngay tại đây)
$ProjRoot  = $RepoRoot
$ProjFile  = Join-Path $ProjRoot "ScanAndRemoveVirus.csproj"
$Exe       = Join-Path $ProjRoot "bin\Debug\ScanAndRemoveVirus.exe"

$MsBuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -products * -requires Microsoft.Component.MSBuild `
    -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
if (-not $MsBuild) { $MsBuild = "msbuild" }

function Show-Error([string]$msg) {
    $safe = $msg.Replace('`', ' ').Replace('"', ' ').Replace("'", ' ')
    if ($safe.Length -gt 1500) { $safe = $safe.Substring(0, 1500) }
    $cmd = "Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.MessageBox]::Show('Build failed - app will run again after next valid save:`n`n$safe', 'Auto-reload', 'OK', 'Warning') | Out-Null"
    $enc = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($cmd))
    Start-Process powershell -ArgumentList '-NoProfile','-WindowStyle','Hidden','-EncodedCommand',$enc | Out-Null
}

function Get-Marker {
    (Get-ChildItem -Path $ProjRoot -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Extension -in '.cs', '.resx', '.csproj', '.config' } |
        Measure-Object -Property LastWriteTimeUtc -Maximum).Maximum
}

function Reload {
    Get-Process ScanAndRemoveVirus -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Milliseconds 400
    [Console]::Out.WriteLine("[autoreload] " + (Get-Date -Format "HH:mm:ss") + " build...")
    $out = & $MsBuild $ProjFile /p:Configuration=Debug /v:m /nologo 2>&1
    if ($LASTEXITCODE -eq 0) {
        [Console]::Out.WriteLine("[autoreload] build OK -> relaunch app")
        Start-Process $Exe
    } else {
        $err = (($out | Select-String -Pattern 'error [A-Z]+\d+' | Select-Object -First 8) | ForEach-Object { $_.Line }) -join "`n"
        if (-not $err) { $err = ($out | Select-Object -Last 8) -join "`n" }
        [Console]::Out.WriteLine("[autoreload] build FAIL:`n" + $err)
        # neu user con dang luoi tay file (nhan chep nua thanh nua - trang thai trung gian) -> khong popup, vong lap se build lai
        Start-Sleep -Milliseconds 2500
        if ((Get-Marker) -gt $Last) {
            [Console]::Out.WriteLine('[autoreload] co luu khac lien sau -> bo popup, tu build lai')
        } else {
            Show-Error $err
        }
    }
}

[Console]::Out.WriteLine("[autoreload] watching: $ProjRoot (exclude bin/obj)  poll=${PollMs}ms debounce=${DebounceMs}ms")
Reload   # build ngay lần đầu để bin khớp source hiện tại
$Last = Get-Marker
Write-Output "READY - auto-reload active"

while ($true) {
    Start-Sleep -Milliseconds $PollMs
    $now = Get-Marker
    if ($now -and $Last -and $now -gt $Last) {
        Start-Sleep -Milliseconds $DebounceMs   # gom nhiều save lien tiep
        $Last = Get-Marker
        Reload
        Start-Sleep -Milliseconds 800           # de marker khong dem lai file vua sinh trong bin/obj
        $Last = Get-Marker
    } elseif ($now -and $Last -and $now -lt $Last) {
        $Last = $now
    }
}
