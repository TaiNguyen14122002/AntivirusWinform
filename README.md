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

> 💡 **Không có virus thật để test?** Bộ 11 tệp mock **VÔ HẠI đã nằm sẵn trong repo** tại `TestSamples\`.
> Cần tạo mới/restore? Mở tab **Cài đặt** → bấm **"Tạo tệp mẫu 3 kỹ thuật"** (idempotent, tự đặt lại attribute Hidden cho mẫu exe ẩn).
> Gồm **8 đe dọa**:
> • KT1 (Chữ ký) ×4 — prefix `XVIRUS-TEST-SIGNATURE::` trong `mau-ky-hieu.txt` **và ở byte 0 của `hook-tien-ich.js`** (chứng minh soi nội dung không phân biệt đuôi), hash SHA256 `mau-hash-sha256.txt`, tên chứa `eicar` (`demo_eicar_named.dat`)
> • KT2 (Heuristic) ×4 — đuôi kép `thong-bao-hoa-don-invoice.pdf.exe`, PowerShell đa marker `update-flash.ps1`, VBS downloader `downloader-tien-ich.vbs` (iex + DownloadString + FromBase64String = 90/100), **exe ẩn + mồi câu** `thong-bao-crack-hidden.exe` (Hidden +40, "crack" +30 = 70/100)
> • **3 tệp sạch đối chứng** (không được phép báo): `keygen-pro.exe` (mồi câu đơn lẻ 30/100 — DƯỚI ngưỡng 60), `script-sach.ps1` (script lành không marker), `README-mau.txt`.
> Quét thư mục đó (**Quét nâng cao ▾ → Quét thư mục** → thêm `TestSamples` → **Bắt đầu quét**) → app phải trả về **đúng 8** đe dọa: 4 Chữ ký + 4 Heuristic.

### 1. Tổng quan — `UcTongQuan`

Giao diện thiết kế lại theo **3 trạng thái** nối tiếp nhau trong cùng một tab, chuẩn hoá theo `Theme` (page-header 18pt Bold + phụ đề xám, card bo góc, nút theo `Theme.BtnRole`):

**a) Trạng thái an toàn (mặc định / sau khi quét không thấy đe dọa)**

| Vùng | Nội dung |
|---|---|
| **Khối trạng thái trung tâm** | Icon khiên **xanh lá** dấu ✓ cỡ lớn · dòng chữ **"Máy tính của bạn được bảo vệ"** (xanh, đậm) · phụ đề xám **"Không phát hiện mối đe dọa."** · 2 nút cạnh nhau ngay bên dưới: **"▶ Quét ngay"** (xanh, `Theme.BtnRole` Primary) chạy thẳng **Quét nhanh** (Desktop + Downloads + Temp), không hỏi gì thêm; và **"⚙ Quét nâng cao ▾"** (viền, Secondary) — bấm mở **dropdown 4 mục** neo ngay dưới nút, mỗi mục gồm icon + tên + mô tả 1 dòng: **Quét toàn bộ hệ thống** *(Kiểm tra tất cả ổ đĩa và tệp)* · **Quét thư mục** *(Chọn thư mục để quét)* · **Quét tệp** *(Chọn một hoặc nhiều tệp để quét)* · **Quét tùy chỉnh** *(Cấu hình vị trí và loại tệp quét)* — chọn 1 mục sẽ điều hướng sang trang **"Quét nâng cao"** (mục d bên dưới), mở sẵn đúng chế độ vừa chọn. |
| **Hàng thống kê (3 thẻ)** | **Mối đe dọa**: `0` (xanh) + phụ đề "Không phát hiện" · **Tệp đã quét**: tổng số tệp của lần quét gần nhất · **Lần quét gần nhất**: `dd/MM/yyyy HH:mm` — cả 3 đọc từ `ScanHistoryStore`, tự khôi phục sau khi mở lại app. Hàng này **chỉ thuộc Tổng quan (a)/(b)**: mở **trang (c)** *Chi tiết kết quả quét* hoặc **trang (d)** *Quét nâng cao* — 2 màn hình riêng — thì hàng **biến mất** (xem ghi chú *lần 8* cho (c) và *lần 5* cho (d)). |
| **Hoạt động gần đây** | Bảng cuộn được chiếm chỗ thẻ *Tuỳ chọn quét nhanh* (đã bỏ — xem ghi chú cuối mục), **hàng giãn theo cửa sổ** (`Percent 100` — cao bao nhiêu tuỳ cửa sổ, không còn cố định 400px): mỗi dòng = **chấm màu** theo loại sự kiện (🔄 cập nhật CSDL, 🛡️✓ bảo vệ thời gian thực, 🔍 phiên quét — **đỏ** khi có đe dọa, **xanh** khi sạch) + **mô tả ngắn** + `dd/MM/yyyy HH:mm` căn phải. Nạp **tối đa 100 dòng mới nhất** từ `scanhistory.log` (mới nhất ở trên, cuộn được), kèm dòng tóm tắt *"N hoạt động gần nhất"* và liên kết **"Mở tab Lịch sử ›"** ở góc phải — không còn dữ liệu mẫu. |

> 🔧 **Thay đổi giao diện 25/09/2026:** thẻ **"Tuỳ chọn quét nhanh"** (3 radio *Quét nhanh / Quét toàn bộ / Quét tùy chọn* + 2 nút *Chọn tệp*/*Chọn thư mục* + ô đường dẫn) đã **được bỏ khỏi trang Tổng quan**.
> Nút **"Quét ngay"** giờ luôn chạy **Quét nhanh** (Desktop + Downloads + Temp); muốn quét toàn bộ ổ đĩa / theo thư mục / theo tệp / tùy chỉnh thì dùng **"Quét nâng cao ▾"** (đủ 4 chế độ, có sẵn phần chọn tệp & thư mục).
> Nhờ vậy trạng thái an toàn gọn hơn hẳn (lúc đó còn **4 khối** = 668px); sang **lần 4** dải trạng thái cũng bị gỡ → chỉ còn **3 khối** (hero · 3 thẻ số liệu · *Hoạt động gần đây*) và thẻ *Hoạt động gần đây* giãn hết chiều cao cửa sổ — xem ghi chú *lần 4* dưới.
>
> 🔧 **Thay đổi giao diện 25/09/2026 (lần 2)** — bỏ tiếp 3 thứ cho gọn trang:
> • **Thanh loading quét** (`pgbScan`) — dải trên cùng giờ chỉ còn **dòng trạng thái quét** ("Đang quét…" / "Hoàn tất — đã quét N tệp…" / "Đã hủy…" / "VirusTotal: …") + chip `Bảo vệ thời gian thực: Bật` và `Phiên bản: x.y.z`. *(25/09/2026, **lần 9**: loading được dựng lại theo thiết kế mới — **dải loading quét** `pnlLoadingQuet` với vòng xoay GDI+ `spinnerDangQuet` + nút "Hủy quét", hiện **ngay khi bấm BẤT KỲ nút quét nào**; xem ghi chú *lần 9* ở dưới.)*
> • **Cả khối "cập nhật dữ liệu"**: chip `CSDL virus: Đã cập nhật`, chip `Cập nhật cuối: dd/MM/yyyy HH:mm` và nút **"Kiểm tra cập nhật"**. Việc cập nhật CSDL chữ ký giờ chỉ còn **tự động theo hạn 24h** (`GuardService.EnsureDailyAutoUpdate()` khi mở app/tab — bật/tắt ở tab **Cài đặt**), và mỗi lần cập nhật vẫn được ghi 1 dòng **"Cập nhật …"** trong bảng *Hoạt động gần đây* / tab Lịch sử.
> • **Thẻ số liệu "4. Đang cách ly"** (bấm số để mở tab Cách ly) — hàng thống kê còn **3 thẻ** (Mối đe dọa · Tệp đã quét · Lần quét gần nhất) chia đều 33.33%; muốn xem khu cách ly thì mở tab **Cách ly** ở sidebar.
>
> 🛠 **Sửa lỗi mở WinForms Designer 25/09/2026 (lần 3)** — VS báo *"The designer could not be shown for this file because none of the classes within it can be designed … The base class 'System.Void' cannot be designed"* khi định mở trang Tổng quan. Đã sửa **đủ 2 nguyên nhân**:
> • **`ScanAndRemoveVirus.csproj`**: `Control\UcTongQuan.ChiTiet.cs` và `Control\UcTongQuan.QuetNangCao.cs` trước đây bị gắn `<SubType>UserControl</SubType>` (kèm `<DependentUpon>`) — hai file này là **code thuần**: không có `InitializeComponent` và khai báo `partial class UcTongQuan` **không có base** → VS mở nhầm designer cho chúng và hiểu base class là `System.Void`. Nay 2 file là `Compile` thuần (giống `Theme.cs` / `UiIcons.cs`); **Designer chỉ mở từ `Control\UcTongQuan.cs`** (nơi khai báo đủ `: UserControl`).
> • **`Control\UcTongQuan.Designer.cs`**: 3 chỗ dùng **vòng lặp `for`** để nạp `ColumnStyles` (`pnlChips`, `pnlThongKe`, `tableLayoutPanelActions`) — Designer **không đọc được vòng lặp** trong `InitializeComponent`. Nay viết tay từng dòng `ColumnStyles.Add(...)`, giá trị y hệt trước: `3×AutoSize + 1×Percent 100F` (chips) · `3×Percent 33.33F` (3 thẻ) · `4×Percent 25F` (4 nút hành động).
> • Phòng ngừa thêm: bỏ 3 chỗ **pattern matching C# 7** (`is bool b && b`) trong `Control\UcTongQuan.cs` → helper `IsTicked(row)` dạng `is bool` + ép kiểu (parser CodeDOM của Designer chỉ hiểu cú pháp C# cũ). Hành vi không đổi.
> • Nếu VS vẫn giữ trạng thái lỗi cũ: **Build lại solution** (Designer cần design-time build thành công), rồi xoá cache `.vs\` (đã nằm trong `.gitignore`) và mở lại `Control\UcTongQuan.cs`.
>
> 🔧 **Dọn bố cục Tổng quan 25/09/2026 (lần 4)** — gỡ nốt dải trên cùng và trả 2 nút quét về đúng khối hero:
> • **Gỡ dải header riêng** (`pnlOverviewHeader` + `pnlScanStrip` + `pnlChips`): không còn ô header 44px nào ở đầu trang. Dòng trạng thái quét (`lblScanProgress`) được **gắn lại vào hero**: nó là con thứ 3 của `flowHeaderActions` (ngay cạnh 2 nút quét, cách 22px, chữ 8.25pt xám) — diễn biến: `Sẵn sàng quét` → `Đang quét...` / `Đang quét: <tệp>  (N tệp)` → `Hoàn tất — đã quét N tệp trong X giây.` (xanh nếu sạch, hổ phách nếu có đe dọa) · `Đã hủy phiên quét.` · `Quét dừng vì lỗi.` · `Đang tính hash + tra cứu VirusTotal...` → `VirusTotal: <tóm tắt>`.
> • **Sửa lỗi mất chữ trạng thái quét**: ở một lần dọn trước, các dòng gán chữ cho nhãn này bị xoá khỏi `Control\UcTongQuan.cs` (chỉ còn `try/catch` rỗng) trong khi **5 assert** của `Tests\UiEndToEnd.cs` vẫn đọc `lblScanProgress` → đã khôi phục đúng như bản `HEAD` (kể cả `MessageBox` khi hủy phiên mà test mong đợi), đồng thời bỏ 1 dòng `OnSetScanningAdvanced(scanning);` bị lặp.
> • **"▶ Quét ngay" + "⚙ Quét nâng cao ▾" (`flowHeaderActions`) nằm trong khối trạng thái bảo vệ** — hàng 4 của `tlpAnToanText`, ngay dưới tiêu đề + phụ đề của `pnlAnToan`, đúng như bảng *a) Trạng thái an toàn* ở trên; trạng thái (b) vẫn dùng 2 nút riêng của `pnlPhatHienDeDoa`.
> • **Hero cao 164px** (hàng 1 của `tableLayoutPanel12`; `pnlAnToan`/`pnlPhatHienDeDoa` = 160px + margin 2) — icon 96×96 + 2 nút vẫn thoáng, không bị chèn.
> • **Trang giãn theo cửa sổ (responsive)**: `tableLayoutPanel11` có **2 hàng**: hàng 0 = **dải loading quét** `pnlLoadingQuet` (hàng `Absolute`, **0px khi rảnh → 46px khi đang quét** — xem ghi chú *lần 9*) + hàng 1 = `tableLayoutPanel12` (**`Percent 100`** — giãn hết chiều cao); `tableLayoutPanel12` còn **6 hàng** `164 / 104 / 400 / 400 / 620 / 640`. `HienThi()` đổi hàng của **khối đang hiện** sang **`Percent 100`** (`SetRowFill`) — thẻ *Hoạt động gần đây* ở (a), bảng đe dọa ở (b), chi tiết ở (c), quét nâng cao ở (d) — nên trang **lấp hết chiều cao cửa sổ**, không còn khoảng trắng ở đáy; các hàng còn lại vẫn `Absolute` và hạ về 0 khi ẩn. Cửa sổ thấp hơn min `980×640` thì `AutoScroll` (mục `Theme.ScrollablePage`) giữ nguyên min, **không ép card nào**; riêng ở (c)/(d) hàng `104` của 3 thẻ số liệu bị hạ về `0` (xem ghi chú *lần 5* cho (d) / *lần 8* cho (c)) và **cả trang (d) còn tự xếp lại theo bề rộng cửa sổ** (xem ghi chú *lần 7*).
> • **Nút "Quét lại" của trạng thái (b) kiêm nút hủy**: đang quét thì đổi thành **"Hủy quét"** (`Theme.BtnRole.Cancel`) và bấm là hủy phiên đang chạy — dùng thay nút "Hủy quét" của dải header đã gỡ.
> • **Sửa lỗi ẩn**: `flowPhatHienActions` (2 nút *Xem chi tiết* / *Quét lại* ở (b)) trước neo `x = 1854` — **ngoài** `pnlPhatHienDeDoa` rộng 1168 nên vô hình; nay neo `Top|Right` tại `(872, 58)`, cách mép phải 24px ở mọi bề rộng cửa sổ.
> • **Kiểm thử**: vẫn **95 check** (sau ghi chú *lần 5* là **97 check**, *lần 6* là **106 check**, *lần 7* là **116 check**, *lần 8* là **118 check**, *lần 9* là **127 check**) — section 3b assert bố cục mới (NoField `pnlOverviewHeader`/`pnlScanStrip`/`pnlChips`; `tlpAnToanText` 4 hàng và `flowHeaderActions` là con `(0, 3)`; `tableLayoutPanel12` còn 6 hàng); section **9c** thêm 3 assert *responsive* (cửa sổ 1280×980: thẻ *Hoạt động* giãn ≥ 400px + hàng Percent vẫn nằm trong khung + nút quét trong tầm nhìn; cửa sổ 560px: `AutoScroll` bật, card giữ ≥ 303px, `grpActivity.Bottom` vẫn trong `tableLayoutPanel11`; trạng thái (b): bảng đe dọa hiện, nút *Xem chi tiết* phải **nằm trong** `pnlPhatHienDeDoa`).
> • **Dọn mã chết**: bỏ field `syncingChecks` trong `Control\UcTongQuan.cs` (khai báo + nhánh `if (syncingChecks || e.RowIndex < 0)` ở `dgvActions.CellValueChanged`) — field chưa bao giờ được gán nên luôn `false` (chỉ tổ tốn cảnh báo CS0649); hành vi chọn-hàng không đổi vì `Theme.PickAll` gán giá trị cell trực tiếp (không bắn `CellValueChanged`).
> • **Đã biên dịch thật trên macOS**: `dotnet build ScanAndRemoveVirus.csproj` (kèm reference assemblies net472) và `dotnet build Tests\UiEndToEnd.compile-check.csproj` (file **mới** — biên dịch rời harness UI test đúng như `csc` trên Windows) đều **Build succeeded — 0 Warning / 0 Error**; công thức nằm ở mục *Kiểm thử tự động*. Máy Mac **không chạy** được WinForms nên `PASS/FAIL` runtime vẫn phải lấy trên Windows.

> 🔧 **Trang "Quét nâng cao" là màn hình riêng — hàng 3 thẻ số liệu biến mất (25/09/2026, lần 5)**
> • Mở trang **(d)** *Quét nâng cao* (`MoQuetNangCao(...)` → `HienThi(TongQuanView.QuetNangCao)`) thì cả hàng 3 thẻ — **Mối đe dọa · Tệp đã quét · Lần quét gần nhất** (`pnlThongKe` chứa `grpThreats`/`grpScannedFiles`/`grpLastScan`) — **biến mất**: hàng 1 của `tableLayoutPanel12` hạ từ **104 → 0** (`SetRowHeight`) và `pnlThongKe.Visible = false`; nhờ đó toàn bộ chiều cao còn lại dành cho vùng chọn chế độ + nút **Bắt đầu quét**.
> • Quay lại **← Quay lại** của trang (d) — hoặc mở (a)/(b) — thì hàng trở lại đúng **104px** và 3 thẻ hiện lại đủ (chỉ là 1 hàng `Absolute` + `Visible` như `grpAction`/`grpActivity`, **không** dựng lại control, nội dung 3 thẻ giữ nguyên). *(Sau ghi chú **lần 8**, sang trang (c) hàng này cũng bị hạ về 0 — xem ghi chú kế tiếp.)*
> • Hằng số mới `ThongKeHeight = 104F` trong `Control\UcTongQuan.cs` (cạnh `HeroHeight = 164F`); **không** đụng tầng Services/API.
> • **Kiểm thử: 95 → 97 check** — thêm section **3e**: mở trang (d) bằng `uc.MoQuetNangCao(CheDoQuetNangCao.FullSystem)` (đúng đường đi từ dropdown *Quét nâng cao ▾*) rồi assert `pnlQuetNangCao.Visible && !pnlThongKe.Visible && !grpThreats/grpScannedFiles/grpLastScan.Visible && RowStyles[hàng 3 thẻ].Height == 0`; bấm `btnQuayLaiNangCao` rồi assert `!pnlQuetNangCao.Visible && pnlThongKe.Visible && grpThreats/grpLastScan.Visible && Height == 104`; cuối cùng gọi `HienThi(viewTruoc, refresh:false)` để **trả nguyên màn hình** cho các section sau.

> 🎛️ **Trang "Quét nâng cao": chuyển qua lại giữa 4 lựa chọn chế độ (25/09/2026, lần 6)**
> • **Lỗi cũ**: 4 thẻ chọn chế độ (*Quét toàn bộ hệ thống · Quét thư mục · Quét tệp · Quét tùy chỉnh*) là 4 `RadioButton` nằm trong **4 container riêng**, mà WinForms **chỉ tự bỏ chọn** radio **cùng parent** → bấm sang thẻ khác thì thẻ cũ **vẫn `Checked = true`** (nhiều thẻ cùng "sáng"), và bấm **lại** thẻ đầu **không** bắn `CheckedChanged` → cột phải **kẹt** ở chế độ vừa chọn, không quay về được.
> • **Cách sửa**: mọi thay đổi trạng thái đi qua **một cửa duy nhất `ChonTheCheDo(mode)`** — bật thẻ đang chọn + **bỏ chọn 3 thẻ còn lại** (cờ chống đệ quy `dangChonTheCheDo`), rồi mới `HienThiNoiDungCheDo(mode)` → luôn **đúng 1/4 thẻ** được chọn, qua lại bao nhiêu lần cũng được.
> • **Bấm được cả thẻ**: bấm vào **dòng mô tả** (hoặc thân thẻ) cũng chọn thẻ đó, không phải nhắm đúng dòng tiêu đề; thẻ đang chọn giữ **viền 2px xanh + nền xanh nhạt**, và **dòng mô tả** cũng đổi theo (`BlueDark` + nền `BlueTint` khi chọn / `TextGray` + `PageBg` khi không).
> • **Bàn phím**: `↑`/`↓` (và `←`/`→`) đi qua lại giữa 4 thẻ, có quấn vòng (`PreviewKeyDown → IsInputKey` để mũi tên không bị coi là dialog key).
> • **Không đổi hành vi khác**: dữ liệu đã nhập của từng chế độ (ổ đĩa đã tick, danh sách thư mục/tệp, vị trí tùy chỉnh) **giữ nguyên** khi qua lại; đang quét thì cả 4 thẻ + dòng mô tả bị **khoá** (không đổi chế độ giữa phiên quét). Không đụng tầng Services/API.
> • **Kiểm thử: 97 → 106 check** — thêm section **3f**: mở (d) bằng `uc.MoQuetNangCao(CheDoQuetNangCao.FullSystem)` rồi `PerformClick()` lần lượt thẻ **1 → 3 → 2 → 0**, mỗi bước assert `soTheDuocChon() == 1` + thẻ vừa bỏ **`!Checked`** + đúng `pnlFolder`/`pnlCustom`/`pnlFiles`/`pnlFullSystem.Visible` + nhãn tóm tắt `lblCheDoTomTat` đổi theo; cuối cùng assert **viền thẻ đang chọn dày hơn** và **dữ liệu ổ đĩa + trạng thái nút "Bắt đầu quét" giữ nguyên** (bắt đúng lỗi cũ: chỉ cần thẻ cũ còn `Checked` là fail ngay).

> 📐 **Trang "Quét nâng cao" tự xếp lại bố cục theo bề rộng cửa sổ (25/09/2026, lần 7)**
> • **Trước đây**: trang (d) là **2 cột cứng** (4 thẻ chế độ dọc bên trái + nội dung bên phải) đặt trong bảng `Absolute` — cửa sổ hẹp là **cắt xén**: nhãn *"Đang chọn: …"* rộng cứng **420px** tràn ra ngoài, nút **"Bắt đầu quét"** (neo góc phải dưới) bị đẩy khỏi khung, nội dung `pnlNoiDungPhai` bị che.
> • **Nay trang (d) reflow theo bề rộng THẬT của vùng trang** (`pnlQuetNangCao.Resize → XepBoCucNangCao()`), **3 mức**:
>   - **≥ 1000px** (`NguongHaiCot`): giữ **2 cột** như thiết kế — 4 thẻ xếp dọc cột trái, nội dung `pnlNoiDungPhai` bên phải (hàng `Percent 100` cho nội dung, thẻ dọc `CaoTheDoc`);
>   - **800 – 999px** (`NguongMotHang`): 4 thẻ về **1 hàng ngang** trên đầu (`DatHang` `25%/25%/25%/25%`, cao `CaoTheNgang`), nội dung **xuống hàng dưới** và chiếm trọn bề ngang;
>   - **< 800px**: 4 thẻ về **lưới 2×2** (`datHang` `50%/50%`, cao `CaoTheNho`), nội dung vẫn ở dưới.
> • **Header + chân trang cũng theo**: header (`tlpHeaderNangCao`) cho nhãn tóm tắt *"Đang chọn: …"* (`lblCheDoTomTat`) **xuống hàng riêng, trải hết bề ngang** (`SetColumnSpan 1 → 3`, hàng header cao `CaoHeaderRong → CaoHeaderHep`) và **trả lại** khi rộng; chân trang (`tlpChanNangCao`) đưa nút **"Bắt đầu quét"** **xuống dưới hộp *Lưu ý*** và đổi `Dock = Right → Fill` (nút giãn hết bề ngang, dễ bấm) rồi **về `Dock.Right`** khi rộng.
> • **Không dựng lại control nào**: chỉ đổi cột/hàng/vị trí của `TableLayoutPanel` (`NoiLuoi` đổi số cột/hàng, `DatCot`/`DatHang` đặt `ColumnStyles`/`RowStyles`, `DatOViTri` gán `(cot, hang)` + `Padding`) — **di chuyển con TRƯỚC, đổi lưới SAU** để WinForms không nuốt control; nên **thẻ đang chọn, dữ liệu ổ đĩa/thư mục/tệp đã nhập, vị trí con trỏ** đều giữ nguyên (test 3g chứng minh bằng `ReferenceEquals`).
> • **Chống giật khi kéo cửa sổ**: cờ `dangXepBoCuc` chặn đệ quy (`Resize` bắn trong lúc xếp) + guard `rong <= 0` (chưa có bề rộng thật) + **chỉ ghi khi tầng bố cục ĐỔI** (2 cờ `dangXepDoc`/`dangTheMotHang`) — kéo cửa sổ liên tục trong cùng một tầng thì `XepBoCucNangCao()` thoát ngay ở dòng đầu, không đụng layout.
> • **Hẹp vẫn xem đủ trang**: chỉ khi mở (d), `tableLayoutPanel11` được hạ bề rộng tối thiểu **980 → 420** (`RongToiThieuTrangNangCao`, chiều cao vẫn `CaoToiThieu = 640`) qua `CapNhatRongToiThieu(bool)` trong `HienThi()`; đóng (d) là mốc **980** trở về cho (a)/(b)/(c) — nhờ vậy (d) **reflow thật** chứ không chỉ sinh thanh cuộn ngang; `OnMoTrangQuetNangCao` còn gọi `XepBoCucNangCao()` để áp bố cục **ngay lúc mở**.
> • **Hằng số tập trung** ở đầu `Control\UcTongQuan.QuetNangCao.cs` (`NguongHaiCot = 1000`, `NguongMotHang = 800`, `RongToiThieuTrangNangCao = 420`, `CaoTheDoc/CaoTheNgang/CaoTheNho`, `CaoHeaderRong/CaoHeaderHep`, `CaoChanRong/CaoChanHep`) — mọi toạ độ cứng trước đây (`86F`, `420F`…) đã thay bằng hằng số. **Không** đụng tầng Services/API.
> • **Kiểm thử: 106 → 116 check** — thêm section **3g**: mở (d) trên **cửa sổ riêng** (`Form` 1400×980) rồi thu hẹp dần **1400 → 950 → 650 → 1400**: mỗi mức assert đúng số cột/hàng của `tlpThanNangCao`/`pnlCheDoTrai`/`tlpHeaderNangCao`/`tlpChanNangCao` (2 cột · 4 thẻ 1 hàng · lưới 2×2), thẻ cuối + nút **không bị cắt** (`oTheCheDo[3].Right/Bottom` trong `pnlCheDoTrai.ClientSize`), nút giãn hết bề ngang khi hẹp; **rộng lại** thì `ReferenceEquals` chứng minh **không dựng lại control** + thẻ *Quét tệp* và dữ liệu ổ đĩa còn nguyên; cuối cùng đóng (d) để khẳng định ngưỡng **980** trở về (`t11r.MinimumSize == 980×640`, `pgr.Visible == false`).

> 🧾 **Trang "Chi tiết kết quả quét" (c): bỏ luôn hàng 3 thẻ số liệu (25/09/2026, lần 8)**
> • **Yêu cầu thiết kế**: ở trang **(c)** *Chi tiết kết quả quét* không còn hàng 3 thẻ *Mối đe dọa · Tệp đã quét · Lần quét gần nhất* (`pnlThongKe` chứa `grpThreats`/`grpScannedFiles`/`grpLastScan`) — nội dung chính của trang là bảng chi tiết + panel 5 tab.
> • **Cách làm**: hàng 3 thẻ nay thuộc **riêng Tổng quan (a)/(b)** — điều kiện trong `HienThi()` đổi từ `view != TongQuanView.QuetNangCao` (lần 5) thành **`view == TongQuanView.AnToan || view == TongQuanView.PhatHienDeDoa`**, nên mở (c) hay (d) đều đi qua **đúng 1 chỗ** `SetRowHeight(pnlThongKe, 0)` + `pnlThongKe.Visible = false` (không thêm nhánh điều hướng mới, không rải logic ra nhiều nơi).
> • **Không mất thông tin**: header của (c) vốn đã hiện **"Thời gian quét: …"** + **"Loại quét: …"** — đúng 2 thông tin cần khi xem chi tiết; số đe dọa hiện ngay ở tiêu đề trang + cột *Mức độ* của bảng.
> • **Lợi ích**: 104px của hàng đó được trả hết cho bảng chi tiết + panel 5 tab (hàng (c) `Absolute 620` → `Percent 100` nên còn cao thêm khi cửa sổ lớn).
> • **Giữ nguyên mọi thứ khác**: control **không** bị gỡ/dựng lại (chỉ đổi `Visible` + chiều cao hàng), dữ liệu 3 thẻ (đếm đe dọa, số tệp đã quét, thời điểm quét) vẫn được cập nhật như cũ và **hiện lại đủ 104px** khi bấm **"← Quay lại"** về (b)/(a); (a)/(b) không đổi gì; **không** đụng tầng Services/API.
> • **Kiểm thử: 116 → 118 check** — section **3e** được mở rộng thành *"màn hình riêng (c)/(d): hàng 3 thẻ biến mất"*: thêm **2 assert cho (c)** — gọi `uc.MoChiTietKetQua(0)` (đúng đường đi của nút *Xem chi tiết* / link *Xem tất cả*) rồi assert `pnlChiTietKetQua.Visible && !pnlThongKe.Visible && !grpThreats/grpScannedFiles/grpLastScan.Visible && RowStyles[hàng 3 thẻ].Height == 0`; bấm `btnQuayLaiChiTiet` rồi assert `!pnlChiTietKetQua.Visible && pnlPhatHienDeDoa.Visible && pnlThongKe.Visible && grpThreats.Visible && Height == 104`.

> 🧾 **Dải loading quét — bấm BẤT KỲ nút quét nào là có loading (25/09/2026, lần 9)**
> • **Yêu cầu thiết kế**: ở trang **Tổng quan**, sau khi nhấn **bất kỳ nút quét nào** thì phải **thấy loading xuất hiện** — trước đó trang chỉ có đúng 1 dòng chữ trạng thái (`lblScanProgress`), nên khi quét ổ đĩa lớn giao diện như "ngồi im".
> • **Một điểm vào duy nhất**: cả 3 nút quét — **"▶ Quét ngay"** ở (a)/(b), **"Quét lại"** ở (b) và **"Bắt đầu quét"** ở (d) — đều chạy qua `ChayPhienQuet` → `SetScanning(true/false)`, nên chỉ cần **1 chỗ** `HienThiDaiLoading(scanning)` trong `SetScanning` là mọi nút đều có loading (không rải logic ra 3 chỗ, không thêm nhánh điều hướng mới).
> • **Dải loading** `pnlLoadingQuet` — **hàng 0 của `tableLayoutPanel11`** (ngay trên toàn bộ nội dung, đúng chỗ hàng header 54px đã gỡ ở lần 4): hàng `Absolute` giãn **0 → 46px** (`LoadingHeight = 46F`) + `Visible = true`; nội dung trang nhường 46px rồi lấy lại khi phiên kết thúc (`SetScanning(false)` nằm trong `finally` của `ChayPhienQuet` nên **xong / hủy / lỗi** đều hạ hàng về **0px**). Dải nằm **trên** nội dung (không phủ lên) nên không che nút nào.
> • **Trong dải** (3 cột: vòng xoay 46px · chữ · nút 120px): **vòng xoay** `spinnerDangQuet` — control **mới** `Control\LoadingSpinner.cs` vẽ GDI+ (`OnPaint`, `Timer` 60ms, mỗi nhịp quay 30°; tự `BatDau`/`Dung` theo `Visible` nên **không tốn Timer lúc máy rảnh**) · tiêu đề **"Đang quét…"** + dòng chi tiết (mặc định *"Vui lòng chờ trong giây lát — có thể bấm "Hủy quét" để dừng."*, và **bám tiến trình thật** khi engine có báo: `CapNhatDongChiTietLoading(lblScanProgress.Text)` trong callback tiến độ → "Đang quét: <tệp>  (N tệp)"; hủy/lỗi → "Đã hủy phiên quét." / "Quét dừng vì lỗi." nên lúc MessageBox hiện không còn chữ "Đang quét") · nút **"Hủy quét"** `btnHuyQuetLoading` (`Theme.BtnRole.Cancel` + icon ■).
> • **Đường hủy thứ 3 (vá luôn 1 lỗ hổng cũ)**: ở trang **(d)**, `btnBatDauQuet` bị **khoá** khi phiên đang chạy (`OnSetScanningAdvanced`) nên trước lần 9 **không có cách nào hủy** phiên quét vừa bắt đầu từ (d). Nay nút trong dải loading gọi chung **`HuyPhienQuet()`** (một nguồn duy nhất cho cả 3 nút hủy: "Quét ngay"/"Quét lại" ở vai trò Hủy + nút của dải) → hủy được từ **mọi** màn hình (a)/(b)/(c)/(d).
> • **Màu sắc vẫn 1 nguồn**: `Theme.StyleLoadingStrip(...)` (nền `BlueTint`, viền `BlueSoft` vẽ trong `Paint`, chữ `BlueDark`/`TextGray`, nút `BtnRole.Cancel`) — Designer chỉ giữ bố cục.
> • **Không dựng lại control nào**: chỉ đổi `Visible` + chiều cao hàng 0 (`SetLoadingRowHeight`), y như `pnlThongKe`/`pnlHeroHost`; (a)/(b)/(c)/(d) **không đổi gì khác**, không đụng tầng Services/API.
> • **Kiểm thử: 118 → 127 check** — section **3b** thêm 1 assert (dải loading có thật, đang **ẩn**, `tableLayoutPanel11.RowStyles.Count == 2`, hàng 0 đúng **0px**, `pnlLoadingQuet` ở hàng 0 / `tableLayoutPanel12` ở hàng 1); section **3c** thêm 3 assert quanh luồng hủy: dải hiện **ngay trong cùng nhịp `PerformClick`** (46px + tiêu đề "Đang quét…" + nút Hủy hiện), **vòng xoay đổi góc sau 350ms** (chứng minh quay thật), sau khi hủy thì dải **ẩn + hàng 0 về 0px + vòng xoay dừng** (góc đứng yên); section **3h** mới (5 assert, dùng lại corpus `cancel-ui` của 3c — mỗi lượt đều `ScanEngine.ClearScanCache()` để phiên chắc chắn còn chạy khi hủy giữa chừng): **hủy bằng nút của dải loading**, mở (d) chế độ *Thư mục* + thêm 1 vị trí (`ThemThuMuc`), bấm **"Bắt đầu quét"** ở (d) → dải hiện ngay + nút bị khoá, vòng xoay vẫn quay khi đang quét từ (d), hủy xong → dải ẩn + hàng 0 về 0px + nút **"Bắt đầu quét" bật lại** và vẫn ở trang (d).


**b) Trạng thái phát hiện mối đe dọa (ngay sau khi "Quét ngay" kết thúc và tìm thấy ≥ 1 đe dọa)**

| Vùng | Nội dung |
|---|---|
| **Khối trạng thái trung tâm** | Icon tròn **đỏ** dấu `!` cỡ lớn · **"Phát hiện N mối đe dọa"** (đỏ, đậm) · phụ đề xám **"Quá trình quét đã hoàn tất."** · 2 nút cạnh nhau: **"Xem chi tiết"** (xanh, Primary) mở trang chi tiết (mục c) và **"Quét lại"** (viền, Secondary) chạy lại đúng kiểu quét vừa rồi. |
| **Hàng thống kê (3 thẻ)** | **Mối đe dọa**: `N` (đỏ) + phụ đề "Tệp bị nhiễm" · **Tệp đã quét**: tổng số tệp đã quét trong phiên (định dạng có dấu chấm phân cách nghìn) · **Lần quét gần nhất**: thời điểm phiên vừa hoàn tất. |
| **Bảng "Mối đe dọa được phát hiện"** | **Chỉ xuất hiện ở trạng thái này** (cao 400px, lấy đúng chỗ thẻ *Hoạt động gần đây* — ở trạng thái an toàn bảng bị ẩn hẳn nên hàng hạ về 0). Liên kết **"Xem tất cả"** ở góc phải mở sang trang chi tiết. Cột: **Chọn** (checkbox, nhấp tiêu đề = chọn/bỏ chọn tất cả) · **Tệp** (đường dẫn đầy đủ) · **Mối đe dọa** (`Kind: Reason`) · **Mức độ** (badge màu: `Cao` đỏ, `Trung bình` cam, `Thấp` vàng — suy ra từ điểm Heuristic/loại chữ ký/VirusTotal) · **Thao tác** (nút "Xem chi tiết" mở đúng dòng đó ở trang chi tiết). Hàng nút ngay dưới bảng: **Cách ly đã chọn · Xóa đã chọn · Cách ly tất cả · Tra VirusTotal**. |
| **Hoạt động gần đây** | Nhường chỗ cho bảng đe dọa ở trạng thái này (hàng hạ về 0); dòng mới nhất đã được ghi thêm vào `scanhistory.log` — xem lại đầy đủ ở trạng thái an toàn hoặc tab Lịch sử. |

**c) Trang "Chi tiết kết quả quét"** (mở từ nút *Xem chi tiết* hoặc liên kết *Xem tất cả*)

| Vùng | Nội dung |
|---|---|
| **Header** | Nút **"← Quay lại"** Tổng quan · tiêu đề **"Chi tiết kết quả quét"** + phụ đề · góc phải: **"Thời gian quét: dd/MM/yyyy HH:mm:ss"**, **"Loại quét: <Quét nhanh/Quét toàn bộ/Quét tùy chọn>"**, nút **"Xuất báo cáo"** (xuất file báo cáo phiên quét đó, khác với "Xuất CSV" toàn bộ lịch sử ở tab Lịch sử). |
| **Bảng đầy đủ** | Cột: **STT · Tên tệp · Đường dẫn · Mối đe dọa** (tên định danh, vd `Trojan.GenericKD.123456`, `Win32.PowerShell.Malware`, `EICAR-Test-File`) **· Mức độ** (badge Cao/Trung bình/Thấp) **· Hash (SHA256)** (rút gọn + nút copy) **· Thao tác** (nút **"Xem"** + mũi tên xổ xuống cho Cách ly/Xóa/Tra VirusTotal của riêng dòng đó). Chọn một dòng → mở panel chi tiết bên dưới cho đúng dòng đó. |
| **Panel chi tiết (theo dòng đang chọn)** | 5 tab con: **Thông tin chi tiết** (mặc định) · **VirusTotal** · **Hành vi** · **Chuỗi ký tự** · **Thông tin bổ sung**. |
| ↳ Tab *Thông tin chi tiết* | Card trái **"Thông tin tệp"**: Tên tệp, Đường dẫn, Kích thước (vd `2.45 MB (2,567,680 bytes)`), Loại tệp, Thời gian tạo, Thời gian sửa đổi, **MD5 / SHA1 / SHA256** đầy đủ kèm nút copy từng dòng. Card phải **"VirusTotal"**: biểu đồ donut `X/72` + dòng **"X/72 công cụ phát hiện"** + câu mô tả kết luận + **"Lần phân tích gần nhất"** + **"Link:"** URL báo cáo (rút gọn, có nút copy) + nút **"Mở trên VirusTotal"** (mở tab trình duyệt tới `virustotal.com/gui/file/<sha256>`). |
| ↳ Các tab còn lại | *VirusTotal*: bảng chi tiết theo từng vendor (malicious/undetected/nhãn) nếu đã tra. *Hành vi*: log WMI/heuristic đã kích hoạt cho tệp này (nếu có, từ guard hành vi). *Chuỗi ký tự*: các chuỗi/marker đáng ngờ trích ra khi quét (vd `iex(`, `-enc`, `DownloadString`…). *Thông tin bổ sung*: metadata khác (MOTW/Zone.Identifier, tiến trình sinh ra tệp…) — các tab này hiển thị "Chưa có dữ liệu" nếu tệp không có thông tin tương ứng, không bịa số liệu. |

> 🧾 **Trang (c) không có hàng 3 thẻ số liệu** (25/09/2026, lần 8): giống trang (d), hàng *Mối đe dọa · Tệp đã quét · Lần quét gần nhất* **chỉ thuộc Tổng quan (a)/(b)** — mở (c) là hàng biến mất (hạ hàng về `0px` + `Visible = false`), bảng chi tiết + panel 5 tab nhận trọn chiều cao; **thời gian/loại quét** vẫn hiện ở **header** của (c) nên không mất thông tin. Bấm **"← Quay lại"** về (b)/(a) thì 3 thẻ hiện lại đủ `104px`. Chi tiết ở ghi chú *lần 8* trong mục a.

**d) Trang "Quét nâng cao"** (mở từ dropdown *Quét nâng cao* ở trạng thái an toàn, hoặc nút *Quét lại* ở trạng thái phát hiện đe dọa khi người dùng muốn đổi phạm vi quét)

Header chung: nút **"← Quay lại"** Tổng quan · tiêu đề **"Quét nâng cao"** + phụ đề **"Chọn chế độ quét phù hợp với nhu cầu của bạn để kiểm tra và phát hiện các mối đe dọa."**. Bố cục **tự xếp lại theo bề rộng cửa sổ** (xem ghi chú *lần 7* ở mục a): **≥ 1000px** thì **2 cột** (4 thẻ chế độ xếp dọc bên trái + nội dung bên phải — như thiết kế), **800 – 999px** thì 4 thẻ về **1 hàng ngang trên đầu**, **< 800px** thì 4 thẻ về **lưới 2×2**; ở 2 mức hẹp, nội dung chế độ **xuống dưới** và chiếm trọn bề ngang, nút *Bắt đầu quét* **xuống dưới hộp *Lưu ý* và giãn hết bề ngang**. **Cột trái** là 4 thẻ chọn chế độ dạng radio (thẻ đang chọn **viền 2px xanh + nền xanh nhạt, dòng mô tả cũng đổi màu**) — *Quét toàn bộ hệ thống · Quét thư mục · Quét tệp · Quét tùy chỉnh*, mỗi thẻ gồm icon + tiêu đề + mô tả ngắn 1 dòng; **luôn đúng 1/4 thẻ được chọn** và bấm **qua lại tự do** giữa 4 lựa chọn (bấm vào cả dòng mô tả thẻ, hoặc dùng `↑`/`↓`) — cột phải đổi nội dung **ngay lập tức trong cùng trang** (không chuyển trang riêng) và **giữ nguyên dữ liệu đã nhập** của từng chế độ (xem ghi chú *lần 6* ở mục a). Trang (d) là **màn hình riêng**: hàng 3 thẻ số liệu của Tổng quan (*Mối đe dọa / Tệp đã quét / Lần quét gần nhất*) **biến mất** khi mở trang này (xem ghi chú *lần 5* ở mục a), nên toàn bộ chiều cao còn lại dành cho vùng chọn chế độ + nút **Bắt đầu quét**.

| Chế độ (cột phải) | Nội dung |
|---|---|
| **Quét toàn bộ hệ thống** | Hộp lưu ý xanh nhạt: *"Chế độ quét toàn bộ hệ thống sẽ kiểm tra tất cả ổ đĩa và có thể mất nhiều thời gian. Bạn vẫn có thể sử dụng máy tính trong khi quét."* · **Tùy chọn quét** (7 checkbox xếp 2 cột, icon ⓘ hover xem giải thích): Quét sâu (kiểm tra chi tiết từng tệp) · Quét file nén (ZIP, RAR, 7Z…) · Kiểm tra bộ nhớ đang hoạt động · Tìm kiếm chương trình không mong muốn (PUA) · Quét khu vực hệ thống (Windows) · Sử dụng phát hiện dựa trên hành vi · Tự động cách ly khi phát hiện mối đe dọa. · **Bảng "Ổ đĩa sẽ quét"**: checkbox từng dòng + cột **Ổ đĩa** (icon phân biệt ổ hệ thống/dữ liệu) · **Loại** (vd `Hệ thống (SSD)`, `Ổ đĩa dữ liệu (HDD)`) · **Tổng dung lượng** · **Dung lượng trống** — đọc thật từ `DriveInfo` của máy, mặc định tick hết mọi ổ cố định. |
| **Quét thư mục** | **"Thư mục cần quét"**: ô đường dẫn (readonly) + nút **"Chọn thư mục"** (mở dialog chọn, **thêm vào danh sách** chứ không thay thế). **Bảng "Danh sách thư mục đã chọn (N)"** + nút **"Xóa tất cả"** góc phải: cột STT · Đường dẫn thư mục · Kích thước (tính đệ quy) · Thao tác (icon xóa từng dòng). **Tùy chọn quét** (6 checkbox, 2 cột): Quét sâu · Quét trí nhớ tạm · Quét file nén (ZIP/RAR/7Z) · Tìm PUA · Sử dụng phát hiện dựa trên hành vi · Tự động cách ly khi phát hiện mối đe dọa. |
| **Quét tệp** | **"Chọn tệp để quét"**: vùng **kéo-thả** (*"Kéo và thả tệp vào đây / Hoặc chọn tệp bằng nút bên dưới"*) + nút **"Chọn tệp ▾"** (dropdown chọn 1 hoặc nhiều tệp cùng lúc). **Bảng "Danh sách tệp đã chọn (N)"** + **"Xóa tất cả"**: STT · Tên tệp (icon theo phần mở rộng: exe/pdf/zip…) · Đường dẫn (rút gọn) · Kích thước · Thao tác (xóa từng dòng). **Tùy chọn quét** (3 checkbox): Sử dụng phát hiện dựa trên hành vi · Kiểm tra tệp nén (ZIP, RAR, 7Z…) · Tự động cách ly khi phát hiện mối đe dọa. |
| **Quét tùy chỉnh** | **"Vị trí quét"**: nút **"+ Thêm vị trí ▾"** (thêm thư mục hoặc tệp lẻ), **"✕ Xóa"** (xóa dòng đang chọn), **"🗑 Xóa tất cả"** · bảng STT · Đường dẫn · **Loại** (`Thư mục`/`Tệp`) · Thao tác. **"Loại tệp quét"** (radio, chọn đúng 1): *Tất cả tệp* · *Chỉ tệp thực thi (.exe, .dll, .sys…)* · *Tệp nén (.zip, .rar, .7z…)* · *Tệp tài liệu (.doc, .docx, .pdf, .xls…)* · *Tùy chỉnh phần mở rộng tệp* (ô nhập vd `exe,dll,sys`, chỉ bật khi chọn radio này). **"Tùy chọn quét"** (6 dòng): Quét sâu · Sử dụng phát hiện dựa trên hành vi · Kiểm tra tệp nén · Phát hiện phần mềm không mong muốn (PUA) · Tự động cách ly khi phát hiện mối đe dọa · **"Bỏ qua các tệp lớn hơn"** (ô nhập số + dropdown đơn vị `KB/MB/GB`). |

Mỗi chế độ kết thúc bằng hộp **"Lưu ý"** (icon ⓘ, 1–2 gạch đầu dòng khác nhau tuỳ chế độ — vd *Quét thư mục/toàn bộ hệ thống*: "Thời gian quét phụ thuộc vào dung lượng và số lượng tệp." / "Bạn có thể tiếp tục sử dụng máy tính trong khi quét."; *Quét tệp*: "Bạn có thể chọn nhiều tệp cùng lúc để quét." / "Hỗ trợ các định dạng tệp phổ biến (exe, dll, doc, pdf, zip, rar, …)."; *Quét tùy chỉnh*: "Bạn có thể thêm nhiều thư mục và tệp cùng lúc để quét." / "Thời gian quét phụ thuộc vào dung lượng dữ liệu và tùy chọn bạn chọn.") và nút **"▶ Bắt đầu quét"** (Primary, góc dưới phải) — bấm sẽ gọi `ScanEngine` với đúng phạm vi + toàn bộ tùy chọn vừa cấu hình ở cột phải, rồi quay về Tổng quan ở trạng thái (a) hoặc (b) tuỳ kết quả.

> Cả 4 màn hình trên vẫn dùng chung dữ liệu **thật** hiện có (`ScanHistoryStore`, `ScanEngine`, `VirusTotalClient`) — đây là bản vẽ lại UI/UX cho `UcTongQuan`, không đổi nguồn dữ liệu hay logic quét/cách ly/VirusTotal ở tầng `Services/`.

> ✅ **Trạng thái triển khai (25/09/2026):** cả 4 màn hình (a)–(d) đã dựng xong **trên dữ liệu thật**, không dùng mock:
> `Theme` + `UiIcons` + `VtDonut` cho token/icon/donut, `ScanEngine` cho quét (nhiều vị trí ở (d) quét **tuần tự từng vị trí** rồi gộp kết quả),
> `ScanHistoryStore` cho thống kê/lịch sử, `VirusTotalClient` cho tra hash **SHA256** (không upload tệp), `DirectorySizeCalculator` cho cỡ thư mục/tệp (tính ở luồng nền, hủy được).
> Hash MD5/SHA1/SHA256 ở trang (c) chỉ tính khi mở tab tương ứng. Lọc "Loại tệp quét"/"Bỏ qua tệp lớn hơn" của (d) áp ở phía UI
> (`ThoaBoLocCuaTrangQuetNangCao`) vì `ScanEngine` không có tham số này. `pnlChiTietKetQua`/`pnlQuetNangCao` được dựng bằng code-behind
> (`UcTongQuan.ChiTiet.cs`, `UcTongQuan.QuetNangCao.cs`) và gắn vào 2 hàng Absolute của `tableLayoutPanel12` — hàng ẩn được hạ về 0 nên không chừa khoảng trắng.
> Đổi giao diện 25/09/2026 *(theo yêu cầu)*: **bỏ thẻ *Tuỳ chọn quét nhanh*** (`grpScan` + 3 radio + `btnPickFile`/`btnPickFolder` + `lblCustomPath`) và **đổi thẻ 3 dòng *Hoạt động gần đây* thành bảng cuộn được**
> (`dgvActivity`: chấm màu + mô tả + thời gian, tối đa 100 dòng từ `scanhistory.log`, link *Mở tab Lịch sử ›* qua `FrmMain.MoTabLichSu()`).
> Bảng **"Mối đe dọa được phát hiện"** (`grpAction`) không còn nằm ở trạng thái an toàn — chỉ hiện ở trạng thái (b) *(đe dọa)* với chiều cao 400px; ở (a) hàng của nó hạ về 0 và thẻ bị ẩn.
> `tableLayoutPanel12` còn **6 hàng Absolute: 164 / 104 / 400 / 400 / 620 / 640**; `HienThi()` đặt hàng của **khối đang hiện** thành **`Percent 100`** (`SetRowFill` — *Hoạt động gần đây* ở (a), bảng đe dọa ở (b), chi tiết ở (c), quét nâng cao ở (d)) và hạ hàng của khối bị ẩn về 0 (`SetRowHeight`), nên trang **giãn hết chiều cao cửa sổ**, không chừa khoảng trắng ở đáy.

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

Danh sách **thật** các tệp đang bị giữ trong `AppData\Quarantine\` (ngay gốc repo) — kèm cột *đường dẫn gốc, mối đe dọa (lý do + loại), thời gian, kích thước*:

* **Khôi phục / Khôi phục tất cả** — trả tệp về **đúng đường dẫn cũ**; nếu vị trí đã có file trùng tên, tự đặt hậu tố ` (1)` — không bao giờ ghi đè mất dữ liệu.
* **Xóa vĩnh viễn** — xóa hẳn các tệp **đã tích Chọn**, hành động không hoàn tác, có hộp xác nhận Yes/No.
* **Làm mới** — và tab tự refresh mỗi khi có biến cố cách ly từ tab khác.
* Sổ cái `quarantine.log`: bạn xóa tay file trong thư mục Quarantine thì dòng tương ứng **tự biến mất** khỏi bảng (tự dọn "dòng ma").

---

### 5. Cài đặt — `UcCaiDat`

Tab riêng (nút **Cài đặt** trên sidebar):

* ☑ *Tự động khởi động cùng Windows* — **thật 100%**: ghi/xóa `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (không cần admin).
* ☑ *Tự động cập nhật* · ☑ *Gửi mẫu ẩn danh* · ☑ *Hiển thị thông báo* — lưu vào `settings.ini`; **đồng bộ hai chiều** với các hàng "Tự động cập nhật"/"Cảnh báo mối đe dọa" của tab Bảo vệ.
* **"Lưu thiết lập"** — ghi file + áp dụng autostart ngay; registry từ chối thì checkbox tự nhả về đúng thực tế + cảnh báo.
* **"Tạo tệp mẫu 3 kỹ thuật"** — phục hồi 6 tệp vô hại trong `TestSamples\` của repo (idempotent); nếu app chạy ngoài repo sẽ tạo ở `Desktop\XVirus-Samples`.

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
   * Key nằm tại gốc repo `vtapikey.txt` (cạnh .csproj) — **được commit có chủ đích** để cả nhóm dùng chung quota free-tier của một key VT (rủi ro: ai cũng thấy được key; nếu bị đốt quota hết sạch → Profile → API key → *regenerate* rồi commit bản mới). Chạy app ngoài repo: fallback `AppData\vtapikey.txt` của thư mục dữ liệu.   * Có thể tự tạo file trên với nội dung = key nếu không muốn dùng hộp thoại.

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
AntivirusWinform/                 # gốc repo = gốc project (mở ScanAndRemoveVirus.slnx là chạy)
├── ScanAndRemoveVirus.slnx       # solution — trỏ thẳng .csproj cùng cấp
├── ScanAndRemoveVirus.csproj     # .NET Framework 4.7.2, WinForms
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
│   ├── DirectorySizeCalculator.cs#   cỡ tệp/thư mục đệ quy (tính ở luồng nền, hủy được) cho (c)/(d)
│   └── TestSamples.cs            #   bộ 11 tệp mock VÔ HẠI phủ đủ 3 kỹ thuật (nút "Tạo tệp mẫu")
├── Control/                      # UI per-tab
│   ├── Theme.cs                  #   ✅ NGUỒN MÀU DUY NHẤT — mọi control tham chiếu Theme.X
│   ├── UiIcons.cs                #   kho icon vẽ bằng GDI+ (khiên/tròn đỏ/▶/■/...), cache theo (tên, cỡ, màu)
│   ├── VtDonut.cs                #   control donut `X/72` của thẻ VirusTotal (vẽ trong OnPaint)
│   ├── LoadingSpinner.cs        #   vòng xoay GDI+ của "dải loading quét" (lần 9, tự chạy/dừng theo Visible)
│   ├── UcTongQuan.cs (+Designer) #   (a)/(b): quét + hành động + VT + thống kê + điều hướng 4 màn hình
│   ├── UcTongQuan.ChiTiet.cs     #   (c) Chi tiết kết quả quét: header + bảng đầy đủ + 5 tab con
│   ├── UcTongQuan.QuetNangCao.cs #   (d) Quét nâng cao: 4 chế độ + cột chọn chế độ + bảng vị trí
│   ├── UcBaoVe.cs (+Designer)    #   10 công tắc guard thật + panel trạng thái
│   ├── UcLichSu.cs (+Designer)   #   3 tab lọc + chi tiết + xuất CSV
│   ├── UcCachLy.cs (+Designer)   #   danh sách + restore/delete/chọn/tất cả/refresh
│   └── UcCaiDat.cs (+Designer)   #   tab Cài đặt: 4 checkbox + lưu thiết lập + tạo tệp mẫu
├── TestSamples/                  # bộ mẫu test (EICAR + mock, commit có chủ đích)
├── AntivirusDB.sql               # schema CSDL chữ ký thật (VirusSignatures) — chờ kết nối
└── Tests/                        # harness csc (xem mục Kiểm thử)
```

**Dữ liệu runtime** — tất cả trong `AppData\` ngay gốc repo (`.gitignore` rồi; chạy ngoài repo mới fallback về `%AppData%\ScanAndRemoveVirus`):

| File | Nội dung | Xóa thì sao |
|---|---|---|
| `Quarantine\*.qtn` | nội dung tệp đã cách ly (đổi tên GUID) | mất khả năng khôi phục |
| `quarantine.log` | sổ: id ↔ đường dẫn gốc ↔ lý do ↔ thời gian ↔ cỡ | bảng Cách ly tự dọn (prune) |
| `scanhistory.log` | mọi phiên quét/cảnh báo/RT/cập nhật (giữ 1000 dòng) | Lịch sử trống |
| `scancache.dat` | cache `đường dẫn → (mtime, size, độc?)` (≤ 500k mục) | quét lại từ đầu, chậm hơn |
| `dbupdate.txt` | tem ngày "cập nhật CSDL chữ ký" (kèm xóa cache mỗi lần tự động cập nhật theo hạn 24h) | tem cũ -> lần mở app sau tự đóng tem mới |
| `settings.ini` | 11 cờ: 4 cài đặt + 10 công tắc tính năng tab Bảo vệ (FeatureFlags) | về mặc định |
| `vtapikey.txt` (project, **commit có chủ đích**) | **API key VirusTotal** dùng chung cho team — nằm cạnh file .csproj; ngoài repo -> fallback AppData | bấm Tra VT sẽ hỏi lại key |

---

## ✅ Kiểm thử tự động

Hai harness **không** nằm trong build app — biên dịch trực tiếp mã nguồn bằng `csc` có sẵn trong Windows:

```powershell
# chạy từ gốc repo
$csc = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\Roslyn\csc.exe" | Select-Object -First 1  # Roslyn: nguồn dùng C#7

# 1) Engine — 86 check: 3 kỹ thuật + cache + cách ly/ledger/restore xung đột,
#    history + CSV + tem CSDL + auto-update 24h, stress scan×hủy giữa chừng (8 vòng),
#    VT parse offline + vector SHA256 NIST, AppSettings + registry (tự backup/restore),
#    Guard thật: USB-simulate, Download-MOTW (ADS), StartUp, Restore-guard, WMI HÀNH VI LIVE
& $csc /nologo /out:eng.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  /r:System.Net.Http.dll /r:System.Management.dll /r:Microsoft.VisualBasic.dll `
  Services\ScanEngine.cs   Services\RealTimeProtection.cs `
  Services\QuarantineLedger.cs Services\ScanHistoryStore.cs `
  Services\VirusTotalClient.cs Services\AppSettings.cs `
  Services\FeatureFlags.cs Services\GuardService.cs `
  Services\TestSamples.cs Services\DataDir.cs `
  Tests\ScanEngineTest.cs
.\eng.exe        # kỳ vọng: == ALL TESTS PASSED ==   (~40s nếu WMI live hoạt động)

# 2) UI End-to-End — 127 check: dựng FrmMain + 5 UserControl THẬT, PerformClick TỪNG NÚT
#    (5 nút sidebar, quét + Hủy giữa phiên, bỏ thẻ "Tuỳ chọn quét nhanh" (NoField + bố cục mới:
#     tableLayoutPanel12 còn 6 hàng, flowHeaderActions nằm trong tlpAnToanText) + SetCustomPath ->
#     chế độ Quét tùy chọn, đã bỏ thanh loading quét CŨ (pgbScan — nay có "dải loading quét" mới) + cả khối "cập nhật dữ liệu" + thẻ "Đang cách ly"
#     (hàng số liệu còn 3 thẻ), dải header Tổng quan cũng đã gỡ (NoField pnlOverviewHeader/pnlScanStrip/
#     pnlChips) và trang giãn theo cửa sổ (section 9c), hàng 3 thẻ số liệu bị bỏ ở CẢ trang chi tiết (c) lẫn
#     trang Quét nâng cao (d) — màn hình riêng, và quay lại thì hiện đủ 104px (section 3e), 4 thẻ chế độ ở trang Quét nâng cao chuyển qua lại tự do (section 3f) + cả trang Quét nâng cao tự xếp lại bố cục theo bề rộng (section 3g), dải loading quét hiện ngay khi bấm bất kỳ nút quét nào (section 3c + 3h — đủ cả 3 nút, kể cả "Bắt đầu quét" ở (d) và hủy bằng nút của dải), tự động cập nhật 24h ghi 1 dòng vào bảng
#     "Hoạt động gần đây", VT flow với key giả,
#     cách ly/xóa CHỌN & TẤT CẢ, bộ mẫu 6 tệp -> đúng 5 threat, tab lịch sử (cột colPick chọn
#     nhiều + nút xóa mục đã chọn), chi tiết, làm mới, liên kết "Mở tab Lịch sử" ở Tổng quan,
#     tab Cài đặt (8 checkbox live-apply, VTkey, Khôi phục mặc định, Xóa cache, nhãn info),
#     LẬT 2 CHIỀU 10 HÀNG tab Bảo vệ, thống kê khởi động không còn mock 2025)
#     — closer-thread tự bấm Có/OK cho MessageBox.
#    Danh sách file đầy đủ: xem comment đầu Tests\UiEndToEnd.cs
#    Matrix phủ nút: powershell -File Tests\coverage-matrix.ps1 -> liệt kê nút CLICKED / nút chỉ mở hộp thoại
#    hệ thống không auto-safe (Chọn tệp / Chọn thư mục ở trang "Quét nâng cao", Xuất CSV, Mở thư mục dữ liệu)
#    — logic phía sau chúng được test trực tiếp (SetCustomPath/GetSelectedScanType/ExportCsv/RefreshDataInfo).
# 3) Tests\ButtonAudit.cs — audit MỌI nút trong app (sidebar + 5 tab, số nút in ra khi chạy):
#    từng nút phải qua Theme.StyleButton/StyleNav thống nhất -> in "ok: <đường dẫn nút>" cho từng nút,
#    kết thúc bằng "== ALL BUTTONS UNIFORM ==" (0 nút lệch)
```

Toàn bộ chạy trong ~1–2 phút; kết quả hiện `PASS/FAIL` từng check.

> **Kiểm tra biên dịch từ macOS/Linux (chỉ *build*, không chạy GUI)** — máy Mac không có `csc` của Visual Studio, nhưng **.NET SDK** (`dotnet`, ở đây `10.0.302`) vẫn build được csproj kiểu cũ `net472` nếu có *reference assemblies* của .NET Framework 4.7.2:
>
> ```bash
> # 1) tải reference assemblies net472 (một lần) rồi build app
> mkdir -p /tmp/ref && cd /tmp/ref && curl -sSL -o ra.nupkg \
>   https://www.nuget.org/api/v2/package/Microsoft.NETFramework.ReferenceAssemblies.net472/1.0.3 \
>   && unzip -oq ra.nupkg -d ra
> cd <gốc repo>
> dotnet build ScanAndRemoveVirus.csproj -p:TargetFrameworkRootPath=/tmp/ref/ra/build/
> #    -> Build succeeded — 0 Warning(s), 0 Error(s)
>
> # 2) build luôn HARNESS UI TEST (không nằm trong csproj app) để bắt lỗi cú pháp
> #    của Tests\UiEndToEnd.cs đối chiếu với mã app thật:
> dotnet build Tests/UiEndToEnd.compile-check.csproj
> #    -> Build succeeded — 0 Warning(s), 0 Error(s)
> ```
>
> Đây là *cổng chặn lỗi cú pháp* trước khi mở Windows — cả 2 lệnh phải **0 Error / 0 Warning**. Bước 1/2 chỉ **biên dịch**, không **chạy**: WinForms/WinExe chỉ chạy trên Windows (`dotnet ui_test_check.exe` trên macOS báo *libhostpolicy.dylib not found*), nên kết quả `PASS/FAIL` thật của 127 + 86 check vẫn phải lấy từ Windows (mục trên).

---

## 🗺 Hướng phát triển

- [ ] Nạp chữ ký **thật** từ bảng `VirusSignatures` (`AntivirusDB.sql` đã có schema) thay chuỗi thử nghiệm → local tự bắt virus thật, giảm phụ thuộc mạng khi tra VT.
- [ ] Luồng **upload VT có kiểm soát**: `POST /files/upload_url` + poll `GET /analyses/{id}` cho tệp nghi vấn chưa có trên cloud — size-gate 32MB, xác nhận riêng tư *từng tệp*, hiện thanh chờ phân tích.
- [ ] Guard hành vi hiện **phát hiện + ghi log + cách ly tệp của tiến trình**; nâng cấp thành chặn/kill tiến trình đang chạy (cân nhắc vì dễ làm phiền phần mềm lành).
- [ ] Kết nối SQL Server thay các file log trong `AppData\` (lịch sử/cách ly/cache/tem).

> ✅ Đã hoàn thành so với bản trước: **cả 10 hàng tab Bảo vệ là cơ chế thật** (USB auto-scan, tải-xuống-MOTW, WMI hành vi, StartUp guard, auto-isolate, auto-update, VT tự động), autostart ghi registry thật, lịch sử/cách ly/cài đặt đều là dữ liệu thật.


---

## 🧪 Giai đoạn UI: dữ liệu giả lập cho 5 tab Chi tiết kết quả quét (bổ sung 24/09/2026)

> ⚠️ **Mục này KHÔNG được triển khai** (chỉ là đề xuất giai đoạn mock). Bản dựng thực tế 25/09/2026 đi thẳng theo
> **mục 14 của `SPEC-UcTongQuan.md`** ("không được dùng mock làm fallback ngầm"): 5 tab chi tiết đọc **dữ liệu thật**
> (`FileInfo`, hash MD5/SHA1/SHA256 tính lazy, `VirusTotalClient` theo SHA256, lý do phát hiện của `ScanEngine`, MOTW `Zone.Identifier`),
> thiếu dữ liệu thì hiện `Chưa có dữ liệu`. **Không** tồn tại `MockUiMode`, `IScanDetailRepository`, `MockScanDetailRepository`
> hay đường đọc `scan-details.mock.json`/`AntivirusDB.mock.json` trong mã nguồn — các file JSON chỉ nằm trong repo làm tài liệu/phụ lục.
> Chi tiết đối chiếu spec ↔ code: xem bảng cuối `SPEC-UcTongQuan.md` (mục 15).

**Ưu tiên của giai đoạn này:** hoàn thiện giao diện WinForms thuần theo 5 ảnh tham chiếu; chưa kết nối hoặc sửa `AntivirusDB`, không thay thế `ScanEngine`, `ScanHistoryStore` và `VirusTotalClient` hiện có. Phần mô tả trước đây về dữ liệu thật áp dụng cho chế độ production, **không áp dụng khi bật `MockUiMode`**. Không dùng kết quả mock để kết luận tệp thật độc hại.

### Khởi chạy chế độ mock

- Nạp `scan-details.mock.json` (UTF-8) qua `MockScanDetailRepository`, map thành các DTO của `IScanDetailRepository`; cấu hình `MockUiMode=true` chỉ trong Debug hoặc tùy chọn nhà phát triển. Không yêu cầu SQL Server, API key hay tạo tệp nguy hiểm.
- 1 phiên `FullSystem` giả lập có 3 phát hiện `setup.exe`, `malware.ps1`, `virus_test.zip`. Chọn một hàng ở bảng trên thì **cả 5 tab dưới** đều đổi theo `detectionId`; tab mặc định là `Thông tin chi tiết`. Dữ liệu trong ảnh (thời gian, đường dẫn, địa chỉ IP, nhãn malware) là ví dụ UI; fixture là nguồn dữ liệu mock duy nhất.
- Nhãn **DỮ LIỆU MẪU** phải hiện ở chế độ Debug/mock. VirusTotal 42/72 ở hàng `setup.exe` là số **giả lập**, không phải kết quả tra cứu; tên engine `MockEngine-*` không phải hãng thực. `virus_test.zip` có `0/72` chỉ để thử giao diện, không có nghĩa tệp an toàn.

### Các tab và tương tác bắt buộc

| Tab | Hiển thị | Kiểm thử tương tác |
|---|---|---|
| Thông tin chi tiết | Card thông tin tệp (tên, đường dẫn, kích thước, loại, tạo/sửa, MD5/SHA1/SHA256); card VirusTotal (donut, tỷ lệ, thời gian, link) | Đổi hàng cập nhật 2 card; copy hash; link mở trang báo cáo **chỉ khi người dùng chủ động bấm**; không gọi API trong mock |
| VirusTotal | Donut `malicious/total`, metadata tệp; 3 bộ lọc Tất cả/Phát hiện/Không phát hiện; bảng engine, kết quả, tên phát hiện, phiên bản, cập nhật; tìm kiếm | Lọc và tìm kiếm kết hợp; đếm đúng; scroll bảng; trạng thái chưa có dữ liệu phân biệt với 0 phát hiện |
| Hành vi | Timeline sự kiện có giờ, loại, mô tả, mức độ; sơ đồ quan hệ tệp→tiến trình/tệp con/địa chỉ mạng; chi tiết tiến trình và hành vi nổi bật | Đổi hàng, cuộn, chọn sự kiện xem chi tiết; không chạy mã hay kết nối IP mock; khi thiếu dữ liệu hiện `Chưa có dữ liệu hành vi` |
| Chuỗi ký tự | Tìm kiếm, bộ lọc loại, checkbox chỉ chuỗi nghi ngờ và phân biệt hoa thường; bảng chuỗi/loại/đánh giá; card chi tiết/offset/ngữ cảnh; phân trang | Bộ lọc phối hợp, chọn hàng, copy; phân trang tính trên kết quả đã lọc; không tự quét tệp thật trong mock |
| Thông tin bổ sung | 4 card: thông tin hệ thống, thuộc tính tệp, mạng liên quan, chữ ký và nhận dạng | Chọn hàng cập nhật 4 card; mở thư mục chứa **chỉ khi đường dẫn thực tồn tại**; copy hash; `NoData` hiện `Chưa có dữ liệu` |

**Thanh trên dùng chung:** Quay lại; thời gian/loại quét; Xuất báo cáo; `DataGridView` STT/Tên tệp/Đường dẫn/Mối đe dọa/Mức độ/SHA256/Thao tác. Nút `Xem` chọn hàng, dropdown Cách ly/Xóa/Tra VirusTotal. Trong mock, mọi thao tác thay đổi trạng thái chỉ cập nhật **bản sao dữ liệu trong RAM**; không di chuyển/xóa tệp, không gửi request, không ghi vào SQL Server. `Xuất báo cáo` gắn nhãn `BÁO CÁO DỮ LIỆU MẪU`.

### Chuyển sang AntivirusDB sau khi hoàn thiện UI

Giữ **nguyên 4 bảng** `Settings`, `VirusSignatures`, `ScanHistory`, `ThreatDetections` cùng index/FK người dùng cung cấp; không ALTER hoặc INSERT dữ liệu mock vào CSDL thật. `ScanHistory.ScanID` liên kết `ThreatDetections.ScanID`; `ThreatDetections.SignatureID` có thể null và tham chiếu `VirusSignatures.SignatureID`. `FileMD5/FileSHA1/FileSHA256`, `FileSizeBytes`, `DetectedAt`, `ActionTaken`, `Status` lấy từ `ThreatDetections`; tên, đường dẫn từ `FileName/OriginalPath`; mức độ ưu tiên lấy `VirusSignatures.Severity` khi có chữ ký.

Schema hiện tại **chưa có** bảng lưu kết quả VirusTotal từng engine, timeline hành vi, chuỗi ký tự, metadata mạng, thuộc tính và phiên bản engine. Khi nối thật, dùng các nguồn service hiện có nếu đã thu thập; thiếu thì hiển thị `Chưa có dữ liệu`. Nếu muốn lưu lâu dài, đề xuất tạo **bảng phụ riêng** (`VirusTotalReports`, `VirusTotalEngineResults`, `BehaviorEvents`, `ExtractedStrings`, `DetectionMetadata`) có FK đến `DetectionID`, không sửa 4 bảng hiện hữu; chốt migration riêng sau. Dùng `IScanDetailRepository` để đổi từ `MockScanDetailRepository` sang `SqlScanDetailRepository`, giữ nguyên các DTO và UI.

### Kiểm thử nghiệm thu tối thiểu

1. Ba hàng mock chọn được; mỗi hàng hiển thị đúng thông tin trên cả 5 tab và không rò dữ liệu của hàng trước.
2. VirusTotal `setup.exe` hiện 42/72; bộ lọc 42 phát hiện và 30 không phát hiện; tìm kiếm kết hợp với bộ lọc.
3. Timeline/sơ đồ hành vi chỉ hiển thị sự kiện có trong fixture; `virus_test.zip` hiện trạng thái trống.
4. Chuỗi ký tự của `setup.exe` có 10 mục, phân trang/lọc/copy đúng; mục không có dữ liệu hiện trạng thái trống.
5. Bật mock rồi dùng Cách ly/Xóa/Tra VirusTotal không tác động hệ thống tệp, mạng hay SQL Server; xuất báo cáo có watermark mẫu.
6. Tắt mock: không hiển thị số liệu giả, nguồn dữ liệu production quyết định nội dung; chưa có dữ liệu phải hiện trạng thái trống.

**Tệp đi kèm:** `scan-details.mock.json`; xem SPEC phần 9–13 để triển khai.


---



---

# Phụ lục UI — Chi tiết kết quả quét: 5 tab theo thiết kế (24/09/2026)

> **Phạm vi:** đặc tả giao diện theo 5 ảnh tham chiếu do người dùng cung cấp. Đây là yêu cầu triển khai mới, ưu tiên hơn các mô tả UI cũ nếu có mâu thuẫn. Giữ nguyên các màn hình Tổng quan, Bảo vệ, Cách ly, Lịch sử và Cài đặt; chỉ hoàn thiện màn hình chi tiết kết quả quét. **Không thêm/sửa bảng `AntivirusDB` ở giai đoạn này.** Bộ dữ liệu trong `scan-details.mock.json` là giả lập, không phải phát hiện mã độc thật hay báo cáo VirusTotal thật.

## 1. Bố cục chung — dùng cho cả 5 tab

- Sidebar bên trái rộng khoảng 245 px: logo Antivirus, Tổng quan (đang chọn), Bảo vệ, Cách ly, Lịch sử; Cài đặt và phiên bản ở cuối. Giữ nguyên sidebar khi đổi tab con; WinForms thuần, không dùng thư viện UI ngoài.
- Vùng nội dung: `← Quay lại`, tiêu đề **Chi tiết kết quả quét**, phụ đề **Danh sách các mối đe dọa được phát hiện trong lần quét vừa rồi.**; góc phải gồm thời gian quét, loại quét và nút **Xuất báo cáo**.
- Bảng phát hiện luôn nằm phía trên tab con, các cột **STT | Tên tệp | Đường dẫn | Mối đe dọa | Mức độ | Hash (SHA256) | Thao tác**. Ba dòng mock: `setup.exe` (Cao), `malware.ps1` (Trung bình), `virus_test.zip` (Thấp). Đây là mức độ gán để thử màu badge, không phải kết luận thực tế về EICAR.
- Nút **Xem** chọn đúng phát hiện và hiển thị dữ liệu 5 tab bên dưới; hàng đang chọn nền xanh nhạt. Mũi tên ở cột Thao tác mở menu riêng cho hàng đó (Cách ly, Xóa, Tra VirusTotal). Các hành động mock chỉ đổi trạng thái trong bộ nhớ; không xóa/di chuyển tệp thật.
- Dải tab nằm ngay dưới bảng: **Thông tin chi tiết | VirusTotal | Hành vi | Chuỗi ký tự | Thông tin bổ sung**. Tab chọn có chữ xanh đậm và gạch chân xanh; chuyển tab không làm mất hàng đang chọn, bộ lọc hoặc dữ liệu đã nạp. Tất cả vùng nội dung có thanh cuộn khi cửa sổ nhỏ; các giá trị dài có tooltip và nút Sao chép khi phù hợp.

## 2. Tab Thông tin chi tiết

Bố cục hai card ngang như ảnh 1. Card **Thông tin tệp** bên trái có icon tệp, tên, đường dẫn, kích thước hiển thị cả MB và byte, loại tệp, thời gian tạo, thời gian sửa đổi, MD5, SHA1 và SHA256. Hash hiển thị rút gọn nếu thiếu chiều rộng nhưng khi sao chép phải là chuỗi đầy đủ; giá trị không có dữ liệu hiện **Chưa có dữ liệu**, không tự tính ra hash giả. Card **VirusTotal** bên phải có logo/tên, nút **Mở trên VirusTotal**, donut số engine phát hiện / số engine có kết quả, mô tả, thời gian phân tích gần nhất, link báo cáo rút gọn và nút copy. Nếu chưa tra cứu: hiện trạng thái **Chưa có kết quả VirusTotal** và nút **Tra cứu**, không hiển thị `0/72` như một kết luận an toàn. Số `42/72` trong ảnh chỉ là dữ liệu mock của `setup.exe`.

## 3. Tab VirusTotal

Nửa trên gồm card trái **Kết quả phân tích từ VirusTotal**: donut `42/72` cho tệp mock `setup.exe`, mô tả, lần phân tích gần nhất; card phải tóm tắt tên tệp, SHA256, kích thước, loại tệp, thời gian tải lên (nếu có), số công cụ phân tích và nút **Xem trên VirusTotal**. Nửa dưới: bộ lọc **Tất cả (72)**, **Phát hiện (42)**, **Không phát hiện (30)**, ô **Tìm kiếm trong kết quả…**; bảng cuộn có cột **STT | Công cụ | Kết quả | Tên phát hiện | Phiên bản | Cập nhật**. Lọc và tìm kiếm kết hợp trên vendor, nhãn và phiên bản; các số đếm tính từ danh sách thực sự nạp, không hard-code 72 cho dữ liệu khác. Các trạng thái API gồm chưa tra, đang tải, có dữ liệu, không tìm thấy (404), lỗi mạng/401/429; không biến 404 thành 'an toàn'. Không tự gửi tệp lên VirusTotal.

## 4. Tab Hành vi

Bốn card theo lưới 2×2: **Phân tích hành vi** (timeline trái trên), **Sơ đồ hành vi** (phải trên), **Chi tiết tiến trình** (trái dưới), **Hành vi nổi bật** (phải dưới). Timeline hiển thị giờ, icon, tên hành vi, mô tả và badge **Nguy hiểm / Đáng ngờ**. Dữ liệu minh họa của `setup.exe`: tạo `powershell.exe`, sửa Registry khởi động, tạo `temp.dll`, kết nối `185.199.111.153:443`, dấu hiệu vô hiệu hóa bảo vệ; toàn bộ chỉ là **kịch bản giả lập**. Sơ đồ bên phải đặt `setup.exe` ở gốc, mũi tên tới `powershell.exe`, `temp.dll` và IP; chỉ vẽ cạnh có sự kiện tương ứng. Card chi tiết tiến trình có tên, đường dẫn, PID, người dùng, dòng lệnh; card hành vi nổi bật liệt kê các nhận định có chứng cứ từ event mock. **Không** tuyên bố ứng dụng đã quan sát hành vi thật chỉ vì phát hiện hash hoặc heuristic. Khi không có telemetry, hiển thị **Chưa ghi nhận hành vi**.

## 5. Tab Chuỗi ký tự

Card trái **Chuỗi ký tự tìm thấy**: chú thích, tìm kiếm, dropdown loại (Tất cả / Tên tiến trình / Tham số dòng lệnh / URL / Registry / PowerShell / Đường dẫn / Phần mở rộng / API), checkbox **Chỉ hiển thị chuỗi nghi ngờ**, checkbox **Phân biệt hoa thường**; bảng **STT | Chuỗi ký tự | Loại | Đánh giá**, thanh cuộn và phân trang (10 dòng/trang). Dữ liệu minh họa: `powershell.exe`, `-nop -w hidden`, URL, `Software\Microsoft\Windows\CurrentVersion\Run`, `Invoke-WebRequest`, `Start-Process`, `C:\Users\Public\`, `.exe`, `CreateRemoteThread`, `VirtualAlloc`. Card phải **Chi tiết chuỗi ký tự** hiển thị nguyên văn chuỗi, nút Sao chép, loại, đánh giá, lý do, offset hex và decimal, ngữ cảnh byte/text xung quanh (highlight chuỗi đang chọn). Chọn dòng đổi card phải mà không đổi tệp đang xem; offset và ngữ cảnh chỉ hiện nếu dữ liệu trích xuất thực sự có. Chuỗi đáng ngờ là **chỉ dấu cần xem xét**, không tự chứng minh mã độc.

## 6. Tab Thông tin bổ sung

Lưới 2×2: **Thông tin hệ thống** (máy, Windows, người dùng, thời gian phát hiện, thời gian sửa đổi, phiên bản phần mềm, loại quét, trạng thái tệp); **Thuộc tính tệp** (tên, đường dẫn, kích thước, loại, tạo/sửa/truy cập, thuộc tính, chủ sở hữu, quyền; nút Mở thư mục chứa); **Thông tin mạng liên quan** (IP, tên miền, cổng, giao thức, quốc gia, ASN nếu được xác minh); **Chữ ký và nhận dạng** (MD5, SHA1, SHA256 có nút copy, PDB Path, Compiler, chữ ký số). Không tự suy ra quốc gia/ASN chỉ từ IP; không hiển thị tên compiler nếu chưa phân tích PE. **Mở thư mục chứa** chỉ bật khi thư mục thật còn tồn tại, nếu tệp đã cách ly thì ưu tiên đường dẫn gốc ở dạng văn bản. Dữ liệu giả lập phải có nhãn **Dữ liệu mẫu** ở chế độ mock.

## 7. Dữ liệu mock, hành vi kiểm thử và tích hợp SQL Server

- Dùng `scan-details.mock.json` làm nguồn dữ liệu chung, khóa phiên `scan.scanId`, khóa hàng `detections[].detectionId`. Các nhánh `file`, `threat`, `virusTotal`, `behavior`, `strings` và `additional` cung cấp dữ liệu cho 5 tab; trường vắng mặt => trạng thái trống, tuyệt đối không tự bịa. Nếu JSON thiếu một nhánh, tab tương ứng vẫn mở được và hiện **Chưa có dữ liệu**.
- Các tương tác cần thử: đổi giữa 3 hàng; đổi 5 tab; lọc/tìm vendor; lọc/tìm/phân trang chuỗi; chọn sự kiện timeline/chuỗi; sao chép hash, link, chuỗi; mở link chỉ khi URL hợp lệ; xuất báo cáo mock ghi rõ dữ liệu giả lập; thu/phóng cửa sổ; thử trường null, danh sách rỗng, 404, 429 và lỗi mạng.
- Khi tích hợp thật, ánh xạ `ScanHistory` -> phiên quét; `ThreatDetections` -> hàng phát hiện và hash; `VirusSignatures` -> tên, loại và mức độ chữ ký; `Settings` -> tên/phiên bản/các công tắc. **Schema gốc không có** bảng kết quả từng vendor, sự kiện hành vi, chuỗi trích xuất, metadata máy, ASN; các phần đó phải lấy từ dịch vụ, tệp log hoặc cache riêng, hoặc thiết kế mở rộng **sau khi được duyệt**. Không được giả vờ rằng bốn bảng hiện tại lưu đủ cả 5 tab.
- Trong `AntivirusDB.schema.sql` và phần SQL ở cuối tài liệu này có **nguyên văn DDL hiện tại** (4 bảng, khóa ngoại và chỉ mục). Giữ nguyên schema và không chạy `CREATE DATABASE` trên cơ sở dữ liệu đang có mà chưa kiểm tra.

## Phụ lục: Cơ sở dữ liệu AntivirusDB hiện tại (SQL Server — nguyên bản)

Đây là **toàn bộ lệnh tạo CSDL do người dùng cung cấp**, giữ nguyên tên bảng, cột, kiểu dữ liệu, giá trị mặc định, khóa ngoại và chỉ mục. CSDL hiện có **4 bảng**: `Settings`, `VirusSignatures`, `ScanHistory`, `ThreatDetections`. Đây là schema dự kiến kết nối sau khi hoàn thiện giao diện; **không thực thi script này khi chạy chế độ mock**. Script độc lập đi kèm: `AntivirusDB.schema.sql`. Không chạy lại `CREATE DATABASE` trên CSDL đã tồn tại.

```sql
create database AntivirusDB
go
use AntivirusDB
go
--*Tao bang*--
create table Settings
(
    SettingID INT IDENTITY(1,1) PRIMARY KEY,
    ProgramName NVARCHAR(100) NOT NULL DEFAULT N'ANTIVIRUS',
    ProgramVersion VARCHAR(20) NOT NULL DEFAULT '1.0.0.0',
    VirusDatabaseVersion VARCHAR(30) NOT NULL DEFAULT '1.0.0.2025',
    LastDatabaseUpdate DATETIME2 NULL,
    RealTimeProtection BIT NOT NULL DEFAULT 1,
    FileProtection BIT NOT NULL DEFAULT 1,
    USBProtection BIT NOT NULL DEFAULT 1,
    WebProtection BIT NOT NULL DEFAULT 1,
    DownloadProtection BIT NOT NULL DEFAULT 1,
    RansomwareProtection BIT NOT NULL DEFAULT 1,
    AutoStart BIT NOT NULL DEFAULT 1,
    AutoUpdate BIT NOT NULL DEFAULT 1,
    SubmitSamples BIT NOT NULL DEFAULT 0,
    ShowNotifications BIT NOT NULL DEFAULT 1,
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
)
go
create table VirusSignatures
(
    SignatureID BIGINT IDENTITY(1,1) PRIMARY KEY,
    MalwareName NVARCHAR(200) NOT NULL,
    MalwareFamily NVARCHAR(100) NULL,
    Category NVARCHAR(100) NULL,
    SignatureType VARCHAR(30) NOT NULL,
    MD5 CHAR(32) NULL,
    SHA1 CHAR(40) NULL,
    SHA256 CHAR(64) NULL,
    FileExtension NVARCHAR(50) NULL,
    Severity VARCHAR(20) NOT NULL DEFAULT 'Medium',
    Description NVARCHAR(1000) NULL,
    RecommendedAction VARCHAR(30) NOT NULL DEFAULT 'Quarantine',
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NULL
)
go
--*Index phuc vu tra cuu hash khi quet*--
create index IX_VirusSignatures_MD5 on VirusSignatures(MD5);
create index IX_VirusSignatures_SHA1 on VirusSignatures(SHA1);
create index IX_VirusSignatures_SHA256 on VirusSignatures(SHA256);
go
create table ScanHistory
(
    ScanID BIGINT IDENTITY(1,1) PRIMARY KEY,
    ScanType VARCHAR(20) NOT NULL,
    StartedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL,
    ScanLocation NVARCHAR(1000) NULL,
    FilesScanned BIGINT NOT NULL DEFAULT 0,
    ThreatCount INT NOT NULL DEFAULT 0,
    ResultStatus VARCHAR(30) NOT NULL DEFAULT 'Running',
    ActivityTitle NVARCHAR(200) NULL,
    ActivityDetails NVARCHAR(1000) NULL,
    ErrorMessage NVARCHAR(1000) NULL
)
go
create table ThreatDetections
(
    DetectionID BIGINT IDENTITY(1,1) PRIMARY KEY,
    ScanID BIGINT NULL,
    SignatureID BIGINT NULL,
    FileName NVARCHAR(260) NOT NULL,
    OriginalPath NVARCHAR(1000) NOT NULL,
    ThreatName NVARCHAR(200) NOT NULL,
    DetectedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FileSizeBytes BIGINT NULL,
    FileMD5 CHAR(32) NULL,
    FileSHA1 CHAR(40) NULL,
    FileSHA256 CHAR(64) NULL,
    ActionTaken VARCHAR(30) NOT NULL DEFAULT 'Quarantined',
    Status VARCHAR(30) NOT NULL DEFAULT 'Quarantined',
    QuarantinePath NVARCHAR(1000) NULL,
    RestoredAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL,
    CONSTRAINT FK_ThreatDetections_Scan
        FOREIGN KEY (ScanID)
        REFERENCES ScanHistory(ScanID),
    CONSTRAINT FK_ThreatDetections_Signature
        FOREIGN KEY (SignatureID)
        REFERENCES VirusSignatures(SignatureID)
)
go
create index IX_ThreatDetections_DetectedAt on ThreatDetections(DetectedAt DESC);
create index IX_ThreatDetections_Status on ThreatDetections(Status);
create index IX_ThreatDetections_SHA256 on ThreatDetections(FileSHA256);
go
```


## Ảnh tham chiếu 5 tab

- [Thông tin chi tiết](UI-References/01-thong-tin-chi-tiet.png)
- [VirusTotal](UI-References/02-virustotal.png)
- [Hành vi](UI-References/03-hanh-vi.png)
- [Chuỗi ký tự](UI-References/04-chuoi-ky-tu.png)
- [Thông tin bổ sung](UI-References/05-thong-tin-bo-sung.png)


---

---
# CHẾ ĐỘ TRIỂN KHAI HIỆN TẠI — JSON GIẢ LẬP THEO SCHEMA `AntivirusDB`

**Áp dụng ưu tiên cho toàn bộ tài liệu:** Trong giai đoạn hoàn thiện UI, toàn bộ màn hình sử dụng `AntivirusDB.mock.json`, **không kết nối SQL Server**. File JSON được thiết kế theo đúng tên bảng và cột trong `AntivirusDB.schema.sql` (4 bảng gốc) và `AntivirusDB.extensions.sql` (6 bảng bổ sung cần thiết cho 5 tab chi tiết). Không sửa 4 bảng gốc. Các hướng dẫn cũ yêu cầu đọc `scanhistory.log`, `settings.ini`, `quarantine.log` hoặc kết nối SQL trực tiếp chỉ là mô tả phiên bản trước; với UI mới phải đọc/ghi mock qua repository.

## Cấu trúc dữ liệu

- `AntivirusDB.mock.json`: đối tượng `tables` gồm `Settings`, `VirusSignatures`, `ScanHistory`, `ThreatDetections`, `DetectionFileMetadata`, `VirusTotalReports`, `VirusTotalEngines`, `DetectionBehaviorEvents`, `DetectionStrings`, `DetectionNetworkEvents`. **Mỗi phần tử trong mảng là một hàng SQL**; khóa JSON giữ nguyên chữ hoa/thường và tên cột SQL, các cột không có dữ liệu dùng `null`, không tạo các tên khác như `scanId` hoặc `file.name` trong file chuẩn này.
- `AntivirusDB.schema.sql`: nguyên văn cấu trúc 4 bảng của bạn. `AntivirusDB.extensions.sql`: sáu bảng phụ để biểu diễn dữ liệu chưa có trong schema gốc. Hai script là hợp đồng schema; **không cần chạy SQL Server khi thử UI**.
- `scan-details.mock.json`: fixture giao diện kiểu lồng từ phiên bản cũ, chỉ giữ làm tài liệu tham khảo; **không dùng làm nguồn dữ liệu UI chính**.
- Các trường có hậu tố `ID` là khóa nối bảng: `ScanHistory.ScanID → ThreatDetections.ScanID`, `VirusSignatures.SignatureID → ThreatDetections.SignatureID`, `ThreatDetections.DetectionID →` các bảng chi tiết; `VirusTotalReports.ReportID → VirusTotalEngines.ReportID`; `DetectionBehaviorEvents.EventID → DetectionNetworkEvents.BehaviorEventID`.

## Toàn bộ màn hình đọc chung một nguồn

| Màn hình | Mảng trong `tables` |
|---|---|
| Tổng quan | `ScanHistory`, `ThreatDetections`, `Settings` |
| Bảo vệ / Cài đặt | `Settings` |
| Lịch sử | `ScanHistory`, `ThreatDetections` |
| Cách ly | `ThreatDetections` với `Status=Quarantined` |
| Chi tiết: Thông tin chi tiết | `ThreatDetections`, `VirusSignatures`, `DetectionFileMetadata`, `VirusTotalReports` |
| Chi tiết: VirusTotal | `VirusTotalReports`, `VirusTotalEngines` |
| Chi tiết: Hành vi | `DetectionBehaviorEvents`, `DetectionNetworkEvents`, `DetectionFileMetadata` |
| Chi tiết: Chuỗi ký tự | `DetectionStrings` |
| Chi tiết: Thông tin bổ sung | `DetectionFileMetadata`, `DetectionNetworkEvents`, `ThreatDetections`, `Settings` |

## Luồng mock và chuyển đổi sang SQL

Tạo `IAntivirusRepository` với `JsonAntivirusRepository` hiện tại và `SqlAntivirusRepository` sau này; UI chỉ làm việc với DTO cùng tên trường và cùng khóa. Khi khởi động, đọc JSON vào bộ nhớ, kiểm tra quan hệ khóa ngoại, chọn phiên `ScanHistory` mới nhất. Chọn dòng đe dọa bằng `DetectionID`, tải 5 tab theo đúng ID này; đổi dòng phải xóa dữ liệu tab cũ và bind lại. Cài đặt, cách ly/khôi phục/xóa trong **mock mode** chỉ thay đổi bản sao JSON (ghi file nguyên tử và giữ bản gốc), tuyệt đối không thay đổi tệp hệ thống hoặc gọi API VirusTotal. Phân biệt rõ `MockResult` với dữ liệu thật; không khẳng định đã phân tích hoặc phát hiện virus thật.

Khi UI hoàn tất, thay dependency injection từ `JsonAntivirusRepository` sang `SqlAntivirusRepository`; SQL sử dụng cùng 10 bảng, tên cột, kiểu nullable và liên kết ID; dùng câu lệnh tham số. Các trường `IDENTITY` do SQL Server tự sinh khi INSERT; không lấy ID fixture làm ID thật. Bảng chi tiết mở rộng là **tùy chọn về triển khai**, nhưng muốn giữ đủ năm tab khi dùng SQL thì phải tạo sáu bảng phụ.

## Cấu trúc SQL gốc và bảng mở rộng

Bốn bảng gốc của bạn được ghi nguyên văn trong `AntivirusDB.schema.sql` và nhúng dưới đây; bảng phụ được ghi trong `AntivirusDB.extensions.sql` và là nguồn đối chiếu cho JSON.

```sql
create database AntivirusDB
go
use AntivirusDB
go
--*Tao bang*--
create table Settings
(
    SettingID INT IDENTITY(1,1) PRIMARY KEY,
    ProgramName NVARCHAR(100) NOT NULL DEFAULT N'ANTIVIRUS',
    ProgramVersion VARCHAR(20) NOT NULL DEFAULT '1.0.0.0',
    VirusDatabaseVersion VARCHAR(30) NOT NULL DEFAULT '1.0.0.2025',
    LastDatabaseUpdate DATETIME2 NULL,
    RealTimeProtection BIT NOT NULL DEFAULT 1,
    FileProtection BIT NOT NULL DEFAULT 1,
    USBProtection BIT NOT NULL DEFAULT 1,
    WebProtection BIT NOT NULL DEFAULT 1,
    DownloadProtection BIT NOT NULL DEFAULT 1,
    RansomwareProtection BIT NOT NULL DEFAULT 1,
    AutoStart BIT NOT NULL DEFAULT 1,
    AutoUpdate BIT NOT NULL DEFAULT 1,
    SubmitSamples BIT NOT NULL DEFAULT 0,
    ShowNotifications BIT NOT NULL DEFAULT 1,
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
)
go
create table VirusSignatures
(
    SignatureID BIGINT IDENTITY(1,1) PRIMARY KEY,
    MalwareName NVARCHAR(200) NOT NULL,
    MalwareFamily NVARCHAR(100) NULL,
    Category NVARCHAR(100) NULL,
    SignatureType VARCHAR(30) NOT NULL,
    MD5 CHAR(32) NULL,
    SHA1 CHAR(40) NULL,
    SHA256 CHAR(64) NULL,
    FileExtension NVARCHAR(50) NULL,
    Severity VARCHAR(20) NOT NULL DEFAULT 'Medium',
    Description NVARCHAR(1000) NULL,
    RecommendedAction VARCHAR(30) NOT NULL DEFAULT 'Quarantine',
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NULL
)
go
--*Index phuc vu tra cuu hash khi quet*--
create index IX_VirusSignatures_MD5 on VirusSignatures(MD5);
create index IX_VirusSignatures_SHA1 on VirusSignatures(SHA1);
create index IX_VirusSignatures_SHA256 on VirusSignatures(SHA256);
go
create table ScanHistory
(
    ScanID BIGINT IDENTITY(1,1) PRIMARY KEY,
    ScanType VARCHAR(20) NOT NULL,
    StartedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL,
    ScanLocation NVARCHAR(1000) NULL,
    FilesScanned BIGINT NOT NULL DEFAULT 0,
    ThreatCount INT NOT NULL DEFAULT 0,
    ResultStatus VARCHAR(30) NOT NULL DEFAULT 'Running',
    ActivityTitle NVARCHAR(200) NULL,
    ActivityDetails NVARCHAR(1000) NULL,
    ErrorMessage NVARCHAR(1000) NULL
)
go
create table ThreatDetections
(
    DetectionID BIGINT IDENTITY(1,1) PRIMARY KEY,
    ScanID BIGINT NULL,
    SignatureID BIGINT NULL,
    FileName NVARCHAR(260) NOT NULL,
    OriginalPath NVARCHAR(1000) NOT NULL,
    ThreatName NVARCHAR(200) NOT NULL,
    DetectedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FileSizeBytes BIGINT NULL,
    FileMD5 CHAR(32) NULL,
    FileSHA1 CHAR(40) NULL,
    FileSHA256 CHAR(64) NULL,
    ActionTaken VARCHAR(30) NOT NULL DEFAULT 'Quarantined',
    Status VARCHAR(30) NOT NULL DEFAULT 'Quarantined',
    QuarantinePath NVARCHAR(1000) NULL,
    RestoredAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL,
    CONSTRAINT FK_ThreatDetections_Scan
        FOREIGN KEY (ScanID)
        REFERENCES ScanHistory(ScanID),
    CONSTRAINT FK_ThreatDetections_Signature
        FOREIGN KEY (SignatureID)
        REFERENCES VirusSignatures(SignatureID)
)
go
create index IX_ThreatDetections_DetectedAt on ThreatDetections(DetectedAt DESC);
create index IX_ThreatDetections_Status on ThreatDetections(Status);
create index IX_ThreatDetections_SHA256 on ThreatDetections(FileSHA256);
go
```

```sql
-- Run AFTER AntivirusDB.schema.sql; no changes to the four original tables.
USE AntivirusDB;
GO
IF OBJECT_ID('dbo.DetectionFileMetadata','U') IS NULL
CREATE TABLE dbo.DetectionFileMetadata (
 DetectionID BIGINT NOT NULL PRIMARY KEY REFERENCES dbo.ThreatDetections(DetectionID),
 CreatedAt DATETIME2 NULL, ModifiedAt DATETIME2 NULL, AccessedAt DATETIME2 NULL,
 FileType NVARCHAR(100) NULL, MimeType NVARCHAR(100) NULL, FileAttributes NVARCHAR(200) NULL,
 OwnerName NVARCHAR(260) NULL, AccessPermissions NVARCHAR(200) NULL,
 ComputerName NVARCHAR(260) NULL, OperatingSystem NVARCHAR(260) NULL, UserName NVARCHAR(260) NULL,
 Compiler NVARCHAR(200) NULL, PdbPath NVARCHAR(1000) NULL,
 SignatureStatus NVARCHAR(100) NULL, SignerName NVARCHAR(300) NULL,
 CollectedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO
IF OBJECT_ID('dbo.VirusTotalReports','U') IS NULL
CREATE TABLE dbo.VirusTotalReports (
 ReportID BIGINT IDENTITY(1,1) PRIMARY KEY,
 DetectionID BIGINT NOT NULL REFERENCES dbo.ThreatDetections(DetectionID),
 FileSHA256 CHAR(64) NOT NULL, AnalyzedAt DATETIME2 NULL,
 RetrievedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
 MaliciousCount INT NOT NULL DEFAULT 0, SuspiciousCount INT NOT NULL DEFAULT 0,
 UndetectedCount INT NOT NULL DEFAULT 0, HarmlessCount INT NOT NULL DEFAULT 0,
 TimeoutCount INT NOT NULL DEFAULT 0, FailureCount INT NOT NULL DEFAULT 0,
 ReportUrl NVARCHAR(1000) NULL
);
GO
IF OBJECT_ID('dbo.VirusTotalEngines','U') IS NULL
CREATE TABLE dbo.VirusTotalEngines (
 EngineID BIGINT IDENTITY(1,1) PRIMARY KEY,
 ReportID BIGINT NOT NULL REFERENCES dbo.VirusTotalReports(ReportID),
 EngineName NVARCHAR(200) NOT NULL, Category VARCHAR(30) NOT NULL,
 ResultName NVARCHAR(300) NULL, EngineVersion NVARCHAR(100) NULL, EngineUpdate DATE NULL,
 CONSTRAINT UQ_VTEngine_ReportName UNIQUE (ReportID, EngineName)
);
GO
IF OBJECT_ID('dbo.DetectionBehaviorEvents','U') IS NULL
CREATE TABLE dbo.DetectionBehaviorEvents (
 EventID BIGINT IDENTITY(1,1) PRIMARY KEY,
 DetectionID BIGINT NOT NULL REFERENCES dbo.ThreatDetections(DetectionID),
 OccurredAt DATETIME2 NULL, EventType VARCHAR(50) NOT NULL,
 Title NVARCHAR(200) NOT NULL, Description NVARCHAR(2000) NULL,
 Severity VARCHAR(20) NULL, ProcessName NVARCHAR(260) NULL,
 ProcessId INT NULL, ParentProcessId INT NULL, CommandLine NVARCHAR(MAX) NULL,
 TargetPath NVARCHAR(1000) NULL, RegistryKey NVARCHAR(1000) NULL,
 RemoteAddress VARCHAR(45) NULL, RemotePort INT NULL
);
GO
IF OBJECT_ID('dbo.DetectionStrings','U') IS NULL
CREATE TABLE dbo.DetectionStrings (
 StringID BIGINT IDENTITY(1,1) PRIMARY KEY,
 DetectionID BIGINT NOT NULL REFERENCES dbo.ThreatDetections(DetectionID),
 StringValue NVARCHAR(MAX) NOT NULL, StringType NVARCHAR(100) NULL,
 Assessment VARCHAR(30) NULL, Reason NVARCHAR(1000) NULL,
 ByteOffset BIGINT NULL, ContextBefore NVARCHAR(500) NULL, ContextAfter NVARCHAR(500) NULL
);
GO
IF OBJECT_ID('dbo.DetectionNetworkEvents','U') IS NULL
CREATE TABLE dbo.DetectionNetworkEvents (
 NetworkEventID BIGINT IDENTITY(1,1) PRIMARY KEY,
 DetectionID BIGINT NOT NULL REFERENCES dbo.ThreatDetections(DetectionID),
 BehaviorEventID BIGINT NULL REFERENCES dbo.DetectionBehaviorEvents(EventID),
 ObservedAt DATETIME2 NULL, RemoteIP VARCHAR(45) NULL,
 DomainName NVARCHAR(253) NULL, RemotePort INT NULL,
 Protocol VARCHAR(20) NULL, Country NVARCHAR(100) NULL,
 AsnNumber BIGINT NULL, AsnOrganization NVARCHAR(260) NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_VTReports_Detection' AND object_id=OBJECT_ID('dbo.VirusTotalReports'))
CREATE INDEX IX_VTReports_Detection ON dbo.VirusTotalReports(DetectionID, AnalyzedAt DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Behavior_Detection' AND object_id=OBJECT_ID('dbo.DetectionBehaviorEvents'))
CREATE INDEX IX_Behavior_Detection ON dbo.DetectionBehaviorEvents(DetectionID, OccurredAt, EventID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Strings_Detection' AND object_id=OBJECT_ID('dbo.DetectionStrings'))
CREATE INDEX IX_Strings_Detection ON dbo.DetectionStrings(DetectionID, StringID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Network_Detection' AND object_id=OBJECT_ID('dbo.DetectionNetworkEvents'))
CREATE INDEX IX_Network_Detection ON dbo.DetectionNetworkEvents(DetectionID, ObservedAt);
GO
```
