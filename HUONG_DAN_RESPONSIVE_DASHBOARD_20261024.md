# Điều chỉnh responsive trang Quản trị — 24/10/2026

## Mục đích

Khắc phục khoảng trắng dư trong vùng **Thống kê hệ thống**; giữ nguyên nghiệp vụ, dữ liệu SQL, danh mục **Top 5** (4 loại có tên + **Chưa phân loại**) và bộ lọc ngày màu xám/góc vuông.

## File nguồn thay đổi

- `Control/UcQuanTri.Dashboard.cs`

## Chi tiết

1. Điều chỉnh tỉ lệ chiều cao vùng biểu đồ trên xuống khoảng **54%**, nhường thêm không gian cho hàng chỉ số dưới. Cách tính dựa trên kích thước thực và có giới hạn tối thiểu.
2. Giảm khoảng cách giữa các khối biểu đồ từ 12 xuống 10 đơn vị vẽ; đồng bộ phép tính của nút **Xem tất cả** và phần tooltip tương ứng.
3. Phân bố đều **5 hàng** ở biểu đồ **Loại mã độc phổ biến** theo chiều cao thực, thanh ngang giữ độ dày giới hạn để không bị quá lớn.
4. Vòng tròn cách ly và cấu hình bảo vệ có thể lớn hơn khi còn không gian; danh sách trạng thái được căn lại nhằm tránh tràn ngoài khu vực ở cửa sổ thấp.
5. Ba thẻ **Tổng tệp đã quét**, **Mối đe dọa đã chặn**, **Thời gian quét TB** được căn cụm biểu tượng/nhãn/giá trị theo chiều dọc thay vì cách xa nhau theo phần trăm chiều cao.

## Kiểm thử khi mở trên Windows

- Mở `ScanAndRemoveVirus.sln` trong Visual Studio và **Rebuild Solution**.
- Kiểm tra kích thước cửa sổ toàn màn hình, 1366×768, 1280×720 và DPI 125%.
- Kiểm tra năm nhóm mã độc phổ biến, số đầy đủ, tooltip các chỉ số, nút **Xem tất cả** và bộ lọc ngày.
- Không chỉnh sửa cơ sở dữ liệu hoặc thuật toán thống kê.

> Chưa chạy build hoặc kiểm thử GDI+ WinForms trong môi trường hiện tại. Đã kiểm tra dấu ngoặc và các giới hạn bố cục theo công thức.
