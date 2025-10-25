# Quy tắc OR — Hash truyền thống (MD5 / SHA-1 / SHA-256)

Trong `Services/HybridSignatureScanner.cs`, phương pháp 1 đọc toàn tệp một lượt,
chỉ tính những hash đang có chữ ký trong SQL, rồi lần lượt kiểm tra SHA-256, SHA-1, MD5.

- Nếu **bất kỳ một** mã băm toàn tệp trùng chữ ký hoạt động trong `VirusSignatures`, trả về kết quả phát hiện ngay (`FullHashMatch`) và **không chạy CDC**.
- Nếu không mã nào trùng, mới chạy CDC: Sliding Window + Rolling Hash xác định chunk, xác minh SHA-256 các chunk.
- Không yêu cầu cả ba mã trùng, không yêu cầu tất cả hash trong một dòng SQL phải được nhập.
- Không có nhóm CDC hợp lệ không làm vô hiệu hóa phương pháp hash toàn tệp.
- Nếu không có chữ ký nào trùng, ghi là **không phát hiện với chữ ký đã có**, không khẳng định tệp chắc chắn an toàn.

## Kiểm tra tại Visual Studio

Build solution, chạy file `VERIFY_HYBRID_STREAMING.cmd` (hoặc `Tests/HybridHashSelfTest.csproj`).
Bài kiểm thử kiểm tra từng tình huống chỉ SHA-256 / chỉ SHA-1 / chỉ MD5 trùng,
trường hợp không mã nào trùng và ưu tiên hash toàn tệp trước CDC.

## SQL Server

```sql
SELECT SignatureID, MalwareName, MD5, SHA1, SHA256, IsActive
FROM dbo.VirusSignatures WHERE IsActive = 1;
```

Lưu ý hash trên một dòng phải là mã băm của tệp thực tế; tên tệp hay phần mở rộng không thay thế cho việc hash khớp.

Mẫu EICAR chuẩn dài 68 byte có MD5 `44D88612FEA8A8F36DE82E1278ABB02F`.
