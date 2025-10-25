# ĐỀ XUẤT BỔ SUNG VÀ HOÀN THIỆN CHỨC NĂNG PHẦN MỀM QUÉT VÀ DIỆT VIRUS

> **Dự án:** ScanAndRemoveVirus (Antivirus WinForms)  
> **Nền tảng:** C# WinForms, .NET Framework 4.7.2, SQL Server / cơ sở dữ liệu `AntivirusDB`  
> **Nguồn đối chiếu:** `AntivirusWinform-TaiNguyen(1).zip` do người phát triển cung cấp  
> **Ngày lập:** 21/10/2026  
> **Cập nhật:** Bổ sung **giải pháp kỹ thuật triển khai chi tiết F01–F14**: thuật toán, kiến trúc C#, SQL, luồng dữ liệu, xử lý lỗi, kiểm thử.  
> **Mục đích:** Tài liệu yêu cầu nâng cấp để triển khai bằng Visual Studio, phân công, kiểm thử và đưa vào báo cáo đồ án.

---

## 1. TỔNG QUAN VÀ PHẠM VI

### 1.1. Mục tiêu nâng cấp

Phát triển phần mềm từ công cụ có khả năng quét, nhận diện và cách ly mã độc dựa trên chữ ký thành một hệ thống bảo vệ máy tính có độ tin cậy cao hơn. Trọng tâm là **quét đúng từng tệp**, **phát hiện được mẫu ẩn trong tệp nén**, **giảm nhận diện nhầm**, **không đánh đồng “không thể quét” với “an toàn”**, **không làm mất dữ liệu khi cách ly/khôi phục**, **giám sát liên tục ở phạm vi đã cấu hình** và **cung cấp kết quả dễ hiểu**.

Tài liệu này đề xuất bổ sung chức năng và sửa lỗi tiềm ẩn; **không khẳng định các lỗi đã tái hiện được**. Những nhận xét về trạng thái hiện tại dựa trên đọc tĩnh mã nguồn. Chưa thực hiện kiểm thử chạy phần mềm trên môi trường Windows/SQL Server thật.

### 1.2. Những chức năng đã có — không viết lại từ đầu

- Quét nhanh, quét toàn bộ, quét thư mục, quét tệp; hỗ trợ danh sách tệp/thư mục và duyệt đệ quy.
- So sánh MD5, SHA-1, SHA-256; quét Chunk Hash/CDC bằng Sliding Window và Rolling Hash.
- Phát hiện nghi vấn theo heuristic tĩnh; giám sát hành vi cơ bản, tiến trình/khởi động, USB, tệp tải xuống.
- Bảo vệ thời gian thực dựa trên `FileSystemWatcher` tại các thư mục theo dõi.
- Cách ly, khôi phục, xóa tệp; lịch sử quét; giao diện WinForms; tra cứu VirusTotal.
- Cập nhật chữ ký qua MalwareBazaar và nhập danh sách chữ ký hoặc mẫu nghi ngờ.

**Lưu ý về cách gọi chức năng:** “Bảo vệ thời gian thực” bằng `FileSystemWatcher` là giám sát thay đổi tệp trong những phạm vi được chọn, **không đồng nghĩa** với khả năng chặn mọi thao tác đọc/mở/chạy tệp ở mức nhân hệ điều hành. “Bảo vệ ransomware” hiện mới có thành phần nhận biết hành vi; chưa nên giới thiệu là có thể ngăn chặn hoặc phục hồi toàn diện.

### 1.3. Phân loại mức ưu tiên

| Mức | Ý nghĩa | Yêu cầu hoàn thành |
|---|---|---|
| **P0** | Cần thực hiện trước khi mở rộng tính năng | Tác động trực tiếp đến tính đúng đắn, an toàn dữ liệu hoặc khả năng phát hiện |
| **P1** | Nên có trong phiên bản nâng cấp chính | Cải thiện bảo vệ thực tế, tự động hóa và trải nghiệm |
| **P2** | Phát triển chuyên sâu/giai đoạn sau | Phức tạp hơn, cần kiểm thử và dữ liệu phù hợp |

### 1.4. Bảng tổng hợp

| Mã | Chức năng | Tình trạng nhận xét từ mã nguồn | Ưu tiên |
|---|---|---|---|
| F01 | Quét tệp bên trong ZIP, RAR, 7z | Chưa thấy triển khai trong luồng quét chính | P0 |
| F02 | Xác minh, duyệt và quản lý chữ ký | Có nhập chữ ký; mặc định kích hoạt mẫu mới dễ gây dương tính giả | P0 |
| F03 | Chế độ dự phòng khi SQL mất kết nối | Có cảnh báo lỗi nạp chữ ký; chưa thấy snapshot chữ ký bền vững ngoại tuyến | P0 |
| F04 | Tăng độ tin cậy của bảo vệ thời gian thực | Có watcher; cần xử lý lỗi/mất sự kiện và quét bù | P0 |
| F05 | Tăng an toàn cách ly/khôi phục | Đã có; cần thống nhất kiểm tra, xác minh tệp và nhật ký giao dịch | P0 |
| F06 | Theo dõi tệp bỏ sót/lỗi và độ bao phủ quét | Đã đếm một số lỗi; cần giải thích rõ tệp nào chưa được quét | P0 |
| F07 | Quét tự động theo lịch | Chưa thấy bộ lập lịch quét độc lập | P1 |
| F08 | Bảo vệ ransomware theo mức cảnh báo | Có giám sát hành vi; chưa đầy đủ cơ chế phòng ngừa/khôi phục | P1 |
| F09 | Danh sách ngoại lệ có kiểm soát | Chưa thấy module quản lý ngoại lệ hoàn chỉnh | P1 |
| F10 | Kiểm tra tình trạng bảo mật Windows | Chưa thấy bảng kiểm tổng hợp | P1 |
| F11 | Báo cáo/nhật ký và xuất kết quả | Có lịch sử và xuất CSV; cần nâng cấp chi tiết và tính kiểm toán | P1 |
| F12 | Kiểm tra chữ ký số và metadata PE | Chưa thấy thành phần đánh giá chữ ký Authenticode | P2 |
| F13 | Bộ luật YARA tùy chọn | Không nằm trong luồng phát hiện hiện tại | P2 |
| F14 | Công cụ kiểm thử, đánh giá chất lượng phát hiện | Có nhiều self-test; cần bộ đánh giá độ chính xác/hiệu năng đầy đủ | P1 |

---

## 2. YÊU CẦU CHỨC NĂNG VÀ GIẢI PHÁP TRIỂN KHAI CHI TIẾT

## F01. QUÉT MÃ ĐỘC BÊN TRONG TỆP NÉN — P0

### Mục đích

Khi người dùng chọn một tệp nén hoặc chọn thư mục chứa tệp nén, phần mềm phải kiểm tra **từng tệp thành phần** thay vì chỉ tính hash của tệp nén như một đối tượng duy nhất. Kết quả phải xác định được **tệp nào bên trong** có mã độc/đáng nghi.

### Phạm vi

1. Giai đoạn đầu: hỗ trợ `.zip` bằng thư viện ZIP phù hợp với .NET Framework 4.7.2.
2. Giai đoạn mở rộng: `.7z`, `.rar`, `.tar`, `.gz` bằng thư viện tương thích, được kiểm định và duy trì cập nhật.
3. Hỗ trợ tệp nén lồng nhau trong giới hạn đã cấu hình.
4. Áp dụng hash toàn tệp, các chữ ký chunk tương thích và heuristic lên **nội dung tệp con**.
5. Không giải nén rồi thực thi bất kỳ mã nào; không ghi tệp con vào đường dẫn bất kỳ do tên entry quyết định.

### Luồng xử lý đề xuất

```text
Người dùng chọn tệp/thư mục
  -> Duyệt từng tệp vật lý
      -> Tệp thông thường: chạy bộ quét hiện tại
      -> Tệp nén được hỗ trợ:
          -> Đọc danh sách entry
          -> Kiểm tra giới hạn tài nguyên
          -> Với từng entry là tệp: quét luồng dữ liệu của entry
          -> Nếu entry là tệp nén: quét đệ quy có giới hạn
          -> Ghi kết quả từng entry, không gom thành “nhiễm cả thư mục”
      -> Tổng hợp: đã quét / phát hiện / nghi vấn / không thể kiểm tra
```

**Ví dụ kết quả:**

```text
TaiLieu.zip
├── BaoCao.pdf                  : Chưa phát hiện mã độc
├── PhanMem/setup.exe           : Phát hiện theo SHA-256
├── Script/update.js            : Nghi vấn heuristic
└── Backup/co-mat-khau.7z       : Chưa thể kiểm tra (yêu cầu mật khẩu)
```

### Yêu cầu an toàn bắt buộc

- Giới hạn độ sâu nén lồng nhau, số entry, kích thước giải nén của từng entry, tổng kích thước giải nén và thời gian/CPU tiêu thụ. Giá trị ban đầu do người phát triển cấu hình và đo kiểm, **không được coi là ngưỡng an toàn tuyệt đối**.
- Chống **Zip Slip**: không cho tên entry như `../../...` thoát ra khỏi thư mục sandbox.
- Chống ZIP bomb, tên entry lặp, symlink bất thường, luồng không giải nén được, CRC hỏng, tệp nén hỏng và thao tác hủy giữa chừng.
- Không tự suy diễn “sạch” khi entry bị mã hóa, cần mật khẩu, vượt giới hạn hoặc không đọc được; hiển thị “Chưa được kiểm tra”.
- Nếu phát hiện tệp con độc hại, hiển thị đường dẫn dạng `C:\...\TaiLieu.zip > PhanMem/setup.exe`; xác định rõ hành động áp dụng cho **toàn bộ tệp nén** hay tạo bản sao tệp con trong vùng cách ly được quản lý. Không tự sửa nội dung archive khi chưa có quy trình an toàn.

### Giao diện

Trong màn kết quả quét, bổ sung nút mở rộng bên cạnh tệp nén, hiển thị tên entry, kích thước, mức độ, phương pháp phát hiện và trạng thái. Có bộ lọc “Chỉ hiện tệp nguy hiểm”, “Tệp chưa kiểm tra” và tùy chọn “Sao chép đường dẫn bên trong tệp nén”.

### Mã nguồn dự kiến

- Sửa `Services/ScanEngine.cs` và `Services/HybridSignatureScanner.cs` theo hướng hỗ trợ đọc `Stream` (không bắt buộc giải nén ra đĩa).
- Thêm `Services/ArchiveScanService.cs`, `Services/ArchiveScanLimits.cs` và mô hình `ScanItemResult`.
- Sửa `Control/UcTongQuan.ChiTiet.cs` hoặc phần danh sách kết quả tương đương.

### Tiêu chí nghiệm thu

- [ ] Một ZIP chứa 100 tệp, trong đó một tệp có hash đã biết, kết quả nêu đúng entry bị phát hiện.
- [ ] Quét được tệp nén nằm trong thư mục con; hiển thị đường dẫn ảo đầy đủ.
- [ ] ZIP lồng ZIP chạy đúng trong giới hạn; trường hợp vượt giới hạn được ghi “Chưa kiểm tra”.
- [ ] Không thể đọc/mật khẩu/CRC lỗi không được hiện “An toàn”.
- [ ] Kiểm thử Zip Slip và ZIP bomb bằng bộ dữ liệu thử nghiệm vô hại.
- [ ] Hủy quét giữa chừng giải phóng tài nguyên và không để lại dữ liệu tạm.

### Giải pháp kỹ thuật triển khai F01

**Giải pháp ưu tiên: tạo lớp quét archive dùng chung, tái sử dụng công cụ hash/CDC/heuristic hiện tại.** Không viết lại thuật toán phát hiện và không coi file `.zip` là một thư mục vật lý. Phần mềm phải tạo đối tượng `ScanTarget` chứa `PhysicalPath`, `EntryPath`, `Depth`, `Length`, `IsArchiveEntry`; `EntryPath` là đường dẫn ảo để hiển thị, ví dụ `D:\Data\a.zip > docs/b.exe`.

**Các bước thực hiện:**

1. Tại `ScanEngine.InspectOneFile(...)`, nhận diện định dạng bằng **magic bytes + phần mở rộng**. Với ZIP, gọi `ArchiveScanService.ScanZipAsync(...)`; với tệp thường giữ nguyên nhánh `Evaluate(...)`. Định dạng chưa hỗ trợ trả `UnsupportedArchiveContent`, không đánh dấu đã kiểm tra nội dung.
2. Dùng `System.IO.Compression.ZipArchive` (có trong hệ sinh thái .NET Framework được dự án dùng) để liệt kê `ZipArchiveEntry`, bỏ qua entry thư mục. Dùng `entry.Open()` để đọc dữ liệu; không chạy tệp, không cho tên entry tự xác định đường dẫn ghi. Với RAR/7z nghiên cứu thêm **SharpCompress bản tương thích với phiên bản .NET và giấy phép** sau khi hoàn tất ZIP.
3. Hiện tại `HybridSignatureScanner.ScanFile(...)` và `StaticHeuristicAnalyzer.Analyze(...)` nhận đường dẫn. Có hai lựa chọn: **(A)** refactor chúng nhận `Stream`/`ScanInput` và xử lý stream có khả năng tua lại; **(B)** giai đoạn đầu, giải nén **từng entry một** vào thư mục tạm riêng của chương trình (tên GUID, ACL hạn chế, không thực thi), quét bằng hàm cũ rồi xóa trong `finally`. Ưu tiên B để tích hợp sớm, sau đó mới tối ưu A. Tránh đọc toàn bộ entry vào `byte[]`.
4. Bộ điều phối `ArchiveScanBudget` theo dõi tổng số entry, tổng byte **thực đã giải nén**, byte mỗi entry, độ sâu lồng, thời gian và dung lượng tạm. Chặn ngay khi vượt giới hạn, kể cả header khai báo kích thước giả. Giá trị khởi điểm để **thử nghiệm**, không phải ngưỡng bảo đảm an toàn: sâu tối đa 3; 5.000 entry/archive; 512 MiB/entry; 2 GiB/tổng; kiểm soát cả tỷ lệ nén cực đoan. Cho cấu hình thấp hơn nếu máy ít tài nguyên.
5. Khi phát hiện entry đáng ngờ, lưu `PhysicalPath`, `ArchiveEntryPath`, `DetectionMethod`, `Status`, `Reason`. Nếu người dùng chọn cách ly, **mặc định cách ly archive gốc** sau khi xác nhận, đồng thời nói rõ entry bị phát hiện; không tự mở/sửa rồi ghi lại ZIP.
6. ZIP có mật khẩu, bị lỗi CRC, archive hỏng, format không hỗ trợ hoặc bị giới hạn phải được ghi vào `ScanFileIssues` với trạng thái `Unreadable`/`Unsupported`/`LimitExceeded`. Đếm riêng các entry đã phân tích thành công và các archive bị bỏ sót nội dung.

**Luồng giả mã:**

```text
ScanPhysicalFile(path):
    if not IsSupportedArchive(path): return ScanRegularFile(path)
    return ScanArchive(path, depth=0, budget)
ScanArchive(container, depth, budget):
    kiểm tra budget, mở archive chế độ chỉ đọc
    for each entry:
        kiểm tra hủy + giới hạn bằng số byte thực đã đọc
        if entry là thư mục: bỏ qua
        if entry là archive và depth còn cho phép: ScanArchive(entry, depth+1, budget)
        else: quét hash -> CDC -> heuristic trên nội dung entry
        ghi trạng thái riêng cho từng entry (Detected / Suspicious / Clean / Unscanned)
```

**Tệp đề nghị:** `Services/ArchiveScanService.cs`, `Services/ArchiveScanBudget.cs`, `Services/ScanTarget.cs`, sửa `Services/ScanEngine.cs`, `Control/UcTongQuan.ChiTiet.cs`; dùng `ScanFileIssues` ở F06. **Kiểm thử riêng:** ZIP 1.000 entry, archive lồng 4 cấp, mật khẩu, hỏng CRC, Zip Slip, thay đổi header kích thước và hủy giữa quá trình giải nén. Không dùng archive độc hại thực trên máy làm việc.

---

## F02. QUẢN LÝ CHỮ KÝ VÀ QUY TRÌNH XÁC MINH — P0

### Vấn đề cần xử lý

Trong `Control/SampleSubmissionForm.cs`, lựa chọn **“Kích hoạt chữ ký mới ngay (IsActive = 1)”** đang mặc định được chọn (`Checked = true`). Một tệp sạch được nhập nhầm và xác nhận là mã độc có thể tạo chữ ký hoạt động, dẫn tới nhận diện nhầm trong các lần quét tiếp theo.

### Yêu cầu

1. Mẫu người dùng tự gửi **mặc định `IsActive = 0`** và ở trạng thái `PendingReview`.
2. Chữ ký lấy từ nguồn đáng tin cậy phải lưu nguồn gốc và cách xác minh; không coi “có mặt trong cơ sở dữ liệu” đồng nghĩa “đã được thẩm định độc lập”.
3. Kiểm tra trùng MD5/SHA-1/SHA-256 trước khi thêm; phân biệt *trùng hash cùng mẫu* với *xung đột tên/thông tin*.
4. Có chức năng duyệt/từ chối/vô hiệu hóa/kích hoạt lại chữ ký, tìm kiếm theo tên họ mã độc, loại, SHA-256, nguồn và mức độ.
5. Không tự kích hoạt chữ ký chỉ dựa trên một kết quả heuristic hoặc một số lượng nhỏ cảnh báo VirusTotal.
6. Lưu lịch sử thay đổi `IsActive`, người thực hiện, thời điểm và lý do; cho phép hoàn tác thay đổi có kiểm soát.
7. Mỗi lần sửa chữ ký, làm mới snapshot phát hiện an toàn sau khi giao dịch DB thành công.

### Giao diện đề xuất

**Cài đặt → Dữ liệu & công cụ → Quản lý chữ ký** gồm các tab:

- **Đang hoạt động:** tên mã độc, họ mã độc, loại, hash, mức độ, nguồn, ngày cập nhật.
- **Chờ xác minh:** xem thông tin và bằng chứng, chọn “Duyệt”, “Từ chối”, “Yêu cầu kiểm tra thêm”.
- **Bị vô hiệu hóa:** lý do vô hiệu hóa và lịch sử quyết định.
- **Lịch sử chỉnh sửa:** thời gian, hành động, người thực hiện, giá trị trước/sau.

### Mã nguồn và dữ liệu

- Sửa `Control/SampleSubmissionForm.cs` để bỏ bật `IsActive` mặc định.
- Sửa `Services/SuspiciousSampleImporter.cs`, `Services/SubmittedSignatureListImporter.cs`, `Services/MalwareBazaarSignatureUpdater.cs` để gắn nguồn và chính sách tin cậy.
- Mở rộng `VirusSignatures`: `SourceName`, `VerificationStatus`, `VerifiedAt`, `VerifiedBy`.
- Thêm `SignatureAuditLogs`, hoặc bảng tương đương, phục vụ kiểm toán.

### Tiêu chí nghiệm thu

- [ ] Tệp sạch người dùng tải lên không mặc định trở thành chữ ký phát hiện.
- [ ] Tệp trùng SHA-256 không bị thêm thành bản ghi hoạt động thứ hai mà không xử lý xung đột.
- [ ] Bản ghi chưa duyệt không tham gia đánh giá mẫu độc trong chế độ nghiêm ngặt.
- [ ] Nhật ký ghi được người thao tác, lý do, thời gian và trạng thái trước/sau.
- [ ] Có kế hoạch rà soát dữ liệu cũ trước khi thay đổi điều kiện lọc chữ ký đang hoạt động.

### Giải pháp kỹ thuật triển khai F02

**Giải pháp ưu tiên: tách việc “thu nhận dữ liệu” khỏi việc “cho phép tham gia phát hiện”.** Chữ ký mới không tự động là chữ ký đã duyệt.

1. Sửa `Control/SampleSubmissionForm.cs`: thuộc tính `Checked = false`, đổi lời nhắc thành **“Mẫu mới được lưu ở trạng thái chờ xác minh (không dùng để kết luận mã độc)”**. Importer phải buộc `IsActive = 0` và `VerificationStatus = 'PendingReview'` với mẫu tự gửi, dù người dùng bỏ qua giao diện.
2. Ở `Services/SuspiciousSampleImporter.cs` và `SubmittedSignatureListImporter.cs`, chuẩn hóa hash về chữ thường, đúng độ dài/hex, loại khoảng trắng, đồng thời chuẩn hóa nguồn. Tra cứu lần lượt SHA-256, SHA-1, MD5 bằng câu lệnh tham số hóa; phát hiện trùng thì **đối chiếu thông tin** chứ không cập nhật đè tên và mức nguy hiểm một cách âm thầm.
3. Thêm `SignatureVerificationService` với các thao tác `Submit`, `Review`, `Approve`, `Reject`, `Disable`, `Revoke`. Khi duyệt, yêu cầu ít nhất nguồn xác minh, bằng chứng, người duyệt, ngày duyệt; nguyên tắc xét duyệt có thể phân biệt MalwareBazaar (đã có thông tin nguồn) với mẫu người dùng tự chọn. Chữ ký nhập từ nguồn ngoài vẫn phải kiểm tra chất lượng, không tự coi mọi nguồn là tuyệt đối đáng tin.
4. `VirusSignatureRepository` cập nhật `VerificationStatus` và `IsActive` **trong cùng transaction** với một bản ghi `SignatureAuditLogs`, sử dụng câu SQL tham số hóa. Chỉ gọi `HybridSignatureScanner.Invalidate()` sau khi commit thành công. Nếu commit lỗi, không áp dụng thay đổi trong bộ nhớ.
5. Khi lấy chữ ký phát hiện, tối thiểu lọc `IsActive = 1`, đồng thời xác định chính sách đối với bản ghi di sản `Legacy` sau khi rà soát. Không thay ngay bằng `VerificationStatus = 'Verified'` khi chưa chuyển đổi chữ ký cũ, vì có thể làm phần mềm mất toàn bộ khả năng phát hiện hiện hành.
6. Trước khi tạo unique index trên các cột hash, chạy truy vấn tìm các nhóm trùng và giải quyết xung đột dữ liệu cũ. Có thể tạo chỉ mục filtered cho hash không null **sau khi** dữ liệu đã được làm sạch.

**Luồng xét duyệt:** `Nhập mẫu -> PendingReview/IsActive=0 -> Tra cứu bằng chứng -> Duyệt hoặc từ chối -> Transaction SQL + Audit -> Invalidate cache -> Dùng ở lần quét sau`.

**Quy tắc an toàn:** không kích hoạt chỉ vì heuristic vượt ngưỡng, không coi `0 detection` trên VirusTotal là bằng chứng sạch và không cho bản ghi bị từ chối được tái bật bằng thao tác import trùng. **Tệp đề nghị:** `Services/SignatureVerificationService.cs`, `Database/SignatureAuditRepository.cs`, `Control/SignatureManagementForm.cs`. **Kiểm thử:** import lặp 2 lần; 2 người duyệt đồng thời; SQL lỗi khi lưu audit; sửa một chữ ký đang được quét; dữ liệu cũ `Legacy`.

---

## F03. CHẾ ĐỘ DỰ PHÒNG KHI KHÔNG KẾT NỐI SQL SERVER — P0

### Hiện trạng

`HybridSignatureScanner.GetSnapshot(true)` cố tải lại tập chữ ký từ SQL Server và trả về `Snapshot.Error` khi lỗi. Bộ quét vẫn có thể chạy heuristic trong tình huống không tải được chữ ký; điều này **không tương đương với quét đầy đủ bằng tập chữ ký**. Việc cách ly hiện cũng cần tạo/cập nhật `ThreatDetections` trong SQL Server.

### Yêu cầu

- Sau lần cập nhật chữ ký thành công, lưu **snapshot chỉ đọc đã xác minh** vào thư mục dữ liệu ứng dụng với phiên bản, thời điểm tạo, checksum/chữ ký bảo vệ tính toàn vẹn.
- Khi SQL không truy cập được, thử nạp snapshot gần nhất **chỉ khi hợp lệ**; nếu không có thì vẫn chạy lớp có thể hoạt động nhưng phải báo khả năng phát hiện bị suy giảm.
- Tạo hàng đợi sự kiện/quét/cách ly cục bộ để đồng bộ sau khi kết nối được khôi phục; sử dụng ID cục bộ không xung đột với `DetectionID` của SQL.
- Ghi rõ `SignatureSource = SQL | OfflineSnapshot | Unavailable`, phiên bản chữ ký và thời điểm cập nhật trong lịch sử từng phiên quét.
- Thử kết nối lại có giới hạn và tăng dần khoảng chờ; không khóa giao diện trong lúc kết nối.
- Không tự động coi một lần quét không lỗi I/O là “máy an toàn” nếu không nạp được chữ ký.

### Thiết kế trạng thái

| Trạng thái | Ý nghĩa | Giao diện |
|---|---|---|
| `Normal` | Có SQL, chữ ký hoạt động và các lớp quét sẵn sàng | “Bảo vệ đang hoạt động” |
| `Degraded` | Dùng chữ ký ngoại tuyến hoặc thiếu một phương pháp phát hiện | “Bảo vệ hạn chế — cần kiểm tra kết nối” |
| `Unavailable` | Không có chữ ký tin cậy hoặc không thể chạy quét | “Không thể xác nhận mức độ an toàn” |

### Tiêu chí nghiệm thu

- [ ] Mất SQL sau khi đã lưu snapshot: quét vẫn dùng tập chữ ký hợp lệ cũ và hiển thị rõ phiên bản.
- [ ] Không có snapshot: không hiển thị kết luận “An toàn” chung chung.
- [ ] Có thể cách ly an toàn trong chế độ offline **sau khi đã triển khai đầy đủ kho metadata cục bộ**, rồi đồng bộ không tạo bản ghi trùng.
- [ ] Snapshot sai checksum hoặc bị thay thế phải bị từ chối.
- [ ] Khôi phục kết nối không làm mất lịch sử hoặc tệp cách ly đang chờ đồng bộ.

### Giải pháp kỹ thuật triển khai F03

**Giải pháp ưu tiên: “SQL chính + snapshot chữ ký chỉ đọc + hàng đợi nhật ký offline”; triển khai theo 2 giai đoạn để không gây thất lạc tệp.** SQL Server tiếp tục là nguồn dữ liệu chính, không thay thế cơ sở dữ liệu hiện có.

**Giai đoạn A — Giữ chức năng phát hiện khi mất SQL:**

1. Sau khi tải chữ ký thành công từ `HybridSignatureScanner.LoadFromDatabase()`, tuần tự hóa **đúng các trường cần quét** (hash, chữ ký chunk, cấu hình CDC, phiên bản) vào snapshot, có `SchemaVersion`, `CreatedAtUtc`, `RecordCount` và mã kiểm tra tính toàn vẹn.
2. Không chỉ dùng SHA-256 của chính snapshot để chống giả mạo (vì người sửa file có thể tính lại SHA). Với ứng dụng cá nhân chạy Windows, có thể dùng `System.Security.Cryptography.ProtectedData` (DPAPI, `DataProtectionScope.CurrentUser` hoặc `LocalMachine` theo tài khoản chạy service) để bảo vệ payload và phát hiện sửa đổi; hạn chế quyền NTFS của thư mục lưu. Nếu dùng HMAC, khóa phải được bảo vệ độc lập bằng DPAPI/ACL, không ghi rõ khóa trong mã nguồn. DPAPI không bảo vệ khi đối phương đã chiếm quyền tài khoản tương ứng.
3. Ghi snapshot bằng file tạm, flush rồi thay thế nguyên tử trong cùng volume; giữ snapshot trước đó để dự phòng. Dùng snapshot bất biến và xác minh header/phiên bản/chữ ký trước khi nạp; nếu không hợp lệ thì từ chối.
4. Khi bắt đầu quét, ưu tiên snapshot nạp từ SQL; nếu lỗi, thử snapshot offline. Ghi `SignatureSource`, phiên bản và cờ `IsDegraded` vào kết quả. Chữ ký quá cũ phải hiển thị ngày cập nhật; không ghi trạng thái “bảo vệ đầy đủ”.

**Giai đoạn B — Nhật ký và cách ly offline:**

5. Dùng **SQLite cục bộ** (cần chọn package tương thích .NET Framework, x86/x64) hoặc sổ sự kiện cục bộ có transaction để lưu `LocalOperationId` (GUID), `OperationType`, `Payload`, `State`, `CreatedAtUtc` và retry count. Trường hợp cách ly, metadata cục bộ phải commit/flush **trước khi chuyển file**; có đối soát lại bằng hash và đường dẫn kho cách ly.
6. Bộ đồng bộ nền gửi sự kiện theo thứ tự, có khóa idempotency `LocalOperationId` trên máy chủ để gửi lại không trùng. **Không** lấy `DetectionID` SQL làm tên file offline khi chưa có bản ghi SQL; dùng `LocalQuarantineId` riêng và lập ánh xạ sau đồng bộ.
7. SQL được khôi phục: đồng bộ theo transaction, xác nhận bản ghi tương ứng rồi đánh dấu hàng đợi hoàn thành; vẫn giữ báo cáo chi tiết nếu phát sinh xung đột. Cấm xóa queue chỉ vì một lần cập nhật SQL bị timeout.

**Ví dụ trạng thái:** `OnlineFull`, `OfflineVerifiedSnapshot`, `NoTrustedSignatures`, `LocalQueuePending`, `SyncConflict`. Trong UI chỉ có thể báo “Đã quét, chưa phát hiện bằng các phương pháp hiện có” nếu có thiếu chữ ký, không báo “An toàn tuyệt đối”.

**Tệp đề nghị:** `Services/OfflineSignatureSnapshotService.cs`, `Services/OfflineSyncQueue.cs`, `Services/SignatureAvailabilityService.cs`, sửa `Services/HybridSignatureScanner.cs`, `Services/ScanEngine.cs`, `Database/DbConnection.cs` và `Services/DataDir.cs`. Với bản đóng gói thật, kho dữ liệu nên đặt dưới `%LocalAppData%` hoặc `%ProgramData%` theo quyền triển khai, **không phụ thuộc đường dẫn repo/test**. **Kiểm thử:** ngắt SQL khi đang quét, file snapshot bị sửa, chạy dưới user khác, trùng sự kiện khi retry, SQL phục hồi nhưng thiếu quyền ghi.

---

## F04. BẢO VỆ THỜI GIAN THỰC CÓ KHẢ NĂNG TỰ PHỤC HỒI — P0

### Hiện trạng và giới hạn

`Services/RealTimeProtection.cs` tạo `FileSystemWatcher` với các sự kiện `Created`, `Changed`, `Renamed`; tại vị trí đã kiểm tra chưa thấy đăng ký `watcher.Error`. `GuardService.cs` có xử lý lỗi watcher **riêng cho phần giám sát hành vi**, nhưng không thể thay thế xử lý của tất cả watcher. `FileSystemWatcher` có thể bỏ lỡ sự kiện khi bộ đệm tràn hoặc thư mục không còn truy cập được.

### Yêu cầu

1. Gắn `Error` cho mọi watcher; ghi log nguyên nhân, đánh dấu trạng thái `Degraded` và lên lịch tạo lại watcher.
2. Khi khôi phục watcher, quét bù các tệp thay đổi kể từ checkpoint gần nhất theo thời gian và metadata phù hợp; không chỉ tăng `InternalBufferSize`.
3. Đưa sự kiện vào **hàng đợi có giới hạn**, xử lý nền với worker hợp lý; gộp sự kiện trùng theo đường dẫn + thông tin thay đổi, tránh lặp quét.
4. Khi tệp đang ghi, chờ cho tệp ổn định theo chính sách retry có giới hạn; nếu hết lượt, đánh dấu “Chưa kiểm tra”, không âm thầm bỏ qua.
5. Tách rõ các thư mục được bảo vệ (Downloads, Desktop, Documents, Temp, Startup...) với phần còn lại của hệ thống.
6. Hiển thị số watcher đang chạy, số watcher lỗi, thời điểm quét bù gần nhất và số sự kiện chờ.
7. Bổ sung cơ chế chạy nền hoặc Windows Service riêng **nếu yêu cầu bảo vệ cả khi giao diện WinForms đã đóng**. Không quảng bá ứng dụng WinForms đơn lẻ là “luôn bảo vệ” khi tiến trình đã thoát.

### Tiêu chí nghiệm thu

- [ ] Tạo/sửa/đổi tên 1.000 tệp thử nghiệm vô hại liên tiếp không làm giao diện treo.
- [ ] Cố ý tạo lỗi watcher: ứng dụng báo tình trạng và có hành động quét bù.
- [ ] Một tệp bị thay đổi nhiều lần liên tiếp không khiến quét vô hạn.
- [ ] Tệp bị khóa có trạng thái xử lý cuối cùng rõ ràng.
- [ ] Tắt chức năng phải giải phóng watcher, worker, hàng đợi và không tự kích hoạt lại ngoài ý muốn.

### Giải pháp kỹ thuật triển khai F04

**Giải pháp ưu tiên: watcher chỉ thu sự kiện; worker mới thực sự quét.** `FileSystemWatcher` không được xử lý hash nặng trực tiếp trong callback.

1. Trong lớp `Services/RealTimeProtection.cs` **đang hoạt động** (tệp có cả đoạn mã cũ được comment), đăng ký `watcher.Error += OnWatcherError` cho tất cả watcher. Mỗi watcher có `WatcherId`, `Root`, `LastHealthyUtc`, `LastCheckpointUtc`, `State` (`Running/Restarting/Degraded/Stopped`).
2. Callback `Created/Changed/Renamed` chuẩn hóa full path, từ chối đường dẫn ngoài phạm vi và đưa một `PendingScan` vào `BlockingCollection<PendingScan>` hoặc hàng đợi có dung lượng giới hạn. Dùng `ConcurrentDictionary` để gộp nhiều event đồng nhất; **không** bỏ sự kiện mới chỉ vì đường dẫn giống nếu size/mtime đã đổi.
3. Worker chạy giới hạn song song, đợi tệp ổn định theo kích thước và `LastWriteTimeUtc` qua vài khoảng đo; mở luồng với `FileShare.ReadWrite | FileShare.Delete` khi thích hợp. Nếu chưa đọc được, retry có giới hạn và lưu `Unreadable` (F06), không lặp vô hạn.
4. Khi lỗi hoặc queue quá tải, đánh dấu khoảng thời gian bị gián đoạn; ngừng watcher bị lỗi, tạo lại với exponential backoff, rồi **quét bù** toàn bộ hoặc theo thay đổi kể từ checkpoint. Dùng các dấu hiệu thời gian/kích thước để tối ưu, nhưng không coi timestamp là cơ chế chống bỏ lọt tuyệt đối. Quét định kỳ toàn phạm vi giám sát để đối soát.
5. Tắt bảo vệ: hủy token, dừng nhận sự kiện, kết thúc worker, dispose watcher và cập nhật UI. Bảo đảm `Start()`/`Stop()` gọi nhiều lần không tạo các watcher trùng.
6. Nếu yêu cầu tiếp tục giám sát khi đóng WinForms, tách thành `ScanAndRemoveVirus.Agent` (Windows Service hoặc process nền được đăng ký phù hợp), giao tiếp với UI qua IPC giới hạn quyền. Đối với Windows 7/10/11 phải kiểm thử service, quyền và cách cài đặt riêng. Không khẳng định `FileSystemWatcher` chặn tệp **trước khi thực thi**.

**Sơ đồ:** `FSW callback -> Hàng đợi giới hạn -> Gộp sự kiện -> Chờ ổn định -> ScanEngine -> Cập nhật phát hiện + lịch sử`. Nhánh lỗi: `FSW.Error/QueueOverflow -> Degraded -> Recreate -> ReconciliationScan -> Running`.

**Tệp đề nghị:** sửa `Services/RealTimeProtection.cs`, `Services/GuardService.cs`; thêm `Services/WatcherSupervisor.cs`, `Services/PendingScanQueue.cs`, `Services/FileStabilityProbe.cs`. **Kiểm thử:** tạo 5.000 tệp thử, mô phỏng mất watcher, rename file sau khi tạo, thay đổi nhiều lần, thư mục bị tháo, bật/tắt nhanh, đóng giao diện khi worker chưa hoàn tất.

---

## F05. CÁCH LY VÀ KHÔI PHỤC AN TOÀN — P0

### Vấn đề cần kiểm tra

`ScanEngine.Quarantine()` lưu tệp vật lý dưới tên `DetectionID.qtn` và ghi metadata trong `ThreatDetections`. Trong `GuardService.RestoreWarningFor()`, mã đang ghép đường dẫn từ `id` mà chưa thêm `.qtn`; cần xác định nhánh gọi đang sử dụng ID nào để tránh bỏ qua cảnh báo khôi phục. `ScanEngine.RestoreQuarantined()` có cơ chế chuyển ngược khi SQL cập nhật thất bại, nhưng còn cần kiểm thử nhất quán ở mọi thao tác.

### Yêu cầu

- Mọi thao tác khôi phục phải xác minh qua **một dịch vụ nghiệp vụ duy nhất**, không chỉ kiểm tra ở màn hình UI.
- Trước khi khôi phục, đối chiếu hash tệp vật lý với hash lưu lúc cách ly; nếu lệch, dừng và cảnh báo thay vì ghi đè.
- Khi trùng tên đường dẫn đích, hiển thị tùy chọn “Đổi tên bản khôi phục”, “Chọn thư mục khác”, “Hủy”, không ghi đè mặc định.
- Có cảnh báo rõ nếu chữ ký vẫn nhận diện là mã độc, yêu cầu người dùng xác nhận nâng cao; không tự chạy tệp được khôi phục.
- Ràng buộc quyền truy cập thư mục cách ly phù hợp; lưu tệp ở trạng thái không có khả năng thực thi trực tiếp hoặc trong kho chứa được kiểm soát.
- Đưa xóa, chuyển tệp, cập nhật SQL và rollback vào luồng có nhật ký, có thể phục hồi khi ứng dụng đột ngột tắt.
- Không hiển thị “Xóa thành công” nếu chỉ xóa vật lý thành công nhưng ghi DB thất bại; cần đối soát lại.
- Nếu quét phát hiện mã độc bên trong ZIP, phân biệt cách ly **archive gốc** và cách ly **entry**; không được giả định entry có đường dẫn vật lý độc lập.

### Giao diện

Bổ sung cột “Tình trạng tệp vật lý”, nút “Kiểm tra lại tệp cách ly”, “Khôi phục đến…”, phần hiển thị lý do không khôi phục được và lịch sử thao tác.

### Tiêu chí nghiệm thu

- [ ] Khôi phục thành công và trạng thái DB/file cùng chuyển sang `Restored`.
- [ ] SQL lỗi giữa chừng thì rollback hợp lý, không thất lạc tệp.
- [ ] Tệp cách ly bị mất, bị chỉnh sửa, bị khóa hoặc USB đã tháo đều có thông báo đúng.
- [ ] Phát hiện trùng tên không ghi đè tài liệu gốc.
- [ ] Các thao tác khôi phục từ nhiều điểm vào đều dùng cùng quy tắc kiểm tra.

### Giải pháp kỹ thuật triển khai F05

**Giải pháp ưu tiên: một `QuarantineService` thống nhất, đối soát file và SQL bằng nhật ký trạng thái (saga) thay vì trông chờ một transaction SQL bao trùm cả filesystem.** SQL transaction **không** tự rollback được thao tác `File.Move`.

1. Tập trung các thao tác `Quarantine`, `Restore`, `Delete`, `VerifyIntegrity` ở `QuarantineService`; WinForms và `GuardService` đều gọi chung. Sửa `GuardService.RestoreWarningFor(...)` để lấy **đường dẫn `QuarantinePath` thực từ `ThreatRepository.GetById(...)`**, không tự `Path.Combine(QuarantineDir, id)` vì tệp hiện được đặt dạng `DetectionID.qtn`.
2. **Quarantine:** xác minh path là tệp vật lý được phép, tính SHA-256, ghi journal `Preparing` (chứa GUID thao tác và vị trí gốc), tạo tên tệp lưu trữ duy nhất và chuyển sang kho cách ly có ACL thích hợp; kiểm tra tệp sau chuyển, cập nhật `ThreatDetections` thành `Quarantined`, đánh journal `Committed`. Nếu bị ngắt ở bất kỳ bước nào, lần mở ứng dụng sau dùng journal để xác định tệp còn ở đâu và chạy đối soát, không tự xóa.
3. **Restore:** lấy trạng thái bản ghi và vị trí lưu thực, kiểm tra tồn tại + hash đã ghi, đánh giá lại chữ ký trước khi khôi phục. Không ghi đè tệp đích đang tồn tại: sinh tên không trùng hoặc cho chọn đích mới. Di chuyển qua đường dẫn trung gian an toàn, đối chiếu kết quả, cập nhật DB; nếu thất bại, thử rollback nhưng phải lưu trạng thái `NeedsReconciliation` khi rollback không thành công.
4. **Delete:** phân biệt `Xóa tệp cách ly` với `Xóa chỉ mục lịch sử`. Ghi bản ghi chờ xóa, xóa dữ liệu, cập nhật DB, sau đó commit nhật ký. Nếu không hoàn chỉnh thì không hiện thông báo thành công.
5. Tất cả thao tác phải khóa theo `QuarantineId` để tránh một luồng xóa trong lúc luồng khác khôi phục; áp dụng SQL parameterized query, validate path, không cho khôi phục vào đường dẫn hệ thống chỉ vì metadata bị sửa. Nếu nguồn bị phát hiện qua `archive.zip > entry.exe`, mặc định xử lý file **archive vật lý** và ghi rõ tác động.

**Các trạng thái gợi ý:** `Preparing`, `Stored`, `Committed`, `Restoring`, `Restored`, `Deleting`, `Deleted`, `NeedsReconciliation`. Không bắt buộc thay các trạng thái `Status` đang dùng ngay; có thể đưa trạng thái thao tác vào `QuarantineOperations` và chuyển dần.

**Tệp đề nghị:** `Services/QuarantineService.cs`, `Services/QuarantineReconciliationService.cs`, sửa `Services/ScanEngine.cs`, `Services/QuarantineLedger.cs`, `Services/GuardService.cs`, `Control/UcCachLy.cs`; thêm `QuarantineOperations`. **Kiểm thử lỗi giả lập từng bước:** ghi DB thành công nhưng `File.Move` lỗi; move xong bị tắt app; tệp bị sửa trong kho; đích khôi phục tồn tại; SQL mất kết nối giữa chừng; 2 lần khôi phục cùng ID.

---

## F06. PHÂN BIỆT TỆP ĐÃ QUÉT, CHƯA QUÉT VÀ LỖI — P0

### Mục đích

Một thư mục có thể chứa hàng nghìn tệp nhưng một phần không thể đọc do quyền truy cập, đang bị khóa, liên kết lặp hoặc lỗi ổ đĩa. Không được sử dụng duy nhất số “tệp đã quét” để kết luận tất cả tệp đã được kiểm tra thành công.

### Yêu cầu

- Phân loại riêng: `Discovered`, `Scanned`, `Clean`, `Detected`, `Suspicious`, `Unreadable`, `Unsupported`, `SkippedByPolicy`, `Cancelled`.
- Khi duyệt cây thư mục, tiếp tục hỗ trợ tất cả tệp con trong giới hạn quyền truy cập; không gộp phát hiện của nhiều tệp vào bản ghi thư mục.
- Lưu danh sách tệp bị lỗi và nguyên nhân (quyền truy cập, I/O, tệp không còn tồn tại, tệp nén bị mã hóa, vượt giới hạn).
- Mỗi phiên quét có tổng số *đã phát hiện*, *đã xử lý*, *đã quét thành công* và *chưa kiểm tra*, với cách tính nhất quán. Với file nén, tách **tệp vật lý** và **entry nội bộ** để tránh cộng đếm nhầm.
- ETA chỉ là ước lượng; cập nhật mượt, không để giao diện “100%” khi vẫn còn công việc trong hàng đợi.

### Gợi ý hiển thị cuối phiên

```text
Kết quả quét thư mục D:\DuLieu
- 2.100 tệp vật lý đã phát hiện
- 2.060 tệp đã được kiểm tra thành công
- 3 tệp phát hiện mã độc theo chữ ký
- 6 tệp nghi vấn heuristic
- 34 tệp không đọc được
- 4 tệp nén chưa kiểm tra nội dung do giới hạn/mật khẩu
=> Kết luận: Quét hoàn tất nhưng còn nội dung chưa được kiểm tra.
```

Các con số trên chỉ là ví dụ minh họa, không phải số liệu từ dự án.

### Tiêu chí nghiệm thu

- [ ] Quét thư mục lồng 5–10 cấp trả về số lượng kiểm tra được đối chiếu với bộ dữ liệu thử.
- [ ] Một tệp bị từ chối quyền truy cập hiện trong danh sách lỗi.
- [ ] Nếu bất kỳ phần nào chưa được kiểm tra thì kết quả tổng thể không được trình bày là “An toàn tuyệt đối”.
- [ ] Hủy giữa chừng hiển thị trạng thái `Cancelled`, không đánh đồng với `Completed`.

### Giải pháp kỹ thuật triển khai F06

**Giải pháp ưu tiên: mô hình kết quả per-file kết hợp bộ đếm chính xác và kiểm kê lỗi.** Không chỉ tăng `TotalFiles` sau khi đã nhận diện thành công.

1. Mỗi phiên quét tạo một `ScanSessionId` và một `ScanCoverageTracker` thread-safe. Đối với file vật lý, sinh bản ghi `Discovered` ngay khi liệt kê; sau xử lý chuyển đúng **một trạng thái kết thúc**: `Clean`, `Detected`, `Suspicious`, `Unreadable`, `Unsupported`, `SkippedByPolicy` hoặc `Cancelled`.
2. Với `Directory.EnumerateFiles/EnumerateDirectories`, lỗi duyệt phải ghi **đường dẫn thư mục và nguyên nhân** (có những thư mục không thể biết bên trong chứa bao nhiêu file; do đó không được kết luận đã phát hiện đầy đủ). Xử lý `ReparsePoint`/symlink và đường dẫn lặp có chính sách rõ, giữ tập đường dẫn hoặc định danh thư mục đã duyệt; không đi theo junction dẫn tới vòng lặp.
3. Archive entry là một loại đối tượng riêng: `ObjectKind=ArchiveEntry`, `ParentObjectId`, `VirtualPath`. Khi tính tổng, công bố **cả số tệp vật lý và số entry**; không cộng nhập nhằng. Nếu một archive không đọc được thì tính archive là `PartiallyScanned/Unreadable`, không giả sử tất cả entry đã được kiểm tra.
4. Quản lý tiến độ theo tổng số đối tượng đã phát hiện cộng số item đang xếp hàng; tổng này có thể thay đổi khi duyệt sâu, nên phần trăm/ETA phải điều chỉnh mà không nhảy lùi bất thường. Chỉ hiển thị `100%` khi toàn bộ queue đã đóng và worker đã xử lý xong. Khi bấm hủy, những item chưa xử lý đổi `Cancelled` nếu đã được phát hiện.
5. Ghi `ScanFileIssues` đối với các trạng thái không kiểm tra được, lưu exception **đã rút gọn/an toàn**, số lần retry, phần nguyên nhân. Không ghi stack trace đầy đủ hay đường dẫn người dùng vào nhật ký chia sẻ công khai mặc định.

**Quy tắc tính:** `Discovered = CompletedSuccessfully + NotChecked + Cancelled + Pending` tại thời điểm chụp số liệu (phân loại không chồng lấn). `CompletedSuccessfully = Clean + Detected + Suspicious`; `NotChecked = Unreadable + Unsupported + SkippedByPolicy`. Nếu còn `Pending`, báo quét chưa hoàn tất. Số tệp sạch chỉ có nghĩa “chưa phát hiện bằng các phép kiểm đã chạy”.

**Tệp đề nghị:** `Services/ScanCoverageTracker.cs`, `Services/ScanItemResult.cs`, `Database/ScanIssueRepository.cs`, sửa `Services/ScanEngine.cs`, `Services/ScanHistoryStore.cs`, `Control/UcTongQuan.cs`. **Kiểm thử:** thư mục không có quyền, ổ bị tháo, file đổi tên khi đang quét, symlink vòng lặp, hủy giữa phiên, ZIP một entry đọc lỗi; đối chiếu số lượng theo từng nhóm.

---

## F07. QUÉT VIRUS TỰ ĐỘNG THEO LỊCH — P1

### Yêu cầu người dùng

Cho phép tạo nhiều lịch quét theo **mỗi ngày**, **ngày trong tuần**, **hằng tháng** hoặc **quét một lần**, áp dụng cho quét nhanh/toàn bộ/thư mục/tệp. Mỗi lịch có tên, trạng thái bật/tắt, đường dẫn, thời điểm chạy, thời điểm lần chạy tiếp theo và chính sách xử lý nếu bỏ lỡ lịch.

### Hành vi hệ thống

1. Không khởi chạy trùng lịch khi phiên quét trước vẫn đang chạy; hỗ trợ xếp hàng hoặc bỏ qua có ghi log.
2. Nếu máy tắt/ngủ vào thời điểm quét, chính sách tùy chọn: quét bù khi khởi động hoặc chuyển sang kỳ tiếp theo.
3. Có thể tạm dừng khi đang sử dụng pin hoặc tài nguyên cao; lựa chọn theo người dùng.
4. Sau mỗi lượt lưu vào `ScanHistory` và hiện thông báo kết quả tổng hợp.
5. Nếu yêu cầu chạy dù WinForms không mở, ưu tiên tích hợp **Windows Task Scheduler** hoặc dịch vụ nền có cơ chế bảo mật, quyền và cài đặt phù hợp.

### Giao diện

**Cài đặt → Lịch quét**: bảng danh sách lịch, nút “Thêm lịch”, “Sửa”, “Bật/Tắt”, “Chạy ngay”, “Xóa”; form tạo lịch có kiểm tra đường dẫn hợp lệ.

### Dữ liệu và kiểm thử

Dùng bảng `ScanSchedules` (xem mục 4). Kiểm thử lịch trùng nhau, qua ngày mới, thay đổi múi giờ/giờ mùa hè ở nơi có áp dụng, quyền truy cập thư mục, quét khi ứng dụng đang mở/đóng và tác vụ bị hủy.

### Giải pháp kỹ thuật triển khai F07

**Giải pháp ưu tiên: sử dụng Windows Task Scheduler làm bộ kích hoạt đáng tin cậy, còn logic quét nằm trong mã hiện tại.** Timer của form chỉ phù hợp khi cửa sổ đang mở và **không** đáp ứng yêu cầu quét khi ứng dụng đã đóng.

1. Bổ sung bảng `ScanSchedules` đã nêu và `ScanScheduleRepository` với `ScheduleID`, `ScanType`, `TargetsJson`, `Frequency`, `NextRunAt`, `RunIfMissed`, `IsEnabled`, `LastOutcome`. Kiểm tra dữ liệu nhập: đường dẫn hợp lệ, giờ, tần suất, quyền quét; không cho truyền tham số là câu lệnh shell.
2. Tách điểm vào **không phụ thuộc UI**: `ScheduledScanRunner.Run(scheduleId, CancellationToken)` đọc cấu hình hợp lệ, xin khóa độc quyền theo lịch (named mutex hoặc khóa DB có timeout), gọi `ScanEngine.Scan(...)`, ghi `ScanHistory` và kết quả. Có thể tạo một tiến trình riêng `ScanAndRemoveVirus.Runner.exe` để Task Scheduler chạy mà không dựng WinForms.
3. Từ màn `ScanScheduleForm`, tạo/chỉnh/sửa/xóa tác vụ trong **Task Scheduler** bằng API COM hoặc thư viện bao Task Scheduler có phiên bản tương thích .NET Framework. Action của tác vụ phải chỉ gọi **đường dẫn exe cố định đáng tin** + một tham số `--schedule-id <số>` đã validate. Không ghép đường dẫn tùy chọn vào lệnh PowerShell/cmd để tránh lỗi escaping và nguy cơ thực thi ngoài ý muốn.
4. Khi khởi chạy, xét chính sách: nếu phiên quét cũ vẫn chạy thì `Skip` hoặc `QueueOne` (chỉ cho phép một phiên chờ); nếu bỏ lỡ lịch thì khởi chạy bù đúng một lần khi điều kiện cho phép. Máy dùng pin có thể hoãn theo cấu hình người dùng.
5. Lưu kết quả quét dù người dùng đóng UI; nếu SQL đang mất kết nối thì dùng cơ chế F03 hoặc ghi trạng thái `Failed` có thể xem sau. Khi có quyền hạn chế, Task Scheduler phải hiển thị lỗi đăng ký rõ ràng; **không** tự đòi quyền Administrator nếu không cần.

**Luồng:** `Lưu lịch SQL -> Đăng ký Scheduled Task -> Đến giờ -> Runner(id) -> Khóa chống trùng -> ScanEngine -> Ghi lịch sử -> Thông báo/tổng hợp`.

**Tệp đề nghị:** `Services/ScanSchedulerService.cs`, `Services/ScheduledScanRunner.cs`, `Database/ScanScheduleRepository.cs`, `Control/ScanScheduleForm.cs`; bổ sung entrypoint trong `Program.cs` hoặc dự án runner riêng. **Kiểm thử:** 2 lịch cùng lúc, đóng WinForms, máy ngủ, không có quyền đọc thư mục, xóa lịch khi đang quét, tác vụ bị sửa ngoài ứng dụng, chạy trên Windows 7/10/11.

---

## F08. GIÁM SÁT VÀ ỨNG PHÓ VỚI RANSOMWARE — P1

### Mục tiêu

Nâng cấp giám sát hành vi hiện có để phát hiện **chuỗi hoạt động đáng nghi** như sửa/xóa hàng loạt tài liệu, đổi phần mở rộng bất thường và thao tác mã hóa có tín hiệu rủi ro. Đây là lớp phát hiện hành vi **bổ sung**, không bảo đảm chống mọi ransomware.

### Yêu cầu

- Ghi nhận tiến trình thực hiện (khi hệ điều hành cho phép), số tệp thay đổi, tần suất, nhóm phần mở rộng, thư mục bị tác động và các tín hiệu bất thường khác.
- Sử dụng điểm rủi ro tổng hợp thay vì một chỉ dấu đơn lẻ; hạn chế dương tính giả với chương trình sao lưu, trình biên tập và công cụ nén hợp pháp.
- Có cấu hình thư mục bảo vệ trọng yếu: Documents, Desktop, thư mục người dùng tự chọn.
- Giai đoạn đầu chỉ **cảnh báo, gắn cờ, lưu chứng cứ**. Chỉ triển khai chặn/đình chỉ tiến trình sau khi đã có cơ chế kiểm chứng, thử nghiệm trên máy ảo và quyền phục hồi.
- Nếu bổ sung sao lưu phiên bản tệp, bản sao phải được bảo vệ khỏi cùng tiến trình và không được coi là tính năng khôi phục hoàn chỉnh khi chưa kiểm tra thực tế.

### Giao diện và tiêu chí nghiệm thu

Hiển thị “Cảnh báo hoạt động bất thường”: ứng dụng/tiến trình, tệp liên quan, bằng chứng, mức độ tin cậy, nút “Xem chi tiết”, “Đánh dấu nhầm”, “Mở vị trí”. Kiểm thử bằng chương trình giả lập sửa nhiều tệp **vô hại**, không dùng mẫu ransomware thực trên máy chính.

### Giải pháp kỹ thuật triển khai F08

**Giải pháp ưu tiên: kết hợp tín hiệu theo cửa sổ thời gian và triển khai chế độ *quan sát/cảnh báo* trước khi tự động can thiệp.** Không suy diễn rằng sự kiện `FileSystemWatcher` tự nó xác định được tiến trình thực hiện thay đổi.

1. Mở rộng `BehavioralActivityAnalyzer` bằng `BehaviorWindow` theo khoảng thời gian trượt, lưu số đường dẫn khác nhau bị sửa, số lần rename/xóa, kiểu phần mở rộng cũ/mới, entropy/định dạng mẫu trên bản thử (nếu phù hợp), tập thư mục tài liệu và tốc độ thay đổi. Dùng `ConcurrentDictionary` + hàng đợi loại bỏ dữ liệu cũ để không giữ lịch sử vô hạn.
2. Tính điểm minh bạch từ **nhiều bằng chứng**: số lượng tài liệu bị sửa hàng loạt, nhiều loại phần mở rộng cùng lúc, tên bị đổi liên tục, hành vi tiến trình bị cảnh báo bởi quy tắc khác. Ngưỡng ban đầu lấy từ bộ mô phỏng vô hại; có thời gian giảm điểm và chống phát cảnh báo liên tục (cooldown). Ví dụ minh họa: thay đổi hàng chục tài liệu trong một cửa sổ ngắn chỉ là **tín hiệu kiểm tra**, chưa đủ khẳng định ransomware.
3. Liên kết tiến trình khi có bằng chứng đáng tin (nhật ký hệ điều hành/ETW phù hợp quyền và phiên bản); nếu chỉ biết `FileSystemWatcher` có thay đổi file thì hiển thị **“Chưa xác định tiến trình”**, không gán tên quy trình dựa trên suy đoán thời gian.
4. Dùng danh sách ứng dụng sao lưu/chỉnh sửa hợp pháp làm yếu tố giảm báo nhầm, **không** biến thành danh sách bỏ kiểm tra vĩnh viễn. Ghi lại các đường dẫn đã ảnh hưởng, loại hành vi, điểm và timestamp làm chứng cứ.
5. Chế độ mặc định: cảnh báo + mở chi tiết + cho người dùng kiểm tra; việc tự dừng tiến trình phải để sau, có lựa chọn xác nhận, danh sách loại trừ an toàn và cơ chế hoàn tác. Muốn **chặn ghi trước khi dữ liệu bị thay đổi** cần nghiên cứu thành phần kernel/Windows API chuyên biệt, không thể chỉ dựa vào `FileSystemWatcher`.
6. Nếu thêm bảo vệ phiên bản tài liệu, sao lưu phải có quyền truy cập hạn chế và dữ liệu khôi phục phải được xác minh định kỳ. Không tuyên bố phục hồi ransomware khi chưa có phương án sao lưu/khôi phục đã kiểm thử độc lập.

**Tệp đề nghị:** `Services/RansomwareBehaviorMonitor.cs`, `Services/BehaviorWindow.cs`, `Services/BehaviorEvidenceStore.cs`, sửa `Services/BehavioralActivityAnalyzer.cs`, `Services/GuardService.cs`. **Kiểm thử:** script thử vô hại đổi tên/sửa file trong thư mục test; so sánh với thao tác sao lưu hợp pháp, giải nén lớn và đồng bộ file để hiệu chỉnh dương tính giả.

---

## F09. DANH SÁCH NGOẠI LỆ CÓ KIỂM SOÁT — P1

### Mục đích

Hỗ trợ trường hợp một tệp hợp pháp bị gắn cờ nhầm, nhưng tránh tạo lỗ hổng bảo vệ vì người dùng vô tình bỏ qua cả ổ đĩa hoặc thư mục hệ thống.

### Yêu cầu

- Ưu tiên ngoại lệ theo **SHA-256 của phiên bản tệp cụ thể**; ngoại lệ theo đường dẫn chỉ dành cho trường hợp có lý do rõ ràng và cảnh báo rủi ro.
- Gắn phạm vi ngoại lệ (quét thủ công/real-time/heuristic), lý do, người tạo, ngày tạo, ngày hết hạn.
- Mọi ngoại lệ có thể thu hồi và cần ghi audit log; danh sách ngoại lệ không được tự động áp dụng cho các tệp bên trong ZIP nếu chưa xác định đúng hash entry.
- Không cho bỏ qua mặc định toàn bộ `C:\Windows`, `C:\Program Files`, ổ hệ thống hoặc toàn bộ ổ đĩa.
- Khi chữ ký nguy hiểm được cập nhật, ứng dụng có thể yêu cầu người dùng xem xét lại ngoại lệ đang có.

### Giao diện

**Cài đặt → Ngoại lệ**: hiển thị tên tệp, SHA-256, nguồn, phạm vi, lý do, ngày hết hạn, trạng thái; có nút “Thêm”, “Xóa”, “Kiểm tra lại”.

### Giải pháp kỹ thuật triển khai F09

**Giải pháp ưu tiên: ngoại lệ có phạm vi hẹp, thời hạn cụ thể và không che khuất phát hiện có độ tin cậy cao.**

1. Tạo bảng `ScanExclusions` với khóa định danh, `SHA256`, `FilePath`, `Scope`, `Reason`, `CreatedAtUtc`, `ExpiresAtUtc`, `CreatedBy`, `IsActive`; log thao tác qua bảng audit hoặc nhật ký chung. Chỉ cho thêm ngoại lệ sau khi người dùng chọn tệp và phần mềm **tự tính hash**; không chỉ tin chuỗi hash nhập tay.
2. `ScanExclusionService.ShouldExclude(ScanTarget target, DetectionStage stage)` kiểm tra **trước mỗi giai đoạn phù hợp**, không tắt cả pipeline một cách mù quáng. Chính sách gợi ý: ngoại lệ hash được duyệt có thể bỏ qua **heuristic báo nhầm**; nhưng chữ ký mã độc đã xác minh vẫn cần cảnh báo, trừ khi có chính sách nâng cao riêng với xác nhận nổi bật. `IsActive=0`/hết hạn phải không còn hiệu lực ngay sau invalidate cache.
3. Với ngoại lệ đường dẫn, chuẩn hóa `Path.GetFullPath`, xác định đối tượng là file hay thư mục, xử lý trường hợp đường dẫn thay đổi qua symlink/junction và chống match tiền tố sai (`C:\App` không tương đương `C:\Apple`). Không cho thêm wildcard tràn lan; chặn/báo mạnh với đường dẫn gốc ổ, `%WINDIR%`, `%ProgramFiles%` và thư mục hệ thống quan trọng.
4. Archive entry chỉ được ngoại lệ theo SHA-256 **của entry cụ thể**, không lấy SHA-256 file `.zip` làm ngoại lệ tự động cho mọi entry con. Khi chữ ký mới khớp hash đang ngoại lệ, chuyển thành `NeedsReview` và hiển thị cảnh báo rõ.
5. Trong giao diện `ScanExclusionForm`, có nút “Thêm bằng cách chọn tệp”, “Xem lý do”, “Hết hạn ngay”, “Kiểm tra lại”; hiển thị nguy cơ bỏ qua scan trước khi lưu và yêu cầu xác nhận cho ngoại lệ phạm vi rộng.

**Luồng:** `Chọn tệp -> Tính SHA-256 -> Chọn lý do + thời hạn -> Validate phạm vi -> Lưu SQL + audit -> Làm mới policy -> Áp dụng trong scan`.

**Tệp đề nghị:** `Services/ScanExclusionService.cs`, `Database/ScanExclusionRepository.cs`, `Control/ScanExclusionForm.cs`, sửa điểm vào `ScanEngine.Evaluate(...)` và tuyến quét realtime. **Kiểm thử:** file đổi nội dung sau khi ngoại lệ; hết hạn; symlink; đường dẫn case-insensitive; cùng tên file nhưng hash khác; ngoại lệ heuristic không che dấu chữ ký mã độc đã xác minh.

---

## F10. KIỂM TRA TÌNH TRẠNG BẢO MẬT WINDOWS — P1

### Mục đích

Tạo màn “Kiểm tra an toàn hệ thống” để đánh giá **tư thế bảo mật** (security posture) của máy tính, tách biệt với chức năng phát hiện tệp mã độc.

### Hạng mục nên kiểm tra

- Windows Firewall đang bật/tắt theo profile đang sử dụng.
- Trạng thái phần mềm bảo vệ được Windows Security nhận diện (nếu API cho phép).
- Windows Update và những cảnh báo cập nhật quan trọng ở mức mà hệ điều hành cho phép truy vấn.
- Danh sách ứng dụng tự khởi động và các mục startup đáng chú ý.
- Task Scheduler: tác vụ mới hoặc có dấu hiệu bất thường; không tự kết luận mọi tác vụ lạ là mã độc.
- Các vị trí tự khởi động trong Registry và thư mục Startup.
- Tình trạng mã hóa ổ đĩa và sao lưu dữ liệu, nếu khả năng truy vấn hệ thống cho phép.

### Nguyên tắc

Chỉ đọc và đưa khuyến nghị ở phiên bản đầu; không tự tắt dịch vụ, tự xóa registry, hay thay đổi tường lửa. Phân biệt **“Không có quyền kiểm tra”** và **“Không phát hiện rủi ro”**.

### Giao diện

Một trang riêng gồm các thẻ “Tường lửa”, “Bảo vệ hệ thống”, “Cập nhật”, “Khởi động”, “Sao lưu”, kèm trạng thái `Đạt / Cần chú ý / Không xác định` và liên kết mở tiện ích Windows liên quan.

### Giải pháp kỹ thuật triển khai F10

**Giải pháp ưu tiên: các bộ kiểm tra chỉ-đọc, độc lập, có timeout và trạng thái “Không xác định”.** Màn hình này là báo cáo cấu hình Windows, không phải chứng nhận thiết bị “không có mã độc”.

1. Xây `SecurityHealthCheckService.RunAllAsync()` trả về danh sách `SecurityCheckResult` gồm `CheckId`, `DisplayName`, `Status` (`Pass/Warning/Unknown/NotSupported`), `EvidenceSummary`, `CheckedAtUtc`, `Recommendation`, `OpenSettingsTarget`. Mỗi phép kiểm tra chạy độc lập: lỗi một hạng mục không làm hỏng cả trang.
2. **Firewall:** có thể truy vấn `INetFwPolicy2` (Windows Firewall COM API) theo từng profile, báo profile hiện hành có bật hay không. Không đồng nhất trạng thái một profile với toàn máy.
3. **Antivirus:** ưu tiên API Windows Security Center phù hợp máy client; với WMI `root\SecurityCenter2` chú ý phiên bản Windows và quyền truy cập. Không có provider/namespace thì `Unknown/NotSupported`, không kết luận “không có antivirus”.
4. **Windows Update:** sử dụng Windows Update Agent API/COM ở chế độ truy vấn; không tự cài đặt hoặc tự khởi động lại. Nếu dịch vụ bị chặn hoặc policy tổ chức quản lý, ghi rõ không kiểm tra được.
5. **Startup/Task Scheduler/Registry:** đọc danh sách mục khởi động và tạo bản tóm tắt; chỉ gắn cờ bằng các dấu hiệu giải thích được (đường dẫn tạm, file không còn tồn tại, thay đổi gần đây và nguồn không rõ), không tự xóa. Trường hợp cần quyền cao hơn, tiếp tục những phép kiểm tra được phép.
6. **Mã hóa ổ và sao lưu:** nếu truy vấn được, báo BitLocker/Device Encryption/backup status; riêng Windows 7 và máy không hỗ trợ tính năng phải trả `NotSupported`, không coi là lỗi bảo mật tất yếu.
7. Giao diện chạy async để không làm treo form; cho phép kiểm tra lại và mở ứng dụng Cài đặt Windows tương ứng, **không** thực thi các lệnh shell do dữ liệu đầu vào cung cấp.

**Tệp đề nghị:** `Services/SecurityHealthCheckService.cs`, `Services/Checks/FirewallCheck.cs`, `AntivirusStatusCheck.cs`, `StartupEntriesCheck.cs`, `WindowsUpdateCheck.cs`, `Control/SecurityHealthForm.cs`. **Kiểm thử:** Windows 7/10/11, user thường, dịch vụ bị tắt, WMI thiếu dữ liệu, timeout, máy ảo không có BitLocker.

---

## F11. NHẬT KÝ VÀ BÁO CÁO CHI TIẾT — P1

### Yêu cầu

- Bổ sung bộ lọc lịch sử theo thời gian, chế độ quét, mức độ, trạng thái xử lý, đường dẫn và phương pháp phát hiện.
- Xuất báo cáo **CSV** (giữ tính năng hiện có) và có thể thêm **HTML/PDF** ở giai đoạn sau.
- Thống kê tệp quét theo ngày/tuần/tháng, tỷ lệ thành công, số tệp chưa đọc được, số tệp nghi vấn và tệp đã cách ly.
- Với từng phát hiện, hiển thị nguồn bằng chứng: SHA-256, chữ ký đầy đủ/CDC, heuristic, VirusTotal (nếu đã tra cứu), phiên bản chữ ký.
- Lưu nhật ký lỗi ứng dụng, kết nối SQL, cập nhật chữ ký, watcher và thao tác cách ly; không chứa API key, mật khẩu hoặc nội dung riêng tư của tệp.
- Có chính sách lưu giữ và xóa dữ liệu lịch sử, không tự ý xóa bằng chứng đang liên kết với bản ghi cách ly.
- Che bớt đường dẫn/thông tin riêng tư khi xuất báo cáo để chia sẻ cho người khác.

### Tiêu chí nghiệm thu

- [ ] Mở lại ứng dụng vẫn xem được kết quả quét cũ trong điều kiện lưu trữ hợp lệ.
- [ ] CSV mở bằng Excel không làm mất dấu tiếng Việt; có phòng vệ CSV formula injection khi xuất dữ liệu người dùng kiểm soát.
- [ ] Nhật ký nêu rõ lý do thất bại và không lộ khóa API.
- [ ] Không xóa nhầm quan hệ tới tệp còn trong kho cách ly.

### Giải pháp kỹ thuật triển khai F11

**Giải pháp ưu tiên: chuẩn hóa sự kiện thành dữ liệu có cấu trúc, tách nhật ký kỹ thuật và báo cáo dễ đọc.**

1. Thiết kế `DiagnosticEvent` gồm `EventId`, `TimeUtc`, `Severity`, `Category` (`Scan`, `Signature`, `SQL`, `Watcher`, `Quarantine`, `Update`), `CorrelationId`, `ScanId`, `ShortMessage`, `ErrorCode`. Với đường dẫn và hash có thể lưu nội bộ theo chính sách riêng tư; với file xuất công khai phải cho người dùng ẩn đường dẫn.
2. Đưa thông tin phát hiện vào `ScanHistoryStore`/repository: phương pháp (`FullHash`, `CDC`, `Heuristic`, `YARA` nếu bật), chữ ký/phiên bản, phần archive entry, hành động đã thực hiện và số đối tượng chưa quét. Nếu cần lưu từng file, dùng bảng chi tiết có index `(ScanID, Status)` và phân trang trên UI; **không** nạp hàng trăm nghìn hàng lên `DataGridView` một lượt.
3. `ApplicationDiagnosticService` sử dụng queue ghi nền (bounded) và rotating log theo ngày/dung lượng. Không ghi thông tin bí mật như VirusTotal API key, chuỗi kết nối chứa credential, nội dung tệp cá nhân hay token vào log.
4. Với CSV: dùng UTF-8 BOM để Excel đọc tiếng Việt; escape dấu phẩy/nháy kép/xuống dòng đúng chuẩn; dữ liệu chuỗi người dùng kiểm soát bắt đầu bằng `=`, `+`, `-`, `@` hoặc ký tự whitespace điều khiển trước công thức cần **escape chống CSV formula injection**. Xuất báo cáo theo đúng bộ lọc hiện hành và chỉ số nhất quán F06.
5. Với HTML: dựng mẫu HTML tĩnh, HTML-encode mọi giá trị động để ngăn chèn script. PDF chỉ thêm sau khi chọn thư viện tương thích; không phải ưu tiên nếu CSV/HTML đã đủ cho đồ án.
6. Thiết lập lưu giữ (ví dụ số ngày tùy người dùng và dung lượng giới hạn), dọn lịch sử **không xóa metadata bắt buộc cho file đang cách ly**. Tạo ID tương quan để dễ tra một lần quét xuyên qua scanner, database, watcher và quarantine.

**Báo cáo đầu ra:** `Thời gian -> Phạm vi -> Nguồn chữ ký -> Đã phát hiện -> Đã kiểm tra thành công -> Bỏ sót và lý do -> Danh sách phát hiện -> Hành động -> Khuyến nghị`.

**Tệp đề nghị:** `Services/ApplicationDiagnosticService.cs`, `Services/ScanReportExporter.cs`, sửa `Services/ScanHistoryStore.cs`, `Database/ScanHistoryRepository.cs`, `Control/UcLichSu.cs`. **Kiểm thử:** chữ tiếng Việt, tên file có xuống dòng/ngoặc kép, ô bắt đầu `=HYPERLINK(...)`, báo cáo hàng chục nghìn tệp, SQL lỗi khi xuất, file xuất sang thư mục không có quyền ghi.

---

## F12. KIỂM TRA CHỮ KÝ SỐ VÀ THÔNG TIN PE — P2

### Mục đích

Đối với `.exe`, `.dll`, `.sys`, kiểm tra chữ ký Authenticode, trạng thái chuỗi chứng thư và metadata tệp thực thi để cung cấp thêm bằng chứng đánh giá. **Có chữ ký số hợp lệ không chứng minh tệp sạch; không có chữ ký số cũng không mặc nhiên là mã độc.**

### Yêu cầu

- Hiển thị nhà phát hành, trạng thái ký, timestamp, thông tin chứng thư và nguyên nhân khi không xác minh được.
- Trích xuất cấu trúc PE cơ bản (headers, section, import table) theo cách đọc không thực thi, có giới hạn kích thước và kiểm soát dữ liệu không tin cậy.
- Tín hiệu metadata chỉ đóng góp vào đánh giá nghi vấn, không tự động cách ly chỉ vì không có chữ ký.

### Giải pháp kỹ thuật triển khai F12

**Giải pháp ưu tiên: kiểm tra uy tín file thực thi bằng Authenticode và PE metadata như một lớp bằng chứng bổ sung, không dùng riêng để gắn nhãn mã độc.**

1. Chỉ kích hoạt với file có magic `MZ` và header `PE\0\0` hợp lệ thay vì chỉ dựa vào `.exe`/`.dll`/`.sys`. Thực hiện kiểm tra kích thước file và offset trước khi đọc section, import table, timestamp và những trường cơ bản.
2. Viết `AuthenticodeVerificationService` dùng `WinVerifyTrust` trên Windows qua P/Invoke có cấu trúc khai báo đúng kiến trúc; **xác định rõ** đã kiểm tra chữ ký nhúng hay catalog, chính sách chứng thư và trạng thái revocation. Nếu offline không thể xác thực thu hồi chứng thư thì kết quả có thể là `Unknown/RevocationUnavailable`, không tự báo hợp lệ.
3. Dùng `FileVersionInfo` để lấy tên sản phẩm/nhà phát hành/phiên bản như thông tin mô tả **không đáng tin tuyệt đối**; phân biệt rõ với danh tính trong chứng thư số. Nếu dùng `System.Reflection.Metadata`/PEReader qua NuGet phải kiểm tra phiên bản hỗ trợ .NET Framework 4.7.2; còn không dùng parser PE đọc-giới-hạn do dự án tự kiểm soát.
4. Từ PE metadata có thể trích: kiến trúc x86/x64, số lượng section, quyền section (read/write/execute), vùng có entropy cao và import bất thường. Các dấu hiệu này đóng góp `HeuristicEvidence`, **không** trực tiếp xác nhận mã độc.
5. Tại giao diện Chi tiết tệp, thêm tab “Chữ ký số & PE” với `Có chữ ký / Không có / Sai chữ ký / Không thể xác minh`, nhà ký, timestamp và các trường PE. Không nhầm chữ ký Authenticode với SHA-256 dùng trong cơ sở dữ liệu.

**Tệp đề nghị:** `Services/AuthenticodeVerificationService.cs`, `Services/PeMetadataReader.cs`, sửa `Services/StaticHeuristicAnalyzer.cs`, `Control/UcTongQuan.ChiTiet.cs`. **Kiểm thử:** PE ký hợp lệ, không ký, chữ ký bị hỏng, file MZ giả, file 0 byte, chứng thư hết hạn nhưng có timestamp, máy không có mạng và PE x64 trên Windows x86 nếu hỗ trợ.

---

## F13. TÍCH HỢP YARA TÙY CHỌN — P2

### Mục đích

Bổ sung lớp dò chuỗi byte/văn bản hoặc điều kiện mẫu cho những trường hợp hash toàn tệp thay đổi. Đây là **phần mở rộng tùy chọn** vì dự án hiện ưu tiên hash, CDC và heuristic.

### Yêu cầu

- Quản lý bộ luật YARA có phiên bản, nguồn gốc và trạng thái đã duyệt.
- Chạy trong worker cô lập, có giới hạn CPU, RAM, thời gian và kích thước đầu vào; tệp/rule lỗi không làm sập toàn bộ phiên quét.
- Ghi tên rule, bằng chứng phù hợp và độ tin cậy; không gộp mọi match thành “mã độc đã xác nhận”.
- Không cho người dùng tải luật không kiểm duyệt rồi tự áp dụng hành động xóa/cách ly.
- Kiểm tra tương thích của thư viện và cách đóng gói runtime với Windows 7/10/11, kiến trúc x86/x64 trước khi tích hợp.

### Giải pháp kỹ thuật triển khai F13

**Giải pháp ưu tiên: plugin YARA riêng, mặc định tắt, không làm thay đổi kết quả hash/CDC hiện có khi plugin không khả dụng.**

1. Tạo `YaraRuleRepository` quản lý `RuleId`, tên, SHA-256 của nội dung rule, nguồn, phiên bản, `IsApproved`, `Enabled`, thời điểm duyệt, mô tả và mức độ tin cậy. Chỉ tải rule đã được duyệt; lưu file rule trong thư mục không cho người dùng thường tùy tiện thay thế.
2. Tạo **worker process** riêng (`YaraWorker.exe`) sử dụng bản YARA native/bridge tương thích Windows mục tiêu, chạy dưới quyền tối thiểu, với hạn mức CPU/bộ nhớ/thời gian; dùng IPC có kiểm tra dữ liệu, protocol version và kích thước thông điệp. Không chạy DLL/YARA lấy từ nguồn không tin cậy như một phần ứng dụng chính.
3. Chuỗi tích hợp: `Hash -> CDC -> Heuristic -> YARA (nếu bật/được cấu hình)`; nếu hash đã xác nhận theo chính sách hiện tại thì có thể bỏ qua YARA để tiết kiệm thời gian. Trong kết quả, phân biệt `YaraRuleMatch` với `ConfirmedMalware` và lưu rule id, phiên bản, tag và bằng chứng phù hợp theo giới hạn riêng tư.
4. Quét file lớn bằng cơ chế của engine có hỗ trợ và giới hạn thời gian; quy tắc có lỗi/không tương thích phải trả `RuleError`, worker timeout phải trả `YaraUnavailable` thay vì “tệp sạch”. Không cho biểu thức rule không kiểm duyệt tự kích hoạt xóa vĩnh viễn.
5. Xây tập luật minh họa vô hại trước, đo thời gian quét, RAM và dương tính giả. Bản Windows 7 có thể cần runtime và kiến trúc riêng; kiểm thử x86/x64, thư viện native, tương thích license và đóng gói đầy đủ trước khi bật mặc định.

**Tệp đề nghị:** `Services/YaraScanCoordinator.cs`, `Services/YaraRuleRepository.cs`, dự án `YaraWorker`, `Control/YaraRulesForm.cs`. Có thể tạo bảng `YaraRules` và `YaraRuleMatches` khi triển khai; **không cần bổ sung vào schema nếu chưa triển khai plugin**. **Kiểm thử:** luật hợp lệ/vô hiệu, một file sạch khớp mẫu giả lập, worker treo/bị tắt, rule xung đột, không có DLL, vượt giới hạn thời gian, ứng dụng bị đóng khi worker đang chạy.

---

## F14. BỘ KIỂM THỬ VÀ ĐÁNH GIÁ CHẤT LƯỢNG — P1

### Mục tiêu

Biến việc đánh giá phần mềm thành dữ liệu đo được, phục vụ cả phát triển và bảo vệ chuyên đề.

### Bộ thử nghiệm

1. **Chức năng:** quét mọi chế độ, cây thư mục nhiều cấp, Unicode/tên dài, tệp 0 byte, tệp lớn, ổ USB, tệp khóa, quyền truy cập hạn chế, hủy quét, quét song song.
2. **Chữ ký:** một trong ba hash trùng thì phát hiện theo quy tắc OR; chữ ký `IsActive=0` không được dùng; kiểm tra xung đột và bản ghi không hợp lệ.
3. **CDC:** các biến thể thay đổi ở đầu/giữa/cuối, dịch offset, nhóm chunk thiếu, ngưỡng tương đồng, kiểm soát dương tính giả.
4. **Heuristic/hành vi:** mẫu thử vô hại đủ gây cảnh báo; kiểm tra không tự gắn “mã độc chắc chắn” khi chỉ có heuristic.
5. **Archive:** nhiều tầng, có mật khẩu, bị hỏng, tên entry độc hại, ZIP bomb giả lập an toàn.
6. **SQL:** kết nối thành công/thất bại/gián đoạn; lịch sử và cách ly không mất dữ liệu.
7. **Hiệu năng:** kích thước tập thử 1 GB, 5 GB, 10 GB (điều chỉnh theo máy); đo tốc độ, peak RAM, CPU, thời gian, tệp/giây.
8. **Giao diện:** DPI 100%, 125%, 150%; cửa sổ nhỏ/lớn; văn bản tiếng Việt dài; tệp đường dẫn dài; trạng thái đang quét và mất SQL.

### Chỉ số đánh giá

```text
Detection Rate (Recall) = TP / (TP + FN)
False Positive Rate     = FP / (FP + TN)
Precision               = TP / (TP + FP)
Scan Throughput         = Tổng số byte được kiểm tra / Thời gian quét
Coverage Rate           = Số đối tượng kiểm tra thành công / Số đối tượng thuộc phạm vi kiểm tra
```

- `TP`: mẫu độc được phát hiện đúng; `FN`: mẫu độc bị bỏ sót.
- `FP`: mẫu sạch bị gắn nhầm; `TN`: mẫu sạch được xác định không phát hiện.
- Các chỉ số chỉ có ý nghĩa khi **bộ dữ liệu đã được gán nhãn đáng tin cậy**. Không dùng kết quả demo hoặc tập hash do phần mềm tự tạo làm bằng chứng cho tỷ lệ phát hiện trên thế giới thực.
- Chỉ dùng dữ liệu thực trong môi trường máy ảo cách ly, được phép sử dụng và có quy trình an toàn; ưu tiên mẫu kiểm thử vô hại/EICAR cho kiểm thử vận hành.

### Giải pháp kỹ thuật triển khai F14

**Giải pháp ưu tiên: tạo bộ kiểm thử có kết quả mong đợi (*ground truth*) độc lập với công cụ đang kiểm tra, chạy lặp lại được và không gây nguy hiểm cho máy thật.**

1. Dùng cấu trúc `Tests/Fixtures/` gồm `Clean`, `BenignSuspiciousBehavior`, `Archives`, `Permissions`, `Corrupt`, `Performance`; xây manifest CSV/JSON mô tả `SampleId`, `ExpectedStatus`, `ExpectedDetectionMethod`, `ExpectedArchiveEntry`, `ExpectedError`, kích thước và nhãn tin cậy. Dữ liệu nên là file thử vô hại; EICAR chỉ dùng kiểm tra luồng nhận diện cơ bản trong môi trường cho phép.
2. **Unit test:** với `HybridSignatureScanner.CreateTestSnapshot(...)`, tạo bảng hash thử có cấu trúc chuẩn, thử OR MD5/SHA-1/SHA-256, signature inactive, CDC similarity, ngưỡng heuristic. Tách `ScanFile` khỏi truy vấn SQL để một số bài test chạy offline mà không cần cài SQL Server.
3. **Integration test:** tạo DB `AntivirusDB_Test` hoặc schema riêng, chạy migration trên bản sao, nạp dữ liệu test rồi rollback/dọn dẹp có kiểm soát. **Không** chạy test quarantine/restore vào thư mục thật của người dùng; tận dụng `XVIRUS_DATA_DIR` và thư mục cách ly test riêng. Test các điểm lỗi giả lập ở F03/F05 để đánh giá khôi phục giao dịch.
4. **Windows UI test:** mở màn quét, lịch sử, cách ly, cập nhật chữ ký và màn bổ sung; kiểm tra UI không treo, thanh tiến trình/ETA không sai, responsive DPI 100/125/150. Tách các tests chỉ chạy Windows khỏi tests logic nếu máy phát triển là macOS.
5. **Benchmark:** cố định bộ dữ liệu, chạy ít nhất 3 lần với chính sách clear-cache được mô tả; ghi CPU, peak working set, throughput MiB/s, files/s, số file lỗi, độ sâu archive, phiên bản chữ ký và cấu hình máy. Không so sánh hai lượt chạy khi cache/nguồn dữ liệu khác nhau mà không chú thích.
6. **Báo cáo chất lượng:** tính TP/FP/TN/FN từ nhãn dữ liệu tin cậy; phân biệt `Suspicious` với `Confirmed` trước khi tính precision/recall, công khai số mẫu và nguồn gốc. Với tập không có nhãn chắc chắn, **chỉ** báo số cảnh báo/độ bao phủ, không suy ra tỷ lệ phát hiện thực tế.
7. Trước mỗi bản release: chạy `Smoke -> Functional -> Integration -> UI -> Performance` theo checklist; lưu kết quả máy, commit source, thời điểm, số test pass/fail và bug chưa xử lý. Không cần tất cả bước chạy trong một môi trường duy nhất.

**Tệp đề nghị:** mở rộng `Tests/ScanAllFilesSelfTest.cs`, `Tests/HybridHashSelfTest.cs`, `Tests/CdcSelfTest.cs`, `Tests/BehavioralActivitySelfTest.cs`; thêm `Tests/ArchiveScanSelfTest.cs`, `Tests/QuarantineRecoveryTest.cs`, `Tests/OfflineSnapshotTest.cs`, `Tests/AcceptanceReportGenerator.cs`. **Thước đo nghiệm thu:** không có thất lạc tệp trong kịch bản cách ly lỗi; không báo an toàn cho vùng chưa kiểm tra; test mẫu vô hại cho mỗi chức năng đều chạy lặp lại được.

---

## 3. YÊU CẦU GIAO DIỆN VÀ TRẢI NGHIỆM

### 3.1. Tổng quan

- Giữ giao diện hiện có với hai thao tác chính “Quét ngay” và “Quét nâng cao”; không thêm quá nhiều nút chính.
- Bổ sung trạng thái có nghĩa: **Bảo vệ đầy đủ / Bảo vệ hạn chế / Cần xử lý**.
- Các ô số liệu “Mối đe dọa”, “Tệp đã quét”, “Lần quét gần nhất” cần thống nhất cách tính với dữ liệu lịch sử.
- Khi không kết nối SQL hoặc chưa tải chữ ký, nêu nguyên nhân ngắn gọn và hướng khắc phục; không dùng thông điệp dễ gây hiểu nhầm rằng toàn bộ máy an toàn.

### 3.2. Đang quét

- Thanh tiến trình mượt, ETA biến thiên hợp lý, không tự nhảy về 0 khi chuyển giai đoạn.
- Hiển thị tệp đang xử lý cố định ở một khu vực, đường dẫn dài được rút gọn có tooltip/copy.
- Số đếm: tệp vật lý phát hiện, tệp xử lý xong, tệp không kiểm tra được, nghi vấn, phát hiện mã độc; khi quét archive nên có thêm bộ đếm entry.
- Nút “Tạm dừng”, “Tiếp tục”, “Hủy” chỉ thêm nếu engine thực sự hỗ trợ tương ứng (không dựng nút không hoạt động).

### 3.3. Kết quả quét

- Mỗi tệp bị phát hiện là **một dòng riêng**, không báo gộp thư mục cha.
- Tab kết quả có thể phân nhóm `Phát hiện / Nghi vấn / Chưa thể kiểm tra`.
- Mỗi dòng hiển thị tên, đường dẫn, phương pháp phát hiện, độ tin cậy, mức độ, hành động và thời điểm.
- Trường hợp tệp nén cho phép mở rộng entry và hiển thị đường dẫn ảo `Archive > Entry`.
- Nút “Cách ly ngay” chỉ được kích hoạt nếu có đối tượng vật lý có thể cách ly an toàn và đã xác định hành động phù hợp.

### 3.4. Cài đặt

Thêm các mục con theo thứ tự: **Chung**, **Bảo vệ**, **Lịch quét**, **Ngoại lệ**, **Cập nhật chữ ký**, **Quản lý chữ ký**, **Dữ liệu & công cụ**. Có thể dùng tab/nhóm trong màn hiện tại thay vì tạo quá nhiều cửa sổ mới.

### 3.5. Các nguyên tắc chung

- Font Segoe UI đủ lớn, không cắt chữ, bố cục thích ứng cửa sổ; kiểm thử tối thiểu 100%/125%/150% DPI.
- Thông báo ngắn, rõ, có “Xem chi tiết” khi nguyên nhân dài; không hiển thị lỗi SQL raw trực tiếp như thông báo chung.
- Không dùng màu đỏ cho mọi cảnh báo heuristic; phân biệt “đã xác định từ chữ ký” và “nghi vấn”.
- Không tự mở hoặc thực thi tệp nghi vấn sau quét.

---

## 4. ĐỀ XUẤT CẬP NHẬT CƠ SỞ DỮ LIỆU

### 4.1. Nguyên tắc

- **Giữ nguyên** các bảng đang dùng: `Settings`, `VirusSignatures`, `ScanHistory`, `ThreatDetections`, `ChunkSignatureGroups`, `ChunkSignatures` và dữ liệu hiện có.
- Tạo bản sao lưu `AntivirusDB` trước khi chạy migration.
- Chỉ sử dụng `ALTER TABLE`/`CREATE TABLE` có kiểm tra tồn tại để cập nhật; không chạy lại script `CREATE DATABASE` ban đầu lên CSDL đang dùng.
- Dữ liệu chữ ký cũ cần rà soát theo lộ trình. **Không tự vô hiệu hóa tất cả chữ ký cũ** hoặc lọc đồng loạt `VerificationStatus = Verified` trước khi hoàn thành chuyển đổi dữ liệu.
- Các bảng mới chỉ là **đề xuất thiết kế**, chưa có sẵn trong ZIP và chưa được kiểm thử tích hợp.

### 4.2. Bảng/cột đề xuất

| Đối tượng | Trường quan trọng | Chức năng |
|---|---|---|
| `VirusSignatures` (bổ sung) | `SourceName`, `VerificationStatus`, `VerifiedAt`, `VerifiedBy` | Quản lý độ tin cậy và nguồn chữ ký |
| `SignatureAuditLogs` | `AuditID`, `SignatureID`, `ActionType`, `OldValue`, `NewValue`, `Reason`, `ChangedAt`, `ChangedBy` | Theo dõi chỉnh sửa |
| `ScanSchedules` | `ScheduleID`, `Name`, `ScanType`, `TargetsJson`, `Frequency`, `StartTime`, `NextRunAt`, `IsEnabled` | Lập lịch quét |
| `ScanFileIssues` | `IssueID`, `ScanID`, `PhysicalPath`, `ArchiveEntryPath`, `IssueType`, `ErrorSummary`, `CreatedAt` | Tệp chưa kiểm tra/lỗi |
| `ScanSignatureMetadata` | `ScanID`, `SignatureSource`, `SignatureVersion`, `LoadedAt`, `IsDegraded` | Truy vết nguồn chữ ký cho từng lần quét |
| `ScanExclusions` | `ExclusionID`, `SHA256`, `FilePath`, `Scope`, `Reason`, `ExpiresAt`, `IsActive` | Ngoại lệ có kiểm soát |
| `QuarantineOperations` | `OperationID`, `DetectionID`, `OperationType`, `BeforeStatus`, `AfterStatus`, `Result`, `ErrorSummary`, `CreatedAt` | Điều tra lỗi cách ly/khôi phục |
| `SecurityHealthChecks` (tùy chọn) | `CheckID`, `CheckedAt`, `CheckType`, `Status`, `Details` | Lưu kết quả kiểm tra bảo mật Windows |

### 4.3. Ví dụ migration cho trạng thái xác minh chữ ký

> **Chỉ là mã SQL tham khảo để phát triển.** Kiểm tra dữ liệu và sao lưu trước khi áp dụng. Các bản ghi cũ được ghi `Legacy`, chưa tự động xác nhận là `Verified`. Importer mới phải đặt `PendingReview` và `IsActive = 0` một cách tường minh.

```sql
USE AntivirusDB;
GO

IF COL_LENGTH('dbo.VirusSignatures', 'SourceName') IS NULL
BEGIN
    ALTER TABLE dbo.VirusSignatures
    ADD SourceName NVARCHAR(100) NULL;
END;
GO

IF COL_LENGTH('dbo.VirusSignatures', 'VerificationStatus') IS NULL
BEGIN
    ALTER TABLE dbo.VirusSignatures
    ADD VerificationStatus VARCHAR(30) NOT NULL
        CONSTRAINT DF_VirusSignatures_VerificationStatus DEFAULT 'Legacy';
END;
GO

IF COL_LENGTH('dbo.VirusSignatures', 'VerifiedAt') IS NULL
BEGIN
    ALTER TABLE dbo.VirusSignatures
    ADD VerifiedAt DATETIME2 NULL;
END;
GO

IF COL_LENGTH('dbo.VirusSignatures', 'VerifiedBy') IS NULL
BEGIN
    ALTER TABLE dbo.VirusSignatures
    ADD VerifiedBy NVARCHAR(100) NULL;
END;
GO

IF OBJECT_ID('dbo.SignatureAuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SignatureAuditLogs
    (
        AuditID BIGINT IDENTITY(1,1) PRIMARY KEY,
        SignatureID BIGINT NOT NULL,
        ActionType VARCHAR(40) NOT NULL,
        OldValue NVARCHAR(1000) NULL,
        NewValue NVARCHAR(1000) NULL,
        Reason NVARCHAR(1000) NULL,
        ChangedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        ChangedBy NVARCHAR(100) NULL,
        CONSTRAINT FK_SignatureAuditLogs_VirusSignatures
            FOREIGN KEY (SignatureID) REFERENCES dbo.VirusSignatures(SignatureID)
    );
END;
GO
```

### 4.4. Ví dụ bảng lịch quét

```sql
USE AntivirusDB;
GO

IF OBJECT_ID('dbo.ScanSchedules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ScanSchedules
    (
        ScheduleID BIGINT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        ScanType VARCHAR(20) NOT NULL,
        TargetsJson NVARCHAR(MAX) NULL,
        Frequency VARCHAR(20) NOT NULL,
        StartTime DATETIME2 NOT NULL,
        NextRunAt DATETIME2 NULL,
        LastRunAt DATETIME2 NULL,
        IsEnabled BIT NOT NULL DEFAULT 1,
        RunIfMissed BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2 NULL
    );
END;
GO
```

**Lưu ý triển khai:** `TargetsJson` cần được ứng dụng xác thực trước khi dùng làm tham số quét; lịch chạy trên máy cụ thể cần bổ sung định danh máy nếu SQL Server phục vụ nhiều máy. Cần kiểm soát quyền tạo Task Scheduler, đường dẫn thực thi và tham số chạy, không cho người dùng không tin cậy tùy ý chạy lệnh hệ thống.

---

## 5. VỊ TRÍ MÃ NGUỒN CẦN CHỈNH SỬA

| Thành phần trong ZIP | Hướng triển khai |
|---|---|
| `Services/ScanEngine.cs` | Tách đối tượng quét vật lý/entry; chuẩn hóa trạng thái tệp; củng cố tính nhất quán cách ly/khôi phục; giữ khả năng quét từng tệp đệ quy |
| `Services/HybridSignatureScanner.cs` | Hỗ trợ kiểm tra từ stream khi quét entry nén; snapshot offline đã xác minh; cập nhật sau thay đổi chữ ký |
| `Services/StaticHeuristicAnalyzer.cs` | Hiện chỉ đọc tối đa 1 MiB đầu tệp; bổ sung các vùng phân tích chọn lọc khi cần, không tải toàn bộ tệp lớn lên RAM |
| `Services/RealTimeProtection.cs` | Bắt watcher `Error`, tạo hàng đợi, retry tệp chưa ổn định, quét bù |
| `Services/GuardService.cs` | Đồng bộ đường dẫn `DetectionID.qtn`, công khai trạng thái các lớp bảo vệ, mở rộng quy tắc giám sát hành vi |
| `Services/BehavioralActivityAnalyzer.cs` | Bổ sung bộ đánh giá theo cửa sổ thời gian và chứng cứ hành vi ransomware |
| `Services/ScanHistoryStore.cs` | Nhật ký kết quả chi tiết, trạng thái SQL lỗi, liên kết dữ liệu quét và nguồn chữ ký |
| `Services/QuarantineLedger.cs` | Đối soát metadata và tệp vật lý, bổ sung nhật ký thao tác và cơ chế sửa sai |
| `Services/MalwareBazaarSignatureUpdater.cs` | Lưu nguồn và phiên bản dữ liệu cập nhật, kiểm tra xung đột/chuỗi xác minh |
| `Services/SuspiciousSampleImporter.cs` | Nhập mẫu ở trạng thái chờ xét duyệt; phân biệt nhập danh sách hash và quét tệp nén |
| `Services/SubmittedSignatureListImporter.cs` | Tránh mặc định bật chữ ký mới chưa xác minh; ghi audit |
| `Database/VirusSignatureRepository.cs` | Truy vấn theo trạng thái xác minh, nguồn, hash, lịch sử và quản trị chữ ký |
| `Database/DbConnection.cs` | Đọc cấu hình từ nơi được bảo vệ, test kết nối, xử lý kết nối thất bại; không phụ thuộc cứng vào một instance khi triển khai |
| `Control/SampleSubmissionForm.cs` | Đổi mặc định checkbox `IsActive`, thêm thông báo rõ về mức độ xác minh |
| `Control/UcCachLy.cs` | Kiểm tra tình trạng vật lý, khôi phục đến vị trí khác, hiển thị lỗi cụ thể |
| `Control/UcTongQuan.cs` và các partial | Thống kê tệp chưa kiểm tra, mục tệp nén, trạng thái bảo vệ hạn chế |
| `Control/UcCaiDat.cs` | Bổ sung lịch quét, ngoại lệ, quản lý chữ ký, theo dõi sức khỏe các lớp bảo vệ |
| `Tests/*` | Unit/integration tests, benchmark, kiểm thử SQLite/local cache nếu được chọn cho chế độ dự phòng, và kiểm thử Windows thực |

### 5.1. Các lớp mới nên có

```text
Services/
├── ArchiveScanService.cs
├── ArchiveScanLimits.cs
├── ScanItemResult.cs
├── SignatureVerificationService.cs
├── OfflineSignatureSnapshotService.cs
├── ScanSchedulerService.cs
├── ScanExclusionService.cs
├── ScanCoverageTracker.cs
├── SecurityHealthCheckService.cs
└── ApplicationDiagnosticService.cs

Database/
├── ScanScheduleRepository.cs
├── ScanIssueRepository.cs
├── ScanExclusionRepository.cs
└── SignatureAuditRepository.cs

Control/
├── ScanScheduleForm.cs
├── SignatureManagementForm.cs
├── ScanExclusionForm.cs
└── SecurityHealthForm.cs
```

**Nguyên tắc:** Đây là gợi ý cấu trúc, không bắt buộc sử dụng đúng tên; ưu tiên tận dụng service/repository hiện có, tránh viết trùng logic.

---

## 5.2. Kiến trúc triển khai chung cho 14 chức năng

**Nguyên tắc giữ tương thích:** không thay chữ ký của các phương thức UI/service công khai đang dùng khi chưa lập danh sách điểm gọi; ưu tiên thêm overload hoặc adapter. Một `ScanCoordinator` có thể tập hợp F01/F02/F03/F04/F06/F09/F13 nhưng tránh một class xử lý mọi việc. `ScanEngine` đảm nhận điều phối quét; repository đảm nhận SQL; WinForms chỉ gọi dịch vụ và hiển thị kết quả.

```text
WinForms (UcTongQuan / UcCaiDat / UcLichSu / UcCachLy)
                |
         Application Services
                |
 ScanCoordinator + ScanCoverageTracker + ScanPolicy
    |               |                 |
 ArchiveScan      FullHash/CDC       Heuristic/YARA (tùy chọn)
    |               |                 |
    +---------------+-----------------+
                    |
              ScanItemResult
                    |
             Result / Alert / UI
                    |
 Repository SQL --- OfflineSnapshot / OfflineQueue
                    |
             QuarantineService
```

### 5.2.1. Hợp đồng dữ liệu nên dùng chung

```csharp
// MẪU THIẾT KẾ, không phải mã đã ghép vào dự án.
public enum ScanObjectKind { PhysicalFile, ArchiveEntry }
public enum ScanItemStatus {
    Discovered, Clean, Detected, Suspicious,
    Unreadable, Unsupported, SkippedByPolicy, Cancelled
}
public sealed class ScanItemResult {
    public Guid SessionId { get; set; }
    public ScanObjectKind ObjectKind { get; set; }
    public string PhysicalPath { get; set; }
    public string ArchiveEntryPath { get; set; }  // null nếu là tệp vật lý
    public ScanItemStatus Status { get; set; }
    public string DetectionMethod { get; set; }
    public string Reason { get; set; }
    public string SignatureVersion { get; set; }
    public long BytesExamined { get; set; }
}
```

`ScanItemResult` tách rõ **vị trí dữ liệu**, **mức độ phát hiện** và **khả năng kiểm tra**. Những chỗ chưa kiểm tra phải có nguyên nhân riêng. **Cần tránh** lưu trực tiếp toàn bộ đường dẫn riêng tư trong dịch vụ gửi báo cáo ra ngoài.

### 5.2.2. Thứ tự tích hợp gợi ý

1. Chốt `ScanItemStatus`, `ScanItemResult`, kiểm kê F06 trước để những module khác dùng thống nhất.
2. Khắc phục F02 và F05 (an toàn dữ liệu) trước khi thêm các hành động tự động.
3. Xây F01, F03, F04; ở mỗi bước chạy lại bộ kiểm thử hash/CDC/heuristic hiện tại.
4. Thêm F07, F09, F10, F11; bảo đảm tính năng cấu hình/đổi chính sách ảnh hưởng cả quét thủ công và realtime.
5. F08, F12, F13 là các lớp bằng chứng nâng cao, để sau khi có bộ đo F14 ổn định.

### 5.2.3. Thay đổi dữ liệu theo migration an toàn

- Viết migration theo phiên bản (ví dụ `001_signature_review.sql`, `002_scan_file_issues.sql`, `003_scan_schedules.sql`), chạy trên bản sao DB và sao lưu trước khi thực thi thật.
- Không tự tạo FK/index nếu chưa kiểm tra tên và kiểu cột thực có; kiểm tra `INFORMATION_SCHEMA` hoặc `sys.columns` trên `AntivirusDB` đang sử dụng.
- Với F02, làm sạch dữ liệu trùng trước khi tạo index unique. Với F03, queue cục bộ cần `LocalOperationId`/idempotency; với F05, cần một nhật ký thao tác không mất sau khi ứng dụng tắt đột ngột.
- Tất cả các bảng dự kiến trong tài liệu là **đề xuất**, không phải xác nhận đã tồn tại trong dự án. Khi viết migration cần làm `IF NOT EXISTS` và chạy thử trên DB test.

### 5.2.4. Chuẩn thông báo kết quả

| Tình huống | Thông báo nên hiển thị | Không nên hiển thị |
|---|---|---|
| Phát hiện theo hash đã xác minh | “Phát hiện tệp khớp chữ ký mã độc” | “Đã diệt hoàn toàn mọi nguy cơ” |
| Chỉ có heuristic/YARA | “Tệp có dấu hiệu đáng nghi — cần xem xét” | “Chắc chắn là virus” |
| ZIP có entry mã hóa | “Chưa thể kiểm tra toàn bộ nội dung tệp nén” | “Tệp an toàn” |
| SQL lỗi, snapshot hợp lệ | “Đang dùng chữ ký ngoại tuyến từ [ngày]” | “Bảo vệ đầy đủ như bình thường” |
| SQL và snapshot đều không dùng được | “Khả năng phát hiện bị hạn chế” | “Không phát hiện nên an toàn” |
| Quarantine ghi DB lỗi | “Cách ly chưa hoàn tất; cần đối soát” | “Đã cách ly thành công” |

---

## 6. LỘ TRÌNH THỰC HIỆN VÀ THỨ TỰ ƯU TIÊN

### Giai đoạn A — An toàn và tính đúng đắn (P0)

1. **F02**: Đổi chính sách chữ ký mặc định, lưu nguồn và xác minh.
2. **F05**: Củng cố kiểm tra khôi phục/cách ly, sửa chênh lệch đường dẫn `.qtn` nếu xác nhận có tác động thực tế.
3. **F06**: Hiển thị số tệp lỗi/chưa quét, làm đúng trạng thái kết quả.
4. **F03**: Snapshot chữ ký dự phòng và cơ chế hoạt động suy giảm có thông báo trung thực.
5. **F04**: `FileSystemWatcher.Error`, hàng đợi xử lý, quét bù.
6. **F01**: Quét từng entry của archive, xử lý ZIP an toàn, sau đó mở rộng định dạng.

**Kết quả bắt buộc:** không bỏ qua lỗi âm thầm, không kích hoạt chữ ký chưa xác minh mặc định, không tự suy diễn “sạch”, cách ly/khôi phục nhất quán, quét được tệp con trong ZIP.

### Giai đoạn B — Tính năng người dùng (P1)

1. **F07**: Quét theo lịch.
2. **F09**: Ngoại lệ kiểm soát theo hash.
3. **F11**: Chi tiết lịch sử, nhật ký, xuất báo cáo.
4. **F10**: Kiểm tra bảo mật Windows.
5. **F14**: Bộ kiểm thử hồi quy và đo hiệu năng cho tất cả thay đổi.
6. **F08**: Cảnh báo ransomware dựa trên chuỗi sự kiện.

**Kết quả bắt buộc:** người dùng kiểm soát được quét định kỳ, ngoại lệ, xem rõ trạng thái và truy vết sự kiện; có bằng chứng kiểm thử đáng tin cậy.

### Giai đoạn C — Nghiên cứu chuyên sâu (P2)

1. **F12**: Chữ ký số/metadata PE.
2. **F13**: Bộ luật YARA tùy chọn, cơ chế chạy cô lập.
3. Nghiên cứu Windows Service hoặc file-system minifilter riêng nếu thật sự cần bảo vệ ở mức sâu; đánh giá chi phí ký driver, tương thích Windows và yêu cầu phân phối trước khi triển khai.

---

## 7. CHECKLIST NGHIỆM THU PHIÊN BẢN NÂNG CẤP

### Độ tin cậy của quét

- [ ] Quét đủ các tệp trong thư mục con mà ứng dụng có quyền truy cập.
- [ ] Tệp bị lỗi hoặc không hỗ trợ được liệt kê rõ và không tính là sạch.
- [ ] Kết quả riêng từng tệp/entry, đường dẫn chính xác.
- [ ] Phương pháp khớp chữ ký dừng đúng theo thiết kế; heuristic chỉ cảnh báo nghi vấn.
- [ ] Hỗ trợ tệp lớn theo stream, không tăng RAM vô hạn.

### An toàn hệ thống

- [ ] Mẫu nhập mới không mặc định kích hoạt `IsActive=1`.
- [ ] Không làm mất tệp khi cách ly/khôi phục gặp lỗi SQL hoặc lỗi ổ đĩa.
- [ ] Không ghi đè đường dẫn khôi phục đang tồn tại.
- [ ] Không tự chạy nội dung độc hại hoặc tự tải lên dịch vụ ngoài mà chưa xin phép.
- [ ] Không lộ API key, đường dẫn riêng tư hoặc nội dung mẫu trong log/báo cáo công khai.

### Chức năng bảo vệ

- [ ] Lỗi watcher được phát hiện và có chiến lược quét bù.
- [ ] Thông báo rõ phạm vi/thời gian/khả năng bảo vệ còn hoạt động.
- [ ] SQL gián đoạn có chế độ dự phòng an toàn hoặc cảnh báo suy giảm.
- [ ] Các tính năng mới có nút bật/tắt thật và dữ liệu lưu bền vững.

### Kiểm thử và trải nghiệm

- [ ] Có test tự động, log test và hướng dẫn tái hiện kết quả.
- [ ] Kiểm tra giao diện ở DPI 100%, 125%, 150% và trên Windows mục tiêu.
- [ ] Không treo UI khi quét thư mục lớn hoặc cập nhật chữ ký.
- [ ] Có thể hủy các thao tác dài; giải phóng worker và tài nguyên.
- [ ] Có thể giải thích bằng chứng của từng phát hiện và phân biệt nhận diện nhầm.

---

## 8. ĐỀ XUẤT CHỌN PHẠM VI CHO ĐỒ ÁN

Nếu thời gian thực hiện còn hạn chế, **không nên triển khai đồng thời tất cả**. Nên tập trung vào **06 hạng mục cốt lõi** sau:

1. **Quét sâu nội dung tệp ZIP**, phát hiện từng entry, có giới hạn tài nguyên.
2. **Quản lý chữ ký đã/chưa xác minh**, chống dương tính giả từ mẫu người dùng gửi.
3. **Tăng độ tin cậy bảo vệ thời gian thực**, hỗ trợ quét bù.
4. **Hoàn thiện cách ly và khôi phục**, có đối soát tệp vật lý và thông báo lỗi.
5. **Quét theo lịch**, lưu lịch sử và kết quả đầy đủ.
6. **Bộ kiểm thử chất lượng phát hiện**, có bảng số liệu và các ca kiểm thử lỗi.

Sau đó, nếu còn nguồn lực, triển khai **chế độ dự phòng khi SQL mất kết nối**, **ngoại lệ theo hash**, **kiểm tra bảo mật Windows**, rồi mới nghiên cứu YARA/chống ransomware nâng cao.

### Giá trị mang lại khi báo cáo/bảo vệ chuyên đề

- Chứng minh sự khác nhau giữa phát hiện **theo hash toàn tệp**, **CDC/chunk** và **heuristic**.
- Minh họa khả năng phát hiện chính xác **một tệp độc hại nằm trong ZIP hoặc thư mục lớn**.
- Trình bày phương án giảm **false positive**, lý do không tự cách ly mẫu chỉ nghi vấn.
- Thể hiện độ tin cậy của hệ thống qua tình huống **mất SQL, lỗi I/O, tệp bị khóa, hủy quét**.
- Có bảng **test case, kết quả thực nghiệm, thời gian quét, độ bao phủ và bộ nhớ** thay vì chỉ ảnh chụp giao diện.

---

## 9. GHI CHÚ KỸ THUẬT VÀ AN TOÀN

- **Windows 7/10/11:** cần kiểm thử riêng mức hỗ trợ API Windows, TLS, thư viện nén, Task Scheduler, quyền ứng dụng và package .NET Framework. Không mặc định mọi chức năng nâng cao chạy như nhau trên ba thế hệ hệ điều hành.
- **VirusTotal:** ưu tiên tra cứu hash; việc tải file lên dịch vụ ngoài cần sự đồng ý rõ ràng vì có thể chứa tài liệu cá nhân. Kết quả VirusTotal không phải nhãn sự thật tuyệt đối.
- **MalwareBazaar:** dữ liệu cập nhật cần có nguồn, thời gian đồng bộ, kiểm tra định dạng, độ tin cậy và quy trình rollback khi nhập lỗi.
- **SQL Server:** ứng dụng dùng trên nhiều máy phải xác định rõ mô hình triển khai, thông tin kết nối, phân quyền đọc/ghi, sao lưu và đồng bộ; tránh cố định SQL `localhost\SQLEXPRESS` cho bản phát hành mọi máy.
- **Kiểm thử malware:** chỉ sử dụng môi trường cách ly, mẫu hợp pháp và các tệp thử vô hại; không thử mã độc thực trên máy học tập/làm việc hằng ngày.

**Kết luận:** Thứ tự ưu tiên nên là **đúng và an toàn trước, tự động hóa sau, mở rộng kỹ thuật phát hiện cuối cùng**. Phần mềm sẽ có chất lượng tốt hơn khi mọi tệp chưa kiểm tra đều được báo rõ, mọi quyết định cách ly có chứng cứ và mọi lỗi hệ thống có thể truy vết, thay vì chỉ có thêm nhiều nút chức năng.
