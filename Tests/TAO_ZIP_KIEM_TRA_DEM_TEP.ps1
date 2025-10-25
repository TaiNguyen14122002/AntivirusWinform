# Chạy trên Windows PowerShell 5.1: .\Tests\TAO_ZIP_KIEM_TRA_DEM_TEP.ps1
# Chỉ tạo dữ liệu vô hại để nhìn bộ đếm tiến trình tăng trong khi quét.
param(
    [int]$Count = 400,
    [int]$KibPerFile = 128
)
$ErrorActionPreference = 'Stop'
if ($Count -lt 1 -or $Count -gt 4000) { throw 'Count phai nam trong 1..4000.' }
if ($KibPerFile -lt 1 -or $KibPerFile -gt 1024) { throw 'KibPerFile phai nam trong 1..1024.' }
Add-Type -AssemblyName System.IO.Compression
$path = Join-Path $PSScriptRoot 'LiveArchiveCounterTest.zip'
if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
$stream = [System.IO.File]::Open($path, [System.IO.FileMode]::CreateNew)
try {
    $zip = New-Object System.IO.Compression.ZipArchive($stream, [System.IO.Compression.ZipArchiveMode]::Create, $false)
    try {
        $rng = New-Object System.Random(21026)
        $buffer = New-Object byte[] (64 * 1024)
        for ($i = 1; $i -le $Count; $i++) {
            $entry = $zip.CreateEntry(('Fixtures/test_{0:D4}.txt' -f $i), [System.IO.Compression.CompressionLevel]::NoCompression)
            $writer = $entry.Open()
            try {
                $remaining = $KibPerFile * 1024
                while ($remaining -gt 0) {
                    $countBytes = [Math]::Min($buffer.Length, $remaining)
                    $rng.NextBytes($buffer)
                    $writer.Write($buffer, 0, $countBytes)
                    $remaining -= $countBytes
                }
            } finally { $writer.Dispose() }
        }
    } finally { $zip.Dispose() }
} finally { $stream.Dispose() }
Write-Host ('Da tao {0} tệp ben trong ZIP: {1}' -f $Count, $path)
Write-Host 'Quet truc tiep file ZIP nay va quan sat bo dem Da kiem tra tang theo moi entry.'
