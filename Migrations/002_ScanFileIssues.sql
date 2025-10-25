-- F06: lưu những tệp/thư mục/entry chưa kiểm tra được.
-- Sao lưu AntivirusDB trước khi chạy trên dữ liệu thật.
USE AntivirusDB;
GO
IF OBJECT_ID(N'dbo.ScanFileIssues', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ScanFileIssues
    (
        IssueID BIGINT IDENTITY(1,1) PRIMARY KEY,
        ScanID BIGINT NULL,
        PhysicalPath NVARCHAR(1000) NOT NULL,
        ArchiveEntryPath NVARCHAR(1000) NULL,
        IssueType VARCHAR(40) NOT NULL,
        ErrorSummary NVARCHAR(500) NOT NULL,
        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_ScanFileIssues_CreatedAt DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_ScanFileIssues_ScanID ON dbo.ScanFileIssues(ScanID, IssueID);
END;
GO
