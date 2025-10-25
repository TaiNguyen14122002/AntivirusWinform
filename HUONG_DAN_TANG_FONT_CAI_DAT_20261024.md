# Tăng cỡ chữ trang Cài đặt – 24/10/2026

## Nguồn hiển thị thực tế
Trang Cài đặt trong ảnh được vẽ từ `Control/Sketch20View.cs` (hàm `Settings()`), không phải các nhãn WinForms trong `UcCaiDat.Designer.cs`. Do đó, sửa font ở `UcCaiDat.cs` không đủ để làm chữ trong ảnh lớn lên.

## Thay đổi
- `ReferenceTextScale` cho riêng trang `settings`: từ `1.0` lên `1.19`; không đổi ở các trang khác.
- Tăng tiêu đề và chú thích ở ba vùng: Thiết lập chung, Thông tin ứng dụng, Thao tác nhanh.
- Tăng chữ tên tính năng, thông tin chi tiết, giá trị phiên bản/CSDL/cách ly, tên nút và ghi chú chân trang.
- Tăng chiều cao tối thiểu các khung, ô chữ mô tả và cho phép chú thích dài xuống hai dòng.
- Đường dẫn thư mục dài có thể tự xuống dòng trong hàng cuối thay vì hiển thị dấu `…`; giữ nút Sao chép.
- Giữ nguyên các thao tác, trạng thái bật/tắt, sự kiện, màu sắc và nút góc vuông.

## Thay thế nhanh
Sao chép đè `Control/Sketch20View.cs` vào đúng thư mục `Control` trong dự án, sau đó mở Visual Studio và chạy **Build > Rebuild Solution**.

## Kiểm tra trên Windows
1. Mở trang **Cài đặt** ở độ phân giải bình thường, kiểm tra các nhãn và mô tả không bị chồng/cắt.
2. Thử thu nhỏ cửa sổ; trang có thể cuộn dọc khi chiều cao thực sự không đủ.
3. Kiểm tra tỷ lệ DPI 100% và 125%, các nút **Mở/Xóa/Tạo**, **Khôi phục mặc định**, **Lưu thiết lập**.
4. Xác nhận các thiết lập hoạt động như trước.

Chưa biên dịch/kiểm thử trực quan trên máy Windows trong môi trường hiện tại.
