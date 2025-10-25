# Sửa lỗi build giao diện Lịch sử

Phiên bản này sửa đúng 2 lỗi CS0136 và 1 cảnh báo CS0219 do thay đổi giao diện Lịch sử.

- `Control/Sketch20View.cs`: đổi tên biến `bar` và `max` riêng trong nhánh `Page == "history"` thành `historyBar`, `historyMax`, tránh trùng với biến xử lý cuộn chung ở cuối phương thức `TableMouseWheel()`.
- `Control/Sketch20View.History.cs`: xóa biến cục bộ `pad` không sử dụng.

Cách cập nhật: chép đè 2 file `.cs` vào thư mục `Control` của dự án, sau đó **Build > Clean Solution > Rebuild Solution** trong Visual Studio.

Chưa chạy được MSBuild dành cho .NET Framework 4.7.2 trong môi trường hiện tại. Nếu có lỗi khác, gửi ảnh Error List mới.
