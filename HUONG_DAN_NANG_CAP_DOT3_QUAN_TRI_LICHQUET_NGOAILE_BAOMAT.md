# Nâng cấp đợt 3: F02, F07, F09, F10, F11

## Đã tích hợp vào mã nguồn

- **F02:** Quản lý chữ ký có tìm kiếm, duyệt/kích hoạt, từ chối, vô hiệu hóa, đưa về chờ xác minh, xem lịch sử. `IsActive` và audit được cập nhật cùng transaction; invalidate snapshot trong RAM sau commit. Chữ ký di sản không tự tắt. Mẫu người dùng gửi mặc định ở trạng thái PendingReview, IsActive=0.
- **F07:** Trang **Cài đặt > Công cụ bảo mật > Lịch quét tự động**. Đăng ký Windows Task Scheduler theo XML; hỗ trợ Quick/Full/Custom, Daily/Weekly/Monthly/Once; hỗ trợ sửa, bật/tắt, chạy ngay, xóa. Task khởi chạy EXE với `--schedule-id <số>` và không phụ thuộc cửa sổ WinForms đang mở. Có mutex chống chạy trùng. Kết quả ScanHistory và ThreatDetections được lưu SQL; không tự cách ly.
- **F09:** Ngoại lệ chỉ cho **heuristic**, chọn file thật và chương trình tự tính SHA-256; tự hết hạn sau 1–90 ngày, có thể thu hồi. **Không áp dụng cho phát hiện hash/CDC.** SQL lỗi => không bỏ qua cảnh báo.
- **F10:** Kiểm tra chỉ đọc: trạng thái Firewall theo profile, danh sách sản phẩm antivirus do Windows Security Center báo, trạng thái dịch vụ Windows Update, số mục Registry Run, BitLocker khi WMI cho phép. Từng phép kiểm tra độc lập, lỗi trả `Không xác định` và không thay đổi cấu hình máy.
- **F11:** Xuất tối đa 10.000 phiên quét gần nhất thành HTML. Encode dữ liệu HTML; hỏi trước khi bao gồm đường dẫn cá nhân. CSV chống công thức Excel đã được nâng cấp ở đợt 2.

## Cài đặt / nâng cấp SQL

1. **Sao lưu `AntivirusDB`** và thử migration trên DB dự phòng trước.
2. Nếu chưa từng chạy migration đợt 1/2: chạy `Migrations/001_QuanLyTrangThaiChuKy.sql`, `002_ScanFileIssues.sql`, `003_ScanSignatureMetadata.sql` theo thứ tự.
3. Chạy `Migrations/004_QuanLyChuKy_LichQuet_NgoaiLe.sql`. Script này kiểm tra sự tồn tại bảng/cột, giữ nguyên chữ ký cũ; tương thích `SignatureAuditLogs` từ migration 001.
4. Mở `ScanAndRemoveVirus.sln` trong Visual Studio với .NET Framework 4.7.2 và Rebuild Solution.
5. Vào **Cài đặt**, cuộn xuống **Công cụ bảo mật và quản trị**. Màn quản lý chữ ký, lịch quét, ngoại lệ yêu cầu xác thực quản trị.

## Nghiệm thu cần làm trên Windows

- **F02:** Tạo mẫu vô hại với IsActive=0; xác minh không phát hiện theo chữ ký, Duyệt rồi đối chiếu `IsActive=1` + bản ghi audit. Từ chối/vô hiệu hóa rồi quét lại. Thử SQL mất kết nối giữa lúc cập nhật.
- **F07:** Tạo lịch hằng ngày cho thư mục vô hại; mở Task Scheduler thấy `AntivirusWinform_Scan_<id>`. Thử `Chạy ngay`, kiểm tra `ScanHistory` và `LastOutcome`. Đóng WinForms nhưng **vẫn đăng nhập Windows**, thử đến giờ lịch. `InteractiveToken` không cam kết chạy khi user đăng xuất. Lịch vẫn cần SQL Server để đọc cấu hình và lưu kết quả.
- **F09:** Chọn tệp sạch gây cảnh báo heuristic nhưng KHÔNG khớp chữ ký, tạo ngoại lệ 1 ngày; xác minh cảnh báo heuristic được bỏ qua. Xác minh hash/CDC vẫn nhận diện bình thường. Thay đổi một byte của tệp rồi quét lại; ngoại lệ cũ không áp dụng. Vô hiệu hóa ngoại lệ và quét lại.
- **F10:** Thử khi WMI hoặc Firewall không truy vấn được: hạng mục phải hiển thị `Không xác định` thay vì giả định an toàn. Không tắt Defender để thử.
- **F11:** Đưa tên/đường dẫn chứa `&`, `<`, `>` vào bản ghi TEST, xuất HTML; trình duyệt phải hiện như văn bản, không thực thi HTML/JS. Thử hai cách có/không hiện đường dẫn.
- **UI:** Thử DPI Windows 100%, 125%, 150%; kéo cửa sổ hẹp. Bộ C# này chưa được biên dịch chạy thử trong môi trường tạo ZIP.

## Giới hạn và các việc CHƯA hoàn tất

- **F03 giai đoạn B:** chưa có hàng đợi cục bộ giao dịch đảm bảo cách ly/khôi phục khi SQL hoàn toàn offline. Chỉ có snapshot chữ ký ngoại tuyến giai đoạn A.
- **F04:** watcher/quét bù đã có từ đợt 1 nhưng cần stress test sự kiện tràn bộ đệm, ngắt mạng và lỗi thiết bị thật.
- **F05:** kiểm tra hash khi khôi phục đã có; chưa có transaction journal cục bộ hoàn chỉnh cho mọi trường hợp tắt nguồn giữa `File.Move` và ghi SQL.
- **F08:** có giám sát hành vi/cảnh báo; chưa chứng minh có thể chặn ransomware hoặc phục hồi dữ liệu.
- **F12:** chưa có `WinVerifyTrust` hoàn chỉnh và màn xác minh Authenticode thực tế.
- **F13:** YARA là plugin tùy chọn, **chưa tích hợp**, tránh thay đổi pipeline Hash → CDC → Heuristic đã thống nhất.
- **F14:** self-test sẵn có được giữ nguyên; chưa có benchmark TP/FP/TN/FN được đo trên bộ dữ liệu gán nhãn độc lập và chưa kiểm thử end-to-end các tính năng mới trên Windows/SQL Server thật.

**Không nên coi dự án đã đạt 100% F01–F14 chỉ nhờ có mã và giao diện.** Các thao tác hệ thống và dữ liệu bảo mật phải kiểm thử đầu cuối trước khi dùng chính thức.
