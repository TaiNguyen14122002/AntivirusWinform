-- CHẠY 1 LẦN TRÊN DATABASE AntivirusDB ĐANG SỬ DỤNG.
-- KHÔNG CHẠY FILE AntivirusDB.sql (CREATE DATABASE) TRÊN DATABASE ĐÃ CÓ DỮ LIỆU.
USE AntivirusDB;
GO
IF OBJECT_ID(N'dbo.VirusSignatures', N'U') IS NULL
    THROW 51000, 'Chua co dbo.VirusSignatures. Cai dat schema goc truoc.', 1;
GO
IF OBJECT_ID(N'dbo.ChunkSignatureGroups', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChunkSignatureGroups
    (
        GroupID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SignatureID BIGINT NOT NULL,
        GroupName NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        MinMatchCount INT NOT NULL CONSTRAINT DF_ChunkGroups_MinMatch DEFAULT(2),
        MaxGapBytes INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ChunkGroups_IsActive DEFAULT(1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ChunkGroups_Created DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_ChunkGroups_Virus FOREIGN KEY (SignatureID)
            REFERENCES dbo.VirusSignatures(SignatureID),
        CONSTRAINT CK_ChunkGroups_MinMatch CHECK (MinMatchCount >= 1),
        CONSTRAINT CK_ChunkGroups_MaxGap CHECK (MaxGapBytes IS NULL OR MaxGapBytes >= 0)
    );
END;
GO
IF OBJECT_ID(N'dbo.ChunkSignatures', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChunkSignatures
    (
        ChunkID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        GroupID BIGINT NOT NULL,
        ChunkHash CHAR(64) NOT NULL,
        FastHash VARBINARY(8) NULL, -- rolling fingerprint of the original chunk bytes
        ChunkSize INT NOT NULL,
        SequenceOrder INT NOT NULL,
        OriginalOffset BIGINT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Chunks_IsActive DEFAULT(1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Chunks_Created DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_Chunks_Group FOREIGN KEY (GroupID)
            REFERENCES dbo.ChunkSignatureGroups(GroupID),
        CONSTRAINT CK_Chunks_Size CHECK (ChunkSize > 0),
        CONSTRAINT CK_Chunks_Order CHECK (SequenceOrder >= 1),
        CONSTRAINT CK_Chunks_Hash CHECK
            (LEN(ChunkHash) = 64 AND ChunkHash COLLATE Latin1_General_BIN2 NOT LIKE '%[^0-9a-f]%'),
        CONSTRAINT CK_Chunks_Offset CHECK (OriginalOffset IS NULL OR OriginalOffset >= 0),
        CONSTRAINT UQ_Chunks_GroupOrder UNIQUE (GroupID, SequenceOrder)
    );
END;
GO
-- Không thể tính FastHash chỉ từ SHA-256. Cần lấy lại byte gốc từng chunk.
-- Tương thích chữ ký cũ: FastHash NULL => vẫn dò streaming, nhưng chậm hơn.
IF COL_LENGTH(N'dbo.ChunkSignatures', N'FastHash') IS NULL
    ALTER TABLE dbo.ChunkSignatures ADD FastHash VARBINARY(8) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Chunks_Hash' AND object_id = OBJECT_ID(N'dbo.ChunkSignatures'))
BEGIN
    CREATE INDEX IX_Chunks_Hash ON dbo.ChunkSignatures(ChunkHash, ChunkSize)
        INCLUDE (GroupID, SequenceOrder) WHERE IsActive = 1;
END;
GO
-- Kiểm tra hai bảng và số dòng hiện tại.
SELECT 'ChunkSignatureGroups' AS Bang, COUNT_BIG(*) AS SoBanGhi FROM dbo.ChunkSignatureGroups
UNION ALL
SELECT 'ChunkSignatures', COUNT_BIG(*) FROM dbo.ChunkSignatures;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Chunks_FastHash_Size' AND object_id = OBJECT_ID(N'dbo.ChunkSignatures'))
BEGIN
    CREATE INDEX IX_Chunks_FastHash_Size
        ON dbo.ChunkSignatures(FastHash, ChunkSize)
        INCLUDE (GroupID, SequenceOrder, ChunkHash)
        WHERE IsActive = 1 AND FastHash IS NOT NULL;
END;
GO
SELECT COUNT_BIG(*) AS FastHashReady FROM dbo.ChunkSignatures WHERE IsActive = 1 AND FastHash IS NOT NULL;
SELECT COUNT_BIG(*) AS LegacyNeedsFingerprint FROM dbo.ChunkSignatures WHERE IsActive = 1 AND FastHash IS NULL;
GO

-- Engine chỉ quét CDC; các nhóm FixedWindow cũ được GIỮ TRONG SQL nhưng BỎ QUA khi quét.
-- Không đổi mode của nhóm cũ sang CDC: phải tái tạo từ byte gốc đã xác minh.
IF COL_LENGTH(N'dbo.ChunkSignatureGroups', N'ChunkingMode') IS NULL
    ALTER TABLE dbo.ChunkSignatureGroups ADD ChunkingMode VARCHAR(20) NOT NULL
        CONSTRAINT DF_ChunkGroups_ChunkingMode DEFAULT('FixedWindow');
GO
IF COL_LENGTH(N'dbo.ChunkSignatureGroups', N'RollingWindowBytes') IS NULL
    ALTER TABLE dbo.ChunkSignatureGroups ADD RollingWindowBytes INT NULL;
IF COL_LENGTH(N'dbo.ChunkSignatureGroups', N'MinChunkSize') IS NULL
    ALTER TABLE dbo.ChunkSignatureGroups ADD MinChunkSize INT NULL;
IF COL_LENGTH(N'dbo.ChunkSignatureGroups', N'AvgChunkSize') IS NULL
    ALTER TABLE dbo.ChunkSignatureGroups ADD AvgChunkSize INT NULL;
IF COL_LENGTH(N'dbo.ChunkSignatureGroups', N'MaxChunkSize') IS NULL
    ALTER TABLE dbo.ChunkSignatureGroups ADD MaxChunkSize INT NULL;
IF COL_LENGTH(N'dbo.ChunkSignatureGroups', N'BoundaryMaskBits') IS NULL
    ALTER TABLE dbo.ChunkSignatureGroups ADD BoundaryMaskBits INT NULL;
IF COL_LENGTH(N'dbo.ChunkSignatureGroups', N'SimilarityThreshold') IS NULL
    ALTER TABLE dbo.ChunkSignatureGroups ADD SimilarityThreshold DECIMAL(5,4) NULL;
GO
-- Bounds for the new mode.  FixedWindow entries are not modified.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name='CK_ChunkGroups_CDC_Mode' AND parent_object_id=OBJECT_ID('dbo.ChunkSignatureGroups'))
    ALTER TABLE dbo.ChunkSignatureGroups ADD CONSTRAINT CK_ChunkGroups_CDC_Mode
        CHECK (ChunkingMode IN ('FixedWindow','CDC'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name='CK_ChunkGroups_CDC_Parameters' AND parent_object_id=OBJECT_ID('dbo.ChunkSignatureGroups'))
    ALTER TABLE dbo.ChunkSignatureGroups ADD CONSTRAINT CK_ChunkGroups_CDC_Parameters
        CHECK (ChunkingMode <> 'CDC' OR (
            MinMatchCount >= 2 AND
            (RollingWindowBytes IS NULL OR RollingWindowBytes BETWEEN 8 AND 128) AND
            (MinChunkSize IS NULL OR MinChunkSize BETWEEN 512 AND 262144) AND
            (AvgChunkSize IS NULL OR AvgChunkSize BETWEEN 512 AND 262144) AND
            (MaxChunkSize IS NULL OR MaxChunkSize BETWEEN 512 AND 262144) AND
            (BoundaryMaskBits IS NULL OR BoundaryMaskBits BETWEEN 6 AND 20) AND
            (SimilarityThreshold IS NULL OR SimilarityThreshold BETWEEN 0.2 AND 1.0)
        ));
GO
SELECT ChunkingMode, COUNT_BIG(*) AS SoNhom FROM dbo.ChunkSignatureGroups GROUP BY ChunkingMode;
GO
