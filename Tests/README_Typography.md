# Kiểm tra font và chữ đè

`Sketch20TypographySmoke.cs` mở form WinForms native, đo chữ bằng `TextRenderer.MeasureText` với Graphics và font của control đang chạy. Test kiểm tra chiều cao cần thiết, giao nhau với control nhập liệu và giao nhau giữa các vùng chữ. Việc giới hạn vùng vẽ không thay cho kiểm tra chữ có đủ chỗ: hàng xuống dòng vẫn bị báo lỗi nếu thiếu chiều cao.

Ma trận gồm 20 màn × 3 kích thước cửa sổ × 2 họ font; thêm 5 màn có đường dẫn dài/kết quả dài ở mỗi kích thước và họ font. Tổng 150 ca màn hình, mỗi ca có PNG native. Các kiểm tra bổ sung xác nhận placeholder nằm phía trước, placeholder ẩn khi nhập và API key vẫn được che; pixel chữ một dòng/nhiều dòng không vẽ ra ngoài ô; ký tự `&` được vẽ nguyên nghĩa. Đường dẫn dài phải giữ nguyên qua tooltip và menu sao chép.

Font mặc định chọn từ các họ đã cài; Windows ưu tiên Segoe UI, Arial, Tahoma. Môi trường Mono/Linux ưu tiên Arial hoặc font tương đương có sẵn; font dự phòng trong ma trận là GenericSansSerif. Font thực tế và DPI được ghi ở `font-metrics.tsv`. Nếu hai họ resolve thành cùng một họ, test chỉ chạy một profile và báo đúng số ca thực tế.

Chạy trên Windows bằng `VERIFY_DASHBOARD.cmd`, hoặc Linux bằng `python3 Tests/verify_dashboard_linux.py`. Không cần SQL hoặc VirusTotal trực tuyến cho ma trận dữ liệu mẫu. Ảnh Linux dùng `Graphics.CopyFromScreen` từ cửa sổ đang hiển thị; ảnh Windows dùng `DrawToBitmap`. Log lỗi chi tiết nằm ở `typography-issues.tsv`; không có hàng dữ liệu nghĩa là không phát hiện lỗi vùng chữ.

Các screenshot không thay cho xác nhận Windows tại DPI của người dùng. Lượt chạy được giao kèm mã nguồn dùng Mono/Xvfb tại 96 DPI, với Nimbus Sans và DejaVu Sans; chưa chạy trên Windows hoặc ở DPI 120/144. `VERIFY_DASHBOARD.cmd` sẵn để kiểm tra lại trên máy Windows.
