# Sửa giao diện Chi tiết tệp – không dùng thanh cuộn ngoài

## Các file mã nguồn cập nhật

- `Control/Sketch20View.cs`
- `Control/Sketch20ReferencePages.cs`

## Thay đổi

1. Tắt thanh cuộn của **toàn bộ trang Chi tiết tệp**; giữ nguyên thanh cuộn riêng trong bảng kết quả VirusTotal/chuỗi ký tự.
2. Giảm nhẹ khoảng cách phía trên vùng thông tin tệp và tự phân bổ chiều cao tab theo khung cửa sổ.
3. Tab VirusTotal tự thu gọn cảnh báo/thẻ thống kê và tự tính số hàng công cụ được hiển thị dựa trên diện tích còn lại; các nút phía dưới không bị các dòng bảng che.
4. Khi cửa sổ quá thấp hoặc Thông tin chi tiết xếp một cột, cuộn **bằng con lăn chuột** mà không hiện thanh cuộn ngoài.
5. Khi chuyển tab hay chuyển sang tệp mới, đưa vị trí cuộn của trang về đầu. Giữ nguyên thuật toán phân tích và dữ liệu.

## Áp dụng

Giải nén và chép đè hai file trong thư mục `Control`, sau đó mở `ScanAndRemoveVirus.sln` và **Clean Solution → Rebuild Solution**.

## Kiểm thử trên Windows

- Mở Chi tiết tệp → VirusTotal tại kích thước cửa sổ thường. Xác nhận **không còn thanh cuộn dọc ngoài cùng bên phải**.
- Rê con lăn trong danh sách công cụ; các hàng tiếp theo phải xuất hiện, bảng không đè lên các nút cuối trang.
- Mở các tab Thông tin chi tiết, Hoạt động và Thông tin bổ sung.
- Thay đổi kích thước cửa sổ: cửa sổ thấp/narrow vẫn có thể cuộn đến thông tin cuối bằng chuột.
- Chuyển tab; vị trí cuộn trang phải trở lại đầu.

**Chú ý:** Mã nguồn được kiểm tra tĩnh về dấu ngoặc và công thức bố cục, nhưng chưa thể biên dịch/chạy WinForms bằng MSBuild trên Windows tại đây.
