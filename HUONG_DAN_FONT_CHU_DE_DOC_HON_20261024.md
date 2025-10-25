# Nâng cỡ chữ đồng bộ toàn dự án – 24/10/2026

## Phạm vi
- Tăng nhẹ (thường khoảng +0,55 đến +0,75 pt) ở các chữ nhỏ 8–13 pt.
- Các tiêu đề lớn tăng ít hơn để không phá vỡ bố cục hiện tại.
- Chỉnh token font dùng chung trong `Control/Theme.cs`, font khai báo tại các file `.Designer.cs` và các form có khai báo font trực tiếp.
- Bộ giao diện custom Sketch20 tăng hệ số `TypographyScale` từ 1.08 lên 1.115.
- Biểu đồ Quản trị tăng kích thước chữ mục tiêu nhưng vẫn dùng cơ chế đo vừa ô, không biến số thành dấu ba chấm.
- Giữ nguyên logic chức năng, dữ liệu SQL Server, màu nút, các góc vuông và chức năng tooltip.

## Cách áp dụng
1. Giải nén ZIP đầy đủ, mở `ScanAndRemoveVirus.sln` bằng Visual Studio trên Windows.
2. `Build > Clean Solution`, sau đó `Build > Rebuild Solution`.
3. Kiểm tra trực quan trên độ phân giải 1366×768, 1920×1080 và Windows Scale 100%/125%.
4. Đặc biệt kiểm tra tên cột DataGridView, hộp thoại, màn Quản trị khi thu hẹp, lựa chọn ngày, các nút cạnh nhau.

Chưa thể xác nhận chạy trực quan/biên dịch Windows trong môi trường tạo ZIP. Các trang có bố cục cực hẹp vẫn có thể cần căn lại riêng sau khi chạy thực tế.
