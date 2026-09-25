-- Run AFTER AntivirusDB.schema.sql; no changes to the four original tables.
USE AntivirusDB;
GO
IF OBJECT_ID('dbo.DetectionFileMetadata','U') IS NULL
CREATE TABLE dbo.DetectionFileMetadata (
 DetectionID BIGINT NOT NULL PRIMARY KEY REFERENCES dbo.ThreatDetections(DetectionID),
 CreatedAt DATETIME2 NULL, ModifiedAt DATETIME2 NULL, AccessedAt DATETIME2 NULL,
 FileType NVARCHAR(100) NULL, MimeType NVARCHAR(100) NULL, FileAttributes NVARCHAR(200) NULL,
 OwnerName NVARCHAR(260) NULL, AccessPermissions NVARCHAR(200) NULL,
 ComputerName NVARCHAR(260) NULL, OperatingSystem NVARCHAR(260) NULL, UserName NVARCHAR(260) NULL,
 Compiler NVARCHAR(200) NULL, PdbPath NVARCHAR(1000) NULL,
 SignatureStatus NVARCHAR(100) NULL, SignerName NVARCHAR(300) NULL,
 CollectedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO
IF OBJECT_ID('dbo.VirusTotalReports','U') IS NULL
CREATE TABLE dbo.VirusTotalReports (
 ReportID BIGINT IDENTITY(1,1) PRIMARY KEY,
 DetectionID BIGINT NOT NULL REFERENCES dbo.ThreatDetections(DetectionID),
 FileSHA256 CHAR(64) NOT NULL, AnalyzedAt DATETIME2 NULL,
 RetrievedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
 MaliciousCount INT NOT NULL DEFAULT 0, SuspiciousCount INT NOT NULL DEFAULT 0,
 UndetectedCount INT NOT NULL DEFAULT 0, HarmlessCount INT NOT NULL DEFAULT 0,
 TimeoutCount INT NOT NULL DEFAULT 0, FailureCount INT NOT NULL DEFAULT 0,
 ReportUrl NVARCHAR(1000) NULL
);
GO
IF OBJECT_ID('dbo.VirusTotalEngines','U') IS NULL
CREATE TABLE dbo.VirusTotalEngines (
 EngineID BIGINT IDENTITY(1,1) PRIMARY KEY,
 ReportID BIGINT NOT NULL REFERENCES dbo.VirusTotalReports(ReportID),
 EngineName NVARCHAR(200) NOT NULL, Category VARCHAR(30) NOT NULL,
 ResultName NVARCHAR(300) NULL, EngineVersion NVARCHAR(100) NULL, EngineUpdate DATE NULL,
 CONSTRAINT UQ_VTEngine_ReportName UNIQUE (ReportID, EngineName)
);
GO
IF OBJECT_ID('dbo.DetectionBehaviorEvents','U') IS NULL
CREATE TABLE dbo.DetectionBehaviorEvents (
 EventID BIGINT IDENTITY(1,1) PRIMARY KEY,
 DetectionID BIGINT NOT NULL REFERENCES dbo.ThreatDetections(DetectionID),
 OccurredAt DATETIME2 NULL, EventType VARCHAR(50) NOT NULL,
 Title NVARCHAR(200) NOT NULL, Description NVARCHAR(2000) NULL,
 Severity VARCHAR(20) NULL, ProcessName NVARCHAR(260) NULL,
 ProcessId INT NULL, ParentProcessId INT NULL, CommandLine NVARCHAR(MAX) NULL,
 TargetPath NVARCHAR(1000) NULL, RegistryKey NVARCHAR(1000) NULL,
 RemoteAddress VARCHAR(45) NULL, RemotePort INT NULL
);
GO
IF OBJECT_ID('dbo.DetectionStrings','U') IS NULL
CREATE TABLE dbo.DetectionStrings (
 StringID BIGINT IDENTITY(1,1) PRIMARY KEY,
 DetectionID BIGINT NOT NULL REFERENCES dbo.ThreatDetections(DetectionID),
 StringValue NVARCHAR(MAX) NOT NULL, StringType NVARCHAR(100) NULL,
 Assessment VARCHAR(30) NULL, Reason NVARCHAR(1000) NULL,
 ByteOffset BIGINT NULL, ContextBefore NVARCHAR(500) NULL, ContextAfter NVARCHAR(500) NULL
);
GO
IF OBJECT_ID('dbo.DetectionNetworkEvents','U') IS NULL
CREATE TABLE dbo.DetectionNetworkEvents (
 NetworkEventID BIGINT IDENTITY(1,1) PRIMARY KEY,
 DetectionID BIGINT NOT NULL REFERENCES dbo.ThreatDetections(DetectionID),
 BehaviorEventID BIGINT NULL REFERENCES dbo.DetectionBehaviorEvents(EventID),
 ObservedAt DATETIME2 NULL, RemoteIP VARCHAR(45) NULL,
 DomainName NVARCHAR(253) NULL, RemotePort INT NULL,
 Protocol VARCHAR(20) NULL, Country NVARCHAR(100) NULL,
 AsnNumber BIGINT NULL, AsnOrganization NVARCHAR(260) NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_VTReports_Detection' AND object_id=OBJECT_ID('dbo.VirusTotalReports'))
CREATE INDEX IX_VTReports_Detection ON dbo.VirusTotalReports(DetectionID, AnalyzedAt DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Behavior_Detection' AND object_id=OBJECT_ID('dbo.DetectionBehaviorEvents'))
CREATE INDEX IX_Behavior_Detection ON dbo.DetectionBehaviorEvents(DetectionID, OccurredAt, EventID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Strings_Detection' AND object_id=OBJECT_ID('dbo.DetectionStrings'))
CREATE INDEX IX_Strings_Detection ON dbo.DetectionStrings(DetectionID, StringID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Network_Detection' AND object_id=OBJECT_ID('dbo.DetectionNetworkEvents'))
CREATE INDEX IX_Network_Detection ON dbo.DetectionNetworkEvents(DetectionID, ObservedAt);
GO
