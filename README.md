> **NÂNG CẤP 21/10/2026 (không thay đổi bảng SQL):** Xem `Docs/BAO_CAO_NANG_CAP_KHONG_THAY_DOI_BANG_SQL_20261021.md`. Đây là source code cần **Rebuild và kiểm thử trên Windows**, không phải EXE nghiệm thu; file `Windows_Release/ScanAndRemoveVirus.exe` là bản cũ. Cấu trúc SQL không thay đổi, các migration lịch sử KHÔNG tự chạy.

# ScannerVirus — đủ 20 màn theo phác thảo

## Bản sửa Windows 1.0.4 — VirusTotal — 05/10/2026

Đã build và chạy trên Windows 11 Home 64-bit, build 26200, .NET Framework 4.x, màn hình 1920×1080, 96 DPI (100%). Font thực tế là Segoe UI và Consolas. Kết quả cuối: **85/85 kiểm tra giao diện**, **151/151 kiểm tra chức năng**, kiểm tra pixel chữ đạt. Lỗi lớp giao diện cũ lọt vào tab/button được tái hiện và sửa trong phần dựng nền control. Controller cũ tiếp tục nhận event và giữ dữ liệu. Vùng tiêu đề đủ chiều cao font Windows; button không có icon không còn vẽ icon mặc định vào chữ; thẻ chọn quét có nền riêng.

Ảnh desktop Windows, ảnh trước/sau, log build, thông số control và SHA-256 của EXE nằm trong `UI_Preview/Evidence_Windows_VirusTotal_1.0.4`. Chẩn đoán mở class `FrmMain` của ứng dụng, chụp desktop bằng CopyFromScreen trước khi đọc metadata qua DrawToBitmap. Model mẫu chỉ dùng để so với 20 phác thảo; ảnh startup dùng model thực trong profile kiểm thử riêng.

`Windows_Release/ScanAndRemoveVirus.exe` là bản Release được build trên Windows và dùng trong lượt nghiệm thu. Có thể chạy file này hoặc build source trong Visual Studio. Phiên bản thực trong Cài đặt → Dữ liệu & công cụ là **1.0.4**. Chạy `VERIFY_UI_WINDOWS.cmd` để kiểm tra lại.

Phiên kiểm thử dùng thư mục tạm, tắt SQL/API và các tác vụ giám sát tự động. Kết quả chỉ áp dụng DPI 100% đã ghi trong log; 125%/150% chưa được nghiệm thu. SQL và VirusTotal trực tuyến cần cấu hình của máy chạy; kiểm tra bao phủ việc giữ dữ liệu khi SQL không khả dụng.

Màn VirusTotal đã bỏ vòng tròn điểm số, thay bằng ba ô số liệu có nhãn và bảng kết quả theo công cụ. Kiểm tra bao gồm 20 màn ở bốn kích thước và năm trạng thái VirusTotal. Đã bỏ tiêu đề/mô tả đầu màn và chuyển Cài đặt thành bánh răng góc trên bên phải. Cả Debug và Release đã build lại, giữ phiên bản 1.0.4.

Giao diện mặc định dùng C# WinForms, .NET Framework 4.7.2. Sidebar, màu, cỡ chữ theo pixel, thẻ, bảng, công tắc và các tab được dựng lại theo bộ 20 phác thảo. Các thao tác sử dụng controller và service của dự án hiện có.

## Mở và chạy

1. Mở `ScanAndRemoveVirus.sln` trong Visual Studio.
2. Chọn `ScanAndRemoveVirus` làm Startup Project và nhấn **F5**.
3. Nếu thiếu thành phần, cài workload .NET desktop development và targeting pack .NET Framework 4.7.2.

Cấu hình SQL hiện có nằm tại `Database/DbConnection.cs`. Các tệp SQL và `AntivirusDB.bak` gốc được giữ trong gói. Cần cấu hình SQL Server trên máy Windows để sử dụng lịch sử và cách ly qua cơ sở dữ liệu.

## 20 màn đã triển khai

| Số | Màn |
|---|---|
| 01 | Tổng quan — an toàn |
| 02 | Bảo vệ — đúng 8 thẻ của phác thảo |
| 03 | Lịch sử — lọc, bảng, phân trang, xuất CSV |
| 04 | Cách ly — chọn tệp, chi tiết, khôi phục, xóa |
| 05 | Cài đặt — Chung |
| 06 | Quét toàn bộ hệ thống |
| 07 | Chi tiết — Thông tin chi tiết |
| 08 | Chi tiết — VirusTotal |
| 09 | Chi tiết — Hành vi |
| 10 | Chi tiết — Chuỗi ký tự |
| 11 | Chi tiết — Thông tin bổ sung |
| 12 | Cài đặt — Bảo vệ chủ động |
| 13 | Cài đặt — VirusTotal |
| 14 | Cài đặt — Dữ liệu & công cụ |
| 15 | Quét thư mục |
| 16 | Quét tệp |
| 17 | Quét tùy chỉnh |
| 18 | Tổng quan — Đang quét |
| 19 | Tổng quan — Phát hiện mối đe dọa |
| 20 | Tổng quan — Bảo vệ tắt |

Hai thiết lập cập nhật chữ ký và thông báo nằm ở Cài đặt → Chung, đúng phác thảo. Màn Bảo vệ có 8 tính năng. Tab chi tiết có 5 nội dung riêng; tab cài đặt có 4 nội dung riêng. Quét thư mục hỗ trợ bật/tắt quét thư mục con, quét tệp hỗ trợ chọn nhiều tệp, quét tùy chỉnh có 4 vị trí và thêm thư mục.

## Xem nhanh cả 20 phác thảo

Sau khi build, chạy:

```text
ScanAndRemoveVirus.exe --phac-thao 1
```

Thay `1` bằng số từ 1 đến 20 để mở màn tương ứng. Nhấn **F6** để chuyển sang màn kế tiếp. Chế độ này dùng cùng renderer WinForms với ứng dụng và dữ liệu mẫu riêng; không gọi SQL/API hay thao tác tệp. Chạy bình thường bằng F5 sử dụng dữ liệu và chức năng thực tế.

Ảnh WinForms và log Windows 1.0.4 mới nhất nằm ở `UI_Preview/Evidence_Windows_VirusTotal_1.0.4`. Ảnh phác thảo gốc và HTML nằm ở `UI_Preview/PhacThao_Goc`. Các bộ ảnh/log cũ đã được dọn để giảm dung lượng.

## Kiểm tra trước đây trên Mono

- Build toàn bộ nguồn bằng Roslyn/C# 7.3, reference assemblies .NET Framework 4.7.2: không có lỗi hoặc cảnh báo.
- Controller và các luồng có sẵn: **122/122** kiểm tra đạt.
- 20 màn và cầu nối với chức năng thật: **149/149** kiểm tra đạt.
- Font, vùng chữ và dữ liệu dài: **155/155** kiểm tra đạt; 20 màn ở 1040×720, 1240×800, 1600×900, với font mặc định và font dự phòng.
- Tổng: **426/426**, mã thoát 0 trên Mono/Xvfb, 96 DPI.

Kết quả kiểm tra font Windows mới nhất nằm cùng bộ evidence 1.0.4. Các bản sửa giữ đường dẫn/hash trong một hàng, giới hạn chữ trong ô, chọn font có sẵn, hiển thị đúng ký tự & và sửa placeholder API key. Rê chuột vào nội dung bị rút gọn để xem toàn bộ; tab Thông tin bổ sung có menu sao chép đường dẫn và hash.

Kiểm tra tích hợp mở ứng dụng thật, thay công tắc service, điều hướng API, quét thư mục không đệ quy, hiện đúng 3 đe dọa trong 4 tệp kiểm thử, mở 5 tab chi tiết, đọc SHA256, quay lại danh sách, quét lại tệp sạch và hủy phiên quét. Khi SQL lỗi, tệp và kết quả được giữ; màn cách ly hiển thị tình trạng kết nối.

Chạy lại trên Linux với .NET SDK, Mono, libgdiplus, Xvfb và Mono.Cecil:

```bash
python3 Tests/verify_dashboard_linux.py
```

Trên Windows, mở Developer Command Prompt for Visual Studio và chạy `VERIFY_DASHBOARD.cmd`; bộ này chạy cả kiểm tra font và lưu ảnh trong `TypographyEvidence`. Bộ kiểm tra sử dụng thư mục dữ liệu riêng và vô hiệu SQL chỉ trong tiến trình test.

Đã chạy trên Windows 11 Home build 26200 ở 96 DPI/100%, đạt 85/85 trường hợp giao diện và 151/151 kiểm tra chức năng. Các số liệu Mono phía trên là kết quả trước đây. SQL hoạt động và VirusTotal trực tuyến cần cấu hình của máy chạy; dữ liệu VirusTotal trong chế độ phác thảo là minh họa.
