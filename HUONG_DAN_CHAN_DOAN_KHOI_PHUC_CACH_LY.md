# Chẩn đoán lỗi khôi phục tệp cách ly

## Chức năng đã cập nhật

- Khi nhấn **Khôi phục** hoặc **Khôi phục tất cả**, ứng dụng hiển thị lý do thất bại của từng tệp, thay vì chỉ hiện số lượng không thể xử lý.
- Trước khi khôi phục, đọc `OriginalPath`, `QuarantinePath` và `Status` từ `ThreatDetections`.
- Nếu không tìm thấy `.qtn`, hiển thị đường dẫn thiếu và đề nghị kiểm tra Windows Security > Virus & threat protection > Protection history.
- Nếu ổ gốc không còn kết nối (ví dụ `Z:`), thiếu quyền ghi, lỗi I/O hoặc SQL Server, thông báo nguyên nhân tương ứng.
- Sau khi di chuyển tệp, kiểm tra đường dẫn đích tồn tại trước khi cập nhật `Status = 'Restored'`.
- Nếu cập nhật SQL không thành công, cố gắng đưa tệp trở lại kho cách ly; nếu rollback cũng lỗi, thông báo rõ để đối chiếu thủ công.

## Kiểm tra hiện trạng SQL

```sql
USE AntivirusDB;
SELECT DetectionID, FileName, OriginalPath, QuarantinePath, Status,
       ActionTaken, RestoredAt
FROM dbo.ThreatDetections
WHERE Status = 'Quarantined'
ORDER BY DetectedAt DESC;
```

Đối chiếu `QuarantinePath` trong File Explorer: file vật lý có thực sự ở đó không? Ổ chứa `OriginalPath` có còn kết nối và có quyền ghi không?

## Trường hợp EICAR

EICAR là mẫu thử antivirus. Microsoft Defender có thể cách ly tệp EICAR ngay sau khi AntivirusWinform đưa tệp về vị trí cũ. Khi thấy thông báo của Windows Security, mở **Windows Security > Virus & threat protection > Protection history** để kiểm tra bản ghi và thời điểm xử lý. Không tắt bảo vệ hoặc tạo ngoại lệ diện rộng chỉ để thử chức năng khôi phục.

Để kiểm thử thành công luồng *cách ly → khôi phục* mà không bị Defender can thiệp, hãy sử dụng tệp văn bản vô hại có hash thử nghiệm do chính bạn nạp vào cơ sở dữ liệu `VirusSignatures`, đảm bảo thử trong môi trường thí nghiệm và chỉ dùng chữ ký thử nghiệm.

## Lưu ý

Cơ chế kiểm tra `File.Exists()` chỉ xác nhận tệp còn tồn tại ở thời điểm kiểm tra. Phần mềm bảo vệ khác vẫn có thể xử lý tệp sau khi thao tác khôi phục đã được ghi nhận. Bộ kiểm thử này chưa được chạy tích hợp với Windows Defender và SQL Server trong môi trường phát triển của ChatGPT.
