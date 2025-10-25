# Đo bố cục thật của từng tab (không đoán qua ảnh chụp).
# Báo: (1) control nào tràn ra ngoài vùng nhìn của cha nó, (2) control nào bị bóp
# nhỏ hơn chiều cao chữ cần, (3) trang có thanh cuộn không.
#   .\Tests\probe.ps1             -> tất cả 5 tab
#   .\Tests\probe.ps1 baove       -> một tab
param(
    [string[]]$Views = @("tongquan", "baove", "cachly", "lichsu", "caidat"),
    [int]$W = 0, [int]$H = 0   # >0: ép cửa sổ về kích thước này (đo ở mức nhỏ nhất)
)
$root = Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
[void][System.Reflection.Assembly]::LoadFrom((Join-Path $root "bin\Debug\ScanAndRemoveVirus.exe"))
$env:XVIRUS_DATA_DIR = Join-Path $root "obj\UiVerification\data"

$frmType = [ScanAndRemoveVirus.FrmMain]
$flag = [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Public

foreach ($v in $Views) {
    $f = [System.Activator]::CreateInstance($frmType)
    if ($W -gt 0) { $f.Size = New-Object System.Drawing.Size($W, $H) }
    $f.Show()
    # ShowForShot là internal -> phải gọi qua reflection với cờ NonPublic
    $frmType.GetMethod("ShowForShot", $flag).Invoke($f, @($v))
    for ($i = 0; $i -lt 30; $i++) { [System.Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 10 }

    # UserControl đang hiển thị trong pnlContent
    $content = $f.Controls.Find("pnlContent", $true)[0]
    $uc = $content.Controls[0]
    Write-Output ""
    Write-Output "=== $v :: $($uc.Name)  page=$($uc.ClientSize.Width)x$($uc.ClientSize.Height)  scroll=$($uc.VerticalScroll.Visible) range=$($uc.VerticalScroll.Maximum) ==="

    $bad = 0
    $walk = {
        param($parent)
        foreach ($c in $parent.Controls) {
            if (-not $c.Visible) { continue }
            $need = 0
            if ($c -is [System.Windows.Forms.Label] -and $c.AutoSize) {
                # PreferredHeight = chiều cao chữ + ~6px đệm viền mà Label luôn cộng thêm.
                # Trừ khoản đệm đó đi, nếu không thì mọi nhãn 18pt nằm gọn trong hàng 36px
                # đều bị báo "bóp" oan (chữ vẽ vẫn đủ chỗ).
                $need = $c.PreferredHeight - 6
            }
            $over = $c.Bottom - $parent.ClientSize.Height
            $squash = $need -gt 0 -and $c.Height -lt $need
            if ($over -gt 1 -or $squash) {
                $msg = "  {0,-28} {1,-22} y={2,4} h={3,4} bottom={4,4} (parent h={5})" -f $c.Name, $c.GetType().Name, $c.Top, $c.Height, $c.Bottom, $parent.ClientSize.Height
                if ($over -gt 1) { $msg += "  TRAN +$over" }
                if ($squash) { $msg += "  BOP (can $need)" }
                Write-Output $msg
                $script:bad++
            }
            & $walk $c
        }
    }
    $script:bad = 0
    & $walk $uc
    if ($script:bad -eq 0) { Write-Output "  (khong phat hien tran/bop)" }
    $f.Close(); $f.Dispose()
}
