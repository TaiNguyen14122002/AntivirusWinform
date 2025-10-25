# TRANG QUẢN TRỊ RIÊNG — ANTIVIRUS WINFORMS

Ngày cập nhật: 23/10/2026 · C# WinForms / .NET Framework 4.7.2 · SQL Server **không thay đổi cấu trúc bảng**.

## I. Cách sử dụng

1. Mở `ScanAndRemoveVirus.sln` trong Visual Studio trên **Windows**.
2. Chọn **Build → Clean Solution → Rebuild Solution** rồi chạy ứng dụng.
3. Từ sidebar nhấn **Quản trị**: hộp thoại đăng nhập hiện ra.
4. Dùng tên đăng nhập `TaiNguyen` và mật khẩu do chủ dự án đã chỉ định trong yêu cầu (không in lại trong tài liệu này).
5. Trên **Tổng quan quản trị** có nút **Đổi mật khẩu Admin**: nên đổi ngay sau lần truy cập đầu tiên.
6. Mỗi khi rời trang Quản trị, phiên Admin được khóa. Nếu ở trong trang quá 20 phút thì xác thực lại. Sai 5 lần liên tục khóa đăng nhập 15 phút trong tiến trình đang chạy.

## II. 15 nhóm trong trang Quản trị (14 nhóm đã thống nhất + VirusTotal)

| STT | Module | Hoạt động đã nối trong mã nguồn | Giới hạn/điều kiện |
|---|---|---|---|
| 1 | Tổng quan quản trị | Số chữ ký, trạng thái SQL/VT, cập nhật và realtime | Không hiển thị lịch sử người dùng |
| 2 | Quản lý chữ ký | Xem/tìm kiếm, thêm SHA256 mới ở `IsActive=0`, sửa tên/mức độ, kích hoạt/vô hiệu hóa; ghi lý do | Công cụ F02 cũ có thể cần migration đã tồn tại; thao tác cơ bản hoạt động trên bảng gốc |
| 3 | Kiểm duyệt chữ ký | Xem chữ ký chưa bật, chọn phê duyệt/tạm ngưng, nhật ký DPAPI | Không thể chứng minh mẫu là mã độc chỉ từ một hash; cần nguồn kiểm chứng |
| 4 | Nguồn dữ liệu | Nguồn MalwareBazaar/VT, mở công cụ nhập mẫu, bật/tắt `SubmitSamples` | Bật cờ không tự gửi tệp lên Internet |
| 5 | Cấu hình bộ máy | Xem SHA256/CDC/sliding/heuristic, bật/tắt realtime, USB, download guard | YARA chưa tích hợp runtime; không hiển thị như đã chạy |
| 6 | Quy tắc phát hiện | Hiển thị chính sách đã có, mở công cụ ngoại lệ heuristic | Không cung cấp UI thay đổi ngưỡng engine khi backend chưa hỗ trợ |
| 7 | Chất lượng dữ liệu | Truy vấn chữ ký thiếu/không hợp lệ/trùng SHA256 để kiểm tra | Không tự xóa dữ liệu nghi vấn |
| 8 | Giám sát dịch vụ | Theo dõi flag guard và SQL, gọi áp dụng lại cấu hình watcher, kiểm tra Windows | Flag “bật” không thay cho xác thực trạng thái watcher OS thực tế |
| 9 | Hiệu năng | Bộ nhớ, RAM private, luồng, thời gian CPU, thời điểm khởi động | Chưa biểu diễn %CPU realtime hoặc lịch sử dài hạn |
| 10 | Kiểm thử antivirus | SHA256 known-answer test trên `abc` vô hại, công cụ kiểm tra PE | Chưa có tập kiểm thử nhãn để tính TPR/FPR |
| 11 | Quản lý cập nhật | Bật/tắt auto-update, mở công cụ nhập MalwareBazaar Full CSV | Quá trình cập nhật cần URL/Auth-Key và kết nối hợp lệ |
| 12 | Nhật ký quản trị | Lưu/đọc sự kiện đăng nhập, thay đổi chữ ký và chính sách | DPAPI theo Windows user, không phải lưu trữ kiểm toán chống sửa tuyệt đối |
| 13 | Sao lưu và phục hồi | SQL `BACKUP ... COPY_ONLY, CHECKSUM`, `RESTORE VERIFYONLY` | Thao tác restore ghi đè được giữ ở SSMS theo quy trình an toàn, KHÔNG tự restore đang chạy |
| 14 | Chẩn đoán | Kết nối SQL, thư mục dữ liệu, VT key, log và trạng thái Windows | Cần kiểm tra `taskschd.msc` cho chi tiết task thực tế |
| 15 | Quản lý VirusTotal | Đặt/gỡ/kiểm tra API key trong DPAPI; bật/tắt tự tra hash; quản lý cờ gửi mẫu | Người dùng vẫn được tra hash theo chức năng quét, không được đổi API key |

**Hai công tắc chuyển từ Cài đặt:** `AutoUpdate` và `SubmitSamples` được chuyển sang module 11 và module 4/15. Ngoài ra nút cấu hình VirusTotal được bỏ khỏi các công cụ nhanh ở Cài đặt và thay bằng điều hướng tới trang Admin có xác thực.

## III. Lưu trữ và bảo mật

- SQL: **giữ nguyên 16 tệp `.sql`/`.bak`**, không tạo/sửa/drop bảng hoặc cột.
- Mật khẩu khởi tạo: trong chương trình chỉ chứa bộ xác minh PBKDF2-HMACSHA1 180.000 vòng với salt riêng; **không nhúng mật khẩu dạng chữ**. Khi đổi mật khẩu, bộ xác minh mới được mã hóa bằng Windows DPAPI tại `%LocalAppData%\ScanAndRemoveVirus\admin-auth.dpapi`.
- Nhật ký quản trị: `%LocalAppData%\ScanAndRemoveVirus\admin-audit.dpapi.log` (từng dòng mã hóa DPAPI, chỉ Windows user hiện tại có thể đọc).
- Khóa API VirusTotal: tiếp tục lưu DPAPI như phiên bản trước. Không đưa API key vào mã nguồn, ZIP, SQL hoặc tài liệu.
- Đường vào trang Quản trị được bảo vệ ở sidebar `Sketch20View`, điều hướng quay lại, và sidebar `FrmMain` cũ. Các thao tác UI quản trị xác minh phiên đăng nhập.
- **Giới hạn quan trọng:** Cơ chế đăng nhập WinForms không thể thay thế phân quyền SQL, NTFS và quản lý quyền Windows. Ai đã có quyền trực tiếp đối với database hoặc sửa file EXE có thể vượt qua giới hạn ở lớp giao diện. Khi triển khai nghiêm túc, tài khoản người dùng thường phải được cấp quyền SQL tối thiểu, quyền ghi quản trị chỉ cấp cho dịch vụ được bảo vệ.
- Giới hạn đăng nhập sai hiện chỉ tồn tại trong bộ nhớ tiến trình. Để chống tấn công đa máy/khởi động lại cần kho xác thực tập trung và audit bất biến.

## IV. Kiểm thử đề nghị trên Windows

1. Nhấn Quản trị → Cancel: **không được mở** trang Admin.
2. Sai mật khẩu: báo lỗi, không hiển thị dữ liệu Admin. Đăng nhập hợp lệ: truy cập được module.
3. Rời Quản trị, quay lại: **bắt buộc đăng nhập lại**.
4. Cài đặt chung: chỉ còn **Khởi động cùng Windows**, **Hiển thị thông báo**; không có AutoUpdate, SubmitSamples, API key.
5. Admin → Nguồn dữ liệu: bật/tắt SendSamples, kiểm tra giá trị `dbo.Settings.SubmitSamples` cập nhật; không tự upload.
6. Admin → Cập nhật: bật/tắt AutoUpdate; kiểm tra `dbo.Settings.AutoUpdate`.
7. Admin → Chữ ký: thêm một SHA256 thử nghiệm được xác minh, trạng thái **IsActive=0**; kiểm tra trùng không được thêm lần hai; thử bật/tắt trên mẫu kiểm thử.
8. Admin → VirusTotal: kiểm tra key trong tài khoản Windows đang sử dụng; **không chia sẻ key** qua ZIP.
9. Admin → Backup: chọn **đường dẫn có thể truy cập từ SQL Server SERVICE** (không mặc định là máy Client), thử tạo `.bak` và VERIFYONLY; thử trên bản SQL không chứa dữ liệu quan trọng.
10. Đảm bảo có thể quét nhanh, quét sâu ZIP và lên lịch như bản trước; kiểm tra trang quản trị không can thiệp phiên quét cá nhân.

**Tình trạng kiểm thử:** đã kiểm tra tĩnh cú pháp cấu trúc nguồn, khai báo csproj, và hash 16 tệp SQL/.bak. Chưa thể chạy MSBuild .NET Framework 4.7.2 hoặc thử kết nối SQL/Task Scheduler trên Windows trong môi trường tạo gói này. Các module phụ thuộc Windows/SQL cần Rebuild và xác nhận tại máy của bạn.
