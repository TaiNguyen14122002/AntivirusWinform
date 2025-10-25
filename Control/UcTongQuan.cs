using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;
using ScanAndRemoveVirus.Database;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>4 màn hình của tab Tổng quan (SPEC-UcTongQuan.md §1).</summary>
    public enum TongQuanView { AnToan, PhatHienDeDoa, ChiTiet, QuetNangCao }

    /// <summary>
    /// TAB TỔNG QUAN — thiết kế lại theo README (mục 1) + SPEC-UcTongQuan.md:
    ///  (a) pnlAnToan        — trạng thái an toàn (khiên xanh, không đe dọa) + thẻ "Hoạt động gần đây"
    ///  (b) pnlPhatHienDeDoa — trạng thái phát hiện đe dọa (tròn đỏ + bảng đe dọa + hành động hàng loạt)
    ///  (c) pnlChiTietKetQua — trang "Chi tiết kết quả quét" (bảng đầy đủ + 5 tab chi tiết)
    ///  (d) pnlQuetNangCao   — trang "Quét nâng cao" (Toàn bộ / Thư mục / Tệp / Tùy chỉnh)
    /// (25/09/2026) Thẻ "Tuỳ chọn quét nhanh" đã bị bỏ: "Quét ngay" luôn là Quét nhanh, còn quét toàn bộ/
    /// tùy chọn nằm ở trang (d). Thẻ "Mối đe dọa được phát hiện" chỉ hiện ở trạng thái (b); ở trạng thái (a)
    /// chỗ đó là thẻ "Hoạt động gần đây" (bảng cuộn được, đọc trực tiếp scanhistory.log).
    /// (25/09/2026, lần 2) Bỏ tiếp 3 thứ khỏi trang: thẻ số liệu "4. Đang cách ly" (grpQuarantine) — còn 3 thẻ,
    /// thanh loading quét (pgbScan) — chỉ giữ nhãn trạng thái quét, và cả khối "cập nhật dữ liệu"
    /// (chip "CSDL virus:"/"Cập nhật cuối:" + nút "Kiểm tra cập nhật").
    /// (25/09/2026, lần 4) Bố cục lại cho gọn + Responsive:
    ///  • Gỡ hẳn hàng header riêng (pnlOverviewHeader + hàng 54px của tableLayoutPanel11): 2 nút
    ///    "▶ Quét ngay" / "⚙ Quét nâng cao ▾" (flowHeaderActions) nay nằm NGAY DƯỚI khối trạng thái (a)
    ///    pnlAnToan — hàng 4 của tlpAnToanText (đúng như README mục 1: "2 nút cạnh nhau ngay bên dưới").
    ///  • tableLayoutPanel12 còn **6 hàng** (bỏ hàng trống 44px): hero 164 · số liệu 104 · bảng đe dọa 400 ·
    ///    "Hoạt động gần đây" 400 · (c) 620 · (d) 640. Hàng nội dung chính của màn hình đang mở được
    ///    chuyển sang Percent 100 (SetRowFill) nên GIÃN theo cửa sổ — không chừa khoảng trắng ở đáy.
    ///  • Ở (b), khi phiên quét đang chạy thì nút "Quét lại" tự đổi thành "Hủy quét" (vì 2 nút quét nay
    ///    nằm trong khối (a)).
    ///  • Sửa lỗi 2 nút của hero (b) bị neo ở x=1854 (ngoài panel nên vô hình): nay cách mép phải 24px.
    /// (25/09/2026, lần 5) Trang (d) "Quét nâng cao" là **màn hình riêng**: khi mở nó, cả hàng 3 thẻ số liệu
    /// (Mối đe dọa / Tệp đã quét / Lần quét gần nhất — `pnlThongKe`) **biến mất** (hàng hạ về 0px + `Visible = false`)
    /// để nhường chiều cao cho nội dung quét.
    /// (25/09/2026, lần 8) Trang (c) "Chi tiết kết quả quét" cũng là **màn hình riêng** nên hàng 3 thẻ số liệu
    /// đó **không hiện nữa**: hàng 3 thẻ chỉ thuộc Tổng quan (a)/(b) — thông tin thời gian/loại quét vốn đã nằm
    /// ở header của (c) ("Thời gian quét: …" · "Loại quét: …"), nhờ vậy bảng chi tiết + 5 tab có thêm chiều cao.
    /// Quay lại (a)/(b) (nút "← Quay lại" của (c)/(d) đều dẫn về (b) nếu còn đe dọa, ngược lại về (a)) thì
    /// cả 3 thẻ hiện lại đúng 104px.
    /// (25/09/2026, lần 9) **Dải loading quét**: bấm BẤT KỲ nút quét nào — "▶ Quét ngay" (a)/(b), "Quét lại" ở
    /// (b) khi rảnh, hay "Bắt đầu quét" ở (d) — đều đi qua đúng 1 đường `ChayPhienQuet` → `SetScanning(true)`
    /// nên **dải loading hiện NGAY ở đầu trang Tổng quan**: hàng 0 của `tableLayoutPanel11` giãn 0 → 46px
    /// (`pnlLoadingQuet`) với vòng xoay GDI+ `spinnerDangQuet` (`Control\LoadingSpinner.cs` — Timer 60ms tự
    /// chạy/dừng theo `Visible`), tiêu đề "Đang quét…" + dòng chi tiết bám tiến trình thật
    /// (`lblLoadingChiTiet` mirror `lblScanProgress`) và nút "Hủy quét" (`btnHuyQuetLoading`) để hủy được từ
    /// **mọi** màn hình — kể cả (d), nơi nút "Bắt đầu quét" bị khoá trong lúc quét (`OnSetScanningAdvanced`).
    /// Phiên xong/hủy/lỗi → `SetScanning(false)` hạ hàng về **0px** + ẩn dải, nội dung trang nhận lại chiều cao.
    /// Dải nằm **trên** nội dung (không phủ lên) nên không che nút nào; control không bị gỡ/dựng lại — chỉ
    /// `Visible` + chiều cao hàng, y như `pnlThongKe`/`pnlHeroHost`.
    /// Không đổi tầng Services: dữ liệu vẫn từ ScanEngine / ScanHistoryStore / VirusTotalClient /
    /// RealTimeProtection; riêng (d) dùng thêm DirectorySizeCalculator (tính size nền).
    /// </summary>
    public partial class UcTongQuan : UserControl
    {
        // ===== HỢP ĐỒNG CŨ (giữ nguyên tên trường/hàm — Tests\UiEndToEnd.cs phản chiếu qua reflection) =====
        private string customScanPath;      // đường dẫn "quét 1 vị trí" — chỉ dùng khi quét không qua thẻ nhanh (đã bỏ)
        private bool isScanning;            // đang có phiên quét chạy
        private CancellationTokenSource cts;
        private ScanTelemetry activeScanTelemetry;
        private ScanTelemetry.Reading activeScanReading;

        // ===== TRẠNG THÁI TRANG =====
        private TongQuanView currentView = TongQuanView.AnToan;
        private readonly List<ThreatRow> threatRows = new List<ThreatRow>();
        private readonly Dictionary<string, ThreatRow> rowByPath = new Dictionary<string, ThreatRow>(StringComparer.OrdinalIgnoreCase);
        private ScanJob lastJob;            // để nút "Quét lại" chạy lại đúng phạm vi vừa rồi
        private ScanResult lastResult;
        private DateTime lastScanTime = DateTime.MinValue;
        private int lastScannedFiles;
        private int detailRowIndex = -1;    // dòng đang chọn ở trang (c)
        private const int MaxActivityRows = 100;   // trần số dòng nạp vào bảng "Hoạt động gần đây"
        private const float HeroHeight = 252F;     // hàng hero (a)/(b) của tableLayoutPanel12: panel 316 + 2 lề 2px
        private const float ThongKeHeight = 152F;  // hàng 3 thẻ số liệu của tableLayoutPanel12: panel 100 + 2 lề 2px (chỉ ở (a)/(b))
        private const float LoadingHeight = 46F;   // hàng 0 của tableLayoutPanel11 — dải loading quét (lần 9): panel 46px khi quét, 0px khi rảnh
        private const int RongToiThieu = 700;      // (Theme.ScrollablePage) bề rộng tối thiểu vùng Tổng quan ở (a)/(b)/(c)
        private const int CaoToiThieu = 560;       // chiều cao tối thiểu vùng Tổng quan (mọi màn hình): nhỏ hơn -> cuộn dọc

        /// <summary>Một dòng đe dọa hiển thị ở (b)/(c) — gộp dữ liệu ScanEngine + VirusTotal.</summary>
        internal sealed class ThreatRow
        {
            public string Path;
            public string ThreatText;                        // "Chữ ký: ..." / "Heuristic: ..." / "VirusTotal [...]: ..."
            public Theme.ThreatLevel Level;
            public bool Quarantined;
            public bool Deleted;
            public bool Allowed;
            public VirusTotalReport VtReport;                // null = chưa tra
            public string VtLink;                            // link báo cáo trên virustotal.com (nếu có)
            public DateTime VtQueriedAt = DateTime.MinValue; // thời điểm app tra VT (API không trả về ngày phân tích)
            public string HashSha256;                        // hash đã tính (null = chưa tính)
            public string HashMd5;
            public string HashSha1;
            public long SizeBytes;
            public DateTime CreatedAt = DateTime.MinValue;
            public DateTime ModifiedAt = DateTime.MinValue;

            public string LevelText { get { return Theme.LevelText(Level); } }
            public string StatusText
            {
                get { return Deleted ? "Đã xóa" : (Quarantined ? "Đã cách ly" : (Allowed ? "Đã cho phép" : "Đang có trên máy")); }
            }
            public bool Resolved { get { return Deleted || Quarantined || Allowed; } }
        }

        /// <summary>Mô tả 1 phiên quét cần chạy (từ thẻ quét nhanh hoặc trang Quét nâng cao).</summary>
        internal sealed class ScanJob
        {
            public ScanType Type = ScanType.Quick;
            public string CustomPath;                        // dùng khi Targets == null
            public List<string> Targets;                     // nhiều vị trí (trang Quét nâng cao)
            public string DisplayName = "Quét nhanh";        // tiêu đề thông báo + "Loại quét" ở (c)
            public string ScopeText = "Khu vực hệ thống";    // cột "Phạm vi" trong scanhistory.log
            public bool AutoQuarantine;                      // "Tự động cách ly khi phát hiện" (chỉ ở (d))
            public HashSet<string> ExtensionFilter;          // null = không lọc theo loại tệp (chỉ ở (d))
            public long SkipLargerThanBytes;                 // 0 = không bỏ qua tệp lớn (chỉ ở (d))
            public bool FromAdvanced;                        // quét từ (d) -> có cách ly tự động/lọc
        }

        public UcTongQuan()
        {
            InitializeComponent();


            // Responsive: cửa sổ nhỏ -> cuộn thay vì cắt nội dung (ngưỡng 980x640; trạng thái (a) cần tối
            // thiểu hero 320 + hàng 3 thẻ số liệu 104 = 424px + lề, phần còn lại là thẻ "Hoạt động gần đây").
            // Riêng trang (d) tự xếp lại theo bề rộng nên HienThi() hạ bề rộng tối thiểu (lần 7).
            // Dashboard tự tính vùng cuộn từ màn hình đang mở, bỏ qua các trang ẩn.
            ApplyTheme();
            BuildChiTietView();       // ruột trang (c) — UcTongQuan.ChiTiet.cs
            BuildQuetNangCaoView();   // ruột trang (d) — UcTongQuan.QuetNangCao.cs
            BuildDashboard();
            WireEvents();
            HienThi(TongQuanView.AnToan, false);
            LoadProtectionStatus();
        }

        // ================= GIAO DIỆN: MỘT NGUỒN THEME DUY NHẤT =================

        private void ApplyTheme()
        {

            // ===== ĐẦU TRANG (chuẩn chung 5 tab — Tests\UiEndToEnd.cs §9b) =====
            //Theme.StylePageHeader(lblOverviewTitle, lblOverviewSubtitle);
            //lblOverviewSubtitle.Text = "Tình trạng bảo vệ và hoạt động gần đây của máy tính.";

            // ===== HERO (a)/(b): khiên lớn canh giữa + 2 nút bo góc =====
            picShield.Image = UiIcons.HeroBadge(140, true);
            picCanhBao.Image = UiIcons.HeroBadge(140, false);
            lblProtectionStatus.Font = Theme.HeroBigFont;
            lblPhatHienTitle.Font = Theme.HeroBigFont;
            lblAnToanSub.Font = Theme.BodyBigFont;
            lblPhatHienSub.Font = Theme.BodyBigFont;
            UiKit.Apply(btnScanNow, Theme.BtnRole.Primary);
            UiKit.Apply(btnQuetNangCao, Theme.BtnRole.Neutral);
            UiKit.Apply(btnXemChiTiet, Theme.BtnRole.Primary);
            UiKit.Apply(btnQuetLai, Theme.BtnRole.Neutral);
            btnScanNow.Icon = UiIcons.Play(13, Color.White);
            btnQuetNangCao.Icon = UiIcons.Gear(15, Theme.TextMid);
            btnXemChiTiet.Icon = UiIcons.External(13, Color.White);
            btnQuetLai.Icon = UiIcons.Refresh(13, Theme.TextMid);

            // ===== HÀNG 3 THẺ SỐ LIỆU: Panel trơn, vạch chia do PnlThongKe_Paint vẽ =====
            lblThreatTitle.Font = Theme.BoldFont;
            lblScannedTitle.Font = Theme.BoldFont;
            lblLastScanTitle.Font = Theme.BoldFont;
            lblThreatCount.Font = Theme.StatBigFont;
            lblScannedCount.Font = Theme.StatBigFont;
            // Token chung (trước đây new Font(Theme.UiFontFamily, 18.25F, Bold) tại đây: hằng số cỡ chữ nằm rải
            // ngoài Theme và GDI font tạo ra không bao giờ được giải phóng).
            lblLastScanDate.Font = Theme.StatValueFont;
            lblThreatText.Font = Theme.SmallFont;
            lblScannedUnit.Font = Theme.SmallFont;
            lblLastScanType.Font = Theme.SmallFont;
            lblThreatCount.ForeColor = Theme.Green;
            lblThreatText.ForeColor = Theme.Green;
            lblScannedCount.ForeColor = Theme.Blue;

            // ===== 2 THẺ LỚN (UiCard): header do UiCard lo, ruột gắn vào Body() ở đây vì Designer
            // không gọi được phương thức — cùng lý do đã ghi ở UcTongQuan.ChiTiet.cs =====
            grpAction.SetHeader("Mối đe dọa được phát hiện", UiIcons.AlertCircle(18));
            grpAction.AddHeaderAction(lnkXemTatCa);
            grpAction.AddHeaderAction(lblThreatSummary);
            grpAction.Body().Controls.Add(tableLayoutPanel18);

            // Mockup 10: "Hoạt động gần đây" là MỘT MỤC TRƠ trên nền trắng — không viền card, không
            // icon tiêu đề, và tiêu đề canh thẳng hàng với "Tổng quan" ở đầu trang. Bỏ luôn lề ngang
            // của card để chữ không thụt vào 20px; phần thụt của icon/dòng do chính ô lưới lo.
            grpActivity.ShowBorder = false;
            grpActivity.Padding = new Padding(0, 12, 0, 0);
            grpActivity.SetHeader("Hoạt động gần đây", null);
            grpActivity.AddHeaderAction(lnkXemLichSu);
            grpActivity.AddHeaderAction(lblActivitySummary);
            grpActivity.Body().Controls.Add(tlpActivity);

            Theme.StyleGrid(dgvActions);
            Theme.StyleGrid(dgvActivity);
            Theme.StyleLinkLabel(lnkXemTatCa);
            Theme.StyleLinkLabel(lnkXemLichSu);
            Theme.StyleButton(btnQuarantineSelected, Theme.BtnRole.Action);
            Theme.StyleButton(btnQuarantineAll, Theme.BtnRole.Action);
            Theme.StyleButton(btnDeleteSelected, Theme.BtnRole.Danger);
            Theme.StyleButton(btnVirusTotal, Theme.BtnRole.Action);
            // (25/09/2026, lần 9) Dải loading quét — màu sắc lấy từ một nguồn theme duy nhất
            Theme.StyleLoadingStrip(pnlLoadingQuet, spinnerDangQuet, lblLoadingTieuDe, lblLoadingChiTiet,
                btnHuyQuetLoading);
            btnHuyQuetLoading.Image = UiIcons.Stop(12, Color.White);
            btnHuyQuetLoading.TextImageRelation = TextImageRelation.ImageBeforeText;

            // ===== BẢNG "HOẠT ĐỘNG GẦN ĐÂY" theo mockup: không tiêu đề cột, không kẻ ô, dòng cao,
            // icon thật thay cho chấm màu =====
            dgvActivity.ColumnHeadersVisible = false;
            dgvActivity.CellBorderStyle = DataGridViewCellBorderStyle.None;
            dgvActivity.RowTemplate.Height = 52;
            dgvActivity.DefaultCellStyle.Font = Theme.BodyBigFont;
            dgvActivity.DefaultCellStyle.BackColor = Color.White;
            dgvActivity.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dgvActivity.DefaultCellStyle.SelectionBackColor = Color.White;   // mockup: không tô nền dòng đang chọn
            dgvActivity.DefaultCellStyle.SelectionForeColor = Theme.TextDark;
            // StyleGrid đặt AutoSizeColumnsMode=Fill toàn cục; riêng 3 cột này phải chia lại tỉ lệ,
            // nếu không cột "Thời gian" giữ nguyên Width=150 của Designer và bị ép về AutoSizeMode.None.
            foreach (DataGridViewColumn c in dgvActivity.Columns)
            {
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                c.MinimumWidth = 2;   // 2 = sàn hợp lệ của DataGridView; bỏ MinimumWidth=200 của colActDesc
            }
            // Cột icon là hằng số thiết kế (22px + lề 16), KHÔNG phải tỉ lệ cửa sổ: để chế độ Fill
            // thì bề rộng phụ thuộc cách WinForms chia lại phần trăm, icon bị bóp lại thành chấm.
            // Cố định bề rộng, nhường toàn bộ phần co giãn cho 2 cột chữ.
            colActMark.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colActMark.Width = 54;   // 16 lề + 22 icon + 16 lề
            colActDesc.FillWeight = 76F;
            colActTime.FillWeight = 24F;
            colActMark.ImageLayout = DataGridViewImageCellLayout.Normal;
            colActMark.DefaultCellStyle.Padding = new Padding(16, 15, 0, 0);   // icon 22px canh giữa dòng 52px
            colActTime.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colActTime.DefaultCellStyle.Padding = new Padding(0, 0, 16, 0);
            colActDesc.DefaultCellStyle.ForeColor = Theme.TextMid;





            



        }

        private void WireEvents()
        {
            // Hàng 3 thẻ số liệu là 3 Panel trơn (không phải GroupBox) — vạch chia do handler này vẽ,
            // nhờ vậy không phải thêm cột/khung vào pnlThongKe (Tests §3e đòi ColumnCount == 3).
            // Ba thẻ dashboard có viền riêng; không vẽ dải phân cách cũ.
            btnScanNow.Click += BtnScanNow_Click;
            btnQuetNangCao.Click += BtnQuetNangCao_Click;
            btnXemChiTiet.Click += delegate { MoChiTietKetQua(); };
            btnQuetLai.Click += BtnQuetLai_Click;
            lnkXemTatCa.LinkClicked += delegate { MoChiTietKetQua(); };
            lnkXemLichSu.LinkClicked += delegate { MoTabLichSu(); };

            btnQuarantineSelected.Click += BtnQuarantineSelected_Click;
            btnDeleteSelected.Click += BtnDeleteSelected_Click;
            btnQuarantineAll.Click += BtnQuarantineAll_Click;
            btnVirusTotal.Click += BtnVirusTotal_Click;
            // (25/09/2026, lần 9) nút "Hủy quét" của dải loading
            btnHuyQuetLoading.Click += BtnHuyQuetLoading_Click;

            colPickAction.HeaderCell = new Theme.SelectAllHeaderCell(
                () => Theme.PickState(dgvActions, colPickAction.Index));
            dgvActions.CurrentCellDirtyStateChanged += delegate
            {
                if (dgvActions.IsCurrentCellDirty) dgvActions.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvActions.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex < 0) return;
                if (e.ColumnIndex == colPickAction.Index) UpdateThreatUi();
            };
            dgvActions.ColumnHeaderMouseClick += delegate(object s, DataGridViewCellMouseEventArgs e)
            {
                if (e.ColumnIndex != colPickAction.Index) return;
                bool any = dgvActions.Rows.Cast<DataGridViewRow>()
                    .Any(r => IsTicked(r));
                Theme.PickAll(dgvActions, colPickAction.Index, !any);
                UpdateThreatUi();
            };
            dgvActions.CellFormatting += DgvActions_CellFormatting;
            dgvActions.CellContentClick += DgvActions_CellContentClick;
            dgvActions.CellDoubleClick += delegate(object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0) MoChiTietKetQua(e.RowIndex);
            };
            RealTimeProtection.StatusChanged += OnRealTimeStatus;
            Disposed += delegate { RealTimeProtection.StatusChanged -= OnRealTimeStatus; };
        }

        // ================= ĐIỀU HƯỚNG GIỮA 4 MÀN HÌNH (a/b/c/d) =================

        /// <summary>
        /// Đổi màn hình: chỉ 1 trong 4 vùng Visible. Các hàng của tableLayoutPanel12 là Absolute
        /// nên khi ẩn phải hạ chiều cao hàng về 0 (SetRowHeight) để không chừa khoảng trắng; hàng nội dung
        /// chính của màn hình đang mở thì chuyển sang Percent 100 (SetRowFill) để giãn theo cửa sổ.
        /// Trang (c) "Chi tiết kết quả quét" và (d) "Quét nâng cao" là 2 màn hình riêng nên hàng 3 thẻ số liệu
        /// (pnlThongKe) bị ẩn hẳn ở cả hai (lần 5 cho (d), lần 8 cho (c)); quay lại (a)/(b) là hiện lại đủ 104px.
        /// </summary>
        private void HienThi(TongQuanView view, bool refresh = true)
        {
            currentView = view;
            bool trangChinh = view == TongQuanView.AnToan || view == TongQuanView.PhatHienDeDoa;
            bool coDeDoa = view == TongQuanView.PhatHienDeDoa;
            // Hàng 3 thẻ số liệu CHỈ thuộc Tổng quan (a)/(b): vào 2 màn hình riêng (c) "Chi tiết kết quả quét"
            // và (d) "Quét nâng cao" thì ẩn hẳn (lần 5 cho (d), lần 8 cho (c)) để nhường chiều cao cho nội dung.
            bool coThongKe = view == TongQuanView.AnToan || view == TongQuanView.PhatHienDeDoa;
            // RESPONSIVE (lần 7): trang (d) tự xếp lại theo bề rộng nên hạ bề rộng tối thiểu của vùng
            // Tổng quan; (a)/(b)/(c) giữ nguyên ngưỡng 980 như trước.
            CapNhatRongToiThieu(view == TongQuanView.QuetNangCao ? RongToiThieuTrangNangCao : RongToiThieu);
            // Chiều cao hàng của tableLayoutPanel12 (6 hàng, đúng số của Designer):
            // 0=hero (a)/(b) 320 (khiên lớn 140 + tiêu đề + mô tả + hàng 2 nút quét, canh giữa) · 1=3 thẻ số liệu 104
            // 2=bảng đe dọa 400 (CHỈ ở trạng thái (b); (a) hạ hàng về 0 + ẩn)
            // 3="Hoạt động gần đây" 400 ở (a) / 0 ở (b) — (b) nhường chỗ cho bảng đe dọa
            // 4=(c) 620 · 5=(d) 640
            SetRowHeight(pnlHeroHost, trangChinh ? HeroHeight : 0);
            // Hàng 3 thẻ số liệu (Mối đe dọa / Tệp đã quét / Lần quét gần nhất): CHỈ ở (a)/(b); mở màn hình
            // riêng (c) "Chi tiết kết quả quét" hoặc (d) "Quét nâng cao" thì hạ hàng về 0px + ẩn hẳn.
            SetRowHeight(pnlThongKe, coThongKe ? ThongKeHeight : 0);
            SetRowHeight(grpAction, coDeDoa ? 400 : 0);
            SetRowHeight(grpActivity, view == TongQuanView.AnToan ? 400 : 0);
            SetRowHeight(pnlChiTietKetQua, view == TongQuanView.ChiTiet ? 620 : 0);
            SetRowHeight(pnlQuetNangCao, view == TongQuanView.QuetNangCao ? 640 : 0);

            // RESPONSIVE: hàng nội dung chính của màn hình đang mở giãn hết phần chiều cao còn lại
            // (cửa sổ lớn -> nội dung cao lên, không chừa khoảng trắng; cửa sổ nhỏ -> cuộn qua tableLayoutPanel11)
            if (view == TongQuanView.AnToan) SetRowFill(grpActivity);
            else if (view == TongQuanView.PhatHienDeDoa) SetRowFill(grpAction);
            else if (view == TongQuanView.ChiTiet) SetRowFill(pnlChiTietKetQua);
            else SetRowFill(pnlQuetNangCao);

            // Đầu trang "Tổng quan" CHỈ thuộc 2 màn Tổng quan: (c) và (d) là màn hình riêng, mỗi màn
            // có đầu trang của chính nó ("← Quay lại" + tiêu đề lớn), nên đầu trang này phải ẩn hẳn —
            // nếu không sẽ thấy tiêu đề "Tổng quan" nằm đè lên trên đầu trang của (c)/(d).
            pnlPageHead.Visible = trangChinh;
            pnlAnToan.Visible = view == TongQuanView.AnToan;
            pnlPhatHienDeDoa.Visible = view == TongQuanView.PhatHienDeDoa;
            // Hàng 3 thẻ số liệu cũng biến mất ở trang (d) "Quét nâng cao" (màn hình riêng, xem SetRowHeight ở trên)
            pnlThongKe.Visible = coThongKe;
            // Bảng "Mối đe dọa được phát hiện" KHÔNG nằm ở trang Tổng quan (a) — chỉ hiện ở (b)
            grpAction.Visible = coDeDoa;
            grpActivity.Visible = view == TongQuanView.AnToan;
            pnlChiTietKetQua.Visible = view == TongQuanView.ChiTiet;
            pnlQuetNangCao.Visible = view == TongQuanView.QuetNangCao;
            if (view == TongQuanView.QuetNangCao) OnMoTrangQuetNangCao();

            UpdateDashboardExtent();
            if (!refresh) return;
            ApplyLastScanFromHistory();
            LoadActivity();
        }

        /// <summary>
        /// RESPONSIVE (25/09/2026, lần 7): đổi bề rộng tối thiểu của vùng Tổng quan (tableLayoutPanel11)
        /// theo màn hình đang mở. Trang (d) "Quét nâng cao" tự xếp lại (2 cột → 1 hàng ngang → lưới 2x2)
        /// nên chỉ cần `RongToiThieuTrangNangCao`: cửa sổ hẹp vẫn thấy đủ 4 thẻ chế độ + nút "Bắt đầu quét"
        /// thay vì phải cuộn ngang; (a)/(b)/(c) vẫn giữ ngưỡng 980 như trước.
        /// </summary>
        private void CapNhatRongToiThieu(int rong)
        {
            if (tableLayoutPanel11.MinimumSize.Width == rong) return;
            tableLayoutPanel11.MinimumSize = new Size(rong, CaoToiThieu);
        }

        /// <summary>
        /// Hàng Absolute: đặt đúng chiều cao thiết kế khi hiện / 0 khi ẩn (tránh chừa khoảng trắng).
        /// Reset luôn SizeType về Absolute vì hàng có thể đang là Percent 100 do SetRowFill để lại.
        /// </summary>
        private void SetRowHeight(System.Windows.Forms.Control child, float height)
        {
            RowStyle style = RowStyleOf(child);
            if (style == null) return;
            if (style.SizeType != SizeType.Absolute) style.SizeType = SizeType.Absolute;
            if (Math.Abs(style.Height - height) > 0.5f) style.Height = height;
        }

        /// <summary>
        /// RESPONSIVE (25/09/2026, lần 4): cho hàng của `child` giãn hết phần chiều cao còn lại của
        /// tableLayoutPanel12 — nội dung chính của màn hình đang mở luôn lấp đầy cửa sổ, không bị cắt.
        /// </summary>
        private void SetRowFill(System.Windows.Forms.Control child)
        {
            RowStyle style = RowStyleOf(child);
            if (style == null) return;
            if (style.SizeType != SizeType.Percent) style.SizeType = SizeType.Percent;
            if (Math.Abs(style.Height - 100F) > 0.5f) style.Height = 100F;
        }

        /// <summary>RowStyle của hàng chứa `child` trong tableLayoutPanel12 (null nếu không có).</summary>
        private RowStyle RowStyleOf(System.Windows.Forms.Control child)
        {
            int row = tableLayoutPanel12.GetRow(child);
            if (row < 0 || row >= tableLayoutPanel12.RowStyles.Count) return null;
            return tableLayoutPanel12.RowStyles[row];
        }

        /// <summary>
        /// Trạng thái (a)/(b) suy trực tiếp từ số đe dọa còn lại: còn tệp -> (b), hết -> (a).
        /// Không áp dụng khi đang mở trang (c)/(d) để không giật màn hình của người dùng.
        /// </summary>
        private void CapNhatTrangThaiHero()
        {
            bool coDeDoa = dgvActions.Rows.Count > 0;
            if (currentView == TongQuanView.AnToan || currentView == TongQuanView.PhatHienDeDoa)
            {
                TongQuanView mongMuon = coDeDoa ? TongQuanView.PhatHienDeDoa : TongQuanView.AnToan;
                if (currentView != mongMuon) HienThi(mongMuon, false);
                pnlAnToan.Visible = !coDeDoa;
                pnlPhatHienDeDoa.Visible = coDeDoa;
            }
            lblPhatHienTitle.Text = string.Format("Phát hiện {0} mối đe dọa", dgvActions.Rows.Count);
            lblPhatHienSub.Text = coDeDoa
                ? "Quá trình quét đã hoàn tất. Xem chi tiết để xử lý từng tệp hoặc quét lại."
                : "Quá trình quét đã hoàn tất, không còn tệp nào cần xử lý.";
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible) return;
            // Mở lại tab -> làm mới số liệu (quét/cách ly có thể đã xảy ra ở tab khác)
            ApplyLastScanFromHistory();
            LoadActivity();
        }

        // ================= CHẠY PHIÊN QUÉT (thẻ quét nhanh + trang Quét nâng cao dùng chung) =================

        /// <summary>Nút "Quét ngay" kiêm nút "Hủy quét" khi phiên đang chạy.</summary>
        private async void BtnScanNow_Click(object sender, EventArgs e)
        {
            if (isScanning)
            {
                HuyPhienQuet();
                return;
            }
            ScanJob job = BuildJobFromQuickCard();
            if (job == null) return;
            await ChayPhienQuet(job);
        }

        /// <summary>
        /// Dựng ScanJob cho nút "Quét ngay". Thẻ "Tuỳ chọn quét nhanh" đã bỏ (25/09/2026) nên mặc định là
        /// Quét nhanh; nếu có đường dẫn tùy chọn (SetCustomPath — luồng quét 1 vị trí không qua thẻ nhanh)
        /// thì quét đúng vị trí đó. Quét toàn bộ / nhiều vị trí nằm ở trang "Quét nâng cao".
        /// </summary>
        private ScanJob BuildJobFromQuickCard()
        {
            ScanType type = GetSelectedScanType();
            string path = type == ScanType.Custom ? customScanPath : null;
            return new ScanJob
            {
                Type = type,
                CustomPath = path,
                DisplayName = DescribeScan(type),
                ScopeText = ScopeOf(type, path)
            };
        }

        /// <summary>
        /// Nút "Quét lại" ở trạng thái (b): chạy lại đúng phạm vi của phiên vừa rồi. Khi phiên đang chạy thì
        /// nút này đã đổi thành "Hủy quét" (SetScanning) nên bấm sẽ hủy phiên thay vì bị bỏ qua.
        /// </summary>
        private async void BtnQuetLai_Click(object sender, EventArgs e)
        {
            if (isScanning)
            {
                HuyPhienQuet();
                return;
            }
            ScanJob job = lastJob ?? BuildJobFromQuickCard();
            if (job == null) return;
            await ChayPhienQuet(job);
        }

        /// <summary>
        /// Chạy phiên quét: nhiều vị trí (trang Quét nâng cao) thì quét tuần tự từng vị trí bằng
        /// ScanEngine rồi gộp kết quả; xong mới rẽ nhánh (a)/(b) và ghi lịch sử.
        /// Trả về true nếu phiên chạy trọn vẹn (false khi bị hủy/lỗi).
        /// </summary>
        private async Task<bool> ChayPhienQuet(ScanJob job)
        {
            lastJob = job;
            SetScanning(true);
            ProtectionNotificationService.Publish("Đã bắt đầu " + job.DisplayName,
                "Ứng dụng đang kiểm tra " + job.ScopeText + ".", ProtectionNoticeLevel.Info,
                "scan-start", 3, true);
            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            bool completed = false;
            var progressGate = new object();
            bool progressQueued = false;
            int latestCount = 0;
            string latestPath = null;
            var progressClock = System.Diagnostics.Stopwatch.StartNew();
            long lastProgressTime = -150;
            var telemetry = new ScanTelemetry();
            activeScanTelemetry = telemetry;
            activeScanReading = null;
            var etaRefresh = new System.Windows.Forms.Timer { Interval = 350 };
            etaRefresh.Tick += (sender, args) =>
            {
                if (!isScanning || IsDisposed || activeScanTelemetry != telemetry) return;
                var reading = telemetry.GetReading();
                activeScanReading = reading;
                // Cộng ngay từng entry ZIP vừa kiểm tra, kể cả khi file ZIP bên ngoài chưa quét xong.
                lblScannedCount.Text = reading.CheckedObjects.ToString("N0");
                string etaText = ScanTelemetry.DescribeRemaining(reading);
                string location = reading.CurrentPath;
                string status = reading.ArchiveEntriesCompleted > 0
                    ? string.Format("Đã kiểm tra {0:N0} tệp ({1:N0} trong tệp nén) • {2} • {3:0.0} MiB/s",
                        reading.CheckedObjects, reading.ArchiveEntriesCompleted, etaText, reading.MiBPerSecond)
                    : string.Format("{0:N0}/{1:N0} tệp • {2} • {3:0.0} MiB/s",
                        reading.FilesCompleted, reading.FilesDiscovered, etaText, reading.MiBPerSecond);
                lblScanProgress.Text = "Đang quét: " + status +
                    " • Phát hiện " + reading.ThreatsFound.ToString("N0") + " mối đe dọa";
                CapNhatDongChiTietLoading(status + (string.IsNullOrWhiteSpace(location) ? "" : " • " + ScanDisplayPath.Leaf(location)));
            };
            etaRefresh.Start();
            Action<int, string> progress = (count, current) =>
            {
                lock (progressGate)
                {
                    latestCount = Math.Max(latestCount, count);
                    if (current != null) latestPath = current;
                    if (progressQueued || progressClock.ElapsedMilliseconds - lastProgressTime < 150) return;
                    progressQueued = true;
                }
                try
                {
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        int shownCount; string shownPath;
                        lock (progressGate)
                        {
                            shownCount = latestCount; shownPath = latestPath;
                            lastProgressTime = progressClock.ElapsedMilliseconds;
                            progressQueued = false;
                        }
                        if (!isScanning || token.IsCancellationRequested) return;
                        // Callback cũ chỉ đếm file vật lý; không cho nó ghi đè số entry ZIP đang chạy.
                        lblScannedCount.Text = telemetry.GetReading().CheckedObjects.ToString("N0");
                        // ETA/tốc độ/đường dẫn được cập nhật bởi timer 350 ms;
                        // callback theo lô không được ghi đè dòng trạng thái ETA.
                        if (shownPath != null && activeScanReading == null)
                            CapNhatDongChiTietLoading("Đang quét: " + ScanDisplayPath.Leaf(shownPath));
                    }));
                }
                catch (ObjectDisposedException) { lock (progressGate) progressQueued = false; }
                catch (InvalidOperationException) { lock (progressGate) progressQueued = false; }
            };

            try
            {
                List<string> targets = job.Targets != null && job.Targets.Count > 0
                    ? new List<string>(job.Targets)
                    : null;

                var worker = Task.Run(() =>
                {
                    // Tải SQL đúng một lần cho toàn bộ phiên, kể cả khi chọn nhiều thư mục.
                    var signatures = HybridSignatureScanner.GetSnapshot(true);
                    return ScanEngine.Scan(job.Type, job.CustomPath, token, progress,
                        telemetry, signatures, targets);
                }, token);
                ScanResult result = await WaitForScan(worker, token);

                // A cancel click can arrive after the worker finishes but before
                // its continuation reaches the UI thread. Honor that request too.
                token.ThrowIfCancellationRequested();
                completed = true;
                // Kết thúc chế độ quét trước khi bật popup hoàn tất để màn hình
                // phía sau không còn hiện 99% hay ĐANG QUÉT.
                etaRefresh.Stop();
                activeScanTelemetry = null;
                activeScanReading = null;
                SetScanning(false);
                ApDungKetQuaQuet(result, job);
            }
            catch (OperationCanceledException)
            {
                lblScanProgress.ForeColor = Theme.Amber;
                lblScanProgress.Text = "Đã hủy phiên quét.";
                ProtectionNotificationService.Publish("Đã hủy quét", "Phiên quét được dừng theo yêu cầu.",
                    ProtectionNoticeLevel.Info, "scan-cancel-result", 3, true);
                CapNhatDongChiTietLoading("Đã hủy phiên quét.");
                AppDialog.Show("Đã hủy phiên quét.", job.DisplayName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblScanProgress.ForeColor = Theme.Red;
                lblScanProgress.Text = "Quét dừng vì lỗi.";
                ProtectionNotificationService.Publish("Quét gặp lỗi", ex.Message,
                    ProtectionNoticeLevel.Error, "scan-error", 5, true);
                CapNhatDongChiTietLoading("Quét dừng vì lỗi.");
                AppDialog.Show("Quét dừng vì lỗi: " + ex.Message, job.DisplayName,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                etaRefresh.Stop();
                etaRefresh.Dispose();
                activeScanTelemetry = null;
                activeScanReading = null;
                if (isScanning) SetScanning(false);
                if (cts != null) { cts.Dispose(); cts = null; }
            }
            return completed;
        }

        private static async Task<ScanResult> WaitForScan(Task<ScanResult> worker, CancellationToken token)
        {
            // Cancellation must release the UI even while the thread pool has not
            // started this worker. The worker keeps its cancelled token and cannot
            // apply results to a later scan session.
            var cancelled = new TaskCompletionSource<bool>();
            using (token.Register(() => cancelled.TrySetResult(true)))
            {
                if (await Task.WhenAny(worker, cancelled.Task) != worker)
                {
                    _ = worker.ContinueWith(t => { var observed = t.Exception; },
                        CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                    // Leave the Cancel() callback before disposing its registration.
                    await Task.Yield();
                    token.ThrowIfCancellationRequested();
                }
                return await worker;
            }
        }

        /// <summary>Hiển thị kết quả phiên quét: bảng đe dọa, 5 tab chi tiết, thống kê, lịch sử, thông báo.</summary>
        private void ApDungKetQuaQuet(ScanResult result, ScanJob job)
        {
            lastResult = result;
            lastScanTime = DateTime.Now;
            lastScannedFiles = result.CheckedObjects;
            lblScannedCount.Text = result.CheckedObjects.ToString("N0");
            lblScanProgress.Text = string.Format("Hoàn tất: đã kiểm tra {0:N0} tệp ({1:N0} tệp vật lý, {2:N0} tệp trong tệp nén; {3:0.#} giây).",
                result.CheckedObjects, Math.Max(0, result.FilesScanned - result.ReadErrorCount),
                result.ArchiveEntriesScanned, result.Duration.TotalSeconds);
            bool signatureWarning = !string.IsNullOrWhiteSpace(result.SignatureLoadWarning);
            bool incompleteCoverage = result.ReadErrorCount > 0 || result.ArchiveEntriesUnchecked > 0 ||
                result.DirectoriesSkipped > 0 || result.EnumerationErrors > 0 || result.MissingTargets > 0;
            var scanSignatures = HybridSignatureScanner.GetSnapshot(false);
            bool wholeFileHashAvailable = scanSignatures.WholeFileSignatureCount > 0;
            lblScanProgress.ForeColor = result.HasThreats || signatureWarning ? Theme.Amber : Theme.Green;
            if (!result.HasThreats && result.SignatureIsDegraded)
                lblScanProgress.Text = result.SignatureSource == "OfflineSnapshot"
                    ? "Đã quét bằng chữ ký ngoại tuyến; SQL chưa kết nối. Bảo vệ hạn chế."
                    : "Không có chữ ký tin cậy; quét chưa thể kết luận an toàn.";
            else if (!result.HasThreats && signatureWarning)
                lblScanProgress.Text = wholeFileHashAvailable
                    ? "Đã kiểm tra hash và heuristic; một số lớp phát hiện chưa sẵn sàng."
                    : "Quét hoàn tất nhưng chữ ký chưa sẵn sàng — chưa thể kết luận tệp an toàn.";

            lblLastScanDate.Text = lastScanTime.ToString("dd/MM/yyyy");
            lblLastScanType.Text = lastScanTime.ToString("HH:mm") + " · " + job.DisplayName;

            LoadDetectedThreats(result);   // đổ vào dgvActions + threatRows (có lọc nếu quét từ (d))
            // Quét xong -> luôn rẽ nhánh về trạng thái (a) hoặc (b) (SPEC §6.1/§6.2)
            HienThi(dgvActions.Rows.Count > 0 ? TongQuanView.PhatHienDeDoa : TongQuanView.AnToan, false);
            lblProtectionStatus.Text = !result.HasThreats && incompleteCoverage
                ? "Một số tệp chưa được kiểm tra" : !result.HasThreats && result.SignatureIsDegraded
                ? "Bảo vệ hạn chế" : !result.HasThreats && signatureWarning
                ? "Độ bao phủ chữ ký chưa đầy đủ" : "Máy tính của bạn được bảo vệ";
            lblAnToanSub.Text = !result.HasThreats && incompleteCoverage
                ? "Có tệp không thể đọc hoặc nội dung tệp nén chưa được kiểm tra."
                : !result.HasThreats && result.SignatureIsDegraded
                ? result.SignatureSource == "OfflineSnapshot"
                    ? "Đang dùng chữ ký ngoại tuyến; hãy kiểm tra kết nối SQL Server."
                    : "Chưa tải được chữ ký mã độc; không thể khẳng định tệp an toàn."
                : !result.HasThreats && signatureWarning
                ? "Heuristic vẫn hoạt động, nhưng cần kiểm tra chữ ký SQL." : "Không phát hiện mối đe dọa.";
            // Nhật ký lưu cùng chuẩn đếm với tiến trình/popup, không thay đổi bảng SQL.
            long scanId = ScanHistoryStore.Add(job.DisplayName, job.ScopeText,
                result.CheckedObjects, result.Threats.Count, result.Duration.TotalSeconds);
            PersistScanThreats(scanId, result);
            ScanIssueRepository.Save(scanId, result);
            ScanSignatureMetadataRepository.Save(scanId, result);

            if (job.AutoQuarantine) TuCachLyTheoPhien();
            AutoQueryHeuristicRows(result);      // hàng "Bảo vệ web" nếu đang bật
            LoadActivity();
            ApplyLastScanFromHistory();
            // Lịch sử hiện lưu số tệp vật lý; giữ tổng hiển thị của phiên vừa quét
            // bao gồm những entry ZIP đã thực sự kiểm tra để số không bị giảm đột ngột.
            lblScannedCount.Text = result.CheckedObjects.ToString("N0");

            // Popup chỉ nêu số liệu cốt lõi. Tên/đường dẫn của từng tệp được
            // hiển thị đầy đủ ở "Mối đe dọa cần xử lý" và "Chi tiết tệp".
            ProtectionNotificationService.Publish("Đã hoàn tất " + job.DisplayName,
                "Đã kiểm tra " + result.CheckedObjects.ToString("N0") + " mục; phát hiện " +
                result.Threats.Count.ToString("N0") + " mối đe dọa" +
                (incompleteCoverage || signatureWarning || result.SignatureIsDegraded ?
                    ". Một số nội dung/chữ ký chưa kiểm tra đầy đủ." : "."),
                result.HasThreats || incompleteCoverage || signatureWarning || result.SignatureIsDegraded ?
                    ProtectionNoticeLevel.Warning : ProtectionNoticeLevel.Success,
                "scan-result", 3, true);
            int heuristicCount = result.Threats.Count(t => ScanEngine.IsHeuristicOnly(t));
            int signatureCount = result.Threats.Count - heuristicCount;
            string scanMessage = string.Format(
                "Đã kiểm tra: {0:N0} tệp\r\n" +
                "Mã độc khớp chữ ký: {1:N0}\r\n" +
                "Tệp nghi vấn (heuristic): {2:N0}\r\n" +
                "Tệp không đọc được: {3:N0}",
                result.CheckedObjects, signatureCount, heuristicCount, result.ReadErrorCount);

            if (result.ArchiveEntriesScanned > 0 || result.ArchiveEntriesUnchecked > 0)
                scanMessage += string.Format("\r\nTrong đó: {0:N0} tệp vật lý; {1:N0} tệp con trong tệp nén.\r\nTệp con trong tệp nén chưa kiểm tra: {2:N0}.",
                    Math.Max(0, result.FilesScanned - result.ReadErrorCount),
                    result.ArchiveEntriesScanned, result.ArchiveEntriesUnchecked);

            if (result.DirectoriesSkipped > 0 || result.EnumerationErrors > 0 || result.MissingTargets > 0)
            {
                scanMessage += string.Format(
                    "\r\nThư mục/phạm vi bỏ qua hoặc gặp lỗi: {0:N0}",
                    result.DirectoriesSkipped + result.EnumerationErrors + result.MissingTargets);
            }

            if (result.HasThreats)
                scanMessage += "\r\n\r\nXem tên và đường dẫn từng tệp trong Mối đe dọa cần xử lý.";
            else if (result.ReadErrorCount == 0 && result.DirectoriesSkipped == 0 &&
                     result.EnumerationErrors == 0 && result.MissingTargets == 0 &&
                     result.ArchiveEntriesUnchecked == 0 && !signatureWarning)
                scanMessage += "\r\n\r\nKhông phát hiện mối đe dọa theo các phương pháp hiện có.";
            else
                scanMessage += "\r\n\r\nKết quả chưa đầy đủ, không thể xác nhận tất cả tệp an toàn.";

            // Cập nhật view lần cuối TRƯỚC hộp thoại modal. Nếu không, màn hình
            // phía sau vẫn ghi 99% / ĐANG QUÉT do Timer bị hộp thoại chặn.
            FrmMain main = FindForm() as FrmMain;
            if (main != null && main.exactView != null && !main.exactView.IsDisposed)
                main.exactView.RefreshModel(true);
            AppDialog.Show(scanMessage, job.DisplayName + " hoàn tất", MessageBoxButtons.OK,
                result.HasThreats || signatureWarning || incompleteCoverage
                    ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private static void PersistScanThreats(long scanId, ScanResult result)
        {
            if (scanId <= 0 || result == null || result.Threats.Count == 0) return;
            var repository = new ThreatDetectionRepository();
            foreach (ThreatFound threat in result.Threats)
            {
                try
                {
                    string path = threat.FilePath ?? "";
                    var info = File.Exists(path) ? new FileInfo(path) : null;
                    repository.Add(scanId, threat.SignatureID, info == null ? ScanDisplayPath.Leaf(path) : info.Name, path,
                        string.IsNullOrWhiteSpace(threat.Kind) ? threat.Reason : threat.Kind + ": " + threat.Reason,
                        info == null ? (long?)null : info.Length, null, null, ScanEngine.ComputeFileSha256(path));
                }
                catch { /* lịch sử chi tiết là best-effort, không làm hỏng kết quả quét */ }
            }
        }

        /// <summary>"Tự động cách ly khi phát hiện mối đe dọa" (tuỳ chọn của trang Quét nâng cao).</summary>
        private void TuCachLyTheoPhien()
        {
            bool coXuLy = false;
            foreach (DataGridViewRow row in dgvActions.Rows.Cast<DataGridViewRow>().ToList())
            {
                string path = row.Cells[colActionFile.Index].Value as string;
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                string reason = row.Cells[colActionThreat.Index].Value as string;
                // Không tự cách ly khi mới chỉ có điểm heuristic nghi vấn.
                if (!string.IsNullOrWhiteSpace(reason) &&
                    reason.StartsWith("Heuristic", StringComparison.OrdinalIgnoreCase)) continue;
                if (!ScanEngine.Quarantine(path, reason)) continue;
                ThreatRow tr;
                if (rowByPath.TryGetValue(path, out tr)) tr.Quarantined = true;
                dgvActions.Rows.Remove(row);
                coXuLy = true;
            }
            if (coXuLy) UpdateThreatUi();
        }

        /// <summary>
        /// Bật/tắt trạng thái "đang quét" — đổi chữ + vai trò nút, khoá lựa chọn phạm vi và (lần 9) bật/tắt
        /// **dải loading quét** ở đầu trang. Mọi nút quét ("Quét ngay" / "Quét lại" / "Bắt đầu quét" ở (d))
        /// đều chạy qua `ChayPhienQuet` nên chỉ cần đúng 1 chỗ này là dải loading hiện/ẩn đúng lúc.
        /// </summary>
        private void SetScanning(bool scanning)
        {
            isScanning = scanning;
            btnScanNow.Text = scanning ? "Hủy quét" : "Quét ngay";
            btnScanNow.Icon = scanning ? UiIcons.Stop(13, Color.White) : UiIcons.Play(13, Color.White);
            UiKit.Apply(btnScanNow, scanning ? Theme.BtnRole.Cancel : Theme.BtnRole.Primary);
            // (25/09/2026, lần 4) 2 nút "Quét ngay"/"Quét nâng cao" nay nằm TRONG khối (a) pnlAnToan, nên khi
            // người dùng đang ở trạng thái (b) thì nút "Quét lại" đảm nhiệm luôn vai trò "Hủy quét" —
            // không mất đường hủy phiên quét giữa chừng.
            btnQuetLai.Text = scanning ? "Hủy quét" : "Quét lại";
            btnQuetLai.Icon = scanning ? UiIcons.Stop(13, Color.White) : UiIcons.Refresh(13, Theme.TextMid);
            UiKit.Apply(btnQuetLai, scanning ? Theme.BtnRole.Cancel : Theme.BtnRole.Neutral);
            // Dòng trạng thái quét nay nằm trong khối (a) pnlAnToan, ngay cạnh 2 nút quét. Designer để nó
            // ẩn ("Sẵn sàng quét" làm bẩn hero canh giữa của mockup); có phiên quét là hiện, và ở lại
            // cùng kết quả ("Hoàn tất…"/"Đã hủy…") sau khi phiên kết thúc.
            lblScanProgress.Visible = true;
            if (scanning) lblScanProgress.ForeColor = Theme.Blue;
            if (scanning)
                lblScanProgress.Text = "Đang quét...";
            // (25/09/2026, lần 9) Dải loading quét: hiện NGAY khi phiên bắt đầu, ẩn khi phiên kết thúc
            // (xong / hủy / lỗi — `SetScanning(false)` nằm trong `finally` của ChayPhienQuet).
            HienThiDaiLoading(scanning);
            OnSetScanningAdvanced(scanning);
            UpdateDashboardExtent();
        }

        // ================= DẢI LOADING QUÉT (25/09/2026, lần 9) =================

        /// <summary>
        /// Hiện/ẩn **dải loading quét** — hàng 0 của `tableLayoutPanel11` (46px khi quét, 0px khi rảnh) — cùng
        /// cơ chế "Absolute + Visible" như `pnlThongKe`/`pnlHeroHost`: control không bị gỡ/dựng lại, chỉ đổi
        /// chiều cao hàng. Vòng xoay `spinnerDangQuet` tự chạy/dừng theo `Visible` (xem `Control\LoadingSpinner.cs`).
        /// </summary>
        private void HienThiDaiLoading(bool hien)
        {
            if (pnlLoadingQuet == null) return;
            SetLoadingRowHeight(hien ? LoadingHeight : 0F);
            pnlLoadingQuet.Visible = hien;
            if (hien)
            {
                lblLoadingTieuDe.Text = "Đang quét…";
                // Lưu ý: engine chỉ báo tiến trình theo TỆP khi quét 1 đường dẫn đơn (ScanEngine.Scan gọi
                // progress(1, path)); quét nhiều vị trí / thư mục lớn thì chưa có tiến trình từng tệp, nên câu
                // mặc định ở đây phải đúng cho CẢ phiên — và nó nhắc luôn đường hủy ngay cạnh.
                CapNhatDongChiTietLoading("Vui lòng chờ trong giây lát — có thể bấm \"Hủy quét\" để dừng.");
                spinnerDangQuet.BatDau();
            }
            else
            {
                spinnerDangQuet.Dung();
            }
        }

        /// <summary>Chiều cao hàng 0 của `tableLayoutPanel11` — hàng chứa dải loading quét (lần 9).</summary>
        private void SetLoadingRowHeight(float height)
        {
            if (tableLayoutPanel11 == null || tableLayoutPanel11.RowStyles.Count == 0) return;
            RowStyle style = tableLayoutPanel11.RowStyles[0];
            if (style.SizeType != SizeType.Absolute) style.SizeType = SizeType.Absolute;
            if (Math.Abs(style.Height - height) > 0.5f) style.Height = height;
        }

        /// <summary>Dòng chi tiết của dải loading quét — bám theo `lblScanProgress` trong lúc quét.</summary>
        private void CapNhatDongChiTietLoading(string text)
        {
            if (lblLoadingChiTiet != null) lblLoadingChiTiet.Text = text;
        }

        /// <summary>Nút "Hủy quét" của dải loading → hủy phiên đang chạy.</summary>
        private void BtnHuyQuetLoading_Click(object sender, EventArgs e)
        {
            HuyPhienQuet();
        }

        /// <summary>
        /// Hủy phiên quét đang chạy — một nguồn duy nhất cho cả 3 đường hủy: "Quét ngay"/"Quét lại" (đã đổi
        /// vai trò thành "Hủy quét" khi phiên chạy) và nút "Hủy quét" của dải loading. Nhờ dải loading, người
        /// dùng hủy được phiên từ **mọi** màn hình — kể cả (d), nơi nút "Bắt đầu quét" bị khoá khi đang quét.
        /// </summary>
        private void HuyPhienQuet()
        {
            if (cts != null) cts.Cancel();
        }

        // ================= BẢNG "MỐI ĐE DỌA ĐƯỢC PHÁT HIỆN" + HÀNH ĐỘNG =================

        /// <summary>Quy tắc gán mức độ (SPEC §6.3): chữ ký/hash/EICAR -> Cao; Heuristic 80+ -> Cao, 65-79 -> Trung bình, 60-64 -> Thấp.</summary>
        internal static Theme.ThreatLevel LevelOf(ThreatFound threat)
        {
            if (threat == null) return Theme.ThreatLevel.Low;
            if (!string.Equals(threat.Kind, "Heuristic", StringComparison.OrdinalIgnoreCase))
                return Theme.ThreatLevel.High;   // Chữ ký / EICAR / VirusTotal
            int score = 60;
            string reason = threat.Reason ?? string.Empty;
            int idx = reason.IndexOf("Nghi vấn", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                int slash = reason.IndexOf('/', idx + 8);
                int parsed;
                if (slash > idx && int.TryParse(reason.Substring(idx + 8, slash - idx - 8).Trim(), out parsed))
                    score = parsed;
            }
            if (score >= 80) return Theme.ThreatLevel.High;
            if (score >= 65) return Theme.ThreatLevel.Medium;
            return Theme.ThreatLevel.Low;
        }

        /// <summary>Đổ danh sách đe dọa của phiên quét vào bảng (b) và dữ liệu cho trang (c).</summary>
        private void LoadDetectedThreats(ScanResult result)
        {
            dgvActions.Rows.Clear();
            threatRows.Clear();
            rowByPath.Clear();
            if (result == null) { UpdateThreatUi(); RefreshDetailGrid(); return; }

            foreach (ThreatFound threat in result.Threats)
            {
                // Engine đã xác nhận chữ ký: tuyệt đối không giấu kết quả phát hiện
                // vì các bộ lọc hiển thị ở trang Quét nâng cao. Bản trước có thể
                // quét ra ThreatFound nhưng làm biến mất hàng trong dgvActions.
                Theme.ThreatLevel level = LevelOf(threat);
                string text = string.IsNullOrEmpty(threat.Kind)
                    ? threat.Reason
                    : threat.Kind + ": " + threat.Reason;
                dgvActions.Rows.Add(false, threat.FilePath, text, Theme.LevelText(level), "Xem chi tiết");

                var row = new ThreatRow { Path = threat.FilePath, ThreatText = text, Level = level };
                try
                {
                    if (File.Exists(threat.FilePath))
                    {
                        var info = new FileInfo(threat.FilePath);
                        row.SizeBytes = info.Length;
                        row.CreatedAt = info.CreationTime;
                        row.ModifiedAt = info.LastWriteTime;
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                threatRows.Add(row);
                rowByPath[threat.FilePath] = row;
            }
            UpdateThreatUi();
            RefreshDetailGrid();
        }

        /// <summary>Bộ lọc phía UI của trang Quét nâng cao (engine không có tham số này).</summary>
        private bool ThoaBoLocCuaTrangQuetNangCao(ThreatFound threat)
        {
            if (lastJob == null || !lastJob.FromAdvanced) return true;
            if (string.IsNullOrEmpty(threat.FilePath)) return true;
            if (lastJob.SkipLargerThanBytes > 0)
            {
                try
                {
                    if (File.Exists(threat.FilePath) && new FileInfo(threat.FilePath).Length > lastJob.SkipLargerThanBytes)
                        return false;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            if (lastJob.ExtensionFilter == null) return true;
            string ext = ScanDisplayPath.Extension(threat.FilePath);
            return !string.IsNullOrEmpty(ext) && lastJob.ExtensionFilter.Contains(ext.ToLowerInvariant());
        }

        /// <summary>Các dòng đang được TÍCH ở cột "Chọn" (cho 2 nút "…đã chọn").</summary>
        private List<DataGridViewRow> TickedRows()
        {
            return dgvActions.Rows.Cast<DataGridViewRow>()
                .Where(r => IsTicked(r))
                .ToList();
        }

        /// <summary>Ô "Chọn" của dòng <paramref name="row"/> có đang được tích hay không (Value là bool).
        /// (25/09/2026) Viết dạng "is bool" + ép kiểu thay vì pattern matching C# 7 ("is bool b") vì
        /// WinForms Designer dùng parser CodeDOM chỉ hiểu tới C# 5/6 — gặp cú pháp C# 7 là không mở được
        /// Designer của trang này ("The base class 'System.Void' cannot be designed").</summary>
        private bool IsTicked(DataGridViewRow row)
        {
            object value = row.Cells[colPickAction.Index].Value;
            return value is bool && (bool)value;
        }

        /// <summary>Đồng bộ số liệu + trạng thái nút sau mỗi thay đổi của bảng đe dọa.</summary>
        private void UpdateThreatUi()
        {
            bool hasRows = dgvActions.Rows.Count > 0;
            bool hasPick = false;
            foreach (DataGridViewRow r in dgvActions.Rows)
                if (IsTicked(r)) { hasPick = true; break; }

            lblThreatCount.Text = dgvActions.Rows.Count.ToString();
            lblThreatCount.ForeColor = hasRows ? Theme.Red : Theme.Green;
            lblThreatText.Text = hasRows ? "Cần xử lý" : "Không phát hiện mối đe dọa";
            lblThreatText.ForeColor = hasRows ? Theme.Red : Theme.Green;
            lblThreatSummary.Text = hasRows
                ? string.Format("{0} mối đe dọa — tích chọn để cách ly/xóa, hoặc mở chi tiết từng tệp.", dgvActions.Rows.Count)
                : "Chưa phát hiện mối đe dọa nào trong phiên này.";
            lnkXemTatCa.Visible = hasRows;

            btnQuarantineSelected.Enabled = hasRows && hasPick;
            btnDeleteSelected.Enabled = hasRows && hasPick;
            btnQuarantineAll.Enabled = hasRows;
            btnVirusTotal.Enabled = hasRows;
            Theme.StyleButton(btnQuarantineSelected, Theme.BtnRole.Action);
            Theme.StyleButton(btnQuarantineAll, Theme.BtnRole.Action);
            Theme.StyleButton(btnDeleteSelected, Theme.BtnRole.Danger);
            Theme.StyleButton(btnVirusTotal, Theme.BtnRole.Action);
            Theme.InvalidatePickHeader(dgvActions);
            CapNhatTrangThaiHero();
        }

        private void DgvActions_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colActionLevel.Index) return;
            string path = dgvActions.Rows[e.RowIndex].Cells[colActionFile.Index].Value as string;
            ThreatRow row;
            if (path != null && rowByPath.TryGetValue(path, out row))
                Theme.PaintBadgeCell(e, row.Level);
        }

        /// <summary>Nút "Xem chi tiết" trong từng dòng của bảng (b) -> mở trang (c) đúng dòng đó.</summary>
        private void DgvActions_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colActionView.Index) return;
            MoChiTietKetQua(e.RowIndex);
        }

        // ================= CÁCH LY / XÓA (dùng chung cho (b), (c) và cả cách ly tự động) =================

        private void BtnQuarantineSelected_Click(object sender, EventArgs e)
        {
            QuarantineRows(TickedRows());
        }

        private void BtnQuarantineAll_Click(object sender, EventArgs e)
        {
            QuarantineRows(dgvActions.Rows.Cast<DataGridViewRow>().ToList());
        }

        private void QuarantineRows(List<DataGridViewRow> rows)
        {
            if (rows == null || rows.Count == 0) return;
            int done = 0;
            var handledArchives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in rows)
            {
                if (row.DataGridView == null) continue; // cùng ZIP có thể có nhiều entry
                string path = row.Cells[colActionFile.Index].Value as string;
                string reason = row.Cells[colActionThreat.Index].Value as string;  // lưu lý do vào sổ cách ly
                int archiveSep = path == null ? -1 : path.IndexOf(" > ", StringComparison.Ordinal);
                if (archiveSep >= 0)
                {
                    string archivePath = path.Substring(0, archiveSep);
                    if (!handledArchives.Add(archivePath)) continue;
                    if (!File.Exists(archivePath)) continue;
                    DialogResult approve = AppDialog.Show(
                        "Phát hiện bên trong tệp nén:\r\n" + path +
                        "\r\n\r\nNếu tiếp tục, TOÀN BỘ ZIP gốc sẽ được cách ly, không chỉ entry này.\r\nBạn xác nhận?",
                        "Xác nhận cách ly tệp nén", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (approve != DialogResult.Yes) continue;
                    if (ScanEngine.Quarantine(archivePath, reason))
                    {
                        foreach (DataGridViewRow related in dgvActions.Rows.Cast<DataGridViewRow>().ToList())
                        {
                            string relatedPath = related.Cells[colActionFile.Index].Value as string;
                            if (string.Equals(relatedPath, archivePath, StringComparison.OrdinalIgnoreCase) ||
                                (relatedPath != null && relatedPath.StartsWith(archivePath + " > ", StringComparison.OrdinalIgnoreCase)))
                            {
                                ThreatRow threatRow;
                                if (rowByPath.TryGetValue(relatedPath, out threatRow)) threatRow.Quarantined = true;
                                dgvActions.Rows.Remove(related);
                                done++;
                            }
                        }
                    }
                    continue;
                }
                // Chỉ xác nhận cách ly khi tệp đã chuyển vào kho và SQL lưu thành công.
                // Trước đây !File.Exists(path) cũng bị tính là cách ly thành công,
                // dù không có bản ghi nào trong bảng dữ liệu Cách ly.
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path) && ScanEngine.Quarantine(path, reason))
                {
                    ThreatRow tr;
                    if (rowByPath.TryGetValue(path, out tr)) tr.Quarantined = true;
                    dgvActions.Rows.Remove(row);
                    done++;
                }
            }
            UpdateThreatUi();
            RefreshDetailGrid();
            ProtectionNotificationService.Publish("Kết quả cách ly", "Đã cách ly " + done + "/" + rows.Count + " tệp.",
                done == rows.Count ? ProtectionNoticeLevel.Success : ProtectionNoticeLevel.Warning,
                "manual-quarantine-result", 2, true);
            if (done != rows.Count)
                AppDialog.Show(string.Format("Đã cách ly {0}/{1} tệp; hãy kiểm tra các tệp chưa xử lý.", done, rows.Count),
                    "Cách ly chưa đầy đủ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void BtnDeleteSelected_Click(object sender, EventArgs e)
        {
            DeleteRows(TickedRows());
        }

        private void DeleteRows(List<DataGridViewRow> rows)
        {
            if (rows == null || rows.Count == 0) return;
            DialogResult answer = AppDialog.Show(
                string.Format("Xóa vĩnh viễn {0} tệp khỏi máy tính? Hành động này không thể hoàn tác.", rows.Count),
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;

            int done = 0;
            foreach (DataGridViewRow row in rows)
            {
                string path = row.Cells[colActionFile.Index].Value as string;
                try
                {
                    if (string.IsNullOrEmpty(path)) continue;
                    // Không coi entry ảo là tệp vật lý, và tuyệt đối không tự xóa cả archive.
                    if (path.IndexOf(" > ", StringComparison.Ordinal) >= 0) continue;
                    if (!File.Exists(path)) continue;
                    File.Delete(path);
                    ThreatRow tr;
                    if (rowByPath.TryGetValue(path, out tr)) tr.Deleted = true;
                    done++;
                    dgvActions.Rows.Remove(row);
                }
                catch (Exception) { } // tệp đang bị khoá: giữ dòng lại để thử lại sau
            }
            UpdateThreatUi();
            RefreshDetailGrid();
            ProtectionNotificationService.Publish("Kết quả xóa tệp", "Đã xóa " + done + "/" + rows.Count + " tệp.",
                done == rows.Count ? ProtectionNoticeLevel.Success : ProtectionNoticeLevel.Warning,
                "manual-delete-result", 2, true);
            if (done != rows.Count)
                AppDialog.Show(string.Format("Đã xóa {0}/{1} tệp; hãy kiểm tra các tệp còn lại.", done, rows.Count),
                    "Xóa chưa đầy đủ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ================= TRA CỨU VIRUSTOTAL (chỉ gửi hash SHA256 — không upload nội dung tệp) =================

        private void BtnVirusTotal_Click(object sender, EventArgs e)
        {
            if (isScanning) return;
            DataGridViewRow row = dgvActions.SelectedRows.Count > 0
                ? dgvActions.SelectedRows[0]
                : (dgvActions.Rows.Count > 0 ? dgvActions.Rows[0] : null);
            if (row == null) return;
            TraVirusTotalTheoPath(row.Cells[colActionFile.Index].Value as string);
        }

        /// <summary>
        /// Tra hash SHA256 của 1 tệp lên VirusTotal (dùng chung cho bảng (b) và trang (c)).
        /// Bắt buộc API key; chỉ gửi hash, không upload nội dung tệp.
        /// </summary>
        internal async void TraVirusTotalTheoPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!File.Exists(path))
            {
                AppDialog.Show("Tệp không còn ở vị trí cũ nên không tính được hash.",
                    "VirusTotal", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!VirusTotalClient.IsConfigured)
            {
                using (var config = new VirusTotalConfigForm()) config.ShowDialog(FindForm());
                if (!VirusTotalClient.IsConfigured) return;
            }

            string hash = null;
            btnVirusTotal.Enabled = false;
            lblScanProgress.Text = "Đang tính hash + tra cứu VirusTotal...";
            lblScanProgress.ForeColor = Theme.Blue;
            lblScanProgress.Visible = true;
            try
            {
                VirusTotalReport report = await Task.Run(() =>
                {
                    hash = ScanEngine.ComputeFileSha256(path);
                    return VirusTotalClient.QueryHashOrUpload(hash, path);
                });

                string verdict = report.Error != null ? "lỗi"
                    : !report.Found ? "chưa có mẫu"
                    : report.IsMalicious ? "ĐỘC HẠI"
                    : report.IsSuspicious ? "nghi ngờ" : "AN TOÀN";
                lblScanProgress.Text = "VirusTotal: " + report.Summary();
                lblScanProgress.ForeColor = report.IsMalicious ? Theme.Red : Theme.Green;

                if (report.Error != null)
                {
                    ThreatRow errorRow;
                    if (rowByPath.TryGetValue(path, out errorRow))
                    {
                        errorRow.VtReport = report;
                        errorRow.VtQueriedAt = DateTime.Now;
                    }
                }
                if (report.Error == null)
                {
                    string cell = string.Format("VirusTotal [{0}]: {1}{2}", verdict, report.Summary(),
                        hash != null ? "  —  SHA256 " + hash.Substring(0, 16) + "…" : string.Empty);
                    foreach (DataGridViewRow r in dgvActions.Rows)
                        if (Equals(r.Cells[colActionFile.Index].Value, path))
                            r.Cells[colActionThreat.Index].Value = cell;

                    ThreatRow tr;
                    if (rowByPath.TryGetValue(path, out tr))
                    {
                        tr.VtReport = report;
                        tr.HashSha256 = hash;
                        tr.VtQueriedAt = DateTime.Now;
                        tr.VtLink = hash == null ? null : "https://www.virustotal.com/gui/file/" + hash;
                        tr.ThreatText = cell;
                        if (report.IsMalicious) tr.Level = Theme.ThreatLevel.High;
                    }
                    RefreshDetailGrid();
                    if (detailRowIndex >= 0 && detailRowIndex < threatRows.Count
                        && threatRows[detailRowIndex].Path == path)
                        NapPanelChiTiet(detailRowIndex);
                }
                AppDialog.Show(report.Summary(), "VirusTotal: " + verdict,
                    MessageBoxButtons.OK,
                    report.IsMalicious ? MessageBoxIcon.Error : MessageBoxIcon.Information);
            }
            finally
            {
                btnVirusTotal.Enabled = dgvActions.Rows.Count > 0;
            }
        }

        /// <summary>Chỉ gửi toàn bộ nội dung tệp sau khi người dùng xác nhận rõ ràng.</summary>
        internal async void GuiMauVirusTotalTheoPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                AppDialog.Show("Tệp không tồn tại ở đường dẫn vật lý. Không thể gửi nội dung tệp nằm bên trong ZIP.",
                    "VirusTotal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!VirusTotalClient.IsConfigured)
            {
                using (var config = new VirusTotalConfigForm()) config.ShowDialog(FindForm());
                if (!VirusTotalClient.IsConfigured) return;
            }
            DialogResult accepted = AppDialog.Show(
                "GỬI TOÀN BỘ TỆP ĐẾN VIRUSTOTAL?\n\n" +
                "VirusTotal có thể lưu trữ và chia sẻ mẫu với các đối tác bảo mật. " +
                "Không gửi tài liệu riêng tư, dữ liệu cá nhân hoặc thông tin mật.\n\n" +
                "Tệp: " + Path.GetFileName(path) + "\n\n" +
                "Chọn YES chỉ khi bạn đồng ý gửi nội dung tệp ra ngoài máy.",
                "Xác nhận gửi mẫu VirusTotal", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (accepted != DialogResult.Yes) return;
            lblScanProgress.Text = "Đang gửi mẫu lên VirusTotal (có thể mất một lúc)...";
            lblScanProgress.Visible = true;
            try
            {
                VirusTotalReport reply = await Task.Run(() => VirusTotalClient.UploadAndAnalyze(path));
                ThreatRow selected;
                if (rowByPath.TryGetValue(path, out selected))
                {
                    selected.VtReport = reply;
                    selected.VtQueriedAt = DateTime.Now;
                    if (detailRowIndex >= 0 && detailRowIndex < threatRows.Count && threatRows[detailRowIndex].Path == path)
                        NapPanelChiTiet(detailRowIndex);
                }
                lblScanProgress.Text = "VirusTotal: " + reply.Summary();
                lblScanProgress.ForeColor = reply.Error == null ? Theme.Blue : Theme.Red;
                AppDialog.Show(reply.Summary(), "VirusTotal: gửi mẫu",
                    MessageBoxButtons.OK, reply.Error == null ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppDialog.Show("Gửi mẫu thất bại: " + ex.Message, "VirusTotal",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string NhapApiKeyDialog()
        {
            return AppDialog.Prompt(FindForm(), "API key VirusTotal",
                "Dán API key miễn phí (virustotal.com, mục Profile, phần API key).\n"
                + "App chỉ gửi hash SHA256 ra ngoài — KHÔNG upload nội dung tệp của bạn.",
                "", true);
        }

        /// <summary>
        /// Hàng "Bảo vệ web (VirusTotal)": heuristic nghi vấn -> tự tra hash trên cloud.
        /// Tối đa 2 dòng, cách 16s — giữ ngưỡng 4 request/phút của gói miễn phí.
        /// </summary>
        private void AutoQueryHeuristicRows(ScanResult result)
        {
            if (!FeatureFlags.VtAutoQuery || !VirusTotalClient.IsConfigured) return;
            var targets = result.Threats.Where(t => t.Kind == "Heuristic").Take(2).ToList();
            if (targets.Count == 0) return;
            Task.Run(() =>
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    if (i > 0) Thread.Sleep(16000);
                    ThreatFound t = targets[i];
                    string hash = ScanEngine.ComputeFileSha256(t.FilePath);
                    if (hash == null) continue;
                    VirusTotalReport rep = VirusTotalClient.QueryHashOrUpload(hash, t.FilePath);
                    if (rep.Error != null || !rep.Found) continue;
                    string label = "VirusTotal [tự động]: " + rep.Summary();
                    string pathAuto = t.FilePath;
                    try
                    {
                        BeginInvoke((MethodInvoker)(() =>
                        {
                            foreach (DataGridViewRow r in dgvActions.Rows)
                                if (Equals(r.Cells[colActionFile.Index].Value, pathAuto))
                                    r.Cells[colActionThreat.Index].Value = label;
                            ThreatRow tr;
                            if (rowByPath.TryGetValue(pathAuto, out tr))
                            {
                                tr.VtReport = rep;
                                tr.HashSha256 = hash;
                                tr.VtLink = "https://www.virustotal.com/gui/file/" + hash;
                                tr.ThreatText = label;
                            }
                        }));
                    }
                    catch (ObjectDisposedException) { return; }
                    catch (InvalidOperationException) { return; }
                }
            });
        }

        // ================= SỐ LIỆU THẬT: THỐNG KÊ, HOẠT ĐỘNG, TRẠNG THÁI BẢO VỆ =================

        private void LoadProtectionStatus()
        {
            // Nếu quá 24 giờ và đã cấu hình Auth-Key, đồng bộ MalwareBazaar ở nền.
            // Không đóng dấu cập nhật giả khi chưa tải dữ liệu thành công.
            GuardService.EnsureDailyAutoUpdate();


            ApplyLastScanFromHistory();
            LoadActivity();
            OnRealTimeStatus(RealTimeProtection.IsRunning);
        }

        /// <summary>"Lần quét gần nhất" + "Tệp đã quét" khôi phục từ lịch sử thật (không dùng dữ liệu mẫu).</summary>
        private void ApplyLastScanFromHistory()
        {
            HistoryEntry e = ScanHistoryStore.LatestOfType("Quét");
            if (e == null)
            {
                // Giữ kết quả phiên hiện tại khi SQL chưa lưu được lịch sử.
                if (lastScanTime != DateTime.MinValue) return;
                lblLastScanDate.Text = "Chưa quét";
                lblLastScanType.Text = "Bắt đầu một phiên quét mới";
            }
            else
            {
                lblLastScanDate.Text = e.Time.ToString("dd/MM/yyyy");
                lblLastScanType.Text = e.Time.ToString("HH:mm") + " · " + e.Type;
                // "Tệp đã quét" cũng khôi phục theo phiên gần nhất (0 lúc mới mở app là dữ liệu mẫu)
                lblScannedCount.Text = e.Files.ToString("N0");
                lastScannedFiles = e.Files;
            }
        }

        /// <summary>Bảng "Hoạt động gần đây" — đọc trực tiếp scanhistory.log, mới nhất lên đầu.</summary>
        private void LoadActivity()
        {
            List<HistoryEntry> all = ScanHistoryStore.Entries();
            if (all.Count == 0 && lastResult != null && lastScanTime != DateTime.MinValue)
                all.Add(new HistoryEntry
                {
                    Time = lastScanTime, Type = lastJob == null ? "Quét" : lastJob.DisplayName,
                    Scope = lastJob == null ? "" : lastJob.ScopeText, Files = lastScannedFiles,
                    Threats = lastResult.Threats.Count, Seconds = lastResult.Duration.TotalSeconds
                });
            dgvActivity.Rows.Clear();
            int shown = 0;
            foreach (HistoryEntry h in all)
            {
                if (h == null) continue;
                if (shown >= MaxActivityRows) break;
                dgvActivity.Rows.Add(BieuTuongSuKien(h), MoTaSuKien(h),
                    h.Time.ToString("dd/MM/yyyy HH:mm"));
                shown++;
            }
            lblActivitySummary.Text = shown == 0
                ? "Chưa có hoạt động nào được ghi trong scanhistory.log."
                : string.Format("{0} hoạt động gần nhất — mới nhất ở trên.", shown);
            lnkXemLichSu.Visible = shown > 0;
            SyncDashboardActivity(shown);
        }

        /// <summary>
        /// Icon của một dòng "Hoạt động gần đây". Mockup dùng icon theo loại sự kiện (không phải chấm
        /// màu) nên mỗi loại có hình riêng; màu vẫn theo <see cref="MauSuKien"/> để giữ tín hiệu cũ.
        /// </summary>
        private static Bitmap BieuTuongSuKien(HistoryEntry h)
        {
            if (h.Type != null && h.Type.StartsWith("Cập nhật"))
                return UiIcons.Refresh(22, MauSuKien(h));
            if (h.Type != null && h.Type.StartsWith("Bảo vệ"))
                return UiIcons.ShieldOk(22);
            return UiIcons.Search(22, MauSuKien(h));
        }

        private static Color MauSuKien(HistoryEntry h)
        {
            if (h.Type != null && h.Type.StartsWith("Cập nhật")) return Theme.Blue;
            if (h.Type != null && h.Type.StartsWith("Bảo vệ")) return Theme.Green;
            return h.Threats > 0 ? Theme.Red : Theme.Green;
        }

        private static string MoTaSuKien(HistoryEntry h)
        {
            if (h.Type != null && h.Type.StartsWith("Cập nhật"))
                return "Cập nhật cơ sở dữ liệu chữ ký virus";
            if (h.Type != null && h.Type.StartsWith("Bảo vệ"))
                return "Bảo vệ thời gian thực: " + h.Scope;
            return string.Format("{0} · {1} tệp · {2} đe dọa", h.Type, h.Files.ToString("N0"), h.Threats);
        }

        private void OnRealTimeStatus(bool running)
        {
            if (!IsHandleCreated) { ApplyRealTimeStatus(running); return; }
            try { BeginInvoke((MethodInvoker)(() => ApplyRealTimeStatus(running))); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private void ApplyRealTimeStatus(bool running)
        {

            lblProtectionStatus.Text = running
                ? "Máy tính của bạn được bảo vệ"
                : "Bảo vệ thời gian thực đang tắt";
            lblProtectionStatus.ForeColor = running ? Theme.Green : Theme.Amber;
            lblAnToanSub.Text = running
                ? "Không phát hiện mối đe dọa."
                : "Không phát hiện mối đe dọa, nhưng bảo vệ thời gian thực đang tắt — hãy bật lại ở tab Bảo vệ.";
            SyncDashboardProtection(running);
        }

        /// <summary>
        /// Vạch chia của hàng 3 thẻ số liệu. Ba thẻ là Panel trơn (không viền) nên pnlThongKe tự vẽ:
        /// 1 đường ngang trên + 1 dưới, 2 đường dọc ở ranh giới cột — đúng kiểu "dải số liệu" của mockup.
        /// Vẽ ở đây (không thêm cột/khung vào pnlThongKe) để giữ ColumnCount == 3 như Tests §3e yêu cầu.
        /// </summary>
        private void PnlThongKe_Paint(object sender, PaintEventArgs e)
        {
            int w = pnlThongKe.ClientSize.Width, h = pnlThongKe.ClientSize.Height;
            if (w < 6 || h < 6) return;
            using (var pen = new Pen(Theme.Divider))
            {
                e.Graphics.DrawLine(pen, 0, 0, w, 0);
                e.Graphics.DrawLine(pen, 0, h - 1, w, h - 1);
                e.Graphics.DrawLine(pen, w / 3, 6, w / 3, h - 7);
                e.Graphics.DrawLine(pen, (w * 2) / 3, 6, (w * 2) / 3, h - 7);
            }
        }

        // (25/09/2026) Đã bỏ khỏi tab Tổng quan: nút "Kiểm tra cập nhật" + BtnCheckUpdate_Click/MarkDbUpdated
        // (khối "cập nhật dữ liệu") và ô đếm cách ly + OnQuarantineChangedTongQuan/ApplyQuarantineCount/
        // lblQuarantineCount_Click (thẻ "4. Đang cách ly"). Tự động cập nhật 24h vẫn do GuardService lo,
        // và bật/tắt nó nằm ở tab Cài đặt; số tệp cách ly xem ở tab Cách ly.

        /// <summary>Liên kết "Mở tab Lịch sử" ở thẻ "Hoạt động gần đây" -> mở tab Lịch sử của FrmMain.</summary>
        private void MoTabLichSu()
        {
            var main = FindForm() as FrmMain;
            if (main != null) main.MoTabLichSu();
        }

        // ================= QUÉT 1 VỊ TRÍ TÙY CHỌN (thẻ "Tuỳ chọn quét nhanh" đã bỏ 25/09/2026) =================

        /// <summary>
        /// Ghi nhận đường dẫn cần quét. Thẻ "Tuỳ chọn quét nhanh" (3 radio + nút Chọn tệp/Chọn thư mục)
        /// đã bị bỏ khỏi giao diện nên đây là API nội bộ cho luồng quét đúng 1 vị trí
        /// (nút "Quét ngay" đọc qua GetSelectedScanType); UI test gọi trực tiếp bằng reflection.
        /// </summary>
        private void SetCustomPath(string path)
        {
            customScanPath = path;
        }

        /// <summary>
        /// "Quét ngay" quét gì: đã có đường dẫn tùy chọn -> Quét tùy chọn (đúng vị trí đó);
        /// chưa có -> Quét nhanh (Desktop + Downloads + Temp). Quét toàn bộ ổ đĩa nằm ở trang
        /// "Quét nâng cao" (chế độ Toàn bộ hệ thống).
        /// </summary>
        private ScanType GetSelectedScanType()
        {
            return string.IsNullOrEmpty(customScanPath) ? ScanType.Quick : ScanType.Custom;
        }

        private static string DescribeScan(ScanType type)
        {
            switch (type)
            {
                case ScanType.Full: return "Quét toàn bộ";
                case ScanType.Custom: return "Quét tùy chọn";
                default: return "Quét nhanh";
            }
        }

        private static string ScopeOf(ScanType type, string customPath)
        {
            if (type == ScanType.Full) return "Toàn bộ ổ đĩa";
            if (type == ScanType.Quick) return "Khu vực hệ thống (Desktop, Downloads, Temp)";
            return File.Exists(customPath) ? "Tệp: " + customPath : "Thư mục: " + customPath;
        }

        // ================= NÚT "QUÉT NÂNG CAO ▾" — POPUP CUSTOM 4 CHẾ ĐỘ =================

        private void BtnQuetNangCao_Click(object sender, EventArgs e)
        {
            UiActionPopup.ShowFor(btnQuetNangCao, new[]
            {
                new UiActionEntry("Quét toàn bộ hệ thống", delegate { MoQuetNangCao(CheDoQuetNangCao.FullSystem); },
                    "Kiểm tra tất cả ổ đĩa và tệp", UiIcons.Dot(14, Theme.Red)),
                new UiActionEntry("Quét thư mục", delegate { MoQuetNangCao(CheDoQuetNangCao.Folder); },
                    "Chọn thư mục để quét", UiIcons.Folder(16, Theme.Blue)),
                new UiActionEntry("Quét tệp", delegate { MoQuetNangCao(CheDoQuetNangCao.Files); },
                    "Chọn một hoặc nhiều tệp để quét", UiIcons.FileIcon(16, ".exe")),
                new UiActionEntry("Quét tùy chỉnh", delegate { MoQuetNangCao(CheDoQuetNangCao.Custom); },
                    "Cấu hình vị trí và loại tệp quét", UiIcons.Gear(16, Theme.Blue))
            }, 320);
        }

        private void pnlPageHead_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnScanNow_Click_1(object sender, EventArgs e)
        {

        }

        private void btnScanNow_Click_2(object sender, EventArgs e)
        {

        }

        private void btnScanNow_Click_3(object sender, EventArgs e)
        {

        }
    }
}









