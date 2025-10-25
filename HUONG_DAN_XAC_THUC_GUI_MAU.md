# Xác thực quản trị khi bật/tắt Gửi mẫu tệp nghi ngờ

## Cách kiểm tra

1. Mở `ScanAndRemoveVirus.sln`, chọn Build → Rebuild Solution rồi chạy ứng dụng.
2. Mở **Cài đặt → Gửi mẫu tệp nghi ngờ**, nhấn vào thẻ hoặc công tắc.
3. Thử **Hủy**: giữ nguyên trạng thái cũ trong ứng dụng và cột `Settings.SubmitSamples`.
4. Thử mật khẩu **sai**: báo lỗi xác thực và không thay đổi dữ liệu SQL.
5. Thử mật khẩu quản trị đã được cấu hình trong dự án: hiện hộp thoại nhỏ **Mật khẩu chính xác.** Sau khi đóng hộp thoại, công tắc đổi trạng thái và lưu `SubmitSamples` vào SQL Server.
6. Thử mất kết nối SQL: sau bước xác thực, giao diện phải báo lỗi lưu và hoàn nguyên công tắc.
7. Thử cả chế độ giao diện Sketch20 và checkbox Cài đặt cũ (nếu được dùng).

Đối chiếu trong SSMS:

```sql
USE AntivirusDB;
SELECT TOP (1) SettingID, SubmitSamples, UpdatedAt
FROM dbo.Settings ORDER BY SettingID ASC;
```

## Lưu ý

- Bước xác thực **không tự tải lên hoặc gửi tệp**. Nó chỉ cho phép bật/tắt cài đặt. Nếu cài đặt được bật, quy trình phân tích/tra cứu VirusTotal đã có trong dự án có thể gửi tệp đến dịch vụ bên ngoài theo cấu hình và điều kiện của ứng dụng. Chỉ bật khi đã hiểu rõ dữ liệu có thể được chia sẻ.
- Bộ kiểm tra mật khẩu được dùng lại từ chức năng Tự động cập nhật chữ ký; mật khẩu rõ không được ghi thêm trong mã nguồn.
- Đây là lớp kiểm soát ở giao diện, không thay thế phân quyền hệ điều hành hoặc kiểm soát phía máy chủ.
- Tính năng cập nhật MalwareBazaar không bị chỉnh sửa.
