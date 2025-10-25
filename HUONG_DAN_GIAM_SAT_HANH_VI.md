# Giám sát hành vi trên Windows (runtime behavioral detection)

## Mục đích

Bổ sung **giám sát các hoạt động thực sự đang xảy ra**; không đồng nhất với phân tích heuristic tĩnh của một file khi quét thủ công.

- **Hash toàn tệp → CDC → heuristic tĩnh** là luồng quét file trước đây, không thay đổi.
- **Giám sát hành vi** chạy trong nền khi tùy chọn **Bảo vệ > Phát hiện hành vi đáng ngờ** bật.
- Dùng `System.Management` WMI cho sự kiện bắt đầu tiến trình mới (poll tối đa 2 giây). Không chạy mã độc, không inject, không tự chặn tiến trình.
- Dùng `FileSystemWatcher` cho Desktop, Documents, Downloads và các thư mục con trong lúc chương trình đang chạy.
- `BehavioralActivityAnalyzer` tương quan các sự kiện trong **30 giây**, phát hiện: ít nhất **12 tài liệu đổi đuôi**; ít nhất **6 đổi đuôi và 6 xóa**; hoặc ít nhất **35 tài liệu khác nhau bị sửa**.
- Chấm điểm và báo cáo căn cứ rõ ràng. Khóa cảnh báo trùng **2 phút mỗi thư mục** và có giới hạn bộ nhớ 128 thư mục, 512 sự kiện/thư mục.
- Các dấu hiệu tiến trình được quan sát: Office sinh shell, PowerShell encoded command, shell tải và chạy nội dung, tệp thực thi trong TEMP. Các dấu hiệu này có thể hợp lệ, nên chỉ báo **nghi vấn**.

## Hành vi khi phát hiện

- Hiển thị hộp thoại cảnh báo (nếu tùy chọn Thông báo đang bật).
- Ghi `dbo.ScanHistory.ResultStatus = 'Suspicious'`, `ThreatCount = 0`, lý do trong `ScanLocation`. Tab Lịch sử hiển thị **Nghi vấn hành vi**.
- **Không tự cách ly** các cảnh báo chỉ có dấu hiệu hành vi, ngay cả khi người dùng bật Tự động cách ly.
- Khi một file có chữ ký MD5/SHA-1/SHA-256 hoặc CDC đã xác nhận, cơ chế phát hiện và cách ly hiện có vẫn áp dụng bình thường.

## Giới hạn rất quan trọng

1. Đây là **quan sát sự kiện đang diễn ra**, không có khả năng suy ra lịch sử hành vi của thư mục đã tồn tại chỉ bằng thao tác 'Quét thư mục'. Khi quét thư mục, chỉ có thể dùng hash/CDC/heuristic tĩnh.
2. `FileSystemWatcher` **không cung cấp PID**, nên cảnh báo sửa/đổi tên hàng loạt chỉ gắn với **thư mục**, không được quy trách nhiệm cho một chương trình riêng.
3. WMI có thể không hoạt động, thiếu quyền đọc `CommandLine`/`ExecutablePath` hoặc bỏ sót tiến trình rất ngắn; watcher có thể quá tải và mất sự kiện. Đây **không phải EDR đầy đủ**, không chặn ransomware và không cung cấp bảo đảm phát hiện mọi mẫu mã độc.
4. Chỉ theo dõi phạm vi thư mục nêu trên và trong thời gian ứng dụng đang chạy. Có thể bỏ sót ổ mạng, đường dẫn khác, việc ghi dữ liệu không sinh sự kiện file hoặc nguồn khác.
5. Một số phần mềm sao lưu, biên tập ảnh, đồng bộ đám mây có thể tạo chuỗi hoạt động tương tự. Chỉ coi là **nghi vấn**, không xác nhận mã độc.
6. SQL Server cần truy cập được để lịch sử lưu lâu dài; cảnh báo vẫn có thể hiện ra trong giao diện nếu lưu lịch sử thất bại.

## Kiểm thử không dùng mã độc thật

1. Mở dự án bằng Visual Studio, `Build > Rebuild Solution`.
2. Trong **Developer Command Prompt** chạy: `VERIFY_BEHAVIOR_WINDOWS.cmd`.
3. Bộ test C# sinh ra các **sự kiện mô phỏng trong bộ nhớ**, không sửa tệp thật, kiểm tra: Office sinh shell, PowerShell mã hóa, 12 lần đổi tên, 35 lần sửa, chống lặp, reset.
4. Muốn kiểm tra giám sát thật, bật `Phát hiện hành vi đáng ngờ`, tạo 12 file `.txt` vô hại trong một thư mục con `Desktop`, rồi đổi tên chúng sang `.locked` trong khoảng 30 giây. Có thể xem `Lịch sử` / thông báo. **Không thực thi mẫu lạ.**

Không yêu cầu bổ sung bảng SQL. Cột `ResultStatus` hiện tại đã hỗ trợ chuỗi `Suspicious`.
