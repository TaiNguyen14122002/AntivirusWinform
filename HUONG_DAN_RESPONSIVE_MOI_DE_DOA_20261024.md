# Cải tiến giao diện Mối đe dọa cần xử lý – 24/10/2026

## Các thay đổi

1. **Chỉ đường dẫn tệp được rút gọn**: Cột Đường dẫn giữ lại tiền tố ổ đĩa và tên tệp; với đường dẫn thành viên ZIP cố gắng giữ tên ZIP và tên mục bên trong. Rê chuột lên dòng để đọc tên, đường dẫn, lý do phát hiện đầy đủ.
2. **Hạn chế mất chữ/mất viền**: Tăng chiều rộng bộ lọc để đủ nội dung như “Tất cả”, “Mức độ cao”; chia thanh tìm kiếm và bộ lọc thành hàng riêng khi cửa sổ hẹp. Trong bảng, tên, loại, mức độ, ngày, trạng thái và thao tác dùng trình vẽ tự cân đối font hoặc xuống dòng, **không tự thêm dấu ba chấm**.
3. **Phân trang ổn định**: 10/20/50 là số mục **mỗi trang**; không giảm xuống tùy theo số hàng vừa cửa sổ. Phân trang đầu kỳ dạng `‹ 1 2 3 … 23 ›` nếu có 23 trang, khi chuyển trang hiển thị các trang lân cận, trang đầu và cuối với dấu lược bớt.
4. **Cuộn bảng khi cần**: Vùng danh sách chỉ chứa tối đa **10 hàng đang nhìn thấy**. Khi chọn 20/50 mục một trang (hoặc cửa sổ quá thấp để đủ 10 hàng), thanh cuộn chỉ xuất hiện trong vùng bảng. Header và footer phân trang đứng yên. Có thể dùng con lăn chuột.
5. **Giảm khoảng trống**: Bỏ cách ép thẻ danh sách cao cố định theo toàn bộ vùng xem; chiều cao bảng dựa trên số hàng hiển thị. Footer được kéo sát ngay sau bảng.
6. **Giữ chức năng**: Tìm kiếm/lọc/sắp xếp, chọn nhiều, cách ly, xóa, cho phép có kiểm soát, mở chi tiết tệp, làm mới và nguồn dữ liệu thật không thay đổi.

## File mã nguồn đã sửa

- `Control/Sketch20View.Threats.cs` – giao diện, cột, rút ngắn đường dẫn, cuộn bảng và phân trang.
- `Control/Sketch20View.cs` – chuyển thao tác lăn chuột vào thanh cuộn bên trong bảng, đặt lại vị trí cuộn khi điều hướng.

## Cách sử dụng

1. Sao lưu dự án cũ.
2. Giải nén ZIP đầy đủ và mở `ScanAndRemoveVirus.sln`, **hoặc** chép đè 2 file trong thư mục `Control` từ bản cập nhật nhỏ.
3. Visual Studio → **Build → Clean Solution**, tiếp theo **Rebuild Solution**.
4. Mở **Mối đe dọa**, kiểm tra với 10, 20, 50 mục/trang; lăn chuột trên bảng và đổi qua trang cuối.
5. Thử ở các độ rộng cửa sổ khác nhau, đặc biệt là khoảng 1024, 1280, 1536 px và các thiết lập DPI Windows.

**Phạm vi kiểm tra:** Đã kiểm tra cân bằng dấu ngoặc, sự hiện diện của hai file trong `.csproj`, công thức phân trang và chiều cao với nhiều kích thước giả lập. Chưa biên dịch hay chạy UI trực tiếp trên Windows trong môi trường tạo gói. Các tên tệp dài bất thường vẫn có thể vượt diện tích vật lý của cột; rê chuột lên dòng để đọc bản gốc.
