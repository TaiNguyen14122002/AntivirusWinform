> Tài liệu của bản Tổng quan trước khi áp dụng toàn bộ phác thảo. Thông tin bản hiện tại ở `README.md` và `UI_SKETCH_IMPLEMENTATION_2026-10-04.md`.

# Màn tổng quan mới — 04/10/2026

Màn tổng quan được chuyển sang bố cục dashboard: tiêu đề và mô tả rõ ràng,
khối trạng thái nằm ngang, hai nút quét có kích thước vừa phải, ba thẻ thống kê
riêng biệt và thẻ hoạt động có trạng thái rỗng. Xanh thể hiện bảo vệ đang bật,
vàng thể hiện bảo vệ đang tắt, đỏ thể hiện đe dọa cần xử lý.

## Chạy ứng dụng

1. Mở `ScanAndRemoveVirus.sln` bằng Visual Studio, có workload .NET desktop development.
2. Chọn project `ScanAndRemoveVirus`, nhấn F5.
3. Sau khi build, chạy `bin/Debug/ScanAndRemoveVirus.exe` trên Windows có .NET Framework 4.7.2.

Cấu hình SQL Server của dự án vẫn dùng `localhost\SQLEXPRESS`, CSDL `AntivirusDB`.
Các tệp SQL và bản sao lưu trong gói nguồn được giữ lại để sử dụng theo cấu hình của bạn.

## Thay đổi

- Khôi phục tiêu đề Tổng quan bị thiếu và sửa panel tiêu đề từ Dock Fill thành Dock Top.
- Giảm khối biểu tượng ở giữa thành khối trạng thái nằm ngang, có khoảng cách đều.
- Ba thẻ số liệu có viền bo góc, nhãn và giá trị dễ đọc; ngày/giờ quét được tách riêng.
- Đưa tiến trình quét vào hàng riêng để không tràn khỏi hai nút quét.
- Giữ màu trạng thái hoàn tất, hủy và lỗi sau khi dải loading tắt.
- Hoạt động gần đây có empty state và liên kết mở Lịch sử.
- Giữ số liệu và hoạt động phiên hiện tại trong bộ nhớ khi SQL chưa lưu được lịch sử.
  Đây là thông tin của phiên đang mở, không thay thế lưu trữ SQL.
- Checkbox chọn tất cả được vẽ bằng GDI, không phụ thuộc font ký hiệu.
- Bỏ hai hộp thoại debug SQL khi khởi động, giữ kết nối và repository hiện có.
- Giải phóng các trang đã tạo sẵn khi đóng Form, bỏ đăng ký sự kiện của Tổng quan khi Dispose.
- Xử lý trường hợp thư mục Windows không tồn tại khi mở danh sách ổ đĩa trong runner Linux.

## Kiểm thử tại đây

**49/49 kiểm tra đạt, mã thoát tiến trình 0.** Toàn bộ nguồn trong project được
biên dịch bằng Roslyn/C# 7.3 với reference assemblies .NET Framework 4.7.2;
app và bộ test biên dịch không có lỗi hoặc cảnh báo. Bộ test chạy control WinForms
thật bằng Mono 6.8 và Xvfb, có ảnh chụp giao diện thật trong `DashboardVerification/`.

| Nhóm | Kết quả |
|---|---|
| Bố cục ở 1414×829, 1366×768, 1120×720, 1040×660, 1600×900 | Nút nằm trong hero, thẻ không chồng nhau, không cuộn ngang |
| Trạng thái ban đầu, chưa có lịch sử | Tiêu đề, ba thẻ và empty state hiện đúng |
| Bảo vệ bật/tắt | Màu và biểu tượng chuyển đúng |
| Quét nâng cao | Menu đủ bốn chế độ, mở từng chế độ và quay lại đúng |
| Engine với bốn tệp vô hại | Quét đủ bốn tệp, nhận hai chữ ký thử nghiệm và một heuristic, không báo nhầm tệp sạch |
| Bấm Quét ngay | Loading xuất hiện ngay, kết quả cập nhật vào thẻ và bảng |
| Phiên sạch tiếp theo | Xóa kết quả đe dọa cũ, trở lại trạng thái an toàn |
| Chọn từng dòng và chọn tất cả | Trạng thái chọn và nút xử lý cập nhật đúng |
| Mở chi tiết và quay lại | Trang chi tiết ẩn thẻ Tổng quan, giữ dữ liệu khi quay về |
| Hủy quét bằng nút trên dải loading | Dừng phiên, ẩn loading, giữ màu cảnh báo và cho phép quét tiếp |

SQL bị vô hiệu chỉ trong tiến trình `DashboardSmoke`, và dữ liệu test nằm trong
thư mục riêng. **Chưa kiểm thử kết nối/lưu trữ SQL Server, thao tác cách ly qua SQL,
VirusTotal trực tuyến hoặc chạy trực tiếp trên Windows.** Các ảnh bảo vệ bật/tắt
là trạng thái kiểm thử giao diện, không phải trạng thái của máy tính người dùng.

Runner có xử lý riêng lỗi finalizer DoubleBuffer rỗng của Mono 6.8 khi đóng control ẩn;
chỉ buffer không có native drawable được bỏ finalizer, buffer đã render vẫn Dispose bình thường.
Xử lý này chỉ nằm trong test runner, không nằm trong ứng dụng.

## Chạy lại test

Windows: mở Developer Command Prompt for Visual Studio, chạy `VERIFY_DASHBOARD.cmd`.

Linux có Mono, .NET SDK, libgdiplus và Xvfb:

```bash
python3 Tests/verify_dashboard_linux.py
```

Kết quả, ảnh và log sẽ nằm trong `DashboardVerification/`.
Các bộ test cũ phụ thuộc Windows/SQL trong dự án chưa được chạy trong phiên này.
