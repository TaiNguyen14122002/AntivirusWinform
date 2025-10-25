using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Control;
using ScanAndRemoveVirus.Database;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus
{
    public partial class FrmMain
    {
        internal Sketch20View exactView;
        private UcTongQuan exactDetailSource;
        private StatusStrip nativeStatusStrip;
        private ToolStripStatusLabel nativeStatusText;
        private ToolStripStatusLabel nativeStatusSpring;
        private ToolStripStatusLabel nativeStatusSignature;

        // Controller Cách ly legacy bị ẩn trong giao diện Sketch20 nên đôi khi
        // chưa có WinForms handle. Nhận sự kiện tại Form (có handle) để không
        // đánh rơi thay đổi dữ liệu SQL sau khi cách ly/khôi phục/xóa.
        private bool quarantineRefreshPending;
        private bool quarantineSnapshotDirty;
        private bool historyRefreshPending;
        private string nextScanSummary = "Đang tải lịch quét tự động...";
        private DateTime lastScheduleFetchUtc = DateTime.MinValue;
        private bool scheduleFetchPending;

        // Không đọc SQL 3 lần/giây từ vòng RefreshModel; chỉ cập nhật 2 phút/lần
        // hoặc ngay khi người dùng chỉnh lịch, luôn truy vấn ở background thread.
        private void QueueScheduleSummaryRefresh(bool force = false)
        {
            if (IsDisposed || Disposing || !IsHandleCreated || scheduleFetchPending) return;
            if (!force && (DateTime.UtcNow - lastScheduleFetchUtc).TotalMinutes < 2) return;
            scheduleFetchPending = true;
            lastScheduleFetchUtc = DateTime.UtcNow;
            Task.Run(() =>
            {
                try { return ScanScheduleService.GetOverviewSummary(); }
                catch (Exception) { return "Chưa đọc được lịch · Kiểm tra SQL Server"; }
            }).ContinueWith(t =>
            {
                if (IsDisposed || Disposing || !IsHandleCreated) return;
                try
                {
                    BeginInvoke((Action)(() =>
                    {
                        scheduleFetchPending = false;
                        if (IsDisposed || Disposing) return;
                        nextScanSummary = t.IsFaulted ? "Không đọc được lịch quét" : t.Result;
                        if (exactView != null && !exactView.IsDisposed && exactView.Page == "overview")
                            exactView.RefreshModel(true);
                    }));
                }
                catch (InvalidOperationException) { scheduleFetchPending = false; }
            });
        }

        // Khi lịch sử được ghi từ quét thủ công hoặc bảo vệ thời gian thực,
        // chỉ cập nhật view nếu tab Lịch sử đang mở. Nếu đang ở tab khác,
        // lần điều hướng kế tiếp sẽ đọc trực tiếp SQL Server.
        private void OnSketchScanHistoryChanged()
        {
            if (IsDisposed || Disposing || !IsHandleCreated) return;
            try
            {
                if (InvokeRequired)
                    BeginInvoke((Action)QueueSketchHistoryRefresh);
                else
                    QueueSketchHistoryRefresh();
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private void QueueSketchHistoryRefresh()
        {
            if (historyRefreshPending || IsDisposed || Disposing || !IsHandleCreated ||
                exactView == null || exactView.IsDisposed || exactView.Page != "history") return;

            historyRefreshPending = true;
            try
            {
                BeginInvoke((Action)(() =>
                {
                    historyRefreshPending = false;
                    if (IsDisposed || Disposing || exactView == null || exactView.IsDisposed ||
                        exactView.Page != "history") return;
                    ucLichSu.ExactRefreshForSketch();
                    exactView.ResetHistoryDisplayPage();
                    exactView.RefreshModel(true);
                }));
            }
            catch (ObjectDisposedException) { historyRefreshPending = false; }
            catch (InvalidOperationException) { historyRefreshPending = false; }
        }


        private void OnSketchQuarantineChanged()
        {
            if (IsDisposed || Disposing || !IsHandleCreated) return;
            try
            {
                if (InvokeRequired) BeginInvoke((Action)QueueSketchQuarantineRefresh);
                else QueueSketchQuarantineRefresh();
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        // Gộp nhiều thông báo (chẳng hạn cách ly hàng loạt) thành một lần đọc SQL.
        private void QueueSketchQuarantineRefresh()
        {
            if (quarantineRefreshPending || IsDisposed || Disposing || !IsHandleCreated) return;
            quarantineRefreshPending = true;
            try
            {
                BeginInvoke((Action)(() =>
                {
                    quarantineRefreshPending = false;
                    if (IsDisposed || Disposing || exactView == null || exactView.IsDisposed) return;
                    ucCachLy.RefreshData();
                    quarantineSnapshotDirty = true;
                    exactView.RefreshModel(true);
                }));
            }
            catch (ObjectDisposedException) { quarantineRefreshPending = false; }
            catch (InvalidOperationException) { quarantineRefreshPending = false; }
        }

        private void InitializeSketch20()
        {
            // Giữ các controller hiện có sống trên UI thread; lớp mới là toàn bộ giao diện hiển thị.
            // Bộ kiểm tra hợp đồng cũ có thể kiểm tra controller mà không phủ view lên control cũ.
            if (Environment.GetEnvironmentVariable("XVIRUS_LEGACY_UI_TEST") == "1") return;
            EnableSketch20();
        }
        private void InitializeNativeStatusStrip()
        {
            if (nativeStatusStrip != null) return;
            nativeStatusStrip = new StatusStrip { Name = "mainStatusStrip", Dock = DockStyle.Bottom, SizingGrip = true, RenderMode = ToolStripRenderMode.System };
            nativeStatusText = new ToolStripStatusLabel("Sẵn sàng");
            nativeStatusSpring = new ToolStripStatusLabel { Spring = true };
            nativeStatusSignature = new ToolStripStatusLabel("CSDL chữ ký: —");
            nativeStatusStrip.Items.AddRange(new ToolStripItem[] { nativeStatusText, nativeStatusSpring, nativeStatusSignature });
            Controls.Add(nativeStatusStrip);
            nativeStatusStrip.BringToFront();
            SizeChanged += delegate
            {
                if (exactView != null) UpdateNativeStatusStrip(exactView.Model);
            };
        }
        private void ShowNativeScanningStatus()
        {
            if (nativeStatusText == null) return;
            nativeStatusText.Text = "Bảo vệ thời gian thực: " + (RealTimeProtection.IsRunning ? "Bật" : "Tắt") + "   |   Đang quét";
        }
        private void UpdateNativeStatusStrip(Sketch20Model model)
        {
            if (nativeStatusStrip == null || model == null) return;
            string state = model.Scanning ? "Đang quét" : model.ThreatCount > 0 ? model.ThreatCount + " mối đe dọa cần xử lý" : "Sẵn sàng";
            bool compact = ClientSize.Width < 900;
            string statusText = compact
                ? (model.Running ? "Bảo vệ: Bật" : "Bảo vệ: Tắt") + "   |   " + state
                : "Bảo vệ thời gian thực: " + (model.Running ? "Bật" : "Tắt") + "   |   " + state;
            string signatureText = "CSDL chữ ký: " + (string.IsNullOrWhiteSpace(model.Signature) ? "—" : model.Signature);
            // Không ép StatusStrip tính layout/vẽ lại nếu văn bản không đổi.
            if (!string.Equals(nativeStatusText.Text, statusText, System.StringComparison.Ordinal))
                nativeStatusText.Text = statusText;
            if (!string.Equals(nativeStatusSignature.Text, signatureText, System.StringComparison.Ordinal))
                nativeStatusSignature.Text = signatureText;
            if (nativeStatusSignature.Visible != !compact) nativeStatusSignature.Visible = !compact;
        }
        internal void EnableSketch20()
        {
            if (exactView != null) return;
            ClientSize = new Size(1240, 900); MinimumSize = new Size(1040, 720);
            Padding = Padding.Empty; BackColor = SystemColors.Control;
            // Loại legacy hosts khỏi Dock layout trước khi thêm view mới.
            pnlSidebar.Visible = false; pnlSidebar.Dock = DockStyle.None;
            pnlContent.Visible = false; pnlContent.Dock = DockStyle.None;
            InitializeNativeStatusStrip();
            var actions = new Sketch20Actions
            {
                Navigate = ExactNavigate,
                Command = ExactCommand,
                // Các chức năng ngoài trang Quản trị không yêu cầu đăng nhập Admin.
                SetFlag = (key, enabled) => { if (key == "autostart" || key == "samples") ucCaiDat.ExactSetGeneral(key, enabled); else ucBaoVe.ExactSetFlag(key, enabled); },
                SaveKey = key => ucCaiDat.ExactSettingsCommand("save-key", key),
                SelectQuarantine = ucCachLy.ExactPickQuarantine,
                SelectAllQuarantine = ucCachLy.ExactPickAllQuarantine,
                OpenFile = ExactOpenFile,
                ProcessThreats = (indexes, operation) => ucTongQuan.ExactProcessThreats(indexes, operation),
                SelectDetailTab = index => (exactDetailSource ?? ucTongQuan).ExactSelectDetailTab(index),
                FilterHistory = ucLichSu.ExactHistoryFilter,
                SortHistory = ucLichSu.ExactHistorySort,
                StartAdvanced = (mode, targets, includeSubfolders) =>
                {
                    ucTongQuan.ExactStartAdvanced(mode, targets, includeSubfolders);
                    if (ucTongQuan.ExactScanning) ShowNativeScanningStatus();
                }
            };
            exactView = new Sketch20View(ExactModel(), actions, ExactModel)
            {
                Name = "Exact20Screens",
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            exactView.SetAdminPage(ucQuanTri);
            ucQuanTri.NavigateRequested = page => { if (exactView != null) exactView.Go(page); };
            Controls.Add(exactView);
            exactView.BringToFront();
            // Với Dock=Fill + StatusStrip Dock=Bottom, view phải đứng trước trong Z-order
            // để WinForms dành phần đáy cho StatusStrip thay vì vẽ chồng lên nó.
            if (nativeStatusStrip != null)
            {
                Controls.SetChildIndex(exactView, 0);
                Controls.SetChildIndex(nativeStatusStrip, Math.Min(1, Controls.Count - 1));
            }
            PerformLayout();
            Sketch20Chrome.Attach(this, exactView);
            InitializeScheduleNotifications();
            QueueScheduleSummaryRefresh(true);
            // Đăng ký sau khi tạo view, chỉ một lần trong vòng đời cửa sổ.
            ScanEngine.QuarantineChanged += OnSketchQuarantineChanged;
            ScanHistoryStore.Changed += OnSketchScanHistoryChanged;
            // Ép WinForms tính lại Dock sau khi status strip + exact view đã được thêm.
            PerformLayout();
            FormClosed += delegate
            {
                ScanEngine.QuarantineChanged -= OnSketchQuarantineChanged;
                ScanHistoryStore.Changed -= OnSketchScanHistoryChanged;
                if (exactDetailSource != null && !exactDetailSource.IsDisposed) exactDetailSource.Dispose();
            };
        }
        private Sketch20Model ExactModel()
        {
            var model = new Sketch20Model { Running = RealTimeProtection.IsRunning, Version = GetType().Assembly.GetName().Version.ToString(3) };
            foreach (string key in Sketch20View.FeatureKeys.Concat(new[] { "alerts", "update" })) model.Flags[key] = UcBaoVe.ExactFlag(key);
            model.GuardDesired["rt"] = FeatureFlags.RealTimeOn;
            model.GuardMessages["rt"] = RealTimeProtection.IsDegraded ?
                "Giám sát bị gián đoạn: " + RealTimeProtection.LastWatcherError :
                RealTimeProtection.IsRunning ? "Đang giám sát tệp trong phạm vi quét nhanh" : "Chưa chạy";
            foreach (var item in new[] {
                new[] { "restore", "file" }, new[] { "usb", "usb" },
                new[] { "download", "download" }, new[] { "behavior", "behavior" },
                new[] { "startup", "startup" }
            })
            {
                model.GuardDesired[item[0]] = UcBaoVe.ExactDesiredFlag(item[0]);
                model.GuardMessages[item[0]] = GuardService.GetGuardStatus(item[1]);
            }
            model.KeyConfigured = VirusTotalClient.IsConfigured;
            model.Signature = ucBaoVe.ExactSignature;
            ucTongQuan.ExactOverview(model);
            model.NextAutomaticScan = nextScanSummary;
            if (exactView == null || exactView.Page == "overview") QueueScheduleSummaryRefresh();

            string page = exactView == null ? "overview" : exactView.Page;
            if (page == "settings") ucCaiDat.ExactSettings(model);

            // Chỉ đọc snapshot đã nạp trong grid. Khi SQL có thay đổi hoặc khi mở
            // trang Cách ly, grid được làm mới riêng (không truy vấn SQL mỗi 500 ms).
            if (exactView == null || page == "quarantine" || quarantineSnapshotDirty)
            {
                ucCachLy.ExactQuarantine(model);
                quarantineSnapshotDirty = false;
            }
            else if (exactView.Model != null && exactView.Model.Quarantine.Count > 0)
                model.Quarantine.AddRange(exactView.Model.Quarantine);

            if (page == "history") ucLichSu.ExactHistory(model);
            if (page == "details") (exactDetailSource ?? ucTongQuan).ExactDetail(model);
            UpdateNativeStatusStrip(model);
            return model;
        }
        private void ExactNavigate(string page)
        {
            // Khi người dùng mở tab Cách ly, luôn đọc lại dữ liệu mới nhất từ SQL.
            // Không phụ thuộc vào handle/Visible của controller legacy đang bị ẩn.
            if (page == "quarantine")
            {
                ucCachLy.RefreshData();
                quarantineSnapshotDirty = true;
            }
            if (page == "history")
            {
                // Nạp mới từ dbo.ScanHistory trước khi view dựng snapshot.
                // Không dựa vào OnVisibleChanged của controller đang ẩn.
                ucLichSu.ExactRefreshForSketch();
            }
            // Giao diện Sketch20 là view chính; không điều hướng pnlContent legacy bị ẩn
            // vì Clear/Add/ResumeLayout ở đó làm click bị trễ dù người dùng không nhìn thấy.
            if (page == "details") (exactDetailSource ?? ucTongQuan).ExactSelectDetailTab(exactView == null ? 0 : exactView.DetailTab);
            if (exactView != null) exactView.BringToFront();
        }
        private void ExactOpenFile(int index, bool quarantine)
        {
            if (quarantine)
            {
                if (exactDetailSource != null) exactDetailSource.Dispose();
                exactDetailSource = new UcTongQuan { Dock = DockStyle.Fill };
                pnlContent.Controls.Add(exactDetailSource); exactDetailSource.CreateControl();
                ucCachLy.ExactLoadQuarantineDetail(index, exactDetailSource);
            }
            else
            {
                string path = null;
                if (exactView != null && exactView.Model != null)
                {
                    var target = exactView.Model.Threats.FirstOrDefault(t => t.Index == index);
                    if (target != null) path = target.Path;
                }
                // Chỉ giữ nguồn chi tiết riêng khi người dùng đang chuyển giữa các tệp
                // của chính phiên lịch sử. Mở từ Dashboard phải quay về controller chính.
                bool historyDetail = exactDetailSource != null && exactView != null &&
                    exactView.Page == "details" && exactView.DetailReturnPage == "history";
                if (!historyDetail && exactDetailSource != null) { exactDetailSource.Dispose(); exactDetailSource = null; }
                (historyDetail ? exactDetailSource : ucTongQuan).ExactOpenThreat(index, path);
            }
        }
        private async void ExactHistoryDetails(int index)
        {
            var history = ucLichSu.ExactHistoryEntry(index); if (history == null) return;
            if (history.Threats == 0) { AppDialog.Show("Phiên quét không phát hiện mối đe dọa.\n" + history.Type + " · " + history.Files + " tệp · " + history.Time.ToString("dd/MM/yyyy HH:mm"), "Kết quả quét", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            try
            {
                DataTable data = await Task.Run(() => new ThreatDetectionRepository().GetByScan(history.ScanID));
                if (data.Rows.Count == 0) { AppDialog.Show("Không có bản ghi chi tiết từng tệp cho phiên quét này.", "Chi tiết lịch sử", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                if (exactDetailSource != null) exactDetailSource.Dispose();
                exactDetailSource = new UcTongQuan { Dock = DockStyle.Fill }; pnlContent.Controls.Add(exactDetailSource); exactDetailSource.CreateControl();
                exactDetailSource.ExactLoadHistory(data, history); exactView.DetailReturnPage = "history"; exactView.Go("details");
            }
            catch (Exception ex) { AppDialog.Show("Không tải được chi tiết phiên quét: " + ex.Message, "Lịch sử", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        private void ExactCommand(string command)
        {
            if (command != null && command.StartsWith("protection-config:", StringComparison.Ordinal))
            {
                ucBaoVe.ExactShowProtectionDetails(command.Substring("protection-config:".Length), this);
                if (exactView != null) exactView.RefreshModel(true);
                return;
            }
            if (command == "vt-config" || command == "save-key" || command == "clear-key" || command == "submit-files")
            {
                // Mở trực tiếp công cụ ở trang hiện tại, không chuyển sang Quản trị.
                ucCaiDat.ExactSettingsCommand(command);
                return;
            }
            if (command == "vt-upload")
            {
                // Không yêu cầu Admin; GuiMauVirusTotalTheoPath vẫn buộc xác nhận
                // trước khi chuyển NỘI DUNG tệp ra VirusTotal.
                (exactDetailSource ?? ucTongQuan).ExactScanCommand("vt-upload");
                return;
            }
            if (command == "schedule")
            {
                ucCaiDat.ExactOpenScheduleManager();
                QueueScheduleSummaryRefresh(true);
                return;
            }
            if (command == "scan")
            {
                bool alreadyRunning = ucTongQuan.ExactScanning;
                ucTongQuan.ExactQuickScan();
                if (!alreadyRunning && ucTongQuan.ExactScanning)
                {
                    ShowNativeScanningStatus();
                }
                return;
            }
            if (command == "enable") { ucBaoVe.ExactSetFlag("rt", true); return; }
            if (command == "cancel" || command == "isolate")
            {
                ucTongQuan.ExactScanCommand(command);
                if (command == "cancel")
                    ProtectionNotificationService.Publish("Yêu cầu dừng quét", "Đang dừng phiên quét hiện tại.",
                        ProtectionNoticeLevel.Info, "scan-cancel", 3, true);
                return;
            }
            if (command == "isolate-file" || command == "delete-file" || command == "vt-query")
            {
                (exactDetailSource ?? ucTongQuan).ExactScanCommand(command);
                if (command == "vt-query")
                    ProtectionNotificationService.Publish("Tra cứu VirusTotal", "Đã gửi yêu cầu tra cứu SHA-256. Kết quả sẽ hiển thị trong Chi tiết tệp.",
                        ProtectionNoticeLevel.Info, "vt-manual-query", 3, true);
                return;
            }
            if (command == "refresh" || command == "restore" || command == "delete") { ucCachLy.ExactQuarantineCommand(command); return; }
            if (command == "export" || command == "history-refresh" || command == "history-prev" || command == "history-next" || command.StartsWith("history-size:", StringComparison.Ordinal) || command.StartsWith("history-page:", StringComparison.Ordinal)) { ucLichSu.ExactHistoryCommand(command); return; }
            if (command.StartsWith("history-detail:")) { int index; if (int.TryParse(command.Substring(15), out index)) ExactHistoryDetails(index); return; }
            if (command == "update")
            {
                ScanEngine.ClearScanCache(); ScanHistoryStore.MarkSignatureUpdated(DateTime.Now);
                ucBaoVe.ExactRefresh();
                return;
            }
            ucCaiDat.ExactSettingsCommand(command);
        }
    }
}
