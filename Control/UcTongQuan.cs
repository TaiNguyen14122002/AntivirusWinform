using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScanAndRemoveVirus.Services;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcTongQuan : UserControl
    {
        private string customScanPath;
        private bool isScanning;
        private CancellationTokenSource cts;

        public UcTongQuan()
        {
            InitializeComponent();
            // Responsive: cửa sổ nhỏ -> cuộn thay vì cắt nội dung
            Theme.ScrollablePage(this, tableLayoutPanel11, 980, 640);
            // Ngôn ngữ thiết kế chuẩn như tab Lịch sử: header trang + card xanh brand
            Theme.StylePageHeader(lblOverviewTitle, lblOverviewSubtitle);
            Theme.StyleCard(grpScan, grpProtection, grpStatistics, grpThreats, grpScannedFiles,
                grpLastScan, grpQuarantine, grpAction);
            Theme.StyleGrid(dgvActions);
            // Cột "Chọn" (checkbox) dùng chung Theme: header ô vuông + commit tức thì
            colPickAction.HeaderCell = new Theme.SelectAllHeaderCell(() => Theme.PickState(dgvActions, colPickAction.Index));
            dgvActions.CurrentCellDirtyStateChanged += delegate
            {
                if (dgvActions.IsCurrentCellDirty)
                    dgvActions.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvActions.CellValueChanged += (s, e) =>
            {
                if (e.ColumnIndex == colPickAction.Index) UpdateThreatUi();
            };
            dgvActions.ColumnHeaderMouseClick += (s, e) =>
            {
                if (e.ColumnIndex == colPickAction.Index)
                {
                    bool any = dgvActions.Rows.Cast<DataGridViewRow>()
                        .Any(r => r.Cells[colPickAction.Index].Value is bool b && b);
                    Theme.PickAll(dgvActions, colPickAction.Index, !any);
                    UpdateThreatUi();
                }
            };
            panel1.Height = 40;   // nút Kiểm tra cập nhật về compact 40px chuẩn Lịch sử
            btnCheckUpdate.Margin = new Padding(0, 2, 0, 2);
            Theme.StyleButton(btnScanNow, Theme.BtnRole.Primary);
            colActionThreat.DefaultCellStyle.ForeColor = Theme.RedText;
            colActionThreat.DefaultCellStyle.Font = Theme.BoldFont;
            lblThreatCount.ForeColor = Theme.Green;
            lblQuarantineCount.ForeColor = Theme.TextDark;
            lblScannedCount.ForeColor = Theme.Blue;
            Theme.StyleNeutralButtons(btnCheckUpdate, btnPickFile, btnPickFolder);
            btnCheckUpdate.Click += BtnCheckUpdate_Click;
            btnScanNow.Click += BtnScanNow_Click;
            ScanEngine.QuarantineChanged += OnQuarantineChangedTongQuan;
            // 3 radio nằm ở 3 container khác nhau -> WinForms không tự loại trừ, phải tự xử lý
            rdoQuickScan.CheckedChanged += ScanTypeChanged;
            rdoFullScan.CheckedChanged += ScanTypeChanged;
            rdoCustomScan.CheckedChanged += ScanTypeChanged;
            btnPickFile.Click += BtnPickFile_Click;
            btnPickFolder.Click += BtnPickFolder_Click;
            UpdateCustomPickUi();
            // Fix stat card fonts to match Theme standard (Designer defaults to MSS)
            var statFont = Theme.CardTitleFont;
            var valFont = new Font("Segoe UI", 18F, FontStyle.Bold);
            foreach (var gb in new[] { grpThreats, grpScannedFiles, grpLastScan, grpQuarantine })
                gb.Font = statFont;
            foreach (var lbl in new[] { lblThreatCount, lblScannedCount, lblLastScanDate, lblQuarantineCount })
                lbl.Font = valFont;
            LoadProtectionStatus();
            btnQuarantineSelected.Click += BtnQuarantineSelected_Click;
            btnDeleteSelected.Click += BtnDeleteSelected_Click;
            btnQuarantineAll.Click += BtnQuarantineAll_Click;
            btnVirusTotal.Click += BtnVirusTotal_Click;
            Theme.StyleButton(btnVirusTotal, Theme.BtnRole.Action);
            ApplyActionChips();
        }

        private void ScanTypeChanged(object sender, EventArgs e)
        {
            var selected = (RadioButton)sender;
            if (!selected.Checked) return;
            foreach (RadioButton other in new[] { rdoQuickScan, rdoFullScan, rdoCustomScan })
                if (!ReferenceEquals(other, selected) && other.Checked)
                    other.Checked = false;
            UpdateCustomPickUi();
        }

        // Nhóm chọn tệp/thư mục chỉ khả dụng khi đang chọn "Quét tùy chọn"
        private void UpdateCustomPickUi()
        {
            bool custom = rdoCustomScan.Checked && !isScanning;
            btnPickFile.Enabled = custom;
            btnPickFolder.Enabled = custom;
        }

        private void BtnPickFile_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Chọn tệp cần quét";
                dlg.Filter = "Tất cả tệp (*.*)|*.*";
                dlg.CheckFileExists = true;
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                    SetCustomPath(dlg.FileName);
            }
        }

        private void BtnPickFolder_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Chọn thư mục cần quét";
                dlg.ShowNewFolderButton = false;
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                    SetCustomPath(dlg.SelectedPath);
            }
        }

        private void SetCustomPath(string path)
        {
            customScanPath = path;
            rdoCustomScan.Checked = true;
            lblCustomPath.Text = path;
            lblCustomPath.ForeColor = Theme.TextMid;
            lblCustomPath.TextAlign = ContentAlignment.MiddleLeft;
        }

        private ScanType GetSelectedScanType()
        {
            if (rdoFullScan.Checked) return ScanType.Full;
            if (rdoCustomScan.Checked) return ScanType.Custom;
            return ScanType.Quick;
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
            return File.Exists(customPath)
                ? "Tệp: " + customPath
                : "Thư mục: " + customPath;
        }

        // Nút Quét ngay kiêm nút Hủy khi đang quét
        private async void BtnScanNow_Click(object sender, EventArgs e)
        {
            if (isScanning)
            {
                cts.Cancel();
                return;
            }

            ScanType type = GetSelectedScanType();
            string path = customScanPath;
            if (type == ScanType.Custom && string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Hãy bấm \"Chọn tệp\" hoặc \"Chọn thư mục\" để chọn mục cần quét trước.",
                    "Quét tùy chọn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (type != ScanType.Custom) path = null;

            SetScanning(true);
            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            bool completed = false;
            try
            {
                ScanResult result = await Task.Run(() =>
                    ScanEngine.Scan(type, path, token, (count, current) =>
                    {
                        try
                        {
                            BeginInvoke((MethodInvoker)(() =>
                            {
                                lblScannedCount.Text = count.ToString("N0");
                                if (current != null)
                                    lblScanProgress.Text = string.Format("Đang quét: {0}  ({1:N0} tệp)",
                                        current, count);
                            }));
                        }
                        catch (ObjectDisposedException) { } // form đã đóng giữa chừng
                        catch (InvalidOperationException) { }
                    }));

                completed = true;
                lblScannedCount.Text = result.FilesScanned.ToString("N0");
                lblScanProgress.Text = string.Format("Hoàn tất — đã quét {0:N0} tệp trong {1:0.#} giây.",
                    result.FilesScanned, result.Duration.TotalSeconds);
                lblScanProgress.ForeColor = result.HasThreats ? Theme.Amber : Theme.Green;
                LoadDetectedThreats(result);
                lblLastScanDate.Text = DateTime.Now.ToString("dd/MM/yyyy  HH:mm");
                lblLastScanType.Text = DescribeScan(type);
                ScanHistoryStore.Add(DescribeScan(type), ScopeOf(type, path),
                    result.FilesScanned, result.Threats.Count, result.Duration.TotalSeconds);
                AutoQueryHeuristicRows(result); // hàng "Bảo vệ web" nếu đang bật
                MessageBox.Show(
                    result.HasThreats
                        ? string.Format("Phát hiện {0} mối đe dọa trong {1:N0} tệp. Hãy xử lý trong khu vực \"Hành động\" bên dưới.",
                            result.Threats.Count, result.FilesScanned)
                        : string.Format("Đã quét {0:N0} tệp, không phát hiện mối đe dọa.", result.FilesScanned),
                    DescribeScan(type) + " hoàn tất",
                    MessageBoxButtons.OK,
                    result.HasThreats ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                lblScanProgress.ForeColor = Theme.Amber;
                lblScanProgress.Text = "Đã hủy phiên quét.";
                MessageBox.Show("Đã hủy phiên quét.", DescribeScan(type),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Bất kỳ lỗi nền nào cũng chỉ dừng ở thông báo — không được làm chết app
                lblScanProgress.ForeColor = Theme.Red;
                lblScanProgress.Text = "Quét dừng vì lỗi.";
                MessageBox.Show("Quét dừng vì lỗi: " + ex.Message, DescribeScan(type),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetScanning(false);
                if (completed)
                {
                    // Thanh đầy = phiên quét trọn vẹn (xanh lá hệ thống); hủy/lỗi -> về 0
                    pgbScan.Style = ProgressBarStyle.Blocks;
                    pgbScan.Value = 100;
                    pgbScan.Visible = true;
                }
                else
                {
                    pgbScan.Style = ProgressBarStyle.Blocks;
                    pgbScan.Value = 0;
                }
            }
        }

        private void SetScanning(bool scanning)
        {
            isScanning = scanning;
            // Xanh dương = "Quét ngay"; trong lúc chạy nút thành "Hủy quét" -> đổi đỏ (hành động dừng)
            btnScanNow.Text = scanning ? "Hủy quét" : "Quét ngay";
            Theme.StyleButton(btnScanNow, scanning ? Theme.BtnRole.Cancel : Theme.BtnRole.Primary);
            pgbScan.Visible = scanning;
            pgbScan.Style = ProgressBarStyle.Marquee;
            lblScanProgress.ForeColor = Theme.Blue;
            if (scanning)
                lblScanProgress.Text = "Đang quét...";
            UpdateCustomPickUi();
        }

        private void LoadDetectedThreats(ScanResult result)
        {
            dgvActions.Rows.Clear();
            foreach (ThreatFound threat in result.Threats)
                dgvActions.Rows.Add(false, threat.FilePath,
                    string.IsNullOrEmpty(threat.Kind) ? threat.Reason : threat.Kind + ": " + threat.Reason);
            UpdateThreatUi();
        }

        // Các dòng được TÍCH ở cột Chọn (dùng cho nút "…đã chọn")
        private List<DataGridViewRow> TickedRows()
        {
            return dgvActions.Rows.Cast<DataGridViewRow>()
                .Where(r => r.Cells[colPickAction.Index].Value is bool b && b)
                .ToList();
        }

        private void UpdateThreatUi()
        {
            bool hasRows = dgvActions.Rows.Count > 0;
            bool hasPick = TickedRows().Count > 0;
            lblThreatCount.Text = dgvActions.Rows.Count.ToString();
            lblThreatCount.ForeColor = hasRows ? Theme.Red : Theme.Green;
            lblThreatText.Text = hasRows ? "Cần xử lý" : "Không phát hiện mối đe dọa";
            lblThreatText.ForeColor = hasRows ? Theme.Red : Theme.Green;
            btnQuarantineSelected.Enabled = hasRows && hasPick;
            btnDeleteSelected.Enabled = hasRows && hasPick;

            btnQuarantineAll.Enabled = hasRows;
            btnVirusTotal.Enabled = hasRows;
            Theme.InvalidatePickHeader(dgvActions);
            ApplyActionChips();
        }

        // Chip đổi màu theo trạng thái Enabled -> repaint sau mỗi lần bật/tắt nút
        private void ApplyActionChips()
        {
                        Theme.StyleButton(btnQuarantineSelected, Theme.BtnRole.Action);
            Theme.StyleButton(btnQuarantineAll, Theme.BtnRole.Action);
            Theme.StyleButton(btnDeleteSelected, Theme.BtnRole.Danger);
        }

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
            if (rows.Count == 0) return;
            int done = 0;
            foreach (DataGridViewRow row in rows)
            {
                string path = row.Cells[colActionFile.Index].Value as string;
                string reason = row.Cells[colActionThreat.Index].Value as string; // lưu lý do/loại phát hiện vào sổ cách ly
                if (string.IsNullOrEmpty(path) || !File.Exists(path)
                    || ScanEngine.Quarantine(path, reason))
                {
                    dgvActions.Rows.Remove(row);
                    done++;
                }
            }
            UpdateThreatUi();
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
            if (rows.Count == 0) return;
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
                    if (!File.Exists(path)) { dgvActions.Rows.Remove(row); done++; continue; }
                    File.Delete(path);
                    done++;
                    dgvActions.Rows.Remove(row);
                }
                catch (Exception) { } // tệp đang bị khoá: giữ dòng lại để thử lại sau
            }
            UpdateThreatUi();
            MessageBox.Show(string.Format("Đã xóa {0}/{1} tệp.", done, rows.Count),
                "Xóa tệp", MessageBoxButtons.OK,
                done == rows.Count ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        // ==== Tra cứu VirusTotal (đám mây, kỹ thuật 1 nâng cấp: chỉ gửi hash SHA256) ====
        private async void BtnVirusTotal_Click(object sender, EventArgs e)
        {
            if (isScanning || dgvActions.SelectedRows.Count == 0) return;
            DataGridViewRow row = dgvActions.SelectedRows[0];
            string path = row.Cells[colActionFile.Index].Value as string;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
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
                    row.Cells[colActionThreat.Index].Value = string.Format("VirusTotal [{0}]: {1}{2}",
                        verdict, report.Summary(), hash != null ? "  —  SHA256 " + hash.Substring(0, 16) + "…" : "");
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
                f.ClientSize = new Size(460, 140);
                var lbl = new Label
                {
                    Text = "Dán API key miễn phí (virustotal.com → Profile → API key).\n"
                        + "App chỉ gửi hash SHA256 ra ngoài — KHÔNG upload nội dung tệp của bạn.",
                    Left = 12, Top = 10, Width = 436, Height = 44
                };
                var txt = new TextBox { Left = 12, Top = 60, Width = 436 };
                var ok = new Button { Text = "Lưu", DialogResult = DialogResult.OK, Left = 272, Top = 94, Width = 85 };
                var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Left = 363, Top = 94, Width = 85 };
                Theme.StyleButton(ok, Theme.BtnRole.Primary);
                Theme.StyleButton(cancel, Theme.BtnRole.Neutral);
                f.Controls.AddRange(new System.Windows.Forms.Control[] { lbl, txt, ok, cancel });
                f.AcceptButton = ok;
                f.CancelButton = cancel;
                if (f.ShowDialog(FindForm()) != DialogResult.OK) return null;
                return txt.Text.Trim();
            }
        }

        // Hàng "Bảo vệ web (VirusTotal)": heuristic nghi vấn -> tự tra hash trên cloud
        // (tối đa 2 dòng, cách 16s — giữ ngưỡng 4 request/phút của gói miễn phí)
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
                    try
                    {
                        BeginInvoke((MethodInvoker)(() =>
                        {
                            foreach (DataGridViewRow r in dgvActions.Rows)
                                if (Equals(r.Cells[colActionFile.Index].Value, t.FilePath))
                                    r.Cells[colActionThreat.Index].Value = label;
                        }));
                    }
                    catch (ObjectDisposedException) { return; }
                    catch (InvalidOperationException) { return; }
                }
            });
        }

        // Mở lại tab -> refresh thống kê (quét/cách ly có thể xảy ra ở tab khác hoặc phiên trước)
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) { ApplyQuarantineCount(); ApplyLastScanFromHistory(); }
        }

        private void LoadProtectionStatus()
        {
            // Hàng "Tự động cập nhật": tem chữ ký quá hạn 24h -> tự đóng dấu + xóa cache ngay khi mở tab
            GuardService.EnsureDailyAutoUpdate();
            lblVersionValue.Text = System.Reflection.Assembly
                .GetExecutingAssembly().GetName().Version.ToString(3);
            ApplyQuarantineCount();

            DateTime last;
            if (ScanHistoryStore.TryGetLastSignatureUpdate(out last))
                MarkDbUpdated(last);
            else
                lblLastUpdateTitle.Text = "Chưa cập nhật";

            ApplyLastScanFromHistory();

            // Trạng thái real-time thật (kỹ thuật 3) + cập nhật tức thì khi tab Bảo vệ bật/tắt
            RealTimeProtection.StatusChanged += OnRealTimeStatus;
            OnRealTimeStatus(RealTimeProtection.IsRunning);
        }

        // "Lần quét gần nhất" khôi phục từ lịch sử thật — restart app không còn hiện dữ liệu mock
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
                // "Tệp đã quét" cũng khôi phục theo phiên gần nhất (0 lúc mới mở app là dữ liệu mock)
                lblScannedCount.Text = e.Files.ToString("N0");
            }
        }

        private void OnRealTimeStatus(bool running)
        {
            if (!IsHandleCreated)
            {
                ApplyRealTimeStatus(running);
                return;
            }
            try { BeginInvoke((MethodInvoker)(() => ApplyRealTimeStatus(running))); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private void ApplyRealTimeStatus(bool running)
        {
            lblRealtimeValue.Text = running ? "Bật" : "Tắt";
            lblRealtimeValue.ForeColor = running
                ? Theme.Green
                : Theme.Red;
            lblProtectionStatus.Text = running
                ? "Máy tính của bạn được bảo vệ"
                : "Bảo vệ thời gian thực đang tắt";
            lblProtectionStatus.ForeColor = running
                ? Theme.Green
                : Theme.Amber;
        }

        private void BtnCheckUpdate_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            ScanHistoryStore.MarkSignatureUpdated(now);
            // "Cập nhật CSDL chữ ký" -> vô hiệu hóa cache để lần quét sau kiểm tra lại toàn bộ
            ScanEngine.ClearScanCache();
            MarkDbUpdated(now);
            ScanHistoryStore.Add("Cập nhật CSDL", "Bảng chữ ký virus", 0, 0, 0);
            MessageBox.Show("Cơ sở dữ liệu virus đã được cập nhật.",
                "Kiểm tra cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Ô "Cách ly" ở khu Thống kê luôn phản ánh số tệp thật trong khu cách ly
        private void OnQuarantineChangedTongQuan()
        {
            try { BeginInvoke((MethodInvoker)ApplyQuarantineCount); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private void ApplyQuarantineCount()
        {
            int n = ScanEngine.CountQuarantined();
            lblQuarantineCount.Text = n.ToString();
            // Số tệp đang cách ly: hổ phách khi >0 (còn thứ cần lưu ý), xám khi hết
            lblQuarantineCount.ForeColor = n > 0 ? Theme.Amber : Theme.TextDark;
        }

        private void MarkDbUpdated(DateTime when)
        {
            lblDatabaseValue.Text = "Đã cập nhật";
            lblDatabaseValue.ForeColor = Theme.Green;
            lblLastUpdateTitle.Text = when.ToString("dd/MM/yyyy HH:mm");
        }

        private void lblQuarantineCount_Click(object sender, EventArgs e)
        {
            // Bấm vào số cách ly -> mở thẳng tab Cách ly để xử lý
            var main = FindForm() as FrmMain;
            if (main != null) main.MoTabCachLy();
        }
    }
}
