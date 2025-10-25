# Sửa ETA và phần trăm khi quét tệp ZIP/TAR/GZ — 21/10/2026

## 1. Vấn đề

Ở bản trước, `ScanTelemetry` lấy tổng dung lượng *tệp vật lý* khi duyệt thư mục, nhưng lại cộng cả số byte đọc khi quét *các entry đã giải nén* vào cùng biến `bytesRead`. ZIP có thể có dung lượng nén nhỏ nhưng chứa hàng nghìn entry hoặc lượng dữ liệu lớn hơn nhiều. Do vậy byte đã đọc dễ đạt hoặc vượt kế hoạch tính từ dung lượng ZIP, khiến phần trăm tiến trình bị chặn tại 99% và ETA báo `~1 giây` dù quá trình quét vẫn tiếp tục.

Ngoài ra, quét ZIP/TAR/GZ tiêu tốn thời gian giải nén, ghi staging file và quét lại nội dung; không thể ước lượng từ kích thước ZIP bên ngoài với độ tin cậy tốt.

## 2. Sửa mã nguồn

- `Services/ScanEngine.cs`: `ScanTelemetry` có `physicalBytesRead` và `archiveBytesRead` riêng; tốc độ hiển thị tính từ cả hai, nhưng *tiến độ theo byte* chỉ sử dụng byte vật lý đối chiếu dung lượng vật lý.
- Đếm archive được phát hiện và số archive đã hoàn tất. Nếu còn archive chưa hoàn tất, `Percent = -1` để thanh hiển thị chuyển sang chế độ chạy động (*marquee*) và `DescribeRemaining()` trả về `Đang quét sâu tệp nén…`. Điều này cố ý không đưa ra ETA khi tổng công việc thực tế chưa xác định.
- Đường quét tệp đơn lẻ và quét thư mục/nhiều tệp đều báo hoàn tất kiểm tra sâu sau khi `ArchiveScanService.Scan` chạy xong, kể cả khi có ngoại lệ.
- Gọi `ReadArchiveBytes` cho các tệp tạm giải nén; không tính lẫn vào byte của tệp vật lý.
- Khi không còn archive, chỉ hiển thị ETA nếu đã liệt kê xong, đủ thời gian lấy mẫu, còn tương đối nhiều việc chưa xử lý. Các ETA quá gần mốc kết thúc được thay bằng `Đang ước tính…` hoặc `Đang hoàn tất…`, tránh báo `~1 giây` thiếu cơ sở.
- `Control/UcTongQuan.cs`, `Control/Sketch20Backend.cs`, `Control/Sketch20View.cs`: dùng cùng một logic trạng thái ETA, chỉ thêm tiền tố `Còn lại:` cho dự đoán thời gian thực sự (chuỗi bắt đầu bằng `~`).

**Lưu ý:** Thanh tiến trình động khi quét tệp nén không phải chỉ báo `%` xác định. Số tệp đã kiểm tra và tốc độ trung bình vẫn cập nhật. ETA có thể không hiển thị trong suốt giai đoạn quét tệp nén sâu — đó là hành vi chủ ý để tránh dự báo sai.

## 3. Giữ nguyên SQL

Không thêm, bớt hoặc sửa bảng/cột SQL Server; 16 tệp `.sql` và `.bak` được đối chiếu SHA-256 với bản ZIP nguồn và giống tuyệt đối.

## 4. Cách kiểm thử trên Windows (Visual Studio)

1. Giải nén ZIP mới vào một thư mục mới để tránh dùng nhầm `bin/obj` cũ.
2. Mở `ScanAndRemoveVirus.sln`; chọn `Build` > `Clean Solution`, sau đó `Build` > `Rebuild Solution`.
3. Quét một thư mục chỉ có các tệp thông thường: nếu số lượng/tổng byte đã biết, chương trình có thể hiển thị % và ETA ước tính (không bảo đảm độ chính xác tuyệt đối).
4. Quét thư mục có ZIP chứa hàng nghìn entry: khi còn archive chưa quét sâu, phải thấy `ĐANG QUÉT` trên thanh hoạt ảnh và thông báo `Đang quét sâu tệp nén…`, **không được hiển thị `99% • ~1 giây`**.
5. Thử quét một tệp ZIP duy nhất, nhiều ZIP, ZIP lồng, TAR, TAR.GZ và GZ.
6. Xác minh `Đã kiểm tra ... tệp` vẫn tăng khi entry được quét; sau khi kết thúc, lịch sử và cảnh báo vẫn lưu như trước.
7. Thử `Hủy quét` giữa khi giải nén: ứng dụng phải ngừng chờ và thoát trạng thái quét.
8. Mở `Tools` > `Task Scheduler` nếu kiểm thử quét tự động; phần lập lịch trước đó được giữ nguyên.

## 5. Giới hạn kiểm thử

Đã kiểm tra tĩnh mã nguồn (cân bằng ngoặc, các điểm tích hợp, đối chiếu tệp SQL). Chưa biên dịch, chạy WinForms/Windows/SQL Server trong môi trường tạo gói. Nếu Visual Studio báo lỗi mới, gửi thông báo lỗi và vị trí dòng để sửa tiếp.
