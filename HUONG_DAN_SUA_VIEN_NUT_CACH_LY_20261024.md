# Sửa viền trái nút Cách ly – 24/10/2026

## Nguyên nhân
Trong trang **Danh sách mối đe dọa**, ô checkbox có chữ **Đã chọn N mục**
được đặt tại X=22, rộng 174 px; khi giao diện rộng, nút **Cách ly** bắt đầu tại X=195.
Do ô checkbox chiếm tới X=196, hai control giao nhau và đè lên đoạn viền trái của nút.

## Đã thay đổi
File `Control/Sketch20View.Threats.cs`, hàm `ThreatTableRedesigned`:

- Trước: `toolbarY + 12, stackedActions ? 154 : 174, ...`
- Sau: `toolbarY + 12, stackedActions ? 154 : 160, ...`

Tạo khoảng cách 13 px giữa checkbox (mép phải X=182) và nút Cách ly (X=195).
Không thay đổi trạng thái bật/tắt, hoạt động cách ly hoặc các trang khác.

## Áp dụng
1. Chép đè `Control/Sketch20View.Threats.cs` vào dự án hiện tại.
2. Chọn **Build > Rebuild Solution** trong Visual Studio.
3. Mở **Mối đe dọa**, kiểm tra viền trái nút **Cách ly** ở trạng thái không có mục được chọn và sau khi chọn mục.

Chưa thể xác nhận bằng cách chạy giao diện WinForms trực tiếp trên Windows trong môi trường tạo bản vá.
