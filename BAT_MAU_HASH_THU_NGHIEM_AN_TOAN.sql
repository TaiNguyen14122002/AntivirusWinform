-- TUY CHON - chỉ bật chữ ký của tệp thử nghiệm vô hại.
-- Tệp: Tests\MauKiemThuHash-ViDu.txt; kích thước 40 byte (ASCII, không BOM).
-- Không áp dụng cho mã độc thật. Có thể tắt lại bằng UPDATE IsActive=0.
USE AntivirusDB;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS (SELECT 1 FROM dbo.VirusSignatures WHERE MalwareName=N'Demo.Hash.SafeFixture'
           AND (SHA256 IS NULL OR LOWER(LTRIM(RTRIM(SHA256))) <> 'ea9958fec77e564d36bd910ca32d15004e41fdb21bfb7c1dca414da943ee3eeb'))
    THROW 51020, 'Da co demo name nhung hash khac. Khong tu dong ghi de.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.VirusSignatures
               WHERE MalwareName=N'Demo.Hash.SafeFixture')
    INSERT dbo.VirusSignatures
        (MalwareName, MalwareFamily, Category, SignatureType, SHA256, Severity,
         Description, RecommendedAction, IsActive)
    VALUES (N'Demo.Hash.SafeFixture',N'Test',N'Test','Hash',
            'ea9958fec77e564d36bd910ca32d15004e41fdb21bfb7c1dca414da943ee3eeb','Low',
            N'Tep kiem thu vo hai 40 byte. Chi su dung de xac minh phuong phap bam.',
            'Quarantine', 0);
UPDATE dbo.VirusSignatures
SET IsActive=1, UpdatedAt=SYSDATETIME()
WHERE MalwareName=N'Demo.Hash.SafeFixture' AND LOWER(LTRIM(RTRIM(SHA256)))='ea9958fec77e564d36bd910ca32d15004e41fdb21bfb7c1dca414da943ee3eeb';
COMMIT TRANSACTION;
SELECT SignatureID, MalwareName, SHA256, IsActive
FROM dbo.VirusSignatures WHERE MalwareName=N'Demo.Hash.SafeFixture';
GO
