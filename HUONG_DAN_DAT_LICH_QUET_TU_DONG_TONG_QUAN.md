# Thêm chức năng Đặt lịch quét tự động trên Tổng quan

## 1. Giao diện và cách sử dụng

Tại **Tổng quan** → dưới **Quét nhanh** / **Quét nâng cao** → chọn **Đặt lịch quét tự động** (biểu tượng lịch).

1. Nhập **Tên lịch** (mặc định “Quét tự động”).
2. Chọn **Chế độ**: Quét nhanh, Quét toàn bộ hệ thống, Quét tệp hoặc thư mục.
3. Nếu quét tùy chọn, chọn tệp hoặc thư mục; các chế độ khác không yêu cầu đường dẫn.
4. Đặt **ngày và giờ bắt đầu**, chọn **Hằng ngày / Hằng tuần / Hằng tháng / Một lần**.
5. Nhấn **Lưu / cập nhật lịch**. Phần mềm đăng ký tác vụ thực trong **Windows Task Scheduler**; không dùng timer của giao diện.
6. Tại cùng cửa sổ, chọn một lịch trong danh sách để **sửa**, **bật/tắt**, **chạy ngay** hoặc **xóa**. Nhấn **Tạo lịch mới** để tránh sửa đè lịch đang chọn.
7. Đóng cửa sổ quản lý; trang Tổng quan tự tải lại dòng **Lần quét tới: dd/MM/yyyy HH:mm**.

**Lưu ý:** Cửa sổ quản lý lịch giữ bước xác thực quản trị giống chức năng F07 có sẵn ở Cài đặt. Đăng ký tác vụ có thể đòi quyền Windows phù hợp.

## 2. Cách chạy nền

- Task Scheduler gọi `ScanAndRemoveVirus.exe --schedule-id <id>` vào thời gian đã đặt.
- Tác vụ dùng `InteractiveToken`: người dùng **phải còn đăng nhập vào Windows**; có thể đóng cửa sổ phần mềm, nhưng chạy sau khi đăng xuất không được đảm bảo.
- Theo thiết kế F07, lịch quét **không tự cách ly/xóa** tệp dựa trên kết quả quét.
- Khi SQL Server còn hoạt động, kết quả quét ghi vào `ScanHistory`/các bảng phát hiện như hiện có.
- Nếu SQL mất kết nối khi bắt đầu quét, chương trình cố gắng chạy engine với dữ liệu chữ ký/snapshot khả dụng và đưa bản ghi kết quả vào hàng đợi lịch sử ngoại tuyến hiện có. Không cam kết kết quả đầy đủ nếu dữ liệu chữ ký/snapshot chưa có.

## 3. Yêu cầu giữ nguyên các bảng SQL

**Không có bất kỳ thay đổi DDL nào.** 16 tệp `.sql`/`.bak` được giữ nguyên so với ZIP đã sửa lỗi Build trước đó.

- Nếu `dbo.ScanSchedules` đã tồn tại, lịch tiếp tục dùng dữ liệu SQL đó.
- Nếu chưa có bảng này hoặc SQL Server tạm không truy cập được, lịch mới được lưu **ngoài SQL** tại `%LOCALAPPDATA%\ScanAndRemoveVirus\ScheduledScans\<id>.xml`.
- Windows Task Scheduler tiếp tục quản lý thời điểm kích hoạt. Các lịch SQL và lịch cục bộ có thể được hiển thị chung.
- Không cần chạy migration hoặc tạo bảng mới để dùng **chức năng lập lịch ở Tổng quan**.

## 4. Những tệp mã nguồn được sửa

- `Control/Sketch20View.cs`: thêm nút ở Tổng quan, bố cục thích nghi kích thước cửa sổ, dòng lịch sắp tới.
- `Control/Sketch20Kit.cs`: thêm trạng thái lịch quét vào mô hình hiển thị.
- `FrmMain.Sketch20.cs`: điều hướng nút mới; tải tóm tắt lịch trên luồng nền, làm mới sau khi sửa.
- `Control/UcScreens.Sketch.cs`: dùng chung cửa sổ quản lý lịch và xác thực quản trị.
- `Control/ScanScheduleForm.cs`: Việt hóa tùy chọn, mặc định 22 giờ hôm sau, cải thiện chọn/tạo/sửa lịch.
- `Services/ScanScheduleService.cs`: tính lần chạy tiếp theo, lưu lịch cục bộ dự phòng, sử dụng Task Scheduler, xử lý lịch sử khi SQL gián đoạn.

## 5. Kiểm thử trên Windows

1. Giải nén ZIP và mở `ScanAndRemoveVirus.sln` bằng Visual Studio 2022/2026.
2. Chọn **Rebuild Solution**. Bản EXE trong `Windows_Release` là bản biên dịch cũ và **không có nút mới**.
3. Chạy chương trình, chọn Tổng quan → **Đặt lịch quét tự động**.
4. Tạo lịch **Quét nhanh → Một lần** sau 5–10 phút, nhấn Lưu.
5. Mở `taskschd.msc`, tìm tác vụ `AntivirusWinform_Scan_<id>`.
6. Đóng cửa sổ Antivirus nhưng giữ đăng nhập Windows, chờ đến giờ; xác nhận trạng thái trong Task Scheduler và lịch sử quét.
7. Thử **Chạy ngay**, **Bật/tắt**, **Sửa**, **Xóa**; kiểm tra dòng “Lần quét tới” trên Tổng quan đổi tương ứng.
8. Thử cơ sở dữ liệu không có `ScanSchedules`: lịch sẽ lưu dưới `%LOCALAPPDATA%` mà không làm thay đổi SQL.
9. Kiểm tra UI tại độ phân giải 1366×768, 1920×1080, DPI 100% và 125%; chú ý tình trạng cuộn khi cửa sổ nhỏ.

## 6. Giới hạn / tình trạng xác minh

Đã thực hiện kiểm tra tĩnh đường dẫn gọi, cây dự án, cú pháp ngoặc và so sánh SHA-256 của 16 tệp SQL/.bak. **Chưa thể chạy Visual Studio/Task Scheduler thực tế tại môi trường tạo bản này**, do đó không khẳng định đã Rebuild hoặc nghiệm thu Windows thành công. Nếu Visual Studio báo lỗi, gửi Error List và các dòng mã lỗi để sửa chính xác.
