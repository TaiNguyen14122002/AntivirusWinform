using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
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
        internal static bool ExactDesiredFlag(string key)
        {
            switch (key)
            {
                case "rt": return FeatureFlags.RealTimeOn;
                case "restore": return FeatureFlags.FileRestoreGuard;
                case "usb": return FeatureFlags.UsbProtection;
                case "download": return FeatureFlags.DownloadProtection;
                case "behavior": return FeatureFlags.BehaviorWatch;
                case "startup": return FeatureFlags.StartupFoldersWatch;
                default: return IsOn(key);
            }
        }

        internal void ExactShowProtectionDetails(string key, IWin32Window owner)
        {
            string title, description, limitation, serviceKey;
            switch (key)
            {
                case "rt":
                    title = "Bảo vệ thời gian thực";
                    description = "Theo dõi các thư mục của lượt quét nhanh. Quét tệp tạo mới hoặc thay đổi bằng engine chữ ký và heuristic.";
                    limitation = "Chỉ hoạt động khi ứng dụng mở; FileSystemWatcher không chặn việc mở/thi hành tệp như driver antivirus.";
                    serviceKey = null; break;
                case "restore":
                    title = "Bảo vệ tệp";
                    description = "Giám sát Desktop, Documents, Downloads: quét tệp tạo hoặc sửa; kiểm tra lại tệp trước khi khôi phục từ cách ly.";
                    limitation = "Không thể chặn truy cập ngay lúc mở tệp. Tệp đang ghi dở được báo chưa kiểm tra, không khẳng định an toàn.";
                    serviceKey = "file"; break;
                case "usb":
                    title = "Bảo vệ USB";
                    description = "Kiểm tra ổ USB mới xuất hiện theo chu kỳ 5 giây và quét toàn bộ tệp có thể đọc được.";
                    limitation = "Không can thiệp AutoRun ở mức hệ điều hành; USB đã cắm trước lúc bật có thể cần quét thủ công.";
                    serviceKey = "usb"; break;
                case "download":
                    title = "Bảo vệ tải xuống";
                    description = "Theo dõi Downloads và quét tệp tải về có dấu Zone.Identifier (MOTW).";
                    limitation = "Một số trình duyệt/hệ thống tệp không tạo MOTW; đây không phải tính năng ngăn tải xuống trước khi lưu.";
                    serviceKey = "download"; break;
                case "behavior":
                    title = "Giám sát hành vi";
                    description = "Theo dõi sự kiện tạo tiến trình qua WMI và nhiều thay đổi tệp qua FileSystemWatcher, dùng các quy tắc heuristic để cảnh báo.";
                    limitation = "Nghi vấn hành vi có thể dương tính giả; không tự cách ly nếu chỉ có bằng chứng heuristic.";
                    serviceKey = "behavior"; break;
                case "startup":
                    title = "Bảo vệ thư mục khởi động";
                    description = "Theo dõi thư mục Startup của người dùng và toàn máy, quét tệp mới xuất hiện.";
                    limitation = "Chưa giám sát khóa Registry Run/RunOnce hay Task Scheduler; chỉ bảo vệ thư mục Startup.";
                    serviceKey = "startup"; break;
                default:
                    title = "Tự động cách ly";
                    description = "Tự cách ly tệp khi phát hiện chữ ký đáng tin cậy bằng engine quét.";
                    limitation = "Phát hiện heuristic đơn thuần không được tự cách ly nhằm tránh xóa nhầm tệp hợp lệ.";
                    serviceKey = null; break;
            }
            using (var dialog = new Form())
            {
                dialog.Text = "Cấu hình – " + title;
                dialog.Font = Theme.DefaultUiFont;
                dialog.BackColor = Color.White;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(650, 435);
                var heading = new Label { Left = 22, Top = 19, Width = 605, Height = 36,
                    Font = new Font(Theme.UiFontFamily, 16f, FontStyle.Bold), Text = title,
                    ForeColor = Color.FromArgb(16, 36, 69) };
                var state = new Label { Left = 22, Top = 62, Width = 605, Height = 29,
                    Font = new Font(Theme.UiFontFamily, 10.6f, FontStyle.Bold) };
                var body = new Label { Left = 22, Top = 99, Width = 605, Height = 50,
                    Text = description, Font = new Font(Theme.UiFontFamily, 10.4f) };
                var rootsTitle = new Label { Left = 22, Top = 155, Width = 605, Height = 25,
                    Text = "PHẠM VI ĐANG GIÁM SÁT", ForeColor = Color.FromArgb(84, 107, 147) };
                var roots = new TextBox { Left = 22, Top = 184, Width = 605, Height = 98,
                    Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                    BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(246, 250, 255),
                    Font = new Font(Theme.UiFontFamily, 9.7f) };
                var caveat = new Label { Left = 22, Top = 291, Width = 605, Height = 54,
                    Text = "Giới hạn: " + limitation, ForeColor = Color.FromArgb(131, 93, 31),
                    Font = new Font(Theme.UiFontFamily, 9.7f) };
                var toggle = new Button { Left = 22, Top = 365, Width = 146, Height = 42,
                    FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(231, 243, 255) };
                var retry = new Button { Left = 179, Top = 365, Width = 155, Height = 42,
                    Text = "Khởi động lại", FlatStyle = FlatStyle.Flat };
                var close = new Button { Left = 494, Top = 365, Width = 133, Height = 42,
                    Text = "Đóng", FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
                toggle.FlatAppearance.BorderColor = retry.FlatAppearance.BorderColor =
                    close.FlatAppearance.BorderColor = Color.FromArgb(198, 215, 233);
                Action refresh = () =>
                {
                    bool desired = ExactDesiredFlag(key);
                    bool running = IsOn(key);
                    string status = key == "rt" ?
                        (RealTimeProtection.IsRunning ? (RealTimeProtection.IsDegraded ? "Đang chạy – có sự kiện bị bỏ sót" : "Đang chạy") : "Chưa chạy") :
                        serviceKey != null ? GuardService.GetGuardStatus(serviceKey) : (running ? "Đang bật" : "Đang tắt");
                    state.Text = (desired ? "Đã yêu cầu bật" : "Đã tắt") + "  •  " + status;
                    state.ForeColor = desired && !running ? Color.Firebrick : running ? Color.ForestGreen : Color.DarkGoldenrod;
                    toggle.Text = desired ? "Tắt bảo vệ" : "Bật bảo vệ";
                    var locations = serviceKey == null ? new string[0] : GuardService.GetWatchRoots(serviceKey);
                    if (key == "rt")
                        locations = ScanEngine.GetScanRoots(ScanType.Quick, null)
                            .Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                            .ToArray();
                    roots.Text = locations.Length == 0 ? "Chưa có thư mục/thiết bị nào được giám sát." :
                        string.Join(Environment.NewLine, locations);
                    retry.Enabled = desired && serviceKey != null;
                };
                toggle.Click += (sender, e) => { SetState(key, !ExactDesiredFlag(key)); SyncRealFeaturesUi(); SyncProtectionStats(); refresh(); };
                retry.Click += (sender, e) =>
                {
                    if (serviceKey != null && ExactDesiredFlag(key))
                    {
                        GuardService.Configure(serviceKey, false);
                        GuardService.Configure(serviceKey, true);
                        SyncRealFeaturesUi(); SyncProtectionStats(); refresh();
                    }
                };
                dialog.Controls.AddRange(new System.Windows.Forms.Control[] { heading, state, body, rootsTitle,
                    roots, caveat, toggle, retry, close });
                dialog.CancelButton = close;
                refresh();
                dialog.ShowDialog(owner);
            }
        }

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
            if (key == "samples") TrySetSampleSubmission(enabled, FindForm() ?? (IWin32Window)this);
        }
        internal void ExactSettingsCommand(string command, string key = null)
        {
            if (command == "save-settings") BtnSaveSettings_Click(null, EventArgs.Empty);
            else if (command == "defaults") BtnResetDefaults_Click(null, EventArgs.Empty);
            else if (command == "open-data") BtnOpenData_Click(null, EventArgs.Empty);
            else if (command == "clear-cache") BtnClearCache_Click(null, EventArgs.Empty);
            else if (command == "submit-files") OpenSampleSubmission(FindForm() ?? (IWin32Window)this);
            else if (command == "samples") BtnSamples_Click(null, EventArgs.Empty);
            else if (command == "clear-key") { BtnClearVtKey_Click(null, EventArgs.Empty); FeatureFlags.VtAutoQuery = false; FeatureFlags.Persist(); FeatureFlags.NotifyChanged(); }
            else if (command == "save-key") { txtVtKey.Text = key ?? ""; BtnSaveVtKey_Click(null, EventArgs.Empty); }
            else if (command == "vt-config")
            {
                using (var dialog = new VirusTotalConfigForm()) dialog.ShowDialog(FindForm());
            }
        }
    }
    public partial class UcLichSu
    {
        internal void ExactHistory(Sketch20Model model)
        {
            model.HistoryPage = lblPage.Text; model.HistoryTotal = dangXem.Count; model.HistoryPageSize = PageSize;
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
            if (command != null && command.StartsWith("history-size:", StringComparison.Ordinal))
            {
                int count;
                if (!int.TryParse(command.Substring("history-size:".Length), out count) ||
                    (count != 10 && count != 20 && count != 50)) return;
                PageSize = count;
                trang = 1;
                Render();
                return;
            }
            if (command != null && command.StartsWith("history-page:", StringComparison.Ordinal))
            {
                int target;
                if (int.TryParse(command.Substring("history-page:".Length), out target)) GoPage(target);
                return;
            }
            if (command == "export") MiXuatCsv_Click(null, EventArgs.Empty);
            else if (command == "history-prev") GoPage(trang - 1);
            else if (command == "history-next") GoPage(trang + 1);
            else if (command == "history-refresh") LoadRealData();

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
            if (isScanning)
            {
                // Bộ đếm live thuộc chính phiên quét đang chạy; không dùng lại
                // threatRows của phiên trước. Chỉ đọc snapshot, không truy cập UI worker.
                model.ThreatCount = 0;
            }
            if (isScanning && activeScanReading != null)
            {
                // Dùng chung một chuẩn đếm cho tiến trình và cửa sổ kết quả.
                model.ScannedFiles = activeScanReading.CheckedObjects;
                model.ThreatCount = activeScanReading.ThreatsFound;
                model.Progress = activeScanReading.Percent;
                model.EstimatedRemaining = ScanTelemetry.DescribeRemaining(activeScanReading);
                model.ScanSpeed = activeScanReading.MiBPerSecond.ToString("0.0") + " MiB/s";
                if (!string.IsNullOrWhiteSpace(activeScanReading.CurrentPath))
                    model.ScanPath = activeScanReading.CurrentPath;
            }
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
            return new Sketch20File { Index = index, Name = ScanDisplayPath.Leaf(row.Path), Path = row.Path, Reason = row.ThreatText, Severity = row.LevelText,
                Size = ExactFileSize(row.SizeBytes), Date = lastScanTime == DateTime.MinValue ? "—" : lastScanTime.ToString("dd/MM/yyyy"), Time = lastScanTime == DateTime.MinValue ? "—" : lastScanTime.ToString("HH:mm"),
                Id = exactQuarantineId ?? "—", Md5 = row.HashMd5, Sha256 = row.HashSha256, Resolved = row.Resolved, Status = row.Quarantined ? "Đang cách ly" : row.StatusText,
                Created = row.CreatedAt == DateTime.MinValue ? "—" : row.CreatedAt.ToString("dd/MM/yyyy · HH:mm"), FileType = detailRowIndex == index ? lblTtLoaiTep.Text : ScanDisplayPath.Extension(row.Path),
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
            model.VtQuerying = IsVirusTotalDetailQuerying(row);
            model.VtLookedUp = row.VtReport != null;
            model.VtNotFound = row.VtReport != null && row.VtReport.Error == null && !row.VtReport.Found && !row.VtReport.Uploaded;
            model.VtUploaded = row.VtReport != null && row.VtReport.Uploaded;
            model.VtHasError = row.VtReport != null && row.VtReport.Error != null;
            if (row.VtReport != null)
            {
                if (row.VtReport.Found && row.VtReport.Error == null)
                {
                    model.VtDetections = row.VtReport.Malicious; model.VtEngineCount = row.VtReport.TotalEngines;
                    model.VtClean = row.VtReport.Undetected + row.VtReport.Harmless;
                    model.VtSuspicious = row.VtReport.Suspicious;
                    model.VtScore = row.VtReport.Malicious + "/" + row.VtReport.TotalEngines;
                }
                model.VtSummary = row.VtReport.Summary();
                model.VtTime = row.VtReport.AnalysisTime.HasValue ? row.VtReport.AnalysisTime.Value.ToString("dd/MM/yyyy · HH:mm") :
                    (row.VtQueriedAt == DateTime.MinValue ? "—" : row.VtQueriedAt.ToString("dd/MM/yyyy · HH:mm"));
            }
            else model.VtSummary = model.VtQuerying ? "Đang tự động tra cứu SHA-256 trên VirusTotal..." :
                !VirusTotalClient.IsConfigured ? "Chưa cấu hình API key VirusTotal. Vào Quản trị để thiết lập." :
                !FeatureFlags.VtAutoQuery ? "Tự động tra cứu đang tắt trong Quản trị; có thể nhấn Tra cứu VirusTotal." :
                "Đang chờ tra cứu SHA-256 tự động.";
            if (row.VtReport != null)
                foreach (VirusTotalEngine engine in row.VtReport.Engines)
                    model.Engines.Add(new[] { engine.Name ?? "—", engine.Category ?? "unknown",
                        engine.Result ?? "—", engine.Update ?? "—" });
            foreach (DataGridViewRow value in dgvChuoiKyTu.Rows)
            { if (!value.IsNewRow && value.Cells.Count >= 2) model.Strings.Add(Convert.ToString(value.Cells[1].Value)); }
            string description = row.ThreatText ?? "";
            model.Behavior.Add(new[] { "Tiến trình", ScanDisplayPath.Leaf(row.Path) });
            model.Behavior.Add(new[] { "Khởi động cùng máy", description.IndexOf("Startup", StringComparison.OrdinalIgnoreCase) >= 0 ? "Có chỉ dấu" : "Chưa có chỉ dấu" });
            model.Behavior.Add(new[] { "Thực thi lệnh", description.IndexOf("PowerShell", StringComparison.OrdinalIgnoreCase) >= 0 ? "Có chỉ dấu PowerShell" : "Chưa có chỉ dấu" });
            model.Behavior.Add(new[] { "Kết nối mạng", model.Strings.Any(s => s.StartsWith("http", StringComparison.OrdinalIgnoreCase)) ? "Có chuỗi địa chỉ trong nội dung tệp" : "Chưa có chuỗi địa chỉ được trích xuất" });
            model.Behavior.Add(new[] { "Kết luận", row.Resolved ? "Tệp đã được xử lý; đối chiếu với kết quả chữ ký." : "Kiểm tra nguồn gốc và kết quả phân tích trước khi chạy." });
        }
        // Xử lý hàng loạt từ trang Mối đe dọa. Dùng lại các phương thức
        // QuarantineRows/DeleteRows đã kiểm tra ZIP, SQL và xác nhận xóa.
        internal void ExactProcessThreats(IList<int> indices, string operation)
        {
            if (isScanning)
            {
                AppDialog.Show("Không xử lý tệp khi phiên quét đang chạy.", "Mối đe dọa",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var targets = (indices ?? new List<int>()).Distinct()
                .Where(i => i >= 0 && i < threatRows.Count && !threatRows[i].Resolved)
                .ToList();
            if (targets.Count == 0) return;
            var paths = new HashSet<string>(targets.Select(i => threatRows[i].Path),
                StringComparer.OrdinalIgnoreCase);
            var rows = dgvActions.Rows.Cast<DataGridViewRow>()
                .Where(r => paths.Contains(Convert.ToString(r.Cells[colActionFile.Index].Value)))
                .ToList();
            if ((operation == "quarantine" || operation == "delete") && rows.Count == 0)
            {
                AppDialog.Show("Các mục đã chọn không còn nằm trong danh sách tệp có thể xử lý. " +
                    "Hãy làm mới kết quả quét và thử lại.", "Mối đe dọa",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (operation == "quarantine")
            {
                QuarantineRows(rows);
                return;
            }
            if (operation == "delete")
            {
                DeleteRows(rows);
                return;
            }
            if (operation != "allow") return;
            // Không yêu cầu đăng nhập Admin ở trang Mối đe dọa.
            // Vẫn giới hạn ngoại lệ chỉ cho heuristic, và xác nhận trước khi ghi.
            var safe = targets.Where(i =>
                (threatRows[i].ThreatText ?? "").StartsWith("Heuristic:", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(threatRows[i].Path) &&
                threatRows[i].Path.IndexOf(" > ", StringComparison.Ordinal) < 0 &&
                File.Exists(threatRows[i].Path)).ToList();
            if (safe.Count != targets.Count)
            {
                AppDialog.Show("Chỉ có thể tạo ngoại lệ cho phát hiện heuristic trên tệp đang tồn tại. " +
                    "Không thể cho phép mẫu đã khớp chữ ký hoặc mục bên trong ZIP.",
                    "Giới hạn an toàn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (AppDialog.Show("Tạo ngoại lệ heuristic theo SHA-256 cho " + safe.Count +
                " tệp trong 7 ngày?\nNgoại lệ không vô hiệu hóa việc phát hiện mã độc bằng chữ ký.",
                "Xác nhận cho phép", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            int created = 0;
            foreach (int i in safe)
            {
                try
                {
                    var row = threatRows[i];
                    ScanExclusionService.AddHeuristicExclusion(row.Path,
                        "Người dùng xác nhận ngoại lệ heuristic từ danh sách mối đe dọa", 7);
                    // Chỉ loại khỏi danh sách sau khi ghi SQL thành công.
                    foreach (var duplicate in threatRows.Where(r =>
                        string.Equals(r.Path, row.Path, StringComparison.OrdinalIgnoreCase)))
                        duplicate.Allowed = true;
                    BoDongTrongBangDeDoa(row.Path);
                    created++;
                }
                catch (Exception ex)
                {
                    AppDialog.Show("Không thể tạo ngoại lệ: " + ex.Message,
                        "Cho phép tệp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                }
            }
            UpdateThreatUi(); RefreshDetailGrid();
            AppDialog.Show("Đã tạo " + created + "/" + safe.Count + " ngoại lệ heuristic có thời hạn.",
                "Cho phép tệp", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        {
            tabChiTietDong.SelectedIndex = index;
            NapTabChiTiet(detailRowIndex, index);
            if (index == 1) LookupVirusTotalForCurrentDetail();
        }
        internal void ExactQuickScan() { if (isScanning) return; HienThi(TongQuanView.AnToan, false); BtnScanNow_Click(null, EventArgs.Empty); }
        internal void ExactScanCommand(string command)
        {
            if (command == "cancel") { if (cts != null) cts.Cancel(); }
            else if (command == "isolate") BtnQuarantineAll_Click(null, EventArgs.Empty);
            else if (command == "isolate-file") ExactQuarantineCurrentDetail();
            else if (command == "delete-file") XuLyMotDong(detailRowIndex, 2);
            else if (command == "vt-query") LookupVirusTotalForCurrentDetail(true);
            else if (command == "vt-upload")
            {
                if (detailRowIndex >= 0 && detailRowIndex < threatRows.Count)
                    GuiMauVirusTotalTheoPath(threatRows[detailRowIndex].Path);
            }
        }
        /// <summary>
        /// Cách ly CHÍNH XÁC tệp đang xem trong trang Chi tiết; không dùng nút
        /// "Cách ly tất cả" và không ảnh hưởng các bản ghi còn lại.
        /// </summary>
        private void ExactQuarantineCurrentDetail()
        {
            int index = detailRowIndex;
            if (index < 0 || index >= threatRows.Count)
            {
                AppDialog.Show("Không xác định được tệp đang xem.", "Cách ly tệp",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ThreatRow row = threatRows[index];
            if (row.Quarantined || row.Deleted)
            {
                AppDialog.Show(row.Quarantined ? "Tệp này đã được cách ly." :
                    "Tệp này đã bị xóa và không thể cách ly.", "Cách ly tệp",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(row.Path) || !File.Exists(row.Path))
            {
                AppDialog.Show("Không tìm thấy tệp ở đường dẫn gốc. Hãy kiểm tra trang Cách ly " +
                    "để xác nhận tệp đã được xử lý hay chưa.", "Cách ly tệp",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string message = "Bạn có chắc chắn muốn cách ly tệp đang xem?\n\n" +
                ScanDisplayPath.Leaf(row.Path) + "\n" + row.Path +
                "\n\nTệp sẽ được chuyển vào khu vực cách ly và không thể sử dụng " +
                "tại vị trí cũ cho đến khi được khôi phục.";
            if (AppDialog.Show(message, "Xác nhận cách ly tệp",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            // Tận dụng luồng cách ly đã có: ScanEngine.Quarantine lưu vào kho
            // và SQL, phát QuarantineChanged để giao diện Cách ly nạp lại.
            // XuLyMotDong chỉ đánh dấu đã cách ly và báo thành công khi lưu thành công.
            XuLyMotDong(index, 1);
        }

        internal async void ExactStartAdvanced(int mode, List<string> targets, bool recursive)
        {
            if (isScanning) return;
            // Luôn duyệt toàn bộ cây thư mục để bảo đảm mỗi tệp trong
            // thư mục được chọn (và mọi cấp con) được kiểm tra riêng.
            // Cờ recursive cũ được giữ trong chữ ký hàm cho tương thích.
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
