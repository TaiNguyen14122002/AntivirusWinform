# Popup tiến trình cập nhật chữ ký — AntivirusWinform

## Luồng giao diện

1. Mở **Cài đặt → Tự động cập nhật chữ ký** (cả thẻ và nút bật/tắt đều được hỗ trợ).
2. Nhập mật khẩu quản trị tại hộp thoại có che ký tự.
3. Nếu mật khẩu sai, giữ nguyên tùy chọn; nếu đúng, **không hiện hộp thoại “Mật khẩu chính xác”** mà mở popup tiến trình.
4. Popup có thanh phần trăm, 6 giai đoạn cùng thời điểm hoàn thành, nút Hủy và nút Đóng.
5. Sau khi đóng popup, ứng dụng tiếp tục áp dụng thay đổi trạng thái AutoUpdate như trước.

## Bản chất việc cập nhật hiện tại

**Chưa có server hoặc API cung cấp bản cập nhật chữ ký mới** trong dự án.

Popup mới **không giả lập việc tải Internet**. Các giai đoạn thực tế:

- Kết nối SQL Server theo `Database/DbConnection.cs`.
- Đọc `Settings.VirusDatabaseVersion`.
- Đếm bản ghi `VirusSignatures` có `IsActive = 1`.
- Nạp lại chữ ký hash toàn tệp và nhóm CDC bằng `HybridSignatureScanner.GetSnapshot(true)`.
- Xóa bộ nhớ đệm quét để phiên sau sử dụng dữ liệu mới nạp.
- Hiển thị số chữ ký đã nạp.

**Quan trọng:** nạp lại chữ ký từ cơ sở dữ liệu hiện tại *không* bổ sung mẫu mới, không đổi phiên bản, không cập nhật `LastDatabaseUpdate`. Để thực sự tải dữ liệu chữ ký virus mới từ Internet, cần cung cấp nguồn dữ liệu đáng tin cậy (API/URL/gói cập nhật) và phương thức xác thực chữ ký tải về.

## Kiểm tra

- Nhập sai/Hủy mật khẩu: không hiện cửa sổ tiến trình, không đổi cài đặt.
- Nhập đúng: xuất hiện cửa sổ có thanh tiến trình chạy theo từng bước thực.
- SQL Server mất kết nối: hiển thị lỗi trong popup, không có thông báo hoàn tất giả.
- Nhấn Hủy: ngừng trước bước tiếp theo (truy vấn SQL đang thực thi có thể cần chờ timeout).
- Khi làm mới thành công: hiện số chữ ký hash và nhóm CDC; bấm Đóng trở về Cài đặt.
- Chạy trên Visual Studio **Build → Rebuild Solution** để kiểm chứng thực tế.

Ghi chú: dự án còn có chức năng theo hạn `GuardService.EnsureDailyAutoUpdate()` cũ; tính năng này trước đây chỉ ghi dấu thời gian và xóa cache, **không tải dữ liệu mới**. Do đó nếu yêu cầu “tự cập nhật chữ ký từ xa” đúng nghĩa, phải xây dựng riêng bộ tải có nguồn và kiểm tra tính toàn vẹn.
