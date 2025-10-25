# Bổ sung trang Quản trị bên trong Antivirus WinForms

**Ngày chỉnh sửa:** 23/10/2026  
**Nguồn:** Bản nâng cấp VirusTotal API v3 gần nhất.  
**Ràng buộc:** Giữ nguyên các bảng, cột, `.sql` và `.bak` hiện có.

## 1. Thay đổi giao diện

Sidebar sử dụng trực tiếp `Sketch20View` có thêm nút **Quản trị** ở giữa **Lịch sử** và **Cài đặt**.

Nhấp vào **Quản trị** sẽ hiển thị một trang **WinForms UserControl (`UcQuanTri`)** tại vùng nội dung bên phải, không mở trình duyệt, không phụ thuộc máy chủ web và không thay đổi cấu trúc cơ sở dữ liệu.

Trang vẫn giữ trạng thái điều hướng hiện có: chuyển qua **Tổng quan**, **Bảo vệ**, **Cách ly**, **Lịch sử** và **Cài đặt** như bình thường.

## 2. Nội dung trang Quản trị

- **Dashboard:** tổng chữ ký, chữ ký đang hoạt động, tổng phiên quét, tổng số lượt phát hiện. Các số liệu được đọc thực tế từ SQL; không dùng số mẫu.
- **Tổng hợp:** tối đa 60 phiên quét mới nhất.
- **Chữ ký:** tối đa 150 chữ ký mới nhất; tìm theo tên hoặc SHA-256. Nút **Quản lý chữ ký** dùng lại `SignatureManagementForm` để phê duyệt, từ chối, vô hiệu hóa, xem audit.
- **Lịch sử quét:** tối đa 150 phiên quét gần nhất từ `ScanHistory`.
- **Phát hiện:** tối đa 150 phát hiện gần nhất từ `ThreatDetections`.
- **Cách ly:** các phát hiện có đường dẫn cách ly, ở chế độ chỉ đọc. Để khôi phục/xóa tệp, sử dụng tab Cách ly có sẵn nhằm cập nhật đồng bộ cả tệp thật và SQL.
- **Lịch quét:** danh sách lấy từ `ScanScheduleService` (gồm SQL/cấu hình cục bộ nếu có). Nút **Lịch quét tự động** mở form quản lý Windows Task Scheduler hiện có.
- **VirusTotal API:** nút mở form thiết lập API key bằng Windows DPAPI có sẵn.
- **Kiểm tra Windows:** nút mở kiểm tra cấu hình bảo mật Windows.
- **Ngoại lệ:** nút quản lý ngoại lệ heuristic có giới hạn thời hạn.

Mọi truy vấn SQL đọc chạy trên background thread để tránh đóng băng màn hình khi mất kết nối, có giới hạn số dòng.

## 3. Các tệp mã nguồn đã thay đổi

| Tệp | Vai trò |
| --- | --- |
| `Control/UcQuanTri.cs` | Trang quản trị mới, giao diện thuần WinForms |
| `Control/Sketch20View.cs` | Nút Quản trị trong sidebar đang chạy + nội dung trang |
| `FrmMain.cs` | Đăng ký trang cho thanh điều hướng cũ (fallback) |
| `FrmMain.Sketch20.cs` | Kết nối `UcQuanTri` với renderer hiện tại |
| `ScanAndRemoveVirus.csproj` | Khai báo thêm UserControl mới |

## 4. Quyền truy cập và giới hạn

Trang hiện tận dụng quyền Windows/SQL Server và xác thực có sẵn ở các form dịch vụ. **Chưa triển khai tài khoản Admin và phân quyền riêng**. Người có quyền chạy ứng dụng có thể nhìn thấy nút Quản trị. Trước khi triển khai cho nhiều người, cần bổ sung phân quyền server-side/Windows group chứ không chỉ ẩn nút trong UI.

Bảng Cách ly trong Admin chỉ xem; không thực thi thao tác tệp trực tiếp từ một câu lệnh SQL.

Không tự sửa bảng/cột trong SQL. Nếu `ScanSchedules` chưa có thì cơ chế lịch tại máy của dịch vụ có sẵn vẫn được dùng.

## 5. Hướng dẫn chạy

1. Giải nén ZIP trên Windows, mở `ScanAndRemoveVirus.sln` bằng Visual Studio.
2. Chọn **Build → Clean Solution → Rebuild Solution**.
3. Chạy chương trình từ Visual Studio, không dùng EXE cũ trong `Windows_Release` hoặc `bin`.
4. Trên sidebar, chọn **Quản trị** giữa **Lịch sử** và **Cài đặt**.
5. Kiểm tra 4 thẻ thống kê và các tab: Tổng hợp, Chữ ký, Lịch sử quét, Phát hiện, Cách ly, Lịch quét.
6. Nhấn **Quản lý chữ ký**, **Lịch quét tự động**, **VirusTotal API** và kiểm tra chức năng quản lý đang có.
7. Ngắt SQL để xác nhận giao diện vẫn phản hồi, có thông báo lỗi rõ ràng thay vì hiện số liệu giả.

**Trạng thái kiểm thử:** Đã kiểm tra tĩnh các liên kết và toàn vẹn các tệp SQL. Chưa biên dịch/kiểm thử runtime .NET Framework 4.7.2 trên Windows trong môi trường tạo gói.
