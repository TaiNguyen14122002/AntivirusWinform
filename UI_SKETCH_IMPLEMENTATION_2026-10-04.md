# Triển khai đủ 20 màn

`Control/Sketch20View.cs` dựng toàn bộ 20 bố cục. `Sketch20Kit.cs` chứa palette, font pixel, icon, nút, công tắc và DTO. `Sketch20Inputs.cs` chứa chọn chế độ, chọn ngày/loại quét, checkbox và ô API key che ký tự.

`Control/Sketch20Backend.cs` là các partial adapter đọc trạng thái controller hiện có và gọi các event/service thực. `FrmMain.Sketch20.cs` nối điều hướng, quét, cách ly, lịch sử, cài đặt và chi tiết. Renderer cập nhật từ trạng thái thực; controller cũ vẫn sống trên UI thread để giữ các chức năng của dự án. Các API điều hướng công khai và chế độ chụp được nối với giao diện mới.

`Sketch20Samples.cs` cung cấp dữ liệu riêng cho `--phac-thao`, sử dụng cùng renderer với ứng dụng. Chế độ mẫu không tạo controller, gọi SQL/API hay thao tác tệp. Số liệu minh họa không đi vào phiên chạy bình thường.

`Tests/Sketch20Smoke.cs` chụp và kiểm tra cả 20 màn; `Tests/Sketch20BridgeSmoke.cs` kiểm tra giao diện mặc định với scan engine/controller thật trong môi trường cô lập. `Tests/DashboardSmoke.cs` và partial Sketch giữ kiểm tra hợp đồng controller hiện có. `VERIFY_DASHBOARD.cmd` và runner Python chạy cả hai bộ kiểm tra.

Kết quả cuối: build không lỗi/cảnh báo; 122/122 và 149/149 kiểm tra đạt, tổng 271/271 trên Mono/Xvfb. Log và ảnh cuối nằm trong `UI_Preview`. SQL bị vô hiệu trong test. Chưa xác minh Windows, SQL Server hoạt động và VirusTotal online.

Runner Linux tạo bản sao framework Mono riêng với kiểm tra null khi giải phóng drawable. Thay đổi tương thích này chỉ phục vụ test; không được đưa vào ứng dụng Windows hoặc gói bàn giao.
