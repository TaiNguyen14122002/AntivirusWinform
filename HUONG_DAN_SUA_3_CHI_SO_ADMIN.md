# Sửa 3 chỉ số Thống kê hệ thống – Antivirus WinForms

## Phạm vi
Chỉ thay đổi `Control/UcQuanTri.Dashboard.cs`, **không thay đổi cấu trúc SQL Server**. Kèm `DOI_CHIEU_CHI_SO_DASHBOARD.sql` chỉ chứa truy vấn đọc để xác minh bằng SSMS.

## 1. Tổng tệp đã quét
- Nguồn: `dbo.ScanHistory.FilesScanned`.
- Chỉ tính bản ghi quét đã hoàn thành (`CompletedAt` hợp lệ, trạng thái không phải Running/Failed/Cancelled/Stopped...); bỏ các sự kiện như cập nhật chữ ký và cảnh báo thời gian thực.
- Tính **lượt tệp được quét**, không phải số đường dẫn/tệp *duy nhất*; cùng một tệp được quét ở 2 phiên sẽ được tính 2 lần.
- Nếu quá trình ghi lịch sử trước đây ghi `FilesScanned` sai, phải xử lý tại bộ máy quét và nơi ghi lịch sử; dashboard không thể suy ra số đúng từ các trường còn lại.

## 2. Mối đe dọa đã chặn
- Nguồn: `dbo.ThreatDetections` (không bắt buộc có `ScanID`).
- Chỉ tính các bản ghi có trạng thái `Blocked`, `Deleted` hoặc `Quarantined` kèm đường dẫn cách ly; có xử lý `ActionTaken=Blocked` khi trạng thái không thể hiện một lần khôi phục/lỗi/phát hiện đơn thuần.
- Lọc theo `DetectedAt` của phát hiện nằm trong khoảng ngày đã chọn, **không** phải thời điểm thao tác cách ly. Vì schema hiện tại chưa có trường thời điểm cách ly riêng, không thể thống kê chính xác số *thao tác cách ly thực hiện trong ngày*.
- Trạng thái đang chặn được đọc từ SQL; không thay thế việc kiểm tra file vật lý trong kho cách ly.
- Nếu người dùng đã khôi phục lại tệp, bản ghi `Restored` không được tính là đang chặn.

## 3. Thời gian quét trung bình
- Nguồn: `ScanHistory.CompletedAt - ScanHistory.StartedAt`.
- Tính trung bình cộng trên các phiên quét hoàn tất và có thời điểm hợp lệ, loại các phiên đang chạy, thất bại hoặc bị hủy.
- Giao diện hiển thị `mm:ss` hoặc `hh:mm:ss`, tránh định dạng sai với các phiên dài hơn 1 giờ.
- Nếu thời điểm ghi lịch sử không phản ánh đúng thời gian thực tế (do lỗi ở tiến trình quét), cần sửa tiếp phần ghi lịch sử.

## Kiểm thử trên Windows
1. Giải nén và mở `ScanAndRemoveVirus.sln` trong Visual Studio.
2. `Build > Clean Solution > Rebuild Solution`.
3. Mở `Quản trị > Thống kê hệ thống`, chọn **7 ngày qua** và bấm **Làm mới**.
4. Chạy `DOI_CHIEU_CHI_SO_DASHBOARD.sql` trong SSMS; đặt cùng ngày bắt đầu/kết thúc với bộ lọc trên giao diện.
5. Đối chiếu 3 con số; rê chuột vào từng thẻ để xem cách tính và số phiên làm mẫu.
6. Thử quét mới, thử cách ly (và khôi phục) một mẫu thử nghiệm an toàn để xác nhận số đã chặn thay đổi theo trạng thái xử lý.

**Lưu ý:** Đã kiểm tra tĩnh mã nguồn và tệp SQL, chưa có môi trường Windows/.NET Framework 4.7.2/SQL Server để biên dịch và nghiệm thu trực tiếp.
