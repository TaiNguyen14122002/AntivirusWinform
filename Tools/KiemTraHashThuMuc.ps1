# Windows PowerShell 5.1 / .NET Framework 4.7.2. Read-only diagnostics.
# Do not execute suspicious samples or insert their hashes into malware DB.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$Folder,
    [string]$Server = 'localhost\SQLEXPRESS',
    [string]$Database = 'AntivirusDB'
)
$ErrorActionPreference = 'Stop'
function Get-DigestHex([string]$Path, [System.Security.Cryptography.HashAlgorithm]$Hasher) {
    $stream = $null
    try {
        $stream = New-Object System.IO.FileStream($Path,
            [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read,
            ([System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete),
            262144, [System.IO.FileOptions]::SequentialScan)
        return [BitConverter]::ToString($Hasher.ComputeHash($stream)).Replace('-', '')
    } finally {
        if ($stream -ne $null) { $stream.Dispose() }
        $Hasher.Dispose()
    }
}
if (-not (Test-Path -LiteralPath $Folder -PathType Container)) {
    throw 'Khong tim thay thu muc: ' + $Folder
}
$Folder = (Resolve-Path -LiteralPath $Folder).ProviderPath
$cs = "Server=$Server;Database=$Database;Integrated Security=True;Connect Timeout=10;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($cs)
try {
    $conn.Open()
    Write-Host ('SQL Server: ' + $conn.DataSource + ' / Database: ' + $conn.Database)
    $countCmd = $conn.CreateCommand()
    $countCmd.CommandText = "SELECT COUNT_BIG(*) FROM dbo.VirusSignatures WHERE IsActive=1 AND (SHA256 IS NOT NULL OR SHA1 IS NOT NULL OR MD5 IS NOT NULL)"
    Write-Host ('Chu ky hash toan tep duoc bat trong SQL: ' + $countCmd.ExecuteScalar())

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @'
SELECT TOP (1) SignatureID, MalwareName,
       CASE WHEN SHA256 COLLATE Latin1_General_CI_AS = @SHA256 THEN 'SHA-256'
            WHEN SHA1 COLLATE Latin1_General_CI_AS = @SHA1 THEN 'SHA-1'
            ELSE 'MD5' END AS MatchedBy
FROM dbo.VirusSignatures
WHERE IsActive = 1 AND (
    SHA256 COLLATE Latin1_General_CI_AS = @SHA256 OR
    SHA1 COLLATE Latin1_General_CI_AS = @SHA1 OR
    MD5 COLLATE Latin1_General_CI_AS = @MD5)
ORDER BY CASE WHEN SHA256 COLLATE Latin1_General_CI_AS = @SHA256 THEN 1
              WHEN SHA1 COLLATE Latin1_General_CI_AS = @SHA1 THEN 2 ELSE 3 END;
'@
    $null = $cmd.Parameters.Add('@SHA256', [System.Data.SqlDbType]::Char, 64)
    $null = $cmd.Parameters.Add('@SHA1', [System.Data.SqlDbType]::Char, 40)
    $null = $cmd.Parameters.Add('@MD5', [System.Data.SqlDbType]::Char, 32)
    $cmd.CommandTimeout = 30
    $rows = New-Object 'System.Collections.Generic.List[object]'
    $errors = @()
    $match = 0
    $unmatched = 0
    $unreadable = 0
    Get-ChildItem -LiteralPath $Folder -File -Recurse -Force -ErrorAction SilentlyContinue -ErrorVariable +errors | ForEach-Object {
        $file = $_
        $record = [ordered]@{
            File = $file.FullName
            SizeBytes = $file.Length
            SHA256 = ''
            SHA1 = ''
            MD5 = ''
            Status = ''
            SignatureID = ''
            MalwareName = ''
            MatchedBy = ''
            Error = ''
        }
        try {
            $record.SHA256 = Get-DigestHex $file.FullName ([System.Security.Cryptography.SHA256]::Create())
            $record.SHA1 = Get-DigestHex $file.FullName ([System.Security.Cryptography.SHA1]::Create())
            $record.MD5 = Get-DigestHex $file.FullName ([System.Security.Cryptography.MD5]::Create())
            $cmd.Parameters['@SHA256'].Value = $record.SHA256
            $cmd.Parameters['@SHA1'].Value = $record.SHA1
            $cmd.Parameters['@MD5'].Value = $record.MD5
            $reader = $cmd.ExecuteReader()
            try {
                if ($reader.Read()) {
                    $record.Status = 'MATCH_HASH_TOAN_TEP'
                    $record.SignatureID = [string]$reader.GetInt64(0)
                    $record.MalwareName = $reader.GetString(1)
                    $record.MatchedBy = $reader.GetString(2)
                    $match++
                } else {
                    $record.Status = 'KHONG_CO_HASH_KHOP_SQL'
                    $unmatched++
                }
            } finally { $reader.Dispose() }
        } catch {
            $record.Status = 'LOI_DOC_HOAC_DOI_CHIEU'
            $record.Error = $_.Exception.Message
            $unreadable++
        }
        $rows.Add([pscustomobject]$record)
        Write-Host ('[' + $record.Status + '] ' + $file.Name + $(if ($record.SignatureID) { ' ID=' + $record.SignatureID } else { '' }))
    }
    $fileName = 'BaoCao_Hash_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.csv'
    $out = Join-Path (Split-Path -Parent $PSScriptRoot) $fileName
    if ($rows.Count -gt 0) {
        $rows | Export-Csv -LiteralPath $out -NoTypeInformation -Encoding UTF8
    }
    Write-Host ''
    Write-Host ('TONG: ' + $rows.Count + ' tep. Hash khop: ' + $match + '; Khong khop: ' + $unmatched + '; Loi: ' + $unreadable)
    if ($errors.Count -gt 0) {
        Write-Warning ('Co ' + $errors.Count + ' loi liet ke thu muc. Co the con tep chua duoc kiem tra.')
    }
    Write-Host ('Bao cao: ' + $out)
    Write-Host 'Chu y: KHONG KHOP hash toan tep khong co nghia la tep an toan. CDC duoc kiem tra rieng trong engine.'
} finally {
    if ($conn.State -ne [System.Data.ConnectionState]::Closed) { $conn.Close() }
    $conn.Dispose()
}
