// Regression test: quét thư mục đệ quy, mỗi tệp là một phát hiện độc lập.
// Dữ liệu hoàn toàn vô hại; hash thử nghiệm nằm trong Snapshot ở bộ nhớ,
// không cần kết nối SQL Server hoặc thêm chữ ký vào VirusSignatures.
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using ScanAndRemoveVirus.Services;

internal static class ScanAllFilesSelfTest
{
    private static int failed;
    private static void Check(string name, bool pass)
    {
        Console.WriteLine((pass ? "PASS: " : "FAIL: ") + name);
        if (!pass) failed++;
    }
    private static string Hash(string path)
    {
        using (var sha = SHA256.Create())
        using (var file = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
    }
    private static void AddHash(HybridSignatureScanner.Snapshot snap, string path, long id)
    {
        string sha = Hash(path);
        snap.SHA256.Add(sha, new HybridSignatureScanner.HashEntry
        { Name = "TEST.Harmless." + id, SHA256 = sha, SignatureID = id });
    }
    private static void Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "scan-all-files-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string deep1 = Directory.CreateDirectory(Path.Combine(root, "cap1", "cap2")).FullName;
            string deep2 = Directory.CreateDirectory(Path.Combine(root, "cap3")).FullName;
            string rootThreat = Path.Combine(root, "sample-a.txt");
            string nestedThreat = Path.Combine(deep1, "sample-b.bin");
            string nestedThreat2 = Path.Combine(deep2, "sample-c.js");
            string clean = Path.Combine(deep1, "clean.txt");
            string clean2 = Path.Combine(root, "clean.txt");
            File.WriteAllText(rootThreat, "harmless fixture a " + Guid.NewGuid());
            File.WriteAllText(nestedThreat, "harmless fixture b " + Guid.NewGuid());
            File.WriteAllText(nestedThreat2, "harmless fixture c " + Guid.NewGuid());
            File.WriteAllText(clean, "ordinary note one");
            File.WriteAllText(clean2, "ordinary note two");
            var snapshot = new HybridSignatureScanner.Snapshot { SQLAvailable = true };
            AddHash(snapshot, rootThreat, 1);
            AddHash(snapshot, nestedThreat, 2);
            AddHash(snapshot, nestedThreat2, 3);

            ScanResult all = ScanEngine.Scan(ScanType.Custom, root, CancellationToken.None,
                null, new ScanTelemetry(), snapshot);
            Check("5 tep duoc quet ke ca trong thu muc con", all.FilesScanned == 5);
            Check("3 tep dung hash duoc phat hien rieng", all.Threats.Count == 3);
            Check("3 duong dan file day du", all.Threats.All(t => File.Exists(t.FilePath) &&
                Path.HasExtension(t.FilePath)) && all.Threats.Select(t => t.FilePath).Distinct().Count() == 3);
            Check("co tep o thu muc cap 2", all.Threats.Any(t => t.FilePath == nestedThreat));
            Check("co tep o cac nhanh khac", all.Threats.Any(t => t.FilePath == nestedThreat2));
            Check("khong bi danh dau ca thu muc", all.Threats.All(t => !Directory.Exists(t.FilePath)));
            Check("duyet du 4 thu muc", all.DirectoriesScanned == 4);
            Check("khong co loi doc tep", all.ReadErrorCount == 0);

            // Người dùng chọn phạm vi cha, con, và một tệp đã thuộc phạm vi cha.
            // Không được quét/phát hiện tệp đó 2-3 lần.
            ScanResult overlapping = ScanEngine.Scan(ScanType.Custom, null, CancellationToken.None,
                null, new ScanTelemetry(), snapshot,
                new[] { root, deep1, nestedThreat, deep2 });
            Check("pham vi chong nhau van chi quet 5 tep", overlapping.FilesScanned == 5);
            Check("khong trung lap 3 phat hien", overlapping.Threats.Count == 3);

            ScanResult individual = ScanEngine.Scan(ScanType.Custom, nestedThreat,
                CancellationToken.None, null, new ScanTelemetry(), snapshot);
            Check("Quet tep rieng chi cho 1 tep", individual.FilesScanned == 1 &&
                individual.Threats.Count == 1 && individual.Threats[0].FilePath == nestedThreat);
        }
        finally
        {
            try { Directory.Delete(root, true); }
            catch (Exception) { }
        }
        Console.WriteLine(failed == 0 ? "ALL TESTS PASSED" : "TESTS FAILED: " + failed);
        if (failed != 0) Environment.ExitCode = 1;
    }
}
