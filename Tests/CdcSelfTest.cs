// Windows/.NET Framework 4.7.2: msbuild Tests\CdcSelfTest.csproj /p:Configuration=Release
// Does not require SQL Server. All samples below are pseudorandom and harmless.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using ScanAndRemoveVirus.Services;

internal static class CdcSelfTest
{
    private static void Check(bool ok, string name)
    {
        if (!ok) throw new InvalidOperationException("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
    private static byte[] RandomData(int length, int seed)
    {
        byte[] bytes = new byte[length];
        new Random(seed).NextBytes(bytes);
        return bytes;
    }
    private static string SHA(byte[] data)
    {
        using (var sha = SHA256.Create()) return HybridSignatureScanner.ToHex(sha.ComputeHash(data));
    }
    private static HybridSignatureScanner.GroupEntry MakeGroup(
        IList<HybridSignatureScanner.CdcChunkDigest> chunks, HybridSignatureScanner.CdcConfig config)
    {
        var group = new HybridSignatureScanner.GroupEntry {
            GroupID = 901, SignatureID = 901, Name = "SAFE-CDC-FIXTURE",
            ContentDefined = true, Cdc = config, MinMatchCount = 4,
            SimilarityThreshold = 0.70m, MaxGapBytes = null
        };
        for (int i = 0; i < chunks.Count; ++i)
            group.Chunks.Add(new HybridSignatureScanner.ChunkEntry {
                GroupID = group.GroupID, ChunkID = i + 1, Order = i + 1,
                Size = chunks[i].Size, SHA256 = chunks[i].SHA256
            });
        return group;
    }
    private static List<HybridSignatureScanner.CdcChunkDigest> Fingerprint(
        byte[] data, HybridSignatureScanner.CdcConfig config)
    {
        using (var ms = new MemoryStream(data, false))
            return HybridSignatureScanner.CreateContentDefinedFingerprint(ms, config, 10000,
                CancellationToken.None);
    }
    private static void Main()
    {
        var config = new HybridSignatureScanner.CdcConfig {
            WindowBytes = 48, MinChunkSize = 512, AvgChunkSize = 2048,
            MaxChunkSize = 8192, BoundaryMaskBits = 11
        };
        Check(config.IsValid, "CDC config valid");
        byte[] original = RandomData(2 * 1024 * 1024, 12345);
        var chunks = Fingerprint(original, config);
        Check(chunks.Count > 80, "Content-defined boundaries generated");
        Check(chunks.Sum(c => (long)c.Size) == original.Length, "All bytes covered exactly once");
        Check(chunks.All(c => c.Size >= 1 && c.Size <= config.MaxChunkSize), "Chunk size bounded");
        Check(chunks.Zip(chunks.Skip(1), (a, b) => a.Offset + a.Size == b.Offset).All(b => b),
            "Chunk offsets continuous");

        var group = MakeGroup(chunks.Skip(15).Take(70).ToList(), config);
        var test = HybridSignatureScanner.CreateTestSnapshot(null, new[] { group });
        string path = Path.Combine(Path.GetTempPath(), "cdc-safe-" + Guid.NewGuid().ToString("N") + ".bin");
        try
        {
            File.WriteAllBytes(path, original);
            var hit = HybridSignatureScanner.ScanFile(path, test, CancellationToken.None);
            Check(hit != null && hit.Technique.Contains("Rolling Hash (CDC)"), "CDC detects original fixture");
            Check(hit.SignatureID == 901, "CDC preserves SignatureID");

            // Insert inside one of the signed chunks. The rolling boundary finder
            // re-synchronizes after the insertion, preserving later chunk SHA-256s.
            int insertionOffset = checked((int)chunks[34].Offset + 100);
            byte[] inserted = new byte[original.Length + 1931];
            Buffer.BlockCopy(original, 0, inserted, 0, insertionOffset);
            Buffer.BlockCopy(RandomData(1931, 777), 0, inserted, insertionOffset, 1931);
            Buffer.BlockCopy(original, insertionOffset, inserted, insertionOffset + 1931,
                original.Length - insertionOffset);
            var shifted = Fingerprint(inserted, config);
            var referenceHashes = new HashSet<string>(group.Chunks.Select(c => c.SHA256));
            int shared = shifted.Count(c => referenceHashes.Contains(c.SHA256));
            Console.WriteLine("CDC shared signed chunks after insertion: " + shared + "/" + group.Chunks.Count);
            Check(shared >= group.RequiredMatches, "CDC stays similar after inserted bytes");
            var baseFingerprint = chunks.Skip(15).Take(70).ToList();
            double similarity = HybridSignatureScanner.CompareCdcFingerprints(baseFingerprint, shifted);
            Check(similarity > 0.70, "Ordered CDC fingerprint similarity survives insertion");
            Check(HybridSignatureScanner.FormatCdcFingerprint(config, baseFingerprint).StartsWith("CDC1|"),
                "Readable CDC fingerprint uses project-local version marker");
            File.WriteAllBytes(path, inserted);
            hit = HybridSignatureScanner.ScanFile(path, test, CancellationToken.None);
            Check(hit != null && hit.Technique.Contains("Rolling Hash (CDC)"),
                "CDC scanner matches mutated (whole-file hash changed)");
            Check(SHA(original) != SHA(inserted), "Whole-file SHA-256 changes after insertion");

            // One identical input chunk must NOT satisfy multiple signature
            // positions even if the group contains repeated SHA-256 digests.
            var repeated = new HybridSignatureScanner.GroupEntry {
                GroupID = 902, SignatureID = 902, Name = "SAFE-REPEATED-CHUNKS",
                ContentDefined = true, Cdc = config, MinMatchCount = 2,
                SimilarityThreshold = 0.8m
            };
            byte[] zeros = new byte[512];
            for (int j = 1; j <= 3; j++) repeated.Chunks.Add(new HybridSignatureScanner.ChunkEntry {
                GroupID = 902, ChunkID = j, Order = j, Size = zeros.Length,
                SHA256 = SHA(zeros)
            });
            var duplicateSnapshot = HybridSignatureScanner.CreateTestSnapshot(null, new[] { repeated });
            Check(HybridSignatureScanner.ScanSlidingBytes(zeros, duplicateSnapshot, CancellationToken.None) == null,
                "One chunk does not count as three repeated signature chunks");

            // Fixed-window groups are no longer an active third scanning method.
            var obsoleteGroup = new HybridSignatureScanner.GroupEntry {
                GroupID = 903, SignatureID = 903, Name = "OLD-FIXED-NOT-SCANNED",
                ContentDefined = false, MinMatchCount = 1
            };
            obsoleteGroup.Chunks.Add(new HybridSignatureScanner.ChunkEntry {
                GroupID = 903, ChunkID = 1, Order = 1, Size = 512, SHA256 = SHA(zeros)
            });
            var obsoleteSnapshot = HybridSignatureScanner.CreateTestSnapshot(null, new[] { obsoleteGroup });
            Check(!obsoleteSnapshot.HasSignatures &&
                HybridSignatureScanner.ScanSlidingBytes(zeros, obsoleteSnapshot, CancellationToken.None) == null,
                "Old fixed-window group cannot create a detection");

            using (var tinyStream = new MemoryStream(original, 0, 32 * 1024, false))
            using (var sqlText = new StringWriter())
            {
                long generated = HybridSignatureScanner.WriteContentDefinedSignatureSql(tinyStream,
                    sqlText, config, 901, "SAFE CDC", 3, 0.6m, CancellationToken.None);
                Check(generated > 3 && sqlText.ToString().Contains("'CDC'") &&
                    sqlText.ToString().Contains("0);"), "SQL exporter emits disabled CDC group");
            }

            File.WriteAllBytes(path, RandomData(original.Length, 98765));
            Check(HybridSignatureScanner.ScanFile(path, test, CancellationToken.None) == null,
                "Unrelated data does not match");

            // Exact SHA-256 remains the only whole-file hash matching method.
            var exact = HybridSignatureScanner.CreateTestSnapshot(new[] {
                new HybridSignatureScanner.HashEntry { Name = "SAFE-FULL-HASH",
                    SignatureID = 4321, SHA256 = SHA(original) }
            }, new[] { group });
            File.WriteAllBytes(path, original);
            hit = HybridSignatureScanner.ScanFile(path, exact, CancellationToken.None);
            Check(hit != null && hit.Technique == "Hash toàn tệp", "Exact SHA-256 precedence");

            // A final CDC chunk may be smaller than MinChunkSize. It must NOT
            // be excluded from the signature index (former false-negative bug).
            var tailConfig = new HybridSignatureScanner.CdcConfig {
                WindowBytes = 48, MinChunkSize = 512, AvgChunkSize = 512,
                MaxChunkSize = 1024, BoundaryMaskBits = 20
            };
            // Zero-filled fixture has a stable non-triggering rolling window
            // under this profile; the first cut is forced at MaxChunkSize.
            byte[] tailBytes = new byte[1024 + 200];
            var tailChunks = Fingerprint(tailBytes, tailConfig);
            Check(tailChunks.Count == 2 && tailChunks[1].Size == 200,
                "CDC final chunk can be smaller than minimum size");
            var tailGroup = new HybridSignatureScanner.GroupEntry {
                GroupID = 904, SignatureID = 904, Name = "SAFE-SHORT-TAIL",
                ContentDefined = true, Cdc = tailConfig, MinMatchCount = 2,
                SimilarityThreshold = 1.0m
            };
            for (int j = 0; j < tailChunks.Count; j++)
                tailGroup.Chunks.Add(new HybridSignatureScanner.ChunkEntry {
                    GroupID = 904, ChunkID = j + 1, Order = j + 1,
                    Size = tailChunks[j].Size, SHA256 = tailChunks[j].SHA256
                });
            var tailSnapshot = HybridSignatureScanner.CreateTestSnapshot(null, new[] { tailGroup });
            Check(HybridSignatureScanner.ScanSlidingBytes(tailBytes, tailSnapshot, CancellationToken.None) != null,
                "CDC detects final short chunk against its signature");

            // Crosses 256 KiB read boundaries, and file exceeds previous 4 MiB limit.
            byte[] large = RandomData(12 * 1024 * 1024, 44221);
            Buffer.BlockCopy(original, 0, large, 5 * 1024 * 1024 + 117, original.Length);
            File.WriteAllBytes(path, large);
            // The surrounding bytes may alter boundary chunks but middle chunks should survive.
            var largeHit = HybridSignatureScanner.ScanFile(path, test, CancellationToken.None);
            Check(largeHit != null, "12 MiB CDC streaming scan finds embedded fixture");

            var canceled = new CancellationTokenSource(); canceled.Cancel();
            bool threw = false;
            try { HybridSignatureScanner.ScanFile(path, test, canceled.Token); }
            catch (OperationCanceledException) { threw = true; }
            Check(threw, "Cancellation token is respected");
        }
        finally { try { File.Delete(path); } catch (IOException) { } }
        Console.WriteLine("All CDC self-tests passed.");
    }
}
