# Đồng bộ kiểu chữ tương tự San Francisco cho Antivirus WinForms

## Vì sao không dùng trực tiếp SF Pro trên Windows?

**San Francisco/SF Pro** là font thuộc Apple. Giấy phép đăng trên https://developer.apple.com/fonts/ giới hạn việc sử dụng theo điều khoản của Apple và không cho phép nhúng font vào phần mềm Windows bên thứ ba. Do đó, bản này **không sử dụng hoặc phân phối SF Pro**.

Ứng dụng được cấu hình dùng font **Inter** (gần phong cách San Francisco, có hỗ trợ tiếng Việt, giấy phép SIL Open Font License 1.1) khi hệ thống đã cài Inter, và tự dùng **Segoe UI** khi chưa cài. Không có file font nào được đóng gói cùng dự án.

- Trang Inter chính thức: https://rsms.me/inter/
- Trang tải chính thức: https://github.com/rsms/inter/releases/latest
- Giấy phép: https://github.com/rsms/inter/blob/master/LICENSE.txt

## Cách áp dụng trên Windows

1. Tải và cài **Inter** từ nguồn chính thức (chọn các kiểu Regular / Bold phù hợp). Việc cài font do người sử dụng Windows thực hiện.
2. **Đóng hoàn toàn ứng dụng** và mở lại để quét lại danh sách font đã cài.
3. Mở `ScanAndRemoveVirus.sln`, chọn **Clean Solution**, sau đó **Rebuild Solution**.
4. Mở các trang Tổng quan, Bảo vệ, Mối đe dọa, Cách ly, Lịch sử, Quản trị, Cài đặt và các hộp thoại; kiểm tra các nhãn dài ở 100%/125%/150% DPI.
5. Nếu chưa cài Inter hoặc Inter không được hệ điều hành nhận, chương trình tự động dùng Segoe UI, không phát sinh lỗi.

## Những thay đổi mã nguồn

- `Control/Theme.cs`: thêm lựa chọn font ưu tiên Inter, dự phòng Segoe UI.
- Các file C# còn đặt cố định `new Font("Segoe UI", ...)` đã chuyển sang `new Font(Theme.UiFontFamily, ...)` để dùng chung nguồn font.
- Trong `FrmMain*.cs` dùng tên lớp đủ namespace khi cần.
- Không thay đổi cỡ chữ, thuật toán quét, cơ sở dữ liệu hoặc các chức năng quản trị.
- **Giữ lại Segoe UI Symbol, Segoe MDL2 Assets** cho icon và ký hiệu hệ thống, tránh icon biến thành ô vuông.

> Lưu ý: Font Inter không có sẵn trên đa số máy Windows. Nếu chưa cài, giao diện vẫn là Segoe UI. Không nên kỳ vọng hình thức thay đổi ngay sau khi chỉ chép các file C#.
