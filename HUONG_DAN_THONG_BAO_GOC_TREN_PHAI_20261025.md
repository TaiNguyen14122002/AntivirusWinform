# Thông báo ở góc trên cùng bên phải

## Những thay đổi
- Thông báo bảo vệ xuất hiện ở góc phải, cách mép trên vùng làm việc 12 px, cách mép phải 16 px.
- Thông báo quét theo lịch cũng hiện ở góc phải phía trên. Nếu xuất hiện đồng thời với thông báo bảo vệ, thông báo quét theo lịch xếp bên dưới (cách 10 px), không che nhau.
- Khi người dùng đóng thông báo bảo vệ hoặc thông báo hết hạn, khung quét theo lịch trở về vị trí trên cùng.
- Khi đổi kích thước cửa sổ, vị trí và chiều rộng thông báo được cập nhật.
- Tự ẩn, giới hạn thông báo trùng, thông báo hệ thống lúc thu nhỏ và toàn bộ cơ chế xử lý bảo vệ không đổi.

## File được sửa
- `FrmMain.ProtectionNotifications.cs`
- `FrmMain.ScheduleNotifications.cs`

## Cài đặt
1. Chép đè hai file vào thư mục gốc dự án (cùng cấp `FrmMain.cs`).
2. Trong Visual Studio chọn Build > Clean Solution, rồi Build > Rebuild Solution.
3. Kiểm thử: bật/tắt Bảo vệ; thay đổi kích thước cửa sổ; thử thông báo quét theo lịch và thông báo bảo vệ cùng lúc.

> Chưa thể chạy Visual Studio/MSBuild Windows trong môi trường tạo bản cập nhật này. Cần kiểm thử thực tế.
