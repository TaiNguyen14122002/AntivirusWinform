// ============================================================================
//  Tab Lịch sử — giao diện dựng lại theo ảnh tham chiếu 26/09/2026 (README §3).
//  Bố cục nằm ở Control\UcLichSu.Designer.cs; file này chỉ lo: nạp dữ liệu THẬT
//  từ ScanHistoryStore (AppData\scanhistory.log), lọc ngày/loại quét, định dạng
//  ô, phân trang 10 dòng và xử lý nút "Xem" của từng dòng.
//  Không JSON giả lập, không hard-code dòng/ngày/số liệu của ảnh.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcLichSu : UserControl
    {
        // ===== Hằng số nghiệp vụ (README §3.2–§3.4) =====
        const int PageSize = 10;                                   // mỗi trang 10 phiên quét như ảnh
        const string TatCa = "Tất cả";                             // "Tất cả" = không giới hạn loại quét
        const string LoaiCapNhat = "Cập nhật CSDL";                // nhật ký cập nhật chữ ký (vẫn giữ trong log)
        const string LoaiThoiGianThuc = "Bảo vệ thời gian thực";   // cảnh báo thời gian thực
        const int IconSize = 16;
        static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
        // 3 gợi ý của ảnh; loại quét khác có thật trong log sẽ được bổ sung khi nạp (§3.2)
        static readonly string[] LoaiGoiY = { "Quét toàn bộ", "Quét nhanh", "Quét tệp" };

        // 3 "mục" xem, thay cho 3 tab con đã bỏ khỏi bố cục chính — chuyển bằng menu phụ "…"
        enum KieuXem { PhienQuet, CanhBao, CapNhat }

        KieuXem kieuXem = KieuXem.PhienQuet;
        bool cuTruoc;                                  // false = mới nhất trước (mặc định), true = cũ → mới
        int trang = 1;                                 // trang đang xem (1-based; 0 = không có kết quả)
        bool daLayKhoangNgay;                          // khoảng ngày mặc định chỉ lấy từ dữ liệu ở lần nạp đầu
        List<HistoryEntry> tatCa = new List<HistoryEntry>();     // toàn bộ log, mới nhất trước
        List<HistoryEntry> dangXem = new List<HistoryEntry>();   // đã lọc + sắp xếp
        List<ScanEngine.QuarantinedItem> soCachLy = new List<ScanEngine.QuarantinedItem>(); // bằng chứng cách ly thật

        public UcLichSu()
        {
            InitializeComponent();
            BackColor = Theme.PageBg;
            // Responsive: cửa sổ nhỏ -> cuộn thay vì cắt chữ/cắt nút (§3.6)
            Theme.ScrollablePage(this, tlpPage, 980, 620);
            Theme.StyleHistoryHeader(lblHistoryTitle, lblHistorySubtitle);
            Theme.StyleFilterLabel(lblTuNgay);
            Theme.StyleFilterLabel(lblDenNgay);
            Theme.StyleFilterLabel(lblLoaiQuet);
            Theme.StyleFilterInput(dtpTuNgay);
            Theme.StyleFilterInput(dtpDenNgay);
            Theme.StyleFilterInput(cboLoaiQuet);

            // ===== Bảng: 7 cột, header 48px, hàng 48px, không sọc xen kẽ (§3.3) =====
            Theme.StyleHistoryGrid(dgvHistory);
            Theme.StyleHistoryGridButton(colView);
            dgvHistory.DefaultCellStyle.SelectionBackColor = Theme.BlueFaint; // vẫn nhận ra dòng đang chọn
            dgvHistory.DefaultCellStyle.SelectionForeColor = Theme.TextDark;
            dgvHistory.AlternatingRowsDefaultCellStyle.SelectionBackColor = Theme.BlueFaint;
            dgvHistory.AlternatingRowsDefaultCellStyle.SelectionForeColor = Theme.TextDark;
            colScanTime.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            colScanTime.DefaultCellStyle.FormatProvider = Vi;
            // HeaderCell riêng giữ mũi tên chiều sắp xếp -> phải gán lại HeaderText/ToolTipText sau khi thay cell
            colScanTime.HeaderCell = new SortHeaderCell(this);
            colScanTime.HeaderText = "Thời gian";
            colScanTime.ToolTipText = "Nhấp để đổi chiều sắp xếp theo thời gian";
            colFileCount.DefaultCellStyle.Format = "N0";          // 125.430 theo vi-VN (§3.3)
            colFileCount.DefaultCellStyle.FormatProvider = Vi;
            colFileCount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colThreatCount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colDuration.DefaultCellStyle.Format = @"hh\:mm\:ss";  // thời lượng quét dạng hh:mm:ss
            colDuration.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            cboLoaiQuet.DropDownWidth = 240;                      // tên loại quét dài không bị cắt

            // ===== Nút (hình hài chung của Theme — Tests\ButtonAudit kiểm tra) =====
            Theme.StyleButton(btnLoc, Theme.BtnRole.Secondary);
            Theme.StyleButton(btnMore, Theme.BtnRole.Secondary);
            Theme.StyleButton(btnPrev, Theme.BtnRole.Secondary);
            Theme.StyleButton(btnNext, Theme.BtnRole.Secondary);
            btnLoc.Image = UiIcons.Search(IconSize, Theme.Blue);
            btnLoc.ImageAlign = ContentAlignment.MiddleLeft;
            btnLoc.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnMore.Image = UiIcons.More(18, Theme.TextMid);
            RefreshPagerIcons();

            // ===== Sự kiện (Designer không gắn sự kiện nào — hành vi gom hết về đây) =====
            btnLoc.Click += BtnLoc_Click;
            dtpTuNgay.KeyDown += FilterKeyDown;
            dtpDenNgay.KeyDown += FilterKeyDown;
            cboLoaiQuet.KeyDown += FilterKeyDown;
            btnMore.Click += BtnMore_Click;
            btnPrev.Click += delegate { GoPage(trang - 1); };
            btnNext.Click += delegate { GoPage(trang + 1); };
            dgvHistory.CellFormatting += Grid_CellFormatting;
            dgvHistory.CellPainting += Grid_CellPainting;
            dgvHistory.CellContentClick += Grid_CellContentClick;
            dgvHistory.CellDoubleClick += Grid_CellDoubleClick;
            dgvHistory.ColumnHeaderMouseClick += Grid_HeaderClick;
            dgvHistory.MouseMove += Grid_MouseMove;
            dgvHistory.Resize += delegate { PositionEmptyLabel(); };
            miXemChiTiet.Click += delegate { OpenDetail(SelectedEntry()); };
            miLamMoi.Click += delegate { LoadRealData(); };
            miXuatCsv.Click += MiXuatCsv_Click;
            miXoaDong.Click += MiXoaDong_Click;
            miXemPhienQuet.Click += delegate { SetKieuXem(KieuXem.PhienQuet); };
            miXemCanhBao.Click += delegate { SetKieuXem(KieuXem.CanhBao); };
            miXemCapNhat.Click += delegate { SetKieuXem(KieuXem.CapNhat); };
            menuLichSu.Opening += MiOpening;

            LoadRealData();
        }

        // ================= NẠP DỮ LIỆU THẬT =================
        // Tab tự nạp khi mở và tự làm mới mỗi lần chuyển vào (giữ hành vi cũ, §3.5)
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) LoadRealData();
        }

        private void LoadRealData()
        {
            tatCa = ScanHistoryStore.Entries();        // log thật, đã là "mới nhất trước"
            soCachLy = ScanEngine.ListQuarantined();   // bằng chứng cách ly để tô "Đã cách ly" (§3.3)
            if (!daLayKhoangNgay) LayKhoangNgayMacDinh();
            BuildTypeItems();
            Rebuild();
            Render();
        }

        // Khoảng ngày mặc định lấy TỪ DỮ LIỆU THẬT (không gán ngày mẫu của ảnh, §3.2)
        private void LayKhoangNgayMacDinh()
        {
            DateTime tu = DateTime.Today.AddDays(-30);
            DateTime den = DateTime.Today;
            if (tatCa.Count > 0)
            {
                tu = DateTime.MaxValue;
                den = DateTime.MinValue;
                foreach (HistoryEntry e in tatCa)
                {
                    if (e.Time < tu) tu = e.Time;
                    if (e.Time > den) den = e.Time;
                }
                tu = tu.Date;
                den = den.Date < DateTime.Today ? DateTime.Today : den.Date; // hôm nay luôn nằm trong khoảng
            }
            GanNgay(dtpTuNgay, tu);
            GanNgay(dtpDenNgay, den);
            daLayKhoangNgay = true;
        }

        private static void GanNgay(DateTimePicker dtp, DateTime d)
        {
            if (d < dtp.MinDate) d = dtp.MinDate;
            if (d > dtp.MaxDate) d = dtp.MaxDate;
            dtp.Value = d;
        }

        // Combo "Loại quét": 3 gợi ý của ảnh + MỌI loại thật có trong log (không làm mất phiên quét, §3.2)
        private void BuildTypeItems()
        {
            string dangChon = cboLoaiQuet.SelectedItem as string;
            var items = new List<string> { TatCa };
            foreach (string s in LoaiGoiY) items.Add(s);
            foreach (HistoryEntry e in tatCa)
                if (InKind(e) && !string.IsNullOrEmpty(e.Type) && !items.Contains(e.Type))
                    items.Add(e.Type);
            cboLoaiQuet.Items.Clear();
            cboLoaiQuet.Items.AddRange(items.ToArray());
            int ix = string.IsNullOrEmpty(dangChon) ? -1 : items.IndexOf(dangChon);
            cboLoaiQuet.SelectedIndex = ix < 0 ? 0 : ix;   // giữ lựa chọn cũ khi vẫn còn hợp lệ
        }

        // ================= LỌC VÀ SẮP XẾP =================
        private void Rebuild()
        {
            // Sắp theo DateTime thật (không theo chuỗi hiển thị "dd/MM/yyyy HH:mm")
            dangXem = tatCa.Where(MatchFilter).OrderByDescending(e => e.Time).ToList();
            if (cuTruoc) dangXem.Reverse();
        }

        // "Mục" đang xem: phiên quét / cảnh báo thời gian thực / nhật ký cập nhật CSDL (§3.3)
        private bool InKind(HistoryEntry e)
        {
            if (e.Type == LoaiCapNhat) return kieuXem == KieuXem.CapNhat;
            bool laCanhBao = e.Type == LoaiThoiGianThuc;
            return laCanhBao == (kieuXem == KieuXem.CanhBao);
        }

        private bool MatchFilter(HistoryEntry e)
        {
            if (!InKind(e)) return false;
            // Lọc NGÀY: bao gồm trọn ngày kết thúc (đến trước 00:00 ngày kế tiếp, §3.2)
            if (e.Time < dtpTuNgay.Value.Date) return false;
            if (e.Time >= dtpDenNgay.Value.Date.AddDays(1)) return false;
            string loai = cboLoaiQuet.SelectedItem as string;
            if (!string.IsNullOrEmpty(loai) && loai != TatCa && e.Type != loai) return false;
            return true;
        }

        // Đơn vị đếm của chân trang đổi theo "mục" đang xem (câu chữ của ảnh: "lần quét")
        private string DonViDem
        {
            get
            {
                if (kieuXem == KieuXem.CanhBao) return "cảnh báo";
                if (kieuXem == KieuXem.CapNhat) return "lần cập nhật CSDL";
                return "lần quét";
            }
        }

        // ================= VẼ BẢNG + CHÂN TRANG =================
        private int SoTrang()
        {
            return dangXem.Count == 0 ? 0 : (dangXem.Count + PageSize - 1) / PageSize;
        }

        private void Render()
        {
            int soTrang = SoTrang();
            if (soTrang == 0) trang = 0;
            else if (trang < 1) trang = 1;
            else if (trang > soTrang) trang = soTrang;   // nạp lại dữ liệu -> tự về trang cuối hợp lệ (§3.4)

            dgvHistory.Rows.Clear();
            if (soTrang > 0)
            {
                int dau = (trang - 1) * PageSize;
                int cuoi = Math.Min(dau + PageSize, dangXem.Count);
                for (int i = dau; i < cuoi; i++) AddRow(dangXem[i]);
            }

            lblTotal.Text = "Tổng cộng: " + dangXem.Count.ToString("N0", Vi) + " " + DonViDem;
            lblPage.Text = trang + " / " + soTrang;       // không có kết quả -> "0 / 0" như ảnh
            btnPrev.Enabled = soTrang > 0 && trang > 1;
            btnNext.Enabled = soTrang > 0 && trang < soTrang;
            RefreshPagerIcons();
            lblEmpty.Visible = soTrang == 0;
            if (lblEmpty.Visible)
            {
                PositionEmptyLabel();
                lblEmpty.BringToFront();
            }
            UpdateMenuChecks();
        }

        private void AddRow(HistoryEntry e)
        {
            bool capNhat = e.Type == LoaiCapNhat;
            object thoiLuong = (capNhat || e.Seconds <= 0) ? "—" : (object)TimeSpan.FromSeconds(e.Seconds);
            int ix = dgvHistory.Rows.Add(
                e.Time,                // colScanTime   (format dd/MM/yyyy HH:mm)
                e.Type,                // colScanType   (+ icon vẽ ở CellPainting)
                KetQuaCua(e),          // colResult     (xanh/đỏ/cam theo dữ liệu thật)
                e.Files,               // colFileCount  (N0)
                e.Threats,             // colThreatCount
                thoiLuong,             // colDuration   (hh:mm:ss)
                null);                 // colView       (nút "Xem" vẽ tay)
            dgvHistory.Rows[ix].Tag = e;   // KHÓA ĐỊNH DANH: tham chiếu bản ghi gốc (§3.5)
        }

        // Kết quả THẬT của phiên: "Đã cách ly" chỉ khi sổ cách ly thật có bản ghi khớp (§3.3)
        private string KetQuaCua(HistoryEntry e)
        {
            if (e.Type == LoaiCapNhat) return "Thành công";
            if (e.Threats <= 0) return "Không phát hiện";
            return CoBangChungCachLy(e) ? "Đã cách ly" : "Phát hiện mối đe dọa";
        }

        private bool CoBangChungCachLy(HistoryEntry e)
        {
            if (soCachLy.Count == 0) return false;
            DateTime tu = e.Time.AddSeconds(-5);
            DateTime den = e.Time.AddSeconds(Math.Max(1.0, e.Seconds) + 60);
            foreach (ScanEngine.QuarantinedItem it in soCachLy)
                if (it.DetectedTime >= tu && it.DetectedTime <= den) return true;
            return false;
        }

        private void RefreshPagerIcons()
        {
            Color mo = btnPrev.Enabled ? Theme.BlueDark : Theme.ChipGrayText;
            Color mo2 = btnNext.Enabled ? Theme.BlueDark : Theme.ChipGrayText;
            btnPrev.Image = UiIcons.ArrowLeft(IconSize, mo);
            btnNext.Image = UiIcons.ArrowRight(IconSize, mo2);
        }

        private void PositionEmptyLabel()
        {
            lblEmpty.Bounds = new Rectangle(0, dgvHistory.ColumnHeadersHeight,
                Math.Max(1, dgvHistory.ClientSize.Width), lblEmpty.Height);
        }

        private void GoPage(int dich)
        {
            int soTrang = SoTrang();
            if (soTrang == 0) return;
            int t = Math.Max(1, Math.Min(soTrang, dich));   // kẹp trong [1, số trang] (§3.4)
            if (t == trang) return;
            trang = t;
            Render();
        }

        // ================= ĐỊNH DẠNG + VẼ Ô =================
        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            if (e.ColumnIndex == colResult.Index)
            {
                // Xanh = sạch, cam = đã cách ly, đỏ = còn mối đe dọa (Theme.PaintStatusCell)
                Theme.PaintStatusCell(e, Convert.ToString(e.Value, Vi));
            }
            else if (e.ColumnIndex == colThreatCount.Index)
            {
                int n;
                if (int.TryParse(Convert.ToString(e.Value, CultureInfo.InvariantCulture), out n) && n > 0)
                {
                    e.CellStyle.ForeColor = Theme.Red;      // 0 giữ màu xám mặc định, > 0 đỏ đậm
                    e.CellStyle.Font = Theme.BoldFont;
                    e.CellStyle.SelectionForeColor = Theme.Red;
                }
            }
        }

        // Icon loại quét + nút "Xem": vẽ tay bằng GDI+ (không dùng thư viện ngoài, §3.6)
        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.ColumnIndex == colScanType.Index)
            {
                e.Paint(e.ClipBounds, e.PaintParts
                    & ~DataGridViewPaintParts.ContentForeground & ~DataGridViewPaintParts.Focus);
                string loai = Convert.ToString(e.Value, Vi);
                int x = e.CellBounds.X + 12;
                Image icon = IconCuaLoai(loai);
                if (icon != null)
                {
                    e.Graphics.DrawImage(icon, x, e.CellBounds.Y + (e.CellBounds.Height - IconSize) / 2, IconSize, IconSize);
                    x += IconSize + 8;
                }
                Rectangle text = new Rectangle(x, e.CellBounds.Y,
                    Math.Max(1, e.CellBounds.Right - x - 8), e.CellBounds.Height);
                TextRenderer.DrawText(e.Graphics, loai, e.CellStyle.Font, text, e.CellStyle.ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                e.Handled = true;
            }
            else if (e.ColumnIndex == colView.Index)
            {
                e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border
                    | DataGridViewPaintParts.SelectionBackground);
                Rectangle nut = new Rectangle(e.CellBounds.X + (e.CellBounds.Width - 68) / 2,
                    e.CellBounds.Y + (e.CellBounds.Height - 30) / 2, 68, 30);
                using (GraphicsPath path = Theme.RoundedPath(nut, 6))
                {
                    using (var nen = new SolidBrush(Theme.ChipGray)) e.Graphics.FillPath(nen, path);
                    using (var vien = new Pen(Theme.BlueSoft)) e.Graphics.DrawPath(vien, path);
                }
                TextRenderer.DrawText(e.Graphics, colView.Text, Theme.BoldFont, nut, Theme.BlueDark,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
        }

        // Icon theo loại quét THẬT trong log (loại lạ -> biểu tượng trung tính, không bịa nhãn)
        private static Image IconCuaLoai(string loai)
        {
            if (string.IsNullOrEmpty(loai)) return UiIcons.Shield(IconSize, Theme.TextGray);
            if (loai == "Quét toàn bộ") return UiIcons.Desktop(IconSize, Theme.Blue);
            if (loai == "Quét nhanh") return UiIcons.Bolt(IconSize, Theme.Amber);
            if (loai.StartsWith("Quét tệp", StringComparison.Ordinal)) return UiIcons.Doc(IconSize, Theme.BlueDark);
            if (loai.StartsWith("Quét thư mục", StringComparison.Ordinal)) return UiIcons.Folder(IconSize, Theme.Amber);
            if (loai.StartsWith("Quét", StringComparison.Ordinal)) return UiIcons.Search(IconSize, Theme.Blue);
            return UiIcons.Shield(IconSize, Theme.BlueDark);   // Bảo vệ thời gian thực / Cập nhật CSDL…
        }

        private void InvalidateHeaderIcons()
        {
            dgvHistory.Invalidate(new Rectangle(0, 0, dgvHistory.Width, dgvHistory.ColumnHeadersHeight));
        }

        // ================= SỰ KIỆN =================
        // Nhấp header "Thời gian" -> đảo chiều sắp xếp (mũi tên trên header đổi theo)
        private void Grid_HeaderClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex >= 0 && e.ColumnIndex == colScanTime.Index) ToggleSort();
        }

        // Sắp xếp nằm ở header "Thời gian" (không có nút riêng) — test tự động gọi qua reflection
        private void ToggleSort()
        {
            cuTruoc = !cuTruoc;
            trang = 1;
            Rebuild();
            Render();
            InvalidateHeaderIcons();
        }

        // Con trỏ bàn tay ở nút "Xem" của dòng và ở header "Thời gian" (bấm được)
        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            DataGridView.HitTestInfo hit = dgvHistory.HitTest(e.X, e.Y);
            bool bamDuoc = (hit.Type == DataGridViewHitTestType.Cell && hit.ColumnIndex == colView.Index)
                || (hit.Type == DataGridViewHitTestType.ColumnHeader && hit.ColumnIndex == colScanTime.Index);
            dgvHistory.Cursor = bamDuoc ? Cursors.Hand : Cursors.Default;
        }

        // Nút "Xem" từng dòng: mở ĐÚNG phiên quét của dòng đó (định danh qua Tag)
        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colView.Index) return;
            OpenDetail(EntryOfRow(e.RowIndex));
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) OpenDetail(EntryOfRow(e.RowIndex));
        }

        private HistoryEntry EntryOfRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvHistory.Rows.Count) return null;
            return dgvHistory.Rows[rowIndex].Tag as HistoryEntry;
        }

        private HistoryEntry SelectedEntry()
        {
            if (dgvHistory.CurrentRow != null)
            {
                HistoryEntry e = dgvHistory.CurrentRow.Tag as HistoryEntry;
                if (e != null) return e;
            }
            if (dgvHistory.SelectedRows.Count > 0) return dgvHistory.SelectedRows[0].Tag as HistoryEntry;
            return null;
        }

        // ================= MENU PHỤ "…" (và menu chuột phải trên bảng) =================
        private void BtnMore_Click(object sender, EventArgs e)
        {
            menuLichSu.Show(btnMore, new Point(0, btnMore.Height + 4));
        }

        private void MiOpening(object sender, EventArgs e)
        {
            bool co = SelectedEntry() != null;
            miXemChiTiet.Enabled = co;   // mục theo DÒNG chỉ bật khi có dòng đang chọn
            miXoaDong.Enabled = co;
            UpdateMenuChecks();
        }

        private void UpdateMenuChecks()
        {
            miXemPhienQuet.Checked = kieuXem == KieuXem.PhienQuet;
            miXemCanhBao.Checked = kieuXem == KieuXem.CanhBao;
            miXemCapNhat.Checked = kieuXem == KieuXem.CapNhat;
        }

        // Đổi "mục" xem: phiên quét / cảnh báo thời gian thực / nhật ký cập nhật CSDL
        private void SetKieuXem(KieuXem k)
        {
            kieuXem = k;
            cuTruoc = false;
            trang = 1;
            BuildTypeItems();
            Rebuild();
            Render();
            InvalidateHeaderIcons();
        }

        private void BtnLoc_Click(object sender, EventArgs e)
        {
            if (dtpTuNgay.Value.Date > dtpDenNgay.Value.Date)
            {
                // Bộ lọc sai: báo ngắn và KHÔNG áp dụng (giữ nguyên bảng đang xem, §3.2)
                MessageBox.Show("Từ ngày không được sau Đến ngày. Bộ lọc chưa được áp dụng.",
                    "Bộ lọc lịch sử", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            trang = 1;               // đổi bộ lọc -> quay về trang 1 (§3.4)
            Rebuild();
            Render();
            InvalidateHeaderIcons();
        }

        private void FilterKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)   // Enter trong vùng lọc = nhấn Lọc (§3.2)
            {
                e.SuppressKeyPress = true;
                BtnLoc_Click(btnLoc, EventArgs.Empty);
            }
        }

        // Xóa đúng dòng đang chọn khỏi lịch sử (giữ nghiệp vụ cũ, giờ nằm trong menu phụ)
        private void MiXoaDong_Click(object sender, EventArgs e)
        {
            HistoryEntry entry = SelectedEntry();
            if (entry == null)
            {
                MessageBox.Show("Hãy chọn một dòng lịch sử (bấm vào dòng, hoặc nút Xem ở dòng đó).",
                    "Xóa dòng lịch sử", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DialogResult traLoi = MessageBox.Show(string.Format(
                "Xóa dòng lịch sử \"{0}\" lúc {1:dd/MM/yyyy HH:mm}?\r\n\r\nHành động này không thể hoàn tác.",
                entry.Type, entry.Time),
                "Xóa dòng lịch sử", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (traLoi != DialogResult.Yes) return;
            if (!ScanHistoryStore.Remove(entry))
            {
                MessageBox.Show("Không xóa được dòng này (bản ghi đã thay đổi). Hãy chọn Làm mới dữ liệu rồi thử lại.",
                    "Xóa dòng lịch sử", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            LoadRealData();   // nạp lại: giữ nguyên bộ lọc, tự về trang cuối hợp lệ (§3.4)
        }

        // Xuất CSV TOÀN BỘ lịch sử (giữ nguyên nghiệp vụ cũ; không thêm nút vào hàng bộ lọc, §3.5)
        private void MiXuatCsv_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Xuất báo cáo lịch sử quét";
                dlg.Filter = "Tệp CSV (*.csv)|*.csv";
                dlg.FileName = "lich-su-quet-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".csv";
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    ScanHistoryStore.ExportCsv(dlg.FileName);
                    MessageBox.Show("Đã xuất báo cáo:\r\n" + dlg.FileName, "Xuất báo cáo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không xuất được báo cáo: " + ex.Message, "Xuất báo cáo",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Chi tiết phiên quét: dùng lại hộp thoại chi tiết hiện có, số liệu 100% từ bản ghi gốc (§3.5)
        private void OpenDetail(HistoryEntry entry)
        {
            if (entry == null)
            {
                MessageBox.Show("Hãy chọn một dòng lịch sử (hoặc bấm nút Xem ở dòng cần xem).",
                    "Chi tiết lịch sử", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            MessageBox.Show(string.Format(Vi,
                "Thời gian: {0:dd/MM/yyyy HH:mm:ss}\r\nLoại: {1}\r\nPhạm vi: {2}\r\n"
                + "Số tệp đã quét: {3:N0}\r\nSố mối đe dọa: {4:N0}\r\nKết quả: {5}\r\nThời lượng: {6}\r\n\r\n"
                + "Chi tiết từng tệp bị phát hiện nằm ở tab Cách ly (mục đã cách ly) và bảng mối đe dọa ở tab Tổng quan.",
                entry.Time, entry.Type,
                string.IsNullOrEmpty(entry.Scope) ? "—" : entry.Scope,
                entry.Files, entry.Threats, KetQuaCua(entry),
                entry.Seconds > 0 ? FormatSeconds(entry.Seconds) : "—"),
                "Chi tiết lịch sử", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string FormatSeconds(double seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:00}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds);
        }

        // ================= HEADER "THỜI GIAN" + MŨI TÊN CHIỀU SẮP XẾP =================
        // Không có nút sắp xếp riêng: chiều sắp xếp nằm ngay trên header "Thời gian" (ảnh 26/09/2026).
        private sealed class SortHeaderCell : DataGridViewColumnHeaderCell
        {
            static readonly Font GlyphFont = new Font("Segoe MDL2 Assets", 9.75F, FontStyle.Bold, GraphicsUnit.Point);
            const string ArrowUp = "\uE70E";     // ChevronUp: đang xếp cũ → mới
            const string ArrowDown = "\uE70D";   // ChevronDown: đang xếp mới → cũ
            readonly UcLichSu owner;

            public SortHeaderCell(UcLichSu owner)
            {
                this.owner = owner;
            }

            protected override void Paint(Graphics g, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
                DataGridViewElementStates cellState, object value, object formattedValue, string errorText,
                DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle,
                DataGridViewPaintParts paintParts)
            {
                base.Paint(g, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText,
                    cellStyle, advancedBorderStyle,
                    paintParts & ~(DataGridViewPaintParts.ContentForeground | DataGridViewPaintParts.Focus));
                if (rowIndex != -1) return;   // chỉ header cột (không vẽ cho ô chọn cột/hàng)
                DataGridViewCellStyle hs = owner.dgvHistory.ColumnHeadersDefaultCellStyle;
                string text = OwningColumn != null ? OwningColumn.HeaderText : null;
                if (string.IsNullOrEmpty(text)) text = Convert.ToString(formattedValue, Vi);
                Size ts = TextRenderer.MeasureText(text, hs.Font);
                int y = cellBounds.Y + (cellBounds.Height - ts.Height) / 2;
                TextRenderer.DrawText(g, text, hs.Font, new Point(cellBounds.X + 10, y), hs.ForeColor);
                TextRenderer.DrawText(g, owner.cuTruoc ? ArrowUp : ArrowDown, GlyphFont,
                    new Rectangle(cellBounds.X + 14 + ts.Width, cellBounds.Y, 26, cellBounds.Height),
                    Theme.Blue, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }
    }
}
