# Khắc phục tình trạng quét xong nhưng không phát hiện

## Nguyên nhân xác nhận trong mã nguồn

1. Sau khi tinh gọn, engine **KHÔNG** nhận diện mã độc bằng tên tệp, chuỗi prefix hoặc heuristic. Chỉ so sánh hash toàn tệp với bản ghi `VirusSignatures.IsActive=1` hoặc nhóm CDC đầy đủ đang kích hoạt.
2. `AntivirusDB.Hash.DemoData.sql` tạo `Demo.Hash.SafeFixture` **bị vô hiệu hóa (`IsActive=0`)**. Tệp chứa chuỗi mẫu sẽ không bị phát hiện cho đến khi bạn chủ động bật chữ ký này.
3. `ChunkSignatureGroups.ChunkingMode` mặc định **FixedWindow** để giữ tương thích dữ liệu cũ. Engine CDC chỉ sử dụng nhóm `ChunkingMode='CDC'` và tất cả bản ghi cha/con đang kích hoạt, đạt số chunk/ngưỡng.
4. App kết nối cố định `localhost\SQLEXPRESS` tới `AntivirusDB` bằng Windows Authentication. Chạy script trên instance/database KHÁC sẽ không làm engine có thêm chữ ký.
5. Trước đây, nếu SQL không có chữ ký hợp lệ thì giao diện vẫn có thể hiện thông báo không phát hiện như thể quét đủ. Bản sửa nay cảnh báo rõ khi thiếu chữ ký/CDC.
6. Đã sửa bộ nạp CDC bỏ qua chunk cuối nhỏ hơn MinChunkSize; đây là một trường hợp bỏ sót chữ ký hợp lệ.
7. Giao diện từng có bộ lọc ở `LoadDetectedThreats` có thể **giấu kết quả ThreatFound đã được engine phát hiện**, khiến trang Tổng quan xuất hiện "An toàn" dù dữ liệu quét có mối đe dọa. Bản này luôn hiển thị các kết quả đã được engine xác nhận; bộ lọc đầu vào cần áp dụng tại bước chọn tệp, không được lọc bỏ kết quả đã phát hiện.

## Các bước kiểm tra

1. **Sao lưu** database trước khi thay đổi bất kỳ dữ liệu nào. Mở SSMS, kết nối chính xác `localhost\SQLEXPRESS` (hoặc sửa `DbConnection.cs` cho đúng instance dùng trong app), chạy `KIEM_TRA_KHONG_PHAT_HIEN.sql` (CHỈ SELECT).
2. `Build > Rebuild Solution` và chạy đúng file .exe vừa build, không chạy file cũ trong `Windows_Release`.
3. Mở **Developer Command Prompt for Visual Studio**, chạy `KIEM_TRA_ENGINE_SQL.cmd`. Nó cho biết SQL kết nối đến server/database nào, tải được mấy chữ ký, hash SHA-256 file thực tế và kết quả engine.
4. Chỉ để kiểm thử an toàn: chạy `BAT_MAU_HASH_THU_NGHIEM_AN_TOAN.sql` trên đúng instance. Tệp mẫu là `Tests\MauKiemThuHash-ViDu.txt` (40 byte ASCII không BOM), SHA-256: `ea9958fec77e564d36bd910ca32d15004e41fdb21bfb7c1dca414da943ee3eeb`. Sau đó chạy lại `KIEM_TRA_ENGINE_SQL.cmd` — phải hiện `DETECTED` và `SignatureID`.
5. Trong giao diện, chọn **Quét tệp**, chọn đúng `Tests\MauKiemThuHash-ViDu.txt`. Chữ ký phải nhận diện được. Không nên sửa nội dung hoặc lưu lại tệp bằng trình soạn thảo thêm BOM/line break vì SHA-256 sẽ thay đổi.
6. Quét một tệp khác, dùng `KIEM_TRA_ENGINE_SQL.cmd "C:\duong-dan\tep-can-quet.bin"` và xem `SHA256 file thuc te` và `SHA256 ton tai trong snapshot`; nếu false, không có chữ ký toàn tệp tương ứng. Nếu không có nhóm CDC đã kích hoạt, chunk cũng không thể khớp.

## Cách kiểm tra CDC

- Chạy migration CDC trên đúng database; nó không tự thêm nhóm chữ ký được bật.
- Dùng `Tests\CdcSignatureBuilder` để tạo chữ ký từ **mẫu đã kiểm chứng**; script xuất ra nhóm ở trạng thái `IsActive=0`, cần rà soát trước khi bật.
- Nên kiểm tra cả số `ChunkSignatures` hoạt động, kích thước/thuật toán cắt trùng profile và `MinMatchCount`, `SimilarityThreshold` trước khi kết luận engine bỏ sót.
- CDC cần tối thiểu 2 chunk; tệp rất ngắn như EICAR 68 byte không phù hợp làm bài kiểm thử CDC. Với EICAR, nên kiểm tra phương pháp hash toàn tệp và dùng bản chuẩn không thêm ký tự.

## Cảnh báo

- Khớp chính xác hoặc tương đồng với chữ ký SQL chỉ chứng minh có phần dữ liệu giống mẫu đã lưu; chữ ký CDC phải được kiểm chứng để tránh phát hiện nhầm.
- Không bật lại heuristic/tên tệp chỉ để tăng số lượng phát hiện giả.
- Nếu Windows Defender đã cách ly tệp thử nghiệm trước khi quét, chương trình không thể đọc tệp đó.
- Chưa có xác nhận trên Windows/.NET Framework tại thời điểm đóng gói. Cần chạy thử thực tế, gửi ảnh hộp thoại cảnh báo hoặc log `KIEM_TRA_ENGINE_SQL.cmd` nếu còn lỗi.
