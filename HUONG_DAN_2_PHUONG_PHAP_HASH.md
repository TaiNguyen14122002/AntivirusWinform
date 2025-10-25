# AntivirusWinform — chỉ hai phương pháp sử dụng hàm băm

## Phương pháp 1. Hash truyền thống (toàn tệp)

- Đọc toàn bộ tệp **theo luồng**; tính SHA-256, và SHA-1/MD5 nếu SQL đang chứa chữ ký tương ứng.
- Tra cứu `VirusSignatures` đã kích hoạt.
- Nếu khớp chính xác, kết quả là `Hash toàn tệp`, có `SignatureID` và tên mã độc từ SQL.
- MD5/SHA-1 hỗ trợ chữ ký cũ, yếu về chống va chạm; ưu tiên SHA-256 trong dữ liệu mới.

## Phương pháp 2. Chunk Hash + Sliding Window + Rolling Hash

- Duy trì **cửa sổ trượt 48 byte** qua toàn bộ luồng dữ liệu tệp.
- Cập nhật **rolling hash** cho từng byte để tìm **trigger point** (mặc định 11 bit).
- Cắt chunk dựa trên nội dung tại trigger khi đã đạt `MinChunkSize` (512 B); cắt bắt buộc ở `MaxChunkSize` (8192 B).
- Băm **SHA-256 của từng chunk động**, không băm ở mọi vị trí byte.
- So khớp SHA-256 chunk với `ChunkSignatures`, tính số lượng khớp theo thứ tự, `MinMatchCount`, `SimilarityThreshold` (và `MaxGapBytes` nếu khai báo) của `ChunkSignatureGroups` có `ChunkingMode='CDC'`.
- Nếu đạt điều kiện, tạo kết quả `Chunk Hash + Sliding Window + Rolling Hash (CDC)` kèm `SignatureID` để ghi vào lịch sử và xử lý tệp.
- Đây là **Content-Defined Chunking (CDC)** và dấu vân tay tương đồng **riêng của dự án**, không phải triển khai ssdeep hoặc TLSH chuẩn.

## Các phương pháp đã ngừng sử dụng trong phát hiện

- Chunk cố định khớp ở mọi vị trí như một phương pháp thứ ba: **đã loại bỏ**.
- Phát hiện bằng **tên tệp** chứa `eicar`, **byte-prefix**, **heuristic/chấm điểm**: **đã loại khỏi quy trình kết luận trong ScanEngine**.
- Các luồng bảo vệ thời gian thực, VirusTotal (tra cứu theo hash), cách ly, nhật ký, UI vẫn giữ, nhưng mọi kết luận phát hiện nội bộ đều dựa vào một trong hai phương pháp trên.
- Hai chuỗi test cũ trong `ScanEngine` được giữ lại để hỗ trợ tạo tệp mẫu, **không tự động bị phát hiện**. File `AntivirusDB.Hash.DemoData.sql` tạo chữ ký SHA-256 cho tệp ASCII `SIM-MALWARE-SIGNATURE::A57C1F37::PAYLOAD` ở trạng thái tắt; chỉ kích hoạt khi kiểm thử có chủ đích.

## Cơ sở dữ liệu

1. Sao lưu `AntivirusDB`.
2. Chạy `AntivirusDB.ChunkHash.Migration.sql` nếu chưa có các cột CDC.
3. Không xóa bảng/chữ ký `FixedWindow` cũ, nhưng **engine sẽ không dùng chúng**.
4. Tạo chữ ký CDC bằng `Tests\CdcSignatureBuilder.csproj` từ mẫu đã xác minh; chạy script SQL được tạo và chỉ bật `IsActive=1` sau khi xác minh chữ ký.
5. Một nhóm CDC cần có đủ số chunk, `ChunkingMode='CDC'` và `IsActive=1`; bảng `VirusSignatures` cha cũng phải `IsActive=1`.

## Kiểm thử trên Windows với Visual Studio

```cmd
VERIFY_HYBRID_STREAMING.cmd
VERIFY_CDC_WINDOWS.cmd
```

Hai script kiểm thử không yêu cầu SQL Server. Mã nguồn đang ở .NET Framework 4.7.2. Cần chạy `Build -> Rebuild Solution` và kiểm thử kết nối SQL trực tiếp trong Windows để xác nhận phần tích hợp thật.

## Chú ý giới hạn

- Không còn giới hạn kích thước **4 MiB** cho engine Chunk CDC. Đọc 256 KiB/lần; tài nguyên phụ thuộc số profile CDC đang chạy (tối đa 4).
- Tệp lớn vẫn phải đọc dữ liệu, nên thời gian quét tùy ổ đĩa và số chữ ký.
- Khớp nhiều chunk chỉ là dấu hiệu tương đồng, không chứng minh chắc chắn tệp là mã độc. Tránh tự động cách ly chỉ dựa vào chữ ký CDC chưa được đánh giá false-positive.
