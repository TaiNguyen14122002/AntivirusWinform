// Harness kiểm thử ScanEngine — biên dịch độc lập bằng csc, KHÔNG nằm trong csproj chính.
// Chạy: csc /out:test.exe Services\ScanEngine.cs Tests\ScanEngineTest.cs && test.exe
using ScanAndRemoveVirus.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    static int Main()
    {
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

            // ---- Danh sách gốc quét nhanh / toàn bộ ----
            Check("roots quét nhanh khác rỗng", ScanEngine.GetScanRoots(ScanType.Quick, null).Any());
            Check("roots quét toàn bộ gồm C:\\", ScanEngine.GetScanRoots(ScanType.Full, null).Contains("C:\\"));
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
