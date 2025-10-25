-- Chay tren AntivirusDB sau khi BACKUP. Migration bo sung F03.
USE AntivirusDB;
GO
IF OBJECT_ID(N'dbo.ScanSignatureMetadata', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ScanSignatureMetadata
    (
        ScanID BIGINT NOT NULL PRIMARY KEY,
        SignatureSource VARCHAR(30) NOT NULL,
        SnapshotCreatedAtUtc DATETIME2 NULL,
        IsDegraded BIT NOT NULL CONSTRAINT DF_ScanSignatureMetadata_IsDegraded DEFAULT (0),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_ScanSignatureMetadata_CreatedAtUtc DEFAULT SYSUTCDATETIME()
    );
END;
GO
-- Khong them FK truoc khi xac nhan schema/ID cua ScanHistory.
