USE AntivirusDB;
GO
-- Read-only verification of recently imported signatures.
SELECT TOP (100) SignatureID, MalwareName, MalwareFamily, Category,
       MD5, SHA1, SHA256, FileExtension, IsActive, CreatedAt
FROM dbo.VirusSignatures
WHERE Description LIKE N'%Danh sách hash do người dùng nhập%'
ORDER BY SignatureID DESC;
GO
-- Summary (active / inactive)
SELECT IsActive, COUNT_BIG(*) AS SoChuKy
FROM dbo.VirusSignatures
WHERE Description LIKE N'%Danh sách hash do người dùng nhập%'
GROUP BY IsActive;
