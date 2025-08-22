create database AntivirusDB
go

use AntivirusDB
go
--*Tao bang*--
create table Settings
(
    SettingID INT IDENTITY(1,1) PRIMARY KEY,
    ProgramName NVARCHAR(100) NOT NULL DEFAULT N'ANTIVIRUS',
    ProgramVersion VARCHAR(20) NOT NULL DEFAULT '1.0.0.0',
    VirusDatabaseVersion VARCHAR(30) NOT NULL DEFAULT '1.0.0.2025',
    LastDatabaseUpdate DATETIME2 NULL,

    RealTimeProtection BIT NOT NULL DEFAULT 1,
    FileProtection BIT NOT NULL DEFAULT 1,
    USBProtection BIT NOT NULL DEFAULT 1,
    WebProtection BIT NOT NULL DEFAULT 1,
    DownloadProtection BIT NOT NULL DEFAULT 1,
    RansomwareProtection BIT NOT NULL DEFAULT 1,

    AutoStart BIT NOT NULL DEFAULT 1,
    AutoUpdate BIT NOT NULL DEFAULT 1,
    SubmitSamples BIT NOT NULL DEFAULT 0,
    ShowNotifications BIT NOT NULL DEFAULT 1,

    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
)
go

create table VirusSignatures
(
    SignatureID BIGINT IDENTITY(1,1) PRIMARY KEY,
    MalwareName NVARCHAR(200) NOT NULL,
    MalwareFamily NVARCHAR(100) NULL,
    Category NVARCHAR(100) NULL,
    SignatureType VARCHAR(30) NOT NULL,
    MD5 CHAR(32) NULL,
    SHA1 CHAR(40) NULL,
    SHA256 CHAR(64) NULL,
    FileExtension NVARCHAR(50) NULL,
    Severity VARCHAR(20) NOT NULL DEFAULT 'Medium',
    Description NVARCHAR(1000) NULL,
    RecommendedAction VARCHAR(30) NOT NULL DEFAULT 'Quarantine',
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NULL
)
go

--*Index phuc vu tra cuu hash khi quet*--
create index IX_VirusSignatures_MD5 on VirusSignatures(MD5);
create index IX_VirusSignatures_SHA1 on VirusSignatures(SHA1);
create index IX_VirusSignatures_SHA256 on VirusSignatures(SHA256);
go

create table ScanHistory
(
    ScanID BIGINT IDENTITY(1,1) PRIMARY KEY,
    ScanType VARCHAR(20) NOT NULL,
    StartedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL,
    ScanLocation NVARCHAR(1000) NULL,
    FilesScanned BIGINT NOT NULL DEFAULT 0,
    ThreatCount INT NOT NULL DEFAULT 0,
    ResultStatus VARCHAR(30) NOT NULL DEFAULT 'Running',
    ActivityTitle NVARCHAR(200) NULL,
    ActivityDetails NVARCHAR(1000) NULL,
    ErrorMessage NVARCHAR(1000) NULL
)
go

create table ThreatDetections
(
    DetectionID BIGINT IDENTITY(1,1) PRIMARY KEY,
    ScanID BIGINT NULL,
    SignatureID BIGINT NULL,
    FileName NVARCHAR(260) NOT NULL,
    OriginalPath NVARCHAR(1000) NOT NULL,
    ThreatName NVARCHAR(200) NOT NULL,
    DetectedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FileSizeBytes BIGINT NULL,
    FileMD5 CHAR(32) NULL,
    FileSHA1 CHAR(40) NULL,
    FileSHA256 CHAR(64) NULL,
    ActionTaken VARCHAR(30) NOT NULL DEFAULT 'Quarantined',
    Status VARCHAR(30) NOT NULL DEFAULT 'Quarantined',
    QuarantinePath NVARCHAR(1000) NULL,
    RestoredAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL,
    CONSTRAINT FK_ThreatDetections_Scan
        FOREIGN KEY (ScanID)
        REFERENCES ScanHistory(ScanID),
    CONSTRAINT FK_ThreatDetections_Signature
        FOREIGN KEY (SignatureID)
        REFERENCES VirusSignatures(SignatureID)
)
go

create index IX_ThreatDetections_DetectedAt on ThreatDetections(DetectedAt DESC);
create index IX_ThreatDetections_Status on ThreatDetections(Status);
create index IX_ThreatDetections_SHA256 on ThreatDetections(FileSHA256);
go

--*Code them du lieu vao bang*--
INSERT INTO dbo.Settings
    (
    ProgramName,
    ProgramVersion,
    VirusDatabaseVersion,
    LastDatabaseUpdate,

    RealTimeProtection,
    FileProtection,
    USBProtection,
    WebProtection,
    DownloadProtection,
    RansomwareProtection,

    AutoStart,
    AutoUpdate,
    SubmitSamples,
    ShowNotifications
    )
VALUES
    (
        N'ANTIVIRUS',
        '1.0.0.0',
        '1.0.0.2025',
        '2025-08-20 08:30:00',
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        0,
        1
);
GO

INSERT INTO dbo.VirusSignatures
    (
    MalwareName,
    MalwareFamily,
    Category,
    SignatureType,
    MD5,
    SHA1,
    SHA256,
    FileExtension,
    Severity,
    Description,
    RecommendedAction,
    IsActive
    )
VALUES
    (
        N'EICAR-Test-File',
        N'EICAR',
        N'Test',
        'Hash',
        '44D88612FEA8A8F36DE82E1278ABB02F',
        '3395856CE81F2B7382DEE72602F798B642F14140',
        NULL,
        N'.com',
        'Low',
        N'Tệp kiểm tra chức năng phát hiện của antivirus.',
        'Quarantine',
        1
),
    (
        N'Trojan.GenericKD.123456',
        N'Generic Trojan',
        N'Trojan',
        'Hash',
        NULL,
        NULL,
        '1111111111111111111111111111111111111111111111111111111111111111',
        N'.exe',
        'High',
        N'Mẫu dữ liệu giả lập cho Trojan.',
        'Quarantine',
        1
),
    (
        N'HackTool.Keygen.8910',
        N'Keygen',
        N'HackTool',
        'Hash',
        NULL,
        NULL,
        '2222222222222222222222222222222222222222222222222222222222222222',
        N'.zip',
        'High',
        N'Mẫu dữ liệu giả lập cho công cụ keygen.',
        'Quarantine',
        1
),
    (
        N'Adware.InstallCore.2345',
        N'InstallCore',
        N'Adware',
        'Hash',
        NULL,
        NULL,
        '3333333333333333333333333333333333333333333333333333333333333333',
        N'.scr',
        'Medium',
        N'Mẫu dữ liệu giả lập cho adware.',
        'Quarantine',
        1
);
GO

INSERT INTO dbo.ScanHistory
    (
    ScanType,
    StartedAt,
    CompletedAt,
    ScanLocation,
    FilesScanned,
    ThreatCount,
    ResultStatus,
    ActivityTitle,
    ActivityDetails
    )
VALUES
    (
        'QUICK',
        '2025-08-20 08:25:00',
        '2025-08-20 08:25:12',
        N'Các khu vực quan trọng',
        125430,
        0,
        'Completed',
        N'Quét nhanh',
        N'Quét nhanh hoàn tất. Không phát hiện mối đe dọa.'
),
    (
        'FULL',
        '2025-08-19 22:10:00',
        '2025-08-19 22:44:21',
        N'Toàn bộ ổ đĩa và hệ thống',
        982340,
        1,
        'Completed',
        N'Quét toàn bộ',
        N'Quét hoàn tất. Đã phát hiện 1 mối đe dọa.'
),
    (
        'CUSTOM',
        '2025-08-18 09:15:00',
        '2025-08-18 09:20:47',
        N'C:\Users\Admin\Downloads',
        35420,
        0,
        'Completed',
        N'Quét tùy chọn',
        N'Quét thư mục Downloads hoàn tất. Không phát hiện mối đe dọa.'
),
    (
        'QUICK',
        '2025-08-17 14:03:00',
        '2025-08-17 14:03:11',
        N'Các khu vực quan trọng',
        125430,
        0,
        'Completed',
        N'Quét nhanh',
        N'Quét nhanh hoàn tất. Hệ thống an toàn.'
),
    (
        'FULL',
        '2025-08-16 10:20:00',
        '2025-08-16 11:02:18',
        N'Toàn bộ ổ đĩa và hệ thống',
        1105420,
        3,
        'Completed',
        N'Quét toàn bộ',
        N'Quét hoàn tất. Đã phát hiện 3 mối đe dọa.'
);
GO

INSERT INTO dbo.ThreatDetections
    (
    ScanID,
    SignatureID,
    FileName,
    OriginalPath,
    ThreatName,
    DetectedAt,
    FileSizeBytes,
    FileMD5,
    FileSHA1,
    FileSHA256,
    ActionTaken,
    Status,
    QuarantinePath
    )
VALUES
    (
        2,
        1,
        N'eicar.com',
        N'C:\Users\Admin\Downloads\eicar.com',
        N'EICAR-Test-File',
        '2025-08-20 08:25:10',
        68,
        '44D88612FEA8A8F36DE82E1278ABB02F',
        '3395856CE81F2B7382DEE72602F798B642F14140',
        NULL,
        'Quarantined',
        'Quarantined',
        N'C:\ProgramData\Antivirus\Quarantine\1'
),
    (
        2,
        2,
        N'setup_fake.exe',
        N'D:\Setup\setup_fake.exe',
        N'Trojan.GenericKD.123456',
        '2025-08-19 22:10:33',
        2572288,
        NULL,
        NULL,
        '1111111111111111111111111111111111111111111111111111111111111111',
        'Quarantined',
        'Quarantined',
        N'C:\ProgramData\Antivirus\Quarantine\2'
),
    (
        3,
        3,
        N'keygen.zip',
        N'C:\Users\Admin\Desktop\keygen.zip',
        N'HackTool.Keygen.8910',
        '2025-08-18 09:15:27',
        1174405,
        NULL,
        NULL,
        '2222222222222222222222222222222222222222222222222222222222222222',
        'Quarantined',
        'Quarantined',
        N'C:\ProgramData\Antivirus\Quarantine\3'
),
    (
        5,
        4,
        N'Invoice.scr',
        N'C:\Users\Admin\Downloads\Invoice.scr',
        N'Adware.InstallCore.2345',
        '2025-08-16 14:05:02',
        524288,
        NULL,
        NULL,
        '3333333333333333333333333333333333333333333333333333333333333333',
        'Quarantined',
        'Quarantined',
        N'C:\ProgramData\Antivirus\Quarantine\4'
);
GO

--*Code truy van*--
SELECT
    SignatureID,
    MalwareName,
    MalwareFamily,
    Category,
    SignatureType,
    MD5,
    SHA1,
    SHA256,
    Severity,
    RecommendedAction,
    IsActive
FROM dbo.VirusSignatures;

SELECT
    ScanID,
    ScanType,
    StartedAt,
    CompletedAt,
    ScanLocation,
    FilesScanned,
    ThreatCount,
    ResultStatus
FROM dbo.ScanHistory
ORDER BY StartedAt DESC;

SELECT
    DetectionID,
    ScanID,
    SignatureID,
    FileName,
    OriginalPath,
    ThreatName,
    DetectedAt,
    ActionTaken,
    Status,
    QuarantinePath
FROM dbo.ThreatDetections
ORDER BY DetectedAt DESC;
GO

--*Code doi chieu HASH khi quet*--
DECLARE @SHA256 CHAR(64);
SET @SHA256 =
    '1111111111111111111111111111111111111111111111111111111111111111';
SELECT TOP 1
    SignatureID,
    MalwareName,
    MalwareFamily,
    Category,
    Severity,
    RecommendedAction,
    Description
FROM dbo.VirusSignatures
WHERE SHA256 = @SHA256
  AND IsActive = 1;
