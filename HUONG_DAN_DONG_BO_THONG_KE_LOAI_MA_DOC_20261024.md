# Đồng bộ tổng phát hiện và phân loại mã độc – trang Quản trị

## Vấn đề

`Mối đe dọa đã phát hiện` lấy tổng `ScanHistory.ThreatCount` của các phiên hoàn tất trong kỳ; số liệu theo loại lấy từ chi tiết `ThreatDetections` có liên kết `ScanID` với `VirusSignatures`.

Nếu một phát hiện chưa có `Category` hợp lệ, hoặc chưa có bản ghi chi tiết tương ứng, các nhóm có tên cộng lại có thể nhỏ hơn tổng phát hiện. Trước đây dashboard chỉ hiển thị `Chưa phân loại` thành dòng ghi chú phía dưới, không vẽ ở bảng `Loại mã độc phổ biến`.

## Thay đổi

- Tính riêng `UnclassifiedCount` và `UnclassifiedByDay` (cùng kỳ lọc và bucket thời gian với dữ liệu phát hiện).
- Các chi tiết thiếu loại hợp lệ được đếm vào `Chưa phân loại`.
- Phần `ScanHistory.ThreatCount` còn lại nhưng không tìm được chi tiết `ThreatDetections.ScanID` cũng được đếm vào `Chưa phân loại`. Đây là nhóm **chưa đủ thông tin để phân loại**, không có nghĩa tất cả các tệp này đã được xác nhận thuộc một loại cụ thể.
- Hiển thị hàng `Chưa phân loại` trong `Loại mã độc phổ biến`, chú giải biểu đồ theo ngày và popup `Xem tất cả`.
- Các loại đã xác định nhưng phải ẩn ở bảng phổ biến do diện tích giới hạn được cộng vào **`Các loại khác`** (không bị gộp nhầm với `Chưa phân loại`).
- Bảng phổ biến tự điều chỉnh số lượng dòng theo chiều cao thực tế để không chồng chữ khi thay đổi kích thước cửa sổ; popup `Xem tất cả` giữ nguyên từng tên loại đã xác định.
- Hiển thị `Tổng ... phát hiện` tại thẻ `Loại mã độc phổ biến` để đối chiếu ngay.
- Không cập nhật dữ liệu, không sửa bảng SQL, không tự ý gán `Category` trong database.

## Kiểm chứng

Với số liệu ví dụ từ ảnh người dùng:

- Tổng phát hiện: **4.525**
- Đã phân loại (toàn bộ tên loại, không chỉ các loại phổ biến): **1.104**
- Chưa phân loại: **3.421**
- **1.104 + 3.421 = 4.525**.

Khi bảng phổ biến không hiển thị hết tên loại:

`Tổng các dòng loại cụ thể + Các loại khác + Chưa phân loại = Tổng phát hiện`.

## Tệp đã sửa

- `Control/UcQuanTri.Dashboard.cs`

## Kiểm tra trên Windows

1. Mở `ScanAndRemoveVirus.sln` trong Visual Studio và `Build > Rebuild Solution`.
2. Vào `Quản trị` → chọn `7 ngày qua`. Đối chiếu tổng `Mối đe dọa đã phát hiện` với các dòng trong `Loại mã độc phổ biến`.
3. Nhấn `Xem tất cả`: phải hiển thị cả dòng `Chưa phân loại`, nếu có.
4. Thu nhỏ cửa sổ: tên loại có thể thu gọn thành nhóm `Các loại khác`, nhưng số lượng tuyệt đối không được biến mất.
5. Thử các bộ lọc ngày khác nhau. Khi có truy vấn SQL chi tiết lỗi, xem nút `Xem lỗi` để biết nguyên nhân.

Lưu ý: file đã được kiểm tra cấu trúc tĩnh, nhưng môi trường đóng gói này không có công cụ MSBuild dành cho WinForms trên Windows, nên chưa thể xác nhận build/run thực tế.
