# Kiểm thử giao diện Windows

Chạy `VERIFY_UI_WINDOWS.cmd` trên Windows. Script tìm MSBuild của Visual Studio, build Release và chạy bộ chẩn đoán trên desktop đang đăng nhập.

Bộ kiểm tra dùng `ScanAndRemoveVirus.FrmMain` của bản EXE, ghi đường dẫn/SHA-256 assembly, font/DPI và chụp bằng `Graphics.CopyFromScreen` trước mọi DrawToBitmap đọc metadata. Từ chối ảnh khi ứng dụng không ở foreground.

- 20 màn × 3 client size: 1040×720, 1240×800, 1600×900.
- 20 màn ở client size 1240×chiều-cao-phác-thảo-gốc.
- Năm trạng thái VirusTotal: thiếu API key, chưa tra cứu, không có cảnh báo, nhiều công cụ, chưa chọn tệp.
- Kiểm tra pixel chữ trên Graphics đã dịch tọa độ.
- Đo chiều cao chữ, giao nhau giữa text/control, input/placeholder native, vùng root, scrollbar ngang và pixel góc thẻ chọn quét.

Output `WindowsEvidence` chứa 86 ảnh desktop, 80 ảnh so sánh, runtime, TSV vấn đề và TSV thông số control. Hai ảnh `text-transform-*` kiểm tra renderer trên bitmap; không gọi chúng là ảnh desktop.

Lượt Windows 11 Home build 26200, 96 DPI, bản 1.0.4 nằm ở `UI_Preview/Evidence_Windows_VirusTotal_1.0.4`, đạt 85/85 giao diện và 151/151 chức năng. Model mẫu chỉ dùng đối chiếu phác thảo; startup dùng model thực trong profile tạm. Mono/Wine bị từ chối. Không đổi DPI hệ thống; kết quả 100% không suy diễn sang 125%/150%.
