-- Chỉ xem dữ liệu; KHÔNG thay đổi hoặc xóa CSDL.
-- Chạy trong SSMS, chọn đúng SQL Server instance mà app kết nối.
USE AntivirusDB;
GO
SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName,
       ORIGINAL_LOGIN() AS LoginName;
GO
SELECT COUNT_BIG(*) AS TongMau,
       SUM(CASE WHEN IsActive=1 THEN CONVERT(BIGINT,1) ELSE 0 END) AS MauDangBat,
       SUM(CASE WHEN IsActive=1 AND (MD5 IS NOT NULL OR SHA1 IS NOT NULL OR SHA256 IS NOT NULL)
                    THEN CONVERT(BIGINT,1) ELSE 0 END) AS ChuKyHashToanTep,
       SUM(CASE WHEN IsActive=0 THEN CONVERT(BIGINT,1) ELSE 0 END) AS MauBiTat
FROM dbo.VirusSignatures;
GO
SELECT SignatureID, MalwareName, SignatureType, IsActive, MD5, SHA1, SHA256
FROM dbo.VirusSignatures
WHERE MalwareName IN (N'EICAR-Test-File', N'Demo.Hash.SafeFixture')
ORDER BY MalwareName;
GO
SELECT COL_LENGTH('dbo.ChunkSignatureGroups','ChunkingMode') AS ChunkingModeColumn,
       OBJECT_ID('dbo.ChunkSignatures','U') AS ChunkSignaturesTable;
GO
IF OBJECT_ID('dbo.ChunkSignatureGroups','U') IS NOT NULL
AND COL_LENGTH('dbo.ChunkSignatureGroups','ChunkingMode') IS NOT NULL
AND OBJECT_ID('dbo.ChunkSignatures','U') IS NOT NULL
BEGIN
  EXEC(N'SELECT g.GroupID,g.GroupName,g.ChunkingMode,g.IsActive AS GroupActive,
               v.IsActive AS MalwareActive, g.MinMatchCount, g.SimilarityThreshold,
               COUNT(c.ChunkID) AS ActiveChunks,
               CASE WHEN g.IsActive=1 AND v.IsActive=1 AND g.ChunkingMode=''CDC''
                        AND COUNT(c.ChunkID)>=2 THEN ''CHECK PROFILE''
                    ELSE ''KHONG DU DIEU KIEN'' END AS KiemTra
         FROM dbo.ChunkSignatureGroups AS g
         INNER JOIN dbo.VirusSignatures AS v ON v.SignatureID=g.SignatureID
         LEFT JOIN dbo.ChunkSignatures AS c ON c.GroupID=g.GroupID AND c.IsActive=1
         GROUP BY g.GroupID,g.GroupName,g.ChunkingMode,g.IsActive,
                  v.IsActive,g.MinMatchCount,g.SimilarityThreshold
         ORDER BY g.GroupID;');
END
ELSE PRINT N'Chưa có bảng/cột CDC, hãy chạy AntivirusDB.ChunkHash.Migration.sql';
GO
-- Lưu ý: tên tệp và chuỗi cũ KHÔNG còn là dấu hiệu phát hiện;
-- file phải khớp hash toàn tệp hoặc nhóm CDC đã bật.
