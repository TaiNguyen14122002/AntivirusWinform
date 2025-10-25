# Quét toàn bộ tệp, báo riêng từng tệp

## Phạm vi quét
- **Quét nhanh:** kiểm tra toàn bộ tệp (kể cả thư mục con) trong những khu vực quan trọng: Desktop, Downloads, Documents và Temp hiện có.
- **Quét toàn bộ hệ thống:** duyệt tất cả tệp trong các ổ đĩa đã sẵn sàng, bỏ qua vòng lặp liên kết thư mục (junction/symbolic link).
- **Quét thư mục:** luôn duyệt tất cả cấp thư mục con. Đã bỏ khả năng tắt quét thư mục con để không vô tình bỏ sót.
- **Quét tệp:** quét riêng từng tệp được chọn, không biến thư mục thành một tệp.

## Kết quả
- Mỗi phát hiện là **một đường dẫn tệp riêng**, không phải cả thư mục gốc.
- Phần thông báo cuối phiên đưa ví dụ 5 tệp đầu; **xem toàn bộ** kết quả trên trang Mối đe dọa cần xử lý và mở Chi tiết tệp.
- Nếu chọn các phạm vi chồng nhau, mỗi đường dẫn tệp được quét một lần.
- Có thống kê thư mục đã duyệt, thư mục liên kết/không truy cập được và lỗi liệt kê riêng với lỗi đọc tệp.
- Các tệp thực sự không khớp chữ ký vẫn được tính là đã quét; tệp không truy cập được **không được xác nhận là an toàn**.
- ZIP/RAR/7Z được xem là **một tệp** khi quét thông thường; bản này **chưa giải nén để quét từng tệp nằm bên trong archive**. Không nên diễn giải kết quả quét ZIP là đã quét toàn bộ nội dung nén.
- Không có phần mềm antivirus nào bảo đảm phát hiện được mọi mã độc chưa có chữ ký hoặc không có dấu hiệu heuristic; kết quả quét là bằng chứng theo các phương pháp hiện có.

## Kiểm thử bằng tệp vô hại
1. Mở **Developer Command Prompt for Visual Studio** có MSBuild và .NET Framework 4.7.2 targeting pack.
2. Chạy `VERIFY_SCAN_ALL_FILES.cmd`.
3. Bộ kiểm thử tạo 5 tệp vô hại trong các thư mục cấp 0–2, đưa 3 SHA-256 của chúng vào snapshot bộ nhớ, rồi xác minh tất cả 5 tệp được quét và chỉ 3 tệp được phát hiện bằng đúng đường dẫn. Không sử dụng hoặc thay đổi SQL Server thật.
4. Bộ kiểm thử tiếp tục thử phạm vi cha/con chồng nhau và chế độ quét một tệp.

**Lưu ý:** Cần chạy trên Windows bằng Visual Studio để xác nhận biên dịch và hành vi thực tế.
