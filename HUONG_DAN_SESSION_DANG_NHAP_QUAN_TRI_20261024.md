# SESSION ĐĂNG NHẬP QUẢN TRỊ — Antivirus WinForms

## Mục tiêu

Đăng nhập Quản trị một lần trong cùng lần chạy ứng dụng. Khi chuyển sang Tổng quan, Bảo vệ, Cách ly, Lịch sử hoặc Cài đặt, **không tự động đăng xuất**. Quay lại Quản trị khi session còn hiệu lực sẽ không yêu cầu nhập lại mật khẩu.

## Quy tắc session

- **Thời gian không hoạt động**: 30 phút. Hoạt động chuột / bàn phím trong ứng dụng sẽ cập nhật thời điểm hoạt động cuối.
- **Thời hạn tối đa**: 8 giờ từ lần đăng nhập, ngay cả khi sử dụng liên tục.
- **Đăng xuất quản trị**: chấm dứt phiên ngay lập tức; vào lại Quản trị phải đăng nhập.
- **Đóng ứng dụng/khởi động lại**: phiên bị hủy, vào Quản trị cần đăng nhập lại.
- **Tự hết hạn khi đang mở trang Quản trị**: ứng dụng chuyển về trang Tổng quan, không để giao diện quản trị tiếp tục hiện sau khi session hết hạn.
- **Không lưu session ID, mật khẩu hay token vào tệp / CSDL**. Session được giữ **chỉ trong bộ nhớ** của tiến trình Windows.
- Mã tạo session ID ngẫu nhiên bằng `RandomNumberGenerator`; hiện chỉ quản lý trạng thái nội bộ ứng dụng, **không phải** một phiên đăng nhập máy chủ.
- Giữ nguyên PBKDF2 để kiểm tra mật khẩu, DPAPI cho thông tin xác thực thay thế và giới hạn đăng nhập sai đang có.

## Mã nguồn thay đổi

- `Control/AdminSecurity.cs`: session trong bộ nhớ, thời hạn idle/absolute, theo dõi thao tác thật bằng `IMessageFilter`, hủy phiên khi đăng xuất.
- `Control/Sketch20View.cs`: không còn `Lock()` khi chuyển trang hoặc quay lại theo lịch sử điều hướng.
- `FrmMain.cs`: không còn `Lock()` khi rời Quản trị trong giao diện legacy; thêm timer 3 giây để tự rời trang quản trị khi session hết hạn; giải phóng phiên lúc đóng cửa sổ.
- `Program.cs`: kết thúc phiên trong `finally` khi đóng ứng dụng.

Không sửa cơ sở dữ liệu SQL Server, các dịch vụ quét và UI của các trang khác.

## Cách kiểm thử trong Visual Studio (Windows)

1. Clean Solution → Rebuild Solution → Run.
2. Chọn **Quản trị**, đăng nhập đúng. Chuyển đến **Tổng quan**, **Cài đặt**, **Lịch sử**, rồi quay lại **Quản trị** ngay: **không hỏi mật khẩu**.
3. Nhấn **Đăng xuất quản trị**; mở Quản trị lại: **phải nhập mật khẩu**.
4. Đăng nhập lại; không thao tác chuột/bàn phím trong **30 phút**, thử vào Quản trị: **phải đăng nhập lại**. Nếu vẫn ở trang Quản trị sẽ tự chuyển về Tổng quan khi session hết hạn.
5. Đóng ứng dụng rồi mở lại: **phải đăng nhập lại**.
6. Nhập sai mật khẩu đủ 5 lần: giới hạn chờ 15 phút vẫn hoạt động như trước.
7. Thử cả giao diện thông thường và giao diện Sketch20 (`XVIRUS_LEGACY_UI_TEST=1` cho UI cũ).

## Điều chỉnh thời gian phiên

Trong `Control/AdminSecurity.cs`:

```csharp
private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);
private static readonly TimeSpan AbsoluteTimeout = TimeSpan.FromHours(8);
```

**Lưu ý bảo mật:** Đây là kiểm soát truy cập ở cấp ứng dụng desktop, không thay thế phân quyền SQL Server/Windows. Đừng lưu mật khẩu quản trị dưới dạng văn bản hay tạo tính năng “nhớ mật khẩu” nếu chưa có cơ chế cấp/thu hồi token an toàn từ máy chủ.
