# Sửa giao diện Danh sách mối đe dọa chiếm hết chiều cao cửa sổ

## Mục tiêu
- Trang Mối đe dọa luôn kéo khung `Danh sách mối đe dọa` gần sát cạnh dưới vùng nội dung, **kể cả khi không có bản ghi**.
- Thông báo `Không có mối đe dọa cần xử lý` nằm giữa vùng bảng.
- Thanh phân trang luôn ở chân khung, tránh xuất hiện khoảng trống lớn bên dưới.
- Mặc định 10 mục/trang; lựa chọn 20/50 mục giữ cuộn nội bộ. Tối đa 10 hàng hiển thị cùng lúc nếu chiều cao màn hình đáp ứng.
- Cửa sổ quá thấp được phép cuộn trang để không cắt các nút/chân bảng.

## Tệp thay đổi
- `Control/Sketch20View.Threats.cs`: tính `rowsH` từ chiều cao viewport, thay vì số hàng hiện có; phân bổ chiều cao mỗi hàng trên màn lớn để giữ tối đa 10 hàng.
- `Control/Sketch20View.cs`: nâng ngưỡng dùng cuộn toàn trang trên cửa sổ quá thấp nhằm tránh cắt chân bảng.

## Cách áp dụng
1. Sao lưu dự án hiện tại.
2. Chép đè hai file `.cs` vào thư mục `Control` tương ứng.
3. Visual Studio → Build → Clean Solution, sau đó Rebuild Solution.
4. Mở trang Mối đe dọa khi có 0 / 1 / 10 / 20 / 50 bản ghi và thử resize cửa sổ.

## Giới hạn kiểm thử
Đã kiểm tra thuật toán tính kích thước với nhiều chiều cao. Chưa thể chạy thử giao diện hoặc build WinForms trên Windows trong môi trường tạo bản vá này.
