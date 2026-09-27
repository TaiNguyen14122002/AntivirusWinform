# Vì sao trang cao hơn vùng nhìn? In PreferredSize của từng tầng để biết tầng nào phình.
param([string[]]$Views = @("lichsu", "baove", "cachly", "caidat", "tongquan"))
$root = Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
[void][System.Reflection.Assembly]::LoadFrom((Join-Path $root "bin\Debug\ScanAndRemoveVirus.exe"))
$env:XVIRUS_DATA_DIR = Join-Path $root "obj\UiVerification\data"
$frmType = [ScanAndRemoveVirus.FrmMain]
$flag = [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Public

foreach ($v in $Views) {
    $f = [System.Activator]::CreateInstance($frmType)
    $f.Show()
    $frmType.GetMethod("ShowForShot", $flag).Invoke($f, @($v))
    for ($i = 0; $i -lt 30; $i++) { [System.Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 10 }
    $content = $f.Controls.Find("pnlContent", $true)[0]
    $uc = $content.Controls[0]
    Write-Output ""
    Write-Output "=== $v :: $($uc.Name) viewport=$($uc.ClientSize.Height) scrollMin=$($uc.AutoScrollMinSize.Height) ==="
    $tlp = $uc.Controls | Where-Object { $_ -is [System.Windows.Forms.TableLayoutPanel] } | Select-Object -First 1
    if ($null -eq $tlp) { Write-Output "  (khong co root TLP)"; $f.Close(); continue }
    Write-Output ("  root {0}  preferred={1}  h={2}" -f $tlp.Name, $tlp.PreferredSize.Height, $tlp.Height)
    for ($r = 0; $r -lt $tlp.RowCount; $r++) {
        $rs = $tlp.RowStyles[$r]
        $kids = @()
        foreach ($c in $tlp.Controls) { if ($tlp.GetRow($c) -eq $r) { $kids += $c } }
        foreach ($c in $kids) {
            Write-Output ("    row{0} {1,-8} {2,-22} pref={3,4} h={4,4} dock={5}" -f $r, $rs.SizeType, $c.Name, $c.PreferredSize.Height, $c.Height, $c.Dock)
            if ($c -is [System.Windows.Forms.TableLayoutPanel]) {
                for ($r2 = 0; $r2 -lt $c.RowCount; $r2++) {
                    foreach ($c2 in $c.Controls) {
                        if ($c.GetRow($c2) -eq $r2) {
                            Write-Output ("        row{0} {1,-8} {2,-20} pref={3,4} h={4,4}" -f $r2, $c.RowStyles[$r2].SizeType, $c2.Name, $c2.PreferredSize.Height, $c2.Height)
                        }
                    }
                }
            }
        }
    }
    $f.Close(); $f.Dispose()
}
