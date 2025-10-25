# Sửa lỗi `Illegal characters in path` khi quét tệp nén ZIP

## Nguyên nhân

- Khi quét ZIP, đường dẫn `D:\mau.zip > folder/file.js` chỉ là **đường dẫn ảo hiển thị**.
- Trước đây giao diện tiến trình và trang chi tiết gọi `Path.GetFileName()`/`Path.GetExtension()` trên đường dẫn ảo; một số tên tệp ZIP chứa ký tự hợp lệ trong metadata ZIP nhưng bị `System.IO.Path` của Windows từ chối.
- Trong scanner, việc dùng `Path.IsPathRooted(entry.FullName)` và `Path.GetExtension(entry.Name)` trên tên entry chưa tin cậy cũng có thể phát sinh `ArgumentException`.

## Đã sửa

1. Có helper `ScanDisplayPath` chỉ **tách tên/đuôi/cha để hiển thị**, không chạm vào ổ đĩa.
2. Khi quét, tên entry ZIP được kiểm tra path traversal, tên rooted và control characters mà không gọi `Path.IsPathRooted()` trên metadata ZIP. Các tên đặc biệt khác vẫn được đọc bằng staging GUID an toàn.
3. Tệp con được ghi vào thư mục tạm có tên GUID, **không bao giờ sử dụng trực tiếp tên entry làm đường dẫn ghi**.
4. Lỗi đọc entry (kể cả `ArgumentException`) được ghi là **chưa kiểm tra**, không để một entry làm hỏng cả phiên quét.
5. Trang Tổng quan, tiến trình, danh sách phát hiện và Chi tiết tệp dùng helper này để lấy tên hiển thị.
6. Giữ nguyên so khớp hash SHA-256/SHA-1/MD5 → CDC → heuristic, không thay đổi SQL schema.

## Kiểm thử trên Windows

1. Giải nén project vào thư mục mới.
2. Trong **Developer Command Prompt for Visual Studio**, chạy `VERIFY_ARCHIVE_SCAN_WINDOWS.cmd` để build và chạy bài kiểm thử ZIP.
3. Rebuild `ScanAndRemoveVirus.sln`.
4. Quét file vô hại `Tests\ArchivePathFixtures\invalid_windows_names.zip`. Kiểm tra rằng tiến trình không bị dừng, không mở popup lỗi .NET và số tệp con được cập nhật.
5. Thử ZIP lồng và ZIP hỏng; nếu không thể đọc phải báo rõ số entry chưa kiểm tra.

**Giới hạn xác minh:** Chưa có môi trường Windows + Visual Studio để chạy thực tế tại đây. Hãy kiểm thử trong máy ảo và gửi ảnh Error List/stack trace nếu xuất hiện lỗi tiếp theo. Không chạy các file thực thi được trích ra từ ZIP nghi ngờ.
