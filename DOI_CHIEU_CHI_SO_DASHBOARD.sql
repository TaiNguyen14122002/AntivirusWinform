-- CHI DOC / SELECT. KHONG ALTER, INSERT, UPDATE, DELETE BAT KY BANG NAO.
-- Chon cung khoang ngay voi trang Quan tri de doi chieu ket qua.
USE AntivirusDB;
GO
DECLARE @TuNgay datetime2 = DATEADD(day, -6, CONVERT(date, GETDATE()));
DECLARE @DenNgayDocQuyen datetime2 = DATEADD(day, 1, CONVERT(date, GETDATE()));

-- 1. Tong luot tep va thoi gian quet trung binh tu CAC PHIEN QUET HOAN TAT.
-- Moi tep duoc quet lai o phien khac se duoc tinh them 1 luot.
;WITH PhienHoanTat AS (
    SELECT ScanID, FilesScanned, StartedAt, CompletedAt,
           DATEDIFF_BIG(MILLISECOND, StartedAt, CompletedAt) / 1000.0 AS SoGiay
    FROM dbo.ScanHistory
    WHERE StartedAt >= @TuNgay AND StartedAt < @DenNgayDocQuyen
      AND CompletedAt IS NOT NULL AND CompletedAt >= StartedAt
      AND LOWER(LTRIM(RTRIM(ISNULL(ResultStatus, '')))) NOT IN
          ('running', 'failed', 'error', 'cancelled', 'canceled', 'aborted', 'interrupted', 'stopped')
      AND LOWER(LTRIM(RTRIM(ISNULL(ScanType, '')))) NOT IN
          ('update', 'behavior', 'realtime', 'real-time')
      AND LOWER(ISNULL(ActivityTitle, '')) NOT LIKE N'%cập nhật csdl%'
      AND LOWER(ISNULL(ActivityTitle, '')) NOT LIKE N'%bảo vệ thời gian thực%'
      AND LOWER(ISNULL(ActivityTitle, '')) NOT LIKE N'%phát hiện hành vi%'
)
SELECT COUNT_BIG(*) AS SoPhienHoanTat,
       COALESCE(SUM(CASE WHEN FilesScanned > 0 THEN FilesScanned ELSE 0 END), 0) AS TongLuotTepDaQuet,
       COUNT_BIG(CASE WHEN SoGiay BETWEEN 0 AND 604800 THEN 1 END) AS SoPhienTinhTrungBinh,
       AVG(CASE WHEN SoGiay BETWEEN 0 AND 604800 THEN SoGiay END) AS GiayQuetTrungBinh
FROM PhienHoanTat;

-- 2. So DETECTION duoc ngan chan / da xu ly, ke ca ScanID NULL.
-- Loc theo DetectedAt, vi cau truc hien co chua co thoi gian QuarantinedAt.
SELECT COUNT_BIG(*) AS SoPhatHienDaChanXuLy
FROM dbo.ThreatDetections
WHERE DetectedAt >= @TuNgay AND DetectedAt < @DenNgayDocQuyen
  AND (
    LOWER(LTRIM(RTRIM(ISNULL(Status, '')))) IN ('blocked', 'deleted')
    OR (LOWER(LTRIM(RTRIM(ISNULL(Status, '')))) = 'quarantined'
        AND NULLIF(LTRIM(RTRIM(QuarantinePath)), '') IS NOT NULL)
    OR (LOWER(LTRIM(RTRIM(ISNULL(ActionTaken, '')))) = 'blocked'
        AND LOWER(LTRIM(RTRIM(ISNULL(Status, ''))))
            NOT IN ('restored', 'deleted', 'failed', 'detected'))
  );
GO
