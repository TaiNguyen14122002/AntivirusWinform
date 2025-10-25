# Bổ sung nút Thêm chữ ký từ tệp — Trang Quản trị

## Thay đổi

Trong **Quản trị → Dữ liệu chữ ký & cập nhật** đã có nút **Thêm chữ ký từ tệp** nằm cùng nhóm các nút Quản lý chữ ký, Kiểm duyệt, Cập nhật thủ công, Kiểm tra dữ liệu, Nguồn dữ liệu.

Nhấn nút sẽ mở **cửa sổ Gửi mẫu tệp nghi ngờ** đã có trong dự án (`SampleSubmissionForm`). Cửa sổ cho phép chọn tệp hoặc danh sách hash, kiểm tra và nhập vào bảng `dbo.VirusSignatures` theo logic `SuspiciousSampleImporter` hiện tại.

- Mẫu/chữ ký chưa xác minh được lưu mặc định **`IsActive = 0`**. Chỉ kích hoạt khi đã có bằng chứng và quản trị viên xác nhận.
- Việc nhập tệp vào cơ sở dữ liệu **không gửi nội dung tệp lên VirusTotal/Internet**.
- Hai công tắc **Tự động cập nhật chữ ký** và **Gửi mẫu tệp nghi ngờ** trong Chính sách quản trị không bị sửa đổi.
- Nút mới thừa hưởng phiên đăng nhập Admin hiện có; sau khi đóng cửa sổ nhập mẫu, bộ đếm chữ ký trên trang được làm mới.
- Bố cục thẻ tự tính số hàng theo lượng nút hiện tại để không đè/chồng nút ở màn hình hẹp.

## Cấu trúc SQL

**Không thay đổi bất kỳ tệp `.sql`, `.bak`, bảng hay cột SQL Server nào**. Sử dụng bảng hiện có theo luồng cũ.

## Tệp mã nguồn được chỉnh sửa

- `Control/UcQuanTri.cs` — bổ sung nút và tính chiều cao động cho thẻ chữ ký.

## Kiểm tra trên Windows

1. Giải nén ZIP, mở `ScanAndRemoveVirus.sln` trong Visual Studio.
2. Chọn **Build → Rebuild Solution**, sau đó chạy ứng dụng.
3. Vào **Quản trị**, đăng nhập bằng tài khoản quản trị của bạn.
4. Trong **Dữ liệu chữ ký & cập nhật**, nhấn **Thêm chữ ký từ tệp**.
5. Chọn một tệp kiểm thử an toàn, giữ bỏ chọn xác minh rồi nhấn **Kiểm tra và lưu**. Kiểm tra bản ghi mới trong `VirusSignatures` (mặc định `IsActive = 0`).
6. Trở lại trang quản trị để xem số lượng chữ ký được làm mới.

> Chưa chạy biên dịch/kiểm thử trên Windows trong môi trường hiện tại. Không nhập/chạy mã độc thực sự ngoài môi trường phân tích cô lập.
