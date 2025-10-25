# Trang Quản trị — Dashboard theo ảnh mẫu

## Những thay đổi đã lập trình

- Chỉnh trực tiếp trang `Quản trị` (WinForms C# .NET Framework 4.7.2), không tạo website, không thêm WebView.
- Phần **Thống kê hệ thống** phía trên: hai biểu đồ theo ngày/khoảng thời gian, biểu đồ phân loại mã độc, tỷ lệ cách ly, ba chỉ số, trạng thái cấu hình bảo vệ.
- Bộ lọc thời gian **7 / 30 / 90 ngày qua**, nút tải lại và thời gian tải dữ liệu lần cuối.
- Ba nhóm chức năng đặt cạnh nhau bên dưới: **Chính sách quản trị**, **Quản lý VirusTotal**, **Dữ liệu chữ ký cập nhật**.
- Giữ nguyên hai công tắc **Tự động cập nhật chữ ký** và **Gửi mẫu tệp nghi ngờ** cùng nghiệp vụ trước đây.
- Giữ các nút quản lý chữ ký, kiểm duyệt, cập nhật thủ công, kiểm tra dữ liệu, nguồn dữ liệu và **Thêm chữ ký từ tệp**.
- Giữ nguyên đăng nhập quản trị và các công cụ nâng cao, ở dưới có thanh cuộn.
- Không thêm/sửa bất kỳ bảng, cột hay file SQL/.bak nào.

## Dữ liệu thống kê thực

Các biểu đồ và chỉ số không dùng số minh họa cố định trong ảnh:

- `ScanHistory`: tổng phiên quét, tệp đã quét, trung bình thời gian, quét nhanh/toàn bộ theo từng khoảng ngày.
- `ThreatDetections` kết hợp `VirusSignatures`: phát hiện, nhóm mã độc, trạng thái cách ly và xử lý.
- `Settings`: trạng thái **cấu hình** bốn cơ chế bảo vệ, không đồng nghĩa trạng thái vận hành ở cấp Windows Service.
- `VirusSignatures`: số chữ ký và số chữ ký kích hoạt trong khối quản lý.

Khi SQL Server không truy cập được, dashboard hiện thông báo thiếu dữ liệu. Phần mềm **không** sử dụng các giá trị mẫu `1.248`, `342`, `94%` hay `100%` trong ảnh làm dữ liệu thật.

**Lưu ý về số liệu:** Số lượng cách ly được suy ra từ `ThreatDetections.Status`. Những mục không có trạng thái cách ly xác định sẽ không được tính vào tỷ lệ cách ly. Số tệp quét theo `ScanHistory.FilesScanned` là tổng số lần kiểm tra tệp trong những phiên phù hợp bộ lọc, có thể trùng tệp giữa nhiều phiên.

## Mã nguồn được chỉnh

- `Control/UcQuanTri.cs`: trang chủ, vị trí thẻ, kết nối nút.
- `Control/UcQuanTri.Dashboard.cs`: vẽ dashboard và truy vấn thống kê dạng chỉ-đọc, chạy trên luồng nền.
- `ScanAndRemoveVirus.csproj`: khai báo file C# bổ sung.

## Chạy trên máy Windows

1. Giải nén ZIP.
2. Mở `ScanAndRemoveVirus.sln` trong Visual Studio trên Windows.
3. Chạy **Build > Clean Solution**, sau đó **Build > Rebuild Solution**.
4. Chạy ứng dụng, mở **Quản trị**, đăng nhập với tài khoản Admin đã thiết lập.
5. Chọn `7 ngày qua` / `30 ngày qua` / `90 ngày qua`; nhấn nút ↻ để tải lại dữ liệu.
6. Kiểm tra nút **Thêm chữ ký từ tệp**, **Cấu hình API**, các công tắc và các module quản trị nâng cao.

Bản ZIP chứa **mã nguồn**. Các EXE dựng sẵn trong thư mục `Windows_Release` (nếu có) có thể là bản cũ và không đại diện cho thay đổi mới.

## Giới hạn kiểm thử

Đã kiểm tra tĩnh cú pháp khối/dấu ngoặc, danh sách file trong project, cấu trúc XML .csproj, và 16 file SQL/.bak đối chiếu byte với ZIP trước đó. Chưa biên dịch và chạy WinForms trên Windows trong môi trường tạo bản này. Khi Visual Studio phát sinh lỗi, cần dùng Error List mới để xác định chính xác.
