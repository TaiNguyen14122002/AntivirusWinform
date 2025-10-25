using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using ScanAndRemoveVirus.Services;

internal static class ArchiveScanSelfTest
{
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }

    private static byte[] MakeZip(Action<ZipArchive> build)
    {
        using (var memory = new MemoryStream())
        {
            using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, true)) build(zip);
            return memory.ToArray();
        }
    }

    private static void Put(ZipArchive zip, string entryName, string data)
    {
        using (var stream = zip.CreateEntry(entryName).Open())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            stream.Write(bytes, 0, bytes.Length);
        }
    }

    // TAR do test tự tạo (không sử dụng tệp độc hại, không giải nén ra tên entry).
    private static byte[] MakeTar(string name, string text)
    {
        byte[] contents = Encoding.UTF8.GetBytes(text);
        byte[] h = new byte[512];
        byte[] filename = Encoding.UTF8.GetBytes(name);
        Array.Copy(filename, 0, h, 0, Math.Min(100, filename.Length));
        WriteOctal(h, 100, 8, 420); // 0644
        WriteOctal(h, 108, 8, 0);
        WriteOctal(h, 116, 8, 0);
        WriteOctal(h, 124, 12, contents.Length);
        WriteOctal(h, 136, 12, 0);
        h[156] = (byte)'0';
        Array.Copy(Encoding.ASCII.GetBytes("ustar"), 0, h, 257, 5);
        for (int i = 148; i < 156; i++) h[i] = (byte)' ';
        long sum = 0;
        foreach (byte b in h) sum += b;
        WriteOctal(h, 148, 8, sum);
        using (var m = new MemoryStream())
        {
            m.Write(h, 0, h.Length);
            m.Write(contents, 0, contents.Length);
            int pad = (512 - contents.Length % 512) % 512;
            m.Write(new byte[pad + 1024], 0, pad + 1024);
            return m.ToArray();
        }
    }
    private static void WriteOctal(byte[] b, int start, int size, long value)
    {
        string digits = Convert.ToString(value, 8).PadLeft(size - 1, '0');
        byte[] text = Encoding.ASCII.GetBytes(digits);
        Array.Copy(text, 0, b, start, Math.Min(size - 1, text.Length));
        b[start + size - 1] = 0;
    }

    public static int Main()
    {
        string temp = Path.Combine(Path.GetTempPath(), "AV-ArchiveTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            byte[] nested = MakeZip(zip => Put(zip, "scripts/sample.js", "console.log('harmless test');"));
            byte[] outer = MakeZip(zip =>
            {
                Put(zip, "docs/ok.txt", "harmless content");
                Put(zip, "../outside.txt", "must never escape");
                using (var stream = zip.CreateEntry("inside.zip").Open()) stream.Write(nested, 0, nested.Length);
            });
            string archive = Path.Combine(temp, "test.zip");
            File.WriteAllBytes(archive, outer);
            var paths = new List<string>();
            var failures = new List<string>();
            ArchiveScanService.Outcome result = ArchiveScanService.Scan(archive,
                (staged, virtualPath) =>
                {
                    Check(File.Exists(staged), "staged stream readable");
                    paths.Add(virtualPath);
                    return true;
                },
                (virtualPath, reason) => failures.Add(virtualPath + ": " + reason),
                CancellationToken.None);
            Check(result.Examined == 2, "two ordinary entries (including nested ZIP)");
            Check(result.Unchecked == 1, "path traversal rejected");
            Check(paths.Exists(v => v.EndsWith(" > docs/ok.txt")), "entry path 1");
            Check(paths.Exists(v => v.EndsWith(" > inside.zip > scripts/sample.js")), "nested virtual path");
            Check(!File.Exists(Path.Combine(temp, "outside.txt")), "no Zip Slip write");
            Check(failures.Count == 1, "unreadable entry has cause");
            Check(ScanDisplayPath.Leaf(archive + " > sub/fake|name.js") == "fake|name.js",
                "virtual entry filename does not use Windows Path");
            Check(ScanDisplayPath.Extension(archive + " > sub/file?.js") == ".js",
                "virtual entry extension does not use Windows Path");
            Check(ScanDisplayPath.Parent(archive + " > scripts/child.js") == archive + " > scripts",
                "virtual entry parent keeps the folder within archive");

            byte[] unusual = MakeZip(zip =>
            {
                Put(zip, "scripts/name|pipe.js", "benign script");
                Put(zip, "scripts/question?.ps1", "benign script");
                Put(zip, "nested/sub<name>.txt", "benign text");
            });
            string unusualArchive = Path.Combine(temp, "unusual.zip");
            File.WriteAllBytes(unusualArchive, unusual);
            var unusualPaths = new List<string>();
            ArchiveScanService.Outcome unusualResult = ArchiveScanService.Scan(unusualArchive,
                (staged, virtualPath) => { unusualPaths.Add(virtualPath); return File.Exists(staged); },
                (virtualPath, reason) => failures.Add(reason), CancellationToken.None);
            Check(unusualResult.Examined == 3 && unusualResult.Unchecked == 0,
                "ZIP entry names with Windows special characters are scanned by GUID staging");
            Check(unusualPaths.Exists(v => v.EndsWith(" > scripts/name|pipe.js")),
                "virtual entry path retained for display");
            string tar = Path.Combine(temp, "test.tar");
            File.WriteAllBytes(tar, MakeTar("docs/a.txt", "harmless TAR test"));
            var tarPaths = new List<string>();
            ArchiveScanService.Outcome tarResult = ArchiveScanService.Scan(tar,
                (staged, virtualPath) => { tarPaths.Add(virtualPath); return File.Exists(staged); },
                (virtualPath, reason) => failures.Add(reason), CancellationToken.None);
            Check(tarResult.Examined == 1 && tarResult.Unchecked == 0, "TAR ordinary entry inspected");
            Check(tarPaths.Exists(v => v.EndsWith(" > docs/a.txt")), "TAR virtual path retained");
            string traversal = Path.Combine(temp, "traversal.tar");
            File.WriteAllBytes(traversal, MakeTar("../outside.txt", "must not be written"));
            var tarTraversal = ArchiveScanService.Scan(traversal,
                (staged, v) => true, (v, reason) => failures.Add(reason), CancellationToken.None);
            Check(tarTraversal.Unchecked == 1 && !File.Exists(Path.Combine(temp, "outside.txt")),
                "TAR path traversal rejected");
            string tgz = Path.Combine(temp, "test.tar.gz");
            using (var output = File.Create(tgz))
            using (var gzip = new GZipStream(output, CompressionMode.Compress))
            {
                byte[] payload = MakeTar("sample.txt", "harmless GZip TAR test");
                gzip.Write(payload, 0, payload.Length);
            }
            var gzipResult = ArchiveScanService.Scan(tgz, (staged, virtualPath) => File.Exists(staged),
                (v, reason) => failures.Add(reason), CancellationToken.None);
            Check(gzipResult.Examined == 1 && gzipResult.Unchecked == 0, "GZip TAR nested entry inspected");
            Console.WriteLine("All ZIP/TAR/GZ scan self-tests passed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { try { Directory.Delete(temp, true); } catch (Exception) { } }
    }
}
