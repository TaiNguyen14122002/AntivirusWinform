# 🛡️ ScanAndRemoveVirus

> Phần mềm diệt virus **mô phỏng** viết bằng **C# WinForms (.NET Framework 4.7.2)** —
> nhưng engine quét là **THẬT**: 3 kỹ thuật phát hiện, quét song song có cache tăng tốc,
> cách ly/khôi phục tệp, bảo vệ thời gian thực và **tra cứu VirusTotal qua API**.

**Mục lục**

1. [Chạy ứng dụng](#-chạy-ứng-dụng)
2. [Hướng dẫn từng tab](#-hướng-dẫn-từng-tab)
   - [Tab Tổng quan](#1-tổng-quan--uctongquan)
   - [Tab Bảo vệ](#2-bảo-vệ--ucbaove)
   - [Tab Lịch sử](#3-lịch-sử--uclichsu)
   - [Tab Cách ly](#4-cách-ly--uccachly)
   - [Tab Cài đặt](#5-cài-đặt--uccaidat)
3. [VirusTotal API — hướng dẫn đầy đủ](#-virustotal-api--hướng-dẫn-đầy-đủ)
4. [3 kỹ thuật quét virus](#-3-kỹ-thuật-quét-virus--trong-app)
5. [Kiến trúc & dữ liệu](#-kiến-trúc--dữ-liệu)
6. [Kiểm thử tự động](#-kiểm-thử-tự-động)
7. [Hướng phát triển](#-hướng-phát-triển)

---

## 🚀 Chạy ứng dụng

**Yêu cầu:** Windows 10/11 (có sẵn .NET Framework 4.7.2) · Visual Studio 2019/2022 workload *.NET desktop development*

```powershell
# Cách 1 — Visual Studio: mở ScanAndRemoveVirus\ScanAndRemoveVirus\ScanAndRemoveVirus.csproj → F5

# Cách 2 — dòng lệnh:
msbuild ScanAndRemoveVirus\ScanAndRemoveVirus\ScanAndRemoveVirus.csproj /p:Configuration=Debug
ScanAndRemoveVirus\ScanAndRemoveVirus\bin\Debug\ScanAndRemoveVirus.exe

# Cách 3 — bản build có sẵn: chạy trực tiếp file .exe ở trên
```

> ⚠️ Đây là bài tập lớn mô phỏng giao diện + engine giáo dục. Chữ ký cục bộ là chuỗi thử nghiệm
> (`XVIRUS-TEST-SIGNATURE::`, mẫu EICAR nhận theo tên) — **không** phải CSDL virus thương mại.
> Tất cả 10 tính năng ở tab Bảo vệ đều là cơ chế **thật** (watcher/WMI/MOTW/registry/VirusTotal).

> 🎨 **Ngôn ngữ thiết kế chung (chuẩn = tab Lịch sử):** mỗi tab có page-header (tên 18pt Bold + phụ đề xám),
> GroupBox dạng card (nhãn 10.125 Bold xanh brand `#0A3E8C` trên nền trắng), lưới header 40px xanh nhạt,
> nút theo 5 vai trò `Theme.BtnRole` (kể cả ô chọn dạng checkbox trong lưới Lịch sử). Ép buộc bằng test
> `UiEndToEnd` section 9b — thêm tab mới chỉ cần gọi `Theme.StylePageHeader/StyleCard/StyleGrid`.

---

## 📖 Hướng dẫn từng tab

Thanh bên trái có 5 nút: **Tổng quan · Bảo vệ · Lịch sử · Cách ly · Cài đặt**.
Mỗi nút mở đúng một tab riêng (`UcTongQuan`, `UcBaoVe`, `UcLichSu`, `UcCachLy`, `UcCaiDat`). Bấm số **"Cách ly"** ở thẻ Thống kê (tab Tổng quan) cũng nhảy thẳng sang tab Cách ly.

> 💡 **Không có virus thật để test?** Bộ 6 tệp mock **VÔ HẠI đã nằm sẵn trong repo** tại `ScanAndRemoveVirus\Samples\`.
> Cần tạo mới/restore? Mở tab **Cài đặt** → bấm **"Tạo tệp mẫu 3 kỹ thuật"**.
> Gồm: 3 tệp khớp kỹ thuật 1 (chữ ký prefix / hash SHA256 / tên `eicar`), 2 tệp khớp kỹ thuật 2
> (đuôi kép `.pdf.exe`, PowerShell độc), **1 tệp sạch đối chứng** (không được phép báo).
> Quét thư mục đó (Quét tùy chọn → Chọn thư mục → `Samples`) → app phải trả về **đúng 5** đe dọa.

### 1. Tổng quan — `UcTongQuan`

| Vùng | Cách dùng |
|---|---|
| **🟢 Trạng thái bảo vệ** | Hiển thị số liệu **thật**: trạng thái *Bảo vệ thời gian thực* (đồng bộ tức thì khi bật/tắt ở tab Bảo vệ), trạng thái CSDL + ngày cập nhật cuối, phiên bản app. Bấm **"Kiểm tra cập nhật"** → đóng dấu ngày giờ mới vào `dbupdate.txt` **và xóa cache quét** để lần quét kế tiếp kiểm tra lại toàn bộ với "chữ ký mới". |
| **🔍 Quét hệ thống** | Chọn 1 trong 3 kiểu: **Quét nhanh** (Desktop + Downloads + Temp) · **Quét toàn bộ** (mọi ổ cố định) · **Quét tùy chọn** — bấm **"Chọn tệp"** hoặc **"Chọn thư mục"** để chỉ định (đường dẫn hiện ngay dưới ô chọn; quét được *một tệp lẻ* lẫn thư mục đệ quy). Bấm **"Quét ngay"**. |
| **⏹ Đang quét** | Nút đổi thành **"Hủy quét"** (màu đỏ), progress chạy, nhãn hiển tệp đang xử lý + số tệp đã quét theo thời gian thực. Bấm giữa chừng → phiên dừng **an toàn**, lịch sử không ghi phiên dở. Quét xong: progress đầy + `Hoàn tất — N tệp trong x giây`. |
| **📊 Thống kê** | 4 thẻ số liệu **thật**: Mối đe dọa của phiên gần nhất (xanh = 0, đỏ khi > 0) · Tệp đã quét · Lần quét gần nhất (tự khôi phục từ lịch sử sau khi mở lại app — không còn dữ liệu ảo) · Số tệp đang cách ly → **bấm vào số để mở tab Cách ly**. |
| **⚡ Hành động** | Sau khi quét, bảng liệt kê mọi phát hiện kèm **loại + lý do**, ví dụ `Chữ ký: SHA256 nội dung khớp chữ ký Malsim.Sample.Hash` hoặc `Heuristic: Nghi vấn 70/100: đuôi kép giả mạo tài liệu (pdf.exe)`. Chọn dòng (Chuột trái / Ctrl / Shift chọn nhiều) rồi dùng 5 nút: **Cách ly đã chọn · Xóa đã chọn · Cách ly tất cả · Xóa tất cả · Tra VirusTotal**. *Cách ly* dời tệp vào vùng cách ly (khôi phục được); *Xóa* là vĩnh viễn, có hộp xác nhận. |

### 2. Bảo vệ — `UcBaoVe`

* **Bảng "Tính năng bảo vệ"** — **cả 10 hàng đều là công tắc THẬT** (bấm nút cột cuối; trạng thái lưu vào `settings.ini`, nhớ qua lần mở app):

| Hàng | Cơ chế thật phía sau |
|---|---|
| Bảo vệ thời gian thực | `FileSystemWatcher` Desktop/Downloads/Temp — tệp mới/sửa được quét tức thì bằng chữ ký + heuristic |
| Bảo vệ tệp | Quét **lại** tệp cách ly ngay trước khi cho Khôi phục về máy — chặn malware quay ngược vào hệ thống |
| Bảo vệ USB | Timer phát hiện **ổ removable vừa cắm** → tự quét nguyên ổ, kết quả vào Hành động + lịch sử |
| Bảo vệ tải xuống | Watcher Downloads soi **MOTW (`Zone.Identifier` của trình duyệt)** → quét *đầy đủ nội dung* tệp tải từ Internet, kể cả đuôi lạ |
| Phát hiện hành vi đáng ngờ | **WMI `Win32_Process`**: tiến trình chạy từ %TEMP% (dropper), PowerShell `-enc`/`iex(`/`DownloadString`, Office (winword/excel/outlook…) sinh shell — đúng mẫu chuỗi khai thác macro |
| Bảo vệ thư mục khởi động | Watcher 2 thư mục StartUp (user + common) — tệp lạ rơi vào = persistence → quét + cảnh báo |
| Tự động cách ly | Gộp pipeline mọi guard: phát hiện là dời thẳng vào Quarantine kèm lý do, không hỏi |
| Cảnh báo mối đe dọa | Công tắc pop-up chung — tắt thì mọi phát hiện vẫn ghi lịch sử, im lặng tuyệt đối |
| Tự động cập nhật | Mở app mà tem chữ ký quá 24h → tự đóng tem mới + xóa cache để quét lại toàn bộ |
| Bảo vệ web (VirusTotal) | dòng `Heuristic` sau phiên quét → **tự tra hash trên VirusTotal** tối đa 2 dòng/phiên, cách 16s (giữ ngưỡng 4 req/phút) — cần API key |

* **Panel "Trạng thái chi tiết"** — 7 chỉ số đều đọc từ nguồn thật: phiên bản CSDL (= version assembly), ngày cập nhật cuối (`dbupdate.txt`), lần quét thời gian thực gần nhất, **tổng số tệp đã quét** và **tổng đe dọa đã chặn** (tính từ toàn bộ lịch sử).
* **Vùng Cài đặt** chuyển thành **tab riêng** — xem mục "Cài đặt — `UcCaiDat`" bên dưới.

### 3. Lịch sử — `UcLichSu`

Toàn bộ là **dữ liệu thật** từ `scanhistory.log` (mới nhất hiện trước):

| Tab con | Nội dung |
|---|---|
| **Lịch sử quét** | Mọi phiên quét (loại, phạm vi, số tệp, số đe dọa, thời lượng, kết quả) |
| **Chỉ mối đe dọa** | Lọc còn các dòng có ≥ 1 phát hiện — gồm cả cảnh báo của bảo vệ thời gian thực |
| **Cập nhật** | Nhật ký sự kiện "Kiểm tra cập nhật" CSDL |

Nút: **Xem chi tiết** (hộp thoại đầy đủ thông tin dòng đang chọn) · **Làm mới** · **Xuất CSV…** (phân cách bằng `;`, kèm BOM — mở trực tiếp bằng Excel không lỗi font). Tab tự làm mới mỗi lần bạn chuyển vào.

### 4. Cách ly — `UcCachLy`

Danh sách **thật** các tệp đang bị giữ trong `%AppData%\ScanAndRemoveVirus\Quarantine\` — kèm cột *đường dẫn gốc, mối đe dọa (lý do + loại), thời gian, kích thước*:

* **Khôi phục / Khôi phục tất cả** — trả tệp về **đúng đường dẫn cũ**; nếu vị trí đã có file trùng tên, tự đặt hậu tố ` (1)` — không bao giờ ghi đè mất dữ liệu.
* **Xóa vĩnh viễn / Xóa tất cả** — hành động không hoàn tác, có hộp xác nhận Yes/No.
* **Làm mới** — và tab tự refresh mỗi khi có biến cố cách ly từ tab khác.
* Sổ cái `quarantine.log`: bạn xóa tay file trong thư mục Quarantine thì dòng tương ứng **tự biến mất** khỏi bảng (tự dọn "dòng ma").

---

### 5. Cài đặt — `UcCaiDat`

Tab riêng (nút **Cài đặt** trên sidebar):

* ☑ *Tự động khởi động cùng Windows* — **thật 100%**: ghi/xóa `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (không cần admin).
* ☑ *Tự động cập nhật* · ☑ *Gửi mẫu ẩn danh* · ☑ *Hiển thị thông báo* — lưu vào `settings.ini`; **đồng bộ hai chiều** với các hàng "Tự động cập nhật"/"Cảnh báo mối đe dọa" của tab Bảo vệ.
* **"Lưu thiết lập"** — ghi file + áp dụng autostart ngay; registry từ chối thì checkbox tự nhả về đúng thực tế + cảnh báo.
* **"Tạo tệp mẫu 3 kỹ thuật"** — phục hồi 6 tệp vô hại trong `ScanAndRemoveVirus\Samples\` của repo (idempotent); nếu app chạy ngoài repo sẽ tạo ở `Desktop\XVirus-Samples`.

> ⚠️ Lưu ý khi tự chạy test: guard hành vi WMI coi "exe chạy từ %TEMP%" là dropper. Nếu app THẬT đang bật guard mà bạn compile + chạy binary test vào TEMP, nó sẽ cách ly chính file test. Tạm tắt guard (hoặc để harness test tự tắt như `UiEndToEnd.cs`).

---

## 🌐 VirusTotal API — hướng dẫn đầy đủ

### VirusTotal là gì và nó cho app thứ gì?

[VirusTotal](https://www.virustotal.com) (thuộc Google Chronicle) là dịch vụ đám mây nơi **~70 engine diệt virus** (Microsoft, Kaspersky, Bitdefender, ESET…) cùng phân tích một mẫu. App dùng nó như **"bảng chữ ký toàn cầu"** — thứ mà CSDL thử nghiệm cục bộ không có:

> Local quét ra `Heuristic: nghi vấn 90/100` — nhưng *nghi* thì chưa *kết luận* được.
> Bấm **Tra VirusTotal** → nhận phán quyết đồng thuận của 70 engine thật.

### Cách lấy API key miễn phí (~2 phút)

1. Đăng ký tài khoản miễn phí tại **virustotal.com** (nút *Create account* góc phải).
2. Đăng nhập → avatar góc phải → **Profile** → mục **API key** → *Reveal key* → sao chép (64 ký tự hex).
3. Trong app: ở khu **Hành động**, chọn 1 dòng đe dọa → bấm **"Tra VirusTotal"**.
   * **Lần đầu** (chưa có key) sẽ hiện hộp thoại *"API key VirusTotal"* → **dán key → Lưu**.
   * Key nằm ngay trong project tại `ScanAndRemoveVirus\ScanAndRemoveVirus\vtapikey.txt` — đã có trong `.gitignore` nên không bao giờ bị commit (khi app chạy ngoài repo, dự phòng là `%AppData%\ScanAndRemoveVirus\vtapikey.txt`).   * Có thể tự tạo file trên với nội dung = key nếu không muốn dùng hộp thoại.

### App gọi API như thế nào?

```
Chọn dòng đe dọa → bấm "Tra VirusTotal"
  1. ScanEngine.ComputeFileSha256(path)                 — tính hash TOÀN BỘ tệp (0 gói tin gửi đi)
  2. GET https://www.virustotal.com/api/v3/files/{sha256}
     Header: x-apikey: <key>                            — CHỈ hash rời khỏi máy
  3. VirusTotalClient.ParseReport()                     — đọc "last_analysis_stats":
     malicious / undetected / harmless / suspicious / timeout / failure
  4. Kết luận (cập nhật cột Mối đe dọa + nhãn progress + pop-up):
       ≥ 2 engine báo độc   →  ĐỘC HẠI          (báo đỏ)
       đúng 1 engine        →  nghi ngờ          (1 vendor đơn lẻ rất hay false positive)
       0 engine             →  AN TOÀN — 0/72
       HTTP 404             →  mẫu chưa có trên VT (thường là vô hại, KHÔNG chắc chắn 100%)
       HTTP 401/429/mạng    →  hiện thẳng lỗi, không làm hỏng phiên quét
```

**Mã nguồn:** `Services/VirusTotalClient.cs` (~160 dòng) + test offline trong `Tests/ScanEngineTest.cs` (dùng payload JSON đúng format API thật, vector SHA256 chuẩn NIST).

### Giới hạn gói miễn phí & cách app cư xử

| Giới hạn | Giá trị | App xử lý |
|---|---|---|
| Tốc độ | **4 request/phút** | không tự tra hàng loạt; chỉ chạy khi bạn bấm; gặp 429 → báo "đợi 1 phút" |
| Số lượng/ngày | **500 request/ngày** | đếm qua mỗi lần bấm |
| File chưa từng có trên cloud | 404 | báo "chưa có mẫu" — vì **app chỉ tra hash, không upload** |
| Key sai/thiếu | 401 / hướng dẫn | hộp thoại dán key hiện ra lần đầu; lỗi key sai hiển rõ |

**Vì sao không upload luôn?** Upload biến "chưa có mẫu" thành kết luận đầy đủ cho **zero-day**, nhưng: (1) *toàn bộ nội dung tệp rời khỏi máy bạn* và được chia cho cộng đồng vendor, (2) heuristic có thể chấm nhầm tài liệu thật của bạn, (3) free-tier giới hạn 32MB/tệp. App hiện chọn giải pháp riêng tư hơn; luồng upload có xác nhận từng tệp nằm trong mục phát triển.

### Mẹo dùng hiệu quả

* **Heuristic → VirutTotal**: dòng nào bị chấm điểm cao mà không trùng chữ ký cục bộ → tra VT để chốt.
* **Tệp vừa tải nghi ngờ**: Quét tùy chọn → *Chọn tệp* đúng file đó → xem dòng kết quả → Tra VirusTotal.
* Kết quả VT kèm **hash rút gọn 16 ký tự**; muốn xem báo cáo web đầy đủ: `https://www.virustotal.com/gui/file/<sha256-đầy-đủ>`.
* VT trả `undetected/nhãn` từng hãng nếu mở GUI web — app chỉ lấy con số đồng thuận cho nhẹ.

---

## 🧠 3 kỹ thuật quét virus — trong app

| # | Kỹ thuật | Trong project |
|---|---|---|
| 1 | **Chữ ký (Signature-based)** | `ScanEngine.CheckContent()` mở tệp **một lần**: so byte đầu với prefix thử nghiệm + tính **SHA256 toàn bộ** đối chiếu bảng `Signatures` (hash chuẩn **EICAR** + hash mẫu `Malsim.Sample.Hash`); luật tên chứa `eicar`. **Mở rộng cloud:** tra VirusTotal theo hash (chương 3). *Nhược: chỉ bắt được virus đã biết.* |
| 2 | **Heuristic** | `ScanEngine.HeuristicScore()` chấm điểm nghi vấn: đuôi kép giả tài liệu (`invoice.pdf.exe`, +70) · tên mồi câu xã hội (+30) · thực thi bị ẩn thuộc tính Hidden (+40) · exe ≤ 2KB nằm trong Temp (+40) · mã độc PowerShell/VBS (`iex(`, `-enc `, `DownloadString`, `FromBase64String`… tối đa 3 marker). Ngưỡng **60/100** → gắn cờ `Heuristic`. *Ưu: bắt mẫu chưa có chữ ký · Nhược: báo nhầm → đã có VT chốt.* |
| 3 | **Giám sát thời gian thực (Behavioral)** | `RealTimeProtection.cs`: `FileSystemWatcher` gác Desktop/Downloads/Temp; tệp mới/sửa → debounce 0.7 s → chạy nguyên pipeline lớp 1+2 → pop-up hỏi hoặc **tự cách ly**. Trạng thái hiển thị đồng bộ 2 tab. *(Chưa giám sát tiến trình đang chạy — cần WMI, xem lộ trình.)* |

**Tăng tốc:** chỉ mở tệp *thuộc diện nghi vấn* (whitelist phần mở rộng thực thi/kịch bản/văn bản, chặn cỡ ≤ 4MB) + **cache** `scancache.dat` theo `(mtime, size)` — tệp không đổi kể từ lần trước dùng lại kết quả, không đọc lại. Đo trên máy dev: quét nhanh 306k tệp từ **~290s (lạnh + Defender contention)** xuống **~9s (trạng thái cache)**.

---

## 🏗 Kiến trúc & dữ liệu

```
ScanAndRemoveVirus/
├── Program.cs                    # điểm vào ứng dụng
├── FrmMain.cs (+Designer)        # cửa sổ chính: sidebar điều hướng, nút Cài đặt, API MoTabCachLy()
├── Services/                     # TẦNG NGHIỆP VỤ — không phụ thuộc UI, test độc lập bằng csc
│   ├── ScanEngine.cs             #   quét song song theo lô + 3 lớp phát hiện + cache
│   ├── RealTimeProtection.cs     #   kỹ thuật 3 (FileSystemWatcher, anti-trùng-lặp)
│   ├── QuarantineLedger.cs       #   sổ cái cách ly (nội bộ, công khai qua ScanEngine)
│   ├── ScanHistoryStore.cs       #   lịch sử + tem cập nhật + số liệu dẫn xuất + export CSV
│   ├── VirusTotalClient.cs       #   API v3: GET /files/{sha256} + ParseReport offline-testable
│   ├── AppSettings.cs            #   settings.ini + đăng ký Run registry (autostart thật)
│   ├── FeatureFlags.cs           #   10 công tắc tính năng — một nguồn, persist settings.ini
│   ├── GuardService.cs           #   các guard: USB / Tải xuống(MOTW) / WMI hành vi / StartUp / auto-update
│   └── TestSamples.cs            #   bộ 6 tệp mock VÔ HẠI phủ 3 kỹ thuật (nút "Tạo tệp mẫu")
├── Control/                      # UI per-tab
│   ├── Theme.cs                  #   ✅ NGUỒN MÀU DUY NHẤT — mọi control tham chiếu Theme.X
│   ├── UcTongQuan.cs (+Designer) #   quét + hành động + VT + thống kê
│   ├── UcBaoVe.cs (+Designer)    #   10 công tắc guard thật + panel trạng thái
│   ├── UcLichSu.cs (+Designer)   #   3 tab lọc + chi tiết + xuất CSV
│   ├── UcCachLy.cs (+Designer)   #   danh sách + restore/delete/chọn/tất cả/refresh
│   └── UcCaiDat.cs (+Designer)   #   tab Cài đặt: 4 checkbox + lưu thiết lập + tạo tệp mẫu
├── form/                         # 3 form cũ chưa sử dụng (Form1, FrmBaoVe, FrmCachLy)
├── AntivirusDB.sql               # schema CSDL chữ ký thật (VirusSignatures) — chờ kết nối
└── Tests/                        # harness csc (xem mục Kiểm thử)
```

**Dữ liệu runtime** — tất cả trong `%AppData%\ScanAndRemoveVirus\`:

| File | Nội dung | Xóa thì sao |
|---|---|---|
| `Quarantine\*.qtn` | nội dung tệp đã cách ly (đổi tên GUID) | mất khả năng khôi phục |
| `quarantine.log` | sổ: id ↔ đường dẫn gốc ↔ lý do ↔ thời gian ↔ cỡ | bảng Cách ly tự dọn (prune) |
| `scanhistory.log` | mọi phiên quét/cảnh báo/RT/cập nhật (giữ 1000 dòng) | Lịch sử trống |
| `scancache.dat` | cache `đường dẫn → (mtime, size, độc?)` (≤ 500k mục) | quét lại từ đầu, chậm hơn |
| `dbupdate.txt` | tem ngày "cập nhật CSDL chữ ký" (kèm xóa cache khi bấm nút) | hiện "Chưa cập nhật" |
| `settings.ini` | 11 cờ: 4 cài đặt + 10 công tắc tính năng tab Bảo vệ (FeatureFlags) | về mặc định |
| `vtapikey.txt` (project, **gitignored**) | **API key VirusTotal** — nằm cạnh file .csproj; ngoài repo -> fallback AppData | bấm Tra VT sẽ hỏi lại key |

---

## ✅ Kiểm thử tự động

Hai harness **không** nằm trong build app — biên dịch trực tiếp mã nguồn bằng `csc` có sẵn trong Windows:

```powershell
cd ScanAndRemoveVirus
$csc = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\Roslyn\csc.exe" | Select-Object -First 1  # Roslyn: nguồn dùng C#7

# 1) Engine — 86 check: 3 kỹ thuật + cache + cách ly/ledger/restore xung đột,
#    history + CSV + tem CSDL + auto-update 24h, stress scan×hủy giữa chừng (8 vòng),
#    VT parse offline + vector SHA256 NIST, AppSettings + registry (tự backup/restore),
#    Guard thật: USB-simulate, Download-MOTW (ADS), StartUp, Restore-guard, WMI HÀNH VI LIVE
& $csc /nologo /out:eng.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  /r:System.Net.Http.dll /r:System.Management.dll `
  ScanAndRemoveVirus\Services\ScanEngine.cs   ScanAndRemoveVirus\Services\RealTimeProtection.cs `
  ScanAndRemoveVirus\Services\QuarantineLedger.cs ScanAndRemoveVirus\Services\ScanHistoryStore.cs `
  ScanAndRemoveVirus\Services\VirusTotalClient.cs ScanAndRemoveVirus\Services\AppSettings.cs `
  ScanAndRemoveVirus\Services\FeatureFlags.cs ScanAndRemoveVirus\Services\GuardService.cs `
  ScanAndRemoveVirus\Services\TestSamples.cs ScanAndRemoveVirus\Services\DataDir.cs `
  Tests\ScanEngineTest.cs
.\eng.exe        # kỳ vọng: == ALL TESTS PASSED ==   (~40s nếu WMI live hoạt động)

# 2) UI End-to-End — 88 check: dựng FrmMain + 5 UserControl THẬT, PerformClick TỪNG NÚT
#    (5 nút sidebar, quét + Hủy giữa phiên, radio loại trừ, VT flow với key giả,
#     cách ly/xóa CHỌN & TẤT CẢ, bộ mẫu 6 tệp -> đúng 5 threat, tab lịch sử (cột colPick chọn
#     nhiều + nút xóa mục đã chọn), chi tiết, làm mới, tab Cài đặt (8 checkbox live-apply, VTkey,
#     Khôi phục mặc định, Xóa cache, nhãn info), LẬT 2 CHIỀU 10 HÀNG tab Bảo vệ,
#     thống kê khởi động không còn mock 2025) — closer-thread tự bấm Có/OK cho MessageBox.
#    Danh sách file đầy đủ: xem comment đầu Tests\UiEndToEnd.cs
#    Matrix phủ nút: powershell -File Tests\coverage-matrix.ps1 -> 28/32 CLICKED;
#    4 nút còn lại mở HỘP THOẠI HỆ THỐNG không auto-safe (Chọn tệp, Chọn thư mục, Xuất CSV,
#    Mở thư mục dữ liệu) — logic phía sau chúng được test trực tiếp (SetCustomPath/ExportCsv/RefreshDataInfo).
# 3) Tests\ButtonAudit.cs — audit 20/20 nút đều đi qua Theme.StyleButton/StyleNav thống nhất
```

Toàn bộ chạy trong ~1–2 phút; kết quả hiện `PASS/FAIL` từng check.

---

## 🗺 Hướng phát triển

- [ ] Nạp chữ ký **thật** từ bảng `VirusSignatures` (`AntivirusDB.sql` đã có schema) thay chuỗi thử nghiệm → local tự bắt virus thật, giảm phụ thuộc mạng khi tra VT.
- [ ] Luồng **upload VT có kiểm soát**: `POST /files/upload_url` + poll `GET /analyses/{id}` cho tệp nghi vấn chưa có trên cloud — size-gate 32MB, xác nhận riêng tư *từng tệp*, hiện thanh chờ phân tích.
- [ ] Guard hành vi hiện **phát hiện + ghi log + cách ly tệp của tiến trình**; nâng cấp thành chặn/kill tiến trình đang chạy (cân nhắc vì dễ làm phiền phần mềm lành).
- [ ] Kết nối SQL Server thay các file log `%AppData%` (lịch sử/cách ly/cache/tem).
- [ ] Dọn: 3 form cũ trong `form/`; untrack `bin/`, `obj/`, `.vs/` khỏi git.

> ✅ Đã hoàn thành so với bản trước: **cả 10 hàng tab Bảo vệ là cơ chế thật** (USB auto-scan, tải-xuống-MOTW, WMI hành vi, StartUp guard, auto-isolate, auto-update, VT tự động), autostart ghi registry thật, lịch sử/cách ly/cài đặt đều là dữ liệu thật.
