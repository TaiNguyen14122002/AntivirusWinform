# AntivirusWinform — Tối ưu tốc độ quét và dự kiến thời gian hoàn thành

## Các thay đổi

- **Một lần tải chữ ký SQL cho một phiên quét**, kể cả khi người dùng chọn nhiều thư mục/tệp. Không gửi câu truy vấn SQL cho từng tệp.
- **Một hàng đợi quét chung** cho nhiều đường dẫn người dùng chọn; tối đa 4 worker, hoặc số CPU nếu ít hơn.
- **Hash truyền thống** MD5/SHA-1/SHA-256 (chỉ thuật toán có chữ ký đang hoạt động) được tính trong một lượt đọc. Trúng **bất kỳ một hash**: phát hiện ngay, không chạy CDC cho tệp đó.
- **CDC** (Sliding Window + Rolling Hash + SHA-256 từng chunk): chỉ dùng khi hash toàn tệp không khớp. Chỉ băm SHA-256 của những chunk có kích thước xuất hiện trong chỉ mục chữ ký SQL.
- Tái sử dụng bộ đệm 256 KiB trên mỗi worker, tránh cấp phát lại cho từng tệp.
- Tiến trình theo **byte đã đọc và số tệp xử lý**, cập nhật trên UI khoảng 350 ms; ETA hiển thị sau khi việc liệt kê thư mục hoàn tất và có đủ số liệu.
- Không lưu cache kết quả 'sạch' để bỏ qua việc quét tệp; giảm nguy cơ bỏ sót tệp bị thay đổi.

## Cách xem ETA

Tổng quan → Quét ngay hoặc Quét nâng cao → Bắt đầu quét. Trong khung **TIẾN TRÌNH ĐANG QUÉT** sẽ xuất hiện:

- `Đã kiểm tra X tệp` / `Phát hiện Y mối đe dọa`
- `Còn lại: ~N phút M giây` và `Tốc độ: N MiB/s` (với cửa sổ đủ rộng)
- Thanh tiến trình theo phần trăm. Khi chưa thống kê đủ tệp: thanh quét dạng đang xử lý và dòng `Đang thống kê tệp…`.
- Dòng trạng thái quét phía trên luôn có số tệp hoàn thành, số tệp đã phát hiện qua liệt kê, ETA và tốc độ.

**ETA là dự đoán, không phải thời hạn đảm bảo.** Nó có thể biến đổi đáng kể với tệp rất lớn, nhiều tệp nhỏ, ổ đĩa HDD/USB/mạng, tiến trình khác đang dùng CPU/ổ đĩa hoặc khi CDC có nhiều profile.

## Kiểm thử trên Windows

1. Mở `ScanAndRemoveVirus.sln` và **Rebuild Solution** bằng Visual Studio với .NET Framework 4.7.2.
2. Chạy chương trình, quét một tệp có hash khớp chữ ký `IsActive=1`: phải phát hiện ngay, CDC không chạy cho tệp đó.
3. Quét thư mục chứa các tệp vô hại kích thước lớn để xem tốc độ MiB/s và ETA, thử chọn nhiều thư mục.
4. Trong Developer Command Prompt for VS, chạy `MSBuild Tests\HybridHashSelfTest.csproj /p:Configuration=Debug` rồi `Tests\bin\HybridHashSelfTest.exe` để kiểm tra ưu tiên hash truyền thống, CDC fallback và đếm byte hai lượt.
5. Thử bấm **Hủy quét**; ETA phải dừng hiển thị khi phiên kết thúc.
6. Nếu không phát hiện tệp nghi ngờ, kiểm tra hash tệp và 3 cột MD5/SHA1/SHA256 đang bật trong `VirusSignatures`, không suy luận từ tên tệp.

**Không thay đổi database, không cần migration SQL mới** cho bản tối ưu tốc độ/ETA.

## Giới hạn kiểm chứng

Đây là thay đổi mã nguồn. Chưa có điều kiện chạy thực tế Visual Studio/SQL Server/Windows trong môi trường đóng gói, nên cần đo tốc độ và kiểm thử tích hợp trên máy của bạn. Tốc độ tăng phụ thuộc loại thiết bị lưu trữ, số lượng chữ ký CDC và cách chọn phạm vi quét.
