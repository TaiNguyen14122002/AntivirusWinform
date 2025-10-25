# Xác thực quản trị cho Tự động cập nhật chữ ký

## Sử dụng

1. Mở trang **Cài đặt** → **Thiết lập chung**.
2. Nhấn vào thẻ **Tự động cập nhật chữ ký** hoặc công tắc của mục này.
3. Hộp thoại **Xác thực quản trị viên** yêu cầu mật khẩu (được che bằng dấu chấm).
4. Khi nhập đúng mật khẩu quản trị được cấu hình trước, ứng dụng hiển thị một popup nhỏ **Mật khẩu chính xác.** và thay đổi trạng thái tính năng.
5. Khi nhập sai hoặc nhấn **Hủy**, trạng thái không đổi. Lần thay đổi tiếp theo phải xác thực lại.

Đường giao diện Bảo vệ (cả UI Sketch20 và bảng truyền thống) và thao tác khôi phục mặc định cũng yêu cầu xác thực khi chúng làm thay đổi cờ cập nhật.

## Ghi chú bảo mật

- Giá trị mật khẩu không được ghi rõ trong mã nguồn. Ứng dụng lưu salt và PBKDF2 verifier, thực hiện so sánh hằng thời gian.
- Đây là bảo vệ ở cấp giao diện của ứng dụng desktop; người có quyền sửa file nhị phân hoặc database có thể bỏ qua nó. Không tương đương xác thực tài khoản quản trị Windows hoặc xác thực phía máy chủ.
- Mục cập nhật chữ ký của dự án vẫn sử dụng luồng cập nhật đã có trước bản sửa này. Hộp thoại mật khẩu chỉ điều khiển quyền thay đổi thiết lập, không tự tạo nguồn tải chữ ký mới.

## Kiểm tra thủ công

| Thao tác | Kết quả mong đợi |
|---|---|
| Nhấn thẻ Tự động cập nhật | Hiện form nhỏ nhập mật khẩu |
| Nhấn trực tiếp công tắc | Hiện form nhỏ nhập mật khẩu |
| Chọn Hủy hoặc đóng form | Giữ nguyên trạng thái |
| Nhập sai | Popup báo mật khẩu không chính xác, trạng thái không đổi |
| Nhập đúng | Popup **Mật khẩu chính xác.**; công tắc đổi trạng thái |
| Nhấn lần thứ hai | Yêu cầu nhập mật khẩu lại |
| Mở Bảo vệ, bấm tính năng cập nhật | Có xác thực tương tự |

Mở `ScanAndRemoveVirus.sln` trong Visual Studio trên Windows và chọn **Build → Rebuild Solution** trước khi kiểm tra.
