# Khắc phục lỗi "Task Scheduler trả mã lỗi 1" khi đặt lịch quét

## Hiện tượng

- Khi nhấn **Lưu / cập nhật lịch**, chương trình thông báo `Task Scheduler trả mã lỗi 1`.
- Bảng danh sách có thể có nhiều lịch **Quét tự động** với trạng thái **Đăng ký thất bại**.
- Các lịch thất bại được lưu ở trạng thái **Tắt**; **chưa có bằng chứng** Windows đã đăng ký tác vụ thành công.

## Lý do bản cũ không xác định được lỗi

Trong `Services/ScanScheduleService.cs`, phương thức gọi `schtasks.exe` chỉ dùng mã thoát và bỏ qua nội dung thông báo lỗi của Windows. Mã `1` là lỗi chung, có thể xảy ra do quyền tài khoản, Task Scheduler bị chặn/tắt hoặc XML đăng ký không hợp lệ. Từ ảnh không thể khẳng định nguyên nhân cụ thể.

## Thay đổi trong phiên bản này

1. Thay việc tạo XML và gọi `schtasks.exe /Create` bằng Windows **Task Scheduler 2.0 COM** (`Schedule.Service`).
2. Đăng ký bằng `TASK_LOGON_INTERACTIVE_TOKEN`, chạy dưới **tài khoản Windows hiện đăng nhập**; **không yêu cầu/ghi mật khẩu**.
3. Tạo lịch **một lần, hằng ngày, hằng tuần, hằng tháng** với các COM trigger tương ứng.
4. Gắn đường dẫn tuyệt đối đến `ScanAndRemoveVirus.exe`, tham số `--schedule-id <mã lịch>`, và **WorkingDirectory**.
5. Bật/tắt, xóa, và chạy ngay cũng sử dụng Task Scheduler COM. Có thể xóa các lịch đã lưu nhưng Windows chưa đăng ký được.
6. Hiện mã lỗi Windows dạng `0x...` và thông báo gốc khi đăng ký bị từ chối.
7. Khi lần đăng ký đầu thất bại sau khi lịch được lưu, lần lưu tiếp theo **sửa bản ghi cũ** thay vì tạo lịch lỗi trùng.
8. Đổi tiêu đề cột danh sách lịch sang tiếng Việt.
9. Không sửa cấu trúc **bất kỳ bảng/cột SQL Server nào**; không sửa tệp `.sql`/`.bak`.

## Cách kiểm thử trên Windows 10/11

1. Giải nén ZIP mới sang thư mục riêng. Mở `ScanAndRemoveVirus.sln` trong Visual Studio; **Build > Rebuild Solution** rồi chạy bản mới. Đảm bảo đang chạy đúng EXE mới.
2. Mở **Tổng quan > Đặt lịch quét tự động**. Trong danh sách, chọn từng lịch trùng tên có `Đăng ký thất bại` và nhấn **Xóa lịch**, hoặc giữ lại một lịch và nhấn **Bật / tắt lịch** để đăng ký lại.
3. Tạo lịch **Quét nhanh**, chọn **Hằng ngày** với giờ bắt đầu trong tương lai, nhấn **Lưu / cập nhật lịch**. Không được có hộp hỏi mật khẩu.
4. Nhấn **Chạy ngay**; mở **Lịch sử** để quan sát kết quả sau khi quét xong.
5. Nhấn `Win + R`, nhập `taskschd.msc`. Trong **Task Scheduler Library**, tìm tên `AntivirusWinform_Scan_<ScheduleID>`. Xác minh **Triggers**, **Actions** và mục **Run only when user is logged on**.
6. Đặt giờ kiểm tra gần nhất sau vài phút và chờ lịch chạy khi vẫn đăng nhập Windows. Máy phải đang bật, không ở chế độ ngủ.
7. Kiểm thử sửa lịch, tắt, bật và xóa.

## Nếu vẫn không tạo được lịch

- Mở `services.msc` kiểm tra dịch vụ **Task Scheduler** đang chạy.
- Kiểm tra chính sách máy tính, quyền tạo tác vụ, chương trình bảo mật chặn hoặc đăng ký sai tài khoản; tài khoản người dùng thông thường có thể bị giới hạn bởi chính sách quản trị của máy.
- Gửi nguyên văn thông báo mới có `Mã Windows: 0x...`, thay vì chỉ ảnh lỗi cũ `mã 1`.
- Khi Task Scheduler hoạt động không được, bản nâng cấp **không** giả vờ rằng lịch đã bật; bản ghi vẫn để tắt.
- Lưu ý: chế độ không mật khẩu chỉ chạy khi **tài khoản Windows đã đăng nhập**. Không cam kết chạy khi máy tắt, ngủ hoặc người dùng đã đăng xuất.

## Giới hạn kiểm thử

Phiên bản này đã được kiểm tra tĩnh mã nguồn, kiểm tra cấu trúc các khối C# và đối chiếu tệp SQL. Môi trường đóng gói không có Windows Task Scheduler/Visual Studio .NET Framework 4.7.2 để biên dịch và chạy kiểm thử tích hợp. Cần kiểm tra bước Rebuild/đăng ký thực tế trên Windows.
