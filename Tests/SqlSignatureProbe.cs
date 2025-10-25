// Diagnostic for the actual SQL instance and runtime scanner. Read-only.
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using ScanAndRemoveVirus.Database;
using ScanAndRemoveVirus.Services;

internal static class SqlSignatureProbe
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("==== ANTIVIRUS HASH / CDC DIAGNOSTIC ====");
        Console.WriteLine(DbConnection.GetConnectionStatus());
        HybridSignatureScanner.Snapshot snapshot = HybridSignatureScanner.GetSnapshot(true);
        Console.WriteLine("SQLAvailable: " + snapshot.SQLAvailable);
        Console.WriteLine("Chu ky hash toan tep hop le dang bat: " + snapshot.WholeFileSignatureCount);
        Console.WriteLine("  SHA256: " + snapshot.SHA256.Count + "; SHA1: " + snapshot.SHA1.Count + "; MD5: " + snapshot.MD5.Count);
        Console.WriteLine("Nhom CDC hop le dang bat: " + snapshot.CdcGroupCount);
        if (!string.IsNullOrWhiteSpace(snapshot.Error))
            Console.WriteLine("WARNING: " + snapshot.Error);
        if (!snapshot.HasSignatures)
            Console.WriteLine("ERROR: Chua co chu ky de doi chieu; ket qua 0 phat hien KHONG phai ket luan an toan.");

        string expected = "ea9958fec77e564d36bd910ca32d15004e41fdb21bfb7c1dca414da943ee3eeb";
        Console.WriteLine("SQL co chu ky demo an toan dang bat: " + snapshot.SHA256.ContainsKey(expected));
        string path = null;
        bool temp = false;
        try
        {
            if (args.Length > 0) path = Path.GetFullPath(args[0]);
            else
            {
                path = Path.Combine(Path.GetTempPath(), "hash-diagnostic-" + Guid.NewGuid().ToString("N") + ".txt");
                File.WriteAllBytes(path, Encoding.ASCII.GetBytes("SIM-MALWARE-SIGNATURE::A57C1F37::PAYLOAD"));
                temp = true;
            }
            if (!File.Exists(path)) { Console.Error.WriteLine("FILE NOT FOUND: " + path); return 3; }
            Console.WriteLine("File: " + path);
            using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(input);
                string hex = HybridSignatureScanner.ToHex(hash);
                Console.WriteLine("SHA256 file thuc te: " + hex);
                Console.WriteLine("SHA256 ton tai trong snapshot: " + snapshot.SHA256.ContainsKey(hex));
            }
            if (snapshot.HasSignatures)
            {
                var hit = HybridSignatureScanner.ScanFile(path, snapshot, CancellationToken.None);
                Console.WriteLine(hit == null ? "NO MATCH: Khong co chu ky trung khop." :
                    "DETECTED: " + hit.Technique + " / " + hit.MalwareName + " / SignatureID " + hit.SignatureID + " / " + hit.Evidence);
                return hit == null ? 2 : 0;
            }
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.GetType().Name + ": " + ex.Message);
            return 1;
        }
        finally { if (temp && path != null) try { File.Delete(path); } catch (IOException) { } }
    }
}
