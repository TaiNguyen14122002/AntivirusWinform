# Chỉnh sửa responsive trang Quản trị – 24/10/2026

## Tệp mã nguồn đã sửa

- `Control/UcQuanTri.cs`
- `Control/UcQuanTri.Dashboard.cs`

## Yêu cầu

1. **Bỏ thanh cuộn của trang Quản trị:** `homeHost.AutoScroll = false`; bỏ `AutoScrollPosition`; layout chính `DockStyle.Fill` và chia chiều cao theo viewport thực tế, không đặt chiều cao lớn hơn chiều cao hiển thị.
2. **Thẻ tỷ lệ cách ly thành công:** Vẽ dashboard thu đều theo cả chiều rộng và chiều cao trên cửa sổ hẹp; vòng donut giới hạn theo bề rộng cột; danh sách trạng thái đặt dưới phụ đề để không còn đè vào “Toàn hệ thống”; điều chỉnh diện tích danh sách ở biểu đồ thấp. Cập nhật tọa độ hit-test và nút “Xem tất cả” tương ứng với tỉ lệ mới.
3. **Quản lý VirusTotal:** “Khóa API” và trạng thái tách thành hai hàng khi card hẹp; tránh vùng chữ đè nhau; hai nút chia đều chiều ngang; hàng tự tra SHA-256 theo kích thước card; bật ellipsis và tooltip cho nhãn dài.
4. **Các thẻ còn lại:** Card quản trị co giãn theo chiều cao; 6 nút quản lý chữ ký chia theo 3 hàng khi card thấp; ẩn mô tả phụ ở chiều cao thấp để nhường chỗ cho thao tác.

## Phạm vi

Không thay đổi SQL Server, model chữ ký, API, thuật toán quét, hoặc event handler đã có.

## Cách kiểm tra trên Windows

1. Mở `ScanAndRemoveVirus.sln` bằng Visual Studio.
2. Clean và Rebuild, mở trang Quản trị.
3. Đổi chiều rộng và chiều cao cửa sổ ở 100% và 125% Display Scale (nếu có).
4. Kiểm tra không có thanh cuộn trên trang Quản trị, khu cách ly không có chữ chồng nhau, các nút VirusTotal còn bấm được và API key không hiển thị dưới dạng thuần văn bản.

**Lưu ý:** Đây là bản chỉnh mã nguồn; chưa thể chạy kiểm thử GUI WinForms thực tế trên Windows trong môi trường tạo tệp.
