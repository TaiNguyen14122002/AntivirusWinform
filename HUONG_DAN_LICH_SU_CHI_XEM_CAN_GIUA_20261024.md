# Chỉnh sửa trang Lịch sử — 24/10/2026

## Đã cập nhật
- Bỏ nút thùng rác ở cột **Thao tác**; giữ duy nhất nút **Xem** căn giữa.
- Bỏ mục xóa trong menu lịch sử cũ và loại bỏ luồng lệnh `history-delete:` trong cả bộ định tuyến và backend. Không thay đổi dữ liệu lịch sử đã lưu.
- Căn giữa cả tiêu đề và nội dung các cột **Thống kê**, **Thời gian quét**, **Kết quả**, **Thao tác**. Hai cột Thời gian / Loại quét giữ căn trái.
- Khi màn hình hẹp, cột gộp Thống kê / Thời gian hiển thị tệp, mối đe dọa và thời lượng theo ba dòng căn giữa.
- Giữ nguyên bộ lọc, phân trang, CSV và xem chi tiết.

## File thay đổi
1. `Control/Sketch20View.History.cs`
2. `Control/UcLichSu.cs`
3. `Control/UcLichSu.Designer.cs`
4. `Control/Sketch20Backend.cs`
5. `FrmMain.Sketch20.cs`

## Kiểm thử Windows
Mở `ScanAndRemoveVirus.sln`, Clean Solution -> Rebuild Solution. Mở Lịch sử, kiểm tra nút Xem; kiểm tra menu `…`/nhấp phải không còn Xóa; thử thay đổi kích thước cửa sổ và phân trang.
