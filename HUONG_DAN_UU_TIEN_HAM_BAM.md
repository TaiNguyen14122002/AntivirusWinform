# Quy tắc ưu tiên phát hiện mã độc bằng hàm băm

Phần mềm chỉ có **hai phương pháp băm** để đối chiếu chữ ký đang hoạt động:

1. **Hash toàn tệp trước**. Đọc tệp theo luồng, tính những hàm băm có chữ ký trong `dbo.VirusSignatures` (ưu tiên SHA-256, rồi SHA-1, rồi MD5). Nếu trùng hash của chữ ký đã kiểm chứng (`IsActive=1`) → **phát hiện ngay**, lưu tên mã độc và `SignatureID` → **không quét CDC cho tệp đó**.
2. **Chỉ khi không trùng hash toàn tệp**, nếu có nhóm chữ ký CDC hợp lệ, chuyển về đầu tệp rồi chạy Sliding Window + Rolling Hash để xác định điểm cắt; tính SHA-256 các chunk và so sánh các nhóm `ChunkSignatureGroups` / `ChunkSignatures`.
3. Nếu cả hai phương pháp không khớp: kết quả là **chưa phát hiện theo cơ sở dữ liệu hiện tại**, không có nghĩa bảo đảm tệp vô hại.

**Lưu ý:** Không có chữ ký hoạt động hoặc lỗi nạp chữ ký SQL sẽ làm giảm khả năng phát hiện. Bản thân tên tệp hoặc chuỗi EICAR không còn là quy tắc phát hiện độc lập; đối với EICAR, cần đúng hash của **nội dung tệp thực tế** đã lưu trong SQL.

## Kiểm thử trên Windows

Mở Developer Command Prompt for Visual Studio tại thư mục chứa `ScanAndRemoveVirus.sln` rồi chạy:

```cmd
msbuild Tests\HybridHashSelfTest.csproj /p:Configuration=Release
Tests\bin\HybridHashSelfTest.exe
```

Bộ test không dùng SQL, không dùng mã độc thật. Kiểm tra: SHA-256/SHA-1/MD5 được ưu tiên ngay cả khi CDC cũng khớp; khi hash toàn tệp thay đổi thì nhánh CDC được gọi; dữ liệu không liên quan thì không phát hiện.

Để kiểm tra chữ ký thực trong SQL Server, chạy `KIEM_TRA_ENGINE_SQL.cmd` và kiểm tra số chữ ký đang hoạt động; ứng dụng đang dùng cấu hình kết nối ở `Database/DbConnection.cs`.
