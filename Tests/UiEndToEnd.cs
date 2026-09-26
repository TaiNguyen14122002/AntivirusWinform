// Test UI đầu-cuối: dựng UserControl thật, BẤM NÚT thật, dialog tự đóng bằng closer thread.
// Chạy (từ gốc repo) — CẦN Roslyn csc (nguồn dùng C#7; csc Framework 4.0 KHÔNG compile nổi):
//   $csc = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\Roslyn\csc.exe" | Select-Object -First 1
//   & $csc /out:ui_test.exe /main:UiEndToEnd /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Net.Http.dll /r:System.Management.dll /r:Microsoft.VisualBasic.dll
//     Services\ScanEngine.cs Services\RealTimeProtection.cs Services\QuarantineLedger.cs Services\ScanHistoryStore.cs
//     Services\VirusTotalClient.cs Services\AppSettings.cs Services\FeatureFlags.cs Services\GuardService.cs
//     Services\TestSamples.cs Services\DataDir.cs Services\DirectorySizeCalculator.cs
//     Control\Theme.cs Control\UiIcons.cs Control\VtDonut.cs Control\LoadingSpinner.cs
//     Control\UcTongQuan.cs Control\UcTongQuan.Designer.cs
//                       Control\UcTongQuan.ChiTiet.cs Control\UcTongQuan.QuetNangCao.cs
//     Control\UcCachLy.cs  Control\UcCachLy.Designer.cs
//     Control\UcLichSu.cs  Control\UcLichSu.Designer.cs
//     Control\UcBaoVe.cs   Control\UcBaoVe.Designer.cs
//     Control\UcCaiDat.cs  Control\UcCaiDat.Designer.cs
//     FrmMain.cs FrmMain.Designer.cs
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
    // Kiểm tra một field KHÔNG còn tồn tại (dùng cho control đã bị gỡ khỏi giao diện)
    static bool NoField(object o, string name)
    {
        return o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) == null;
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
                // (25/09/2026) Thẻ "Tuỳ chọn quét nhanh" đã bị bỏ: "Quét ngay" + SetCustomPath = quét 1 vị trí
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
                // (25/09/2026) Thẻ "4. Đang cách ly" đã bị bỏ khỏi Tổng quan -> kiểm tra qua sổ cách ly thật
                Check("[UI] cách ly tất cả: sổ cách ly tăng đúng 2",
                    ScanEngine.CountQuarantined() == qBefore + 2);

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

                // ===== 3b. Thẻ "Tuỳ chọn quét nhanh" đã bị bỏ (25/09/2026) =====
                Check("[UI] TQ đã bỏ thẻ quét nhanh: không còn grpScan/rdo*/lblCustomPath/btn chọn tệp",
                    NoField(uc, "grpScan") && NoField(uc, "rdoQuickScan") && NoField(uc, "rdoFullScan")
                    && NoField(uc, "rdoCustomScan") && NoField(uc, "lblCustomPath")
                    && NoField(uc, "btnPickFile") && NoField(uc, "btnPickFolder"));
                Check("[UI] tableLayoutPanel12 còn 6 hàng (bỏ hàng trống 44px + hàng thẻ quét nhanh)",
                    F<TableLayoutPanel>(uc, "tableLayoutPanel12").RowStyles.Count == 6);
                // (25/09/2026, lần 2) Bỏ thanh loading quét cũ + cả khối "cập nhật dữ liệu" + thẻ "4. Đang cách ly"
                Check("[UI] TQ đã bỏ THANH loading quét cũ (pgbScan) — chỉ còn nhãn trạng thái quét",
                    NoField(uc, "pgbScan"));
                // (25/09/2026, lần 9) ...nhưng loading nay được DỰNG LẠI theo thiết kế mới: dải `pnlLoadingQuet` ở
                // hàng 0 của tableLayoutPanel11 (KHÔNG phải ProgressBar cũ), gồm vòng xoay GDI+ + 2 nhãn + nút hủy.
                Check("[UI] TQ có dải loading quét mới (lần 9): hàng 0 của tableLayoutPanel11 = 0px + ẩn khi rảnh",
                    F<Panel>(uc, "pnlLoadingQuet") != null
                    && F<LoadingSpinner>(uc, "spinnerDangQuet") != null
                    && F<Button>(uc, "btnHuyQuetLoading").Text == "Hủy quét"
                    && !F<Panel>(uc, "pnlLoadingQuet").Visible
                    && F<TableLayoutPanel>(uc, "tableLayoutPanel11").RowStyles.Count == 2
                    && Math.Abs(F<TableLayoutPanel>(uc, "tableLayoutPanel11").RowStyles[0].Height) < 0.5F
                    && F<TableLayoutPanel>(uc, "tableLayoutPanel11").GetRow(F<Control>(uc, "pnlLoadingQuet")) == 0
                    && F<TableLayoutPanel>(uc, "tableLayoutPanel11").GetRow(F<Control>(uc, "tableLayoutPanel12")) == 1
                    && F<TableLayoutPanel>(uc, "tableLayoutPanel12").RowStyles.Count == 6);
                Check("[UI] TQ đã bỏ khối cập nhật dữ liệu (chip CSDL/Cập nhật cuối + nút Kiểm tra cập nhật)",
                    NoField(uc, "lblDatabaseTitle") && NoField(uc, "lblDatabaseValue")
                    && NoField(uc, "lblLastUpdate") && NoField(uc, "lblLastUpdateTitle")
                    && NoField(uc, "btnCheckUpdate"));
                Check("[UI] TQ đã bỏ thẻ \"Đang cách ly\": hàng số liệu còn 3 thẻ",
                    NoField(uc, "grpQuarantine") && NoField(uc, "lblQuarantineCount")
                    && F<TableLayoutPanel>(uc, "pnlThongKe").ColumnCount == 3);
                // (25/09/2026, lần 4) Bố cục lại: gỡ hàng header riêng (pnlOverviewHeader), đưa 2 nút quét
                // xuống ngay dưới khối trạng thái (a) — hàng 4 của tlpAnToanText, "Quét ngay" ở bên trái
                Check("[UI] TQ gọn bố cục: bỏ hàng header/dải trạng thái/chip; 2 nút quét nằm trong khối (a)",
                    NoField(uc, "pnlOverviewHeader") && NoField(uc, "pnlScanStrip") && NoField(uc, "pnlChips")
                    && F<TableLayoutPanel>(uc, "tlpAnToanText").RowStyles.Count == 4
                    && ReferenceEquals(F<Control>(uc, "flowHeaderActions").Parent,
                        F<Control>(uc, "tlpAnToanText"))
                    && F<FlowLayoutPanel>(uc, "flowHeaderActions").Controls[0] == F<Control>(uc, "btnScanNow"));
                // Nút Chọn tệp/Chọn thư mục mở dialog HỆ THỐNG (không auto được) -> chỉ còn logic sau dialog:
                // SetCustomPath(path) => "Quét ngay" chuyển sang chế độ Quét tùy chọn đúng vị trí đó
                typeof(UcTongQuan).GetMethod("SetCustomPath", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(uc, new object[] { Path.Combine(root, "sau-dialog.txt") });
                Check("[UI] SetCustomPath: đường dẫn tùy chọn -> chế độ quét là Quét tùy chọn",
                    (ScanType)typeof(UcTongQuan)
                        .GetMethod("GetSelectedScanType", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(uc, null) == ScanType.Custom);

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
                F<Button>(uc, "btnScanNow").PerformClick();
                // (25/09/2026, lần 9) Dải loading quét phải hiện NGAY trong cùng nhịp bấm: `ChayPhienQuet` gọi
                // `SetScanning(true)` (=> HienThiDaiLoading) TRƯỚC `await Task.Run(...)` nên trạng thái đã đổi
                // xong khi PerformClick trả về — không phải chờ Timer hay BeginInvoke nào.
                var t11c = F<TableLayoutPanel>(uc, "tableLayoutPanel11");
                Check("[UI] bấm \"Quét ngay\" -> dải loading hiện NGAY: hàng 0 giãn 0 -> 46px + vòng xoay chạy",
                    F<Panel>(uc, "pnlLoadingQuet").Visible
                    && Math.Abs(t11c.RowStyles[0].Height - 46F) < 0.5F
                    && F<Label>(uc, "lblLoadingTieuDe").Text == "Đang quét…"
                    && F<Label>(uc, "lblLoadingChiTiet").Text.Contains("Hủy quét")
                    && F<Button>(uc, "btnHuyQuetLoading").Visible);
                int gocTruoc = F<LoadingSpinner>(uc, "spinnerDangQuet").Goc;
                PumpMs(350); // đang quét dở
                int gocSau = F<LoadingSpinner>(uc, "spinnerDangQuet").Goc;
                Check("[UI] vòng xoay loading QUAY thật trong lúc quét (góc đổi sau 350ms)",
                    gocSau != gocTruoc);
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
                int gocDung1 = F<LoadingSpinner>(uc, "spinnerDangQuet").Goc;
                PumpMs(300);
                Check("[UI] hết phiên: dải loading ẩn, hàng 0 về 0px, vòng xoay DỪNG (không tốn Timer khi rảnh)",
                    !F<Panel>(uc, "pnlLoadingQuet").Visible
                    && Math.Abs(t11c.RowStyles[0].Height) < 0.5F
                    && F<Label>(uc, "lblLoadingChiTiet").Text.Contains("hủy")
                    && F<LoadingSpinner>(uc, "spinnerDangQuet").Goc == gocDung1);

                // ===== 3d. Nút Tra VirusTotal (key giả -> đi hết luồng xử lý, 401 hay lỗi mạng đều được) =====
                string vtKeyBak = File.Exists(VirusTotalClient.ApiKeyPath)
                    ? File.ReadAllText(VirusTotalClient.ApiKeyPath) : null;
                try
                {
                    File.WriteAllText(Path.Combine(root, "vtprobe.txt"), ScanEngine.TestSignature + "vt button flow!!");
                    SetF(uc, "customScanPath", Path.Combine(root, "vtprobe.txt"));
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
                SetF(uc, "customScanPath", null);

                // ===== 3e. Màn hình riêng (c)/(d): hàng 3 thẻ số liệu BIẾN MẤT — (c) lần 8, (d) lần 5 =====
                // (c) "Chi tiết kết quả quét" và (d) "Quét nâng cao" đều KHÔNG phải Tổng quan: hàng 3 thẻ
                // (Mối đe dọa / Tệp đã quét / Lần quét gần nhất) chỉ thuộc (a)/(b) -> mở (c)/(d) phải ẩn HẲN
                // (hàng hạ về 0px) để nhường chiều cao cho bảng chi tiết / vùng chọn chế độ quét.
                var viewTruoc = (TongQuanView)F<object>(uc, "currentView");
                var tlp12 = F<TableLayoutPanel>(uc, "tableLayoutPanel12");
                int hangThongKe = tlp12.GetRow(F<Control>(uc, "pnlThongKe"));
                // -- (c): mở đúng đường đi của người dùng (nút "Xem chi tiết" / link "Xem tất cả")
                uc.MoChiTietKetQua(0);
                PumpMs(300);
                Check("[UI] mở trang (c) Chi tiết kết quả quét: 3 thẻ số liệu biến mất (hàng về 0px)",
                    F<Control>(uc, "pnlChiTietKetQua").Visible
                    && !F<Control>(uc, "pnlThongKe").Visible
                    && !F<GroupBox>(uc, "grpThreats").Visible
                    && !F<GroupBox>(uc, "grpScannedFiles").Visible
                    && !F<GroupBox>(uc, "grpLastScan").Visible
                    && Math.Abs(tlp12.RowStyles[hangThongKe].Height) < 0.5F);
                F<Button>(uc, "btnQuayLaiChiTiet").PerformClick();   // nút "← Quay lại" của trang (c)
                PumpMs(300);
                Check("[UI] rời trang (c) bằng \"← Quay lại\": 3 thẻ số liệu hiện lại đúng 104px (trạng thái (b))",
                    !F<Control>(uc, "pnlChiTietKetQua").Visible
                    && F<Control>(uc, "pnlPhatHienDeDoa").Visible
                    && F<Control>(uc, "pnlThongKe").Visible
                    && F<GroupBox>(uc, "grpThreats").Visible
                    && Math.Abs(tlp12.RowStyles[hangThongKe].Height - 104F) < 0.5F);
                // -- (d): dropdown "⚙ Quét nâng cao ▾" -> mở trang (d) chế độ quét toàn bộ hệ thống
                uc.MoQuetNangCao(CheDoQuetNangCao.FullSystem);
                PumpMs(250);
                Check("[UI] sang trang Quét nâng cao: 3 thẻ số liệu (đe dọa/tệp đã quét/lần quét) biến mất",
                    F<Control>(uc, "pnlQuetNangCao").Visible
                    && !F<Control>(uc, "pnlThongKe").Visible
                    && !F<GroupBox>(uc, "grpThreats").Visible
                    && !F<GroupBox>(uc, "grpScannedFiles").Visible
                    && !F<GroupBox>(uc, "grpLastScan").Visible
                    && Math.Abs(tlp12.RowStyles[hangThongKe].Height) < 0.5F);
                // ===== 3f. Trang (d): chuyển QUA LẠI giữa 4 lựa chọn chế độ (25/09/2026, lần 6) =====
                // Cột trái có 4 thẻ (Toàn bộ hệ thống / Thư mục / Tệp / Tùy chỉnh). 4 thẻ nằm trong 4
                // container riêng nên WinForms KHÔNG tự bỏ chọn thẻ cũ (radio chỉ loại trừ trong cùng
                // parent) -> nếu không tự đồng bộ: bấm sang thẻ khác rồi bấm LẠI thẻ đầu thì thẻ đầu vẫn
                // Checked = true, không có CheckedChanged => cột phải kẹt ở chế độ vừa chọn.
                var theCheDo = F<RadioButton[]>(uc, "theCheDo");
                var soTheDuocChon = new Func<int>(delegate
                {
                    int n = 0;
                    foreach (RadioButton r in theCheDo) if (r.Checked) n++;
                    return n;
                });
                int soODia = F<DataGridView>(uc, "dgvODia").Rows.Count;
                bool batDauTruoc = F<Button>(uc, "btnBatDauQuet").Enabled;
                Check("[UI] trang (d) vừa mở: đúng 1/4 thẻ chế độ được chọn (thẻ \"Quét toàn bộ hệ thống\")",
                    theCheDo.Length == 4 && soTheDuocChon() == 1 && theCheDo[0].Checked
                    && F<Panel>(uc, "pnlFullSystem").Visible);
                theCheDo[1].PerformClick();                             // bấm thẻ "Quét thư mục"
                PumpMs(150);
                Check("[UI] bấm thẻ \"Quét thư mục\": thẻ cũ BỎ CHỌN + cột phải sang nội dung thư mục",
                    soTheDuocChon() == 1 && theCheDo[1].Checked && !theCheDo[0].Checked
                    && F<Panel>(uc, "pnlFolder").Visible && !F<Panel>(uc, "pnlFullSystem").Visible);
                Check("[UI] nhãn tóm tắt đổi theo thẻ đang chọn: \"Đang chọn: Quét thư mục\"",
                    F<Label>(uc, "lblCheDoTomTat").Text.Contains("Quét thư mục"));
                theCheDo[3].PerformClick();                             // bấm thẻ "Quét tùy chỉnh"
                PumpMs(150);
                Check("[UI] bấm thẻ \"Quét tùy chỉnh\": vẫn đúng 1 thẻ được chọn + cột phải sang nội dung tùy chỉnh",
                    soTheDuocChon() == 1 && theCheDo[3].Checked && !theCheDo[1].Checked
                    && F<Panel>(uc, "pnlCustom").Visible);
                theCheDo[2].PerformClick();                             // bấm thẻ "Quét tệp"
                PumpMs(150);
                Check("[UI] bấm thẻ \"Quét tệp\": vẫn đúng 1 thẻ được chọn + cột phải sang nội dung tệp",
                    soTheDuocChon() == 1 && theCheDo[2].Checked && !theCheDo[3].Checked
                    && F<Panel>(uc, "pnlFiles").Visible);
                theCheDo[0].PerformClick();                             // QUAY LẠI thẻ đầu
                PumpMs(150);
                Check("[UI] quay lại thẻ \"Quét toàn bộ hệ thống\": 3 thẻ kia bỏ chọn + cột phải về đúng chế độ",
                    soTheDuocChon() == 1 && theCheDo[0].Checked
                    && !theCheDo[1].Checked && !theCheDo[2].Checked && !theCheDo[3].Checked
                    && F<Panel>(uc, "pnlFullSystem").Visible && !F<Panel>(uc, "pnlFolder").Visible
                    && !F<Panel>(uc, "pnlFiles").Visible && !F<Panel>(uc, "pnlCustom").Visible);
                Check("[UI] nhãn tóm tắt về lại \"Đang chọn: Quét toàn bộ hệ thống\"",
                    F<Label>(uc, "lblCheDoTomTat").Text.Contains("Quét toàn bộ hệ thống"));
                Check("[UI] thẻ đang chọn có viền dày hơn thẻ chưa chọn (dấu hiệu nhận biết trên giao diện)",
                    theCheDo[0].FlatAppearance.BorderSize > theCheDo[1].FlatAppearance.BorderSize);
                Check("[UI] đi qua lại 4 thẻ: dữ liệu ổ đĩa + trạng thái nút \"Bắt đầu quét\" giữ nguyên",
                    F<DataGridView>(uc, "dgvODia").Rows.Count == soODia
                    && F<Button>(uc, "btnBatDauQuet").Enabled == batDauTruoc);
                F<Button>(uc, "btnQuayLaiNangCao").PerformClick();   // nút "← Quay lại" của trang (d)
                PumpMs(500);
                Check("[UI] quay lại từ Quét nâng cao: 3 thẻ số liệu hiện lại, hàng về đúng 104px",
                    !F<Control>(uc, "pnlQuetNangCao").Visible
                    && F<Control>(uc, "pnlThongKe").Visible
                    && F<GroupBox>(uc, "grpThreats").Visible && F<GroupBox>(uc, "grpLastScan").Visible
                    && Math.Abs(tlp12.RowStyles[hangThongKe].Height - 104F) < 0.5F);
                // Trả lại đúng màn hình cho các section sau (HienThi(viewTruoc, refresh:false))
                typeof(UcTongQuan).GetMethod("HienThi", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(uc, new object[] { viewTruoc, false });
                PumpMs(150);

                // ===== 3g. Trang (d) RESPONSIVE: tự xếp lại bố cục theo bề rộng cửa sổ (25/09/2026, lần 7) =====
                // Mở trang (d) trên một cửa sổ RIÊNG rồi thu hẹp dần (1400 -> 950 -> 650 -> 1400):
                //   • >= 1000px: 2 cột — 4 thẻ xếp dọc bên trái, nội dung bên phải (thiết kế cũ);
                //   • 800..999px: 4 thẻ về 1 hàng ngang trên đầu, nội dung chiếm trọn bề ngang bên dưới;
                //   • < 800px  : 4 thẻ về lưới 2x2, nội dung vẫn ở dưới.
                // Nhãn "Đang chọn: …" + nút "Bắt đầu quét" phải xuống hàng riêng khi hẹp (không bị cắt) và
                // 4 thẻ chế độ phải GIỮ NGUYÊN control (không dựng lại) nên chế độ + dữ liệu còn nguyên.
                Console.WriteLine("== SECTION 3g ==");
                using (var geoR = new Form())
                using (var ucR = new UcTongQuan())
                {
                    ucR.Dock = DockStyle.Fill;
                    geoR.Controls.Add(ucR);
                    geoR.StartPosition = FormStartPosition.Manual;
                    geoR.Location = new Point(-2600, -2600);
                    geoR.ClientSize = new Size(1400, 980);
                    geoR.Show(); Application.DoEvents();
                    var t11r = F<Control>(ucR, "tableLayoutPanel11");
                    var pgr = F<Panel>(ucR, "pnlQuetNangCao");
                    var than = F<TableLayoutPanel>(ucR, "tlpThanNangCao");
                    var cotThe = F<TableLayoutPanel>(ucR, "pnlCheDoTrai");
                    var headR = F<TableLayoutPanel>(ucR, "tlpHeaderNangCao");
                    var chanR = F<TableLayoutPanel>(ucR, "tlpChanNangCao");
                    var tomTatR = F<Label>(ucR, "lblCheDoTomTat");
                    var nutQuet = F<Button>(ucR, "btnBatDauQuet");
                    var phaiR = F<TableLayoutPanel>(ucR, "pnlNoiDungPhai");
                    var oTheR = F<TableLayoutPanel[]>(ucR, "oTheCheDo");
                    var theCheDoR = F<RadioButton[]>(ucR, "theCheDo");
                    Check("[UI] TQ (a): bề rộng tối thiểu vùng trang vẫn 980px (ngưỡng cũ của (a)/(b)/(c))",
                        t11r.MinimumSize.Width == 980 && t11r.MinimumSize.Height == 640);
                    ucR.MoQuetNangCao(CheDoQuetNangCao.Files);   // chế độ "Quét tệp" -> kiểm tra giữ trạng thái
                    PumpMs(200);
                    Check("[UI] mở (d): hạ bề rộng tối thiểu (để tự xếp lại) nhưng giữ chiều cao tối thiểu 640",
                        t11r.MinimumSize.Width <= 480 && t11r.MinimumSize.Height == 640);
                    Console.WriteLine("  geoRong page.W=" + pgr.ClientSize.Width + " than.Cols=" + than.ColumnCount
                        + " cotThe=" + cotThe.ColumnCount + "x" + cotThe.RowCount);
                    Check("[UI] (d) vùng trang RỘNG (>=1000px): 2 cột — 4 thẻ dọc bên trái, nội dung bên phải",
                        than.ColumnCount == 2 && than.GetColumn(cotThe) == 0 && than.GetColumn(phaiR) == 1
                        && cotThe.ColumnCount == 1 && cotThe.RowCount == 5
                        && cotThe.GetRow(oTheR[0]) == 0 && cotThe.GetRow(oTheR[3]) == 3
                        && headR.RowCount == 1 && chanR.ColumnCount == 2 && chanR.GetRow(nutQuet) == 0);
                    geoR.ClientSize = new Size(950, 980);        // VỪA: 800..999px
                    PumpMs(200); ucR.PerformLayout(); Application.DoEvents();
                    Console.WriteLine("  geoVua page.W=" + pgr.ClientSize.Width + " than.Rows=" + than.RowCount
                        + " cotThe=" + cotThe.ColumnCount + "x" + cotThe.RowCount + " head.Rows=" + headR.RowCount);
                    Check("[UI] (d) vùng trang VỪA (800..999px): 4 thẻ về 1 HÀNG ngang trên đầu, nội dung xuống dưới",
                        than.ColumnCount == 1 && than.RowCount == 2
                        && than.GetRow(cotThe) == 0 && than.GetRow(phaiR) == 1
                        && cotThe.ColumnCount == 4 && cotThe.RowCount == 1
                        && cotThe.GetColumn(oTheR[3]) == 3 && cotThe.GetRow(oTheR[0]) == 0
                        && headR.RowCount == 2 && chanR.ColumnCount == 1 && chanR.RowCount == 2);
                    Check("[UI] (d) hẹp: nhãn tóm tắt xuống hàng dưới (trải hết bề ngang) + nút \"Bắt đầu quét\" giãn hết",
                        tomTatR.Dock == DockStyle.Fill && headR.GetRow(tomTatR) == 1 && headR.GetColumnSpan(tomTatR) == 3
                        && nutQuet.Dock == DockStyle.Fill && chanR.GetRow(nutQuet) == 1);
                    Check("[UI] (d) hẹp: nút + nội dung + nhãn đều nằm TRONG vùng trang (không bị cắt ngang)",
                        nutQuet.Right <= chanR.ClientSize.Width + 1 && phaiR.Right <= than.ClientSize.Width + 1
                        && tomTatR.Right <= headR.ClientSize.Width + 1 && than.Bottom <= pgr.ClientSize.Height + 1
                        && F<Panel>(ucR, "pnlContent").ClientSize.Width >= t11r.Width - 1);
                    geoR.ClientSize = new Size(650, 980);        // HẸP: < 800px
                    PumpMs(200); ucR.PerformLayout(); Application.DoEvents();
                    Console.WriteLine("  geoHep page.W=" + pgr.ClientSize.Width + " cotThe=" + cotThe.ColumnCount
                        + "x" + cotThe.RowCount);
                    Check("[UI] (d) vùng trang HẸP (<800px): 4 thẻ về LƯỚI 2x2, nội dung vẫn ở dưới",
                        than.ColumnCount == 1 && than.GetRow(cotThe) == 0 && than.GetRow(phaiR) == 1
                        && cotThe.ColumnCount == 2 && cotThe.RowCount == 2
                        && cotThe.GetColumn(oTheR[1]) == 1 && cotThe.GetRow(oTheR[2]) == 1
                        && cotThe.GetColumn(oTheR[3]) == 1 && cotThe.GetRow(oTheR[3]) == 1);
                    Check("[UI] (d) hẹp nhất: thẻ cuối + nút vẫn nằm trong vùng trang (không cắt ngang/dọc)",
                        oTheR[3].Right <= cotThe.ClientSize.Width + 1 && oTheR[3].Bottom <= cotThe.ClientSize.Height + 1
                        && nutQuet.Right <= chanR.ClientSize.Width + 1 && than.Bottom <= pgr.ClientSize.Height + 1
                        && pgr.ClientSize.Width >= 400);
                    geoR.ClientSize = new Size(1400, 980);       // RỘNG LẠI
                    PumpMs(200); ucR.PerformLayout(); Application.DoEvents();
                    Check("[UI] (d) rộng lại: về 2 cột + chế độ \"Quét tệp\" và dữ liệu ổ đĩa GIỮ NGUYÊN (không dựng lại control)",
                        than.ColumnCount == 2 && cotThe.ColumnCount == 1 && cotThe.RowCount == 5
                        && headR.RowCount == 1 && nutQuet.Dock == DockStyle.Right && cotThe.Controls.Count == 4
                        && ReferenceEquals(oTheR[0], F<TableLayoutPanel[]>(ucR, "oTheCheDo")[0])
                        && ReferenceEquals(theCheDoR[0], F<RadioButton[]>(ucR, "theCheDo")[0])
                        && theCheDoR[2].Checked && F<Panel>(ucR, "pnlFiles").Visible
                        && F<DataGridView>(ucR, "dgvODia").Rows.Count > 0);
                    F<Button>(ucR, "btnQuayLaiNangCao").PerformClick();
                    PumpMs(200);
                    Check("[UI] đóng (d): bề rộng tối thiểu về lại 980px + trang (d) đã ẩn",
                        t11r.MinimumSize.Width == 980 && t11r.MinimumSize.Height == 640 && !pgr.Visible);
                    geoR.Close();
                }

                // ===== 3h. Dải loading quét phục vụ MỌI nút quét (25/09/2026, lần 9) =====
                // Yêu cầu thiết kế: "nhấn bất kỳ nút quét nào thì thấy loading xuất hiện". Cả 3 nút quét
                // ("Quét ngay" ở (a)/(b), "Quét lại" ở (b), "Bắt đầu quét" ở (d)) đều chạy qua ChayPhienQuet
                // -> SetScanning(true), nên chỉ cần 1 chỗ HienThiDaiLoading là mọi nút đều có loading. Ở (d)
                // nút "Bắt đầu quét" bị khoá khi phiên chạy -> nút "Hủy quét" của dải loading là đường hủy duy nhất.
                var t11h = F<TableLayoutPanel>(uc, "tableLayoutPanel11");
                var daiH = F<Panel>(uc, "pnlLoadingQuet");
                // 3c vừa quét dở ĐÚNG corpus này nên cache có thể khiến lượt 2 xong gần như tức thì -> xoá cache
                // để phiên quét chắc chắn còn chạy khi ta bấm Hủy (giống giả định của 3c: ~0.3s/3300 tệp).
                ScanEngine.ClearScanCache();
                SetF(uc, "customScanPath", cancelDir);
                F<Button>(uc, "btnScanNow").PerformClick();
                PumpMs(400);
                F<Button>(uc, "btnHuyQuetLoading").PerformClick();   // hủy bằng nút CỦA DẢI LOADING
                var hUntil = DateTime.Now.AddSeconds(25);
                while (DateTime.Now < hUntil && (bool)F<object>(uc, "isScanning"))
                { Application.DoEvents(); Thread.Sleep(15); }
                PumpMs(700);
                Check("[UI] hủy bằng nút của dải loading: phiên dừng, dải ẩn, hàng 0 về 0px, nút về 'Quét ngay'",
                    !(bool)F<object>(uc, "isScanning") && !daiH.Visible
                    && Math.Abs(t11h.RowStyles[0].Height) < 0.5F
                    && F<Label>(uc, "lblScanProgress").Text.Contains("hủy")
                    && F<Button>(uc, "btnScanNow").Text == "Quét ngay");
                SetF(uc, "customScanPath", null);

                // -- Nút "Bắt đầu quét" của trang (d): dải loading cũng phải hiện (không chỉ ở (a)/(b))
                // Lượt quét ngay trước cũng đã đi qua corpus này -> xoá cache lần nữa để phiên (d) chạy đủ lâu.
                ScanEngine.ClearScanCache();
                uc.MoQuetNangCao(CheDoQuetNangCao.Folder);
                PumpMs(250);
                bool themDuoc = (bool)typeof(UcTongQuan)
                    .GetMethod("ThemThuMuc", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(uc, new object[] { cancelDir });
                PumpMs(300);
                var nutBatDauH = F<Button>(uc, "btnBatDauQuet");
                Check("[UI] (d) chế độ Thư mục: thêm được 1 vị trí -> nút \"Bắt đầu quét\" bật, dải loading còn ẩn",
                    themDuoc && nutBatDauH.Enabled && !daiH.Visible
                    && Math.Abs(t11h.RowStyles[0].Height) < 0.5F);
                nutBatDauH.PerformClick();
                Check("[UI] bấm \"Bắt đầu quét\" ở (d) -> dải loading hiện NGAY + nút bị khoá (chống double click)",
                    daiH.Visible && !nutBatDauH.Enabled
                    && Math.Abs(t11h.RowStyles[0].Height - 46F) < 0.5F
                    && F<Button>(uc, "btnHuyQuetLoading").Visible);
                int gocDauH = F<LoadingSpinner>(uc, "spinnerDangQuet").Goc;
                PumpMs(400);
                Check("[UI] đang quét từ (d): vòng xoay vẫn quay (loading sống) + vẫn ở trang (d)",
                    F<LoadingSpinner>(uc, "spinnerDangQuet").Goc != gocDauH
                    && F<Control>(uc, "pnlQuetNangCao").Visible);
                F<Button>(uc, "btnHuyQuetLoading").PerformClick();
                var dUntil = DateTime.Now.AddSeconds(25);
                while (DateTime.Now < dUntil && (bool)F<object>(uc, "isScanning"))
                { Application.DoEvents(); Thread.Sleep(15); }
                PumpMs(700);
                Check("[UI] hủy phiên khởi động từ (d): dải loading ẩn, hàng 0 về 0px, nút \"Bắt đầu quét\" bật lại",
                    !(bool)F<object>(uc, "isScanning") && !daiH.Visible
                    && Math.Abs(t11h.RowStyles[0].Height) < 0.5F
                    && F<Button>(uc, "btnBatDauQuet").Enabled
                    && F<Control>(uc, "pnlQuetNangCao").Visible);
                // Trả lại đúng màn hình cho các section sau
                typeof(UcTongQuan).GetMethod("HienThi", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(uc, new object[] { viewTruoc, false });
                PumpMs(200);

                // ===== 4. Tự động cập nhật CSDL (nút "Kiểm tra cập nhật" đã bỏ khỏi Tổng quan 25/09/2026) =====
                // Đường cập nhật còn lại là tự động theo hạn 24h (GuardService) -> đóng tem + ghi 1 dòng
                // "Cập nhật CSDL" vào scanhistory.log; dòng đó phải hiện trong bảng "Hoạt động gần đây".
                if (FeatureFlags.AutoUpdateEnabled)
                {
                    ScanHistoryStore.MarkSignatureUpdated(DateTime.Now.AddHours(-30)); // tem cũ -> chắc chắn chạy
                    bool vuaChay = GuardService.EnsureDailyAutoUpdate();
                    PumpMs(300);
                    Check("[UI] tự động cập nhật 24h: đóng tem mới + ghi lịch sử 'Cập nhật CSDL'",
                        vuaChay && ScanHistoryStore.Entries().Any(h => h.Type == "Cập nhật CSDL"));
                    InvokeM(uc, "LoadActivity");
                    Check("[UI] bảng Hoạt động gần đây có dòng 'Cập nhật ...' (không còn UI cập nhật thủ công)",
                        F<DataGridView>(uc, "dgvActivity").Rows.Cast<DataGridViewRow>()
                            .Any(r => Convert.ToString(r.Cells[1].Value).StartsWith("Cập nhật")));
                }
                else
                    Console.WriteLine("  [bỏ qua] 'Tự động cập nhật' đang tắt trong .ini");

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
                // (25/09/2026) Ô đếm cách ly ở Tổng quan đã bỏ -> kiểm tra đồng bộ qua sổ cách ly thật
                Check("[UI] sổ cách ly đã trả hết 2 tệp test",
                    !ScanEngine.ListQuarantined().Any(x => x.OriginalPath.StartsWith(root)));
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

                // ===== 6. Tab Lịch sử: giao diện mới theo ảnh 26/09/2026 (README §3) =====
                Console.WriteLine("== SECTION 6 ==");
                var hist = new UcLichSu();
                hist.Dock = DockStyle.Fill;
                form.Controls.Add(hist);
                Application.DoEvents();
                var grid = F<DataGridView>(hist, "dgvHistory");
                int ciTime = F<DataGridViewColumn>(hist, "colScanTime").Index;
                int ciType = F<DataGridViewColumn>(hist, "colScanType").Index;
                int ciResult = F<DataGridViewColumn>(hist, "colResult").Index;
                int ciFiles = F<DataGridViewColumn>(hist, "colFileCount").Index;
                int ciThreats = F<DataGridViewColumn>(hist, "colThreatCount").Index;
                int ciDur = F<DataGridViewColumn>(hist, "colDuration").Index;
                int ciView = F<DataGridViewColumn>(hist, "colView").Index;
                var cboLoai = F<ComboBox>(hist, "cboLoaiQuet");
                var dtpTu = F<DateTimePicker>(hist, "dtpTuNgay");
                var dtpDen = F<DateTimePicker>(hist, "dtpDenNgay");
                var lblTong = F<Label>(hist, "lblTotal");
                var lblTrang = F<Label>(hist, "lblPage");
                var lblTrong = F<Label>(hist, "lblEmpty");
                var btnTruoc = F<Button>(hist, "btnPrev");
                var btnSau = F<Button>(hist, "btnNext");
                var btnLoc = F<Button>(hist, "btnLoc");
                var menu = F<ContextMenuStrip>(hist, "menuLichSu");
                var mLamMoi = F<ToolStripMenuItem>(hist, "miLamMoi");
                var mXoa = F<ToolStripMenuItem>(hist, "miXoaDong");
                var mChiTiet = F<ToolStripMenuItem>(hist, "miXemChiTiet");
                var mPhien = F<ToolStripMenuItem>(hist, "miXemPhienQuet");
                var mCanhBao = F<ToolStripMenuItem>(hist, "miXemCanhBao");
                var mCapNhat = F<ToolStripMenuItem>(hist, "miXemCapNhat");

                // ----- 6a. Bố cục tĩnh: 7 cột đúng thứ tự, KHÔNG còn cột Chọn / nút xóa hàng loạt -----
                Console.WriteLine("== SECTION 6a ==");
                Check("[UI] Lịch sử: đúng 7 cột theo thứ tự ảnh (Thời gian/Loại quét/Kết quả/Số tệp/Đe dọa/Thời lượng/Chi tiết)",
                    grid.Columns.Count == 7 && ciTime == 0 && ciType == 1 && ciResult == 2 && ciFiles == 3
                    && ciThreats == 4 && ciDur == 5 && ciView == 6
                    && F<DataGridViewColumn>(hist, "colScanTime").HeaderText == "Thời gian"
                    && F<DataGridViewColumn>(hist, "colScanType").HeaderText == "Loại quét"
                    && F<DataGridViewColumn>(hist, "colResult").HeaderText == "Kết quả"
                    && F<DataGridViewColumn>(hist, "colFileCount").HeaderText == "Số tệp quét"
                    && F<DataGridViewColumn>(hist, "colThreatCount").HeaderText == "Số mối đe dọa"
                    && F<DataGridViewColumn>(hist, "colDuration").HeaderText == "Thời gian"
                    && F<DataGridViewColumn>(hist, "colView").HeaderText == "Chi tiết");
                Check("[UI] Lịch sử: đã GỠ cột tick colPick + nút xóa hàng loạt + bộ nút lọc cũ",
                    NoField(hist, "colPick") && NoField(hist, "btnDeleteHistory") && NoField(hist, "lblSelection")
                    && NoField(hist, "btnFilterAll") && NoField(hist, "btnFilterThreats")
                    && NoField(hist, "btnFilterUpdates") && NoField(hist, "btnRefreshHistory")
                    && NoField(hist, "btnViewDetail") && NoField(hist, "lblStats"));
                Check("[UI] Lịch sử: lưới chỉ-đọc, chọn cả hàng, header + hàng cao 46–50px, cột giãn Fill",
                    grid.ReadOnly && !grid.AllowUserToAddRows && !grid.RowHeadersVisible
                    && grid.SelectionMode == DataGridViewSelectionMode.FullRowSelect
                    && grid.AutoSizeColumnsMode == DataGridViewAutoSizeColumnsMode.Fill
                    && grid.ColumnHeadersHeight >= 46 && grid.ColumnHeadersHeight <= 50
                    && grid.RowTemplate.Height >= 46 && grid.RowTemplate.Height <= 50);
                var ttlH = F<Label>(hist, "lblHistoryTitle");
                var subH = F<Label>(hist, "lblHistorySubtitle");
                Check("[UI] Lịch sử: tiêu đề 'Lịch sử' 24–26pt đậm + phụ đề 10–11pt xám đúng câu của ảnh",
                    ttlH.Text == "Lịch sử" && Math.Abs(ttlH.Font.SizeInPoints - 25f) < 0.15f && ttlH.Font.Bold
                    && Math.Abs(subH.Font.SizeInPoints - 10.5f) < 0.15f
                    && subH.Text == "Xem lại các lần quét và những mối đe dọa đã được xử lý.");
                Check("[UI] Lịch sử: hàng bộ lọc đủ Từ ngày/Đến ngày/Loại quét + nút Lọc (dd/MM/yyyy, DropDownList)",
                    dtpTu.Format == DateTimePickerFormat.Custom && dtpTu.CustomFormat == "dd/MM/yyyy"
                    && dtpDen.Format == DateTimePickerFormat.Custom && dtpDen.CustomFormat == "dd/MM/yyyy"
                    && cboLoai.DropDownStyle == ComboBoxStyle.DropDownList
                    && cboLoai.Items.Contains("Tất cả") && cboLoai.Items.Contains("Quét nhanh")
                    && (cboLoai.SelectedItem as string) == "Tất cả"
                    && btnLoc.Text == "Lọc" && btnLoc.Image != null);
                Check("[UI] Lịch sử: chân trang 'Tổng cộng: N lần quét' + nút Xem từng dòng, 10 phiên/trang",
                    lblTong.Text.StartsWith("Tổng cộng:") && lblTong.Text.EndsWith("lần quét")
                    && F<DataGridViewColumn>(hist, "colView").HeaderText == "Xem"
                    && grid.Rows.Count == Math.Min(10, ScanHistoryStore.Entries().Count(e =>
                        e.Type != "Cập nhật CSDL" && e.Type != "Bảo vệ thời gian thực")));

                // ----- 6b. Dữ liệu THẬT: phiên quét tùy chọn của section 1 + trạng thái cách ly thật -----
                Console.WriteLine("== SECTION 6b ==");
                int truocLoc = grid.Rows.Count;
                var rowQuet = grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r =>
                    Convert.ToString(r.Cells[ciType].Value) == "Quét tùy chọn"
                    && Convert.ToInt32(r.Cells[ciThreats].Value) == 2);
                Check("[UI] 6b: bảng có dòng THẬT của phiên quét tùy chọn (2 tệp, 2 đe dọa, phạm vi = thư mục test)",
                    rowQuet != null && Convert.ToInt32(rowQuet.Cells[ciFiles].Value) == 2
                    && rowQuet.Tag is HistoryEntry && ((HistoryEntry)rowQuet.Tag).Scope.Contains(root));
                var entryQuet = rowQuet == null ? null : rowQuet.Tag as HistoryEntry;
                string ketQuaMongDoi = "<không có dòng>";
                if (entryQuet != null)
                {
                    bool coBangChung = ScanEngine.ListQuarantined().Any(it =>
                        it.DetectedTime >= entryQuet.Time.AddSeconds(-5)
                        && it.DetectedTime <= entryQuet.Time.AddSeconds(Math.Max(1.0, entryQuet.Seconds) + 60));
                    ketQuaMongDoi = coBangChung ? "Đã cách ly" : "Phát hiện mối đe dọa";
                }
                Check("[UI] 6b: cột Kết quả = '" + ketQuaMongDoi + "' — khớp ĐÚNG bằng chứng sổ cách ly thật (không bịa)",
                    entryQuet != null && Convert.ToString(rowQuet.Cells[ciResult].Value) == ketQuaMongDoi);
                Check("[UI] 6b: cột dữ liệu đúng KIỂU thật (DateTime + TimeSpan; log thiếu thời lượng -> '—')",
                    rowQuet != null && rowQuet.Cells[ciTime].Value is DateTime
                    && (rowQuet.Cells[ciDur].Value is TimeSpan
                        || Convert.ToString(rowQuet.Cells[ciDur].Value) == "—")
                    && F<DataGridViewColumn>(hist, "colScanTime").DefaultCellStyle.Format == "dd/MM/yyyy HH:mm");
                // Bộ lọc chỉ áp dụng khi nhấn Lọc (§3.2)
                cboLoai.SelectedItem = "Quét toàn bộ";
                PumpMs(80);
                Check("[UI] 6b: đổi combo Loại quét nhưng CHƯA nhấn Lọc -> bảng giữ nguyên",
                    grid.Rows.Count == truocLoc);
                cboLoai.SelectedItem = "Quét tùy chọn";
                btnLoc.PerformClick();
                PumpMs(200);
                int soDongQuetTuyChon = grid.Rows.Count;
                Check("[UI] 6b: nhấn Lọc với Loại quét = 'Quét tùy chọn' -> mọi dòng đúng loại đó",
                    soDongQuetTuyChon > 0 && grid.Rows.Cast<DataGridViewRow>()
                        .All(r => Convert.ToString(r.Cells[ciType].Value) == "Quét tùy chọn")
                    && lblTong.Text.EndsWith("lần quét"));
                // Enter trong vùng lọc cũng kích hoạt Lọc; "Tất cả" không làm mất phiên quét (§3.2)
                cboLoai.SelectedItem = "Tất cả";
                typeof(UcLichSu).GetMethod("FilterKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(hist, new object[] { cboLoai, new KeyEventArgs(Keys.Enter) });
                PumpMs(200);
                Check("[UI] 6b: Enter trong vùng lọc -> áp dụng 'Tất cả' mà không mất phiên quét nào",
                    grid.Rows.Count >= soDongQuetTuyChon
                    && grid.Rows.Cast<DataGridViewRow>().Count(r =>
                        Convert.ToString(r.Cells[ciType].Value) == "Quét tùy chọn") == soDongQuetTuyChon);


                // ----- 6c. Phân trang 10 phiên/trang + tổng số + nút Trước/Sau + sắp xếp theo header -----
                Console.WriteLine("== SECTION 6c ==");
                const string loaiTrang = "6c page test";
                for (int i = 1; i <= 12; i++)
                    ScanHistoryStore.AddEntry(new HistoryEntry {
                        Time = DateTime.Now.AddMinutes(-i), Type = loaiTrang, Scope = "6c #" + i,
                        Files = 100 + i, Threats = 0, Seconds = i });
                mLamMoi.PerformClick();                 // "Làm mới dữ liệu" trong menu phụ (§3.5)
                PumpMs(300);
                cboLoai.SelectedItem = loaiTrang;       // loại có THẬT trong log -> combo tự bổ sung khi nạp
                dtpTu.Value = DateTime.Today.AddDays(-1);   // an toàn quanh nửa đêm: phiên test luôn trong khoảng
                dtpDen.Value = DateTime.Today;
                btnLoc.PerformClick();
                PumpMs(200);
                Func<int[]> tepTrang = delegate {
                    return grid.Rows.Cast<DataGridViewRow>()
                        .Select(r => Convert.ToInt32(r.Cells[ciFiles].Value)).ToArray();
                };
                Check("[UI] 6c: trang 1 đủ 10 phiên MỚI NHẤT (tệp 101..110), nhãn '1 / 2', nút Trước tắt",
                    grid.Rows.Count == 10 && lblTrang.Text == "1 / 2"
                    && tepTrang().SequenceEqual(Enumerable.Range(101, 10))
                    && !btnTruoc.Enabled && btnSau.Enabled);
                Check("[UI] 6c: 'Tổng cộng: 12 lần quét' = cả 12 phiên, không phải 10 dòng đang hiện",
                    lblTong.Text.StartsWith("Tổng cộng: 12"));
                Check("[UI] 6c: mọi dòng có khóa định danh (Tag = bản ghi gốc), không dựa chỉ số dòng",
                    grid.Rows.Cast<DataGridViewRow>().All(r => r.Tag is HistoryEntry
                        && ((HistoryEntry)r.Tag).Type == loaiTrang));
                btnSau.PerformClick();
                PumpMs(200);
                Check("[UI] 6c: sang trang 2 -> 2 phiên cũ nhất (111, 112), nhãn '2 / 2', nút Sau tắt",
                    grid.Rows.Count == 2 && lblTrang.Text == "2 / 2"
                    && tepTrang().SequenceEqual(new[] { 111, 112 })
                    && btnSau.Enabled == false && btnTruoc.Enabled);
                Check("[UI] 6c: đổi trang KHÔNG mất bộ lọc và không đụng dữ liệu",
                    (cboLoai.SelectedItem as string) == loaiTrang
                    && grid.Rows.Cast<DataGridViewRow>().All(r =>
                        Convert.ToString(r.Cells[ciType].Value) == loaiTrang));
                mLamMoi.PerformClick();                 // nạp lại: giữ bộ lọc, trang hiện tại vẫn hợp lệ (§3.4)
                PumpMs(300);
                Check("[UI] 6c: nhấn Làm mới giữ nguyên bộ lọc và vẫn ở trang 2",
                    lblTrang.Text == "2 / 2" && (cboLoai.SelectedItem as string) == loaiTrang
                    && grid.Rows.Count == 2);
                btnTruoc.PerformClick();
                PumpMs(200);
                Check("[UI] 6c: quay lại trang 1 -> đủ 10 dòng như cũ",
                    grid.Rows.Count == 10 && lblTrang.Text == "1 / 2"
                    && tepTrang().SequenceEqual(Enumerable.Range(101, 10)));
                // Sắp xếp nằm ở header "Thời gian": nhấp lần 1 -> cũ trước, lần 2 -> mới trước (§3.3)
                var hdrClick = typeof(UcLichSu).GetMethod("Grid_HeaderClick",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Action nhapHeader = delegate {
                    hdrClick.Invoke(hist, new object[] { null,
                        new DataGridViewCellMouseEventArgs(ciTime, -1, 20, 20,
                            new MouseEventArgs(MouseButtons.Left, 1, 20, 20, 0)) });
                    PumpMs(150);
                };
                nhapHeader();
                Check("[UI] 6c: nhấp header Thời gian -> cũ lên đầu (trang 1 = tệp 112..103)",
                    tepTrang().SequenceEqual(Enumerable.Range(103, 10).Reverse()) && lblTrang.Text == "1 / 2");
                nhapHeader();
                Check("[UI] 6c: nhấp header lần 2 -> mới nhất lên đầu lại (101..110)",
                    tepTrang().SequenceEqual(Enumerable.Range(101, 10)));
                Check("[UI] 6c: nhãn cột vẫn là 'Thời gian' + có tooltip đổi chiều sắp xếp",
                    F<DataGridViewColumn>(hist, "colScanTime").HeaderText == "Thời gian"
                    && F<DataGridViewColumn>(hist, "colScanTime").ToolTipText.Length > 0
                    && F<DataGridViewColumn>(hist, "colView").HeaderText == "Xem");

                // ----- 6d. Nút "Xem" TỪNG DÒNG + menu phụ "…" (định danh theo bản ghi, §3.5) -----
                Console.WriteLine("== SECTION 6d ==");
                var mContent = typeof(UcLichSu).GetMethod("Grid_CellContentClick",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var eDong6 = grid.Rows[6].Tag as HistoryEntry;
                mContent.Invoke(hist, new object[] { null, new DataGridViewCellEventArgs(ciView, 6) });
                PumpMs(600);                            // MessageBox "Chi tiết lịch sử" -> closer bấm OK
                Check("[UI] 6d: bấm 'Xem' dòng 7 -> mở đúng phiên của dòng đó, bảng nguyên vẹn",
                    eDong6 != null && Convert.ToInt32(grid.Rows[6].Cells[ciFiles].Value) == eDong6.Files
                    && Convert.ToString(grid.Rows[6].Cells[ciType].Value) == eDong6.Type
                    && grid.Rows.Count == 10 && lblTrang.Text == "1 / 2");
                btnSau.PerformClick();                  // sang trang 2: "Xem" vẫn phải đúng bản ghi
                PumpMs(200);
                var eTrang2 = grid.Rows[0].Tag as HistoryEntry;
                mContent.Invoke(hist, new object[] { null, new DataGridViewCellEventArgs(ciView, 0) });
                PumpMs(600);
                Check("[UI] 6d: 'Xem' ở trang 2 vẫn mở ĐÚNG bản ghi (không theo chỉ số dòng của trang 1)",
                    eTrang2 != null && eTrang2.Files == 111 && eTrang2.Scope == "6c #11"
                    && grid.Rows.Count == 2 && lblTrang.Text == "2 / 2");
                btnTruoc.PerformClick();
                PumpMs(200);
                grid.CurrentCell = grid.Rows[0].Cells[ciTime];  // "dòng đang chọn" cho menu phụ
                bool menuDaMo = false;
                menu.Opened += delegate { menuDaMo = true; };   // phòng khi Visible chưa kịp đổi trên máy chậm
                F<Button>(hist, "btnMore").PerformClick();   // mở menu phụ "…"
                PumpMs(250);
                Check("[UI] 6d: nút '…' mở menu phụ; mục theo dòng + mục 'Chỉ hiện phiên quét' đang tick",
                    (menu.Visible || menuDaMo) && mChiTiet.Enabled && mXoa.Enabled && mPhien.Checked
                    && !mCanhBao.Checked && !mCapNhat.Checked && mChiTiet.Text.StartsWith("Xem chi tiết"));
                mChiTiet.PerformClick();                // "Xem chi tiết dòng đang chọn" -> hộp thoại
                PumpMs(600);
                Check("[UI] 6d: menu 'Xem chi tiết' mở hộp chi tiết, bảng + bộ lọc giữ nguyên",
                    grid.Rows.Count == 10 && (cboLoai.SelectedItem as string) == loaiTrang);

                // ----- 6e. Menu "Xóa dòng đang chọn" (nghiệp vụ cũ, nay nằm trong menu phụ) -----
                Console.WriteLine("== SECTION 6e ==");
                btnSau.PerformClick();                  // sang trang 2 (2 dòng) để kiểm tra tự chỉnh trang
                PumpMs(200);
                grid.CurrentCell = grid.Rows[0].Cells[ciTime];   // dòng cũ nhất còn lại (tệp 111)
                int truocXoa = ScanHistoryStore.Entries().Count(x => x.Type == loaiTrang);
                mXoa.PerformClick();
                PumpMs(700);                            // YesNo -> closer bấm "Có"
                Check("[UI] 6e: xóa ĐÚNG bản ghi đang chọn (còn 11 phiên, mất đúng tệp 111)",
                    truocXoa == 12
                    && ScanHistoryStore.Entries().Count(x => x.Type == loaiTrang) == 11
                    && !ScanHistoryStore.Entries().Any(x => x.Type == loaiTrang && x.Files == 111));
                Check("[UI] 6e: xóa xong ở lại trang hợp lệ, giữ bộ lọc, lưới khớp dữ liệu còn lại",
                    lblTrang.Text == "2 / 2" && grid.Rows.Count == 1
                    && Convert.ToInt32(grid.Rows[0].Cells[ciFiles].Value) == 112
                    && (cboLoai.SelectedItem as string) == loaiTrang);
                while (ScanHistoryStore.Entries().Any(x => x.Type == loaiTrang))
                {
                    grid.CurrentCell = grid.Rows[Math.Min(1, grid.Rows.Count - 1)].Cells[ciTime];
                    mXoa.PerformClick();
                    PumpMs(500);
                }
                Check("[UI] 6e: xóa hết phiên test -> loại đó rời combo 'Loại quét' (tự về 'Tất cả'), bảng nạp lại phiên THẬT",
                    ScanHistoryStore.Entries().All(x => x.Type != loaiTrang)
                    && !cboLoai.Items.Contains(loaiTrang) && (cboLoai.SelectedItem as string) == "Tất cả"
                    && grid.Rows.Count > 0 && !lblTrong.Visible
                    && grid.Rows.Cast<DataGridViewRow>().All(r =>
                        Convert.ToString(r.Cells[ciType].Value) != loaiTrang)
                    && lblTrang.Text.StartsWith("1 /")   // tự về trang 1 của danh sách mới hợp lệ
                    && lblTong.Text.StartsWith("Tổng cộng: ") && lblTong.Text.EndsWith("lần quét"));
                // (Trạng thái rỗng *"Không có lịch sử quét phù hợp"* + nhãn "0 / 0" được khẳng định ở 6g.)

                // ----- 6f. Ba "mục" xem qua menu phụ (thay 3 tab con đã bỏ) — nhật ký cập nhật/cảnh báo vẫn còn -----
                Console.WriteLine("== SECTION 6f ==");
                mCapNhat.PerformClick();
                PumpMs(250);
                int soCapNhat = ScanHistoryStore.Entries().Count(e => e.Type == "Cập nhật CSDL");
                Check("[UI] 6f: mục 'Nhật ký cập nhật CSDL' -> bảng CHỈ còn loại 'Cập nhật CSDL', đơn vị đếm đổi",
                    soCapNhat > 0
                    && grid.Rows.Cast<DataGridViewRow>()
                        .All(r => Convert.ToString(r.Cells[ciType].Value) == "Cập nhật CSDL")
                    && grid.Rows.Count == Math.Min(10, soCapNhat)
                    && lblTong.Text.StartsWith("Tổng cộng: " + soCapNhat + " ")
                    && lblTong.Text.EndsWith("lần cập nhật CSDL")
                    && mCapNhat.Checked && !mPhien.Checked && !mCanhBao.Checked);
                mCanhBao.PerformClick();
                PumpMs(250);
                Check("[UI] 6f: mục 'Cảnh báo thời gian thực' -> chỉ dòng cảnh báo, không trộn phiên quét",
                    grid.Rows.Cast<DataGridViewRow>()
                        .All(r => Convert.ToString(r.Cells[ciType].Value) == "Bảo vệ thời gian thực")
                    && lblTong.Text.EndsWith("cảnh báo")
                    && mCanhBao.Checked && !mPhien.Checked && !mCapNhat.Checked);
                mPhien.PerformClick();
                PumpMs(250);
                Check("[UI] 6f: quay lại 'Chỉ hiện phiên quét' -> phiên quét thật hiện lại, nhật ký bị loại",
                    grid.Rows.Count > 0 && lblTong.Text.EndsWith("lần quét")
                    && grid.Rows.Cast<DataGridViewRow>().All(r =>
                    {
                        string t = Convert.ToString(r.Cells[ciType].Value);
                        return t != "Cập nhật CSDL" && t != "Bảo vệ thời gian thực";
                    })
                    && mPhien.Checked && !mCanhBao.Checked && !mCapNhat.Checked);

                // ----- 6g. Lọc theo ngày + chặn khoảng ngày sai (§3.2) -----
                Console.WriteLine("== SECTION 6g ==");
                DateTime ngayCuNhat = DateTime.Today;
                foreach (HistoryEntry e in ScanHistoryStore.Entries())
                    if (e.Time.Date < ngayCuNhat) ngayCuNhat = e.Time.Date;
                DateTime ngayTrong = ngayCuNhat.AddDays(-7);
                if (ngayTrong < dtpTu.MinDate) ngayTrong = dtpTu.MinDate;
                dtpTu.Value = ngayTrong;
                dtpDen.Value = ngayTrong;
                btnLoc.PerformClick();
                PumpMs(200);
                Check("[UI] 6g: khoảng ngày không có dữ liệu -> 'Không có lịch sử quét phù hợp', tổng 0, '0 / 0'",
                    grid.Rows.Count == 0 && lblTrong.Visible && lblTrang.Text == "0 / 0"
                    && !btnTruoc.Enabled && !btnSau.Enabled
                    && lblTong.Text.StartsWith("Tổng cộng: 0"));
                dtpDen.Value = ngayCuNhat;              // ngày có dữ liệu: phải bao gồm TRỌN ngày kết thúc
                btnLoc.PerformClick();
                PumpMs(200);
                int mongDoiTrongKhoang = ScanHistoryStore.Entries().Count(e =>
                    e.Time.Date >= ngayTrong && e.Time.Date <= ngayCuNhat
                    && e.Type != "Cập nhật CSDL" && e.Type != "Bảo vệ thời gian thực");
                Check("[UI] 6g: Đến ngày = ngày có dữ liệu (trọn ngày) -> phiên quét hiện lại đúng tổng số",
                    grid.Rows.Count > 0 && !lblTrong.Visible
                    && lblTong.Text.StartsWith("Tổng cộng: " + mongDoiTrongKhoang + " "));
                int truocSai = grid.Rows.Count;
                string tongTruocSai = lblTong.Text;
                dtpTu.Value = DateTime.Today;
                dtpDen.Value = DateTime.Today.AddDays(-1);
                btnLoc.PerformClick();
                PumpMs(700);                            // MessageBox cảnh báo -> closer bấm OK
                Check("[UI] 6g: Từ ngày > Đến ngày -> báo ngắn và GIỮ NGUYÊN bảng đang xem (không lọc sai)",
                    grid.Rows.Count == truocSai && lblTong.Text == tongTruocSai);
                dtpTu.Value = ngayTrong;
                dtpDen.Value = DateTime.Today;
                btnLoc.PerformClick();
                PumpMs(200);
                Check("[UI] 6g: khôi phục khoảng ngày hợp lệ -> bảng đầy đủ trở lại", grid.Rows.Count > 0);
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
                        // CHỈ dọn fallback Desktop — TestSamples\ trong repo là nguồn, không được xóa
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
                    // (25/09/2026) Ô đếm cách ly đã bỏ -> kiểm tra liên kết "Mở tab Lịch sử" của thẻ Hoạt động gần đây
                    var lnkLichSu = F<LinkLabel>(ov, "lnkXemLichSu");
                    Console.WriteLine("== 9: lnkXemLichSu.OnLinkClicked -> MoTabLichSu ==");
                    Check("[UI] thẻ Hoạt động gần đây có link 'Mở tab Lịch sử'",
                        lnkLichSu.Visible && lnkLichSu.Text.StartsWith("Mở tab Lịch sử"));
                    typeof(LinkLabel).GetMethod("OnLinkClicked", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(lnkLichSu, new object[] { new LinkLabelLinkClickedEventArgs(new LinkLabel.Link()) });
                    Application.DoEvents();
                    Check("[UI] bấm link Lịch sử ở Tổng quan -> nhảy sang tab Lịch sử",
                        panel.Controls.Count == 1 && panel.Controls[0].GetType().Name == "UcLichSu");
                    Console.WriteLine("== 9: close mf ==");
                    mf.Close();
                }
                Console.WriteLine("== SECTION 9 done ==");

                // ===== 9b. Ngôn ngữ thiết kế đồng bộ theo chuẩn tab Lịch sử =====
                // 5/5 tab phải có page-header: 18pt Bold (title) + 9.75 xám (subtitle) — RIÊNG tab Lịch sử
                // theo ảnh tham chiếu 26/09/2026 dùng cỡ LỚN 25pt + phụ đề 10.5 (Theme.PageTitleLargeFont).
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
                        // (26/09/2026) Tab Lịch sử nay là bản dựng theo ảnh tham chiếu: tiêu đề LỚN 25pt +
                        // phụ đề 10.5 (Theme.PageTitleLargeFont/PageSubLargeFont, §3.1); 4 tab còn lại 18pt.
                        bool laLichSu = tb[0] == "ucLichSu";
                        Font fTtl = laLichSu ? Theme.PageTitleLargeFont : Theme.PageTitleFont;
                        Font fSub = laLichSu ? Theme.PageSubLargeFont : Theme.PageSubFont;
                        bool ok = Math.Abs(ttl.Font.SizeInPoints - fTtl.SizeInPoints) < 0.15f
                            && ttl.Font.Bold && ttl.ForeColor == Theme.TextDark;
                        if (tb[2] != null)
                        {
                            var sub = F<Label>(tabUc, tb[2]);
                            ok = ok && Math.Abs(sub.Font.SizeInPoints - fSub.SizeInPoints) < 0.15f
                                && sub.ForeColor == Theme.TextGray;
                        }
                        if (ok) headerOk++;
                        else Console.WriteLine("  header lệch: " + tb[1] + " font=" + ttl.Font.Size);
                    }
                    Check("[UI] 5/5 tab có page-header đúng chuẩn (Lịch sử 25B theo ảnh, 4 tab kia 18B + subtitle xám)",
                        headerOk == 5);

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

                    // 9c. regression layout Tổng quan: 2 nút quét nằm trong khối (a), trang giãn theo cửa sổ
                    Console.WriteLine("== SECTION 9c ==");
                    using (var geo = new Form())
                    using (var ucX = new UcTongQuan())
                    {
                        ucX.Dock = DockStyle.Fill;
                        geo.Controls.Add(ucX);
                        geo.StartPosition = FormStartPosition.Manual;
                        geo.Location = new Point(-2600, -2600);
                        geo.ClientSize = new Size(1280, 980); // cao hơn min 640 -> không cần scroll, hàng Percent giãn
                        geo.Show(); Application.DoEvents();
                        var gs = F<GroupBox>(ucX, "grpActivity");   // thẻ "Hoạt động gần đây"
                        var ga = F<GroupBox>(ucX, "grpAction");     // bảng đe dọa — chỉ hiện ở trạng thái (b)
                        var t12 = F<Control>(ucX, "tableLayoutPanel12");
                        var pn = F<Panel>(ucX, "pnlContent");
                        var fh = F<Control>(ucX, "flowHeaderActions");  // 2 nút quét — nay nằm trong khối (a)
                        int fhBottom = fh.Bottom;   // quy đổi đáy nút về toạ độ trang Tổng quan
                        for (var pp = fh.Parent; pp != null && pp != ucX; pp = pp.Parent) fhBottom += pp.Top;
                        Console.WriteLine("  geo850 gs.H=" + gs.Height + " tlp12.H=" + t12.Height
                            + " ga.Visible=" + ga.Visible + " client.H=" + ucX.ClientSize.Height
                            + " tlp11.H=" + F<Control>(ucX, "tableLayoutPanel11").Height);
                        // RESPONSIVE: hàng "Hoạt động gần đây" là Percent -> cao lên theo cửa sổ (không chừa
                        // khoảng trắng ở đáy); 2 nút quét nằm trong khối (a) và trong tầm nhìn của cửa sổ
                        Check("[UI] TQ cửa sổ đủ cao: thẻ Hoạt động GIÃN theo cửa sổ (>=400px), bảng đe dọa ẩn",
                            gs.Height >= 400 && t12.Height >= 306 && !ga.Visible
                            && gs.Bottom <= ucX.ClientSize.Height + 2
                            && F<Control>(ucX, "pnlAnToan").Visible && fhBottom <= ucX.ClientSize.Height);

                        geo.ClientSize = new Size(1000, 560); // thấp hơn min -> PHẢI scroll, nội dung không bị ép
                        Application.DoEvents(); geo.Refresh();
                        gs = F<GroupBox>(ucX, "grpActivity");
                        var t11 = F<Control>(ucX, "tableLayoutPanel11");
                        Console.WriteLine("  geo560 gs.H=" + gs.Height + " tlp11.H=" + t11.Height
                            + " AutoScroll=" + pn.AutoScroll);
                        // min theo Theme.ScrollablePage: TLP11 giữ MinimumSize 980x640 -> t11.H >= 630,
                        // thẻ Hoạt động vẫn là hàng Percent nên đủ chỗ, AutoScroll bật thay vì ép các card
                        Check("[UI] TQ cửa sổ thấp: AutoScroll bật + card KHÔNG bị ép (giữ >=303px)",
                            pn.AutoScroll && gs.Height >= 303 && t11.Height >= 630
                            && gs.Bottom <= t11.Height + 2);

                        // Trạng thái (b) — "Phát hiện đe dọa": bảng đe dọa hiện 400px, thẻ Hoạt động nhường chỗ
                        F<DataGridView>(ucX, "dgvActions").Rows.Add(false, @"C:\fake\threat.bin",
                            "Chữ ký: ui-9c", "Cao", "Xem chi tiết");
                        InvokeM(ucX, "UpdateThreatUi");
                        Application.DoEvents();
                        geo.PerformLayout();
                        ucX.PerformLayout();
                        Application.DoEvents();
                        ga = F<GroupBox>(ucX, "grpAction");
                        gs = F<GroupBox>(ucX, "grpActivity");
                        var fa = F<Control>(ucX, "flowPhatHienActions");   // 2 nút của hero (b)
                        var parentB = F<Control>(ucX, "pnlPhatHienDeDoa");
                        Console.WriteLine("  geoB ga.H=" + ga.Height + " ga.Visible=" + ga.Visible
                            + " gs.Visible=" + gs.Visible + " heroDeDoa=" + parentB.Visible
                            + " fa.Right=" + fa.Right + " hero.W=" + parentB.ClientSize.Width
                            + " fh.Visible=" + fh.Visible);
                        // 2 nút quét nằm trong khối (a) nên ở (b) chúng bị ẩn; 2 nút của hero (b) phải nằm
                        // TRONG panel (lỗi cũ: neo x=1854 -> vô hình)
                        Check("[UI] TQ trạng thái (b): bảng đe dọa hiện, thẻ Hoạt động nhường chỗ, nút hero nằm TRONG panel",
                            ga.Visible && ga.Height >= 303 && !gs.Visible && parentB.Visible
                            && !fh.Visible && fa.Visible && fa.Right <= parentB.ClientSize.Width);
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
