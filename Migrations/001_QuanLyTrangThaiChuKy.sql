-- F02: ÁP DỤNG SAU KHI SAO LƯU DATABASE AntivirusDB.
-- Không tự động tắt chữ ký cũ: mọi bản ghi hiện có được gán trạng thái Legacy.
USE AntivirusDB;
GO
IF OBJECT_ID(N'dbo.VirusSignatures', N'U') IS NULL
    THROW 51000, 'Thieu bang dbo.VirusSignatures.', 1;
GO
IF COL_LENGTH('dbo.VirusSignatures', 'SourceName') IS NULL
    ALTER TABLE dbo.VirusSignatures ADD SourceName NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.VirusSignatures', 'VerificationStatus') IS NULL
    ALTER TABLE dbo.VirusSignatures ADD VerificationStatus VARCHAR(30) NOT NULL
        CONSTRAINT DF_VirusSignatures_VerificationStatus DEFAULT 'Legacy';
GO
IF COL_LENGTH('dbo.VirusSignatures', 'VerifiedAt') IS NULL
    ALTER TABLE dbo.VirusSignatures ADD VerifiedAt DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.VirusSignatures', 'VerifiedBy') IS NULL
    ALTER TABLE dbo.VirusSignatures ADD VerifiedBy NVARCHAR(100) NULL;
GO
IF OBJECT_ID(N'dbo.SignatureAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SignatureAuditLogs
    (
        AuditID BIGINT IDENTITY(1,1) PRIMARY KEY,
        SignatureID BIGINT NOT NULL,
        ActionType VARCHAR(40) NOT NULL,
        OldValue NVARCHAR(1000) NULL,
        NewValue NVARCHAR(1000) NULL,
        Reason NVARCHAR(1000) NULL,
        ChangedAt DATETIME2 NOT NULL CONSTRAINT DF_SignatureAuditLogs_ChangedAt DEFAULT SYSUTCDATETIME(),
        ChangedBy NVARCHAR(100) NULL,
        CONSTRAINT FK_SignatureAuditLogs_VirusSignatures
            FOREIGN KEY (SignatureID) REFERENCES dbo.VirusSignatures(SignatureID)
    );
END;
GO
-- Các bản ghi cũ vẫn giữ IsActive hiện tại. Cần rà soát trước khi duyệt hàng loạt.
SELECT VerificationStatus, IsActive, COUNT_BIG(*) AS SignatureCount
FROM dbo.VirusSignatures
GROUP BY VerificationStatus, IsActive;
GO
