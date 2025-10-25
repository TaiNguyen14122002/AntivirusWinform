# Khởi động cùng Windows – AntivirusWinform

**Chức năng:** Khi người dùng đăng nhập Windows 10/11 (hoặc Windows 7 với .NET Framework tương thích), chương trình được khởi động từ EXE hiện tại. Cơ chế dùng HKCU Run, không cần quyền Administrator. Đây không phải Windows Service và không chạy trước khi người dùng đăng nhập.

## Sử dụng

1. Giải nén/đưa bản chạy (EXE và các thư viện cần thiết) vào **một thư mục cố định** có quyền đọc.
2. Mở AntivirusWinform → **Cài đặt** → bật **Khởi động cùng Windows** → nhấn **Lưu thiết lập**.
3. Thoát ứng dụng, đăng xuất/đăng nhập Windows để thử.
4. Tắt: bỏ chọn công tắc, nhấn **Lưu thiết lập**.

## Kiểm tra trong Windows

Mở **Command Prompt** và nhập:

```cmd
reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v ScanAndRemoveVirus
```

- Khi **bật**, mục `ScanAndRemoveVirus` phải có dạng `"C:\duong-dan-co-dinh\ScanAndRemoveVirus.exe" --autostart` (tên EXE thực tế theo bản Build).
- Khi **tắt**, mục đó không còn. Không xóa toàn bộ khóa Run.
- Có thể kiểm tra mục Khởi động tại Task Manager → Startup apps, nhưng Registry là nguồn trạng thái mà giao diện đối chiếu.
- Nếu đổi tên/di chuyển thư mục ứng dụng, hãy mở ứng dụng ở đường dẫn mới và bật/lưu lại.

## Lưu ý kiểm thử

Trong Visual Studio, `Application.ExecutablePath` thường trỏ tới `bin\Debug\...exe`. Nếu muốn khởi động bản chính thức, hãy chạy bản đã Build/Release từ vị trí cuối cùng rồi bật chức năng.

Thiết lập `AutoStart` trong SQL Server là dữ liệu đồng bộ; việc Windows có khởi động ứng dụng hay không dựa vào Registry. Không cần bổ sung bảng SQL.

Khi Windows tự khởi chạy, tham số `--autostart` bỏ qua hộp thoại kiểm tra SQL Server đầu tiên để không phải nhấn OK. Khi mở thủ công, hộp thoại kiểm tra SQL hiện tại vẫn hoạt động.
