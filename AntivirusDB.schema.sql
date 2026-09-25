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
