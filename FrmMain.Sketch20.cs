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
        private void InitializeSketch20()
        {
            // Giữ các controller hiện có sống trên UI thread; lớp mới là toàn bộ giao diện hiển thị.
            // Bộ kiểm tra hợp đồng cũ có thể kiểm tra controller mà không phủ view lên control cũ.
            if (Environment.GetEnvironmentVariable("XVIRUS_LEGACY_UI_TEST") == "1") return;
            EnableSketch20();
        }
        internal void EnableSketch20()
        {
            if (exactView != null) return;
            ClientSize = new Size(1240, 800); MinimumSize = new Size(1000, 690);
            Padding = new Padding(1); BackColor = Sketch20Style.Border;
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
                StartAdvanced = ucTongQuan.ExactStartAdvanced
            };
            exactView = new Sketch20View(ExactModel(), actions, ExactModel) { Name = "Exact20Screens", Dock = DockStyle.None,
                Bounds = new Rectangle(1, 1, ClientSize.Width - 2, ClientSize.Height - 2), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            Controls.Add(exactView); exactView.BringToFront();
            // Controller cũ vẫn nhận Load/event và giữ dữ liệu, nhưng không có vùng vẽ phía dưới view mới.
            // WinForms native có thể in các sibling này khi dựng nền control trong suốt.
            foreach (var host in new[] { pnlSidebar, pnlContent })
            {
                var region = new Region(); region.MakeEmpty(); host.Region = region;
            }
            FormClosed += delegate { if (exactDetailSource != null && !exactDetailSource.IsDisposed) exactDetailSource.Dispose(); };
        }
        private Sketch20Model ExactModel()
        {
            var model = new Sketch20Model { Running = RealTimeProtection.IsRunning, Version = GetType().Assembly.GetName().Version.ToString(3) };
            foreach (string key in Sketch20View.FeatureKeys.Concat(new[] { "alerts", "update" })) model.Flags[key] = UcBaoVe.ExactFlag(key);
            model.KeyConfigured = VirusTotalClient.IsConfigured;
            model.Signature = ucBaoVe.ExactSignature;
            ucTongQuan.ExactOverview(model); ucCaiDat.ExactSettings(model); ucLichSu.ExactHistory(model); ucCachLy.ExactQuarantine(model);
            (exactDetailSource ?? ucTongQuan).ExactDetail(model);
            return model;
        }
        private void ExactNavigate(string page)
        {
            Navigate(page == "protection" ? btnBaoVe : page == "history" ? btnLichSu : page == "quarantine" ? btnCachLy : page == "settings" ? btnCaiDat : btnTongQuan);
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
            else { if (exactDetailSource != null) { exactDetailSource.Dispose(); exactDetailSource = null; } ucTongQuan.ExactOpenThreat(index); }
        }
        private async void ExactHistoryDetails(int index)
        {
            var history = ucLichSu.ExactHistoryEntry(index); if (history == null) return;
            if (history.Threats == 0) { MessageBox.Show("Phiên quét không phát hiện mối đe dọa.\n" + history.Type + " · " + history.Files + " tệp · " + history.Time.ToString("dd/MM/yyyy HH:mm"), "Kết quả quét", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            try
            {
                DataTable data = await Task.Run(() => new ThreatDetectionRepository().GetByScan(history.ScanID));
                if (data.Rows.Count == 0) { MessageBox.Show("Không có bản ghi chi tiết từng tệp cho phiên quét này.", "Chi tiết lịch sử", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                if (exactDetailSource != null) exactDetailSource.Dispose();
                exactDetailSource = new UcTongQuan { Dock = DockStyle.Fill }; pnlContent.Controls.Add(exactDetailSource); exactDetailSource.CreateControl();
                exactDetailSource.ExactLoadHistory(data, history); exactView.DetailReturnPage = "history"; exactView.Go("details");
            }
            catch (Exception ex) { MessageBox.Show("Không tải được chi tiết phiên quét: " + ex.Message, "Lịch sử", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        private void ExactCommand(string command)
        {
            if (command == "scan") { ucTongQuan.ExactQuickScan(); return; }
            if (command == "enable") { ucBaoVe.ExactSetFlag("rt", true); return; }
            if (command == "cancel" || command == "isolate") { ucTongQuan.ExactScanCommand(command); return; }
            if (command == "isolate-file" || command == "delete-file" || command == "vt-query") { (exactDetailSource ?? ucTongQuan).ExactScanCommand(command); return; }
            if (command == "refresh" || command == "restore" || command == "delete") { ucCachLy.ExactQuarantineCommand(command); return; }
            if (command == "export" || command == "history-prev" || command == "history-next") { ucLichSu.ExactHistoryCommand(command); return; }
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
