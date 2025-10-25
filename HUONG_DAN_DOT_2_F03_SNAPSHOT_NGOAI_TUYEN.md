# NÂNG CẤP ĐỢT 2: F03 - DỰ PHÒNG CHỮ KÝ KHI MẤT SQL

## Phạm vi đã lập trình

- Sau khi nạp **thành công toàn bộ** chữ ký đang bật từ SQL, engine tạo snapshot cho **MD5/SHA-1/SHA-256** và **CDC**.
- Dữ liệu được nén và bảo vệ bằng **Windows DPAPI (CurrentUser)**. Snapshot có phiên bản/UTC timestamp, được ghi file tạm rồi `File.Replace` (lưu bản `.bak`), không lưu API key.
- Khi không tải được chữ ký từ SQL, engine thử tải snapshot đã được DPAPI kiểm tra; snapshot giả mạo/hỏng/sai tài khoản Windows bị từ chối.
- Khi dùng snapshot: `SignatureSource = OfflineSnapshot`, `IsDegraded = true`, UI báo rõ **Bảo vệ hạn chế**; không đánh đồng không phát hiện với an toàn tuyệt đối.
- Không có snapshot: `SignatureSource = Unavailable`, vẫn có thể thực hiện heuristic nhưng thông báo giảm khả năng phát hiện.
- Khi SQL khả dụng và có migration 003, lưu nguồn chữ ký và thời điểm snapshot vào bảng `ScanSignatureMetadata`.

## Đường dẫn dữ liệu

Mặc định: `%LOCALAPPDATA%\ScanAndRemoveVirus\offline-signatures.v1.dat`, bản sao trước `.bak`.

Chế độ test có thể đặt biến môi trường `XVIRUS_DATA_DIR` để cách ly dữ liệu. Snapshot gắn với tài khoản Windows hiện tại; không sao chép nó sang máy hoặc người dùng khác để sử dụng.

## Triển khai SQL (tùy chọn)

1. **Sao lưu AntivirusDB**.
2. Chạy `Migrations/003_ScanSignatureMetadata.sql`. Script tạo bảng mới khi chưa tồn tại, không xóa hoặc sửa dữ liệu cũ.
3. Giữ nguyên hai migration 001, 002 từ đợt trước nếu chưa chạy.

Nếu chưa chạy migration 003, chương trình vẫn quét; chỉ không lưu được bảng nguồn chữ ký.

## Thử nghiệm trên Windows

1. **Build -> Rebuild Solution** trong Visual Studio.
2. Chạy `VERIFY_OFFLINE_SNAPSHOT_WINDOWS.cmd`. Ca thử độc lập kiểm tra: lưu snapshot, mở lại, phát hiện SHA-256 bằng snapshot, từ chối snapshot bị chỉnh sửa.
3. Kết nối SQL Server, quét một thư mục vô hại để ghi snapshot. Xác nhận file `%LOCALAPPDATA%\ScanAndRemoveVirus\offline-signatures.v1.dat` tồn tại.
4. **Tạm dừng SQL Server trong máy ảo kiểm thử**, mở ứng dụng, quét lại tệp test. Kiểm tra hiển thị **Bảo vệ hạn chế** và nội dung "chữ ký ngoại tuyến".
5. Phục hồi SQL, thử quét tiếp để đảm bảo lại lấy chữ ký từ SQL.
6. Thử đổi sang tài khoản Windows khác hoặc chỉnh sửa file snapshot, xác nhận không thể dùng snapshot bị thay đổi.

**Không được thử cách ly/khôi phục khi mất SQL và coi đó là chức năng ngoại tuyến đã hoàn thành.** F03 mới thực hiện **giai đoạn A: đọc chữ ký**. Hàng đợi lịch sử/cách ly offline an toàn (giai đoạn B) chưa triển khai.

## Giới hạn và an toàn

- Snapshot có giới hạn kích thước dữ liệu nén để giảm rủi ro quá tải RAM; nếu quá lớn, không ghi đè bản cũ và phần mềm quét bằng tập chữ ký trong RAM của phiên hiện tại. Cần triển khai cách ghi streaming qua mã hóa/HMAC độc lập nếu kho chữ ký phát triển rất lớn.
- DPAPI không bảo vệ nếu phần mềm độc hại đã chiếm quyền tài khoản Windows tương ứng.
- Snapshot cũ chỉ là nguồn dự phòng, phải có cảnh báo ngày lưu. Không bảo đảm cập nhật được khi không có SQL.
- Các thao tác lịch sử, cách ly, cập nhật chữ ký vẫn có thể cần SQL. Không quảng bá "bảo vệ đầy đủ" khi mất SQL.
- Chưa kiểm thử biên dịch/chạy thực tế trong môi trường Windows ở đây; bắt buộc Rebuild và chạy test theo bước trên.

## Trạng thái các hạng mục F01-F14

| Mã | Trạng thái đợt này |
|---|---|
| F01 | Giữ quét ZIP và xử lý tên entry từ bản gần nhất; chưa hỗ trợ giải nén RAR/7z |
| F02 | Giữ nhập mẫu chờ xác minh từ đợt 1; UI duyệt chữ ký đầy đủ chưa có |
| F03 | **Đã thêm giai đoạn A**: snapshot hash/CDC DPAPI, fallback, cảnh báo suy giảm, metadata |
| F04 | Giữ khôi phục watcher/quét bù đợt 1; cần kiểm thử sự kiện thực tế |
| F05 | Giữ xác minh SHA-256 khi khôi phục đợt 1; journal nghiệp vụ đa bước chưa hoàn chỉnh |
| F06 | Giữ kiểm kê lỗi quét, đếm ZIP và `ScanFileIssues` từ đợt 1 |
| F07–F10, F12–F14 | Chưa triển khai đầy đủ trong đợt 2; ưu tiên F07/F09 sau kiểm thử F03 |
| F11 | Cải tiến CSV lịch sử: chặn chuỗi công thức khi mở bằng Excel; các bộ lọc/nội dung báo cáo mới chưa triển khai |


## Cải tiến nhỏ F11 - xuất CSV an toàn

`ScanHistoryStore.ExportCsv()` giữ mã hóa UTF-8 BOM và bổ sung thoát công thức cho ô dữ liệu bắt đầu bằng `=`, `+`, `-`, `@` (kể cả sau khoảng trắng đầu). Nội dung từ tệp/đường dẫn không được tự thực thi như công thức Excel khi người dùng mở báo cáo. Cần kiểm thử thêm trên Excel thực tế và chỉ coi đây là một bước của F11.
