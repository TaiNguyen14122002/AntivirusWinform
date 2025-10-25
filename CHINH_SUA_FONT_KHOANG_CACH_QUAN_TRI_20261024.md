# Cải tiến hiển thị trang Quản trị (24/10/2026)

## Đã điều chỉnh

- Tăng cỡ chữ có kiểm soát trong biểu đồ, các nhãn trạng thái, tiêu đề thẻ, mô tả, công tắc và nút quản lý chữ ký.
- Thu gọn chiều cao khu vực thẻ dưới để nhường thêm không gian cho biểu đồ.
- Bố trí lại vị trí các hàng quản trị, khóa API, tra cứu SHA-256 và nhóm sáu nút chữ ký để giảm khoảng trắng mà không chồng lấn ở chiều rộng thông thường.
- Giảm lề ngang thẻ dưới từ 6 px xuống 4 px; giữ nguyên bộ lọc ngày xám, góc vuông và chế độ không cuộn trang.
- Bổ sung `UseMnemonic = false` để tiêu đề có ký tự `&` hiển thị đầy đủ.

## File mã nguồn đã sửa

- `Control/UcQuanTri.cs`
- `Control/UcQuanTri.Dashboard.cs`

## Cài đặt

- Với bản đầy đủ: mở solution `ScanAndRemoveVirus.sln`, chọn Clean Solution rồi Rebuild Solution.
- Với bản cập nhật nhỏ: chép hai file `.cs` vào thư mục `Control/` tương ứng rồi Rebuild Solution.

## Kiểm thử cần thực hiện trên Windows

- Phóng to, thu nhỏ giao diện ở các độ phân giải, DPI 100% và 125% hoặc 150%.
- Kiểm tra tiêu đề, chú thích biểu đồ, 2 hàng chính sách, khóa API, 3 hàng nút chữ ký không chồng lấn.
- Xác nhận bộ lọc ngày, Refresh, Logout, VirusTotal, cập nhật chữ ký vẫn hoạt động.

Lưu ý: Cấu trúc dấu ngoặc mã nguồn đã được kiểm tra tự động; chưa chạy biên dịch MSBuild/WinForms thực tế trong môi trường này.
