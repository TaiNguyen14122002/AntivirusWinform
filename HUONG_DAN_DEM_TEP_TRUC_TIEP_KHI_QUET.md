# Cập nhật bộ đếm tệp ngay trong lúc quét

## Hành vi sau khi sửa

- `ScanTelemetry.FilesCompleted`: số tệp vật lý đã **xử lý** xong, dùng để tính ETA và tiến độ của hàng đợi.
- `ScanTelemetry.ArchiveEntriesCompleted`: số tệp con bên trong ZIP đã **kiểm tra thành công**, được tăng ngay sau mỗi entry (bao gồm ZIP lồng nhau).
- `ScanTelemetry.CheckedObjects`: số tệp vật lý kiểm tra thành công + số entry đã kiểm tra thành công. Đây là số hiển thị ở `Đã kiểm tra ... tệp` trong lúc quét.
- Các entry bị lỗi/mã hóa/vượt giới hạn không được tính vào `CheckedObjects`; báo riêng ở báo cáo chưa kiểm tra. Tệp ZIP bên ngoài được tính là một tệp vật lý nếu bản thân nó đọc/đối chiếu được.
- Giao diện tiến trình làm mới 350 ms/lần, không chờ ZIP hoàn tất hoặc chờ callback theo lô 100 file. Dòng tệp đang kiểm tra được cập nhật theo entry hiện tại dưới dạng `archive.zip > folder/file.ext`.
- Sau quét, số liệu `ScanResult.FilesScanned` (tệp vật lý đã thử xử lý) và `ScanResult.ArchiveEntriesScanned` vẫn lưu **tách riêng**, không cộng dồn vào cột SQL chỉ lưu file vật lý.

## Kiểm tra trên Windows

1. Giải nén dự án mới, Build -> Clean Solution rồi Build -> Rebuild Solution trong Visual Studio.
2. Mở Windows PowerShell trong thư mục dự án, chạy:
   ```powershell
   .\Tests\TAO_ZIP_KIEM_TRA_DEM_TEP.ps1 -Count 400 -KibPerFile 128
   ```
3. Trong phần mềm, chọn **Quét nâng cao > Quét tệp** và chọn `Tests\LiveArchiveCounterTest.zip`.
4. Quan sát chỉ số `Đã kiểm tra`: tăng từ 0, 1, 2... trong lúc ứng dụng vẫn đang quét ZIP. Dòng `Tệp đang kiểm tra` hiển thị entry hiện tại. Không cần có chữ ký mã độc để thử khả năng đếm.
5. Kiểm tra tổng kết: mục ZIP đã kiểm tra đủ 400 entry nếu không có lỗi truy cập; các lỗi nếu có nằm trong thống kê chưa kiểm tra.
6. Có thể xóa ZIP thử nghiệm sau khi kiểm tra. Không cần tắt Windows Defender hoặc thử mã độc thật.

**Giới hạn:** Số tệp chỉ tăng khi **một entry đã kiểm tra xong**, không tăng theo phần trăm byte đọc trong một entry đang kiểm tra. ETA vẫn là ước lượng, không phải cam kết thời gian tuyệt đối. Bản này chưa được kiểm thử build WinForms thực tế trên Windows tại môi trường tạo ZIP.
