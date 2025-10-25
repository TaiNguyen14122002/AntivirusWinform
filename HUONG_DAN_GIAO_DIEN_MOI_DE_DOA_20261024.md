# Cập nhật giao diện Mối đe dọa cần xử lý — 24/10/2026

## Đã thực hiện

- Thiết kế lại **Mối đe dọa cần xử lý** theo bố cục mẫu: tiêu đề, nút **Cách ly tất cả**, tiêu đề danh sách với số lượng lấy từ dữ liệu quét thực tế, tìm kiếm, bộ lọc, hàng thao tác và bảng có 9 cột (chọn, loại tệp, tên, đường dẫn, loại mã độc, mức độ, thời gian, trạng thái, xem chi tiết).
- Badge loại tệp EXE/PS1/DLL/ZIP, mức độ Cao/Trung bình/Thấp, trạng thái Chưa xử lý và nút Xem chi tiết có màu phân biệt.
- Có thể tìm tên tệp/đường dẫn/loại mã độc; lọc theo mức độ/heuristic, sắp xếp theo tên/ngày/mức độ và chọn nhiều tệp.
- Điều khiển chọn trên đầu trang và checkbox ở tiêu đề bảng áp dụng cho **các mục đang hiển thị trên trang hiện tại**.
- Hỗ trợ phân trang, điều chỉnh cỡ trang tối đa 10/20/50 (số dòng hiển thị thực tế tự giảm nếu cửa sổ thấp), tự đổi sang bố cục gọn khi màn hình hẹp.
- Thêm lối tắt **Mối đe dọa** vào thanh điều hướng.
- Nút **Cách ly** dùng lại `QuarantineRows`, nút **Xóa** dùng lại `DeleteRows` với xác nhận. Không giả lập kết quả thành công.
- Nút **Cho phép** chỉ tạo ngoại lệ SHA-256 **heuristic** có thời hạn 7 ngày, yêu cầu đăng nhập quản trị và xác nhận. Không cho phép bỏ qua chữ ký hoặc mục ảo trong ZIP. Sử dụng bảng `dbo.ScanExclusions` đã có trong schema/migrations; nếu không có bảng, hệ thống báo lỗi.
- Không thay đổi SQL hay động cơ phát hiện mã độc.

## Các file thay đổi

- `Control/Sketch20View.cs`
- `Control/Sketch20View.Threats.cs` (mới)
- `Control/Sketch20Kit.cs`
- `Control/Sketch20Backend.cs`
- `Control/UcTongQuan.cs`
- `FrmMain.Sketch20.cs`
- `ScanAndRemoveVirus.csproj`

## Cách áp dụng

1. Sao lưu mã nguồn cũ. Nếu dùng ZIP patch, giải nén vào thư mục chứa `ScanAndRemoveVirus.sln`, giữ đúng đường dẫn `Control/`.
2. Mở solution trong Visual Studio trên Windows, chọn **Build → Clean Solution**, rồi **Build → Rebuild Solution**.
3. Chạy phần mềm, vào **Mối đe dọa** từ sidebar (hoặc từ danh sách phát hiện ở Tổng quan).
4. Kiểm tra ở kích thước 1600 × 900, 1280 × 720 và khi thu nhỏ cửa sổ. Dùng dữ liệu quét thực tế để kiểm tra lọc, tìm kiếm, chọn trang, mở chi tiết.
5. Chỉ thử nghiệm cách ly/xóa/cho phép với các tệp thử nghiệm an toàn được tạo riêng, không dùng tệp hệ thống. Tệp EICAR hoặc mẫu kiểm thử được phép là thích hợp cho việc đối chiếu quy trình quét; không xóa tệp thật đang cần dùng.

## Lưu ý

- Số lượng hiển thị lấy từ các mối đe dọa chưa được xử lý trong phiên/danh sách mà bộ điều khiển `UcTongQuan` đang giữ. Số **624** trong ảnh mẫu không phải số liệu cố định.
- Với màn hình thấp, trang sẽ giảm số dòng thực tế trên mỗi trang để tránh thanh cuộn ngoài, dù cấu hình "Số dòng/trang" là giới hạn tối đa.
- Môi trường đóng gói không có Visual Studio/MSBuild Windows; mã đã được kiểm tra cấu trúc và quan hệ các file nhưng cần Rebuild/kiểm thử giao diện thực tế trên Windows trước khi phát hành.
