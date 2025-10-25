# Thống kê Quản trị từ bảng Lịch sử trong AntivirusDB

## 1. Nguồn dữ liệu

- **Nguồn chính:** `dbo.ScanHistory`. Tổng số lượt quét, tổng tệp kiểm tra, tổng số phát hiện, thời gian quét trung bình và biểu đồ theo ngày đều dựa trên các cột `ScanID`, `ScanType`, `StartedAt`, `CompletedAt`, `FilesScanned`, `ThreatCount` và `ActivityTitle`.
- **Chi tiết tham khảo (nếu tồn tại):** `dbo.ThreatDetections`, nối `ThreatDetections.ScanID = ScanHistory.ScanID`, bổ sung tên/loại mã độc và trạng thái xử lý; `dbo.VirusSignatures` bổ sung `Category`/`MalwareFamily`. Các dòng chi tiết **không bao giờ được cộng thêm vào tổng phát hiện** từ lịch sử. Nếu không có chi tiết hoặc ít hơn số phát hiện ghi trong lịch sử, phần thiếu được giữ ở nhóm `Khác/chưa phân loại`.
- **Trạng thái bật/tắt bảo vệ:** đọc từ `dbo.Settings` vì bảng lịch sử không chứa trạng thái hiện tại của các công tắc. Trạng thái cấu hình *không khẳng định* dịch vụ bảo vệ đã vận hành thành công.

## 2. Cách thống kê

1. Bộ lọc chọn hôm nay, hôm qua, 7/30/90 ngày hoặc khoảng ngày tùy chọn được áp vào `ScanHistory.StartedAt`. Ngày kết thúc được bao gồm bằng điều kiện nhỏ hơn 0 giờ ngày tiếp theo.
2. Mỗi bản ghi lịch sử là một phiên hoặc sự kiện. Số **lượt quét** chỉ tính Quick, Full, Other/Custom; sự kiện Update/Behavior/RealTime không được nhầm là lượt quét thủ công. Tuy nhiên những phát hiện do sự kiện này ghi nhận qua `ThreatCount` vẫn được tính trong **tổng phát hiện đã ghi vào lịch sử**.
3. **Tổng tệp** = tổng `FilesScanned` của các lượt quét (không cộng nhầm sự kiện cập nhật chữ ký).
4. **Tổng phát hiện** = tổng `ThreatCount` của các bản ghi `ScanHistory` trong khoảng ngày.
5. **Phân loại mã độc**: chỉ sử dụng chi tiết có `ScanID` trùng bản ghi lịch sử và không vượt quá `ScanHistory.ThreatCount` của phiên đó.
6. **Tỷ lệ cách ly thành công** chỉ tính trên các thao tác cách ly có bằng chứng thành công hoặc thất bại trong chi tiết. Các phát hiện thiếu chi tiết được ghi là `Chưa rõ/xử lý`, không tự coi là đã xử lý.
7. **Cập nhật lần cuối** là thời gian phần mềm tải thành công dữ liệu lịch sử, không phải ngày cập nhật chữ ký mã độc.

## 3. Câu lệnh kiểm tra đọc dữ liệu trong SSMS

Chỉnh ngày phù hợp khoảng thống kê trong ứng dụng:

```sql
DECLARE @Start DATETIME2 = '2026-10-17';
DECLARE @EndExclusive DATETIME2 = '2026-10-24';

SELECT ScanID, ScanType, ActivityTitle, StartedAt, CompletedAt,
       FilesScanned, ThreatCount, ResultStatus
FROM dbo.ScanHistory
WHERE StartedAt >= @Start AND StartedAt < @EndExclusive
ORDER BY StartedAt DESC, ScanID DESC;

SELECT CAST(StartedAt AS DATE) AS Ngay,
       COUNT_BIG(*) AS SoBanGhiLichSu,
       SUM(CONVERT(BIGINT, ThreatCount)) AS SoPhatHien,
       SUM(CONVERT(BIGINT, FilesScanned)) AS SoTepTheoLichSu
FROM dbo.ScanHistory
WHERE StartedAt >= @Start AND StartedAt < @EndExclusive
GROUP BY CAST(StartedAt AS DATE)
ORDER BY Ngay;
```

**Chú ý:** `SoBanGhiLichSu` có thể lớn hơn số lượt quét trong dashboard nếu chứa sự kiện bảo vệ thời gian thực hoặc cập nhật. `SoTepTheoLichSu` của truy vấn kiểm tra cũng bao gồm mọi loại sự kiện; dashboard chỉ tổng hợp số tệp của các phiên quét đã phân loại.

## 4. Các thay đổi trong dự án

- Sửa: `Control/UcQuanTri.Dashboard.cs`.
- Không sửa tên bảng, cột, khóa, chỉ mục, script SQL hoặc file `.bak`.
- Không chứa số liệu biểu đồ giả.
- Không thay đổi chức năng quét hay lịch sử.

## 5. Cách thử trên Windows

1. Giải nén ZIP vào thư mục mới; mở `ScanAndRemoveVirus.sln`.
2. Chọn **Build → Clean Solution**, sau đó **Rebuild Solution**.
3. Quét thử một thư mục an toàn. Kiểm tra phiên mới đã được ghi vào `dbo.ScanHistory`.
4. Vào **Quản trị → Thống kê hệ thống**, chọn đúng ngày và bấm làm mới.
5. Kiểm tra tổng phát hiện trên dashboard bằng `SUM(ThreatCount)` từ SSMS. Nếu khác nhau, kiểm tra bộ lọc ngày và các bản ghi phát sinh từ sự kiện realtime.
6. Nếu bảng chi tiết không có đủ dữ liệu phân loại hoặc cách ly, không kỳ vọng biểu đồ phân loại giống nguồn VirusTotal; mục `Khác/chưa phân loại` sẽ phản ánh phần thiếu.

**Giới hạn kiểm thử:** Mã nguồn được kiểm tra tĩnh trong môi trường hiện tại, chưa biên dịch WinForms .NET Framework 4.7.2 và chưa kiểm thử trực tiếp trên SQL Server Windows.
