using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Database;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcBaoVe
    {
        internal static bool ExactFlag(string key) { return IsOn(key); }
        internal void ExactSetFlag(string key, bool enabled)
        {
            if (key == "vtq" && enabled && !VirusTotalClient.IsConfigured) return;
            SetState(key, enabled); SyncRealFeaturesUi(); SyncProtectionStats();
        }
        internal string ExactSignature { get { return lblUpdateDateValue.Text; } }
        internal void ExactRefresh() { SyncRealFeaturesUi(); SyncProtectionStats(); }
    }
    public partial class UcCaiDat
    {
        internal void ExactSettings(Sketch20Model model)
        {
            model.Flags["autostart"] = chkAutoStart.Checked; model.Flags["samples"] = chkSendSamples.Checked;
            model.DataFolder = DataDir.Resolve("");
        }
        internal void ExactSetGeneral(string key, bool enabled)
        {
            if (key == "autostart") chkAutoStart.Checked = enabled;
            if (key == "samples") chkSendSamples.Checked = enabled;
        }
        internal void ExactSettingsCommand(string command, string key = null)
        {
            if (command == "save-settings") BtnSaveSettings_Click(null, EventArgs.Empty);
            else if (command == "defaults") BtnResetDefaults_Click(null, EventArgs.Empty);
            else if (command == "open-data") BtnOpenData_Click(null, EventArgs.Empty);
            else if (command == "clear-cache") BtnClearCache_Click(null, EventArgs.Empty);
            else if (command == "samples") BtnSamples_Click(null, EventArgs.Empty);
            else if (command == "clear-key") { BtnClearVtKey_Click(null, EventArgs.Empty); FeatureFlags.VtAutoQuery = false; FeatureFlags.Persist(); FeatureFlags.NotifyChanged(); }
            else if (command == "save-key") { txtVtKey.Text = key ?? ""; BtnSaveVtKey_Click(null, EventArgs.Empty); }
        }
    }
    public partial class UcLichSu
    {
        internal void ExactHistory(Sketch20Model model)
        {
            model.HistoryPage = lblPage.Text; model.HistoryTotal = dangXem.Count;
            model.HistoryPrev = btnPrev.Enabled; model.HistoryNext = btnNext.Enabled;
            model.HistoryFrom = dtpTuNgay.Value.Date; model.HistoryTo = dtpDenNgay.Value.Date;
            model.HistoryFilter = cboLoaiQuet.SelectedItem as string ?? "Tất cả";
            for (int i = 0; i < dgvHistory.Rows.Count; i++)
            {
                var row = dgvHistory.Rows[i]; var source = row.Tag as HistoryEntry; if (source == null) continue;
                model.History.Add(new Sketch20History { Index = i, Date = source.Time.ToString("dd/MM/yyyy"), Time = source.Time.ToString("HH:mm"),
                    Type = source.Type, Scope = source.Scope, Result = source.Threats == 0 ? "An toàn" : KetQuaCua(source), Files = source.Files.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")),
                    Threats = source.Threats.ToString(), Duration = source.Seconds > 0 ? FormatSeconds(source.Seconds) : "—", Risk = source.Threats > 0 });
            }
        }
        internal void ExactHistoryFilter(DateTime start, DateTime end, string type)
        {
            if (start.Date > end.Date)
            {
                AppDialog.Show("Từ ngày không được sau Đến ngày. Bộ lọc chưa được áp dụng.",
                    "Bộ lọc lịch sử", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GanNgay(dtpTuNgay, start.Date);
            GanNgay(dtpDenNgay, end.Date);
            string requested = string.IsNullOrWhiteSpace(type) ? "Tất cả" : type.Trim();
            int found = -1;
            for (int i = 0; i < cboLoaiQuet.Items.Count; i++)
                if (string.Equals(Convert.ToString(cboLoaiQuet.Items[i]), requested, StringComparison.OrdinalIgnoreCase))
                { found = i; break; }
            if (found < 0)
                for (int i = 0; i < cboLoaiQuet.Items.Count; i++)
                {
                    string item = Convert.ToString(cboLoaiQuet.Items[i]);
                    if (HistoryTypeMatches(item, requested)) { found = i; break; }
                }
            cboLoaiQuet.SelectedIndex = found >= 0 ? found : 0;
            BtnLoc_Click(null, EventArgs.Empty);
        }
        internal void ExactHistorySort(bool oldestFirst)
        {
            cuTruoc = oldestFirst;
            trang = 1;
            Rebuild();
            Render();
            InvalidateHeaderIcons();
        }
        internal void ExactHistoryCommand(string command)
        {
            if (command == "export") MiXuatCsv_Click(null, EventArgs.Empty);
            else if (command == "history-prev") GoPage(trang - 1);
            else if (command == "history-next") GoPage(trang + 1);
            else if (command == "history-refresh") LoadRealData();
            else if (command != null && command.StartsWith("history-delete:", StringComparison.Ordinal))
            {
                int index;
                if (!int.TryParse(command.Substring("history-delete:".Length), out index)) return;
                HistoryEntry entry = ExactHistoryEntry(index);
                if (entry == null) return;
                DialogResult answer = AppDialog.Show(string.Format("Xóa nhật ký \"{0}\" lúc {1:dd/MM/yyyy HH:mm}?\r\n\r\nHành động này không thể hoàn tác.", entry.Type, entry.Time), "Xóa nhật ký hoạt động", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer != DialogResult.Yes) return;
                if (!ScanHistoryStore.Remove(entry))
                {
                    LoadRealData();
                    AppDialog.Show("Bản ghi đã thay đổi hoặc không thể xóa. Nhật ký đã được làm mới tự động.", "Xóa nhật ký hoạt động", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                LoadRealData();
            }
        }
        internal HistoryEntry ExactHistoryEntry(int index)
        { return index >= 0 && index < dgvHistory.Rows.Count ? dgvHistory.Rows[index].Tag as HistoryEntry : null; }
    }
    public partial class UcCachLy
    {
        internal void ExactQuarantine(Sketch20Model model)
        {
            if (sketchQuarantineStatus != null && sketchQuarantineStatus.Text.StartsWith("Chưa kết nối")) model.QuarantineNotice = sketchQuarantineStatus.Text;
            for (int i = 0; i < dgvQuarantine.Rows.Count; i++)
            {
                var item = dgvQuarantine.Rows[i].Tag as QuarantineRow; if (item == null) continue;
                model.Quarantine.Add(ExactQuarantinedFile(item, i));
            }
        }
        private static Sketch20File ExactQuarantinedFile(QuarantineRow item, int index)
        {
            DateTime date = item.DetectedAt ?? DateTime.MinValue;
            return new Sketch20File { Index = index, Id = item.DetectionID.ToString(), Name = item.FileName, Path = item.OriginalPath, Reason = item.ThreatName,
                Date = date == DateTime.MinValue ? "—" : date.ToString("dd/MM/yyyy"), Time = date == DateTime.MinValue ? "—" : date.ToString("HH:mm"),
                Size = FormatFileSize(item.FileSizeBytes), Severity = item.ThreatName != null && item.ThreatName.IndexOf("EICAR", StringComparison.OrdinalIgnoreCase) >= 0 ? "Kiểm thử" : "Cao",
                Md5 = item.FileMD5, Sha256 = item.FileSHA256, Status = "Đang cách ly", Resolved = true, Source = "Lịch sử cách ly", Created = "—", FileType = Path.GetExtension(item.OriginalPath ?? "") };
        }
        internal void ExactPickQuarantine(int index, bool selected)
        { if (index >= 0 && index < dgvQuarantine.Rows.Count) { dgvQuarantine.Rows[index].Cells[colPick.Index].Value = selected; SyncButtons(); } }
        internal void ExactPickAllQuarantine(bool selected)
        { for (int i = 0; i < dgvQuarantine.Rows.Count; i++) ExactPickQuarantine(i, selected); }
        internal void ExactQuarantineCommand(string command)
        {
            if (command == "refresh") RefreshData();
            else if (command == "restore") btnRestore.PerformClick();
            else if (command == "delete") btnDeletePermanent.PerformClick();
        }
        internal void ExactLoadQuarantineDetail(int index, UcTongQuan detail)
        {
            if (index < 0 || index >= dgvQuarantine.Rows.Count) return;
            var item = dgvQuarantine.Rows[index].Tag as QuarantineRow; if (item == null) return;
            detail.OpenQuarantinedFileDetails(item.OriginalPath, item.ThreatName, item.FileSizeBytes ?? 0, item.DetectedAt ?? DateTime.MinValue, item.FileMD5, item.FileSHA1, item.FileSHA256);
            detail.exactQuarantineId = item.DetectionID.ToString();
        }
    }
    public partial class UcTongQuan
    {
        internal string exactQuarantineId;
        internal bool ExactScanning { get { return isScanning; } }
        private static string ExactFileSize(long bytes)
        { return bytes < 1024 ? bytes + " B" : bytes < 1024 * 1024 ? (bytes / 1024.0).ToString("0.#") + " KB" : (bytes / 1024.0 / 1024.0).ToString("0.#") + " MB"; }
        internal void ExactOverview(Sketch20Model model)
        {
            model.Scanning = isScanning;
            model.ThreatCount = threatRows.Count(t => !t.Resolved);
            string digits = new string((lblScannedCount.Text ?? "0").Where(char.IsDigit).ToArray()); int count; int.TryParse(digits, out count); model.ScannedFiles = count;
            model.LastDate = lblLastScanDate.Text; model.LastTime = lastScanTime == DateTime.MinValue ? "—" : lastScanTime.ToString("HH:mm"); model.LastType = lastJob == null ? "—" : lastJob.DisplayName;
            model.Status = lblScanProgress.Text == "Sẵn sàng quét" ? "Ứng dụng sẵn sàng" : lblScanProgress.Text;
            model.ScanPath = lblScanProgress.Text;
            for (int i = 0; i < threatRows.Count; i++) if (!threatRows[i].Resolved) model.Threats.Add(ExactFile(threatRows[i], i));
            for (int i = 0; i < Math.Min(5, dgvActivity.Rows.Count); i++)
            {
                var row = dgvActivity.Rows[i]; if (row.IsNewRow) continue;
                string description = Convert.ToString(row.Cells[colActDesc.Index].Value);
                int separator = description.IndexOf(": ", StringComparison.Ordinal);
                string title = separator > 0 ? description.Substring(0, separator) : description;
                string detail = separator > 0 ? description.Substring(separator + 2) : "";
                string icon = description.IndexOf("cách ly", StringComparison.OrdinalIgnoreCase) >= 0 ? "archive" : description.IndexOf("phát hiện", StringComparison.OrdinalIgnoreCase) >= 0 ? "alert" : description.IndexOf("cập nhật", StringComparison.OrdinalIgnoreCase) >= 0 ? "refresh" : "check";
                model.Activities.Add(new[] { icon, title, detail, Convert.ToString(row.Cells[colActTime.Index].Value) });
            }
        }
        private Sketch20File ExactFile(ThreatRow row, int index)
        {
            return new Sketch20File { Index = index, Name = Path.GetFileName(row.Path), Path = row.Path, Reason = row.ThreatText, Severity = row.LevelText,
                Size = ExactFileSize(row.SizeBytes), Date = lastScanTime == DateTime.MinValue ? "—" : lastScanTime.ToString("dd/MM/yyyy"), Time = lastScanTime == DateTime.MinValue ? "—" : lastScanTime.ToString("HH:mm"),
                Id = exactQuarantineId ?? "—", Md5 = row.HashMd5, Sha256 = row.HashSha256, Resolved = row.Resolved, Status = row.Quarantined ? "Đang cách ly" : row.StatusText,
                Created = row.CreatedAt == DateTime.MinValue ? "—" : row.CreatedAt.ToString("dd/MM/yyyy · HH:mm"), FileType = detailRowIndex == index ? lblTtLoaiTep.Text : Path.GetExtension(row.Path),
                Source = lastJob == null ? "Bảo vệ thời gian thực" : lastJob.DisplayName };
        }
        internal void ExactDetail(Sketch20Model model)
        {
            // Trang chi tiết phải dùng đúng danh sách của nguồn hiện tại (phiên quét đang xem
            // hoặc phiên lịch sử), không giữ danh sách từ Dashboard chính.
            model.Threats.Clear();
            for (int i = 0; i < threatRows.Count; i++) model.Threats.Add(ExactFile(threatRows[i], i));
            if (detailRowIndex < 0 || detailRowIndex >= threatRows.Count) return;
            var row = threatRows[detailRowIndex]; model.Detail = ExactFile(row, detailRowIndex);
            if (row.VtReport != null)
            {
                model.VtDetections = row.VtReport.Malicious; model.VtEngineCount = row.VtReport.TotalEngines;
                model.VtScore = row.VtReport.Malicious + "/" + row.VtReport.TotalEngines;
                model.VtSummary = row.VtReport.Summary(); model.VtTime = row.VtQueriedAt == DateTime.MinValue ? "—" : row.VtQueriedAt.ToString("dd/MM/yyyy · HH:mm");
            }
            else model.VtSummary = "Chưa tra cứu VirusTotal cho tệp này.";
            foreach (DataGridViewRow vendor in dgvVtVendors.Rows)
            { if (!vendor.IsNewRow && vendor.Cells.Count >= 2) model.Engines.Add(new[] { Convert.ToString(vendor.Cells[0].Value), Convert.ToString(vendor.Cells[1].Value) }); }
            foreach (DataGridViewRow value in dgvChuoiKyTu.Rows)
            { if (!value.IsNewRow && value.Cells.Count >= 2) model.Strings.Add(Convert.ToString(value.Cells[1].Value)); }
            string description = row.ThreatText ?? "";
            model.Behavior.Add(new[] { "Tiến trình", Path.GetFileName(row.Path) });
            model.Behavior.Add(new[] { "Khởi động cùng máy", description.IndexOf("Startup", StringComparison.OrdinalIgnoreCase) >= 0 ? "Có chỉ dấu" : "Chưa có chỉ dấu" });
            model.Behavior.Add(new[] { "Thực thi lệnh", description.IndexOf("PowerShell", StringComparison.OrdinalIgnoreCase) >= 0 ? "Có chỉ dấu PowerShell" : "Chưa có chỉ dấu" });
            model.Behavior.Add(new[] { "Kết nối mạng", model.Strings.Any(s => s.StartsWith("http", StringComparison.OrdinalIgnoreCase)) ? "Có chuỗi địa chỉ trong nội dung tệp" : "Chưa có chuỗi địa chỉ được trích xuất" });
            model.Behavior.Add(new[] { "Kết luận", row.Resolved ? "Tệp đã được xử lý; đối chiếu với kết quả chữ ký." : "Kiểm tra nguồn gốc và kết quả phân tích trước khi chạy." });
        }
        internal void ExactOpenThreat(int index, string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                int found = threatRows.FindIndex(r => string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase));
                if (found >= 0) index = found;
            }
            if (index < 0 || index >= threatRows.Count) return;
            HienThi(TongQuanView.ChiTiet, false);
            RefreshDetailGrid();
            ChonDongChiTiet(index);
        }
        internal void ExactSelectDetailTab(int index)
        { tabChiTietDong.SelectedIndex = index; NapTabChiTiet(detailRowIndex, index); }
        internal void ExactQuickScan() { if (isScanning) return; HienThi(TongQuanView.AnToan, false); BtnScanNow_Click(null, EventArgs.Empty); }
        internal void ExactScanCommand(string command)
        {
            if (command == "cancel") { if (cts != null) cts.Cancel(); }
            else if (command == "isolate") BtnQuarantineAll_Click(null, EventArgs.Empty);
            else if (command == "isolate-file") XuLyMotDong(detailRowIndex, 1);
            else if (command == "delete-file") XuLyMotDong(detailRowIndex, 2);
            else if (command == "vt-query") XuLyMotDong(detailRowIndex, 3);
        }
        internal async void ExactStartAdvanced(int mode, List<string> targets, bool recursive)
        {
            if (isScanning) return;
            if (mode != 0 && !recursive && mode == 1)
            {
                var topFiles = new List<string>();
                foreach (var folder in targets) { try { topFiles.AddRange(Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)); } catch (Exception ex) { AppDialog.Show("Không đọc được thư mục: " + ex.Message, "Quét thư mục", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } }
                targets = topFiles;
            }
            if (mode != 0 && targets.Count == 0) { AppDialog.Show("Chưa có tệp hoặc vị trí nào để quét.", "Quét nâng cao", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var job = new ScanJob { Type = mode == 0 ? ScanType.Full : ScanType.Custom, Targets = mode == 0 ? null : targets,
                DisplayName = mode == 0 ? "Quét toàn bộ hệ thống" : mode == 1 ? "Quét thư mục" : mode == 2 ? "Quét tệp" : "Quét tùy chỉnh",
                ScopeText = mode == 0 ? "Các ổ đĩa sẵn sàng" : string.Join("; ", targets), FromAdvanced = true, AutoQuarantine = RealTimeProtection.AutoQuarantine };
            await ChayPhienQuet(job);
        }
        internal void ExactLoadHistory(DataTable table, HistoryEntry history)
        {
            threatRows.Clear(); detailRowIndex = 0; lastScanTime = history.Time;
            lastJob = new ScanJob { DisplayName = history.Type, ScopeText = history.Scope };
            foreach (DataRow value in table.Rows)
            {
                string status = Convert.ToString(value["Status"]);
                threatRows.Add(new ThreatRow { Path = Convert.ToString(value["OriginalPath"]), ThreatText = Convert.ToString(value["ThreatName"]),
                    Level = Theme.ThreatLevel.High, SizeBytes = value["FileSizeBytes"] == DBNull.Value ? 0 : Convert.ToInt64(value["FileSizeBytes"]),
                    HashMd5 = Convert.ToString(value["FileMD5"]), HashSha1 = Convert.ToString(value["FileSHA1"]), HashSha256 = Convert.ToString(value["FileSHA256"]),
                    Quarantined = status.IndexOf("quarant", StringComparison.OrdinalIgnoreCase) >= 0 || status.IndexOf("cách ly", StringComparison.OrdinalIgnoreCase) >= 0,
                    Deleted = status.IndexOf("deleted", StringComparison.OrdinalIgnoreCase) >= 0 });
            }
            if (threatRows.Count > 0) MoChiTietKetQua(0);
        }
    }
}
