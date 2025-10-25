# HOÀN THIỆN VIRUSTOTAL API v3 — 23/10/2026

## Mục tiêu

Bổ sung tính năng thực tế cho dự án WinForms C# (.NET Framework 4.7.2) mà **không đổi cấu trúc hoặc tệp SQL**.

## Đã thực hiện

1. `Services/VirusTotalClient.cs`: API v3 `GET /files/{SHA256}`; kiểm tra SHA-256; phân biệt 200/404/400/401/403/429; giới hạn bảo thủ tối đa 4 request/phút; thông báo Retry-After khi có; timeout và lỗi kết nối dễ hiểu.
2. JSON: dùng `JavaScriptSerializer` đọc `last_analysis_stats` và `last_analysis_results` để lấy tên engine, phân loại (malicious/suspicious/undetected/harmless/...), tên mã độc, phiên bản và ngày cập nhật. Không tạo dữ liệu giả.
3. Khóa API: lưu bằng DPAPI `CurrentUser` tại `%LocalAppData%\ScanAndRemoveVirus\virustotal-api-key.dpapi`. API key không được đưa vào mã nguồn, gói ZIP, SQL hoặc Git. Biến môi trường `VIRUSTOTAL_API_KEY` vẫn được ưu tiên nếu có. Không còn đọc khóa từ `.env` trong repo.
4. Giao diện **Cài đặt → Thao tác nhanh → VirusTotal API**: nhập khóa (ẩn ký tự), Lưu, Kiểm tra, Gỡ. Chưa có key thì tab VirusTotal hiện nút **Thiết lập API key**.
5. Giao diện **Chi tiết → VirusTotal**: tra cứu hash, báo cáo theo từng hãng, thống kê số công cụ báo độc/nghi ngờ/không phát hiện, đường dẫn báo cáo. Thời điểm phân tích được lấy từ API nếu có.
6. **Gửi mẫu thủ công**: `POST /api/v3/files` cho tệp dưới 32 MB, bắt buộc người dùng xác nhận rõ tác động riêng tư. Kết quả "đã gửi/đang phân tích" không bị trình bày như đã hoàn tất.
7. **Không tự động upload mẫu** khi tra bằng hash hoặc heuristic. `QueryHashOrUpload()` duy trì tên hàm cũ cho tương thích nhưng chỉ tra cứu hash; upload toàn tệp chỉ qua nút gửi mẫu thủ công có xác nhận.

## Cách cấu hình

1. Mở `ScanAndRemoveVirus.sln` trên **Windows**, chọn **Build → Clean Solution → Rebuild Solution**.
2. Khởi chạy ứng dụng mới biên dịch (không dùng EXE Windows_Release cũ).
3. Vào **Cài đặt → Thao tác nhanh → VirusTotal API → Thiết lập**.
4. Dán khóa từ tài khoản VirusTotal vào ô được che, bấm **Lưu API key**.
5. Bấm **Kiểm tra**. Nếu báo thành công, vào **Tổng quan → quét một tệp → Chi tiết → VirusTotal → Tra cứu**.
6. Kết quả `404` nghĩa là VirusTotal **chưa có mẫu**, không phải kết luận tệp an toàn. Nếu muốn gửi mẫu thật, nhấn **Gửi tệp (cần xác nhận)** và xác nhận cảnh báo quyền riêng tư.

## Kiểm thử nên thực hiện trên Windows

| Tình huống | Kết quả mong đợi |
|---|---|
| Không có khóa | Hiện nút thiết lập, không gọi mạng |
| Nhập key sai định dạng | Báo phải có 64 ký tự hex, không lưu |
| Key không hợp lệ | Báo HTTP 401 hoặc 403 tùy phản hồi của VirusTotal |
| Hash sai | Báo SHA-256 không hợp lệ |
| Hash chưa có báo cáo | "Chưa có báo cáo", không tự upload |
| Đã có báo cáo | Hiện số liệu và danh sách engine thực |
| Gọi liên tục | Bị giới hạn cục bộ 4 request/phút; HTTP 429 có thời gian thử lại |
| Gửi mẫu | Hiện xác nhận, hủy thì không tải tệp lên |
| Tệp >32 MB | Từ chối gửi, không xin upload URL |
| Gỡ khóa | Xóa tệp DPAPI, giao diện chuyển sang chưa cấu hình |
| SQL Server ngừng hoạt động | Tra cứu VirusTotal trực tiếp không cần tạo bảng mới (tùy khả năng mở giao diện tổng thể) |

## Lưu ý về bảo mật và dịch vụ

- Bạn đã đưa khóa API vào hội thoại: nên **thu hồi/tạo khóa mới trong tài khoản VirusTotal** rồi nhập khóa mới trên Windows. Không chia sẻ API key công khai.
- Đăng báo cáo theo hash vẫn gửi chỉ số hash đến VirusTotal; hãy cân nhắc dữ liệu nhạy cảm trước khi tra cứu. Gửi tệp có thể khiến nội dung tệp được chia sẻ với các đối tác của dịch vụ; không gửi tài liệu riêng tư hoặc mật.
- Số công cụ báo `0` không đảm bảo tệp an toàn; kết quả VirusTotal **không phải quyết định tự động cách ly/xóa**.
- Tốc độ và hạn mức API phụ thuộc quyền gói tài khoản. Không được dùng public API ngoài phạm vi được VirusTotal cho phép, đặc biệt cần đối chiếu điều khoản áp dụng cho sản phẩm antivirus.
- Bản này **chưa chạy kiểm thử tích hợp trên Windows** hoặc thử khóa thật trên mạng. Kiểm tra tĩnh đã đối chiếu mã, file project, dấu ngoặc và nội dung SQL; cần Rebuild và kiểm tra thực tế trước khi sử dụng.

## Các tệp thay đổi/chèn thêm

- `Services/VirusTotalClient.cs`
- `Control/VirusTotalConfigForm.cs` (mới)
- `Control/Sketch20View.cs`, `Control/Sketch20Kit.cs`, `Control/Sketch20Backend.cs`
- `Control/UcTongQuan.cs`, `Control/UcTongQuan.ChiTiet.cs`
- `Control/UcCaiDat.cs`
- `FrmMain.Sketch20.cs`
- `ScanAndRemoveVirus.csproj`

Các tệp `.sql` và `.bak` giữ nguyên byte so với ZIP nguồn 21/10/2026; không sinh SQL migration mới.
