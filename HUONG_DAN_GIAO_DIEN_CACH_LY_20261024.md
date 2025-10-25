# Cập nhật giao diện Cách ly theo ảnh mẫu

## File thay đổi

- `Control/Sketch20View.Quarantine.cs` — **file mới**: giao diện Cách ly, dòng dữ liệu, phân trang 10/20/50 và cuộn nội bộ; các nút Khôi phục/Xóa được nối với xử lý hiện hành.
- `Control/Sketch20ReferencePages.cs` — trang Cách ly gọi giao diện mới, các trang khác giữ nguyên.
- `Control/Sketch20View.cs` — bổ sung trạng thái phân trang/scroll, ưu tiên con lăn chuột trong bảng.
- `ScanAndRemoveVirus.csproj` — đưa file C# mới vào dự án.

## Tính năng và lưu ý

1. Banner thông tin trên cùng, khung danh sách có tổng số tệp, lựa chọn Mới nhất/Cũ nhất.
2. Bảng gồm ô chọn, Tệp tin, Mức độ, Phát hiện, Ngày cách ly, Thao tác. Dòng xen kẽ màu nhạt, tiêu đề bảng màu xanh nhạt.
3. Mặc định 10 dòng/trang, có thể chọn 20 hoặc 50; nếu không đủ chỗ, thanh cuộn chỉ nằm trong thân bảng.
4. Phân trang có nút tới các trang cụ thể, trang đầu/cuối và nút trước/sau. Khi không có dữ liệu vẫn hiển thị tiêu đề và phân trang.
5. Chọn tất cả dùng dữ liệu hiện có; Khôi phục/Xóa vĩnh viễn vẫn gọi các hàm đang có của UcCachLy, bao gồm xác nhận an toàn trước khi khôi phục/xóa. Không sửa database.
6. Rê chuột vào dòng để xem tên, đường dẫn đầy đủ, tên phát hiện. Có thể nhấn Space chọn tệp, Enter xem chi tiết khi dòng đang được focus.

## Cách cập nhật

Sao lưu dự án, rồi chép các file đúng thư mục và **bao gồm cả file mới** `Control/Sketch20View.Quarantine.cs`. Nếu chỉ chép `.cs` mà không cập nhật `.csproj` trên dự án .NET Framework cũ, file mới sẽ không được biên dịch. Mở `ScanAndRemoveVirus.sln`, chọn Clean Solution rồi Rebuild Solution.

## Kiểm thử trên Windows

- Không có tệp: bảng rỗng, số tệp 0, nút thao tác vô hiệu.
- Có 6 tệp: tên, mức độ, phát hiện, ngày và nút Xem chi tiết hiển thị đúng; chọn nhiều dòng.
- Có trên 10 tệp: phân trang hoạt động, chuyển số dòng/trang 10/20/50, cuộn riêng khi cần.
- Chọn tệp rồi thử Khôi phục/Xóa vĩnh viễn, hủy hộp xác nhận và kiểm tra dữ liệu không bị xử lý nhầm.
- Thu nhỏ/phóng to cửa sổ và thử mức DPI 100%/125%/150%.

**Kiểm thử trực quan WinForms và tương tác với SQL Server/Windows chưa thực hiện được trong môi trường tạo tệp này.**
