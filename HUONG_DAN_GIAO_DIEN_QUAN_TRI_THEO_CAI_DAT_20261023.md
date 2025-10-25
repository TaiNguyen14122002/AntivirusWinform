# TRUNG TÂM QUẢN TRỊ — GIAO DIỆN THEO TRANG CÀI ĐẶT

Ngày: 23/10/2026. Dự án WinForms thuần, giữ nguyên bảng và cột SQL Server.

## 1. Thiết kế mới

Nhấn **Quản trị** từ sidebar sẽ yêu cầu đăng nhập quản trị riêng trước khi hiển thị trang. Trang quản trị chính dùng các **thẻ trắng** với tiêu đề và giải thích giống bố cục **Cài đặt**, có thanh cuộn khi cửa sổ thấp hoặc hẹp.

### Thẻ 1 — Chính sách quản trị

- **Tự động cập nhật chữ ký** (công tắc BẬT/TẮT): sử dụng nguyên trạng `FeatureFlags.AutoUpdateEnabled` → `FeatureFlags.Persist()` → `FeatureFlags.NotifyChanged()`; vẫn ghi `Settings.AutoUpdate` theo logic hiện có, vẫn xác nhận trước khi thay đổi.
- **Gửi mẫu tệp nghi ngờ** (công tắc BẬT/TẮT): sử dụng nguyên trạng `AppSettings.Load().SendSamples` và `SettingsRepository.UpdateSubmitSamples(...)`, tương ứng `Settings.SubmitSamples`. **Bật tính năng không đồng nghĩa tải mẫu lên Internet**; VirusTotal vẫn có xác nhận riêng trước thao tác gửi mẫu.

Cả hai tùy chọn chỉ được chỉnh trong Quản trị, không đưa trở lại Cài đặt người dùng.

### Thẻ 2 — Quản lý VirusTotal

- Kiểm tra trạng thái khóa API đã được lưu qua DPAPI, **không hiện khóa dưới dạng văn bản thuần**.
- Nút **Cấu hình API** mở `VirusTotalConfigForm` hiện có để lưu/gỡ/kiểm tra kết nối.
- Nút **Thông tin VirusTotal** mở công cụ quản trị chi tiết.
- Công tắc **Tự động tra cứu SHA-256** giữ nguyên `FeatureFlags.VtAutoQuery` (không tự gửi tệp lên API).

### Thẻ 3 — Dữ liệu chữ ký & cập nhật

- Thống kê tổng chữ ký và số đang kích hoạt bằng truy vấn SQL chạy nền, không chặn cửa sổ.
- Quản lý chữ ký; kiểm duyệt; cập nhật thủ công; kiểm tra dữ liệu; nguồn dữ liệu.

### Thẻ 4 — Công cụ quản trị nâng cao

- Tổng quan kỹ thuật; cấu hình bộ máy; quy tắc phát hiện; giám sát dịch vụ; giám sát hiệu năng; kiểm thử antivirus; cập nhật hệ thống; nhật ký; sao lưu/phục hồi; chẩn đoán.
- Các trang công cụ cũ vẫn được giữ lại, chỉ xuất hiện khi người quản trị mở công cụ tương ứng; có nút **Về trung tâm quản trị**.
- Có nút **Đăng xuất quản trị**.

## 2. Thay đổi mã nguồn

- Chỉnh sửa `Control/UcQuanTri.cs`: tạo trang chủ thẻ mới, giữ nguyên các module kỹ thuật đang có.
- Sửa câu lệnh thêm chữ ký bị khai báo hai lần `OUTPUT INSERTED.SignatureID` thành một lần.
- Giữ nguyên các hàm thực thi `ToggleAutoUpdate`, `ToggleSamples`, `ToggleAutoVT`.
- Không thay đổi `Sketch20View` sidebar, cơ chế đăng nhập Admin hoặc các phần quét/cách ly/lịch quét.
- Không đưa API key hoặc mật khẩu quản trị vào mã nguồn/tài liệu.

## 3. Kiểm thử trên Windows

1. Giải nén, mở `ScanAndRemoveVirus.sln`, chọn **Clean Solution → Rebuild Solution**.
2. Nhấn **Quản trị**: chưa đăng nhập không được nhìn thấy trang.
3. Sau khi đăng nhập, trang mặc định hiển thị 4 thẻ theo mẫu Cài đặt.
4. Thử bật rồi tắt **Tự động cập nhật chữ ký**: xác nhận, kiểm tra `dbo.Settings.AutoUpdate`; thử lại sau khi khởi động lại.
5. Thử bật rồi tắt **Gửi mẫu tệp nghi ngờ**: xác nhận, kiểm tra `dbo.Settings.SubmitSamples`; chắc chắn không tự gửi mẫu khi chỉ bật công tắc.
6. Bấm **Cấu hình API**: kiểm tra trạng thái VirusTotal; thử tra SHA-256 nhưng không gửi tệp riêng tư ngoài ý muốn.
7. Bấm từng công cụ nâng cao rồi quay lại; khi rời sidebar Admin sang Tổng quan và quay lại phải đăng nhập lại.
8. Thu nhỏ cửa sổ; kiểm tra trang có thanh cuộn, nút không bị khuất.

## 4. Giới hạn

Không thể biên dịch WinForms .NET Framework 4.7.2 hoặc kết nối SQL Server Windows trong môi trường tạo tệp này. Cần chạy Rebuild và kiểm thử trên Windows. Các module kỹ thuật hiện có không được nâng cấp bộ máy phát hiện trong lần sửa giao diện này; chẳng hạn YARA vẫn không được nhận là đã tích hợp nếu chưa có runtime thực tế.

**Cấu trúc SQL Server được giữ nguyên hoàn toàn**: không tạo, xóa hoặc thay đổi bảng/cột, không sửa file `.sql` hay `.bak`.
