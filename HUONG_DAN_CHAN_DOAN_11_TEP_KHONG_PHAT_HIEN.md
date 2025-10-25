# Khắc phục kết quả quét 0 phát hiện dù có 26.590 chữ ký

## Những gì đã xác nhận từ mã nguồn

- `HybridSignatureScanner.GetSnapshot(true)` đọc các bản ghi `dbo.VirusSignatures` có `IsActive=1` (không giới hạn `.exe`, `.js`, `.vbs`...); nạp MD5, SHA-1, SHA-256 hợp lệ vào bộ nhớ.
- Khi quét, hàm `HybridSignatureScanner.ScanFullHash` tính hash của **toàn bộ dữ liệu tệp**, tra cứu các dictionary không phân biệt chữ hoa/thường. **Nếu có hash khớp, trả về phát hiện ngay**, không chạy CDC cho tệp đó.
- Nếu không khớp hash toàn tệp, engine chỉ chạy CDC khi có nhóm `ChunkSignatureGroups` đã được tạo theo định dạng CDC và đủ điều kiện kích hoạt.
- Trên ảnh người dùng: 26.590 chữ ký hash toàn tệp được nạp, **0 nhóm CDC hợp lệ**. Như vậy không có phát hiện từ phương pháp thứ hai. Điều này **không chứng minh** hash của 11 tệp có trong DB.
- Một số chữ ký dữ liệu mẫu như `11111111...`, `22222222...` là hash giả lập, không tương ứng với dữ liệu mã độc thực tế.

## Đã cải thiện mã nguồn ở bản này

- Ghi `ReadErrorCount` để phân biệt số tệp thử quét với số tệp không đọc được (file bị khóa, thiếu quyền, lỗi I/O). Không coi lỗi đọc là âm tính.
- Hộp thoại kết quả thông báo rõ lỗi đọc; đề nghị đối chiếu **hash thực tế** thay vì chỉ nhìn số chữ ký đã nạp.
- Giữ nguyên thứ tự ưu tiên: (1) hash toàn tệp khớp -> phát hiện ngay, (2) không khớp mới thử CDC, (3) không khớp -> "không phát hiện".

## Kiểm tra trực tiếp 11 tệp đang quét

1. Giải nén ZIP, chạy `KIEM_TRA_HASH_THU_MUC.cmd` trên Windows có SQL Server.
2. Nhập **đúng đường dẫn thư mục** chứa 11 tệp, thí dụ `C:\Users\tennguoidung\Desktop\XVirus-Samples`.
3. Công cụ kết nối `localhost\SQLEXPRESS` và database `AntivirusDB` như trong `Database/DbConnection.cs`. Nếu SQL của bạn đặt ở instance khác, dùng PowerShell để chỉ định: 

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\KiemTraHashThuMuc.ps1 -Folder 'C:\DUONG_DAN_THU_MUC' -Server 'localhost\SQLEXPRESS' -Database 'AntivirusDB'
   ```

4. Xem file `BaoCao_Hash_yyyymmdd_hhmmss.csv` trong thư mục dự án. Mỗi dòng bao gồm tên tệp, byte, SHA-256, SHA-1, MD5 và trạng thái khớp SQL.

| Giá trị `Status` | Ý nghĩa | Bước tiếp theo |
|---|---|---|
| `MATCH_HASH_TOAN_TEP` | SQL có chữ ký đang kích hoạt trùng hash toàn tệp | Nếu ứng dụng vẫn báo 0, gửi dòng CSV và ảnh kết quả: có khả năng lỗi tích hợp/quét phiên bản cũ |
| `KHONG_CO_HASH_KHOP_SQL` | Cả 3 hash không nằm trong chữ ký đang kích hoạt | Bổ sung **chỉ khi được xác minh** từ nguồn đáng tin; hoặc tạo nhóm CDC hợp lệ từ mẫu đã kiểm chứng |
| `LOI_DOC_HOAC_DOI_CHIEU` | Không đọc file hoặc không tra được SQL | Kiểm tra cột `Error`, quyền truy cập, tệp bị khóa, sự ổn định SQL |

**Không đưa hash của mọi tệp trong thư mục vào `VirusSignatures` với nhãn "mã độc" khi chưa kiểm chứng.** Việc đó gây phát hiện nhầm.

## Ví dụ EICAR để đối chiếu có thể tái lập

`VirusSignatures` có bản ghi EICAR với MD5 `44D88612FEA8A8F36DE82E1278ABB02F` và SHA-1 `3395856CE81F2B7382DEE72602F798B642F14140`. Đây là fingerprint của **mẫu EICAR chuẩn 68 byte**. Một tệp có tên `demo_eicar_named.dat` nhưng chỉ 53 byte không thể có hash toàn tệp đó.

## Về phương pháp thứ hai

Nhóm CDC hiện bằng 0: phải tạo chữ ký CDC từ tệp mẫu đã xác minh, cấu hình `ChunkingMode='CDC'`, `IsActive=1` ở nhóm và chunk, đủ ngưỡng `MinMatchCount`, kích thước chunk và cấu hình thuật toán giống khi quét. Đây là nguồn chữ ký khác với danh sách hash toàn tệp. Không nên tự tạo chữ ký CDC từ tất cả các tệp chưa được phân tích.

## Môi trường kiểm thử

Đã kiểm tra tĩnh các thay đổi. Chưa thể kết nối tới SQL Server trên máy người dùng hoặc chạy .NET Framework WinForms trong môi trường hiện tại. Công cụ PowerShell được viết cho Windows PowerShell 5.1, không sử dụng module bên ngoài; cần chạy trên máy để xác nhận.
