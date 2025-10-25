# Thông báo bảo vệ và thao tác — Antivirus WinForms

## Phạm vi

Bản cập nhật bổ sung `ProtectionNotificationService` (phát sự kiện không phụ thuộc UI) và `FrmMain.ProtectionNotifications` (hiển thị WinForms và balloon khi cửa sổ thu nhỏ). Không sửa cấu trúc SQL Server.

| Khi nào | Nội dung thông báo |
|---|---|
| Mở ứng dụng | Trạng thái số mô-đun đã khởi động / 6; không tuyên bố bảo vệ tuyệt đối |
| Bật/tắt một công tắc tại Bảo vệ | Bật thành công, đã tắt, hoặc yêu cầu bật nhưng watcher chưa chạy |
| Watcher/WMI phát sinh lỗi | Cần kiểm tra bộ bảo vệ, chống lặp cảnh báo |
| Phát hiện tệp bởi GuardService / RealTimeProtection | Cảnh báo tên tệp; heuristic không tự cách ly |
| Tự động cách ly | Chỉ báo đã cách ly khi engine trả về thành công |
| Phát hiện USB | Đang quét và kết quả tổng số mục, mối đe dọa, tệp không quét được |
| Tự cập nhật chữ ký MalwareBazaar | Bổ sung chữ ký thành công hoặc lỗi kết nối/API |
| Quét nhanh / tùy chọn | Bắt đầu, hoàn tất, hủy hoặc lỗi; không che giấu vùng quét chưa đầy đủ |
| Cách ly, khôi phục, xóa | Số mục xử lý thành công và không thành công, sau khi thao tác thực sự hoàn tất |
| Tra cứu VirusTotal thủ công | Xác nhận đã yêu cầu tra cứu, không khẳng định đã có báo cáo |

## Quy tắc thông báo

- Khi ứng dụng mở: thông báo góc phải, tự ẩn sau 6–9 giây.
- Khi thu nhỏ cửa sổ: thông báo qua khu vực thông báo Windows (NotifyIcon).
- Khi có rất nhiều thay đổi tệp: chống lặp và giới hạn hàng đợi; **không thông báo cho từng tệp sạch**.
- Công tắc **Hiển thị thông báo** tắt: không hiển thị cảnh báo bảo vệ nền; thao tác người dùng trực tiếp vẫn có xác nhận kết quả.
- Hộp thoại **xác nhận trước khi xóa, khôi phục tệp nghi vấn hoặc cách ly ZIP** vẫn giữ. Lỗi xử lý một phần có hộp thoại nêu kết quả để dễ khắc phục.
- Nút thông báo chỉ giúp người dùng biết trạng thái, không bảo đảm đã chặn thực thi mã độc ở cấp hệ điều hành. Giám sát bằng FileSystemWatcher không tương đương driver antivirus.

## File thay đổi

- Mới: `Services/ProtectionNotificationService.cs`
- Mới: `FrmMain.ProtectionNotifications.cs`
- Sửa: `FrmMain.cs`, `FrmMain.Sketch20.cs`, `Control/UcBaoVe.cs`, `Control/UcTongQuan.cs`, `Control/UcCachLy.cs`, `Services/GuardService.cs`, `Services/RealTimeProtection.cs`, `ScanAndRemoveVirus.csproj`.

## Kiểm thử trên Windows

1. Rebuild Solution; vào Bảo vệ, tắt/bật USB hoặc Bảo vệ tệp: thấy kết quả tương ứng.
2. Bật **Hiển thị thông báo**, thử tạo mẫu kiểm thử an toàn EICAR tại thư mục đang giám sát (nếu môi trường test cho phép): thấy cảnh báo và kiểm tra lại bản ghi lịch sử/cách ly thực tế.
3. Quét nhanh rồi hủy: thấy thông báo đã bắt đầu/đã hủy, không báo hoàn tất thành công.
4. Quét một thư mục thử nghiệm: thông báo hoàn tất phải khớp với tổng số mục/đe dọa và cảnh báo nếu quét chưa đầy đủ.
5. Khôi phục / xóa tệp thử nghiệm trong cách ly: xác nhận trước thao tác; kết quả chỉ báo thành công khi engine trả về thành công.
6. Thu nhỏ ứng dụng, thao tác có tạo thông báo: kiểm tra balloon tại khay hệ thống Windows.
7. Tắt **Hiển thị thông báo** rồi thực hiện một sự kiện bảo vệ nền: không có thông báo gây phiền.

**Chưa được kiểm thử Windows thực tế trong môi trường tạo mã này**. Task Scheduler, sự kiện USB/WMI, quyền truy cập và chính sách Windows có thể ảnh hưởng hành vi.
