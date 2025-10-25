# Giao diện Quản trị WinForms – Bản sửa responsive theo khung

## Mục đích

Khắc phục hiện tượng thanh **Chọn ngày / Cập nhật lần cuối / Làm mới / Đăng xuất quản trị** vẫn nằm sai vị trí, và một số nút/nhãn của ba thẻ quản trị phía dưới chồng lên nhau hoặc bị cắt.

## Thay đổi mã nguồn

- `Control/UcQuanTri.cs`: thay việc đặt từng khung trên màn hình bằng tọa độ tuyệt đối bằng **TableLayoutPanel** ba hàng: (1) thanh công cụ, (2) Thống kê hệ thống, (3) ba thẻ quản trị. Ba thẻ dưới nằm trong một bảng ba cột, mỗi thẻ có vùng riêng. Các trường, nút và nhãn bên trong cũng căn lại theo chiều rộng/chiều cao của từng thẻ. Không có thanh cuộn ở màn hình thông thường.
- `Control/UcQuanTri.Dashboard.cs`: giữ dashboard bên trong ô bố cục mới mà không gọi `SetBounds` cho khung được `Dock=Fill`.
- **Không thay đổi** cơ chế quét, lịch sử, cách ly, chữ ký, VirusTotal, đăng nhập Admin hay bất kỳ bảng SQL nào.

## Cách chạy đúng bản mới (rất quan trọng)

1. **Đóng toàn bộ** cửa sổ Antivirus đang chạy.
2. Giải nén ZIP **vào một thư mục mới**, không chép đè từng tệp lên nhiều bản dự án cũ.
3. Mở `ScanAndRemoveVirus.sln` trong thư mục vừa giải nén.
4. Trong Visual Studio, chọn cấu hình **Debug / Any CPU** (cấu hình solution; project tự đặt `PlatformTarget=x64`), chọn **Build → Clean Solution** rồi **Build → Rebuild Solution**.
5. Nhấn **F5**, hoặc sau khi Rebuild thành công chạy `bin\Debug\ScanAndRemoveVirus.exe`.
6. Không chạy `Windows_Release\ScanAndRemoveVirus.exe` từ những bản ZIP cũ: đó là file thực thi chưa được biên dịch từ mã mới. Gói ZIP này đã bỏ EXE cũ để tránh nhầm.

Kiểm tra đúng giao diện: thanh chọn ngày, thời gian cập nhật, nút làm mới, nút đăng xuất đều nằm trong **một khung riêng nằm TRÊN thẻ Thống kê hệ thống**; dưới biểu đồ là ba thẻ Chính sách, VirusTotal, Dữ liệu chữ ký **trên cùng một hàng**, không có nút Đăng xuất ở chân trang.

## Giới hạn kiểm thử

Đã đối chiếu thay đổi mã và tệp SQL bằng công cụ tĩnh. Môi trường hiện tại không có bộ biên dịch WinForms .NET Framework trên Windows và chưa chạy thử ở các thiết lập DPI thực tế. Nếu lỗi phát sinh, gửi ảnh Error List có tên tệp và dòng lỗi.
