# Tiến độ triển khai theo DE_XUAT_BO_SUNG_CHUC_NANG (21/10/2026)

**Căn cứ:** `Docs/DE_XUAT_BO_SUNG_CHUC_NANG_21_10_2026.md`.
**Nền tảng:** WinForms C#, .NET Framework 4.7.2, SQL Server `AntivirusDB`.

> Đây là **bản nâng cấp P0 đợt 1**, KHÔNG PHẢI bằng chứng đã nghiệm thu tất cả F01–F14. Môi trường đóng gói không có Visual Studio/SQL Server trên Windows nên chưa chạy được các bài kiểm thử tích hợp.

## Các thay đổi đã tích hợp vào mã nguồn

| Mã | Mức triển khai trong ZIP này | Nội dung |
|---|---|---|
| F01 | **Một phần có tích hợp vào luồng quét** | ZIP và ZIP lồng tới 3 cấp; hash toàn tệp + CDC + heuristic qua tệp tạm; đường dẫn ảo `archive.zip > entry`; giới hạn entry/dung lượng/CPU; chống Zip Slip; cách ly ZIP gốc **chỉ sau khi xác nhận**. RAR/7z/tar/gz mới ghi chưa hỗ trợ nội dung, không báo sạch. |
| F02 | **Một phần** | Mẫu do người dùng gửi mặc định `IsActive=0`; lưu `SourceName`, `VerificationStatus`, nhập audit nếu chạy migration 001; checkbox có thể bật bằng xác nhận thủ công. **Chưa có màn hình quản lý/duyệt độc lập với bằng chứng bắt buộc**. |
| F03 | **Chưa triển khai** | Chưa có snapshot chữ ký offline được xác minh và queue đồng bộ. Không khẳng định quét offline đầy đủ. |
| F04 | **Một phần** | Bắt `FileSystemWatcher.Error`, thử tạo lại watcher tối đa 3 lần, quét bù toàn cây giám sát, có `LastWatcherError`. **Chưa** có hàng đợi bounded/debounce theo metadata, checkpoint bền vững hoặc giao diện trạng thái. |
| F05 | **Một phần** | Kiểm tra SHA-256 của tệp `.qtn` với `FileSHA256` trong SQL trước khi khôi phục, chặn hash lệch/thiếu; chỉ cho cách ly khi đã tính được SHA-256; sửa `GuardService.RestoreWarningFor` dùng `QuarantinePath` thực từ SQL. **Chưa** có journal giao dịch/quản trị ACL hoàn chỉnh. |
| F06 | **Một phần** | `ScanResult.Issues`, số entry ZIP đã/chưa kiểm tra; ghi SQL `ScanFileIssues` (sau migration 002), trạng thái tổng quan không báo bảo vệ đầy đủ khi có tệp chưa kiểm tra. **Chưa** có màn hình tra cứu chi tiết/per-file trạng thái tất cả tệp sạch. |
| F07 | Chưa triển khai | Quét theo lịch qua Windows Task Scheduler. |
| F08 | Chưa nâng cấp trong đợt này | Bộ cảnh báo hành vi cũ được giữ; chưa có lớp evidence ransomware mới. |
| F09 | Chưa triển khai | Ngoại lệ theo SHA-256 có audit và thời hạn. |
| F10 | Chưa triển khai | Kiểm tra Firewall / Defender / Update / Startup / Backup. |
| F11 | **Một phần liên quan F06** | Lưu sự cố quét tại SQL; chưa có module báo cáo CSV/HTML cải tiến và nhật ký kỹ thuật thống nhất. |
| F12 | Chưa triển khai | Authenticode và metadata PE. |
| F13 | Chưa triển khai | Plugin YARA được duyệt và cách ly. |
| F14 | **Một phần** | Thêm `Tests/ArchiveScanSelfTest.csproj` và `VERIFY_ARCHIVE_SCAN_WINDOWS.cmd` với dữ liệu vô hại (nested ZIP, Zip Slip, thống kê). Các test cũ được giữ; chưa có báo cáo benchmark/detection-rate có ground truth. |

## Quan trọng: chạy migration trước khi dùng chức năng nhập mẫu nâng cấp

1. Sao lưu `AntivirusDB` bằng SSMS.
2. Chạy `Migrations/001_QuanLyTrangThaiChuKy.sql` rồi `Migrations/002_ScanFileIssues.sql` trên **đúng** `AntivirusDB`.
3. Không chạy lại `AntivirusDB.sql` để tránh ảnh hưởng dữ liệu cũ.
4. Các chữ ký đã có trước migration được đặt `VerificationStatus='Legacy'`; **không tự vô hiệu hóa** chúng.
5. Nhập mẫu mới theo Cài đặt: mặc định `IsActive=0` (thay đổi so với phiên bản cũ, thực hiện theo F02 để tránh false positive). Nếu bật thủ công, trạng thái mới được gọi `UserApproved`, không được coi là bằng chứng xác minh độc lập.

## Kiểm thử trong Windows

1. Giải nén ZIP ở vị trí mới và mở `ScanAndRemoveVirus.sln`.
2. Chạy **Build > Clean Solution** rồi **Build > Rebuild Solution** trong Visual Studio với .NET Framework 4.7.2 Developer Pack.
3. Trong **Developer Command Prompt for Visual Studio**, chạy `VERIFY_ARCHIVE_SCAN_WINDOWS.cmd`.
4. Thử quét ZIP có hai tệp văn bản vô hại, một tệp có hash thử nghiệm được thêm vào `VirusSignatures` trong **database test**. Chú ý file ZIP có thể đã khớp hash của chính nó nếu chữ ký tồn tại; kết quả cho entry vẫn cần hiển thị riêng.
5. Thử ZIP lồng ZIP và ZIP lỗi: trong popup phải có đếm `ZIP: ... đã kiểm tra / ... chưa kiểm tra`, không kết luận sạch khi có entry lỗi.
6. Với entry bị phát hiện, bấm **Cách ly** và kiểm tra popup xác nhận xử lý *toàn bộ ZIP gốc*; bấm Hủy phải giữ nguyên file.
7. Sao chép một tệp sạch vào thư mục cách ly test (không trên dữ liệu thật), tạo hash metadata nhất quán, thử khôi phục; sửa vài byte của file `.qtn` sau đó thử lại để xác nhận có cảnh báo lệch hash.
8. Ngắt watcher thử trong VM rồi quan sát `RealTimeProtection.LastWatcherError`; chưa có giao diện hiển thị chi tiết trong bản này.

## Giới hạn an toàn và hiện trạng cần tiếp tục

- File ZIP entry được giải nén từng phần vào thư mục có tên ngẫu nhiên dưới `%LocalAppData%/ScanAndRemoveVirus/ArchiveScanTemp`, được xóa khi xong; nếu antivirus khác đang khóa tệp hoặc ứng dụng tắt đột ngột, dữ liệu tạm có thể còn và cần dọn an toàn sau. **Không** thực thi nội dung.
- Cấu hình giới hạn ban đầu: 3 cấp nén, tổng 5000 entry, 512 MiB/entry, 2 GiB byte giải nén thực, tối đa 120 giây mỗi ZIP; đây chỉ là giới hạn kiểm thử, không bảo đảm phòng mọi kỹ thuật ZIP bomb.
- Không có tính năng xử lý mật khẩu ZIP; mã hóa/lỗi đọc được ghi trạng thái chưa kiểm tra. CRC ZIP cần kiểm thử theo khả năng xác minh của thư viện hệ thống.
- Đường dẫn entry ảo không phải tệp vật lý. Khi người dùng chấp thuận, thao tác cách ly chuyển ZIP gốc; lệnh xóa entry ảo sẽ không xóa dữ liệu hay báo thành công sai.
- `FileSystemWatcher` chỉ theo dõi thay đổi trong thư mục cấu hình khi ứng dụng đang chạy; không chặn mọi thao tác trước khi thực thi và không hoạt động như Windows kernel minifilter.
- Cần kiểm thử thực tế Windows 7, 10, 11, DPI 100/125/150%, SQL Server, Microsoft Defender, VM và quyền truy cập.

## Lộ trình tiếp theo

1. F03 snapshot chữ ký offline được bảo vệ bằng DPAPI và hàng đợi đồng bộ an toàn.
2. Hoàn thiện F02 quản lý chữ ký theo trạng thái với audit, người duyệt và bằng chứng; F05 journal giao dịch/khôi phục sang đường dẫn khác.
3. Hoàn thiện F04 queue bounded và quét bù theo checkpoint; F06 danh sách tệp chưa kiểm tra có lọc và xuất CSV.
4. F07 quét theo lịch, F09 ngoại lệ hẹp, F11 báo cáo, F10 kiểm tra bảo mật Windows.
5. F08 chuyên sâu; F12 PE/Authenticode; F13 YARA tùy chọn; F14 bench với bộ nhãn độc lập.
