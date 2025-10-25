# Đồng bộ font Segoe UI — Antivirus WinForms

## Mục tiêu

- Dùng **Segoe UI** cho nội dung chữ trên toàn bộ các màn hình WinForms và giao diện tự vẽ `Sketch20View` (Tổng quan, Bảo vệ, Cách ly, Mối đe dọa, Lịch sử, Quản trị, Cài đặt, hộp thoại và bảng).
- **Giữ cỡ chữ và FontStyle** đã thiết lập trước đó, tránh việc phóng to toàn bộ khiến control bị tràn hoặc chồng lấn.
- Giữ các font biểu tượng `Segoe UI Symbol` và `Segoe MDL2 Assets`: đây là font ký hiệu dành cho glyph, không phải font nội dung.
- Không thay đổi dữ liệu SQL, cấu hình API, cơ chế quét, xử lý mã độc hoặc bố cục phân trang.

## Vị trí cấu hình chính

- `Control/Theme.cs`: `Theme.UiFontFamily = "Segoe UI"` và `Theme.DefaultUiFont`; mọi token cỡ chữ trong `Theme` dùng cùng họ Segoe UI.
- `Control/Sketch20Kit.cs`: hàm `Sketch20Style.Font(...)` chọn Segoe UI cho cả chữ thường, chuỗi hash/đường dẫn (`mono=true`). Khi hệ thống không có Segoe UI, dùng fallback sans-serif để giao diện vẫn vẽ được.
- `FrmMain.cs`, `Control/Sketch20View.cs`: gán Segoe UI làm font mặc định cho các control kế thừa.
- Cửa sổ phụ của bộ chọn, hộp thoại nhập, phác thảo, lịch sử chữ ký, chi tiết tệp cách ly: dùng font mặc định Segoe UI.
- `Control/SampleSubmissionForm.cs`: vùng thông tin kỹ thuật chuyển từ Consolas sang Segoe UI.
- `Services/ScanReportExporter.cs`: giao diện báo cáo HTML cũng ưu tiên Segoe UI.

## Danh sách file C# đã sửa

1. `Control/Theme.cs`
2. `Control/Sketch20Kit.cs`
3. `Control/SampleSubmissionForm.cs`
4. `FrmMain.cs`
5. `Control/Sketch20View.cs`
6. `Control/Sketch20Samples.cs`
7. `Control/UiSelect.cs`
8. `Control/AppDialog.cs`
9. `Control/SignatureManagementForm.cs`
10. `Control/UcScreens.Sketch.cs`
11. `Services/ScanReportExporter.cs`

## Cách áp dụng

**Bản cập nhật nhỏ:** Giải nén, chép các file C# vào đúng đường dẫn trong thư mục dự án, sau đó **Visual Studio → Build → Clean Solution → Rebuild Solution**.

**Bản đầy đủ:** Giải nén vào thư mục mới rồi mở `ScanAndRemoveVirus.sln`. Không cần chạy script SQL hay thêm font; Windows 10/11 có sẵn Segoe UI.

## Kiểm thử khuyến nghị

1. Kiểm tra sidebar, nút Quản trị, bảng Mối đe dọa (0 / 10 / 20 dòng), Lịch sử và Cài đặt.
2. Mở các hộp thoại Quản trị, tra cứu VirusTotal, xem chi tiết tệp, chọn thời gian, gửi mẫu (không gửi tệp thật nếu chỉ kiểm thử UI).
3. Thử chế độ thu nhỏ cửa sổ và Windows Display scaling 100% / 125% để phát hiện nội dung dài bị cắt.
4. Font biểu tượng vẫn phải hiển thị đúng; không thay font glyph sang Segoe UI thường.

**Kiểm tra đã thực hiện:** Đối chiếu các khai báo `new Font(...)` trong mã nguồn và kiểm tra cân bằng dấu ngoặc của các file đã sửa. **Chưa thể Rebuild hoặc chạy giao diện WinForms thực tế** vì môi trường không có bộ build .NET Framework trên Windows.
