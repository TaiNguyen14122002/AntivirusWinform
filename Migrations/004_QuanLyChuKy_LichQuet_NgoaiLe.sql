USE AntivirusDB;
GO
-- Chạy sau khi sao lưu. Migration có thể chạy lại, không xóa dữ liệu hiện có.
IF COL_LENGTH('dbo.VirusSignatures', 'SourceName') IS NULL
    ALTER TABLE dbo.VirusSignatures ADD SourceName NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.VirusSignatures', 'VerificationStatus') IS NULL
    ALTER TABLE dbo.VirusSignatures ADD VerificationStatus VARCHAR(30) NOT NULL CONSTRAINT DF_AV_VerificationStatus DEFAULT 'Legacy';
GO
IF COL_LENGTH('dbo.VirusSignatures', 'VerifiedBy') IS NULL
    ALTER TABLE dbo.VirusSignatures ADD VerifiedBy NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.VirusSignatures', 'VerifiedAt') IS NULL
    ALTER TABLE dbo.VirusSignatures ADD VerifiedAt DATETIME2 NULL;
GO
IF OBJECT_ID('dbo.SignatureAuditLogs','U') IS NULL
BEGIN
 CREATE TABLE dbo.SignatureAuditLogs (
  AuditID BIGINT IDENTITY(1,1) PRIMARY KEY,
  SignatureID BIGINT NOT NULL,
  ActionType VARCHAR(40) NOT NULL,
  OldValue NVARCHAR(1000) NULL,
  NewValue NVARCHAR(1000) NULL,
  Reason NVARCHAR(1000) NULL,
  ChangedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  ChangedBy NVARCHAR(100) NULL,
  CONSTRAINT FK_SignatureAuditLogs_VirusSignatures FOREIGN KEY (SignatureID)
   REFERENCES dbo.VirusSignatures(SignatureID)
 );
END;
GO
IF OBJECT_ID('dbo.ScanSchedules','U') IS NULL
BEGIN
 CREATE TABLE dbo.ScanSchedules (
  ScheduleID BIGINT IDENTITY(1,1) PRIMARY KEY,
  Name NVARCHAR(150) NOT NULL,
  ScanType VARCHAR(20) NOT NULL,
  TargetPath NVARCHAR(1000) NULL,
  Frequency VARCHAR(20) NOT NULL,
  StartTime DATETIME2 NOT NULL,
  DayOfWeek INT NULL,
  DayOfMonth INT NULL,
  LastRunAt DATETIME2 NULL,
  LastOutcome NVARCHAR(500) NULL,
  IsEnabled BIT NOT NULL DEFAULT 1,
  CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
 );
END;
GO
IF OBJECT_ID('dbo.ScanExclusions','U') IS NULL
BEGIN
 CREATE TABLE dbo.ScanExclusions (
  ExclusionID BIGINT IDENTITY(1,1) PRIMARY KEY,
  SHA256 CHAR(64) NOT NULL,
  FilePath NVARCHAR(1000) NOT NULL,
  Scope VARCHAR(30) NOT NULL DEFAULT 'Heuristic',
  Reason NVARCHAR(500) NOT NULL,
  CreatedBy NVARCHAR(128) NOT NULL,
  CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  ExpiresAtUtc DATETIME2 NOT NULL,
  IsActive BIT NOT NULL DEFAULT 1
 );
 CREATE INDEX IX_ScanExclusions_Hash ON dbo.ScanExclusions(SHA256,IsActive,ExpiresAtUtc);
END;
GO
IF OBJECT_ID('dbo.SecurityHealthChecks','U') IS NULL
BEGIN
 CREATE TABLE dbo.SecurityHealthChecks (
  CheckID BIGINT IDENTITY(1,1) PRIMARY KEY,
  CheckedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  CheckType NVARCHAR(80) NOT NULL,
  Status VARCHAR(20) NOT NULL,
  Details NVARCHAR(1000) NULL
 );
END;
GO
