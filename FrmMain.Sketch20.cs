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
            nativeStatusText.Text = compact
                ? (model.Running ? "Bảo vệ: Bật" : "Bảo vệ: Tắt") + "   |   " + state
                : "Bảo vệ thời gian thực: " + (model.Running ? "Bật" : "Tắt") + "   |   " + state;
            nativeStatusSignature.Text = "CSDL chữ ký: " + (string.IsNullOrWhiteSpace(model.Signature) ? "—" : model.Signature);
            nativeStatusSignature.Visible = !compact;
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
                SetFlag = (key, enabled) => { if (key == "autostart" || key == "samples") ucCaiDat.ExactSetGeneral(key, enabled); else ucBaoVe.ExactSetFlag(key, enabled); },
                SaveKey = key => ucCaiDat.ExactSettingsCommand("save-key", key),
                SelectQuarantine = ucCachLy.ExactPickQuarantine,
                SelectAllQuarantine = ucCachLy.ExactPickAllQuarantine,
                OpenFile = ExactOpenFile,
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
            // Ép WinForms tính lại Dock sau khi status strip + exact view đã được thêm.
            PerformLayout();
            FormClosed += delegate { if (exactDetailSource != null && !exactDetailSource.IsDisposed) exactDetailSource.Dispose(); };
        }
        private Sketch20Model ExactModel()
        {
            var model = new Sketch20Model { Running = RealTimeProtection.IsRunning, Version = GetType().Assembly.GetName().Version.ToString(3) };
            foreach (string key in Sketch20View.FeatureKeys.Concat(new[] { "alerts", "update" })) model.Flags[key] = UcBaoVe.ExactFlag(key);
            model.KeyConfigured = VirusTotalClient.IsConfigured;
            model.Signature = ucBaoVe.ExactSignature;
            ucTongQuan.ExactOverview(model);

            string page = exactView == null ? "overview" : exactView.Page;
            if (page == "settings") ucCaiDat.ExactSettings(model);

            // Không đọc lại toàn bộ grid cách ly ở mỗi tick quét. Giữ snapshot hiện có
            // cho các màn không cần dữ liệu cách ly chi tiết; chỉ refresh thật khi mở Cách ly.
            if (exactView == null || page == "quarantine") ucCachLy.ExactQuarantine(model);
            else if (exactView.Model != null && exactView.Model.Quarantine.Count > 0)
                model.Quarantine.AddRange(exactView.Model.Quarantine);

            if (page == "history") ucLichSu.ExactHistory(model);
            if (page == "details") (exactDetailSource ?? ucTongQuan).ExactDetail(model);
            UpdateNativeStatusStrip(model);
            return model;
        }
        private void ExactNavigate(string page)
        {
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
            if (command == "scan") { ucTongQuan.ExactQuickScan(); if (ucTongQuan.ExactScanning) ShowNativeScanningStatus(); return; }
            if (command == "enable") { ucBaoVe.ExactSetFlag("rt", true); return; }
            if (command == "cancel" || command == "isolate") { ucTongQuan.ExactScanCommand(command); return; }
            if (command == "isolate-file" || command == "delete-file" || command == "vt-query") { (exactDetailSource ?? ucTongQuan).ExactScanCommand(command); return; }
            if (command == "refresh" || command == "restore" || command == "delete") { ucCachLy.ExactQuarantineCommand(command); return; }
            if (command == "export" || command == "history-refresh" || command == "history-prev" || command == "history-next" || command.StartsWith("history-delete:", StringComparison.Ordinal)) { ucLichSu.ExactHistoryCommand(command); return; }
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
