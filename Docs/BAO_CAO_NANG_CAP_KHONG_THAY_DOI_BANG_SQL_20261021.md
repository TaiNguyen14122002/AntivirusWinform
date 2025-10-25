# Báo cáo triển khai bổ sung — giữ nguyên cấu trúc SQL Server

**Ngày:** 21/10/2026  
**Dự án:** ScanAndRemoveVirus, WinForms C# 7.3, .NET Framework 4.7.2  
**Ràng buộc bất biến:** KHÔNG `CREATE TABLE`, `ALTER TABLE`, `DROP TABLE`, thêm cột hoặc thay cấu trúc bất kỳ bảng SQL Server nào.

> **Quan trọng:** Đây là mã nguồn sửa đổi phục vụ kiểm thử, **không phải** bộ antivirus đã được chứng nhận/kiểm thử trên Windows thực tế. Môi trường thực hiện hiện tại không có Windows, MSBuild/C# Compiler, SQL Server hoặc bộ mẫu mã độc độc lập. Không thể xác nhận mọi chức năng đã hoạt động đúng trên Windows 10/11. Các chức năng cần driver/filter driver/dịch vụ độc lập vẫn chưa hoàn thành. Không chạy thử trên máy chứa dữ liệu quan trọng trước khi backup và kiểm thử trên VM.

## 1. Các phần đã thay đổi trong mã nguồn

| Hạng mục | Trạng thái | Giải pháp cụ thể | Tệp |
|---|---|---|---|
| RT-01: Bảo vệ thời gian thực tránh quá tải | Đã sửa mã nguồn, cần test Windows | Hàng đợi bounded 2.048 phần tử, 2 worker nền, chống trùng sự kiện, đợi tệp ổn định, giám sát sự kiện bị bỏ qua | `Services/RealTimeProtection.cs` |
| RT-02: Tái khởi động watcher và quét bù | Đã sửa mã nguồn, cần test stress | Nếu watcher lỗi/overflow, đánh dấu bảo vệ suy giảm, thử tạo lại watcher, quét lại thư mục bị ảnh hưởng; không tự coi tệp chưa kiểm tra là sạch | `Services/RealTimeProtection.cs` |
| ISSUE-01: Ghi nhận tệp chưa kiểm tra | Đã thêm mã nguồn và giao diện | Ghi nhật ký JSONL ngoài SQL về tệp không đọc được, archive bị lỗi, sự kiện realtime; giao diện lọc và thử quét lại đường dẫn vật lý | `Services/LocalScanIssueStore.cs`, `Database/ScanIssueRepository.cs`, `Control/ScanIssuesForm.cs` |
| Q-01: Giao dịch cách ly/khôi phục có nhật ký | Đã thêm, cần kiểm thử crash/mất điện | Ghi trạng thái `Prepared` trước di chuyển NTFS, `FileMoved`, `Completed`/`RolledBack`; phát hiện giao dịch dở dang và đối soát tình trạng file (không tự ý di chuyển/xóa) | `Services/QuarantineRecoveryJournal.cs`, `Services/ScanEngine.cs`, `Control/RecoveryJournalForm.cs` |
| OFF-01: Đồng bộ lịch sử khi SQL lỗi | Đã thêm mã nguồn, cần kiểm thử giao dịch thật | Ghi JSON vào LocalAppData trước SQL, mỗi bản ghi có GUID; INSERT/SELECT idempotent bằng `ActivityDetails` có sẵn; retry khi mở app, mỗi 5 phút và bằng nút thủ công | `Services/OfflineHistoryQueue.cs`, `Services/ScanHistoryStore.cs`, `Program.cs` |
| ARC-01: Mở rộng quét nén | Đã thêm mã nguồn, cần chạy selftest trên Windows | Quét TAR, GZ, TGZ, TAR.GZ; giữ ZIP gốc; giới hạn thời gian/số entry/dung lượng/độ sâu; từ chối traversal, symlink và special TAR | `Services/ArchiveScanService.cs`, `Services/ScanEngine.cs` |
| ARC-02: Báo chính xác định dạng chưa hỗ trợ | Đã thêm | RAR/7z/ISO và các archive lồng nhau chưa kiểm tra sâu được ghi rõ là chưa kiểm tra (không đánh dấu sạch) | `Services/ArchiveScanService.cs` |
| PE-01: Metadata EXE/DLL và chữ ký số | Đã thêm mã nguồn, chưa kiểm chứng WinVerifyTrust | Đọc DOS/PE header, kiến trúc, section count, chứng chỉ khai báo, dùng `WinVerifyTrust` của Windows để xác minh ở chế độ cache-only; không chạy tệp | `Services/PortableExecutableInspector.cs`, `Control/PeInspectForm.cs` |
| REPORT-01: Báo cáo HTML chi tiết hơn | Đã thêm | Tổng số tệp, phát hiện, phiên lỗi/nghi vấn, số bản ghi cục bộ chưa đồng bộ, số sự kiện chưa được kiểm tra | `Services/ScanReportExporter.cs` |
| UI-01: Trung tâm công cụ | Đã nối vào menu Cài đặt | Thêm Tệp chưa kiểm tra, Đối soát cách ly, Đồng bộ lịch sử, Phân tích EXE/DLL; tăng chiều cao khối cho giao diện co giãn | `Control/UcScreens.Sketch.cs`, `.csproj` |

## 2. SQL Server giữ nguyên tuyệt đối

- **Không thay đổi byte nào** trong `AntivirusDB.sql`, `AntivirusDB.schema.sql`, `AntivirusDB.extensions.sql`, `AntivirusDB.ChunkHash.Migration.sql`, các tệp `Migrations/001`–`004` và toàn bộ `.sql` khác của gói ban đầu.
- **Không sửa** `AntivirusDB.bak`, connection string, tên bảng/cột, khóa chính, khóa ngoại hoặc index.
- Các code đã có trong dự án ban đầu **vẫn có thể đòi hỏi các migration cũ** (F02/F07/F09/F10/F11). Nếu database chưa có bảng/cột các đợt trước, chức năng cũ tương ứng sẽ báo lỗi. Gói lần này **không tự chạy migration**.
- Bảng `dbo.ScanHistory` vẫn ghi bằng schema hiện tại. Để tránh lặp lịch sử khi mất SQL, giá trị `ActivityDetails` ở **bản ghi mới** được gắn tiền tố `EventId:<GUID>|`, còn `ScanLocation` giữ nguyên phạm vi quét. Điều này thay đổi *nội dung dữ liệu* cột có sẵn, **không thay đổi cấu trúc bảng**.
- Cần backup CSDL trước khi thử các tính năng có thao tác dữ liệu thật.

### Vị trí lưu dữ liệu bổ sung không nằm trong SQL

- `%LOCALAPPDATA%\ScanAndRemoveVirus\PendingHistory\`: bản ghi lịch sử chờ đồng bộ, mỗi giao dịch một JSON. Nên giới hạn quyền đọc theo tài khoản Windows.
- `%LOCALAPPDATA%\ScanAndRemoveVirus\RecoveryJournal\`: nhật ký NTFS/SQL để chẩn đoán thao tác cách ly/khôi phục dang dở.
- `DataDir.Resolve("scan-issues.jsonl")`: nhật ký các tệp chưa kiểm tra, chịu ảnh hưởng của `XVIRUS_DATA_DIR` khi dùng để chạy kiểm thử.

**Giới hạn quan trọng:** Cách ly mới **vẫn cần SQL Server** để tạo `DetectionID`. Hàng đợi offline mới chỉ đảm bảo phần lịch sử quét, **không cho phép cách ly ngoại tuyến an toàn**, không đồng bộ `ThreatDetections` hoặc `ScanFileIssues` khi SQL mất kết nối. Những thao tác này không được giả định là đã hoàn thành.

## 3. Các chức năng chưa thể kết luận hoàn thiện

| Chức năng | Trạng thái / việc còn lại |
|---|---|
| Quét sâu RAR/7z/ISO | **Chưa tích hợp.** Cần thư viện/decoder đáng tin cậy, test với file mã hóa, bom giải nén và đường dẫn độc hại; không dùng `7z.exe` tùy tiện. |
| YARA thực thụ | **Chưa tích hợp.** Cần YARA runtime chính thức, cơ chế quản lý/quyền duyệt rule, test timeout/false positive. |
| Phân tích ransomware nâng cao và ngăn chặn tiến trình | **Chưa triển khai đầy đủ.** Hiện vẫn có giám sát hành vi và cảnh báo kế thừa; chưa chứng minh khả năng chặn. |
| Bộ quét incremental an toàn | **Chưa triển khai.** Cache theo mtime/size không đủ an toàn; cần cache keyed signature epoch/hash và chính sách buộc quét lại. |
| Quản lý ứng dụng tự khởi động chuyên sâu | **Chưa hoàn tất.** F10 hiện kiểm tra trạng thái, chưa phân tích toàn bộ dịch vụ, scheduled task, Registry Run. |
| Quy tắc phân loại rủi ro toàn diện | **Chưa hoàn tất.** Có heuristic/matching cũ; chưa có mô hình thống nhất chất lượng bằng chứng. |
| Rollback nguyên bộ chữ ký | **Chưa triển khai.** F02 duyệt/audit chữ ký đã có; rollback batch cần triển khai/quyền hạn và test SQL transaction. |
| Offline queue toàn bộ phát hiện và cách ly | **Chưa triển khai.** Không thể đảm bảo transaction end-to-end chỉ bằng JSON và SQL schema hiện tại nếu không thiết kế chi tiết đối soát. |
| Windows Service bảo vệ sau khi đóng UI | **Chưa triển khai.** Watcher mới chỉ chạy khi tiến trình WinForms đang chạy. |
| Bộ kiểm thử false-positive, false-negative, stress Windows | **Chưa chạy.** Cần VM riêng và tập mẫu hợp pháp, có nhãn độc lập. |
| Kiểm tra nâng cao quyền SQL/ACL | **Chưa tự động cài đặt phân quyền.** Cần giới hạn tài khoản DB, quyền NTFS và loại bỏ xác thực quản trị thuần UI nếu dùng thật. |
| Kiểm thử chức năng F02/F07/F09/F10/F11 trong ZIP gốc | **Chưa nghiệm thu lại**; không coi như đã hoàn thiện chỉ vì có source. |

## 4. Hướng dẫn build và kiểm thử trên Windows

**Bắt buộc:** Windows 10/11, Visual Studio 2022 với workload **.NET desktop development** và **.NET Framework 4.7.2 Developer Pack/Targeting Pack**.

1. Backup thư mục dự án và SQL Server `AntivirusDB` (bao gồm dữ liệu cách ly). Tốt nhất thử trên máy ảo Windows, không dùng dữ liệu quan trọng.
2. Mở `ScanAndRemoveVirus.sln`, đặt `ScanAndRemoveVirus` làm Startup Project, **Clean Solution** → **Rebuild Solution**. Hoặc Developer Command Prompt:

   ```bat
   msbuild ScanAndRemoveVirus.sln /t:Rebuild /p:Configuration=Debug /v:minimal
   ```

3. Chạy `VERIFY_ARCHIVE_SCAN_WINDOWS.cmd` để kiểm tra ZIP/TAR/GZ/TGZ. Nếu máy có SQL đúng schema hiện tại, chạy thêm các script verify riêng của dự án.
4. Kiểm tra UI Cài đặt → Dữ liệu & công cụ: có đủ 4 công cụ mới. Chạy test từ thư mục tạm `XVIRUS_DATA_DIR` để tránh lưu vào dữ liệu thật.
5. Ngắt SQL có chủ đích sau khi backup, chạy một phiên quét, xác nhận `PendingHistory` được tạo rồi bật SQL và chọn **Đồng bộ lịch sử**. Xem SQL `ScanHistory.ActivityDetails` để bảo đảm cùng `EventId` chỉ xuất hiện một lần.
6. Kiểm thử journal cách ly/khôi phục trong VM bằng tệp mẫu vô hại với chữ ký thử nghiệm; **không làm mất điện trên máy thật**.
7. Dùng nhiều tệp nhỏ thử làm đầy queue realtime, quan sát cảnh báo "Bảo vệ suy giảm", quét bù và log chưa quét. Không khẳng định độ tin cậy trước khi stress test.
8. Xem báo cáo HTML và bảo đảm người dùng có thể chọn ẩn đường dẫn riêng tư.

### Về tệp EXE có sẵn

`Windows_Release/ScanAndRemoveVirus.exe` là **EXE cũ từ ZIP đầu vào, không được build lại**. Đừng dùng EXE này để đánh giá các thay đổi. Phải **Rebuild** trên Windows rồi dùng `bin/Debug` hoặc `bin/Release` tương ứng. Không có lý do để thay cấu trúc SQL chỉ nhằm build bản này.

## 5. Kết quả kiểm tra trong môi trường tạo gói

- Đã so sánh checksum byte giữa các tệp `.sql` trong ZIP đầu vào và gói mới: **không tệp SQL nào thay đổi**.
- XML `.csproj` đọc hợp lệ, không thiếu file `<Compile>` và không trùng mục compile.
- Đã rà soát dấu ngoặc bằng lexer C# trên tất cả file C# thay đổi và thêm mới: không thấy mất cân đối dấu ngoặc.
- **Chưa thể chạy C# compiler, unit test, Windows/SQL integration test** trong môi trường hiện tại. Đây không phải xác nhận build pass.

## 6. Thứ tự nghiệm thu đề nghị

1. Build sạch .NET Framework 4.7.2 (bắt và sửa toàn bộ lỗi compiler nếu có).
2. Hồi quy quét ZIP, TAR/GZ, hash/CDC và báo cáo chưa kiểm tra.
3. Hồi quy giao dịch cách ly/khôi phục và xử lý crash giữa hai bước.
4. Hồi quy offline queue 20+ phiên, SQL mất kết nối và thử lại.
5. Stress FileSystemWatcher, không kết luận "đang bảo vệ tốt" nếu watcher suy giảm.
6. Kiểm tra false positive, quyền ứng dụng và mức độ nguy hiểm trước khi cho phép người dùng thật sử dụng.

**Lưu ý:** Không nên tuyên bố đã hoàn thiện "tất cả" nếu các mục phần 3 chưa được thực thi và nghiệm thu. Đây là đợt nâng cấp mã nguồn có giới hạn được công khai để có thể tiếp tục phát triển trên chính CSDL hiện có.
