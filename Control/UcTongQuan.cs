using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

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
        private const float HeroHeight = 164F;     // hàng hero (a)/(b) của tableLayoutPanel12: panel 160 + 2 lề 2px
        private const float ThongKeHeight = 104F;  // hàng 3 thẻ số liệu của tableLayoutPanel12: panel 100 + 2 lề 2px (chỉ ở (a)/(b))
        private const float LoadingHeight = 46F;   // hàng 0 của tableLayoutPanel11 — dải loading quét (lần 9): panel 46px khi quét, 0px khi rảnh
        private const int RongToiThieu = 980;      // (Theme.ScrollablePage) bề rộng tối thiểu vùng Tổng quan ở (a)/(b)/(c)
        private const int CaoToiThieu = 640;       // chiều cao tối thiểu vùng Tổng quan (mọi màn hình): nhỏ hơn -> cuộn dọc

        /// <summary>Một dòng đe dọa hiển thị ở (b)/(c) — gộp dữ liệu ScanEngine + VirusTotal.</summary>
        internal sealed class ThreatRow
        {
            public string Path;
            public string ThreatText;                        // "Chữ ký: ..." / "Heuristic: ..." / "VirusTotal [...]: ..."
            public Theme.ThreatLevel Level;
            public bool Quarantined;
            public bool Deleted;
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
                get { return Deleted ? "Đã xóa" : (Quarantined ? "Đã cách ly" : "Đang có trên máy"); }
            }
            public bool Resolved { get { return Deleted || Quarantined; } }
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
            // thiểu 164+104+303 = 571px + lề nên ở ngưỡng này vẫn thấy đủ 4 khối, không cắt ngang nút).
            // Riêng trang (d) tự xếp lại theo bề rộng nên HienThi() hạ bề rộng tối thiểu (lần 7).
            Theme.ScrollablePage(this, tableLayoutPanel11, RongToiThieu, CaoToiThieu);
            ApplyTheme();
            BuildChiTietView();       // ruột trang (c) — UcTongQuan.ChiTiet.cs
            BuildQuetNangCaoView();   // ruột trang (d) — UcTongQuan.QuetNangCao.cs
            WireEvents();
            HienThi(TongQuanView.AnToan, false);
            LoadProtectionStatus();
        }

        // ================= GIAO DIỆN: MỘT NGUỒN THEME DUY NHẤT =================

        private void ApplyTheme()
        {

            // (25/09/2026) Thẻ "4. Đang cách ly" (grpQuarantine) đã bị bỏ khỏi tab Tổng quan
            Theme.StyleCard(grpThreats, grpScannedFiles, grpLastScan,
                grpAction, grpActivity);
            Theme.StyleGrid(dgvActions);
            Theme.StyleGrid(dgvActivity);
            Theme.StyleLinkLabel(lnkXemTatCa);
            Theme.StyleLinkLabel(lnkXemLichSu);
            Theme.StyleButton(btnScanNow, Theme.BtnRole.Primary);
            Theme.StyleButton(btnQuetNangCao, Theme.BtnRole.Secondary);
            Theme.StyleButton(btnXemChiTiet, Theme.BtnRole.Primary);
            Theme.StyleButton(btnQuetLai, Theme.BtnRole.Secondary);
            Theme.StyleButton(btnQuarantineSelected, Theme.BtnRole.Action);
            Theme.StyleButton(btnQuarantineAll, Theme.BtnRole.Action);
            Theme.StyleButton(btnDeleteSelected, Theme.BtnRole.Danger);
            Theme.StyleButton(btnVirusTotal, Theme.BtnRole.Action);
            // (25/09/2026, lần 9) Dải loading quét — màu sắc lấy từ một nguồn theme duy nhất
            Theme.StyleLoadingStrip(pnlLoadingQuet, spinnerDangQuet, lblLoadingTieuDe, lblLoadingChiTiet,
                btnHuyQuetLoading);
            btnHuyQuetLoading.Image = UiIcons.Stop(12, Color.White);
            btnHuyQuetLoading.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnScanNow.Image = UiIcons.Play(13, Color.White);
            btnScanNow.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnQuetNangCao.Image = UiIcons.Gear(15, Theme.BlueDark);
            btnQuetNangCao.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnXemChiTiet.Image = UiIcons.External(13, Color.White);
            btnQuetLai.Image = UiIcons.Refresh(13, Theme.BlueDark);
            picShield.Image = UiIcons.ShieldOk(72);
            picCanhBao.Image = UiIcons.AlertCircle(72);
            lblThreatCount.ForeColor = Theme.Green;
            lblThreatText.ForeColor = Theme.Green;
            lblScannedCount.ForeColor = Theme.Blue;
        }

        private void WireEvents()
        {
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
            // 0=hero (a)/(b) 164 (đủ cho khiên 72 + 3 dòng chữ + hàng 2 nút quét) · 1=3 thẻ số liệu 104
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
            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            bool completed = false;
            Action<int, string> progress = (count, current) =>
            {
                try
                {
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        lblScannedCount.Text = count.ToString("N0");
                        if (current != null)
                        {
                            lblScanProgress.Text = string.Format("Đang quét: {0}  ({1:N0} tệp)",
                                current, count);
                            // (lần 9) dải loading ở đầu trang bám đúng tiến trình của nhãn trạng thái quét
                            CapNhatDongChiTietLoading(lblScanProgress.Text);
                        }
                    }));
                }
                catch (ObjectDisposedException) { } // form đã đóng giữa chừng
                catch (InvalidOperationException) { }
            };

            try
            {
                List<string> targets = job.Targets != null && job.Targets.Count > 0
                    ? new List<string>(job.Targets)
                    : null;

                ScanResult result = await Task.Run(() =>
                {
                    if (targets == null)
                        return ScanEngine.Scan(job.Type, job.CustomPath, token, progress);

                    var merged = new ScanResult();
                    foreach (string target in targets)
                    {
                        token.ThrowIfCancellationRequested();
                        ScanResult part = ScanEngine.Scan(ScanType.Custom, target, token, progress);
                        merged.FilesScanned += part.FilesScanned;
                        merged.Duration += part.Duration;
                        foreach (ThreatFound t in part.Threats) merged.Threats.Add(t);
                    }
                    return merged;
                });

                completed = true;
                ApDungKetQuaQuet(result, job);
            }
            catch (OperationCanceledException)
            {
                lblScanProgress.ForeColor = Theme.Amber;
                lblScanProgress.Text = "Đã hủy phiên quét.";
                CapNhatDongChiTietLoading("Đã hủy phiên quét.");
                MessageBox.Show("Đã hủy phiên quét.", job.DisplayName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblScanProgress.ForeColor = Theme.Red;
                lblScanProgress.Text = "Quét dừng vì lỗi.";
                CapNhatDongChiTietLoading("Quét dừng vì lỗi.");
                MessageBox.Show("Quét dừng vì lỗi: " + ex.Message, job.DisplayName,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetScanning(false);
                if (cts != null) { cts.Dispose(); cts = null; }
            }
            return completed;
        }

        /// <summary>Hiển thị kết quả phiên quét: bảng đe dọa, 5 tab chi tiết, thống kê, lịch sử, thông báo.</summary>
        private void ApDungKetQuaQuet(ScanResult result, ScanJob job)
        {
            lastResult = result;
            lastScanTime = DateTime.Now;
            lastScannedFiles = result.FilesScanned;
            lblScannedCount.Text = result.FilesScanned.ToString("N0");
            lblScanProgress.Text = string.Format("Hoàn tất — đã quét {0:N0} tệp trong {1:0.#} giây.",
                result.FilesScanned, result.Duration.TotalSeconds);
            lblScanProgress.ForeColor = result.HasThreats ? Theme.Amber : Theme.Green;

            lblLastScanDate.Text = lastScanTime.ToString("dd/MM/yyyy  HH:mm");
            lblLastScanType.Text = job.DisplayName;

            LoadDetectedThreats(result);   // đổ vào dgvActions + threatRows (có lọc nếu quét từ (d))
            // Quét xong -> luôn rẽ nhánh về trạng thái (a) hoặc (b) (SPEC §6.1/§6.2)
            HienThi(dgvActions.Rows.Count > 0 ? TongQuanView.PhatHienDeDoa : TongQuanView.AnToan, false);
            ScanHistoryStore.Add(job.DisplayName, job.ScopeText,
                result.FilesScanned, result.Threats.Count, result.Duration.TotalSeconds);

            if (job.AutoQuarantine) TuCachLyTheoPhien();
            AutoQueryHeuristicRows(result);      // hàng "Bảo vệ web" nếu đang bật
            LoadActivity();
            ApplyLastScanFromHistory();

            MessageBox.Show(
                result.HasThreats
                    ? string.Format("Phát hiện {0} mối đe dọa trong {1:N0} tệp. Hãy xử lý trong khu vực \"Mối đe dọa được phát hiện\" bên dưới.",
                        result.Threats.Count, result.FilesScanned)
                    : string.Format("Đã quét {0:N0} tệp, không phát hiện mối đe dọa.", result.FilesScanned),
                job.DisplayName + " hoàn tất", MessageBoxButtons.OK,
                result.HasThreats ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
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
            btnScanNow.Image = scanning ? UiIcons.Stop(13, Color.White) : UiIcons.Play(13, Color.White);
            Theme.StyleButton(btnScanNow, scanning ? Theme.BtnRole.Cancel : Theme.BtnRole.Primary);
            // (25/09/2026, lần 4) 2 nút "Quét ngay"/"Quét nâng cao" nay nằm TRONG khối (a) pnlAnToan, nên khi
            // người dùng đang ở trạng thái (b) thì nút "Quét lại" đảm nhiệm luôn vai trò "Hủy quét" —
            // không mất đường hủy phiên quét giữa chừng.
            btnQuetLai.Text = scanning ? "Hủy quét" : "Quét lại";
            btnQuetLai.Image = scanning ? UiIcons.Stop(13, Color.White) : UiIcons.Refresh(13, Theme.BlueDark);
            Theme.StyleButton(btnQuetLai, scanning ? Theme.BtnRole.Cancel : Theme.BtnRole.Secondary);
            // Dòng trạng thái quét nay nằm trong khối (a) pnlAnToan, ngay cạnh 2 nút quét
            lblScanProgress.ForeColor = Theme.Blue;
            if (scanning)
                lblScanProgress.Text = "Đang quét...";
            // (25/09/2026, lần 9) Dải loading quét: hiện NGAY khi phiên bắt đầu, ẩn khi phiên kết thúc
            // (xong / hủy / lỗi — `SetScanning(false)` nằm trong `finally` của ChayPhienQuet).
            HienThiDaiLoading(scanning);
            OnSetScanningAdvanced(scanning);
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
                if (!ThoaBoLocCuaTrangQuetNangCao(threat)) continue;   // "Loại tệp quét" / "Bỏ qua tệp lớn hơn"
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
            string ext = Path.GetExtension(threat.FilePath);
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
            foreach (DataGridViewRow row in rows)
            {
                string path = row.Cells[colActionFile.Index].Value as string;
                string reason = row.Cells[colActionThreat.Index].Value as string;  // lưu lý do vào sổ cách ly
                if (string.IsNullOrEmpty(path) || !File.Exists(path) || ScanEngine.Quarantine(path, reason))
                {
                    ThreatRow tr;
                    if (path != null && rowByPath.TryGetValue(path, out tr)) tr.Quarantined = true;
                    dgvActions.Rows.Remove(row);
                    done++;
                }
            }
            UpdateThreatUi();
            RefreshDetailGrid();
            MessageBox.Show(string.Format("Đã cách ly {0}/{1} tệp.", done, rows.Count),
                "Cách ly", MessageBoxButtons.OK,
                done == rows.Count ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void BtnDeleteSelected_Click(object sender, EventArgs e)
        {
            DeleteRows(TickedRows());
        }

        private void DeleteRows(List<DataGridViewRow> rows)
        {
            if (rows == null || rows.Count == 0) return;
            DialogResult answer = MessageBox.Show(
                string.Format("Xóa vĩnh viễn {0} tệp khỏi máy tính? Hành động này không thể hoàn tác.", rows.Count),
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;

            int done = 0;
            foreach (DataGridViewRow row in rows)
            {
                string path = row.Cells[colActionFile.Index].Value as string;
                try
                {
                    if (string.IsNullOrEmpty(path)) { dgvActions.Rows.Remove(row); continue; }
                    if (File.Exists(path)) File.Delete(path);
                    ThreatRow tr;
                    if (rowByPath.TryGetValue(path, out tr)) tr.Deleted = true;
                    done++;
                    dgvActions.Rows.Remove(row);
                }
                catch (Exception) { } // tệp đang bị khoá: giữ dòng lại để thử lại sau
            }
            UpdateThreatUi();
            RefreshDetailGrid();
            MessageBox.Show(string.Format("Đã xóa {0}/{1} tệp.", done, rows.Count),
                "Xóa tệp", MessageBoxButtons.OK,
                done == rows.Count ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
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
                MessageBox.Show("Tệp không còn ở vị trí cũ nên không tính được hash.",
                    "VirusTotal", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!VirusTotalClient.IsConfigured)
            {
                string key = NhapApiKeyDialog();
                if (key == null) return; // người dùng hủy
                if (key.Length == 0)
                {
                    MessageBox.Show("Lấy API key miễn phí tại virustotal.com (Hồ sơ → API key), "
                        + "hoặc tự tạo tệp:\n" + VirusTotalClient.ApiKeyPath,
                        "VirusTotal cần API key", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                VirusTotalClient.SaveApiKey(key);
            }

            string hash = null;
            btnVirusTotal.Enabled = false;
            lblScanProgress.Text = "Đang tính hash + tra cứu VirusTotal...";
            lblScanProgress.ForeColor = Theme.Blue;
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
                MessageBox.Show(report.Summary(), "VirusTotal: " + verdict,
                    MessageBoxButtons.OK,
                    report.IsMalicious ? MessageBoxIcon.Error : MessageBoxIcon.Information);
            }
            finally
            {
                btnVirusTotal.Enabled = dgvActions.Rows.Count > 0;
            }
        }

        private string NhapApiKeyDialog()
        {
            using (var f = new Form())
            {
                f.Text = "API key VirusTotal";
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.MinimizeBox = false;
                f.MaximizeBox = false;
                f.ClientSize = new Size(460, 150);
                f.BackColor = Theme.PageBg;
                var lbl = new Label
                {
                    Text = "Dán API key miễn phí (virustotal.com → Profile → API key).\n"
                        + "App chỉ gửi hash SHA256 ra ngoài — KHÔNG upload nội dung tệp của bạn.",
                    Left = 12, Top = 10, Width = 436, Height = 46, ForeColor = Theme.TextMid
                };
                var txt = new TextBox { Left = 12, Top = 62, Width = 436 };
                var ok = new Button { Text = "Lưu", DialogResult = DialogResult.OK, Left = 272, Top = 100, Width = 85, Height = 32 };
                var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Left = 363, Top = 100, Width = 85, Height = 32 };
                Theme.StyleButton(ok, Theme.BtnRole.Primary);
                Theme.StyleButton(cancel, Theme.BtnRole.Neutral);
                f.Controls.AddRange(new System.Windows.Forms.Control[] { lbl, txt, ok, cancel });
                f.AcceptButton = ok;
                f.CancelButton = cancel;
                if (f.ShowDialog(FindForm()) != DialogResult.OK) return null;
                return txt.Text.Trim();
            }
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
            // "Tự động cập nhật": tem chữ ký quá hạn 24h -> tự đóng dấu + xóa cache ngay khi mở tab
            // (25/09/2026) Khối "cập nhật dữ liệu" trên Tổng quan đã bỏ -> ở đây chỉ còn đồng bộ nhãn phiên bản;
            // tem CSDL vẫn được đóng dấu trong scanhistory.log và hiện thành dòng ở bảng "Hoạt động gần đây".
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
                lblLastScanDate.Text = "Chưa có phiên quét nào";
                lblLastScanType.Text = "—";
            }
            else
            {
                lblLastScanDate.Text = e.Time.ToString("dd/MM/yyyy  HH:mm");
                lblLastScanType.Text = e.Type;
                // "Tệp đã quét" cũng khôi phục theo phiên gần nhất (0 lúc mới mở app là dữ liệu mẫu)
                lblScannedCount.Text = e.Files.ToString("N0");
                lastScannedFiles = e.Files;
            }
        }

        /// <summary>Bảng "Hoạt động gần đây" — đọc trực tiếp scanhistory.log, mới nhất lên đầu.</summary>
        private void LoadActivity()
        {
            List<HistoryEntry> all = ScanHistoryStore.Entries();
            dgvActivity.Rows.Clear();
            int shown = 0;
            foreach (HistoryEntry h in all)
            {
                if (h == null) continue;
                if (shown >= MaxActivityRows) break;
                dgvActivity.Rows.Add(UiIcons.Dot(12, MauSuKien(h)), MoTaSuKien(h),
                    h.Time.ToString("dd/MM/yyyy HH:mm"));
                shown++;
            }
            lblActivitySummary.Text = shown == 0
                ? "Chưa có hoạt động nào được ghi trong scanhistory.log."
                : string.Format("{0} hoạt động gần nhất — mới nhất ở trên.", shown);
            lnkXemLichSu.Visible = shown > 0;
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
            if (currentView == TongQuanView.AnToan)
                picShield.Image = running ? UiIcons.ShieldOk(72) : UiIcons.AlertCircle(72);
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

        // ================= NÚT "QUÉT NÂNG CAO ▾" — DROPDOWN 4 CHẾ ĐỘ =================

        private void BtnQuetNangCao_Click(object sender, EventArgs e)
        {
            ctxQuetNangCao.Items.Clear();
            ctxQuetNangCao.ShowImageMargin = false;
            ThemMucQuetNangCao("Quét toàn bộ hệ thống", "Kiểm tra tất cả ổ đĩa và tệp", CheDoQuetNangCao.FullSystem);
            ThemMucQuetNangCao("Quét thư mục", "Chọn thư mục để quét", CheDoQuetNangCao.Folder);
            ThemMucQuetNangCao("Quét tệp", "Chọn một hoặc nhiều tệp để quét", CheDoQuetNangCao.Files);
            ThemMucQuetNangCao("Quét tùy chỉnh", "Cấu hình vị trí và loại tệp quét", CheDoQuetNangCao.Custom);
            ctxQuetNangCao.Show(btnQuetNangCao, new Point(0, btnQuetNangCao.Height + 2));
        }

        /// <summary>
        /// Mỗi mục menu là 1 host 2 dòng: icon chấm màu + tiêu đề đậm + mô tả xám
        /// (SPEC §3.1 mục 6) — dựng bằng control thật nên không phụ thuộc font biểu tượng.
        /// </summary>
        private void ThemMucQuetNangCao(string title, string subtitle, CheDoQuetNangCao mode)
        {
            var host = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Theme.PageBg,
                Padding = new Padding(10, 6, 14, 6),
                Margin = Padding.Empty,
                Size = new Size(320, 50)
            };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32F));
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            host.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var pic = new PictureBox
            {
                Image = UiIcons.Dot(14, mode == CheDoQuetNangCao.FullSystem ? Theme.Red : Theme.Blue),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            var lbl = new Label
            {
                Text = title, Font = Theme.BoldFont, ForeColor = Theme.TextDark,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty
            };
            var sub = new Label
            {
                Text = subtitle, Font = Theme.SmallFont, ForeColor = Theme.TextGray,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty
            };
            host.Controls.Add(pic, 0, 0);
            host.SetRowSpan(pic, 2);
            host.Controls.Add(lbl, 1, 0);
            host.Controls.Add(sub, 1, 1);

            EventHandler hover = delegate { host.BackColor = Theme.BlueTint; };
            EventHandler leave = delegate { host.BackColor = Theme.PageBg; };
            EventHandler click = delegate
            {
                ctxQuetNangCao.Close();
                MoQuetNangCao(mode);
            };
            foreach (System.Windows.Forms.Control c in new System.Windows.Forms.Control[] { host, pic, lbl, sub })
            {
                c.Cursor = Cursors.Hand;
                c.Click += click;
                c.MouseEnter += hover;
                c.MouseLeave += leave;
            }
            var item = new ToolStripControlHost(host)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false,
                Size = host.Size
            };
            ctxQuetNangCao.Items.Add(item);
        }
    }
}












