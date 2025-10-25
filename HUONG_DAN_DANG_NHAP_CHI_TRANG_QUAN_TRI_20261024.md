# Phân quyền đăng nhập: chỉ trang Quản trị

## Mục tiêu

Chỉ khi mở mục **Quản trị** ở thanh điều hướng mới hiển thị hộp thoại đăng nhập. Người dùng thao tác tại **Tổng quan, Bảo vệ, Mối đe dọa, Cách ly, Lịch sử, Cài đặt** và **Chi tiết tệp** không phải đăng nhập tài khoản quản trị.

## Thay đổi mã nguồn

- `FrmMain.cs`: giữ điều kiện `EnsureLogin` trong điều hướng tới Quản trị (luồng WinForms cũ).
- `Control/Sketch20View.cs`: giữ xác thực khi `Go("admin")`, khi phục hồi lịch sử điều hướng đến `admin`, và kiểm tra phiên trước khi vẽ trang Quản trị; bỏ chặn đăng nhập ở công tắc bảo trì ngoài trang Quản trị. Nếu bật VirusTotal mà chưa có API key, mở hộp cấu hình trực tiếp tại trang hiện tại, không chuyển sang Quản trị.
- `FrmMain.Sketch20.cs`: bỏ kiểm tra phiên Admin tại các nút/công tắc của những trang khác. Các lệnh cấu hình VirusTotal, gửi mẫu và lưu key được xử lý tại trang hiện tại. Thao tác tải nội dung tệp lên VirusTotal vẫn cần người dùng xác nhận tường minh.
- `Control/Sketch20Backend.cs`: nút Cho phép ở Mối đe dọa không bắt đăng nhập, nhưng **chỉ cho ngoại lệ heuristic** đúng điều kiện, có xác nhận và thời hạn; không bỏ qua chữ ký mã độc khớp mẫu.
- `Control/UcScreens.Sketch.cs`: không yêu cầu phiên Admin để mở công cụ chữ ký và ngoại lệ từ Cài đặt.
- `Control/UcBaoVe.cs`: công tắc cập nhật ngoài Bảo vệ không yêu cầu đăng nhập; luồng cửa sổ tiến trình được giữ.
- `Control/UcCaiDat.cs`: cài đặt gửi mẫu, cập nhật tự động và mở công cụ nhập mẫu không yêu cầu đăng nhập; lỗi lưu SQL vẫn xử lý như trước.
- `Control/AppDialog.cs`: loại bỏ lớp xác thực Admin cũ không còn được sử dụng ngoài trang Quản trị.

## Quy tắc sau khi sửa

| Tình huống | Có hỏi đăng nhập? |
|---|---|
| Mở lần đầu trang Quản trị | Có |
| Chuyển từ Quản trị sang trang khác rồi quay lại khi session còn hiệu lực | Không hỏi lại |
| Quay lại Quản trị sau khi đăng xuất/hết hạn | Có |
| Bật/tắt bảo vệ, cập nhật, gửi mẫu từ giao diện Bảo vệ/Cài đặt | Không |
| Cách ly/xóa/cho phép ngoại lệ heuristic từ trang Mối đe dọa | Không (vẫn hỏi xác nhận khi cần) |
| Tra cứu hoặc gửi mẫu lên VirusTotal từ Chi tiết tệp | Không (tải nội dung tệp lên vẫn phải xác nhận) |
| Mở cấu hình API VirusTotal từ chức năng ở trang khác | Không |
| Đổi mật khẩu quản trị, quản trị chữ ký và các công cụ trong trang Quản trị | Phải có phiên Admin |

## Kiểm tra trên Windows

1. Mở `ScanAndRemoveVirus.sln`, chạy `Build > Clean Solution` rồi `Build > Rebuild Solution`.
2. Chưa đăng nhập: mở từng trang ngoài Quản trị và thử các chức năng, xác nhận **không hiện hộp đăng nhập Admin**.
3. Vào Quản trị: phải hiện hộp đăng nhập. Thử bấm Hủy, đảm bảo không xem được trang.
4. Đăng nhập đúng, chuyển qua các trang khác rồi quay lại: vẫn giữ session.
5. Nhấn Đăng xuất, vào lại Quản trị: phải nhập mật khẩu lại.
6. Thử tính năng có rủi ro: ngoại lệ heuristic, xóa/cách ly và **gửi toàn bộ tệp lên VirusTotal**; hộp xác nhận cần hoạt động bình thường.

## Lưu ý bảo mật

Đây là phân quyền **giao diện WinForms**, không thay thế phân quyền SQL Server/NTFS. Khi cho phép người dùng mở công cụ chữ ký/ngoại lệ ngoài trang Quản trị, quyền sửa dữ liệu thực tế sẽ phụ thuộc quyền Windows/SQL của tài khoản đang chạy ứng dụng. Nếu triển khai cho nhiều người dùng, nên cấp tài khoản SQL tối thiểu theo vai trò và xem xét thêm kiểm tra phân quyền phía dịch vụ.

**Ghi chú:** Chưa biên dịch/chạy UI WinForms trong môi trường tạo bản cập nhật. Cần Rebuild và kiểm thử trên máy Windows.
