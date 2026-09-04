// Test UI đầu-cuối: dựng UserControl thật, BẤM NÚT thật, dialog tự đóng bằng closer thread.
// Chạy (từ thư mục ScanAndRemoveVirus) — CẦN Roslyn csc (nguồn dùng C#7; csc Framework 4.0 KHÔNG compile nổi):
//   $csc = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\Roslyn\csc.exe" | Select-Object -First 1
//   & $csc /out:ui_test.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Net.Http.dll /r:System.Management.dll
//     ScanAndRemoveVirus\Services\ScanEngine.cs ScanAndRemoveVirus\Services\RealTimeProtection.cs
//     ScanAndRemoveVirus\Services\QuarantineLedger.cs ScanAndRemoveVirus\Services\ScanHistoryStore.cs
//     ScanAndRemoveVirus\Services\VirusTotalClient.cs ScanAndRemoveVirus\Services\AppSettings.cs
//     ScanAndRemoveVirus\Services\FeatureFlags.cs ScanAndRemoveVirus\Services\GuardService.cs
//     ScanAndRemoveVirus\Services\TestSamples.cs ScanAndRemoveVirus\Services\DataDir.cs
//     ScanAndRemoveVirus\Control\Theme.cs
//     ScanAndRemoveVirus\Control\UcTongQuan.cs ScanAndRemoveVirus\Control\UcTongQuan.Designer.cs
//     ScanAndRemoveVirus\Control\UcCachLy.cs  ScanAndRemoveVirus\Control\UcCachLy.Designer.cs
//     ScanAndRemoveVirus\Control\UcLichSu.cs  ScanAndRemoveVirus\Control\UcLichSu.Designer.cs
//     ScanAndRemoveVirus\Control\UcBaoVe.cs   ScanAndRemoveVirus\Control\UcBaoVe.Designer.cs
//     ScanAndRemoveVirus\Control\UcCaiDat.cs  ScanAndRemoveVirus\Control\UcCaiDat.Designer.cs
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
        var fi = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (fi == null)
            throw new Exception("thiếu field '" + name + "' trên " + o.GetType().Name);
        object v = fi.GetValue(o);
        if (v == null)
            throw new Exception("field '" + name + "' trên " + o.GetType().Name + " đang null");
        return (T)v;
    }
    static void SetF(object o, string name, object val)
    {
        o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(o, val);
    }
    // Đặt giá trị checkbox thẳng vào cell KHÔNG bắn CellValueChanged -> nút hành động
    // vẫn Disabled (SyncButtons/UpdateThreatUi không chạy). Gọi method sync thủ công.
    static void InvokeM(object o, string name)
    {
        var mi = o.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (mi == null) throw new Exception("thiếu method '" + name + "' trên " + o.GetType().Name);
        mi.Invoke(o, null);
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
        // Test ghi vào kho dữ liệu riêng, không đụng scanhistory.log/quarantine thật của user
        Environment.SetEnvironmentVariable("XVIRUS_DATA_DIR",
            Path.Combine(Path.GetTempPath(), "xvirus-test-data-" + Guid.NewGuid().ToString("N")));
        var closer = new Thread(CloserLoop) { IsBackground = true };
        closer.Start();

        var th = new Thread(Run);
        th.SetApartmentState(ApartmentState.STA);
        th.Start();
        th.Join(TimeSpan.FromSeconds(900)); // 600s đôi khi không đủ trên máy chậm (VT live + WMI)
        stopCloser = true;
        if (th.IsAlive) { Console.WriteLine("FAIL: test treo"); return 1; }

        Console.WriteLine(failures == 0 ? "== UI E2E ALL PASSED ==" : "== " + failures + " UI E2E FAILED ==");
        return failures == 0 ? 0 : 1;
    }

    static void Run()
    {
        // CÔ LẬP SUITE: tắt mọi watcher sống trong tiến trình.test để không có guard nền nào
        // chạm vào binary/tệp của suite (trên máy này, exe chạy từ %TEMP% chính là đối tượng
        // luật dropper — nếu app thật đang chạy nền sẽ cách ly cả file test của ta).
        try
        {
            RealTimeProtection.Stop();
            RealTimeProtection.AutoQuarantine = false;
            GuardService.Configure("usb", false);
            GuardService.Configure("download", false);
            GuardService.Configure("behavior", false);
            GuardService.Configure("startup", false);
        }
        catch (Exception) { }

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
                    .All(r => Convert.ToString(r.Cells[2].Value).Contains("Chữ ký")));
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
                dgv.Rows[0].Cells[0].Value = true; // "Xóa đã chọn" là con đường xóa duy nhất còn lại
                InvokeM(uc, "UpdateThreatUi");
                F<Button>(uc, "btnDeleteSelected").PerformClick();
                PumpMs(1400);
                Check("[UI] Xóa đã chọn: xác nhận Yes -> tệp bị xóa vĩnh viễn",
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
                // Corpus đủ LỚN để phiên quét còn chạy khi bấm Hủy (máy rảnh quét ~0.3s/3300 tệp)
                string cancelDir = Path.Combine(root, "cancel-ui");
                for (int cd = 0; cd < 60; cd++)
                {
                    string dd = Directory.CreateDirectory(Path.Combine(cancelDir, "sub" + cd)).FullName;
                    for (int cf = 0; cf < 150; cf++)
                        File.WriteAllText(Path.Combine(dd, "c" + cf + ".log"), new string('y', 120));
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
                    string threatCell = Convert.ToString(agd.Rows[0].Cells[2].Value);
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
                    .Where(r => Convert.ToString(r.Cells[2].Value) != null
                        && Convert.ToString(r.Cells[2].Value).StartsWith(root)).ToList();
                Check("[UI] tab Cách ly liệt kê 2 tệp test", mineRows.Count == 2);
                Check("[UI] dòng Cách ly có lý do từ bảng Hành động",
                    mineRows.Any(r => Convert.ToString(r.Cells[3].Value).Contains("Chữ ký")));
                foreach (DataGridViewRow r in dgvQ(q).Rows) r.Cells[0].Value = false;
                foreach (var r in mineRows) r.Cells[0].Value = true;
                Application.DoEvents();
                F<Button>(q, "btnRestore").PerformClick();
                PumpMs(800);
                Check("[UI] Restore đưa 2 tệp về đường dẫn gốc",
                    File.Exists(Path.Combine(root, "drop.txt")) && File.Exists(Path.Combine(root, "qr_eicar.dat")));
                Check("[UI] label tổng tab Cách ly giảm còn 0 tệp test",
                    !dgvQ(q).Rows.Cast<DataGridViewRow>()
                        .Any(r => Convert.ToString(r.Cells[2].Value).StartsWith(root)));
                Check("[UI] đếm cách ly tab Tổng quan đồng bộ",
                    F<Label>(uc, "lblQuarantineCount").Text == qBefore.ToString());
                var listNow = ScanEngine.ListQuarantined().Where(x => x.OriginalPath.StartsWith(root)).ToList();

                // ===== 5b. Bốn nút còn lại của tab Cách ly =====
                // block 5b xóa SẠCH khu cách ly -> chỉ chạy khi nền máy trống (qBefore==0) để không phá dữ liệu thật
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
                    .Where(r => Convert.ToString(r.Cells[2].Value).StartsWith(root)).Count();
                Check("[UI] refresh thấy 2 mục mới cách ly", own == 2);
                // Xóa vĩnh viễn 1 dòng đang TÍCH (checkbox cột Chọn)
                Application.DoEvents(); // xả trước BeginInvoke(RefreshData) do QuarantineChanged queue
                foreach (DataGridViewRow rr in dgvQ(q).Rows) rr.Cells[0].Value = false;
                foreach (DataGridViewRow rr in dgvQ(q).Rows)
                    if (Convert.ToString(rr.Cells[2].Value) == e1) rr.Cells[0].Value = true;
                InvokeM(q, "SyncButtons"); // Value= không bắn CellValueChanged -> tự gọi sync nút
                F<Button>(q, "btnDeletePermanent").PerformClick();
                PumpMs(800); // confirm Yes (closer)
                Check("[UI] btnDeletePermanent: 1 mục bị xóa, sổ giảm còn 1",
                    dgvQ(q).Rows.Cast<DataGridViewRow>()
                        .Count(r => Convert.ToString(r.Cells[2].Value).StartsWith(root)) == 1
                    && !ScanEngine.ListQuarantined().Any(x => x.Id == idA));
                // Khôi phục tất cả
                F<Button>(q, "btnRestoreAll").PerformClick();
                PumpMs(800);
                Check("[UI] btnRestoreAll: tệp e2 về lại vị trí cũ",
                    File.Exists(e2) && !dgvQ(q).Rows.Cast<DataGridViewRow>()
                        .Any(r => Convert.ToString(r.Cells[2].Value).StartsWith(root)));
                // Cách ly lại cả 2 -> Xóa vĩnh viễn cả 2 qua tick checkbox
                File.WriteAllText(e2, ScanEngine.TestSignature + "button flow two!!");
                string idA2, idB2;
                ScanEngine.Quarantine(e1, "Btn test", out idA2);
                ScanEngine.Quarantine(e2, "Btn test", out idB2);
                q.RefreshData();
                Application.DoEvents(); // xả BeginInvoke(RefreshData) trước khi tick
                foreach (DataGridViewRow rr in dgvQ(q).Rows)
                    if (Convert.ToString(rr.Cells[2].Value).StartsWith(root)) rr.Cells[0].Value = true;
                InvokeM(q, "SyncButtons");
                F<Button>(q, "btnDeletePermanent").PerformClick();
                PumpMs(800);
                Check("[UI] btnDeletePermanent x2: sạch danh mục test, tệp biến mất",
                    !dgvQ(q).Rows.Cast<DataGridViewRow>()
                        .Any(r => Convert.ToString(r.Cells[2].Value).StartsWith(root))
                    && !File.Exists(e1) && !File.Exists(e2));
                listNow = ScanEngine.ListQuarantined().Where(x => x.OriginalPath.StartsWith(root)).ToList();
                }
                else Console.WriteLine("  SKIP 5b (máy đang có tệp cách ly thật, không dám xóa hàng loạt)");

                // ===== 6. Tab Lịch sử: dữ liệu thật + tab đe dọa =====
                var hist = new UcLichSu();
                hist.Dock = DockStyle.Fill;
                form.Controls.Add(hist);
                Application.DoEvents();
                var grid = F<DataGridView>(hist, "dgvHistory");
                // cột 0 là colPick (checkbox), cột 1=colTime, 2=colScanType, ... 5=colThreatCount
                Check("[UI] Lịch sử có phiên quét tùy chọn + đe dọa=2",
                    grid.Rows.Cast<DataGridViewRow>().Any(r =>
                        (string)r.Cells[2].Value == "Quét tùy chọn" && (string)r.Cells[5].Value == "2"));
                F<Button>(hist, "btnFilterThreats").PerformClick(); // chỉ dòng đe dọa > 0
                PumpMs(200);
                Check("[UI] filter chỉ-mối-de-dọa: mọi dòng Threats>0",
                    grid.Rows.Count > 0 && grid.Rows.Cast<DataGridViewRow>().All(r =>
                    {
                        int n;
                        return int.TryParse(Convert.ToString(r.Cells[5].Value), out n) && n > 0;
                    }));
                F<Button>(hist, "btnFilterUpdates").PerformClick();
                PumpMs(200);
                Check("[UI] filter Cập nhật: có dòng 'Cập nhật CSDL'",
                    grid.Rows.Cast<DataGridViewRow>().Any(r => (string)r.Cells[2].Value == "Cập nhật CSDL"));
                // Quay lại Mọi phiên quét + hai nút: Chi tiết + Làm mới
                F<Button>(hist, "btnFilterAll").PerformClick();
                PumpMs(150);
                int beforeDetail = grid.Rows.Count;
                grid.Rows[0].Selected = true;
                F<Button>(hist, "btnViewDetail").PerformClick();
                PumpMs(500); // MessageBox chi tiết -> closer bấm OK
                Check("[UI] btnViewDetail mở hộp chi tiết xong vẫn còn bảng", grid.Rows.Count == beforeDetail);
                F<Button>(hist, "btnRefreshHistory").PerformClick();
                PumpMs(300);
                Check("[UI] btnRefreshHistory nạp lại dữ liệu", grid.Rows.Count > 0);
                // ===== 6b. Cột Chọn (checkbox) + nút Xóa mục đã chọn + Sắp xếp =====
                ScanHistoryStore.Add("Xoa sort test", "pham vi test 6b", 7, 0, 1.0);
                F<Button>(hist, "btnRefreshHistory").PerformClick();
                PumpMs(300);
                var testRows = grid.Rows.Cast<DataGridViewRow>()
                    .Where(r => (string)r.Cells[2].Value == "Xoa sort test").ToList();
                Check("[UI] 6b: dòng test xuất hiện sau refresh", testRows.Count == 1);
                // nút Xóa chỉ bật khi có tick -> tick 1 dòng
                Check("[UI] 6b: btnDeleteHistory disabled khi chưa tick", !F<Button>(hist, "btnDeleteHistory").Enabled);
                testRows[0].Cells[0].Value = true;
                Application.DoEvents();
                Check("[UI] 6b: tick bật nút Xóa + label 'Đã chọn 1 dòng'",
                    F<Button>(hist, "btnDeleteHistory").Enabled
                    && F<Label>(hist, "lblSelection").Text.Contains("1"));
                int totalBefore = ScanHistoryStore.Entries().Count;
                F<Button>(hist, "btnDeleteHistory").PerformClick();
                PumpMs(600); // confirm Yes -> closer
                Check("[UI] 6b: btnDeleteHistory xóa đúng dòng đã tick",
                    !ScanHistoryStore.Entries().Any(x => x.Type == "Xoa sort test")
                    && ScanHistoryStore.Entries().Count == totalBefore - 1);
                Check("[UI] 6b: lưới sau xóa không còn dòng test",
                    !grid.Rows.Cast<DataGridViewRow>().Any(r => (string)r.Cells[2].Value == "Xoa sort test"));
                // Sắp xếp deterministic: cắm 2 mốc thời gian cách nhau 90 phút
                ScanHistoryStore.AddEntry(new HistoryEntry {
                    Time = DateTime.Now.AddMinutes(-90), Type = "6b sort cu", Scope = "x",
                    Files = 1, Threats = 0, Seconds = 1 });
                ScanHistoryStore.AddEntry(new HistoryEntry {
                    Time = DateTime.Now.AddMinutes(-2), Type = "6b sort moi", Scope = "x",
                    Files = 1, Threats = 0, Seconds = 1 });
                F<Button>(hist, "btnRefreshHistory").PerformClick();
                PumpMs(300);
                Func<string, int> idxOf = type => grid.Rows.Cast<DataGridViewRow>()
                    .Select((r, i) => new { r, i })
                    .First(z => (string)z.r.Cells[2].Value == type).i;
                Check("[UI] 6b: mặc định mới-trước (moi above cu)",
                    idxOf("6b sort moi") < idxOf("6b sort cu"));
                typeof(UcLichSu).GetMethod("ToggleSort", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(hist, null);
                Check("[UI] 6b: header Thời gian -> cũ lên đầu",
                    idxOf("6b sort cu") < idxOf("6b sort moi"));
                typeof(UcLichSu).GetMethod("ToggleSort", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(hist, null);
                Check("[UI] 6b: bấm lần 2 -> trả lại mới trước",
                    idxOf("6b sort moi") < idxOf("6b sort cu"));
                Check("[UI] 6b: header Chọn là icon (HeaderText rỗng, có tooltip)",
                    F<DataGridViewColumn>(hist, "colPick").HeaderText == ""
                    && F<DataGridViewColumn>(hist, "colPick").ToolTipText.Length > 0);
                var hdrClick = typeof(UcLichSu).GetMethod("Grid_HeaderClick",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                int pickCol = F<DataGridViewCheckBoxColumn>(hist, "colPick").Index;
                hdrClick.Invoke(hist, new object[] { null,
                    new DataGridViewCellMouseEventArgs(pickCol, -1, 20, 20, new MouseEventArgs(MouseButtons.Left, 1, 20, 20, 0)) });
                Check("[UI] 6b: click header checkbox -> chọn TẤT CẢ dòng",
                    grid.Rows.Count > 0
                    && grid.Rows.Cast<DataGridViewRow>().All(r => r.Cells[pickCol].Value is bool bb && bb)
                    && F<Button>(hist, "btnDeleteHistory").Enabled);
                hdrClick.Invoke(hist, new object[] { null,
                    new DataGridViewCellMouseEventArgs(pickCol, -1, 20, 20, new MouseEventArgs(MouseButtons.Left, 1, 20, 20, 0)) });
                Check("[UI] 6b: click lần 2 -> bỏ chọn toàn bộ, nút Xóa tắt lại",
                    !grid.Rows.Cast<DataGridViewRow>().Any(r => r.Cells[pickCol].Value is bool bb && bb)
                    && !F<Button>(hist, "btnDeleteHistory").Enabled);
                // dọn 2 mốc sort: TICK cả 2 dòng rồi xóa 1 phát (lint luồng multi-delete)
                grid.Rows[idxOf("6b sort moi")].Cells[0].Value = true;
                grid.Rows[idxOf("6b sort cu")].Cells[0].Value = true;
                F<Button>(hist, "btnDeleteHistory").PerformClick();
                PumpMs(600);
                Check("[UI] 6b: multi-delete xóa sạch 2 dòng đã tick",
                    !ScanHistoryStore.Entries().Any(x =>
                        x.Type == "6b sort cu" || x.Type == "6b sort moi"));
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

                // Các hàng guard giờ là công tắc THẬT: lật ô Trạng thái + bật/tắt cơ chế tương ứng
                string iniBak = File.Exists(AppSettings.SettingsPath)
                    ? File.ReadAllText(AppSettings.SettingsPath) : null;
                try
                {
                    int usbRow = -1, behRow = -1, alertRow = -1;
                    for (int i = 0; i < bgrid.Rows.Count; i++)
                    {
                        string feat = Convert.ToString(bgrid.Rows[i].Cells[0].Value);
                        if (feat == "Bảo vệ USB") usbRow = i;
                        if (feat == "Phát hiện hành vi đáng ngờ") behRow = i;
                        if (feat == "Cảnh báo mối đe dọa") alertRow = i;
                    }
                    Check("[UI] tab Bảo vệ: đủ 10 hàng, có các hàng guard",
                        bgrid.Rows.Count == 10 && usbRow >= 0 && behRow >= 0 && alertRow >= 0);

                    // Đưa guard USB về trạng thái TẮT確定 để test lật 2 chiều
                    FeatureFlags.UsbProtection = false; GuardService.Configure("usb", false);
                    cellClick.Invoke(bv, new object[] { bgrid, new DataGridViewCellEventArgs(colAction, usbRow) }); // -> Bật
                    PumpMs(200);
                    Check("[UI] hàng 'Bảo vệ USB' bật guard thật", FeatureFlags.UsbProtection && GuardService.IsGuardRunning("usb"));
                    cellClick.Invoke(bv, new object[] { bgrid, new DataGridViewCellEventArgs(colAction, usbRow) }); // -> Tắt
                    PumpMs(200);
                    Check("[UI] hàng 'Bảo vệ USB' tắt guard thật", !FeatureFlags.UsbProtection && !GuardService.IsGuardRunning("usb"));

                    cellClick.Invoke(bv, new object[] { bgrid, new DataGridViewCellEventArgs(colAction, alertRow) });
                    PumpMs(150);
                    Check("[UI] hàng 'Cảnh báo' lật FeatureFlags thật", FeatureFlags.ThreatAlerts == false);
                    cellClick.Invoke(bv, new object[] { bgrid, new DataGridViewCellEventArgs(colAction, alertRow) });
                    Check("[UI] bật lại cảnh báo", FeatureFlags.ThreatAlerts == true);

                    cellClick.Invoke(bv, new object[] { bgrid, new DataGridViewCellEventArgs(colAction, behRow) });
                    PumpMs(250);
                    bool after1 = FeatureFlags.BehaviorWatch;
                    cellClick.Invoke(bv, new object[] { bgrid, new DataGridViewCellEventArgs(colAction, behRow) });
                    Check("[UI] hàng 'Hành vi' (WMI) lật 2 chiều", after1 != FeatureFlags.BehaviorWatch);
                    GuardService.Configure("behavior", false); // dọn watcher cho tiến trình test

                    // ===== 7b. Bộ mock virus 3 kỹ thuật: nút tạo mẫu (tab Cài đặt) -> quét qua UI -> đúng 5 threat =====
                    var cd = new UcCaiDat();
                    F<Button>(cd, "btnSamples").PerformClick();
                    PumpMs(500); // hộp thông báo -> closer OK
                    Check("[UI] btnSamples tạo 11 tệp mẫu ở thư mục Samples",
                        Directory.Exists(TestSamples.FolderPath)
                        && Directory.GetFiles(TestSamples.FolderPath).Length == 11);
                    cd.Dispose();
                    try
                    {
                        // Smoke Quét nhanh THẬT trên UI với HỦY giữa chừng (vùng người dùng ~300k tệp)
                        qkR.Checked = true;
                        SetF(uc, "customScanPath", null);
                        F<Button>(uc, "btnScanNow").PerformClick();
                        PumpMs(300);
                        bool running = (bool)F<object>(uc, "isScanning");
                        if (running)
                        {
                            Check("[UI] Quick scan thật: nút thành 'Hủy quét'",
                                F<Button>(uc, "btnScanNow").Text == "Hủy quét");
                            F<Button>(uc, "btnScanNow").PerformClick();
                        }
                        var kUntil = DateTime.Now.AddSeconds(40);
                        while (DateTime.Now < kUntil && (bool)F<object>(uc, "isScanning"))
                        { Application.DoEvents(); Thread.Sleep(15); }
                        PumpMs(500);
                        string qtxt = F<Label>(uc, "lblScanProgress").Text;
                        Check("[UI] Quick scan kết thúc an toàn (hủy hoặc hoàn tất)",
                            qtxt.Contains("hủy") || qtxt.StartsWith("Hoàn tất") || qtxt.Contains("lỗi"));

                        // Quét THƯ MỤC MẪU qua UI: đúng 8 dòng, 4 Chữ ký + 4 Heuristic
                        cmR.Checked = true;
                        SetF(uc, "customScanPath", TestSamples.FolderPath);
                        F<Label>(uc, "lblThreatCount"); // noop guard for reflection field types
                        F<Button>(uc, "btnScanNow").PerformClick();
                        PumpTarget = uc;
                        Pump(delegate { });
                        PumpMs(700);
                        var sdgv = F<DataGridView>(uc, "dgvActions");
                        int nSig = 0, nHeur = 0;
                        // cột: [0]=Chọn (checkbox), [1]=Tệp, [2]=Đe dọa ("Kind: Reason")
                        foreach (DataGridViewRow rr in sdgv.Rows)
                        {
                            string reason = Convert.ToString(rr.Cells[2].Value);
                            if (reason.Contains("Chữ ký")) nSig++;
                            else if (reason.Contains("Heuristic")) nHeur++;
                        }
                        Check("[UI] Quét bộ mẫu: đúng 8/11 dòng đe dọa", sdgv.Rows.Count == 8);
                        Check("[UI] Phân loại: 4 Chữ ký + 4 Heuristic", nSig == 4 && nHeur == 4);
                        Check("[UI] Ba tệp sạch (README + keygen + script-sach) không bị báo",
                            !sdgv.Rows.Cast<DataGridViewRow>().Any(r =>
                                Convert.ToString(r.Cells[1].Value).Contains(TestSamples.BenignSampleName)
                                || Convert.ToString(r.Cells[1].Value).Contains(TestSamples.CrackedExeName)
                                || Convert.ToString(r.Cells[1].Value).Contains(TestSamples.CleanScriptName)));
                        Check("[UI] History ghi phiên quét mẫu threats=8", ScanHistoryStore.Entries()
                            .Any(h => h.Type == "Quét tùy chọn" && h.Threats == 8));

                        // ===== 7c. hai nút "đã chọn": btnQuarantineSelected & btnDeleteSelected =====
                        int qBase = ScanEngine.CountQuarantined();
                        string sampFile1 = Convert.ToString(sdgv.Rows[0].Cells[1].Value);
                        sdgv.Rows[0].Cells[0].Value = true; // nút "đã chọn" lọc theo checkbox cột Chọn
                        InvokeM(uc, "UpdateThreatUi");
                        F<Button>(uc, "btnQuarantineSelected").PerformClick(); // OK box "Đã cách ly 1/1"
                        PumpMs(900);
                        Check("[UI] 'Cách ly đã chọn': chỉ đúng 1 dòng bị dời, bảng còn 7",
                            sdgv.Rows.Count == 7 && !File.Exists(sampFile1)
                            && ScanEngine.CountQuarantined() == qBase + 1);
                        string movedId = null;
                        foreach (var qi in ScanEngine.ListQuarantined())
                            if (qi.OriginalPath == sampFile1) movedId = qi.Id;
                        string sampFile2 = Convert.ToString(sdgv.Rows[0].Cells[1].Value);
                        sdgv.Rows[0].Cells[0].Value = true;
                        InvokeM(uc, "UpdateThreatUi");
                        F<Button>(uc, "btnDeleteSelected").PerformClick(); // Yes + OK box "Đã xóa 1/1"
                        PumpMs(900);
                        Check("[UI] 'Xóa đã chọn': dòng mất + tệp xóa vĩnh, không vào cách ly",
                            sdgv.Rows.Count == 6 && !File.Exists(sampFile2)
                            && ScanEngine.CountQuarantined() == qBase + 1);
                        // dọn: bỏ mục đã cách ly + khôi phục đủ 6 mẫu cho VT-smoke bên dưới
                        if (movedId != null) ScanEngine.DeleteQuarantined(movedId);
                        sdgv.Rows.Clear();
                        F<Label>(uc, "lblThreatCount").Text = "0"; // re-sync nhẹ; khi quét lại LoadDetectedThreats tự chỉnh
                        TestSamples.Create();

                        // VT auto-query smoke: bật cờ + key giả -> heuristic tự tra, lỗi 401 không phá hàng đợi
                        var flagBak = FeatureFlags.VtAutoQuery;
                        string vtbak = File.Exists(VirusTotalClient.ApiKeyPath)
                            ? File.ReadAllText(VirusTotalClient.ApiKeyPath) : null;
                        try
                        {
                            FeatureFlags.VtAutoQuery = true;
                            Directory.CreateDirectory(Path.GetDirectoryName(VirusTotalClient.ApiKeyPath));
                            File.WriteAllText(VirusTotalClient.ApiKeyPath, "autoquery-flow-test-key");
                            cmR.Checked = true;
                            F<Button>(uc, "btnScanNow").PerformClick();
                            Pump(delegate { });
                            PumpMs(600);            // auto-query chạy nền: để nó chạy đủ lâu
                            PumpMs(4000);
                            Application.DoEvents();
                            Check("[UI] VT auto-query chạy nền không làm sập/đổi bảng (8 dòng nguyên vẹn)",
                                F<DataGridView>(uc, "dgvActions").Rows.Count == 8);
                        }
                        finally
                        {
                            FeatureFlags.VtAutoQuery = flagBak;
                            if (vtbak == null) { try { File.Delete(VirusTotalClient.ApiKeyPath); } catch { } }
                            else File.WriteAllText(VirusTotalClient.ApiKeyPath, vtbak);
                        }
                    }
                    finally
                    {
                        // CHỈ dọn fallback Desktop — Samples\ trong repo là nguồn, không được xóa
                        string desktopSamples = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "XVirus-Samples");
                        try
                        {
                            if (TestSamples.FolderPath.Equals(desktopSamples, StringComparison.OrdinalIgnoreCase))
                                Directory.Delete(TestSamples.FolderPath, true);
                        }
                        catch { }
                    }
                }
                finally
                {
                    if (iniBak != null) File.WriteAllText(AppSettings.SettingsPath, iniBak);
                }
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
                var cd2 = new UcCaiDat();
                try
                {
                    F<CheckBox>(cd2, "chkAutoUpdate").Checked = true;
                    F<CheckBox>(cd2, "chkSendSamples").Checked = false;
                    F<Button>(cd2, "btnSaveSettings").PerformClick(); // có MessageBox -> closer lo
                    PumpMs(900);
                    var cfg1 = AppSettings.Load();
                    Check("[UI] chk + btn 'Lưu cài đặt' ghi vào settings.ini",
                        cfg1.AutoUpdate && !cfg1.SendSamples);
                }
                finally { ScanAndRemoveVirus.Services.AppSettings.Save(cfg0); cd2.Dispose(); }

                // ===== 8b. Tab Cài đặt bản mới: checkbox áp-ngay, VTkey, mặc định, cache, info =====
                {
                    string vtKeyBak2 = File.Exists(VirusTotalClient.ApiKeyPath)
                        ? File.ReadAllText(VirusTotalClient.ApiKeyPath) : null;
                    var flags3 = AppSettings.Load();
                    using (var fHost = new Form())
                    {
                        var cd3 = new UcCaiDat { Dock = DockStyle.Fill };
                        fHost.Controls.Add(cd3);
                        fHost.StartPosition = FormStartPosition.Manual;
                        fHost.Location = new Point(-2500, -2500);
                        fHost.Size = new Size(1000, 800);
                        fHost.Show(); Application.DoEvents();
                        try
                        {
                            // checkbox flag: ghi settings.ini + áp dụng NGAY + hint xanh
                            bool vtWas = FeatureFlags.VtAutoQuery;
                            F<CheckBox>(cd3, "chkVtAutoQuery").Checked = !vtWas;
                            PumpMs(250);
                            Check("[UI] Cài đặt: checkbox flag live-apply",
                                FeatureFlags.VtAutoQuery == !vtWas && AppSettings.Load().VtAutoQuery == !vtWas
                                && F<Label>(cd3, "lblGeneralHint").Text.StartsWith("Đã ghi & áp dụng ngay"));

                            // guard USB: checkbox điều khiển guard THẬT
                            F<CheckBox>(cd3, "chkUsbGuard").Checked = false; PumpMs(300);
                            bool usbStopped = !GuardService.IsGuardRunning("usb");
                            F<CheckBox>(cd3, "chkUsbGuard").Checked = true; PumpMs(300);
                            Check("[UI] Cài đặt: chk USB bật/tắt guard thật",
                                usbStopped && GuardService.IsGuardRunning("usb"));

                            // VTkey cấu hình QUA TỆP (UI nhập key bị ẩn ở MỌI bản):
                            // ghi vtapikey.txt -> status xanh; xóa -> status hổ phách
                            var txtVt = F<TextBox>(cd3, "txtVtKey");
                            Check("[UI] Cài đặt: ô nhập API key VirusTotal đã ẩn",
                                !txtVt.Visible && !F<Button>(cd3, "btnSaveVtKey").Visible
                                && !F<Button>(cd3, "btnClearVtKey").Visible);
                            var reload = typeof(UcCaiDat).GetMethod("LoadSettingsIntoUi",
                                BindingFlags.Instance | BindingFlags.NonPublic);
                            VirusTotalClient.SaveApiKey("e2e-dummy-vtkey-0123456789");
                            reload.Invoke(cd3, null);
                            Application.DoEvents();
                            Check("[UI] Cài đặt: key từ tệp -> IsConfigured + status xanh",
                                VirusTotalClient.IsConfigured
                                && File.ReadAllText(VirusTotalClient.ApiKeyPath) == "e2e-dummy-vtkey-0123456789"
                                && F<Label>(cd3, "lblVtStatus").Text.Contains("đã cấu hình"));
                            File.Delete(VirusTotalClient.ApiKeyPath);
                            reload.Invoke(cd3, null);
                            Application.DoEvents();
                            Check("[UI] Cài đặt: gỡ key -> status hổ phách",
                                !VirusTotalClient.IsConfigured && !File.Exists(VirusTotalClient.ApiKeyPath)
                                && F<Label>(cd3, "lblVtStatus").Text.Contains("chưa cấu hình"));

                            // Khôi phục mặc định: đặt cờ lệch trước -> bấm Yes
                            FeatureFlags.VtAutoQuery = true; FeatureFlags.ThreatAlerts = false;
                            FeatureFlags.Persist();
                            F<Button>(cd3, "btnResetDefaults").PerformClick(); PumpMs(500); // Yes + có thể thêm box
                            Check("[UI] Cài đặt: Khôi phục mặc định đúng bộ cờ",
                                FeatureFlags.ThreatAlerts && !FeatureFlags.AutoUpdateEnabled
                                && !FeatureFlags.VtAutoQuery && FeatureFlags.UsbProtection
                                && FeatureFlags.DownloadProtection && FeatureFlags.BehaviorWatch
                                && FeatureFlags.StartupFoldersWatch && FeatureFlags.FileRestoreGuard
                                && !AppSettings.Load().SendSamples);

                            // Xóa cache: file mất, hộp thông báo đóng an toàn
                            F<Button>(cd3, "btnClearCache").PerformClick(); PumpMs(400);
                            Check("[UI] Cài đặt: Xóa cache chạy hết, file cache sạch",
                                !File.Exists(ScanEngine.ScanCachePath));

                            // Nhãn thông tin dữ liệu
                            Check("[UI] Cài đặt: 3 nhãn info đúng nguồn thật",
                                F<Label>(cd3, "lblAppVersion").Text.StartsWith("Phiên bản ứng dụng: ")
                                && F<Label>(cd3, "lblDataPath").Text ==
                                    "Thư mục dữ liệu: " + Path.GetDirectoryName(ScanHistoryStore.LogPath)
                                && F<Label>(cd3, "lblQuarantined").Text ==
                                    "Đang cách ly: " + ScanEngine.CountQuarantined() + " tệp");
                        }
                        finally
                        {
                            fHost.Hide();
                            if (vtKeyBak2 != null) File.WriteAllText(VirusTotalClient.ApiKeyPath, vtKeyBak2);
                            else { try { File.Delete(VirusTotalClient.ApiKeyPath); } catch { } }
                            FeatureFlags.ThreatAlerts = flags3.ShowNotifications;
                            FeatureFlags.AutoUpdateEnabled = flags3.AutoUpdate;
                            FeatureFlags.VtAutoQuery = flags3.VtAutoQuery;
                            FeatureFlags.UsbProtection = flags3.UsbProtection;
                            FeatureFlags.DownloadProtection = flags3.DownloadProtection;
                            FeatureFlags.BehaviorWatch = flags3.BehaviorWatch;
                            FeatureFlags.StartupFoldersWatch = flags3.StartupFoldersWatch;
                            FeatureFlags.FileRestoreGuard = flags3.FileRestoreGuard;
                            FeatureFlags.Persist();
                            GuardService.ApplyAll();
                        }
                    }
                }
                bv2.Dispose(); uc2.Dispose();

                // ===== 9. Điều hướng FrmMain: nút Cài đặt + shortcut số cách ly =====
                Console.WriteLine("== SECTION 9 ==");
                using (var mf = new ScanAndRemoveVirus.FrmMain())
                {
                    // FrmMain chưa Show -> CanSelect=false -> PerformClick là no-op.
                    // Hiển thị ngoài màn hình để các nút sidebar nhận click thật.
                    Console.WriteLine("== 9: ctor done, Show ==");
                    mf.StartPosition = FormStartPosition.Manual;
                    mf.Location = new Point(-2000, -2000);
                    mf.Show(); Application.DoEvents();
                    Console.WriteLine("== 9: shown ==");
                    var panel = F<Panel>(mf, "pnlContent");
                    Check("[UI] FrmMain khởi động = tab Tổng quan",
                        panel.Controls.Count == 1 && panel.Controls[0].GetType().Name == "UcTongQuan");
                    Console.WriteLine("== 9: click CaiDat ==");
                    F<Button>(mf, "btnCaiDat").PerformClick();
                    Application.DoEvents();
                    Console.WriteLine("== 9: click TONGQUAN ==");
                    F<Button>(mf, "btnTongQuan").PerformClick();
                    Application.DoEvents();
                    // 3 nút sidebar còn lại
                    Console.WriteLine("== 9: click LichSu ==");
                    F<Button>(mf, "btnLichSu").PerformClick(); Application.DoEvents();
                    Console.WriteLine("== 9: click CachLy ==");
                    F<Button>(mf, "btnCachLy").PerformClick(); Application.DoEvents();
                    Console.WriteLine("== 9: click BaoVe ==");
                    F<Button>(mf, "btnBaoVe").PerformClick(); Application.DoEvents();
                    Console.WriteLine("== 9: click TONGQUAN 2 ==");
                    F<Button>(mf, "btnTongQuan").PerformClick(); Application.DoEvents();
                    var ov = F<UserControl>(mf, "ucTongQuan");
                    var lblQ = F<Label>(ov, "lblQuarantineCount");
                    Console.WriteLine("== 9: OnClick lblQuarantineCount ==");
                    typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(lblQ, new object[] { EventArgs.Empty });
                    Check("[UI] bấm số đếm Cách ly ở Tổng quan -> nhảy sang tab Cách ly",
                        panel.Controls.Count == 1 && panel.Controls[0].GetType().Name == "UcCachLy");
                    Console.WriteLine("== 9: close mf ==");
                    mf.Close();
                }
                Console.WriteLine("== SECTION 9 done ==");

                // ===== 9b. Ngôn ngữ thiết kế đồng bộ theo chuẩn tab Lịch sử =====
                // 5/5 tab phải có page-header 18pt Bold (title) + 9.75 xám (subtitle);
                // mọi GroupBox đầu tiên mỗi tab phải mang card-style 10.125 Bold BlueDark.
                Console.WriteLine("== SECTION 9b ==");
                {
                    var mfd = new ScanAndRemoveVirus.FrmMain();
                    var tabs = new[] {
                        new[] { "ucTongQuan", "lblOverviewTitle",   "lblOverviewSubtitle" },
                        new[] { "ucBaoVe",    "lblProtectionTitle", "lblProtectionSubtitle" },
                        new[] { "ucLichSu",   "lblHistoryTitle",    "lblHistorySubtitle" },
                        new[] { "ucCachLy",   "lblQuarantineTitle", "lblQuarantineSubtitle" },
                        new[] { "ucCaiDat",   "lblSettingsTitle",   "lblSettingsSubtitle" },
                    };
                    int headerOk = 0;
                    foreach (string[] tb in tabs)
                    {
                        var tabUc = F<UserControl>(mfd, tb[0]);
                        var ttl = F<Label>(tabUc, tb[1]);
                        bool ok = Math.Abs(ttl.Font.SizeInPoints - Theme.PageTitleFont.SizeInPoints) < 0.15f
                            && ttl.Font.Bold && ttl.ForeColor == Theme.TextDark;
                        if (tb[2] != null)
                        {
                            var sub = F<Label>(tabUc, tb[2]);
                            ok = ok && Math.Abs(sub.Font.SizeInPoints - Theme.PageSubFont.SizeInPoints) < 0.15f
                                && sub.ForeColor == Theme.TextGray;
                        }
                        if (ok) headerOk++;
                        else Console.WriteLine("  header lệch: " + tb[1] + " font=" + ttl.Font.Size);
                    }
                    Check("[UI] 5/5 tab có page-header chuẩn Lịch sử (18B + subtitle xám)", headerOk == 5);

                    int grpWith = 0, grpStyled = 0;
                    foreach (string[] tb in tabs)
                    {
                        var tabUc2 = F<UserControl>(mfd, tb[0]);
                        GroupBox gb = FirstGroupBox(tabUc2);
                        if (gb == null) continue;
                        grpWith++;
                        if (gb.Font.Bold && Math.Abs(gb.Font.SizeInPoints - Theme.CardTitleFont.SizeInPoints) < 0.15f
                            && gb.ForeColor == Theme.BlueDark) grpStyled++;
                        else Console.WriteLine("  card lệch: " + tb[0] + "/" + gb.Name + " font=" + gb.Font);
                    }
                    Check("[UI] mọi card GroupBox đầu tab đều card-style 10.125Bold BlueDark (" + grpStyled + "/" + grpWith + ")",
                        grpWith > 0 && grpStyled == grpWith);

                    // 9c. regression layout Tổng quan: header 58px không được chèn ép card "Quét hệ thống"
                    Console.WriteLine("== SECTION 9c ==");
                    using (var geo = new Form())
                    using (var ucX = new UcTongQuan())
                    {
                        ucX.Dock = DockStyle.Fill;
                        geo.Controls.Add(ucX);
                        geo.StartPosition = FormStartPosition.Manual;
                        geo.Location = new Point(-2600, -2600);
                        geo.ClientSize = new Size(1280, 980); // đủ cao cho toàn bộ min 832 -> không cần scroll
                        geo.Show(); Application.DoEvents();
                        var gs = F<GroupBox>(ucX, "grpScan");
                        var ga = F<GroupBox>(ucX, "grpAction");
                        var t12 = F<Control>(ucX, "tableLayoutPanel12");
                        var pn = F<Panel>(ucX, "pnlContent");
                        Console.WriteLine("  geo850 gs.H=" + gs.Height + " tlp12.H=" + t12.Height
                            + " ga.Bottom=" + ga.Bottom + " client.H=" + ucX.ClientSize.Height
                            + " tlp11.H=" + F<Control>(ucX, "tableLayoutPanel11").Height);
                        Check("[UI] TQ cửa sổ đủ cao: card Quét đủ 303px, Hàng động nằm trọn trong khung",
                            gs.Height >= 303 && t12.Height >= 306
                            && ga.Bottom <= ucX.ClientSize.Height + 2);

                        geo.ClientSize = new Size(1000, 560); // thấp hơn min -> PHẢI scroll, nội dung không bị ép
                        Application.DoEvents(); geo.Refresh();
                        gs = F<GroupBox>(ucX, "grpScan");
                        var t11 = F<Control>(ucX, "tableLayoutPanel11");
                        Console.WriteLine("  geo560 gs.H=" + gs.Height + " tlp11.H=" + t11.Height
                            + " AutoScroll=" + pn.AutoScroll);
                        // min mới theo Theme.ScrollablePage: TLP11 giữ MinimumSize 980x640 -> t11.H >= 630,
                        // card Quét vẫn nguyên 303px, AutoScroll bật thay vì ép các card
                        Check("[UI] TQ cửa sổ thấp: AutoScroll bật + card KHÔNG bị ép (giữ >=303px)",
                            pn.AutoScroll && gs.Height >= 303 && t11.Height >= 630);
                        geo.Close();
                    }
                    mfd.Close();
                }

                // ===== 10. Dọn các mục còn lại trong test ledger =====
                Console.WriteLine("== SECTION 10 ==");
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

    static GroupBox FirstGroupBox(Control root)
    {
        var q = new Queue<Control>();
        q.Enqueue(root);
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            if (c is GroupBox) return (GroupBox)c;
            foreach (Control ch in c.Controls) q.Enqueue(ch);
        }
        return null;
    }
}
