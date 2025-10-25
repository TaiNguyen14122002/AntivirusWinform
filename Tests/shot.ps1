Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$src = @"
using System; using System.Runtime.InteropServices;
public class W {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
}
"@
Add-Type -TypeDefinition $src
$p = Get-Process ScanAndRemoveVirus | Select-Object -First 1
[void][W]::ShowWindow($p.MainWindowHandle, 9)   # SW_RESTORE
[void][W]::SetForegroundWindow($p.MainWindowHandle)
Start-Sleep -Milliseconds 1200
$r = New-Object W+RECT
[void][W]::GetWindowRect($p.MainWindowHandle, [ref]$r)
# 24bpp (KHÔNG alpha): BitBlt của CopyFromScreen không ghi kênh alpha, nên bitmap mặc định
# (32bppArgb) sẽ có alpha=0 ở toàn ảnh; trình xem ghép trên nền đen sẽ cho ra "vùng đen" giả
# quanh các thẻ bo góc. 24bpp = đúng màu nhìn thấy trên màn hình.
$bmp = New-Object System.Drawing.Bitmap(($r.R - $r.L), ($r.B - $r.T), [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
$out = Join-Path $env:TEMP "kilo\shot_$($args[0]).png"
$bmp.Save($out)
$g.Dispose(); $bmp.Dispose()
Write-Output "saved: $out"
