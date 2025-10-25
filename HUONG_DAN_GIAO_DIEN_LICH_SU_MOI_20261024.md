# Nâng cấp giao diện Lịch sử – 24/10/2026

## Mục tiêu

Thiết kế lại phần Lịch sử theo ảnh tham chiếu, sử dụng dữ liệu thật của `UcLichSu`/`ScanHistoryStore`.

- Khung đầu: tiêu đề **Lịch sử**, mô tả, bộ lọc **Từ ngày**, **Đến ngày**, **Loại quét**, cùng nút **Lọc**, **Làm mới**, **Xuất CSV**.
- Khung dưới: **Nhật ký hoạt động**, tổng số bản ghi, sắp xếp mới nhất/cũ nhất; bảng hiển thị ngày/giờ, loại quét và phạm vi, số tệp/mối đe dọa, thời gian quét, kết quả, và thao tác **Xóa/Xem**.
- Chân bảng: tổng số bản ghi, chọn **10/20/50** bản ghi một trang; phân trang có nút số (`1 2 3 … cuối`) và trước/sau.
- Bảng dùng vùng cuộn dọc riêng khi số dòng nhiều hơn chiều cao cửa sổ; thanh phân trang và tiêu đề cột được giữ cố định.
- Khi không có dữ liệu: vẫn hiển thị khung và thông báo rỗng.
- Font Segoe UI, màu xanh–trắng, nút hành động góc vuông. Hỗ trợ bố cục gọn hơn khi cửa sổ hẹp.

## Tương thích

Không sửa cấu trúc SQL, không tạo dữ liệu mẫu. Việc lọc, sắp xếp, xem chi tiết, xóa, xuất CSV vẫn gọi đúng những hàm điều khiển sẵn có ở `UcLichSu`.

## Những file có thay đổi

1. `Control/Sketch20View.cs`: chuyển trang Lịch sử sang renderer mới, cuộn chuột trong bảng.
2. `Control/Sketch20View.History.cs`: giao diện mới và dòng bảng tương tác.
3. `Control/UcLichSu.cs`: PageSize thay đổi được (10, 20, 50).
4. `Control/Sketch20Backend.cs`: cung cấp page size, xử lý lệnh nhảy trang/đổi số dòng.
5. `Control/Sketch20Kit.cs`: đưa page size vào model và dấu phiên bản dữ liệu.
6. `FrmMain.Sketch20.cs`: chuyển tiếp lệnh phân trang đến `UcLichSu`.
7. `ScanAndRemoveVirus.csproj`: đăng ký file C# mới.

## Kiểm thử trong Visual Studio trên Windows

1. Mở `ScanAndRemoveVirus.sln`, chọn **Build > Clean Solution** rồi **Rebuild Solution**.
2. Vào trang **Lịch sử**; kiểm tra bố cục khi phóng to và thu nhỏ cửa sổ.
3. Lọc theo ngày và loại quét, thử ngày bắt đầu sau ngày kết thúc.
4. Chọn 10, 20, 50 dòng, kiểm tra các trang đầu/giữa/cuối, cuộn bên trong bảng.
5. Dùng **Làm mới**, **Xuất CSV**, **Xem** và **Xóa**. Xác nhận hộp thoại xóa trước khi thực hiện.
6. Thử bộ lọc không có bản ghi, kiểm tra thông báo rỗng và phân trang.

**Giới hạn kiểm chứng:** Thay đổi đã được kiểm tra cấu trúc mã nguồn và file nén trong môi trường hiện tại; chưa có trình biên dịch .NET Framework WinForms/Windows để chạy giao diện và kiểm chứng trực quan.
