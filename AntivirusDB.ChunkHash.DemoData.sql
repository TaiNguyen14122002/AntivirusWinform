-- Các chữ ký demo cũ thuộc nhánh FixedWindow đã ngừng được quét.
-- Tránh tự động thêm dữ liệu cố định vào schema CDC vì chúng không tương thích.
-- Để tạo nhóm CDC hợp lệ, dùng Tests\CdcSignatureBuilder.exe với mẫu đã kiểm chứng,
-- xem HUONG_DAN_CDC_FUZZY_HASHING.md. SQL xuất ra sẽ để IsActive = 0.
USE AntivirusDB;
GO
SELECT ChunkingMode, COUNT_BIG(*) AS SoNhom
FROM dbo.ChunkSignatureGroups
GROUP BY ChunkingMode;
GO
