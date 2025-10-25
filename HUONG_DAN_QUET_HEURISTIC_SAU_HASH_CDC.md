# Hướng dẫn: Hash truyền thống → CDC → Heuristic Analysis

## 1. Thứ tự ưu tiên, dừng ngay khi có kết quả

1. **Hash truyền thống:** tính MD5, SHA-1 và/hoặc SHA-256 toàn tệp, chỉ với thuật toán có chữ ký đang hoạt động trong SQL Server. **Chỉ cần một hash trùng** (`OR`) là phát hiện theo chữ ký và dừng ngay, không chạy các bước sau.
2. **Chunk Hash + Sliding Window + Rolling Hash:** chỉ chạy khi hash toàn tệp không trùng. Rolling Hash xác định điểm cắt theo nội dung (CDC); đối chiếu SHA-256 từng chunk và điều kiện nhóm trong `ChunkSignatureGroups` / `ChunkSignatures`. Nếu nhóm khớp theo ngưỡng quy định, phát hiện theo chữ ký và dừng.
3. **Heuristic Analysis:** chỉ chạy khi **cả hai lớp hash đều không khớp**. Phân tích **tĩnh** nội dung script hoặc cấu trúc Portable Executable (PE), kết hợp nhiều dấu hiệu nghi vấn. Nếu đạt ngưỡng 65/100, hiển thị **Nghi vấn x/100** cùng các dấu hiệu cụ thể. Heuristic là **cảnh báo nghi vấn**, không phải xác nhận mã độc.
4. Nếu không phát hiện, chỉ có thể ghi **Không phát hiện qua các phương pháp hiện có**; không kết luận tệp chắc chắn an toàn.

## 2. Phạm vi heuristic và cơ chế tối ưu

- **Script:** `.ps1`, `.psm1`, `.vbs`, `.vbe`, `.js`, `.jse`, `.wsf`, `.hta`, `.bat`, `.cmd`. Các tổ hợp như tải nội dung mạng + thực thi; payload mã hóa + thực thi; lệnh xóa bản sao lưu + thực thi; tải nội dung + thiết lập tự khởi động.
- **Windows PE:** kiểm tra header MZ/PE hợp lệ và các section, quyền vừa ghi vừa thực thi, entropy cao tại section thực thi, tổ hợp API sửa bộ nhớ tiến trình + tạo luồng từ xa, hoặc tải nội dung + khởi chạy tiến trình.
- Không đánh dấu chỉ vì **tên tệp**, **đuôi tệp** hoặc **một từ khóa riêng lẻ**. Các quy tắc trên vẫn có thể phát hiện nhầm, đặc biệt với công cụ quản trị hệ thống.
- Heuristic đọc **tối đa 1 MiB đầu mỗi tệp**. Tệp thường (không phải PE và không thuộc nhóm script) được bỏ qua nhanh. Với tệp lớn, việc đọc giới hạn giúp khống chế RAM, nhưng có thể bỏ sót dấu hiệu nằm ngoài phần đã đọc.
- Không thực thi tệp, không chạy sandbox, không phân tích hành vi thời gian chạy; đây là **static heuristic**, không phải phân tích động hay mô hình AI được huấn luyện.

## 3. Kết quả, lịch sử và cách ly

- **Phát hiện bằng hash/CDC:** `ThreatFound.Kind = "Chữ ký"`, có `SignatureID` SQL tương ứng; quá trình xử lý/cách ly theo quy trình cũ.
- **Phát hiện bằng heuristic:** `ThreatFound.Kind = "Heuristic"`, `Reason = "Nghi vấn x/100: ..."`; `SignatureID = NULL`. Kết quả hiện ở danh sách mối đe dọa, ghi lịch sử và có thể xem chi tiết.
- **Để tránh cách ly nhầm:** heuristic không được **tự động cách ly** ngay cả khi bật tùy chọn tự cách ly. Người dùng có thể xem kỹ dấu hiệu, tra cứu thêm và chủ động cách ly.
- Không cần thay đổi cấu trúc `AntivirusDB`. Chữ ký SQL vẫn phục vụ hai phương pháp hash, heuristic sử dụng các quy tắc trong mã C#.

## 4. Cài đặt và kiểm thử

1. Mở `ScanAndRemoveVirus.sln` trong Visual Studio với .NET Framework 4.7.2 Developer Pack.
2. Chọn **Build → Rebuild Solution**.
3. Trong **Developer Command Prompt for Visual Studio**, chạy `VERIFY_HASH_CDC_HEURISTIC.cmd`.
4. Bộ kiểm thử tự tạo các **nội dung vô hại** trong thư mục tạm để kiểm tra: SHA-256/SHA-1/MD5, CDC, script bình thường, tổ hợp dấu hiệu nghi vấn bằng văn bản, UTF-16LE, PE mô phỏng không thể thực thi và hủy quét.
5. Kiểm thử trên Windows với SQL Server thật, đo tốc độ và tỷ lệ false positive trên các phần mềm hợp lệ trước khi dùng trong thực tế. Không cần sử dụng mã độc thật để kiểm thử các quy tắc minh họa.

**Giới hạn xác minh:** Trong môi trường tạo bản ZIP không có trình biên dịch .NET Framework và SQL Server Windows. Mã đã được rà soát tĩnh; chưa thể khẳng định bài kiểm thử C# hay thử nghiệm tích hợp đã chạy đạt cho đến khi kiểm tra trên máy Windows.
