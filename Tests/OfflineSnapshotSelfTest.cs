using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using ScanAndRemoveVirus.Services;

internal static class OfflineSnapshotSelfTest
{
    static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception("FAIL: " + message);
        Console.WriteLine("PASS: " + message);
    }

    static void Main()
    {
        string prior = Environment.GetEnvironmentVariable("XVIRUS_DATA_DIR");
        string root = Path.Combine(Path.GetTempPath(), "antivirus-offline-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("XVIRUS_DATA_DIR", root);
        try
        {
            string file = Path.Combine(root, "test-benign.txt");
            File.WriteAllText(file, "Safe offline snapshot regression fixture");
            string sha;
            using (SHA256 hash = SHA256.Create())
            using (FileStream f = File.OpenRead(file))
                sha = HybridSignatureScanner.ToHex(hash.ComputeHash(f));

            var snapshot = HybridSignatureScanner.CreateTestSnapshot(new[] {
                new HybridSignatureScanner.HashEntry {
                    SignatureID = 900001, Name = "OFFLINE-TEST-ONLY", SHA256 = sha
                }
            }, null);
            snapshot.DatabaseLoadComplete = true;
            string failure;
            Check(OfflineSignatureSnapshotService.TrySave(snapshot, out failure), "save DPAPI-protected snapshot: " + failure);
            var loaded = OfflineSignatureSnapshotService.TryLoad(out failure);
            Check(loaded != null && loaded.SignatureSource == "OfflineSnapshot" && loaded.IsDegraded &&
                  !loaded.SQLAvailable, "offline mode reports degraded correctly");
            var match = HybridSignatureScanner.ScanFile(file, loaded, CancellationToken.None);
            Check(match != null && match.SignatureID == 900001, "offline signature still detects matching SHA-256");

            string filePath = OfflineSignatureSnapshotService.SnapshotPath;
            byte[] changed = File.ReadAllBytes(filePath);
            changed[changed.Length - 1] ^= 0x10;
            File.WriteAllBytes(filePath, changed);
            Check(OfflineSignatureSnapshotService.TryLoad(out failure) == null,
                "tampered DPAPI payload is rejected");
            Console.WriteLine("All offline snapshot checks passed.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("XVIRUS_DATA_DIR", prior);
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
