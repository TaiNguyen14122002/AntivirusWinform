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
$bmp = New-Object System.Drawing.Bitmap(($r.R - $r.L), ($r.B - $r.T))
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
$out = Join-Path $env:TEMP "kilo\shot_$($args[0]).png"
$bmp.Save($out)
$g.Dispose(); $bmp.Dispose()
Write-Output "saved: $out"
