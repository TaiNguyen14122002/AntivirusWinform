# Thông báo khi lịch quét bắt đầu và hoàn tất

## Mục tiêu

Khi Windows Task Scheduler khởi chạy một lịch quét, trên **giao diện chính đang mở** của Antivirus sẽ xuất hiện thông báo ở **góc trên bên phải**:

- **Đã bắt đầu quét tự động** (xanh dương): tên lịch và chế độ quét.
- **Đã hoàn tất quét tự động** (xanh lá): tên lịch, số tệp đã quét và số phát hiện (nếu bằng 0).
- **Hoàn tất · Có mối đe dọa** (màu vàng): số tệp đã quét và số phát hiện lớn hơn 0.
- **Quét tự động gặp lỗi** (màu đỏ): lý do không thể quét hoặc phiên bị bỏ qua.

Thông báo tự ẩn sau khoảng 9 giây, có nút `×`. Nếu quét kết thúc sớm, thông báo bắt đầu hiện ít nhất 2 giây rồi chuyển sang kết quả. Giao diện Lịch sử được làm mới khi thông báo kết thúc đến trong lúc người dùng đang ở tab Lịch sử.

## Cơ chế kỹ thuật

1. Trong `Services/ScanScheduleService.cs`, tiến trình chạy với `--schedule-id` ghi sự kiện bắt đầu / kết thúc / lỗi.
2. `Services/ScheduledScanNoticeStore.cs` ghi sự kiện dạng XML vào `%LocalAppData%\ScanAndRemoveVirus\ScheduleNotifications`. Việc đổi tên từ `.tmp` sang `.xml` hạn chế đọc dữ liệu chưa ghi xong. Không phụ thuộc SQL Server.
3. `FrmMain.ScheduleNotifications.cs` sử dụng WinForms `Timer` để đọc thông báo trong khoảng 900 ms một lần, hiển thị thẻ ở góc phải trên cùng giao diện.
4. Dữ liệu sự kiện chỉ được đọc một lần và những thông báo đã cũ sẽ không làm phiền người dùng.

**Quan trọng:** Nếu giao diện WinForms đang đóng hoặc bị thu nhỏ, thông báo nằm *trong ứng dụng* sẽ không xuất hiện trên màn hình. Lịch quét vẫn hoạt động độc lập; kết quả có thể xem trong Lịch sử. Bản này chưa cài thông báo hệ thống Windows ở ngoài ứng dụng.

## Thay đổi mã nguồn

- `Program.cs`: thông báo lỗi khi thực thi `--schedule-id` gặp ngoại lệ ngoài dự kiến.
- `Services/ScanScheduleService.cs`: phát sự kiện khi bắt đầu, hoàn tất, lỗi hoặc bị bỏ qua.
- `Services/ScheduledScanNoticeStore.cs`: mới, lưu và đọc sự kiện giữa hai tiến trình.
- `FrmMain.ScheduleNotifications.cs`: mới, hiển thị thông báo góc trên bên phải giao diện.
- `FrmMain.Sketch20.cs`: khởi tạo lớp thông báo sau khi giao diện chính sẵn sàng.
- `ScanAndRemoveVirus.csproj`: khai báo hai tệp C# mới.

**SQL Server:** Giữ nguyên tất cả các tệp `.sql` và `.bak`. Không tạo, thêm, xóa, sửa bất kỳ bảng hay cột SQL nào.

## Cài đặt và thử nghiệm

1. Giải nén toàn bộ ZIP, mở `ScanAndRemoveVirus.sln` trong Visual Studio 2022/2026 trên Windows.
2. Chọn `Build -> Clean Solution`, sau đó `Build -> Rebuild Solution`.
3. Mở ứng dụng, vào `Tổng quan -> Đặt lịch quét tự động`.
4. Tạo một lịch **Quét nhanh** và chọn `Chạy ngay` hoặc lên lịch gần thời gian hiện tại; giữ cửa sổ Antivirus trên màn hình.
5. Kiểm tra thông báo bắt đầu xuất hiện góc trên bên phải; khi kết thúc, kiểm tra thông báo kết quả.
6. Kiểm tra tệp tạm dưới `%LocalAppData%\ScanAndRemoveVirus\ScheduleNotifications` được tiêu thụ sau khi UI nhận.
7. Thử lịch quét chứa tệp nghi vấn đã xác minh, kiểm tra màu vàng và con số phát hiện. Không dùng mã độc thật để thử.
8. Thử SQL tạm mất kết nối: sự kiện thông báo vẫn có thể tới giao diện nếu cơ chế quét ngoại tuyến hoạt động.

> Việc kiểm tra ở môi trường hiện tại chỉ là kiểm tra mã nguồn và cấu trúc tệp. Chưa biên dịch hoặc kiểm thử thực tế Task Scheduler/WinForms trên Windows.
