USE AntivirusDB;
SELECT COUNT_BIG(*) AS TongChuKyDangBat FROM dbo.VirusSignatures WHERE IsActive=1;
SELECT COUNT_BIG(*) AS TongSHA256DangBat FROM dbo.VirusSignatures WHERE IsActive=1 AND SHA256 IS NOT NULL;
SELECT COUNT_BIG(*) AS ChuKyTuFullExport FROM dbo.VirusSignatures
 WHERE Description LIKE N'Nguồn: MalwareBazaar full export%';
SELECT TOP (1) LastDatabaseUpdate FROM dbo.Settings ORDER BY SettingID;
SELECT TOP (20) SignatureID, MalwareName, SHA256, MD5, SHA1, CreatedAt
FROM dbo.VirusSignatures WHERE Description LIKE N'Nguồn: MalwareBazaar full export%'
ORDER BY SignatureID DESC;
