# Nâng cấp Dashboard Quản trị: phân loại mã độc động, không dùng nhóm “Khác”

## Mục tiêu
Khi phát hiện mã độc, hai biểu đồ **Mối đe dọa đã phát hiện** và **Loại mã độc phổ biến** phải thể hiện tên loại mã độc có căn cứ từ dữ liệu thực, không cố định ở Virus/Trojan/Ransomware/Spyware và không gộp phát hiện chưa rõ loại thành “Khác”.

## Mã nguồn đã sửa
- `Control/UcQuanTri.Dashboard.cs`

## Dữ liệu và thứ tự ưu tiên
1. Tổng số phát hiện: `dbo.ScanHistory.ThreatCount` trong khoảng thời gian được lọc.
2. Loại mã độc chi tiết: `dbo.ThreatDetections` nối `dbo.ScanHistory` theo `ScanID`, và nối `dbo.VirusSignatures` theo `SignatureID`.
3. Ưu tiên phân loại `VirusSignatures.Category`; nếu Category chưa xác định thì chỉ dùng `MalwareFamily` hoặc `ThreatName` khi có dấu hiệu **loại** mã độc rõ ràng.
4. Nhận diện các loại như: Virus, Trojan, Ransomware, Spyware, Adware, Worm, Rootkit, Backdoor, Downloader, Dropper, Stealer, Keylogger, Miner, HackTool, Riskware, PUA/PUP, Exploit, Botnet, Banker, Wiper. Category hợp lệ khác cũng hiển thị bằng tên được lưu trong cơ sở dữ liệu.
5. Nếu không đủ thông tin hoặc không có bản ghi chi tiết, chỉ **đếm riêng “Chưa phân loại”**, không giả định loại mã độc và **không tạo cột “Khác”**.

## Giao diện mới
- Biểu đồ mối đe dọa theo ngày: tự sinh chuỗi dữ liệu cho các loại đã xác định, ưu tiên 5 loại xuất hiện nhiều nhất trong phạm vi xem. Không tự đẩy phần chưa phân loại vào biểu đồ.
- Biểu đồ loại mã độc phổ biến: hiển thị tối đa 7 loại có số lượng cao nhất.
- Nút **Xem tất cả** trong thẻ “Loại mã độc phổ biến”: mở bảng chi tiết liệt kê **đầy đủ tất cả các loại** có số liệu và số lượt phát hiện. Bảng cũng cho biết số chưa phân loại và tổng theo lịch sử.
- Dòng **“N chưa phân loại”** xuất hiện riêng để tổng số phát hiện không bị hiểu nhầm.
- Khi đổi bộ lọc ngày hoặc bấm làm mới, dữ liệu loại mã độc được tính lại từ SQL Server.

## Đối chiếu SQL
Dùng truy vấn chỉ đọc sau để xem các Category thực có trong dữ liệu (điều chỉnh ngày nếu cần):

```sql
SELECT COALESCE(NULLIF(LTRIM(RTRIM(s.Category)), ''), N'(Chưa có Category)') AS LoaiTrongCSDL,
       COUNT_BIG(*) AS SoBanGhi
FROM dbo.ThreatDetections AS d
JOIN dbo.ScanHistory AS h ON h.ScanID = d.ScanID
LEFT JOIN dbo.VirusSignatures AS s ON s.SignatureID = d.SignatureID
WHERE h.StartedAt >= '2026-10-17' AND h.StartedAt < '2026-10-24'
GROUP BY COALESCE(NULLIF(LTRIM(RTRIM(s.Category)), ''), N'(Chưa có Category)')
ORDER BY SoBanGhi DESC;
```

Số lần trên truy vấn có thể khác biểu đồ do dashboard giới hạn số chi tiết của từng `ScanID` theo `ScanHistory.ThreatCount` để không vượt tổng lịch sử.

## Lưu ý
- Không thêm, xóa hoặc thay đổi bảng/cột SQL Server.
- Mã nguồn đã được kiểm tra tĩnh, **chưa biên dịch chạy trên Windows** trong môi trường hiện tại.
- Nếu chữ ký không có `Category`, không có tên/họ đủ thông tin, cần bổ sung/kiểm duyệt dữ liệu nguồn để phần “Chưa phân loại” giảm. Không nên tự tạo tên loại không có căn cứ.
- Các biểu đồ nhỏ hiển thị các loại xuất hiện nhiều nhất để tránh chữ chồng; nút **Xem tất cả** hiển thị danh sách không bị giới hạn này.
