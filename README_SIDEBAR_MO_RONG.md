# Điều chỉnh sidebar - 14/10/2026

## Phạm vi
- Thay đổi `Control/Sketch20View.cs` và `Control/Sketch20Kit.cs`.
- Áp dụng cho giao diện Sketch20 đang được `FrmMain.Sketch20.cs` sử dụng.
- Giữ các trang Tổng quan, Bảo vệ, Cách ly, Lịch sử và Cài đặt (bao gồm chỉnh sửa full-screen trước đó).

## Thay đổi
1. Chiều rộng sidebar mở rộng từ **208 px lên 264 px** ở cửa sổ rộng từ 1180 px trở lên; ở cửa sổ nhỏ hơn, tự thu về **80 px** và giữ tooltip trên biểu tượng.
2. Khu vực logo/tên sản phẩm và tiêu đề điều hướng được phân nhóm, có đường phân cách.
3. 5 nút điều hướng cao 48 px, cách nhau 57 px, biểu tượng/chữ rõ ràng hơn. Nút active và hover giữ **góc vuông**.
4. Thẻ thông tin thiết bị được nới rộng và gắn phía dưới. Cửa sổ thấp hiển thị thẻ dạng rút gọn (tên máy + trạng thái); khi quá thấp thẻ sẽ ẩn để không che menu.
5. Thông tin phiên bản nằm riêng dưới đường phân cách chân sidebar. Các trang nội dung tự nhận diện chiều rộng còn lại.

## Lưu ý kiểm thử
- Mở solution trong Visual Studio trên Windows (.NET Framework 4.7.2), chạy **Build > Rebuild Solution**.
- Xem ở kích thước cửa sổ 1600×900, 1366×768, 1280×720; thu nhỏ về 1100×700 và 1024×600 để kiểm tra chế độ icon-only.
- Chưa có môi trường WinForms/Visual Studio để kiểm thử runtime trong phiên chỉnh sửa này.
