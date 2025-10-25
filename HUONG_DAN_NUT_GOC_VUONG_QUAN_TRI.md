# Chỉnh giao diện góc vuông tại trang Quản trị

Các nút thao tác trên trang Quản trị vẫn giữ FlatStyle.Flat và viền 1 px, góc vuông.
Bộ lọc khoảng ngày được chuyển sang góc vuông ở cả ô chính, vùng mũi tên, bảng tùy chọn và dòng đang chọn.
Màu xanh nhạt, viền xanh và chức năng lọc ngày không thay đổi.

## Mã nguồn được sửa
- `Control/UcQuanTri.cs`: cấu hình nút Quản trị góc vuông, viền 1 px.
- `Control/UcQuanTri.Dashboard.cs`: đặt `SquareCorners = true` cho bộ lọc ngày.
- `Control/UiSelect.cs`: hỗ trợ thuộc tính `SquareCorners` (mặc định `false` cho trang khác), áp dụng cho popup và các lựa chọn.

## Kiểm tra trên Windows
1. Giải nén và mở `ScanAndRemoveVirus.sln` trong Visual Studio.
2. Build > Clean Solution, sau đó Build > Rebuild Solution.
3. Vào Quản trị, kiểm tra bộ lọc ngày trong trạng thái bình thường, rê chuột, mở danh sách, và resize cửa sổ.

Lưu ý: Chưa thể xác nhận build Windows trong môi trường tạo ZIP này do thiếu bộ reference assemblies .NET Framework 4.7.2.
