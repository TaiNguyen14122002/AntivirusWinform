# Tối ưu hiệu năng Antivirus WinForms – 25/10/2026

## 1. Vấn đề đã xác định trong mã nguồn

- `Sketch20View` trước đây gọi `RefreshModel()` mỗi **350 ms**. Khi trạng thái quét thay đổi, `Render()` hủy và tạo lại nhiều control WinForms của trang; thao tác này có thể gây giật khi đồng thời quét tệp.
- Mỗi lần render còn gán lại tọa độ sidebar, tooltip và yêu cầu vẽ lại mọi mục điều hướng, kể cả khi trang và độ rộng không thay đổi.
- `UpdateNativeStatusStrip()` gán lại văn bản ở mọi lần refresh ngay cả khi thông tin giữ nguyên.
- Hàng đợi **Bảo vệ tệp** dùng `SemaphoreSlim.Wait()` trong `ThreadPool.QueueUserWorkItem`, khiến nhiều luồng bị giữ trong trạng thái chờ khi nhiều tệp thay đổi đồng thời.
- Thanh quét có vòng hoạt ảnh riêng 33 ms (khoảng 30 lần/giây); trên máy ảo ít tài nguyên có thể tốn thêm CPU.

## 2. Những tối ưu đã áp dụng

| Hạng mục | Cũ | Mới |
|---|---|---|
| Vòng cập nhật model | 350 ms | 750 ms khi quét trang Tổng quan, 1200 ms ở các trang khác |
| Ngưỡng dựng lại trang khi đang quét | 330 ms | 650 ms |
| Tìm kiếm có debounce | 130 ms | 230 ms |
| Vẽ lại sidebar | Mỗi lượt dựng trang | Chỉ khi điều hướng/trạng thái sidebar thay đổi |
| Thay đổi kích thước/tooltip sidebar | Mỗi lượt dựng trang | Chỉ khi chuyển chế độ sidebar mở rộng/thu gọn |
| StatusStrip | Gán lại văn bản mỗi lượt | Chỉ cập nhật khi nội dung thực sự đổi |
| Bảo vệ tệp | `SemaphoreSlim.Wait()` chiếm luồng đang chờ | `WaitAsync()` không giữ luồng đang chờ |
| Hoạt ảnh thanh quét | 33 ms | 50 ms (20 fps), giữ chuyển động mượt |
| Khi cửa sổ thu nhỏ | Vẫn đọc và dựng model | Bỏ qua việc refresh trang, **bảo vệ nền vẫn hoạt động** |

**Phạm vi thay đổi:** `Control/Sketch20View.cs`, `FrmMain.Sketch20.cs`, `Services/GuardService.cs`. Không chỉnh cấu trúc SQL Server, chữ ký, phương pháp phát hiện hoặc logic cách ly.

## 3. Cập nhật dự án

1. Sao lưu dự án cũ.
2. Với gói file sửa: chép đúng 3 file vào đúng đường dẫn tương ứng trong dự án.
3. Mở `ScanAndRemoveVirus.sln` trên máy Windows.
4. Chọn **Build > Clean Solution**, sau đó **Build > Rebuild Solution**.
5. Chạy lại ứng dụng, vào Tổng quan, Bảo vệ, Lịch sử, Cách ly và Chi tiết tệp; thử chuyển tab nhiều lần.

## 4. Đo tài nguyên bằng PowerShell

Mở **PowerShell** khi ứng dụng đang chạy:

```powershell
$processName = 'ScanAndRemoveVirus'
$p1 = Get-Process -Name $processName -ErrorAction Stop
$cpuBefore = $p1.CPU
$ramBefore = [math]::Round($p1.WorkingSet64 / 1MB, 1)
Start-Sleep -Seconds 15
$p2 = Get-Process -Id $p1.Id -ErrorAction Stop
$cpuSeconds = [math]::Round($p2.CPU - $cpuBefore, 2)
$cpuCoresAverage = [math]::Round($cpuSeconds / 15, 2)
$ramAfter = [math]::Round($p2.WorkingSet64 / 1MB, 1)
[pscustomobject]@{
    'CPU giây / 15 giây' = $cpuSeconds
    'CPU nhân trung bình' = $cpuCoresAverage
    'RAM ban đầu (MB)' = $ramBefore
    'RAM sau 15 giây (MB)' = $ramAfter
} | Format-List
```

`CPU nhân trung bình` tính theo số giây CPU / 15 giây thực, tức `1.0` xấp xỉ dùng hết một nhân; đây không phải phần trăm CPU toàn máy.

Lặp lại ở các trường hợp: (1) để yên 30 giây, (2) chuyển trang liên tục, (3) chạy Quét nhanh, (4) bật đồng thời sáu chức năng Bảo vệ, (5) sao chép nhiều tệp thử (an toàn) trong Documents/Downloads. So sánh kết quả trước và sau cập nhật trên **cùng máy và cùng dữ liệu**.

## 5. Khi vẫn còn giật lag

- Mở Task Manager > **Processes/Details** kiểm tra CPU, Memory, Disk của `ScanAndRemoveVirus.exe`, `sqlservr.exe`, `MsMpEng.exe` và hệ điều hành.
- Nếu thấy Disk liên tục 100% khi quét, nguyên nhân có thể là I/O, SQL Server, hoặc nhiều chương trình cùng đọc cùng tệp. Không nên tắt Windows Defender để đổi lấy tốc độ.
- Nếu CPU tăng vọt khi di chuyển giữa các trang ngay cả khi **không quét**, chụp lại Performance/CPU bằng Visual Studio Profiler và ghi rõ tên trang cùng số tệp đang hiển thị.
- Nếu dùng Windows trong máy ảo, xem mức RAM/CPU cấp cho máy ảo và dung lượng còn trống; đừng cấp RAM quá nhiều khiến máy chủ thiếu bộ nhớ.
- Khi quét thư mục khổng lồ, tránh chạy nhiều phiên quét chủ động cùng lúc; chức năng bảo vệ nền vẫn hoạt động.

## 6. Kiểm thử hồi quy bắt buộc

- Bật/tắt cả 6 công tắc Bảo vệ; trạng thái thực tế phải đúng.
- Test tạo tệp nghi vấn **vô hại** theo tài liệu kiểm thử PowerShell; xác nhận có cảnh báo trên góc **trên bên phải**.
- Quét nhanh và quét tùy chọn: thanh tiến trình cập nhật và có thể hủy quét.
- VirusTotal: tìm kiếm, xem bảng nhiều công cụ, chuyển tab.
- Lịch sử và Cách ly: mở chi tiết và xác nhận không mất nút cuối trang.
- Thu nhỏ/phóng to, thay đổi kích thước cửa sổ: không còn vẽ sidebar vô ích; không ảnh hưởng bộ bảo vệ nền.

> Chưa thể Rebuild hoặc đo hiệu năng WinForms trên Windows trong môi trường đóng gói. Kết quả phụ thuộc CPU, RAM, ổ đĩa, SQL Server và dữ liệu kiểm thử thực tế; không có cam kết mức FPS/CPU cụ thể nếu chưa đo trên máy chạy ứng dụng.
