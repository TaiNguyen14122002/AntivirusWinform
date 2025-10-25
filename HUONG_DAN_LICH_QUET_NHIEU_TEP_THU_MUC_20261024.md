# Đặt lịch quét nhiều tệp và thư mục

## Đã bổ sung

- Ở cửa sổ **Đặt thời gian quét virus tự động**, chọn chế độ **Quét tệp và thư mục (nhiều mục tiêu)**.
- **Thêm tệp**: chọn cùng lúc nhiều tệp bằng Ctrl/Shift hoặc chọn nhiều file trong hộp thoại Windows.
- **Thêm thư mục**: chọn một thư mục; bấm lặp lại để thêm thư mục thứ hai, thứ ba...
- **Danh sách** (hoặc nhấn vào ô tóm tắt) để xem toàn bộ đường dẫn, thêm hoặc xóa nhiều mục, xóa hết và áp dụng danh sách.
- Bảng lịch hiển thị tóm tắt số tệp/thư mục. Rê chuột vào cột **Tệp / thư mục** để xem các đường dẫn cụ thể.
- Task Scheduler chạy **một lịch**, engine nhận **nhiều mục tiêu**, cùng một snapshot chữ ký, một phiên lịch sử quét và một kết quả tổng hợp.
- Dữ liệu lịch đơn mục tiêu cũ vẫn đọc được. Không cần thay đổi bảng SQL Server.

## Ràng buộc

- Phải chọn ít nhất một mục tiêu trong chế độ quét tùy chọn.
- Mục tiêu phải có đường dẫn tuyệt đối và còn tồn tại khi lưu lịch.
- Tự bỏ đường dẫn trùng (không phân biệt hoa/thường). Các tệp nằm trong thư mục được chọn được engine khử trùng khi quét.
- Nếu đến giờ một số mục tiêu không còn tồn tại, vẫn quét các mục còn lại và ghi số lượng đã bỏ qua. Nếu tất cả mất thì báo lỗi, không ghi quét thành công.
- Giữ nguyên kiểm tra tên lịch, thời gian bắt đầu sau hiện tại trên 2 phút, tần suất và các ràng buộc lịch trước đó.
- Giới hạn lưu của cột `dbo.ScanSchedules.TargetPath` hiện tại là `NVARCHAR(1000)`. Nếu toàn bộ đường dẫn vượt 1.000 ký tự, phần mềm thông báo và yêu cầu chọn ít mục tiêu hơn hoặc chọn thư mục cha. Không tự cắt dữ liệu. Muốn lưu danh sách rất lớn cần thiết kế bảng mục tiêu riêng ở lần nâng cấp sau.

## Cấu trúc lưu dữ liệu

- Một đường dẫn: giữ nguyên kiểu dữ liệu cũ (chuỗi đường dẫn).
- Nhiều đường dẫn: chuỗi `MULTI_TARGETS_V1:` + ký tự xuống dòng + các đường dẫn cách nhau bởi ký tự xuống dòng (Windows không cho phép ký tự này trong tên tệp).
- `ScanScheduleService.DecodeTargets()` khôi phục cả hai định dạng, `EncodeTargets()` chuẩn hóa và chống trùng.
- Cả SQL `TargetPath` và bản lịch XML cục bộ đều dùng cấu trúc này.

## Kiểm thử trên Windows / Visual Studio

1. **Build → Clean Solution → Rebuild Solution**.
2. Mở **Đặt thời gian quét virus tự động** → chế độ **Quét tệp và thư mục (nhiều mục tiêu)**.
3. Chọn 2 tệp, thêm 2 thư mục khác nhau, mở **Danh sách**, xóa một mục, lưu lịch trong tương lai.
4. Chọn lại lịch: kiểm tra danh sách mục tiêu được nạp chính xác, không bị thành một chuỗi đường dẫn dài.
5. Bấm **Chạy ngay** khi lịch đang bật; kiểm tra **Lịch sử** có một phiên quét với kết quả tổng hợp.
6. Tạo lịch với một mục tiêu, sau đó mở lại: kiểm tra tương thích lịch cũ.
7. Chọn một mục không còn tồn tại rồi lưu: phải có cảnh báo; nếu mục bị xóa sau khi lưu, kiểm tra kết quả chạy có thông báo bỏ qua.
8. Kiểm tra lịch bị trùng tên, lịch trùng thông số nhưng đảo thứ tự mục tiêu, thời gian quá khứ và giới hạn 1.000 ký tự.
9. Kiểm tra cả trường hợp SQL Server không kết nối và sử dụng file lịch cục bộ tại `%LocalAppData%`.

**Lưu ý:** Chưa chạy được kiểm thử WinForms và Task Scheduler trong môi trường tạo bản cập nhật. Không thay đổi thuật toán phát hiện mã độc hoặc lược đồ SQL Server.
