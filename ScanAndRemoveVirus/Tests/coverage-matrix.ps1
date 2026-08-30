$src = Join-Path $PSScriptRoot "..\ScanAndRemoveVirus"
if (-not (Test-Path $src)) { $src = "ScanAndRemoveVirus\ScanAndRemoveVirus" }
$test = Get-Content (Join-Path $PSScriptRoot "UiEndToEnd.cs") -Raw
$lines = $test -split "`n"
$btns = Select-String -Path (Join-Path $src "Control\*.Designer.cs"),(Join-Path $src "FrmMain.Designer.cs") `
        -Pattern "private System\.Windows\.Forms\.Button (\w+);" |
        ForEach-Object { $_.Matches[0].Groups[1].Value } | Sort-Object -Unique
foreach ($n in $btns) {
    $clickLine = $lines | Where-Object { $_ -match [regex]::Escape($n) -and ($_ -match "PerformClick|OnClick|\.Click \+") } | Select-Object -First 1
    if ($clickLine) { "CLICKED    $n" }
    else { "NOT-CLICK  $n" }
}
