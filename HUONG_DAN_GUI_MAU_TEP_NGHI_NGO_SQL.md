# Gửi mẫu tệp nghi ngờ → chuẩn hóa hash → AntivirusDB

## Thao tác

1. Mở **Cài đặt → Gửi mẫu tệp nghi ngờ**, nhấn **vào thẻ nội dung** (hoặc bật công tắc, nếu trước đó chưa bật).
2. Nhập mật khẩu quản trị trong hộp thoại. Không có popup "Mật khẩu chính xác" trung gian: sau xác thực hiển thị ngay cửa sổ **Gửi mẫu tệp nghi ngờ**.
3. Chọn **Chọn tệp...** để chọn nhiều file `.zip`, `.exe`, `.js`, `.dll`, `.ps1`, `.bat` hoặc bất kỳ tệp nào (bộ lọc "Tất cả tệp").
4. Tùy chọn **"Kích hoạt chữ ký mới ngay (IsActive = 1)" được chọn mặc định**; chữ ký mới sẽ được lưu vào `VirusSignatures` với `IsActive = 1` sau khi quản trị viên xác nhận cảnh báo.
5. **Chỉ giữ tùy chọn này khi các mẫu đã được xác minh là mã độc.** Nếu chưa xác minh, bỏ chọn để lưu mẫu mới với `IsActive = 0` và hành động `Review`, tránh phát hiện và cách ly nhầm tệp bình thường.
6. Nhấn **Kiểm tra và lưu**, theo dõi thống kê số chữ ký thêm mới, đã có, lỗi, bỏ qua. Nhấn **Hủy xử lý** nếu cần.

## Quy tắc nhập và cấu trúc dữ liệu

| Cột dbo.VirusSignatures | Nội dung |
|---|---|
| MalwareName | `UserSuspected.<tên tệp>` hoặc `UserVerified.<tên tệp>` (đây là nhãn nhập mẫu, không phải tên chủng mã độc đã xác định) |
| MalwareFamily | `NULL` (chưa có cơ sở để xác định họ) |
| Category | `Suspected` hoặc `UserVerified` |
| SignatureType | `Hash` |
| MD5 / SHA1 / SHA256 | Tính từ chính các byte của tệp (viết hoa hexadecimal) |
| FileExtension | Phần mở rộng tệp |
| Severity | `Medium` (nhãn mặc định, không phải nhận định nguy hiểm từ nguồn uy tín) |
| Description | Nguồn nhập, số byte, trạng thái xác minh |
| RecommendedAction | `Review` nếu chưa xác minh, `Quarantine` nếu đã xác minh |
| IsActive | Mặc định `1` khi tùy chọn kích hoạt được chọn; `0` nếu quản trị viên chủ động bỏ chọn |

Các bước trùng lặp: (1) SHA-256 trùng trong bảng: **bỏ qua**, kể cả bản ghi đang tắt. (2) Nếu bản ghi cũ không có SHA-256 nhưng trùng MD5 hoặc SHA-1: **bỏ qua**. Một giao dịch SQL `SERIALIZABLE` riêng cho mỗi mẫu giúp hạn chế nhập trùng khi nhiều phiên chạy đồng thời. Dữ liệu cũ không bị sửa.

**ZIP:** Hash tệp ZIP và tất cả tệp trực tiếp bên trong, mỗi entry là một chữ ký riêng. Không giải nén tệp ra ổ đĩa hoặc chạy lệnh/chương trình; ZIP nằm trong ZIP sẽ được xem là tệp nhị phân, **không quét lồng nhau**. Để tránh ZIP bomb: tối đa 2.000 entry/ZIP, 512 MiB/entry, tổng 2 GiB dữ liệu giải nén/ZIP, và 500 tệp được chọn/lượt. Tệp độc lập tới 8 GiB được hash streaming; quá giới hạn sẽ được báo và bỏ qua.

**Chữ ký đã kích hoạt:** cache phát hiện hash sẽ được làm mới sau khi chèn các mẫu có `IsActive = 1`. Lưu ý các mẫu này sẽ tham gia phát hiện hash ở những lần quét sau và có thể bị đề xuất cách ly; chỉ kích hoạt mẫu đã xác minh. Những mẫu bỏ chọn sẽ có `IsActive = 0`, không tham gia engine quét.

**Riêng biệt với Internet:** Popup này chỉ nhập **metadata/hash vào SQL Server local**. Không tải mẫu lên MalwareBazaar hay VirusTotal. Tuy nhiên công tắc `SubmitSamples` vốn có trong ứng dụng có thể điều khiển hành vi gửi VirusTotal ở những tính năng khác; nếu không muốn gửi tệp ra ngoài, không bật công tắc đó. Chỉ cần nhấn vào **thẻ nội dung** để nhập hash cục bộ.

## SQL kiểm tra sau khi nhập

```sql
USE AntivirusDB;
GO
SELECT TOP (100)
    SignatureID, MalwareName, SignatureType, MD5, SHA1, SHA256,
    FileExtension, Category, IsActive, CreatedAt
FROM dbo.VirusSignatures
WHERE Category IN (N'Suspected', N'UserVerified')
ORDER BY SignatureID DESC;
```

## Kiểm thử an toàn

Thư mục `Tests/SampleSubmissionFixtures` có hai file văn bản, một script JS chỉ chứa `console.log` và ZIP có hai entry vô hại. Chọn `sample-not-malware.zip` để kiểm tra nhập ZIP và từng entry; nhập lại lần 2 phải báo **đã tồn tại**. Hash nằm trong `expected_hashes.csv`. Với các fixture vô hại này, **bỏ chọn** ô kích hoạt (`IsActive = 1`) trước khi nhập để không tạo chữ ký phát hiện nhầm.

**Giới hạn kiểm thử tại đây:** Chỉ rà soát tĩnh mã C# và cấu trúc ZIP; để xác nhận vận hành thật, cần Build dự án và kết nối SQL Server trên Windows.
