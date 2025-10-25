# Hoàn thiện tra cứu VirusTotal tự động — 24/10/2026

## Kết quả

- Khi mở **Chi tiết tệp** hoặc chuyển sang tab **VirusTotal**, chương trình tự động tra SHA-256 (nếu đã cấu hình API và bật **Tự động tra cứu SHA-256** trong trang Quản trị).
- Không cần bấm nút **Tra cứu VirusTotal**. Nút còn lại để **Tra cứu lại** (làm mới, không dùng cache).
- Trong lúc chờ có dòng **Đang tra cứu...**; HTTP 200 hiện chi tiết engine/phát hiện; HTTP 404 hiện **Chưa có báo cáo trên VirusTotal (không đồng nghĩa an toàn)**; HTTP 401/403/429/mất mạng hiện lỗi phù hợp, KHÔNG đánh đồng với 404.
- Sử dụng SHA-256 sẵn có từ lịch sử/quarantine khi hợp lệ. Khi chưa có, tính SHA-256 từ tệp trên máy ở luồng nền; tận dụng lượt tính hash hiện có để tránh đọc một tệp hai lần.
- Cache kết quả theo hash trong lần chạy: 30 phút nếu có báo cáo, 3 phút nếu 404. Những lỗi kết nối không cache. Chỉ tra tệp mà người dùng thực sự mở chi tiết, không tự quét toàn bộ danh sách khiến tốn quota.
- Không tự động upload tệp. Nút **Gửi tệp (cần xác nhận)** cần phiên quản trị và xác nhận rõ ràng trước khi gửi tệp ra Internet.

## Vì sao vẫn có thể thấy "Chưa có báo cáo"?

Đây là phản hồi HTTP 404 thực tế của API `/api/v3/files/{sha256}` — không thể tự tạo báo cáo chỉ bằng hash. Kiểm tra SHA-256 của tệp (tab **Thông tin chi tiết**) và thử tìm hash đó trực tiếp trên https://www.virustotal.com/. Nếu muốn VirusTotal phân tích mẫu chưa tồn tại, cần chọn **Gửi tệp (cần xác nhận)**, đọc cảnh báo quyền riêng tư, chấp thuận tải lên, sau đó đợi xử lý và chọn **Tra cứu lại**. Không gửi tài liệu riêng tư hoặc tài liệu mật.

Không kết luận tệp an toàn khi VirusTotal trả 404, báo cáo có 0 phát hiện, không kết nối được API hoặc chưa tra cứu.

## Kiểm thử trên Windows (Visual Studio)

1. `Build > Clean Solution`, sau đó `Build > Rebuild Solution`.
2. Vào **Quản trị → Quản lý VirusTotal**, cấu hình API key và bật **Tự động tra cứu SHA-256**.
3. Mở **Tổng quan → Chi tiết tệp**, xem tab **VirusTotal**; quan sát thông báo tự tra không cần bấm nút.
4. Nếu API trả 200: phải có số engine và danh sách. Nếu 404: hiển thị "Chưa có báo cáo"; không hiện số 0/0 như một kết luận an toàn. Nếu 429: hiển thị giới hạn quota, không hiện nhầm "Chưa có báo cáo".
5. Chuyển sang tệp khác rồi quay lại, xác nhận cache ngăn tra trùng; chọn **Tra cứu lại** để thực hiện một lần tra mới.
6. Tắt tự tra cứu trong Quản trị, kiểm tra không tự gửi yêu cầu API; nút tra thủ công vẫn hoạt động.
7. Thử xem tệp đã cách ly nhưng còn SHA-256 đã lưu: phải tra bằng hash mà không cần file gốc. Với tệp không còn và thiếu SHA-256: hiển thị lỗi không thể tra bằng tên tệp.
8. Bấm **Gửi tệp (cần xác nhận)**: yêu cầu xác thực quản trị, xác nhận rõ ràng, **không gửi** nếu chọn No/Hủy.

**Lưu ý:** Các bước này cần thử trực tiếp trên Windows với Internet/API key hợp lệ; gói ZIP không kèm API key và việc kiểm tra cú pháp tĩnh không thay thế Rebuild.

## File chỉnh sửa

- `Control/UcTongQuan.VirusTotalAuto.cs` — **mới**: truy vấn nền, chống trùng, cache, trạng thái, không tự upload
- `Control/UcTongQuan.ChiTiet.cs` — kích hoạt tra cứu khi mở/đổi tệp, dùng lại hash nền, nút tra thủ công chuyển thành làm mới không hiện popup
- `Control/Sketch20Backend.cs` — đồng bộ trạng thái VirusTotal với model và lệnh Tra cứu lại
- `Control/Sketch20Kit.cs` — trạng thái đang tra/404/lỗi/đã upload
- `Control/Sketch20View.cs` — banner đúng trạng thái, nút Đang tra cứu/Tra cứu lại
- `FrmMain.Sketch20.cs` — sửa luồng nút Gửi tệp để yêu cầu quản trị + xác nhận
- `ScanAndRemoveVirus.csproj` — khai báo file C# mới
