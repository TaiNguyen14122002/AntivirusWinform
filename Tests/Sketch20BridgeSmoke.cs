// Chỉ dùng tệp kiểm thử trong thư mục riêng. Không truy cập SQL/API của người dùng.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ScanAndRemoveVirus;
using ScanAndRemoveVirus.Control;
using ScanAndRemoveVirus.Database;
using ScanAndRemoveVirus.Services;

internal static partial class Sketch20Smoke
{
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr handle, StringBuilder value, int length);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);
    private delegate bool VisitWindow(IntPtr handle, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(VisitWindow visit, IntPtr parameter);
    [DllImport("user32.dll")] private static extern IntPtr GetDlgItem(IntPtr handle, int id);
    private static T Read<T>(object target, string name) { return (T)target.GetType().GetField(name, Private).GetValue(target); }
    private static void Click(Sketch20View view, string name) { var control = view.Controls.Find(name, true).FirstOrDefault() as Button; if (control == null) control = view.Canvas.Controls[name] as Button; if (control == null) throw new InvalidOperationException("Không tìm thấy control: " + name); control.PerformClick(); Pump(); }
    private static void WaitScan(UcTongQuan scan)
    { var watch = Stopwatch.StartNew(); while (scan.ExactScanning && watch.ElapsedMilliseconds < 15000) Pump(30); Pump(300); }
    private static void LiveShot(Form form, string path)
    {
        using (var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height, PixelFormat.Format24bppRgb))
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix) using (var g = Graphics.FromImage(bitmap)) g.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, bitmap.Size);
            else form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(path, ImageFormat.Png);
        }
    }
    private static void VerifyBridge(string output)
    {
        string folder = Path.Combine(output, "bridge-data"); Directory.CreateDirectory(folder);
        Environment.SetEnvironmentVariable("XVIRUS_DATA_DIR", folder);
        Environment.SetEnvironmentVariable("XVIRUS_LEGACY_UI_TEST", "0");
        var apiPath = typeof(VirusTotalClient).GetField("cachedApiKeyPath", BindingFlags.NonPublic | BindingFlags.Static);
        object previousApiPath = apiPath.GetValue(null);
        apiPath.SetValue(null, Path.Combine(folder, "isolated-test-api-key.txt"));
        typeof(DbConnection).GetField("ConnectionString", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, "Sketch20SmokeIsolation=true");
        var settings = AppSettings.Load(); settings.RealTimeOnPersist = settings.UsbProtection = settings.DownloadProtection = settings.BehaviorWatch = settings.StartupFoldersWatch = settings.AutoUpdate = settings.VtAutoQuery = false;
        AppSettings.Save(settings);
        var closeDialogs = new System.Windows.Forms.Timer { Interval = 80 };
        closeDialogs.Tick += delegate
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                EnumWindows((handle, parameter) =>
                {
                    uint processId; GetWindowThreadProcessId(handle, out processId);
                    var name = new StringBuilder(64); GetClassName(handle, name, name.Capacity);
                    if (processId == (uint)Process.GetCurrentProcess().Id && name.ToString() == "#32770")
                        PostMessage(handle, 0x0111, new IntPtr(GetDlgItem(handle, 1) != IntPtr.Zero ? 1 : 2), IntPtr.Zero);
                    return true;
                }, IntPtr.Zero);
                return;
            }
            foreach (Form dialog in Application.OpenForms.Cast<Form>().ToArray())
            { if (!dialog.Modal || dialog is FrmMain) continue; dialog.DialogResult = DialogResult.OK; dialog.Close(); }
        };
        closeDialogs.Start();
        try
        {
            using (var form = new FrmMain())
            {
                form.Show(); Pump(450);
                var view = form.exactView; var scan = Read<UcTongQuan>(form, "ucTongQuan");
                Check("Ứng dụng mở mặc định bằng giao diện 20 màn", view != null && view.Visible && !view.Model.Sample);
                var nativeStatus = form.Controls["mainStatusStrip"] as StatusStrip;
                var legacySidebar = Read<Panel>(form, "pnlSidebar");
                var legacyContent = Read<Panel>(form, "pnlContent");
                bool layoutOk = nativeStatus != null && view.Left == 0 && view.Top == 0 && view.Width == form.ClientSize.Width && view.Bottom == nativeStatus.Top && !legacySidebar.Visible && !legacyContent.Visible;
                if (!layoutOk) Console.WriteLine("LAYOUT DIAG: client=" + form.ClientSize + "; view=" + view.Bounds + "; status=" + (nativeStatus == null ? "null" : nativeStatus.Bounds.ToString()) + "; sidebarVisible=" + legacySidebar.Visible + "; contentVisible=" + legacyContent.Visible + "; padding=" + form.Padding);
                Check("Giao diện phủ đủ vùng ứng dụng, không còn khoảng dư bên trái", layoutOk);
                Check("Thanh trạng thái là WinForms StatusStrip thuần", nativeStatus != null && nativeStatus.RenderMode == ToolStripRenderMode.System && nativeStatus.Dock == DockStyle.Bottom);
                Check("Ứng dụng thật không dùng số liệu minh họa", view.Model.ScannedFiles == 0 && view.Model.Quarantine.Count == 0 && view.Model.Detail == null);
                Check("Trạng thái bảo vệ tắt lấy từ service", view.ScreenNumber == 20 && !RealTimeProtection.IsRunning);
                Click(view, "nav-settings");
                Check("Bánh răng góc phải mở cài đặt thật", view.Page == "settings" && view.Canvas.Controls["tab-0"] != null);
                var settingsNav = view.Controls.Find("nav-settings", true).FirstOrDefault() as Sketch20Button;
                Check("Bánh răng cố định ở góc phải giữ nhãn truy cập và trạng thái đang chọn", settingsNav != null && settingsNav.Parent == view.Canvas && settingsNav.Text.Length == 0 && settingsNav.IconName == "settings" && settingsNav.AccessibleName == "Cài đặt" && settingsNav.Selected && settingsNav.Left > view.Canvas.Width - 230);
                view.Go("protection"); Pump();
                var protection = Read<UcBaoVe>(form, "ucBaoVe"); protection.ExactSetFlag("aq", false); view.RefreshModel(true);
                ((Sketch20Switch)view.Canvas.Controls["switch-aq"]).Checked = true; Pump();
                Check("Công tắc mới điều khiển AutoQuarantine thật", RealTimeProtection.AutoQuarantine && view.Model.Flag("aq"));
                ((Sketch20Switch)view.Canvas.Controls["switch-aq"]).Checked = false; Pump();
                ((Sketch20Switch)view.Canvas.Controls["switch-vtq"]).Checked = true; Pump();
                Check("Chưa có API key dẫn tới màn cấu hình", view.Page == "settings" && view.SettingsTab == 2 && !FeatureFlags.VtAutoQuery);
                view.SettingsTab = 1; view.Go("settings");
                ((Sketch20Switch)view.Canvas.Controls["switch-restore"]).Checked = false; Pump();
                Check("Cài đặt bảo vệ đồng bộ cờ của service", !FeatureFlags.FileRestoreGuard && !view.Model.Flag("restore"));
                form.MoCaiDatVirusTotal(); Pump(); Check("API điều hướng VirusTotal mở tab mới", view.Page == "settings" && view.SettingsTab == 2);
                form.MoTabLichSu(); Pump(); Check("API điều hướng lịch sử mở màn mới", view.Page == "history");
                form.MoTabCachLy(); Pump(); Check("API điều hướng cách ly mở màn mới", view.Page == "quarantine");
                string samples = Path.Combine(folder, "scan-fixtures"); Directory.CreateDirectory(samples);
                File.WriteAllText(Path.Combine(samples, "clean.txt"), "A clean document.");
                File.WriteAllText(Path.Combine(samples, "signature.txt"), ScanEngine.TestSignature + "sample");
                File.WriteAllText(Path.Combine(samples, "demo_eicar.bin"), "harmless sample");
                File.WriteAllText(Path.Combine(samples, "invoice.pdf.exe"), "harmless double-extension sample");
                Directory.CreateDirectory(Path.Combine(samples, "nested")); File.WriteAllText(Path.Combine(samples, "nested/other.txt"), "A clean nested document.");
                view.SelectScreen(15); typeof(Sketch20View).GetField("folders", Private).SetValue(view, new List<string> { samples }); view.Render();
                var recursive = view.Canvas.Controls.OfType<Sketch20Check>().Single(); recursive.Checked = false;
                ((Button)view.Canvas.Controls["start-advanced"]).PerformClick();
                Check("Nút bắt đầu mới khởi chạy engine và màn đang quét", scan.ExactScanning && view.ScreenNumber == 18);
                var statusText = Read<ToolStripStatusLabel>(form, "nativeStatusText");
                Check("StatusStrip cập nhật trạng thái đang quét", scan.ExactScanning && statusText != null && statusText.Text.Contains("Đang quét"));
                var scanViewport = view.Controls.Find("Sketch20Viewport", true).FirstOrDefault() as Panel;
                Check("Tổng quan đang quét không hiện scrollbar", scanViewport != null && !scanViewport.AutoScroll && !scanViewport.VerticalScroll.Visible);
                view.RefreshModel(true);
                Pump(120);
                Check("Refresh tiến trình không bật lại scrollbar", !scan.ExactScanning || (scanViewport != null && !scanViewport.AutoScroll && !scanViewport.VerticalScroll.Visible));
                WaitScan(scan); view.RefreshModel(true);
                Check("Quét thư mục không đệ quy kiểm tra đúng 4 tệp", !scan.ExactScanning && view.Model.ScannedFiles == 4);
                Check("Kết quả thật hiện 3 đe dọa cùng tên và lý do", view.ScreenNumber == 19 && view.Model.Threats.Count == 3 && view.Model.Threats.Any(t => t.Name == "invoice.pdf.exe") && !view.Model.Threats.Any(t => t.Name == "invoice_update.exe"));
                LiveShot(form, Path.Combine(output, "21-live-threats.png"));
                Click(view, "threat-detail-0");
                Check("Xem từ bảng mở đúng tệp trong phiên thật", view.Page == "details" && view.Model.Detail != null && view.Model.Detail.Path.StartsWith(samples));
                string[] detailLabels = { "VirusTotal", "Hoạt động", "Chuỗi ký tự", "Chi tiết" };
                for (int tab = 1; tab <= 4; tab++)
                { string label = detailLabels[tab - 1]; var targetTab = view.Canvas.Controls.OfType<Sketch20Button>().Single(b => b.Tab && b.Text == label); Click(view, targetTab.Name); Check("Tab thật " + tab + " nạp dữ liệu controller", Read<TabControl>(scan, "tabChiTietDong").SelectedIndex == tab && view.DetailTab == tab); }
                Check("Thông tin bổ sung giữ SHA256 thực", view.Model.Detail.Sha256 != null && view.Model.Detail.Sha256.Length == 64);
                LiveShot(form, Path.Combine(output, "22-live-metadata.png"));
                Click(view, "details-back"); Check("Quay lại từ chi tiết giữ danh sách phiên quét", view.Page == "overview" && view.Model.Threats.Count == 3);
                Click(view, "isolate-all");
                Check("SQL lỗi giữ tệp và kết quả, không báo cách ly thành công", view.Model.Threats.Count == 3 && File.Exists(Path.Combine(samples, "signature.txt")));
                view.Go("quarantine"); Pump(300); view.RefreshModel(true);
                Check("Cách ly hiển thị lỗi kết nối thay vì dữ liệu mẫu", view.Model.Quarantine.Count == 0 && !string.IsNullOrEmpty(view.Model.QuarantineNotice));
                LiveShot(form, Path.Combine(output, "23-live-quarantine-unavailable.png"));
                string clean = Path.Combine(folder, "clean-fixtures"); Directory.CreateDirectory(clean); File.WriteAllText(Path.Combine(clean, "notes.txt"), "A clean document.");
                scan.GetType().GetMethod("SetCustomPath", Private).Invoke(scan, new object[] { clean }); view.Go("overview");
                Click(view, "quick-scan"); WaitScan(scan); view.RefreshModel(true);
                Check("Nút quét lại thật xóa kết quả phiên cũ khi tệp sạch", view.Model.Threats.Count == 0 && view.Model.ScannedFiles == 1 && !view.Model.Scanning);
                string cancelFolder = Path.Combine(folder, "cancel-fixtures"); Directory.CreateDirectory(cancelFolder);
                for (int i = 0; i < 600; i++) File.WriteAllText(Path.Combine(cancelFolder, "notes-" + i + ".txt"), new string('x', 4096));
                view.SelectScreen(16); typeof(Sketch20View).GetField("files", Private).SetValue(view, Directory.GetFiles(cancelFolder).ToList()); view.Render();
                ((Button)view.Canvas.Controls["start-advanced"]).PerformClick();
                ((Button)view.Canvas.Controls["cancel-scan"]).PerformClick(); WaitScan(scan); view.RefreshModel(true);
                if (scan.ExactScanning || !view.Model.Status.Contains("Đã hủy"))
                    Console.WriteLine("Cancel diagnostic: scanning=" + scan.ExactScanning + "; status=" + view.Model.Status);
                Check("Nút hủy mới ngừng phiên quét của engine", !scan.ExactScanning && view.Model.Status.Contains("Đã hủy"));
                form.ClientSize = new Size(1040, 720); Pump();
                nativeStatus = form.Controls["mainStatusStrip"] as StatusStrip;
                Check("Ứng dụng thật co giãn đủ vùng cửa sổ", nativeStatus != null && view.Left == 0 && view.Width == form.ClientSize.Width && view.Bottom == nativeStatus.Top);
                form.Close();
            }
        }
        catch (Exception ex) { Check("Luồng tích hợp chạy đến cuối: " + ex, false); }
        finally { closeDialogs.Stop(); closeDialogs.Dispose(); foreach (var key in new[] { "usb", "download", "behavior", "startup" }) GuardService.Configure(key, false); RealTimeProtection.Stop(); apiPath.SetValue(null, previousApiPath); }
    }
}
