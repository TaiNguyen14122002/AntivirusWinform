// Test UI đầu-cuối: dựng UserControl thật, BẤM NÚT thật, dialog tự đóng bằng closer thread.
// Chạy (từ thư mục ScanAndRemoveVirus):
// csc /out:ui_test.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll
//     ScanAndRemoveVirus\Services\ScanEngine.cs ScanAndRemoveVirus\Services\RealTimeProtection.cs
//     ScanAndRemoveVirus\Services\QuarantineLedger.cs ScanAndRemoveVirus\Services\ScanHistoryStore.cs
//     ScanAndRemoveVirus\Services\VirusTotalClient.cs
//     ScanAndRemoveVirus\Control\Theme.cs
//     ScanAndRemoveVirus\Control\UcTongQuan.cs ScanAndRemoveVirus\Control\UcTongQuan.Designer.cs
//     ScanAndRemoveVirus\Control\UcCachLy.cs  ScanAndRemoveVirus\Control\UcCachLy.Designer.cs
//     ScanAndRemoveVirus\Control\UcLichSu.cs  ScanAndRemoveVirus\Control\UcLichSu.Designer.cs
//     ScanAndRemoveVirus\Control\UcBaoVe.cs   ScanAndRemoveVirus\Control\UcBaoVe.Designer.cs
//     Tests\UiEndToEnd.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ScanAndRemoveVirus.Control;
using ScanAndRemoveVirus.Services;

static class UiEndToEnd
{
    static int failures;
    static volatile bool stopCloser;

    static void Check(string name, bool cond)
    {
        Console.WriteLine("{0}: {1}", cond ? "PASS" : "FAIL", name);
        if (!cond) failures++;
    }

    // ---------- tự đóng MessageBox ----------
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr l);
    delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string win);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    const uint BM_CLICK = 0xF5;

    static void CloserLoop()
    {
        while (!stopCloser)
        {
            EnumWindows(delegate (IntPtr h, IntPtr l)
            {
                var cls = new StringBuilder(64);
                GetClassName(h, cls, 64);
                if (cls.ToString() != "#32770") return true;
                var preferred = IntPtr.Zero; var fallback = IntPtr.Zero;
                for (IntPtr btn = FindWindowEx(h, IntPtr.Zero, "Button", null);
                     btn != IntPtr.Zero; btn = FindWindowEx(h, btn, "Button", null))
                {
                    var t = new StringBuilder(64);
                    GetWindowText(btn, t, 64);
                    string text = t.ToString();
                    if (text.StartsWith("Có") || text.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                        { if (preferred == IntPtr.Zero) preferred = btn; }
                    else if (fallback == IntPtr.Zero) fallback = btn;
                }
                IntPtr pick = preferred != IntPtr.Zero ? preferred : fallback;
                if (pick != IntPtr.Zero) PostMessage(pick, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                return true;
            }, IntPtr.Zero);
            Thread.Sleep(150);
        }
    }

    // ---------- reflection helpers ----------
    static T F<T>(object o, string name)
    {
        return (T)o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);
    }
    static void SetF(object o, string name, object val)
    {
        o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(o, val);
    }

    static void Pump(Action doWork)
    {
        var sw = Stopwatch.StartNew();
        doWork();
        while (sw.Elapsed.TotalSeconds < 40 &&
               (bool)F<object>(PumpTarget, "isScanning") == true)
        { Application.DoEvents(); Thread.Sleep(15); }
    }
    static object PumpTarget;

    static void PumpMs(int ms)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < ms)
        { Application.DoEvents(); Thread.Sleep(15); }
    }

    static int Main()
    {
        var closer = new Thread(CloserLoop) { IsBackground = true };
        closer.Start();

        var th = new Thread(Run);
        th.SetApartmentState(ApartmentState.STA);
        th.Start();
        th.Join(TimeSpan.FromSeconds(150));
        stopCloser = true;
        if (th.IsAlive) { Console.WriteLine("FAIL: test treo"); return 1; }

        Console.WriteLine(failures == 0 ? "== UI E2E ALL PASSED ==" : "== " + failures + " UI E2E FAILED ==");
        return failures == 0 ? 0 : 1;
    }

    static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "xvirus-ui-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        int qBefore = ScanEngine.CountQuarantined();
        try
        {
            using (var form = new Form())
            using (var uc = new UcTongQuan())
            {
                form.Controls.Add(uc); uc.Dock = DockStyle.Fill;
                form.Size = new Size(1400, 900); form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-2000, -2000); // không chiếm màn hình
                form.Show(); Application.DoEvents();

                // ===== 1. Tổng quan: quét tùy chọn 1 thư mục có 2 đe dọa =====
                File.WriteAllText(Path.Combine(root, "drop.txt"), ScanEngine.TestSignature + "ui-e2e-payload!!");
                File.WriteAllText(Path.Combine(root, "qr_eicar.dat"), "hello");
                var rdoCustom = F<RadioButton>(uc, "rdoCustomScan");
                rdoCustom.Checked = true;
                SetF(uc, "customScanPath", root);
                PumpTarget = uc;
                F<Button>(uc, "btnScanNow").PerformClick();
                Pump(delegate { });
                PumpMs(700);

                var dgv = F<DataGridView>(uc, "dgvActions");
                Check("[UI] bảng Hành động nhận 2 đe dọa", dgv.Rows.Count == 2);
                Check("[UI] cột đe dọa ghi rõ loại", dgv.Rows.Cast<DataGridViewRow>()
                    .All(r => Convert.ToString(r.Cells[1].Value).Contains("Chữ ký")));
                Check("[UI] thống kê: số lượng = 2", F<Label>(uc, "lblThreatCount").Text == "2");
                Check("[UI] thống kê: tệp đã quét = 2", F<Label>(uc, "lblScannedCount").Text.Trim() == "2");
                Check("[UI] progress hoàn tất", F<Label>(uc, "lblScanProgress").Text.StartsWith("Hoàn tất"));
                Check("[UI] history ghi phiên quét", ScanHistoryStore.Entries().Any(h =>
                    h.Type == "Quét tùy chọn" && h.Threats == 2 && h.Scope.Contains(root)));

                // ===== 2. Cách ly tất cả từ nút UI =====
                F<Button>(uc, "btnQuarantineAll").PerformClick();
                PumpMs(1200);
                Check("[UI] cách ly hết -> bảng trống", dgv.Rows.Count == 0);
                Check("[UI] tệp sạch chỗ cũ", !File.Exists(Path.Combine(root, "drop.txt"))
                    && !File.Exists(Path.Combine(root, "qr_eicar.dat")));
                Check("[UI] đếm cách ly tăng đúng 2 và label cập nhật",
                    ScanEngine.CountQuarantined() == qBefore + 2
                    && F<Label>(uc, "lblQuarantineCount").Text == (qBefore + 2).ToString());

                // ===== 3. Cách khác: xóa vĩnh viễn từ nút UI =====
                File.WriteAllText(Path.Combine(root, "kill1.txt"), ScanEngine.TestSignature + "delete-me-please!!");
                F<Button>(uc, "btnScanNow").PerformClick();
                Pump(delegate { });
                PumpMs(500);
                Check("[UI] quét lại bắt tệp mới (1 dòng)", dgv.Rows.Count == 1);
                F<Button>(uc, "btnDeleteAll").PerformClick();
                PumpMs(1400);
                Check("[UI] Xóa tất cả: xác nhận Yes -> tệp bị xóa vĩnh viễn",
                    dgv.Rows.Count == 0 && !File.Exists(Path.Combine(root, "kill1.txt")));
                Check("[UI] xóa vĩnh viễn không vào khu cách ly",
                    ScanEngine.CountQuarantined() == qBefore + 2);

                // ===== 3b. Radio loại trừ lẫn nhau + logic sau dialog chọn đường dẫn =====
                var qkR = F<RadioButton>(uc, "rdoQuickScan");
                var flR = F<RadioButton>(uc, "rdoFullScan");
                var cmR = F<RadioButton>(uc, "rdoCustomScan");
                flR.Checked = true;
                Check("[UI] radio: bật Full tự tắt cái khác", !qkR.Checked && !cmR.Checked && flR.Checked);
                cmR.Checked = true;
                Check("[UI] radio: bật Custom tự tắt Full", !qkR.Checked && !flR.Checked && cmR.Checked);
                // Nút Chọn tệp/Chọn thư mục mở dialog HỆ THỐNG (không auto được) -> test logic sau dialog:
                typeof(UcTongQuan).GetMethod("SetCustomPath", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(uc, new object[] { Path.Combine(root, "sau-dialog.txt") });
                Check("[UI] SetCustomPath: tự chọn Custom + hiện đường dẫn + bật picker",
                    cmR.Checked && F<Label>(uc, "lblCustomPath").Text == Path.Combine(root, "sau-dialog.txt")
                    && F<Button>(uc, "btnPickFile").Enabled);

                // ===== 3c. Hủy quét giữa chừng trên UI (engine đã stress x8, đây là luồng nút) =====
                string cancelDir = Path.Combine(root, "cancel-ui");
                for (int cd = 0; cd < 30; cd++)
                {
                    string dd = Directory.CreateDirectory(Path.Combine(cancelDir, "sub" + cd)).FullName;
                    for (int cf = 0; cf < 110; cf++)
                        File.WriteAllText(Path.Combine(dd, "c" + cf + ".log"), new string('y', 60));
                }
                SetF(uc, "customScanPath", cancelDir);
                cmR.Checked = true;
                F<Button>(uc, "btnScanNow").PerformClick();
                PumpMs(350); // đang quét dở
                Check("[UI] nút đổi thành 'Hủy quét' khi đang quét", F<Button>(uc, "btnScanNow").Text == "Hủy quét");
                F<Button>(uc, "btnScanNow").PerformClick();
                var cUntil = DateTime.Now.AddSeconds(25);
                while (DateTime.Now < cUntil && (bool)F<object>(uc, "isScanning"))
                { Application.DoEvents(); Thread.Sleep(15); }
                PumpMs(600); // cho MessageBox + finally kịp chạy
                Check("[UI] bấm Hủy -> phiên dừng, nhãn báo đã hủy, nút về 'Quét ngay'",
                    !(bool)F<object>(uc, "isScanning")
                    && F<Label>(uc, "lblScanProgress").Text.Contains("hủy")
                    && F<Button>(uc, "btnScanNow").Text == "Quét ngay");

                // ===== 3d. Nút Tra VirusTotal (key giả -> đi hết luồng xử lý, 401 hay lỗi mạng đều được) =====
                string vtKeyBak = File.Exists(VirusTotalClient.ApiKeyPath)
                    ? File.ReadAllText(VirusTotalClient.ApiKeyPath) : null;
                try
                {
                    File.WriteAllText(Path.Combine(root, "vtprobe.txt"), ScanEngine.TestSignature + "vt button flow!!");
                    SetF(uc, "customScanPath", Path.Combine(root, "vtprobe.txt"));
                    cmR.Checked = true;
                    F<Button>(uc, "btnScanNow").PerformClick();
                    PumpTarget = uc;
                    Pump(delegate { });
                    PumpMs(600);
                    var agd = F<DataGridView>(uc, "dgvActions");
                    Check("[UI] scan tệp đơn cho VT-probe: 1 dòng", agd.Rows.Count == 1);
                    Directory.CreateDirectory(Path.GetDirectoryName(VirusTotalClient.ApiKeyPath));
                    File.WriteAllText(VirusTotalClient.ApiKeyPath, "invalid-key-for-flow-test");
                    agd.Rows[0].Selected = true;
                    F<Button>(uc, "btnVirusTotal").PerformClick();
                    var vUntil = DateTime.Now.AddSeconds(50); // timeout HTTP 30s vẫn nằm trong luồng
                    while (DateTime.Now < vUntil && !F<Label>(uc, "lblScanProgress").Text.StartsWith("VirusTotal"))
                    { Application.DoEvents(); Thread.Sleep(25); }
                    Check("[UI] Tra VirusTotal: chạy hết luồng, cập nhật nhãn",
                        F<Label>(uc, "lblScanProgress").Text.StartsWith("VirusTotal"));
                    string threatCell = Convert.ToString(agd.Rows[0].Cells[1].Value);
                    Check("[UI] Tra VirusTotal: cột đe dọa hoặc cập nhật VT hoặc giữ lý do gốc khi lỗi mạng/401",
                        threatCell.StartsWith("VirusTotal") || threatCell.Contains("Chữ ký"));
                }
                finally
                {
                    if (vtKeyBak == null) { try { File.Delete(VirusTotalClient.ApiKeyPath); } catch { } }
                    else File.WriteAllText(VirusTotalClient.ApiKeyPath, vtKeyBak);
                }
                qkR.Checked = true;
                SetF(uc, "customScanPath", null);

                // ===== 4. Kiểm tra cập nhật -> danh hiệu DB + entry history =====
                F<Button>(uc, "btnCheckUpdate").PerformClick();
                PumpMs(600);
                Check("[UI] nút cập nhật: nhãn DB đổi 'Đã cập nhật'",
                    F<Label>(uc, "lblDatabaseValue").Text == "Đã cập nhật");
                Check("[UI] history ghi sự kiện cập nhật CSDL",
                    ScanHistoryStore.Entries().Any(h => h.Type == "Cập nhật CSDL"));

                // ===== 5. Tab Cách ly: hiện đúng tệp, Restore trả tệp về =====
                // Lưu ý: phải có handle (đặt vào form đang hiển thị) thì TabControlSelectedIndex
                // + BeginInvoke sự kiện mới hoạt động như trong app thật.
                var q = new UcCachLy();
                q.Dock = DockStyle.Fill;
                form.Controls.Add(q);
                uc.BringToFront(); // q nhận handle nhưng uc vẫn là trang người dùng "đang mở"
                Application.DoEvents(); q.BringToFront(); Application.DoEvents(); uc.BringToFront();
                PumpMs(300);
                q.RefreshData();
                var mineRows = dgvQ(q).Rows.Cast<DataGridViewRow>()
                    .Where(r => Convert.ToString(r.Cells[1].Value) != null
                        && Convert.ToString(r.Cells[1].Value).StartsWith(root)).ToList();
                Check("[UI] tab Cách ly liệt kê 2 tệp test", mineRows.Count == 2);
                Check("[UI] dòng Cách ly có lý do từ bảng Hành động",
                    mineRows.Any(r => Convert.ToString(r.Cells[2].Value).Contains("Chữ ký")));
                foreach (DataGridViewRow r in dgvQ(q).Rows) r.Selected = false;
                foreach (var r in mineRows) r.Selected = true;
                F<Button>(q, "btnRestore").PerformClick();
                PumpMs(800);
                Check("[UI] Restore đưa 2 tệp về đường dẫn gốc",
                    File.Exists(Path.Combine(root, "drop.txt")) && File.Exists(Path.Combine(root, "qr_eicar.dat")));
                Check("[UI] label tổng tab Cách ly giảm còn 0 tệp test",
                    !dgvQ(q).Rows.Cast<DataGridViewRow>()
                        .Any(r => Convert.ToString(r.Cells[1].Value).StartsWith(root)));
                Check("[UI] đếm cách ly tab Tổng quan đồng bộ",
                    F<Label>(uc, "lblQuarantineCount").Text == qBefore.ToString());
                var listNow = ScanEngine.ListQuarantined().Where(x => x.OriginalPath.StartsWith(root)).ToList();

                // ===== 5b. Bốn nút còn lại của tab Cách ly =====
                // btnDeleteAll xóa SẠCH khu cách ly -> chỉ chạy khi nền máy trống (qBefore==0) để không phá dữ liệu thật
                if (qBefore == 0)
                {
                string e1 = Path.Combine(root, "btn-flow1.txt");
                string e2 = Path.Combine(root, "btn-flow2.txt");
                string idA, idB;
                File.WriteAllText(e1, ScanEngine.TestSignature + "button flow one!!");
                File.WriteAllText(e2, ScanEngine.TestSignature + "button flow two!!");
                Check("[UI] cách ly 2 tệp nền cho test nút",
                    ScanEngine.Quarantine(e1, "Btn test", out idA) && ScanEngine.Quarantine(e2, "Btn test", out idB));
                q.RefreshData();
                F<Button>(q, "btnRefreshQuarantine").PerformClick(); // smoke: không nổ
                var own = dgvQ(q).Rows.Cast<DataGridViewRow>()
                    .Where(r => Convert.ToString(r.Cells[1].Value).StartsWith(root)).Count();
                Check("[UI] refresh thấy 2 mục mới cách ly", own == 2);
                // Xóa vĩnh viễn 1 dòng đang chọn
                foreach (DataGridViewRow rr in dgvQ(q).Rows) rr.Selected = false;
                foreach (DataGridViewRow rr in dgvQ(q).Rows)
                    if (Convert.ToString(rr.Cells[1].Value) == e1) rr.Selected = true;
                F<Button>(q, "btnDeletePermanent").PerformClick();
                PumpMs(800); // confirm Yes (closer) 
                Check("[UI] btnDeletePermanent: 1 mục bị xóa, sổ giảm còn 1",
                    dgvQ(q).Rows.Cast<DataGridViewRow>()
                        .Count(r => Convert.ToString(r.Cells[1].Value).StartsWith(root)) == 1
                    && !ScanEngine.ListQuarantined().Any(x => x.Id == idA));
                // Khôi phục tất cả
                F<Button>(q, "btnRestoreAll").PerformClick();
                PumpMs(800);
                Check("[UI] btnRestoreAll: tệp e2 về lại vị trí cũ",
                    File.Exists(e2) && !dgvQ(q).Rows.Cast<DataGridViewRow>()
                        .Any(r => Convert.ToString(r.Cells[1].Value).StartsWith(root)));
                // Cách ly lại cả 2 -> Xóa tất cả
                File.WriteAllText(e2, ScanEngine.TestSignature + "button flow two!!");
                string idA2, idB2;
                ScanEngine.Quarantine(e1, "Btn test", out idA2);
                ScanEngine.Quarantine(e2, "Btn test", out idB2);
                q.RefreshData();
                F<Button>(q, "btnDeleteAll").PerformClick();
                PumpMs(800);
                Check("[UI] btnDeleteAll: sạch danh mục test, tệp biến mất",
                    !dgvQ(q).Rows.Cast<DataGridViewRow>()
                        .Any(r => Convert.ToString(r.Cells[1].Value).StartsWith(root))
                    && !File.Exists(e1) && !File.Exists(e2));
                listNow = ScanEngine.ListQuarantined().Where(x => x.OriginalPath.StartsWith(root)).ToList();
                }
                else Console.WriteLine("  SKIP 5b (máy đang có tệp cách ly thật, không dám bấm Xóa tất cả)");

                // ===== 6. Tab Lịch sử: dữ liệu thật + tab đe dọa =====
                var hist = new UcLichSu();
                hist.Dock = DockStyle.Fill;
                form.Controls.Add(hist);
                Application.DoEvents();
                var grid = F<DataGridView>(hist, "dgvHistory");
                Check("[UI] Lịch sử có phiên quét tùy chọn + đe dọa=2",
                    grid.Rows.Cast<DataGridViewRow>().Any(r =>
                        (string)r.Cells[1].Value == "Quét tùy chọn" && (string)r.Cells[4].Value == "2"));
                F<TabControl>(hist, "tabHistory").SelectedIndex = 1; // tab đe dọa
                PumpMs(200);
                Check("[UI] tab chỉ-mối-de-dọa: mọi dòng Threats>0",
                    grid.Rows.Count > 0 && grid.Rows.Cast<DataGridViewRow>().All(r =>
                    {
                        int n;
                        return int.TryParse(Convert.ToString(r.Cells[4].Value), out n) && n > 0;
                    }));
                F<TabControl>(hist, "tabHistory").SelectedIndex = 2;
                PumpMs(200);
                Check("[UI] tab Cập nhật: có dòng 'Cập nhật CSDL'",
                    grid.Rows.Cast<DataGridViewRow>().Any(r => (string)r.Cells[1].Value == "Cập nhật CSDL"));
                // Hai nút chưa bấm: Chi tiết + Làm mới
                F<TabControl>(hist, "tabHistory").SelectedIndex = 0;
                PumpMs(150);
                int beforeDetail = grid.Rows.Count;
                grid.Rows[0].Selected = true;
                F<Button>(hist, "btnViewDetail").PerformClick();
                PumpMs(500); // MessageBox chi tiết -> closer bấm OK
                Check("[UI] btnViewDetail mở hộp chi tiết xong vẫn còn bảng", grid.Rows.Count == beforeDetail);
                F<Button>(hist, "btnRefreshHistory").PerformClick();
                PumpMs(300);
                Check("[UI] btnRefreshHistory nạp lại dữ liệu", grid.Rows.Count > 0);
                hist.Dispose();

                // ===== 7. Tab Bảo vệ: 2 công tắc thật =====
                var bv = new UcBaoVe();
                var bgrid = F<DataGridView>(bv, "dgvProtecctionFeatures");
                int rtRow = -1, aqRow = -1;
                for (int i = 0; i < bgrid.Rows.Count; i++)
                {
                    string feat = Convert.ToString(bgrid.Rows[i].Cells[0].Value);
                    if (feat == "Bảo vệ thời gian thực") rtRow = i;
                    if (feat == "Tự động cách ly") aqRow = i;
                }
                var cellClick = typeof(UcBaoVe).GetMethod("Grid_CellClick", BindingFlags.Instance | BindingFlags.NonPublic);
                int colAction = F<DataGridViewColumn>(bv, "colAction").Index;
                Check("[UI] tab Bảo vệ có 2 hàng điều khiển thật", rtRow >= 0 && aqRow >= 0);
                cellClick.Invoke(bv, new object[] { bgrid, new DataGridViewCellEventArgs(colAction, rtRow) });
                PumpMs(300);
                Check("[UI] bật BVTGT -> IsRunning + ô Bật",
                    RealTimeProtection.IsRunning && (string)bgrid.Rows[rtRow].Cells[2].Value == "Bật");
                bool autoWas = RealTimeProtection.AutoQuarantine;
                cellClick.Invoke(bv, new object[] { bgrid, new DataGridViewCellEventArgs(colAction, aqRow) });
                Check("[UI] công tắc Tự động cách ly lật state", RealTimeProtection.AutoQuarantine != autoWas);
                cellClick.Invoke(bv, new object[] { bgrid, new DataGridViewCellEventArgs(colAction, rtRow) });
                PumpMs(200);
                Check("[UI] tắt BVTGT -> IsRunning false + ô Tắt",
                    !RealTimeProtection.IsRunning && (string)bgrid.Rows[rtRow].Cells[2].Value == "Tắt");
                RealTimeProtection.AutoQuarantine = autoWas;
                bv.Dispose();

                // ===== 8. Số liệu thống kê PHẢI là dữ liệu thật (regression mock 2025) =====
                var uc2 = new UcTongQuan();
                uc2.Dock = DockStyle.Fill;
                form.Controls.Add(uc2);
                Application.DoEvents(); uc2.BringToFront(); Application.DoEvents();
                Check("[UI] 'Lần quét gần nhất' khôi phục từ lịch sử, không phải mock",
                    F<Label>(uc2, "lblLastScanDate").Text.Contains(DateTime.Now.ToString("dd/MM/yyyy"))
                    && F<Label>(uc2, "lblLastScanType").Text == "Quét tùy chọn");
                var bv2 = new UcBaoVe();
                Check("[UI] tab Bảo vệ: phiên bản CSDL không còn mock '1.0.0.2025'",
                    F<Label>(bv2, "lblDatabaseVersionValue").Text != "1.0.0.2025"
                    && F<Label>(bv2, "lblDatabaseVersionValue").Text ==
                        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
                long scannedTotal;
                Check("[UI] tab Bảo vệ: tổng tệp quét là số thật (không còn '125.430')",
                    long.TryParse(F<Label>(bv2, "lblScannedFilesValue").Text.Replace(".", ""),
                        System.Globalization.NumberStyles.AllowThousands,
                        System.Globalization.CultureInfo.InvariantCulture, out scannedTotal)
                    && F<Label>(bv2, "lblScannedFilesValue").Text != "125.430");
                Check("[UI] tab Bảo vệ: nhãn trạng thái khớp RT state thật",
                    F<Label>(bv2, "lblStatusValue").Text == (RealTimeProtection.IsRunning
                        ? "Tất cả các tính năng đang hoạt động tốt" : "Bảo vệ thời gian thực đang tắt"));
                var cfg0 = ScanAndRemoveVirus.Services.AppSettings.Load();
                try
                {
                    F<CheckBox>(bv2, "chkAutoUpdate").Checked = true;
                    F<CheckBox>(bv2, "chkSendSamples").Checked = false;
                    F<Button>(bv2, "btnSaveSettings").PerformClick(); // có MessageBox -> closer lo
                    PumpMs(900);
                    var cfg1 = AppSettings.Load();
                    Check("[UI] chk + btn 'Lưu cài đặt' ghi vào settings.ini",
                        cfg1.AutoUpdate && !cfg1.SendSamples);
                }
                finally { ScanAndRemoveVirus.Services.AppSettings.Save(cfg0); }
                bv2.Dispose(); uc2.Dispose();

                // ===== 9. Điều hướng FrmMain: nút Cài đặt + shortcut số cách ly =====
                using (var mf = new ScanAndRemoveVirus.FrmMain())
                {
                    // FrmMain chưa Show -> CanSelect=false -> PerformClick là no-op.
                    // Hiển thị ngoài màn hình để các nút sidebar nhận click thật.
                    mf.StartPosition = FormStartPosition.Manual;
                    mf.Location = new Point(-2000, -2000);
                    mf.Show(); Application.DoEvents();
                    var panel = F<Panel>(mf, "pnlContent");
                    Check("[UI] FrmMain khởi động = tab Tổng quan",
                        panel.Controls.Count == 1 && panel.Controls[0].GetType().Name == "UcTongQuan");
                    F<Button>(mf, "btnCaiDat").PerformClick();
                    Application.DoEvents();
                    string afterCaiDat = panel.Controls.Count == 0 ? "(empty)" : panel.Controls[0].GetType().Name;
                    Check("[UI] nút sidebar 'Cài đặt' mở vùng cài đặt trong tab Bảo vệ [" + afterCaiDat + "]",
                        afterCaiDat == "UcBaoVe");
                    F<Button>(mf, "btnTongQuan").PerformClick();
                    Application.DoEvents();
                    // 3 nút sidebar còn lại
                    F<Button>(mf, "btnLichSu").PerformClick(); Application.DoEvents();
                    Check("[UI] sidebar Lịch sử đổi nội dung", panel.Controls[0].GetType().Name == "UcLichSu");
                    F<Button>(mf, "btnCachLy").PerformClick(); Application.DoEvents();
                    Check("[UI] sidebar Cách ly đổi nội dung", panel.Controls[0].GetType().Name == "UcCachLy");
                    F<Button>(mf, "btnBaoVe").PerformClick(); Application.DoEvents();
                    Check("[UI] sidebar Bảo vệ đổi nội dung", panel.Controls[0].GetType().Name == "UcBaoVe");
                    F<Button>(mf, "btnTongQuan").PerformClick(); Application.DoEvents();
                    var ov = F<UserControl>(mf, "ucTongQuan");
                    var lblQ = F<Label>(ov, "lblQuarantineCount");
                    typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(lblQ, new object[] { EventArgs.Empty });
                    Check("[UI] bấm số đếm Cách ly ở Tổng quan -> nhảy sang tab Cách ly",
                        panel.Controls.Count == 1 && panel.Controls[0].GetType().Name == "UcCachLy");
                    mf.Close();
                }

                // ===== 10. Dọn các mục còn lại trong test ledger =====
                foreach (var item in listNow) ScanEngine.DeleteQuarantined(item.Id);
                Check("[UI] dọn sạch cách ly về nền ban đầu", ScanEngine.CountQuarantined() == qBefore);

                form.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("FAIL EX: " + ex);
            failures++;
        }
        finally
        {
            RealTimeProtection.Stop();
            try { Directory.Delete(root, true); } catch { }
        }
    }

    static DataGridView dgvQ(UcCachLy q) { return F<DataGridView>(q, "dgvQuarantine"); }
}
