# Hoàn thiện 6 chức năng ở trang Bảo vệ (25/10/2026)

## Phạm vi

Bản vá sử dụng **AntivirusWinform_VirusTotal_BangMoRong_TimKiemGocVuong_20261025.zip** làm cơ sở. Không thay đổi cấu trúc SQL Server, thuật toán phát hiện của `ScanEngine`, hay logic đăng nhập trang Quản trị.

Các công tắc và nút bánh răng ở trang **Bảo vệ** được nối với engine/guard đang có, không chỉ đổi màu giao diện:

| Mô-đun | Cơ chế hoạt động | Giới hạn thực tế |
|---|---|---|
| Bảo vệ thời gian thực | `RealTimeProtection` giám sát thư mục thuộc chế độ quét nhanh, đưa sự kiện vào hàng đợi hữu hạn, quét tệp ổn định | Không phải minifilter driver; không đảm bảo chặn tệp trước khi chạy; chỉ hoạt động khi app còn mở |
| Bảo vệ tệp | **Mới**: theo dõi tệp được tạo/sửa trong Desktop, Documents, Downloads; quét bằng `ScanEngine.EvaluateFile` và giữ kiểm tra khi khôi phục cách ly | Không phát hiện sự kiện **chỉ mở/đọc** tệp; bỏ qua báo cáo “đã kiểm tra” với tệp còn ghi; có giới hạn hàng đợi |
| Bảo vệ USB | `GuardService` kiểm tra ổ removable mới (chu kỳ 5 giây), chạy phiên quét toàn ổ và ghi một lần quét tổng hợp | USB đã cắm sẵn trước lúc bật cần quét thủ công; không chặn AutoRun |
| Bảo vệ tải xuống | Theo dõi Downloads, kiểm tra tệp ổn định có dấu nguồn Internet `Zone.Identifier` (MOTW) | Một số hệ thống/tệp tải không có MOTW; không đảm bảo chặn tệp trước khi mở |
| Giám sát hành vi | WMI giám sát tiến trình; FileSystemWatcher tìm chuỗi thay đổi tệp đáng ngờ; cảnh báo heuristic | Không tự kết luận chắc chắn nhiễm mã độc và không tự cách ly nếu chỉ có heuristic; WMI có thể bị giới hạn quyền |
| Bảo vệ thư mục khởi động | Theo dõi thư mục Startup người dùng/toàn máy; quét tệp mới thêm | Chưa theo dõi khóa Run/RunOnce trong Registry hay Scheduled Tasks |

### Cải tiến dùng chung

- Hộp thoại riêng cho nút **bánh răng**: xem trạng thái thực tế, phạm vi đang giám sát, giới hạn, bật/tắt và khởi động lại bộ giám sát (nếu hỗ trợ).
- Trạng thái **Cần kiểm tra** khi đã yêu cầu bật nhưng watcher không khởi động hoặc bị gián đoạn.
- Hạn chế quá tải: Bảo vệ tệp kiểm tra với tối đa 2 tác vụ đồng thời, hàng đợi tối đa 256 mục, báo vấn đề khi vượt ngưỡng thay vì im lặng bỏ qua.
- Không quét lại tệp thuộc phạm vi RT đang giám sát bình thường; Bảo vệ tệp có thể kiểm tra bổ sung nếu RT suy giảm hoặc đã tắt.
- Ghi rõ lỗi quét không thành công vào LocalScanIssueStore.
- Dừng watcher/timer khi đóng ứng dụng.
- USB tránh ghi trùng số liệu: một phiên quét tổng hợp và các cảnh báo phát hiện riêng, không tạo bản ghi lịch sử mới cho mỗi phát hiện trong cùng phiên.

## Cách cập nhật

1. Sao lưu dự án, đặc biệt cấu hình kết nối và SQL Server.
2. Dùng bản đầy đủ **hoặc** chép 8 tệp C# từ bản vá nhỏ vào đúng thư mục.
3. Mở `ScanAndRemoveVirus.sln` trong Visual Studio trên Windows.
4. **Build → Clean Solution → Rebuild Solution**.
5. Khởi chạy và kiểm tra chức năng trên máy thử nghiệm có quyền đọc các thư mục liên quan.

## Quy trình kiểm thử chức năng

1. **RT**: bật công tắc, tạo một tệp vô hại ở thư mục quét nhanh, xác nhận watcher không báo lỗi; kiểm tra `RealTimeProtection.ScannedEvents` khi có thay đổi. Tắt RT và thử lại để xác nhận không chạy.
2. **Bảo vệ tệp**: tắt RT để phân biệt hai guard; bật bảo vệ tệp, tạo/sửa tệp thử ở Documents; theo dõi log và `LocalScanIssueStore`. Kiểm tra trường hợp tệp đang bị khóa. Thử tắt guard rồi tạo tệp, xác nhận không có quét mới.
3. **USB**: bật bảo vệ USB, cắm ổ USB có một số tệp thử không nguy hiểm; xác nhận xuất hiện lượt quét USB ở Lịch sử. Rút ra/cắm lại. Kiểm thử ổ đang gắn từ trước riêng biệt.
4. **Tải xuống**: tải một tệp thử an toàn bằng trình duyệt vào Downloads trên NTFS, kiểm tra nó có Alternate Data Stream `Zone.Identifier` và guard có phản hồi phù hợp. Nếu file không có MOTW thì chức năng không tự nhận là đã quét.
5. **Hành vi**: chạy bộ kiểm thử `Tests/BehavioralActivitySelfTest.cs` theo dự án test hiện có và kiểm tra WMI đã khởi tạo trong nút bánh răng. Đừng dùng mã độc thật trên máy chính.
6. **Startup**: tạo/sửa một tệp thử vô hại trong thư mục Startup của tài khoản thử, xác nhận sự kiện được giám sát; không thử bằng tệp tự chạy nguy hiểm.
7. Với mỗi mô-đun, thử **bật → tắt → bật**, mở hộp thoại bánh răng, xem trạng thái và phạm vi theo dõi; kiểm tra sau khi đóng/mở ứng dụng trạng thái được giữ theo cấu hình.

**Quan trọng:** Đây là công cụ nghiên cứu WinForms, không phải giải pháp antivirus có driver kernel và cập nhật bảo mật được chứng nhận. Không tắt Microsoft Defender chỉ vì các công tắc trong ứng dụng hiển thị “Đang bật”.

## Kiểm tra đã thực hiện trong bản vá

- Đối chiếu 8 tệp C# thay đổi với bản cơ sở.
- Kiểm tra dấu ngoặc/phạm vi chuỗi, và sự tồn tại của các điểm nối `Configure`, `ExactShowProtectionDetails`, `GuardMessages`.
- **Chưa biên dịch trên Windows hay thử thực tế** `FileSystemWatcher`, WMI, ổ USB, Task Scheduler và SQL Server. Các bước trên là kiểm thử cần thực hiện sau khi Rebuild.
