# Content-Defined Chunking (CDC) — antivirus hybrid scanner

## Phân biệt thuật ngữ

Bản này tích hợp **Content-Defined Chunking** sử dụng **Sliding Window + Rolling Hash + điểm cắt theo nội dung (Trigger Point)**. Sau đó mỗi chunk được xác minh bằng **SHA-256** và so khớp thành chuỗi chunk có thứ tự. Kết quả là **dấu vân tay CDC dựa trên các chunk** và tỷ lệ tương đồng của nhóm chữ ký.

Đây là **thiết kế fuzzy-like riêng của dự án**, **KHÔNG phải triển khai định dạng/thuật toán chuẩn `ssdeep` hoặc `TLSH`**. Cụ thể `ssdeep` dùng context-triggered piecewise hashing cùng cách mã hóa/so sánh riêng; `TLSH` dùng local-sensitive hashing cho toàn bộ đặc trưng tệp. Không nên gọi chuỗi các SHA-256 chunk của dự án là kết quả `ssdeep`/`TLSH`.

## Luồng quét

1. **Hash truyền thống**: SHA-256 (cùng SHA-1/MD5 tương thích) toàn tệp, đối chiếu `VirusSignatures`.
2. **Chunk Hash + Sliding Window + Rolling Hash (CDC)**: đọc theo luồng 256 KiB/lần, duy trì cửa sổ cuốn dài `RollingWindowBytes` (mặc định 48 byte) trên toàn bộ tệp; rolling hash tìm trigger point. **Không chạy thêm nhánh fixed-window riêng.**
3. Sau tối thiểu `MinChunkSize` byte, nếu `(rollingHash & ((1 << BoundaryMaskBits) - 1)) == 0`, tạo chunk tại điểm đó. Nếu chưa có trigger, cắt bắt buộc ở `MaxChunkSize`.
4. Mỗi chunk có `OriginalOffset`, `ChunkSize`, `SequenceOrder`, SHA-256. SHA-256 tính **một lần tại điểm cắt**, không phải mỗi byte.
5. So sánh với nhóm CDC từ SQL: đối chiếu **SHA-256 từng chunk**, kiểm tra thứ tự xuất hiện, ngưỡng tối thiểu `MinMatchCount` và tỷ lệ `SimilarityThreshold` (mặc định 0.6). Chỉ khi cả hai ngưỡng đạt mới tạo phát hiện. Có thể thêm giới hạn `MaxGapBytes`; nhánh này dùng matcher tương thích, chi phí cao hơn.
6. Khi tìm được chữ ký: đưa phát hiện kèm `SignatureID` về engine, trang Chi tiết tệp, Lịch sử và Cách ly như cũ.

Phương thức `FormatCdcFingerprint(...)` xuất chuỗi `CDC1|cấu hình|kích_thước:SHA256,...` cho nghiên cứu; `CompareCdcFingerprints(...)` tính tỷ lệ khớp **có xét thứ tự** và không đếm một chunk đích nhiều lần. Đây là định dạng riêng của dự án, không tương thích với chuỗi `ssdeep`/`TLSH`. Cơ chế phát hiện thực tế không cần xây dựng toàn bộ chuỗi này trong RAM.

**Ví dụ**: với một mẫu đã xác minh gồm 70 chunk, `MinMatchCount=3` và `SimilarityThreshold=0.70`, cần ít nhất 49 chunk khớp đúng thứ tự. Một đoạn byte chèn vào có thể làm thay đổi một vài chunk quanh điểm chèn, nhưng về sau trigger điểm cắt có thể đồng bộ trở lại, giúp nhận diện các chunk còn nguyên.

## SQL Server: nâng cấp an toàn

- Sao lưu dữ liệu `AntivirusDB`.
- **Cơ sở dữ liệu đang dùng**: chạy `AntivirusDB.ChunkHash.Migration.sql` (giữ nhóm cũ ở dạng lưu trữ, **không quét nhóm FixedWindow**).
- **Cài DB từ đầu**: file `AntivirusDB.sql` đã có các cột CDC mới.
- Bảng `ChunkSignatureGroups` được bổ sung:
  - `ChunkingMode`: chỉ `CDC` được engine sử dụng; nhóm `FixedWindow` cũ còn trong dữ liệu nhưng không được quét;
  - `RollingWindowBytes`, `MinChunkSize`, `AvgChunkSize`, `MaxChunkSize`, `BoundaryMaskBits`;
  - `SimilarityThreshold` (0.2–1.0).
- `ChunkSignatures` giữ các cột `ChunkHash`, `ChunkSize`, `SequenceOrder`, `OriginalOffset`. `FastHash` là cột tương thích lịch sử, **không được engine dùng**. CDC tính trigger trực tiếp từ byte và tra cứu SHA-256 chunk.
- Cấu hình CDC mặc định trong code: cửa sổ 48 B, min 512 B, trung bình **xấp xỉ** 2048 B (không bảo đảm), max 8192 B, trigger 11 bit. `AvgChunkSize` là tham số metadata/kiểm tra cấu hình, còn tần suất cắt chủ yếu phụ thuộc `BoundaryMaskBits`.
- Có thể nạp tối đa 4 cấu hình CDC khác nhau trong một snapshot để giới hạn CPU. Các nhóm dùng chung cấu hình được chia sẻ một bộ đọc.

**Quan trọng**: Không thể chuyển chính xác dữ liệu chữ ký FixedWindow sang CDC bằng cách đổi `ChunkingMode`. Cần đọc lại **byte gốc của mẫu đã được kiểm chứng** và tạo chuỗi chunk mới.

## Tạo chữ ký CDC từ mẫu đã được xác minh

Dùng trên máy thử nghiệm cô lập với tệp mẫu tin cậy; không chạy tệp, chỉ đọc byte:

```cmd
msbuild Tests\CdcSignatureBuilder.csproj /t:Rebuild /p:Configuration=Release
Tests\bin\CdcSignatureBuilder.exe "C:\SafeSamples\verified-sample.bin" 42 "Verified Sample CDC v1" "C:\SafeSamples\CDC.SignatureImport.sql"
```

- `42` phải là `SignatureID` **đang tồn tại và IsActive=1** trong `dbo.VirusSignatures`.
- Script SQL được ghi ra tệp theo luồng, không nạp mọi chunk lên RAM; `ChunkingMode='CDC'`, `MinMatchCount=3`, `SimilarityThreshold=0.6000`.
- **Nhóm mới mặc định `IsActive=0`** để tránh sử dụng chữ ký chưa kiểm chứng. Sau kiểm tra về tính đặc trưng và false positive mới chủ động bật nhóm trong SQL.
- `ChunkHash` = SHA-256 của **nguyên chunk**; không dùng rolling hash 64-bit để xác nhận mã độc.
- Không nhập tự động dữ liệu CDC của tệp ngẫu nhiên thành chữ ký mã độc sản xuất.

## Bộ kiểm thử không cần SQL Server

```cmd
VERIFY_CDC_WINDOWS.cmd
```

Bao gồm: xác định điểm cắt, kiểm tra offset, hash toàn tệp ưu tiên, khớp CDC trên mẫu sạch, chèn 1.931 byte giữa file (giữ nhiều chunk gốc), loại trừ dữ liệu ngẫu nhiên khác, streaming quét file 12 MiB, và hủy quét.

Bộ kiểm thử C# chưa được chạy trong môi trường soạn sửa (không có MSBuild/.NET Framework). Phải biên dịch và kiểm thử trên Windows trước khi dùng với dữ liệu thật.

## Hiệu năng và giới hạn

- Không có giới hạn **4 MiB** của bản trước; quá trình quét CDC chính thức **không nạp cả file vào RAM**. Với mỗi cấu hình CDC chỉ dùng 1 rolling window và 1 chunk buffer (tối đa 256 KiB); đọc stream theo buffer 256 KiB.
- Tốc độ phụ thuộc vào số profile CDC, khả năng đọc đĩa và số chunk cần băm. Đừng đánh đồng thuật toán này với một trình antivirus thương mại có cơ sở dữ liệu malware được kiểm chứng đầy đủ.
- Danh sách fingerprint **xuất để nghiên cứu** (`CreateContentDefinedFingerprint`) có giới hạn `maxChunks` vì cố tình lưu các digest trong bộ nhớ. Engine **quét tệp** và **xuất script SQL** không bị giới hạn bằng danh sách đó.
- Chunk nhỏ và các nhóm khớp phổ biến có thể gây false positive; nên dùng ngưỡng tương đồng, nhiều chunk, độ đặc trưng và xét cả kết quả phát hiện khác trước khi cách ly tự động.
- Không nên mặc định coi một kết quả tương đồng là chắc chắn độc hại: xét Severity, ngưỡng và xác minh thủ công; một lần so sánh tương đồng chỉ là một tín hiệu phân tích.
