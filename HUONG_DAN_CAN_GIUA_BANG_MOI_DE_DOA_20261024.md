# Căn giữa nội dung bảng Mối đe dọa

## File chỉnh sửa
`Control/Sketch20View.Threats.cs`

## Thay đổi
- Dùng cùng một dãy độ rộng cột cho phần tiêu đề và từng hàng dữ liệu. Khi thanh cuộn dọc xuất hiện, trừ chiều rộng thanh cuộn khỏi cả hai bố cục, tránh lệch cột.
- Căn giữa theo chiều ngang và chiều dọc nội dung các cột Loại, Loại mã độc, Mức độ, Ngày phát hiện, Trạng thái và Thao tác; đưa badge/nút nằm giữa đúng cột.
- Căn giữa tiêu đề các cột.
- Vẫn căn trái Tên tệp và Đường dẫn để dễ đọc; chỉ rút gọn đường dẫn dài như trước.
- Giữ nguyên chọn tệp, nút Cách ly/Xóa/Cho phép, phân trang và cuộn bảng.

## Cách áp dụng
Sử dụng bản dự án đầy đủ hoặc chép đè `Control/Sketch20View.Threats.cs` từ gói cập nhật nhỏ vào đúng thư mục dự án, sau đó Clean và Rebuild Solution bằng Visual Studio trên Windows.

## Lưu ý kiểm thử
Chưa có môi trường biên dịch và chạy ứng dụng WinForms trên Windows trong phiên này. Nên kiểm tra căn cột ở chiều rộng lớn, khi chọn 20/50 dòng gây xuất hiện thanh cuộn, và ở chế độ cửa sổ hẹp.
