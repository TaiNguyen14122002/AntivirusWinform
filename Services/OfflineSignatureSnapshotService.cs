using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// F03: snapshot chu ky hash + CDC chi-doc, ma hoa/xac thuc bang DPAPI cho Windows user.
    /// SQL van la nguon chinh; snapshot KHONG thay the DB cua lich su/cach ly.
    /// Khong ghi token, API key, password hay duong dan tep cua nguoi dung vao snapshot.
    /// </summary>
    internal static class OfflineSignatureSnapshotService
    {
        private const int Version = 1;
        private const int MaxProtectedBytes = 192 * 1024 * 1024;
        private const int MaxSignatureRecords = 5000000;
        private const int MaxGroups = 200000;
        private const int MaxChunksPerGroup = 200000;
        private static readonly byte[] Entropy = System.Text.Encoding.UTF8.GetBytes("ScanAndRemoveVirus:signature-snapshot:v1");
        private const string Magic = "AVSS1";

        internal static string SnapshotPath
        {
            get
            {
                string testRoot = Environment.GetEnvironmentVariable("XVIRUS_DATA_DIR");
                string baseDir = string.IsNullOrWhiteSpace(testRoot)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScanAndRemoveVirus")
                    : testRoot;
                return Path.Combine(baseDir, "offline-signatures.v1.dat");
            }
        }

        internal static bool TrySave(HybridSignatureScanner.Snapshot snapshot, out string failure)
        {
            failure = null;
            if (snapshot == null || !snapshot.SQLAvailable || !snapshot.DatabaseLoadComplete || !snapshot.HasSignatures)
            {
                failure = "Bo chu ky SQL chua du dieu kien luu snapshot.";
                return false;
            }
            string target = SnapshotPath;
            string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                byte[] compressed;
                using (var memory = new MemoryStream())
                {
                    using (var gzip = new GZipStream(memory, CompressionMode.Compress, true))
                    using (var writer = new BinaryWriter(gzip, System.Text.Encoding.UTF8, true))
                        WriteSnapshot(writer, snapshot);
                    if (memory.Length > MaxProtectedBytes - 1024)
                        throw new InvalidDataException("Snapshot qua lon; khong ghi de ban sao hop le cu.");
                    compressed = memory.ToArray();
                }
                // DPAPI xac thuc payload; sua 1 bit se khong giai ma duoc.
                byte[] protectedData = ProtectedData.Protect(compressed, Entropy, DataProtectionScope.CurrentUser);
                if (protectedData.Length > MaxProtectedBytes)
                    throw new InvalidDataException("Snapshot vuot gioi han tai nguyen.");
                using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(file))
                {
                    writer.Write(Magic);
                    writer.Write(protectedData.Length);
                    writer.Write(protectedData);
                    writer.Flush();
                    file.Flush(true);
                }
                if (File.Exists(target))
                {
                    // File.Replace thay atomically trong cung volume; giu last-good trong .bak.
                    File.Replace(temp, target, target + ".bak", true);
                }
                else File.Move(temp, target);
                return true;
            }
            catch (Exception ex)
            {
                failure = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        internal static HybridSignatureScanner.Snapshot TryLoad(out string failure)
        {
            failure = null;
            string path = SnapshotPath;
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    using (var fs = new FileStream(candidate, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var br = new BinaryReader(fs))
                    {
                        if (br.ReadString() != Magic)
                            throw new InvalidDataException("Sai magic snapshot.");
                        int length = br.ReadInt32();
                        if (length <= 0 || length > MaxProtectedBytes || length != fs.Length - fs.Position)
                            throw new InvalidDataException("Kich thuoc snapshot khong hop le.");
                        byte[] ciphertext = br.ReadBytes(length);
                        if (ciphertext.Length != length)
                            throw new EndOfStreamException("Snapshot chua du bytes.");
                        byte[] compressed = ProtectedData.Unprotect(ciphertext, Entropy, DataProtectionScope.CurrentUser);
                        using (var input = new MemoryStream(compressed, false))
                        using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                        using (var reader = new BinaryReader(gzip, System.Text.Encoding.UTF8))
                        {
                            HybridSignatureScanner.Snapshot result = ReadSnapshot(reader);
                            if (result == null || !result.HasSignatures)
                                throw new InvalidDataException("Snapshot khong co chu ky hop le.");
                            result.SQLAvailable = false;
                            result.SignatureSource = "OfflineSnapshot";
                            result.IsDegraded = true;
                            result.Error = "SQL Server khong san sang. Dang dung chu ky ngoai tuyen luu luc " +
                                result.SignatureCreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm") +
                                ". Bao ve han che; du lieu co the da cu. Lich su va cach ly van can SQL.";
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    failure = "Khong doc duoc " + Path.GetFileName(candidate) + ": " + ex.GetType().Name;
                }
            }
            if (failure == null) failure = "Chua co snapshot hop le cho tai khoan Windows nay.";
            return null;
        }

        private static void WriteSnapshot(BinaryWriter w, HybridSignatureScanner.Snapshot snapshot)
        {
            w.Write(Version);
            w.Write(DateTime.UtcNow.Ticks);
            var entries = snapshot.SHA256.Values.Concat(snapshot.SHA1.Values).Concat(snapshot.MD5.Values)
                .GroupBy(x => x.SignatureID).Select(x => x.First()).ToList();
            if (entries.Count > MaxSignatureRecords || snapshot.Groups.Count > MaxGroups)
                throw new InvalidDataException("So luong chu ky vuot gioi han snapshot.");
            w.Write(entries.Count);
            foreach (var h in entries)
            {
                w.Write(h.SignatureID);
                w.Write(h.Name ?? "");
                w.Write(h.MD5 ?? "");
                w.Write(h.SHA1 ?? "");
                w.Write(h.SHA256 ?? "");
            }
            w.Write(snapshot.Groups.Count);
            foreach (var group in snapshot.Groups)
            {
                if (group.Chunks.Count > MaxChunksPerGroup)
                    throw new InvalidDataException("CDC group vuot gioi han.");
                w.Write(group.GroupID); w.Write(group.SignatureID);
                w.Write(group.Name ?? ""); w.Write(group.MinMatchCount);
                w.Write(group.MaxGapBytes.HasValue);
                if (group.MaxGapBytes.HasValue) w.Write(group.MaxGapBytes.Value);
                w.Write(group.ContentDefined); w.Write(group.SimilarityThreshold);
                var cfg = group.Cdc ?? new HybridSignatureScanner.CdcConfig();
                w.Write(cfg.WindowBytes); w.Write(cfg.MinChunkSize); w.Write(cfg.AvgChunkSize);
                w.Write(cfg.MaxChunkSize); w.Write(cfg.BoundaryMaskBits);
                w.Write(group.Chunks.Count);
                foreach (var chunk in group.Chunks)
                {
                    w.Write(chunk.GroupID); w.Write(chunk.ChunkID);
                    w.Write(chunk.Size); w.Write(chunk.Order); w.Write(chunk.SHA256 ?? "");
                }
            }
        }

        private static HybridSignatureScanner.Snapshot ReadSnapshot(BinaryReader r)
        {
            if (r.ReadInt32() != Version) throw new InvalidDataException("Schema snapshot khac phien ban.");
            long ticks = r.ReadInt64();
            DateTime created = new DateTime(ticks, DateTimeKind.Utc);
            if (created > DateTime.UtcNow.AddMinutes(15) || created < new DateTime(2020, 1, 1))
                throw new InvalidDataException("Thoi gian snapshot khong hop le.");
            int count = r.ReadInt32();
            if (count < 0 || count > MaxSignatureRecords) throw new InvalidDataException("Signature count khong hop le.");
            var hashes = new List<HybridSignatureScanner.HashEntry>();
            for (int i = 0; i < count; i++)
            {
                var h = new HybridSignatureScanner.HashEntry
                {
                    SignatureID = r.ReadInt64(), Name = r.ReadString(),
                    MD5 = r.ReadString(), SHA1 = r.ReadString(), SHA256 = r.ReadString()
                };
                if (h.Name.Length > 4096) throw new InvalidDataException("Malware name qua dai.");
                hashes.Add(h);
            }
            int groupCount = r.ReadInt32();
            if (groupCount < 0 || groupCount > MaxGroups) throw new InvalidDataException("CDC group count khong hop le.");
            var groups = new List<HybridSignatureScanner.GroupEntry>();
            for (int i = 0; i < groupCount; i++)
            {
                var g = new HybridSignatureScanner.GroupEntry();
                g.GroupID = r.ReadInt64(); g.SignatureID = r.ReadInt64();
                g.Name = r.ReadString(); g.MinMatchCount = r.ReadInt32();
                bool hasGap = r.ReadBoolean();
                g.MaxGapBytes = hasGap ? (int?)r.ReadInt32() : null;
                g.ContentDefined = r.ReadBoolean();
                g.SimilarityThreshold = r.ReadDecimal();
                g.Cdc = new HybridSignatureScanner.CdcConfig
                {
                    WindowBytes = r.ReadInt32(), MinChunkSize = r.ReadInt32(), AvgChunkSize = r.ReadInt32(),
                    MaxChunkSize = r.ReadInt32(), BoundaryMaskBits = r.ReadInt32()
                };
                int chunkCount = r.ReadInt32();
                if (chunkCount < 0 || chunkCount > MaxChunksPerGroup)
                    throw new InvalidDataException("CDC chunk count khong hop le.");
                for (int j = 0; j < chunkCount; j++)
                    g.Chunks.Add(new HybridSignatureScanner.ChunkEntry
                    {
                        GroupID = r.ReadInt64(), ChunkID = r.ReadInt64(),
                        Size = r.ReadInt32(), Order = r.ReadInt32(), SHA256 = r.ReadString()
                    });
                groups.Add(g);
            }
            var snapshot = HybridSignatureScanner.CreateTestSnapshot(hashes, groups);
            snapshot.SignatureCreatedAtUtc = created;
            snapshot.DatabaseLoadComplete = false;
            return snapshot;
        }
    }
}
