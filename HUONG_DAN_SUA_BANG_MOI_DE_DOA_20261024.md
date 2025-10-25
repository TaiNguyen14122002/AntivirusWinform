# Sửa giao diện Mối đe dọa cần xử lý

## Các điểm đã cập nhật

1. Bảng luôn giữ một vùng hiển thị tối đa 10 hàng (hoặc số hàng phù hợp chiều cao màn hình), kể cả khi không có dữ liệu. Khi rỗng vẫn hiển thị đầy đủ tiêu đề, tìm kiếm, bộ lọc, các nút thao tác và phân trang 0 mục.
2. Tăng font tiêu đề cột và nội dung từng dòng (tên tệp, đường dẫn, loại mã độc, mức độ, ngày phát hiện, trạng thái, xem chi tiết), giữ việc rút gọn dấu `…` chỉ cho **đường dẫn**.
3. Các nút Cách ly / Xóa / Cho phép hiển thị rõ khi bị vô hiệu hóa. Đây là trạng thái **chưa chọn mục**, không phải lỗi: chọn ít nhất 1 checkbox để thao tác.
4. Nút Cách ly tất cả luôn hiện, nhưng chỉ bật khi còn mối đe dọa chưa xử lý. Các biến thể bố cục đều gọi cùng luồng xử lý có xác nhận.
5. Nếu dữ liệu đã đổi và không còn dòng xử lý tương ứng, ứng dụng báo rõ thay vì bỏ qua im lặng. Sau thao tác bị hủy/thất bại, các mục chưa xử lý vẫn được giữ lựa chọn để có thể thử lại.
6. Phân trang 10/20/50 mục/trang và cuộn trong danh sách giữ nguyên. Các nút thao tác chỉ thực thi qua xử lý thật của dự án.

## File sửa

- `Control/Sketch20View.Threats.cs` — bố cục bảng, trạng thái rỗng, kích thước chữ và trạng thái các nút.
- `Control/Sketch20View.cs` — nút Cách ly tất cả ở mọi kích thước cửa sổ.
- `Control/Sketch20Kit.cs` — màu chữ của nút vô hiệu hóa nhưng vẫn dễ đọc.
- `Control/Sketch20Backend.cs` — báo rõ khi mục được chọn không còn trong bảng xử lý.

## Cách áp dụng bản vá

Giải nén bản chỉ-file, chép đè đúng các file trong `Control`, sau đó vào Visual Studio chọn **Clean Solution** và **Rebuild Solution**.

## Kiểm tra thủ công trên Windows

- Danh sách = 0: vẫn thấy khung danh sách có chiều cao, hàng tiêu đề cột, tìm kiếm, lọc, phân trang; các nút xử lý mờ vừa phải nhưng **đọc được**.
- Danh sách >= 1: chọn checkbox một hàng và kiểm tra nút Cách ly được kích hoạt; bấm và thử **No** trên hộp thoại thì lựa chọn vẫn còn.
- Danh sách >= 20: chọn 20 dòng/trang, kéo thanh cuộn bên trong bảng; tiêu đề cột và phân trang vẫn đứng yên.
- Dòng trong ZIP: khi cách ly, ứng dụng phải xác nhận việc cách ly **toàn bộ tệp ZIP nguồn**.
- Thử đổi kích thước cửa sổ và DPI Windows 100%/125% để kiểm tra tránh tràn chữ.

**Lưu ý:** Đã kiểm tra cân bằng dấu ngoặc C# và file ZIP. Không có MSBuild trên môi trường dựng gói nên chưa xác nhận Rebuild hoặc hành vi WinForms thực tế.
