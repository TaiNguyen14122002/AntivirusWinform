# Cập nhật giao diện Đặt thời gian quét virus tự động — 24/10/2026

## File thay đổi

- `Control/ScanScheduleForm.cs`: sửa bố cục và kiểm tra đầu vào, bảng trạng thái, các nút.
- `Services/ScanScheduleService.cs`: bổ sung ràng buộc tại dịch vụ, phát hiện lịch trùng, bảo vệ lịch một lần đã hết hạn.

Không thay đổi cấu trúc SQL Server hoặc tệp dữ liệu lịch cũ.

## Bố cục mới

- Tiêu đề và thời gian Windows hiện tại nằm ở phía trên.
- Thẻ "Thông tin lịch quét" có bốn hàng cùng chiều cao, co giãn theo chiều rộng cửa sổ.
- Chọn tệp hoặc thư mục ngay bên cạnh trường đường dẫn; chỉ bật cho chế độ quét tùy chọn.
- Bảng lịch sử dụng nền trắng, tăng chữ, có hàng xen kẽ và tooltip để xem đầy đủ đường dẫn, kết quả lỗi.
- Sáu nút được chia đều hàng dưới, đổi trạng thái theo bản ghi được chọn.
- Cửa sổ mặc định 1170 × 740 (vùng nội dung), tối thiểu 940 × 640; bảng chiếm phần chiều cao còn lại.
- Nút và khung đều góc vuông; dùng Segoe UI.

## Ràng buộc lưu lịch

1. Tên lịch sau khi bỏ khoảng trắng phải có 3–150 ký tự, không có xuống dòng.
2. Tất cả chế độ/tần suất đều phải có **thời gian bắt đầu muộn hơn hiện tại 2 phút**; không cho chọn thời gian trước hoặc đúng hiện tại để lưu.
3. Giới hạn thời điểm bắt đầu trong 5 năm tới để phòng chọn nhầm ngày.
4. Với Quét tệp hoặc thư mục: bắt buộc chọn đường dẫn tuyệt đối, tồn tại lúc đăng ký, dài tối đa 1.000 ký tự.
5. Với Quét nhanh / Toàn bộ: bỏ đường dẫn tùy chọn cũ, tránh dùng nhầm.
6. Khi tạo lịch mới, chặn trùng tên và lịch có cùng loại quét, mục tiêu, tần suất và thời gian. Lịch đã tồn tại trùng nhau từ phiên bản cũ vẫn có thể chỉnh sửa nếu giữ nguyên trường trùng.
7. Lịch hằng tháng ngày 29, 30, 31 có cảnh báo riêng: Windows sẽ bỏ qua tháng không có ngày tương ứng.
8. Bật lại lịch Một lần sau khi ngày đã qua: bị từ chối; phải sửa thời gian.
9. Xóa lịch cần hộp thoại xác nhận; Run Now chỉ hoạt động với lịch đang bật.
10. Nếu đăng ký Task Scheduler thất bại: báo lỗi thật, giữ lịch tắt để cho phép thử bật lại. Không báo "Đăng ký thành công" khi Windows từ chối.

## Kiểm tra trong Visual Studio

1. Giải nén dự án hoặc chép hai file sửa vào đúng đường dẫn như trên.
2. Mở `ScanAndRemoveVirus.sln`, chọn Build → Clean Solution rồi Rebuild Solution.
3. Mở cửa sổ Đặt lịch và thử thay đổi kích thước, xem bảng còn trống xám hay không.
4. Chọn ngày hôm qua, đúng hiện tại, sau 1 phút và sau 5 phút; chỉ trường hợp sau 5 phút được phép lưu.
5. Chọn Quét tệp hoặc thư mục mà chưa chọn đường dẫn: phải hiện cảnh báo.
6. Tạo hai lịch trùng tên/giờ với nhau: lịch sau phải bị chặn. Thử sửa lịch cũ có tên trùng từ trước.
7. Tạo lịch tháng 31: phải xuất hiện hộp thoại cảnh báo.
8. Thử tắt/bật/chạy ngay/xóa và kiểm tra Windows Task Scheduler (`taskschd.msc`).
9. Xem đầy đủ lỗi trên cột Kết quả bằng rê chuột vào ô bị cắt.

### Lưu ý về Windows Task Scheduler

Lỗi "Đăng ký thất bại: Task Scheduler ..." trong lịch cũ không phải do responsive của giao diện. Các tác vụ đã báo lỗi sẽ không tự được sửa. Kiểm tra quyền tài khoản Windows hiện tại, dịch vụ Task Scheduler và việc chạy chương trình dưới file `ScanAndRemoveVirus.exe` đã biên dịch; sau đó chọn lịch để thử bật lại. Cơ chế hiện tại dùng tài khoản Windows đang đăng nhập và không yêu cầu lưu mật khẩu.

Bản ZIP mới đã được kiểm tra cấu trúc, nhưng không thể Rebuild hoặc chạy Windows Task Scheduler trong môi trường Linux hiện tại.
