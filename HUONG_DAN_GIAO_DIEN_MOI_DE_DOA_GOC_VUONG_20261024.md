# Cập nhật giao diện Mối đe dọa – 24/10/2026

## Nội dung

- **Bỏ hẳn banner tiêu đề** "Mối đe dọa cần xử lý" phía trên thẻ danh sách, bao gồm mô tả và nút Cách ly tất cả cũ. Trang bắt đầu từ thẻ danh sách ở vị trí y=10.
- **Đổi icon ở tiêu đề danh sách** sang chiếc khiên có dấu chấm than (`shield-alert`) của bộ icon hiện có, không cần ảnh hoặc thư viện ngoài.
- **Đặt nút Cách ly tất cả cạnh nút Làm mới** ở bên phải thanh công cụ. Khi bề ngang dưới 1050px, hai nút này nằm cạnh nhau ở hàng phía trên, còn Cách ly/Xóa/Cho phép ở hàng dưới.
- **Đổi góc vuông** cho các thẻ/khung, ô tìm kiếm, nút, bộ lọc, danh sách xổ xuống, nhãn mức độ/trạng thái trong trang Mối đe dọa. Không thay đổi hình dạng thẻ ở những trang khác.
- Giữ nguyên xử lý dữ liệu thực, lọc/tìm kiếm, phân trang và thao tác xác nhận cách ly.

## File đã sửa

- `Control/Sketch20View.cs`: bỏ tiêu đề trùng, đưa thẻ danh sách lên cao, render các thẻ/nút/menu góc vuông riêng cho trang này.
- `Control/Sketch20View.Threats.cs`: icon khiên, bố trí hai nút cùng hàng, vẽ badge góc vuông.

## Áp dụng

Nếu đang dùng đúng bản `AntivirusWinform_MoiDeDoa_SuaCachLy_Font_EmptyState_20261024.zip`, có thể chép đè hai tệp `.cs` ở trên vào thư mục `Control`. Sau đó chọn **Build > Clean Solution > Rebuild Solution** trong Visual Studio.

## Kiểm thử cần thực hiện trên Windows

1. Trang Mối đe dọa không còn tiêu đề "Mối đe dọa cần xử lý" ngoài thẻ danh sách.
2. Icon khiên đúng vị trí; nút Cách ly tất cả cạnh Làm mới ở cả chế độ rộng và hẹp.
3. Thử kích thước cửa sổ có vùng nội dung rộng khoảng 520, 750, 1050 và 1250px; không có nhóm nút nào chồng nhau.
4. Kiểm tra dropdown Bộ lọc và Số dòng/trang là góc vuông, không bị mất chữ.
5. Chọn một mục và thử Cách ly (xác nhận hoặc hủy), đảm bảo kết quả vẫn đúng; khi trống, các nút không thực hiện xử lý.

**Lưu ý:** Môi trường tạo gói không có MSBuild/.NET Framework cho WinForms Windows, nên chỉ kiểm tra tĩnh mã và tính toàn vẹn ZIP, chưa xác nhận Rebuild trên Windows.
