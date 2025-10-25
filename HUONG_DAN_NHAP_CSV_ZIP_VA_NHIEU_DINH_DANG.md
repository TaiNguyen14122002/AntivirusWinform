# Gửi mẫu nghi ngờ — hỗ trợ CSV.ZIP và nhiều định dạng

## 1. Hai loại dữ liệu nhập

**Danh sách chữ ký có sẵn** — tệp `.csv`, `.tsv`, `.jsonl`, `.ndjson`, `.json` (mảng JSON tối đa 16 MiB), `.csv.zip`, `.csv.gz`, `.tsv.gz`, `.jsonl.gz`, `.sha256`, `.sha1`, `.md5`, `.hashlist`, `.hashes.txt`. Với ZIP, nếu có entry là các định dạng danh sách này thì chỉ đọc các entry danh sách thành bản ghi và **không lưu hash của chính ZIP hay của README/tệp đính kèm khác**. Có bộ mẫu `CSV_Kem_Readme_Khong_Nhap_Hash_Readme.csv.zip` để kiểm tra việc này. CSV/TSV/JSONL được đọc tuần tự, 1.000 chữ ký/lô. CSV.ZIP có thể chứa CSV có kích thước lớn.

**Tệp mẫu nhị phân/script** — bất kỳ tệp nào (`.exe`, `.dll`, `.sys`, `.scr`, `.js`, `.vbs`, `.ps1`, `.bat`, `.cmd`, `.com`, `.pdf`, `.docm`, `.apk`, `.jar`, `.bin`, v.v.) được tính MD5/SHA-1/SHA-256 **theo nội dung tệp**. ZIP không có tệp danh sách được hash chính ZIP và hash các entry tệp trong ZIP, không ghi tệp ra đĩa. `.rar`, `.7z`, `.tar`, `.iso` chưa hỗ trợ đọc tệp con; nếu chọn thì chỉ hash chính file nén. ZIP mã hóa mật khẩu chưa được hỗ trợ.

Các file CSV/JSON là **danh sách hash**, không phải đối tượng mã độc. Không được dùng hash của `database.csv.zip` hay `database.csv` làm chữ ký mã độc.

## 2. Tiêu đề cột chấp nhận

- `SHA256` / `sha256_hash`, `SHA1` / `sha1_hash`, `MD5` / `md5_hash`. Phải có tối thiểu **một hash hợp lệ** 64/40/32 ký tự hex.
- `MalwareName` / `malware_name` / `signature`, `MalwareFamily` / `malware_family`, `file_name` / `filename`, `FileExtension` / `file_extension`, `file_type`.
- Trường `Description` / `first_seen_utc` được đưa vào mô tả; dữ liệu `Severity`, `RecommendedAction`, `IsActive` ở file nguồn **không được tự tin cậy**.
- CSV hỗ trợ trường có dấu nháy và nhiều dòng trong dấu nháy, BOM UTF-8, các dòng comment bắt đầu `#` trước header. Hash không đúng định dạng bị bỏ qua.
- `.jsonl` / `.ndjson`: mỗi dòng là một đối tượng JSON. `.json`: mảng đối tượng, một đối tượng, hoặc object có trường `data`/`samples` là mảng.

## 3. Trùng lặp và bảo vệ dữ liệu

Các hash MD5/SHA-1/SHA-256 được chuẩn hóa chữ hoa, đối chiếu với bảng `VirusSignatures`. Trùng **bất kỳ** hash hiện có thì không thêm mới. SQL dùng `SqlBulkCopy` 1.000 dòng/lô và lệnh `INSERT ... NOT EXISTS`, không sửa mẫu cũ. Việc nhập có thể hủy giữa chừng, các lô đã lưu vẫn giữ lại.

Tùy chọn **`IsActive=1` được chọn mặc định theo yêu cầu** của dự án. Ứng dụng phải yêu cầu xác nhận trước khi thêm chữ ký đang hoạt động. **Chỉ chọn kích hoạt khi bản ghi đã được xác minh độc hại**; dữ liệu từ người dùng/CSV không phải bằng chứng xác minh. Bỏ chọn để nhập vào trạng thái `IsActive=0` và tránh phát hiện nhầm.

Giới hạn an toàn: tối đa 500 tệp chọn/lượt, 2.000 entry trong ZIP, 512 MiB/entry nhị phân, 8 GiB cho CSV entry, tổng tối đa 12 GiB CSV trong ZIP, mỗi tệp được chọn tối đa 8 GiB. Bộ đọc theo luồng giới hạn RAM ở mức bộ đệm và lô SQL, không nạp toàn bộ CSV vào bộ nhớ.

## 4. Kiểm thử chức năng

Trong `Tests/SubmissionFormatFixtures` có `.csv.zip`, `.csv.gz`, `.csv`, `.tsv`, `.jsonl`, `.json`, `.sha256` và tệp `.js` vô hại. Các tệp danh sách mô tả một hash có thật của `sample-harmless.js` và một hash giả lập khác.

1. **Bỏ chọn `IsActive=1`** vì đây là dữ liệu thử nghiệm vô hại.
2. Chọn `MalwareBazaar-format-example.csv.zip`, `CSV_Kem_Readme_Khong_Nhap_Hash_Readme.csv.zip`, sau đó chọn từng định dạng khác và kiểm tra thống kê "đã tồn tại". Với ZIP có README, số chữ ký thêm mới không được tính hash của README.
3. Kiểm tra bằng câu lệnh SQL:

```sql
SELECT TOP (30) SignatureID, MalwareName, MD5, SHA1, SHA256, IsActive, CreatedAt
FROM dbo.VirusSignatures
WHERE Description LIKE N'%danh sách hash do người dùng nhập%'
ORDER BY SignatureID DESC;
```

4. Nhập lại danh sách phải báo trùng, không nhân đôi số chữ ký.
5. Kiểm tra ZIP không có CSV: chọn `sample-not-malware.zip` từ thư mục fixture cũ; đây là chế độ tính hash tệp.

**Quan trọng:** Bản này chưa được biên dịch trên Windows và chưa kiểm thử trực tiếp với SQL Server. Cần chạy Build/Rebuild Solution, thử trên database sao lưu trước khi sử dụng dữ liệu lớn. Không đưa tệp mẫu lên Internet.
