# Trang Quản trị: 5 loại phổ biến và hiển thị đầy đủ số liệu

## Thay đổi

- `Control/UcQuanTri.Dashboard.cs`: biểu đồ **Loại mã độc phổ biến** hiển thị tối đa **4 loại đã xác định nhiều nhất + 1 nhóm Chưa phân loại** (nếu có dữ liệu; nhóm Chưa phân loại hiển thị kể cả giá trị bằng 0).
- Không còn hiển thị **Các loại khác** trong biểu đồ top 5; toàn bộ nhóm được giữ tại **Xem tất cả**.
- `DrawText`: chuyển từ `StringTrimming.EllipsisCharacter` sang `StringTrimming.None` và đo chuỗi để giảm cỡ chữ khi cần; dành thêm độ rộng cho các giá trị số ở biểu đồ ngang và bảng tỷ lệ cách ly.
- Giữ nguyên cơ sở dữ liệu, truy vấn SQL, bộ lọc ngày và các chức năng trước đó.

## Đối chiếu với số liệu trong ảnh

Ví dụ tổng 4.525, trong đó Chưa phân loại 3.421, bốn loại phổ biến nhất là Spyware 522, Ransomware 278, Trojan 141 và Worm 32. Biểu đồ **Top 5** sẽ hiển thị 5 nhóm này. Do chỉ chọn 4 nhóm đã phân loại đứng đầu, **tổng cộng 5 hàng không nhất thiết bằng tổng 4.525 phát hiện**. Những loại khác (Dropper, Backdoor, Wiper...) không bị mất, vẫn hiển thị trong **Xem tất cả**.

## Kiểm tra

1. Giải nén dự án, mở `ScanAndRemoveVirus.sln` trong Visual Studio trên Windows và Rebuild Solution.
2. Mở trang Quản trị, thử các kỳ 7/30/90 ngày và khoảng ngày tùy chọn.
3. Kiểm tra số nhóm ở biểu đồ bên phải (tối đa 5, có Chưa phân loại) và số lớn không hiển thị dấu ba chấm.
4. Thu nhỏ, phóng to cửa sổ; kiểm tra hiển thị ở các mức Scale Windows khác nhau (100%, 125%, 150%).

**Lưu ý:** Chưa được kiểm chứng bằng chạy WinForms trên Windows trong môi trường chỉnh sửa này.
