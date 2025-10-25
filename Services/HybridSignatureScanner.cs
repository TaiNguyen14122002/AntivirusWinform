using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using ScanAndRemoveVirus.Database;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Hai phương pháp băm duy nhất: (1) MD5/SHA-1/SHA-256 toàn tệp; (2)
    /// Sliding Window + Rolling Hash tìm trigger để chia chunk theo nội dung (CDC),
    /// đối chiếu SHA-256 các chunk theo thứ tự và ngưỡng của nhóm SQL Server.
    /// Quét streaming, không giới hạn 4 MiB, không dùng nhánh fixed-window riêng.
    /// </summary>
    public static class HybridSignatureScanner
    {
        private const int ReadBufferSize = 256 * 1024;
        // Giữ một bộ đệm trên mỗi worker; tránh tạo mảng 256 KiB cho mọi file nhỏ.
        [ThreadStatic] private static byte[] workerReadBuffer;
        private static byte[] GetReadBuffer()
        {
            if (workerReadBuffer == null) workerReadBuffer = new byte[ReadBufferSize];
            return workerReadBuffer;
        }
        private const ulong RollingBase = 257UL;
        private static readonly object Sync = new object();
        private static readonly TimeSpan LiveRefreshInterval = TimeSpan.FromMinutes(2);
        private static Snapshot cached;
        private static DateTime loadedAt;

        public sealed class HashEntry
        {
            public string Name, SHA256, SHA1, MD5;
            public long SignatureID;
        }
        public sealed class ChunkEntry
        {
            public long GroupID, ChunkID;
            public int Size, Order;
            public string SHA256;
        }
        public sealed class GroupEntry
        {
            public long GroupID, SignatureID;
            public string Name;
            public int MinMatchCount;
            public int? MaxGapBytes;
            // CDC groups are matched only against dynamically determined chunk boundaries.
            public bool ContentDefined;
            public CdcConfig Cdc;
            public decimal SimilarityThreshold;
            public int RequiredMatches
            {
                get
                {
                    int ratioCount = (int)Math.Ceiling(Chunks.Count * (double)SimilarityThreshold);
                    return Math.Max(2, Math.Max(MinMatchCount, ratioCount));
                }
            }
            public readonly List<ChunkEntry> Chunks = new List<ChunkEntry>();
        }
        /// <summary>
        /// Content-defined chunking profile. The rolling window chooses boundaries;
        /// chunk SHA-256 is the verification digest, not the rolling hash itself.
        /// </summary>
        public sealed class CdcConfig
        {
            public int WindowBytes = 48;
            public int MinChunkSize = 512;
            public int AvgChunkSize = 2048;
            public int MaxChunkSize = 8192;
            public int BoundaryMaskBits = 11;
            public string Key { get { return WindowBytes + ":" + MinChunkSize + ":" + AvgChunkSize + ":" + MaxChunkSize + ":" + BoundaryMaskBits; } }
            public bool IsValid
            {
                get
                {
                    return WindowBytes >= 8 && WindowBytes <= 128 &&
                        MinChunkSize >= WindowBytes && MinChunkSize >= 64 &&
                        MaxChunkSize >= MinChunkSize && MaxChunkSize <= 262144 &&
                        AvgChunkSize >= MinChunkSize && AvgChunkSize <= MaxChunkSize &&
                        BoundaryMaskBits >= 6 && BoundaryMaskBits <= 20;
                }
            }
        }
        public sealed class CdcChunkDigest
        {
            public int Order, Size;
            public long Offset;
            public string SHA256;
        }
        internal sealed class CdcIndex
        {
            internal CdcConfig Config;
            // Bỏ SHA-256 trên những kích thước chunk không có trong CSDL.
            internal readonly HashSet<int> Sizes = new HashSet<int>();
            internal readonly Dictionary<string, List<ChunkEntry>> Hashes =
                new Dictionary<string, List<ChunkEntry>>(StringComparer.OrdinalIgnoreCase);
        }
        public sealed class Snapshot
        {
            public readonly Dictionary<string, HashEntry> SHA256 = new Dictionary<string, HashEntry>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, HashEntry> SHA1 = new Dictionary<string, HashEntry>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, HashEntry> MD5 = new Dictionary<string, HashEntry>(StringComparer.OrdinalIgnoreCase);
            public readonly List<GroupEntry> Groups = new List<GroupEntry>();
            internal readonly Dictionary<string, CdcIndex> byCdcProfile = new Dictionary<string, CdcIndex>();
            public bool SQLAvailable;
            public string Error;
            public bool DatabaseLoadComplete;
            public bool IsDegraded;
            public string SignatureSource = "Unavailable";
            public DateTime SignatureCreatedAtUtc;
            // Number of distinct active whole-file signatures and usable CDC groups.
            public int WholeFileSignatureCount { get { return SHA256.Values.Concat(SHA1.Values).Concat(MD5.Values).Select(h => h.SignatureID).Distinct().Count(); } }
            public int CdcGroupCount { get { return Groups.Count(g => g.ContentDefined && g.Cdc != null && byCdcProfile.ContainsKey(g.Cdc.Key)); } }
            public bool HasSignatures { get { return SHA256.Count > 0 || SHA1.Count > 0 || MD5.Count > 0 || byCdcProfile.Count > 0; } }
        }
        public sealed class Match
        {
            public string Technique, MalwareName, Evidence;
            public long SignatureID;
        }

        public static void Invalidate()
        {
            lock (Sync) { cached = null; loadedAt = DateTime.MinValue; }
        }
        public static Snapshot GetSnapshot(bool refresh)
        {
            lock (Sync)
            {
                if (!refresh && cached != null && DateTime.UtcNow - loadedAt < LiveRefreshInterval) return cached;
                var fresh = LoadFromDatabase();
                if (fresh.SQLAvailable && fresh.DatabaseLoadComplete && fresh.HasSignatures)
                {
                    fresh.SignatureSource = "SQL";
                    fresh.SignatureCreatedAtUtc = DateTime.UtcNow;
                    string saveError;
                    // Khong chan luong quet neu khong luu duoc snapshot (ACL/disk/DPAPI).
                    OfflineSignatureSnapshotService.TrySave(fresh, out saveError);
                }
                else if (!fresh.SQLAvailable)
                {
                    string loadError;
                    var offline = OfflineSignatureSnapshotService.TryLoad(out loadError);
                    if (offline != null) fresh = offline;
                    else
                    {
                        fresh.SignatureSource = "Unavailable";
                        fresh.IsDegraded = true;
                        fresh.Error += " Khong co snapshot ngoai tuyen hop le (" + loadError + ").";
                    }
                }
                cached = fresh;
                loadedAt = DateTime.UtcNow;
                return cached;
            }
        }
        private static Snapshot LoadFromDatabase()
        {
            var result = new Snapshot();
            bool wholeHashesLoaded = false;
            try
            {
                using (SqlConnection conn = DbConnection.Create())
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"
SELECT SignatureID, MalwareName, MD5, SHA1, SHA256 FROM dbo.VirusSignatures WHERE IsActive = 1", conn))
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            var entry = new HashEntry
                            {
                                SignatureID = reader.GetInt64(0), Name = reader.GetString(1),
                                MD5 = reader.IsDBNull(2) ? null : reader.GetString(2).Trim(),
                                SHA1 = reader.IsDBNull(3) ? null : reader.GetString(3).Trim(),
                                SHA256 = reader.IsDBNull(4) ? null : reader.GetString(4).Trim()
                            };
                            if (IsHex(entry.SHA256, 64)) result.SHA256[entry.SHA256] = entry;
                            if (IsHex(entry.SHA1, 40)) result.SHA1[entry.SHA1] = entry;
                            if (IsHex(entry.MD5, 32)) result.MD5[entry.MD5] = entry;
                        }

                    // Đã hoàn thành phương pháp 1. Lỗi đọc bảng CDC phía sau
                    // không được vô hiệu hóa các chữ ký hash toàn tệp đã nạp.
                    wholeHashesLoaded = true;
                    bool cdcColumnsExist;
                    using (var col = new SqlCommand("SELECT COL_LENGTH(N'dbo.ChunkSignatureGroups', N'ChunkingMode')", conn))
                    {
                        object value = col.ExecuteScalar();
                        cdcColumnsExist = value != null && value != DBNull.Value;
                    }
                    if (!cdcColumnsExist)
                    {
                        result.Error = "CSDL da ket noi nhung chua co cot ChunkingMode cho CDC. " +
                            "Chay AntivirusDB.ChunkHash.Migration.sql. " +
                            "Hash toan tep dang co " + result.WholeFileSignatureCount + " chu ky hop le.";
                        if (result.WholeFileSignatureCount == 0)
                            result.Error += " Khong co chu ky hash toan tep nao dang bat (IsActive=1).";
                        result.SQLAvailable = true;
                        result.IsDegraded = true;
                        result.SignatureSource = "SQLPartial";
                        return result;
                    }
                    string sql = @"
SELECT g.GroupID, v.MalwareName, g.MinMatchCount, g.MaxGapBytes,
       c.ChunkID, c.ChunkHash, c.ChunkSize, c.SequenceOrder, g.SignatureID,
       g.ChunkingMode, g.RollingWindowBytes, g.MinChunkSize, g.AvgChunkSize,
       g.MaxChunkSize, g.BoundaryMaskBits, g.SimilarityThreshold
FROM dbo.ChunkSignatureGroups AS g
JOIN dbo.VirusSignatures AS v ON v.SignatureID = g.SignatureID
JOIN dbo.ChunkSignatures AS c ON c.GroupID = g.GroupID
WHERE g.IsActive = 1 AND v.IsActive = 1 AND c.IsActive = 1
  AND g.ChunkingMode = 'CDC'
ORDER BY g.GroupID, c.SequenceOrder";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var groups = new Dictionary<long, GroupEntry>();
                        while (reader.Read())
                        {
                            long id = reader.GetInt64(0);
                            GroupEntry group;
                            if (!groups.TryGetValue(id, out group))
                            {
                                var cdc = new CdcConfig();
                                if (!reader.IsDBNull(10)) cdc.WindowBytes = reader.GetInt32(10);
                                if (!reader.IsDBNull(11)) cdc.MinChunkSize = reader.GetInt32(11);
                                if (!reader.IsDBNull(12)) cdc.AvgChunkSize = reader.GetInt32(12);
                                if (!reader.IsDBNull(13)) cdc.MaxChunkSize = reader.GetInt32(13);
                                if (!reader.IsDBNull(14)) cdc.BoundaryMaskBits = reader.GetInt32(14);
                                group = new GroupEntry
                                {
                                    GroupID = id, SignatureID = reader.GetInt64(8),
                                    Name = reader.GetString(1), MinMatchCount = reader.GetInt32(2),
                                    MaxGapBytes = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                                    ContentDefined = true, Cdc = cdc,
                                    SimilarityThreshold = reader.IsDBNull(15) ? 0.6m : reader.GetDecimal(15)
                                };
                                groups.Add(id, group);
                            }
                            int size = reader.GetInt32(6);
                            string hash = reader.GetString(5).Trim();
                            if (size < 1 || size > 262144 || !IsHex(hash, 64)) continue;
                            group.Chunks.Add(new ChunkEntry
                            {
                                GroupID = id, ChunkID = reader.GetInt64(4), Size = size,
                                Order = reader.GetInt32(7), SHA256 = hash.ToLowerInvariant()
                            });
                        }
                        foreach (var group in groups.Values)
                            if (group.MinMatchCount >= 2 && group.Cdc != null && group.Cdc.IsValid &&
                                group.SimilarityThreshold >= 0.2m && group.SimilarityThreshold <= 1.0m &&
                                group.Chunks.Count >= group.RequiredMatches &&
                                group.Chunks.Select(c => c.Order).Distinct().Count() == group.Chunks.Count)
                                result.Groups.Add(group);
                    }
                }
                BuildCdcIndex(result);
                result.SQLAvailable = true;
                result.DatabaseLoadComplete = true;
                result.SignatureSource = "SQL";
                // Without active signatures, a scan cannot classify files as clean.
                // Expose this as a warning to the result dialog, never as a detection.
                if (!result.HasSignatures)
                    result.Error = "SQL Server ket noi thanh cong nhung KHONG co chu ky bam dang hoat dong/hop le. " +
                        "Kiem tra dbo.VirusSignatures.IsActive, hash MD5/SHA1/SHA256, " +
                        "va cac nhom CDC (ChunkingMode='CDC', IsActive=1, du chunk). " +
                        "Dau hieu ten tep/chuoi thu nghiem cu khong con duoc nhan dien tu dong.";
                else if (result.CdcGroupCount == 0)
                    result.Error = "Da nap " + result.WholeFileSignatureCount + " chu ky hash toan tep, " +
                        "nhung KHONG co nhom CDC nao hop le dang duoc kich hoat. " +
                        "Du lieu FixedWindow cu khong duoc su dung. " +
                        "Hay tao va kich hoat nhom CDC tu mau da xac minh.";
            }
            catch (Exception ex)
            {
                result.SQLAvailable = wholeHashesLoaded;
                result.IsDegraded = true;
                result.SignatureSource = wholeHashesLoaded ? "SQLPartial" : "Unavailable";
                result.Error = wholeHashesLoaded
                    ? "Da nap " + result.WholeFileSignatureCount + " chu ky hash toan tep. " +
                      "CDC chua san sang (" + ex.GetType().Name + "): " + ex.Message +
                      ". Hash toan tep VAN DUOC SU DUNG; neu khong khop moi can CDC."
                    : "Khong nap duoc chu ky hash toan tep tu SQL Server (" + ex.GetType().Name + "): " + ex.Message +
                      ". Kiem tra SQLEXPRESS va AntivirusDB.";
                BuildCdcIndex(result);
            }
            return result;
        }

        private static bool IsHex(string value, int length)
        {
            if (value == null || value.Length != length) return false;
            foreach (char c in value)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return false;
            return true;
        }
        /// <summary>Fingerprint theo đa thức trên modulo 2^64, có thể tính từ chunk gốc.</summary>
        public static ulong ComputeRollingFingerprint(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");
            ulong hash = 0;
            unchecked { foreach (byte b in bytes) hash = hash * RollingBase + (ulong)b + 1UL; }
            return hash;
        }
        public static string FingerprintSqlHex(byte[] bytes)
        {
            return "0x" + ComputeRollingFingerprint(bytes).ToString("X16");
        }
        public static Snapshot CreateTestSnapshot(IEnumerable<HashEntry> hashes, IEnumerable<GroupEntry> groups)
        {
            var snapshot = new Snapshot { SQLAvailable = true };
            if (hashes != null)
                foreach (var h in hashes)
                {
                    if (IsHex(h.SHA256, 64)) snapshot.SHA256[h.SHA256] = h;
                    if (IsHex(h.SHA1, 40)) snapshot.SHA1[h.SHA1] = h;
                    if (IsHex(h.MD5, 32)) snapshot.MD5[h.MD5] = h;
                }
            if (groups != null) snapshot.Groups.AddRange(groups.Where(group => group.ContentDefined));
            BuildCdcIndex(snapshot);
            return snapshot;
        }
        private static void BuildCdcIndex(Snapshot snapshot)
        {
            snapshot.byCdcProfile.Clear();
            foreach (GroupEntry group in snapshot.Groups)
            {
                if (!group.ContentDefined || group.Cdc == null || !group.Cdc.IsValid ||
                    group.RequiredMatches > group.Chunks.Count) continue;
                // Do not mark a CDC profile as available when too few of its
                // chunks are indexable. A final short chunk is legal.
                var validChunks = group.Chunks.Where(chunk =>
                    chunk.Size >= 1 && chunk.Size <= group.Cdc.MaxChunkSize &&
                    IsHex(chunk.SHA256, 64)).ToArray();
                if (validChunks.Length < group.RequiredMatches) continue;
                CdcIndex index;
                if (!snapshot.byCdcProfile.TryGetValue(group.Cdc.Key, out index))
                {
                    if (snapshot.byCdcProfile.Count >= 4) continue;
                    snapshot.byCdcProfile.Add(group.Cdc.Key, index = new CdcIndex { Config = group.Cdc });
                }
                foreach (ChunkEntry chunk in validChunks)
                {
                    index.Sizes.Add(chunk.Size);
                    List<ChunkEntry> candidates;
                    if (!index.Hashes.TryGetValue(chunk.SHA256, out candidates))
                        index.Hashes.Add(chunk.SHA256, candidates = new List<ChunkEntry>());
                    candidates.Add(chunk);
                }
            }
        }

        /// <summary>
        /// Duy nhất hai bước, có dừng sớm:
        /// 1. So khớp băm toàn tệp. Trúng SHA-256/SHA-1/MD5 => trả về kết quả
        ///    ngay, tuyệt đối KHÔNG thực hiện quét CDC cho tệp này.
        /// 2. Chỉ khi KHÔNG có hash toàn tệp trùng mới quay lại đầu luồng và
        ///    chạy Chunk Hash + Sliding Window + Rolling Hash (CDC).
        /// </summary>
        public static Match ScanFile(string path, Snapshot snapshot, CancellationToken cancellation)
        {
            return ScanFile(path, snapshot, cancellation, null);
        }

        // Đếm byte đọc thực tế cho thông lượng/ETA; không thay đổi logic phát hiện.
        public static Match ScanFile(string path, Snapshot snapshot, CancellationToken cancellation,
            Action<long> onBytesRead)
        {
            if (snapshot == null || !snapshot.HasSignatures || string.IsNullOrWhiteSpace(path)) return null;
            cancellation.ThrowIfCancellationRequested();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, ReadBufferSize, FileOptions.SequentialScan))
            {
                // Ưu tiên tuyệt đối chữ ký đã xác minh của toàn bộ tệp.
                if (snapshot.SHA256.Count != 0 || snapshot.SHA1.Count != 0 || snapshot.MD5.Count != 0)
                {
                    Match fullFileMatch = ScanFullHash(fs, snapshot, cancellation, onBytesRead);
                    if (fullFileMatch != null)
                        return fullFileMatch; // ĐÃ PHÁT HIỆN: không chạy CDC nữa.
                }

                // Không khớp hash truyền thống -> chỉ lúc này mới đến CDC.
                if (snapshot.byCdcProfile.Count == 0 || fs.Length == 0)
                    return null;
                fs.Position = 0;
                return ScanSlidingStream(fs, snapshot, cancellation, onBytesRead);
            }
        }

        /// <summary>
        /// Tính đồng thời các thuật toán thật sự có chữ ký trong một lượt đọc.
        /// SHA-256 ưu tiên cao hơn SHA-1 rồi MD5; thấy một hash khớp là đủ.
        /// </summary>
        private static Match ScanFullHash(Stream input, Snapshot snapshot, CancellationToken ct, Action<long> onBytesRead)
        {
            using (var sha = snapshot.SHA256.Count > 0 ? SHA256.Create() : null)
            using (var sha1 = snapshot.SHA1.Count > 0 ? SHA1.Create() : null)
            using (var md5 = snapshot.MD5.Count > 0 ? MD5.Create() : null)
            {
                byte[] buffer = GetReadBuffer();
                int n;
                while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    if (onBytesRead != null) onBytesRead(n);
                    if (sha != null) sha.TransformBlock(buffer, 0, n, buffer, 0);
                    if (sha1 != null) sha1.TransformBlock(buffer, 0, n, buffer, 0);
                    if (md5 != null) md5.TransformBlock(buffer, 0, n, buffer, 0);
                }
                if (sha != null) sha.TransformFinalBlock(new byte[0], 0, 0);
                if (sha1 != null) sha1.TransformFinalBlock(new byte[0], 0, 0);
                if (md5 != null) md5.TransformFinalBlock(new byte[0], 0, 0);
                // Điều kiện OR độc lập: chỉ MỘT trong ba mã băm khớp là đủ.
                // Thứ tự dưới đây xác định cách gọi tên chữ ký, KHÔNG yêu cầu
                // ba hash phải cùng khớp hoặc cùng thuộc một bản ghi.
                HashEntry hit;
                if (sha != null && snapshot.SHA256.TryGetValue(ToHex(sha.Hash), out hit))
                    return FullHashMatch(hit, "SHA-256");
                if (sha1 != null && snapshot.SHA1.TryGetValue(ToHex(sha1.Hash), out hit))
                    return FullHashMatch(hit, "SHA-1");
                if (md5 != null && snapshot.MD5.TryGetValue(ToHex(md5.Hash), out hit))
                    return FullHashMatch(hit, "MD5");
                return null; // Không trùng bất kỳ hash nào -> thử CDC ở bước kế tiếp.
            }
        }
        private static Match FullHashMatch(HashEntry entry, string algorithm)
        {
            return new Match { Technique = "Hash toàn tệp", MalwareName = entry.Name, SignatureID = entry.SignatureID,
                Evidence = algorithm + " toàn tệp khớp " + entry.Name };
        }
        public static string ToHex(byte[] bytes) { return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant(); }

        private sealed class MatchNode
        {
            internal long End, Offset;
            internal int Count;
            internal MatchNode Previous;
        }
        private sealed class MatchBucket
        {
            internal readonly List<MatchNode> Candidates = new List<MatchNode>();
            internal int Head;
            // Cùng order, cùng chiều dài => End tăng cùng Offset, có thể binary search.
            internal MatchNode LatestEndingAtMost(long offset, int? gap)
            {
                int lo = Head, hi = Candidates.Count - 1, best = -1;
                while (lo <= hi)
                {
                    int mid = lo + (hi - lo) / 2;
                    if (Candidates[mid].End <= offset) { best = mid; lo = mid + 1; }
                    else hi = mid - 1;
                }
                if (best < Head) return null;
                MatchNode node = Candidates[best];
                if (gap.HasValue && offset - node.End > gap.Value) return null;
                return node;
            }
            internal void Add(MatchNode item, long now)
            {
                Candidates.Add(item);
                // Chỉ giữ: state hoàn thành gần nhất + các state vẫn đang chờ
                // hết overlap. Như vậy bộ nhớ không tăng theo độ dài của file.
                int lastOld = Head - 1;
                int lo = Head, hi = Candidates.Count - 1;
                while (lo <= hi)
                {
                    int mid = lo + (hi - lo) / 2;
                    if (Candidates[mid].End <= now) { lastOld = mid; lo = mid + 1; }
                    else hi = mid - 1;
                }
                if (lastOld >= Head) Head = lastOld;
                if (Head > 1024 && Head > Candidates.Count / 2)
                {
                    Candidates.RemoveRange(0, Head);
                    Head = 0;
                }
            }
        }
        private sealed class GroupMatcher
        {
            internal readonly GroupEntry Group;
            private readonly Dictionary<int, Dictionary<int, MatchBucket>> states = new Dictionary<int, Dictionary<int, MatchBucket>>();
            internal GroupMatcher(GroupEntry group) { Group = group; }
            internal Match Offer(ChunkEntry chunk, long start)
            {
                MatchNode bestPrevious = null;
                int nextCount = 1;
                foreach (var orderState in states)
                {
                    if (orderState.Key >= chunk.Order) continue;
                    foreach (var countState in orderState.Value)
                    {
                        if (countState.Key >= Group.RequiredMatches) continue;
                        MatchNode previous = countState.Value.LatestEndingAtMost(start, Group.MaxGapBytes);
                        if (previous != null && previous.Count + 1 > nextCount)
                        {
                            nextCount = previous.Count + 1;
                            bestPrevious = previous;
                        }
                    }
                }
                var node = new MatchNode { Offset = start, End = start + chunk.Size,
                    Count = nextCount, Previous = bestPrevious };
                if (node.Count >= Group.RequiredMatches)
                {
                    var positions = new List<long>();
                    for (MatchNode n = node; n != null; n = n.Previous) positions.Add(n.Offset);
                    positions.Reverse();
                    return new Match
                    {
                        Technique = "Chunk Hash + Sliding Window + Rolling Hash (CDC)", MalwareName = Group.Name,
                        SignatureID = Group.SignatureID,
                        Evidence = string.Format("{0}: khớp {1}/{2} đoạn SHA-256 đúng thứ tự (offset: {3})",
                            Group.Name, positions.Count, Group.Chunks.Count, string.Join(", ", positions))
                    };
                }
                Dictionary<int, MatchBucket> forOrder;
                if (!states.TryGetValue(chunk.Order, out forOrder))
                    states.Add(chunk.Order, forOrder = new Dictionary<int, MatchBucket>());
                MatchBucket bucket;
                if (!forOrder.TryGetValue(nextCount, out bucket))
                    forOrder.Add(nextCount, bucket = new MatchBucket());
                bucket.Add(node, start);
                return null;
            }
        }

        // Rolling Rabin-Karp-style polynomial boundary detector. It never retains the file,
        // only the last W bytes and at most MaxChunkSize bytes for SHA-256 verification.
        private sealed class CdcState
        {
            internal readonly CdcIndex Index;
            private readonly CdcConfig config;
            private readonly byte[] window;
            private readonly byte[] chunk;
            private readonly ulong power;
            private readonly ulong mask;
            private ulong rolling;
            private int filled, head, count;
            private long start;

            internal CdcState(CdcConfig config, CdcIndex index)
            {
                this.config = config;
                Index = index;
                window = new byte[config.WindowBytes];
                chunk = new byte[config.MaxChunkSize];
                mask = (1UL << config.BoundaryMaskBits) - 1UL;
                ulong p = 1;
                unchecked { for (int i = 1; i < config.WindowBytes; i++) p *= RollingBase; }
                power = p;
            }
            internal bool Push(byte b, long offset, SHA256 sha, out long chunkStart, out string hex)
            {
                unchecked
                {
                    if (filled == window.Length)
                    {
                        byte old = window[head];
                        rolling = (rolling - ((ulong)old + 1UL) * power) * RollingBase + (ulong)b + 1UL;
                    }
                    else
                    {
                        rolling = rolling * RollingBase + (ulong)b + 1UL;
                        filled++;
                    }
                }
                window[head++] = b;
                if (head == window.Length) head = 0;
                chunk[count++] = b;
                if (count >= config.MaxChunkSize ||
                    (count >= config.MinChunkSize && filled == window.Length && (rolling & mask) == 0))
                    return Finish(offset + 1, sha, out chunkStart, out hex);
                chunkStart = 0;
                hex = null;
                return false;
            }
            internal bool Flush(SHA256 sha, out long chunkStart, out string hex)
            {
                if (count == 0) { chunkStart = 0; hex = null; return false; }
                return Finish(start + count, sha, out chunkStart, out hex);
            }
            private bool Finish(long end, SHA256 sha, out long chunkStart, out string hex)
            {
                chunkStart = start;
                hex = Index != null && !Index.Sizes.Contains(count)
                    ? null : ToHex(sha.ComputeHash(chunk, 0, count));
                start = end;
                count = 0;
                // Keep the rolling window across chunk boundaries so a byte insertion
                // only perturbs boundaries near the insertion, not the entire tail.
                return true;
            }
        }

        // Fenwick tree stores the longest ordered matching subsequence of CDC chunk
        // signature indices; each offer costs O(log N) even for large signature groups.
        private sealed class CdcOrderMatcher
        {
            private readonly GroupEntry group;
            private readonly Dictionary<int, int> positions = new Dictionary<int, int>();
            private readonly int[] prefixMax;
            internal CdcOrderMatcher(GroupEntry group)
            {
                this.group = group;
                int[] orders = group.Chunks.Select(c => c.Order).Distinct().OrderBy(i => i).ToArray();
                for (int i = 0; i < orders.Length; i++) positions[orders[i]] = i + 1;
                prefixMax = new int[orders.Length + 1];
            }
            // All signature entries sharing one SHA-256 may describe the SAME
            // input chunk. Query them as a batch, then update; one target chunk
            // must never count twice towards the matching threshold.
            internal Match OfferBatch(IEnumerable<ChunkEntry> entries, long offset)
            {
                var updates = new List<KeyValuePair<int, int>>();
                int maximum = 0;
                foreach (ChunkEntry entry in entries)
                {
                    int pos;
                    if (!positions.TryGetValue(entry.Order, out pos)) continue;
                    int best = 0;
                    for (int i = pos - 1; i > 0; i -= i & -i)
                        if (prefixMax[i] > best) best = prefixMax[i];
                    int length = best + 1;
                    updates.Add(new KeyValuePair<int, int>(pos, length));
                    if (length > maximum) maximum = length;
                }
                foreach (var update in updates)
                    for (int i = update.Key; i < prefixMax.Length; i += i & -i)
                        if (update.Value > prefixMax[i]) prefixMax[i] = update.Value;
                if (maximum < group.RequiredMatches) return null;
                return new Match
                {
                    Technique = "Chunk Hash + Sliding Window + Rolling Hash (CDC)",
                    MalwareName = group.Name, SignatureID = group.SignatureID,
                    Evidence = string.Format("{0}: {1}/{2} chunk SHA-256 khớp đúng thứ tự (>= {3:P0}); offset cuối: {4}",
                        group.Name, maximum, group.Chunks.Count, group.SimilarityThreshold, offset)
                };
            }
        }
        private static Match OfferCdcChunk(CdcIndex index, string digest, long offset,
            Dictionary<long, GroupMatcher> matchers, Dictionary<long, CdcOrderMatcher> cdcMatchers)
        {
            List<ChunkEntry> chunks;
            if (digest == null || !index.Hashes.TryGetValue(digest, out chunks)) return null;
            foreach (var group in chunks.GroupBy(chunk => chunk.GroupID))
            {
                GroupMatcher gapMatcher;
                if (matchers.TryGetValue(group.Key, out gapMatcher))
                {
                    foreach (ChunkEntry entry in group)
                    {
                        Match hit = gapMatcher.Offer(entry, offset);
                        if (hit == null) continue;
                        hit.Technique = "Chunk Hash + Sliding Window + Rolling Hash (CDC)";
                        return hit;
                    }
                }
                CdcOrderMatcher matcher;
                if (cdcMatchers.TryGetValue(group.Key, out matcher))
                {
                    Match hit = matcher.OfferBatch(group, offset);
                    if (hit != null) return hit;
                }
            }
            return null;
        }
        /// <summary>
        /// Explicitly export a CDC fingerprint: ordered SHA-256 chunk digests.
        /// Not compatible with ssdeep or TLSH. Production scanning uses streaming
        /// without retaining this list in RAM. maxChunks prevents unbounded exports.
        /// </summary>
        public static List<CdcChunkDigest> CreateContentDefinedFingerprint(
            Stream input, CdcConfig config, int maxChunks, CancellationToken cancellation)
        {
            if (input == null) throw new ArgumentNullException("input");
            if (config == null || !config.IsValid) throw new ArgumentException("Invalid CDC config", "config");
            if (maxChunks < 1) throw new ArgumentOutOfRangeException("maxChunks");
            var output = new List<CdcChunkDigest>();
            var state = new CdcState(config, null);
            var buffer = new byte[ReadBufferSize];
            long offset = -1;
            using (var sha = SHA256.Create())
            {
                int n;
                while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                    for (int i = 0; i < n; i++)
                    {
                        if ((i & 0x3fff) == 0) cancellation.ThrowIfCancellationRequested();
                        long start;
                        string digest;
                        offset++;
                        if (!state.Push(buffer[i], offset, sha, out start, out digest)) continue;
                        if (output.Count >= maxChunks) throw new InvalidOperationException("Fingerprint exceeds maxChunks; use streaming scan instead.");
                        output.Add(new CdcChunkDigest { Order = output.Count + 1, Offset = start,
                            Size = checked((int)(offset - start + 1)), SHA256 = digest });
                    }
                long lastStart;
                string lastDigest;
                if (state.Flush(sha, out lastStart, out lastDigest))
                {
                    if (output.Count >= maxChunks) throw new InvalidOperationException("Fingerprint exceeds maxChunks; use streaming scan instead.");
                    output.Add(new CdcChunkDigest { Order = output.Count + 1, Offset = lastStart,
                        Size = checked((int)(offset - lastStart + 1)), SHA256 = lastDigest });
                }
            }
            return output;
        }

        /// <summary>
        /// An explicit, human-readable CDC fingerprint. This format is PRIVATE TO THIS
        /// PROJECT, not ssdeep or TLSH. Never generate it during routine large scans;
        /// scanning uses a streaming matcher without retaining the fingerprint list.
        /// </summary>
        public static string FormatCdcFingerprint(CdcConfig config, IList<CdcChunkDigest> chunks)
        {
            if (config == null || !config.IsValid) throw new ArgumentException("Invalid CDC config", "config");
            if (chunks == null) throw new ArgumentNullException("chunks");
            return "CDC1|" + config.Key + "|" + string.Join(",",
                chunks.Select(c => c.Size + ":" + c.SHA256).ToArray());
        }

        /// <summary>
        /// Ordered chunk similarity with a project-local score (not an ssdeep/TLSH score).
        /// Every target chunk contributes at most one match even when SHA digests repeat.
        /// </summary>
        public static double CompareCdcFingerprints(IList<CdcChunkDigest> reference,
            IList<CdcChunkDigest> target)
        {
            if (reference == null) throw new ArgumentNullException("reference");
            if (target == null) throw new ArgumentNullException("target");
            if (reference.Count == 0) return 0d;
            var map = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reference.Count; ++i)
            {
                string key = reference[i].Size + ":" + reference[i].SHA256;
                List<int> positions;
                if (!map.TryGetValue(key, out positions))
                    map.Add(key, positions = new List<int>());
                positions.Add(i + 1);
            }
            var tree = new int[reference.Count + 1];
            int maximum = 0;
            foreach (CdcChunkDigest candidate in target)
            {
                List<int> positions;
                if (!map.TryGetValue(candidate.Size + ":" + candidate.SHA256, out positions)) continue;
                var updates = new List<KeyValuePair<int, int>>();
                // Read tree before updating: don't match multiple reference chunks
                // to the same candidate when reference hashes are duplicated.
                foreach (int pos in positions)
                {
                    int score = 1;
                    for (int j = pos - 1; j > 0; j -= j & -j)
                        if (tree[j] + 1 > score) score = tree[j] + 1;
                    updates.Add(new KeyValuePair<int, int>(pos, score));
                    if (score > maximum) maximum = score;
                }
                foreach (var pair in updates)
                    for (int j = pair.Key; j < tree.Length; j += j & -j)
                        if (pair.Value > tree[j]) tree[j] = pair.Value;
            }
            return (double)maximum / reference.Count;
        }

        /// <summary>
        /// Build a DISABLED CDC signature group from a trusted known sample.
        /// Output is streamed to a SQL script (no in-memory list of file chunks).
        /// The reviewer must verify the sample and explicitly enable the group.
        /// </summary>
        public static long WriteContentDefinedSignatureSql(Stream input, TextWriter writer,
            CdcConfig config, long signatureID, string groupName, int minMatchCount,
            decimal similarityThreshold, CancellationToken cancellation)
        {
            if (input == null) throw new ArgumentNullException("input");
            if (writer == null) throw new ArgumentNullException("writer");
            if (config == null || !config.IsValid) throw new ArgumentException("Invalid CDC config", "config");
            if (signatureID <= 0) throw new ArgumentOutOfRangeException("signatureID");
            if (string.IsNullOrWhiteSpace(groupName) || groupName.Length > 200)
                throw new ArgumentException("Group name must contain 1-200 characters", "groupName");
            if (minMatchCount < 2) throw new ArgumentOutOfRangeException("minMatchCount");
            if (similarityThreshold < 0.2m || similarityThreshold > 1.0m)
                throw new ArgumentOutOfRangeException("similarityThreshold");
            string name = groupName.Replace("'", "''");
            writer.WriteLine("-- Generated from a vetted file by ContentDefinedChunking v1; never execute unknown samples.");
            writer.WriteLine("-- This group is INACTIVE by default. Review it before enabling.");
            writer.WriteLine("USE AntivirusDB;");
            writer.WriteLine("GO");
            writer.WriteLine("SET XACT_ABORT ON;");
            writer.WriteLine("BEGIN TRANSACTION;");
            writer.WriteLine("IF NOT EXISTS (SELECT 1 FROM dbo.VirusSignatures WHERE SignatureID={0} AND IsActive=1)", signatureID);
            writer.WriteLine("    THROW 51033, 'SignatureID must reference an active verified VirusSignatures row.', 1;");
            writer.WriteLine("DECLARE @CdcGroupID BIGINT;");
            writer.WriteLine("INSERT dbo.ChunkSignatureGroups (SignatureID, GroupName, Description, MinMatchCount, MaxGapBytes,");
            writer.WriteLine("    ChunkingMode, RollingWindowBytes, MinChunkSize, AvgChunkSize, MaxChunkSize,");
            writer.WriteLine("    BoundaryMaskBits, SimilarityThreshold, IsActive)");
            writer.WriteLine("VALUES ({0}, N'{1}', N'Vetted CDC signature; review before enabling', {2}, NULL,",
                signatureID, name, minMatchCount);
            writer.WriteLine("    'CDC', {0}, {1}, {2}, {3}, {4}, {5}, 0);",
                config.WindowBytes, config.MinChunkSize, config.AvgChunkSize, config.MaxChunkSize,
                config.BoundaryMaskBits, similarityThreshold.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteLine("SET @CdcGroupID = CAST(SCOPE_IDENTITY() AS BIGINT);");
            var state = new CdcState(config, null);
            byte[] buffer = new byte[ReadBufferSize];
            long position = -1, order = 0;
            using (var sha = SHA256.Create())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancellation.ThrowIfCancellationRequested();
                    for (int i = 0; i < read; i++)
                    {
                        if ((i & 0x3fff) == 0) cancellation.ThrowIfCancellationRequested();
                        long start;
                        string hex;
                        position++;
                        if (!state.Push(buffer[i], position, sha, out start, out hex)) continue;
                        order++;
                        writer.WriteLine("INSERT dbo.ChunkSignatures (GroupID, ChunkHash, ChunkSize, SequenceOrder, OriginalOffset, IsActive)");
                        writer.WriteLine("VALUES (@CdcGroupID, '{0}', {1}, {2}, {3}, 1);", hex,
                            position - start + 1, order, start);
                    }
                }
                long lastStart;
                string lastHash;
                if (state.Flush(sha, out lastStart, out lastHash))
                {
                    order++;
                    writer.WriteLine("INSERT dbo.ChunkSignatures (GroupID, ChunkHash, ChunkSize, SequenceOrder, OriginalOffset, IsActive)");
                    writer.WriteLine("VALUES (@CdcGroupID, '{0}', {1}, {2}, {3}, 1);", lastHash,
                        position - lastStart + 1, order, lastStart);
                }
            }
            writer.WriteLine("IF (SELECT COUNT_BIG(*) FROM dbo.ChunkSignatures WHERE GroupID=@CdcGroupID) < {0}", minMatchCount);
            writer.WriteLine("    THROW 51034, 'Not enough CDC chunks to satisfy MinMatchCount.', 1;");
            writer.WriteLine("COMMIT TRANSACTION;");
            writer.WriteLine("SELECT @CdcGroupID AS CreatedGroupID, {0} AS ChunkCount;", order);
            writer.WriteLine("-- After independent validation: UPDATE dbo.ChunkSignatureGroups SET IsActive=1 WHERE GroupID=<CreatedGroupID>;");
            return order;
        }

        /// <summary>
        /// Phương pháp 2: cửa sổ trượt rolling hash xác định điểm cắt theo nội dung,
        /// SHA-256 xác minh từng chunk và bộ khớp xét ngưỡng/chuỗi thứ tự.
        /// Bộ nhớ tệp O(ReadBufferSize + Sum(MaxChunkSize+WindowBytes) của profile).
        /// </summary>
        private static Match ScanSlidingStream(Stream input, Snapshot snapshot, CancellationToken ct, Action<long> onBytesRead = null)
        {
            if (snapshot.byCdcProfile.Count == 0) return null;
            var gapMatchers = snapshot.Groups.Where(g => g.ContentDefined && g.MaxGapBytes.HasValue &&
                g.Cdc != null && snapshot.byCdcProfile.ContainsKey(g.Cdc.Key))
                .ToDictionary(g => g.GroupID, g => new GroupMatcher(g));
            var orderMatchers = snapshot.Groups.Where(g => g.ContentDefined && !g.MaxGapBytes.HasValue &&
                g.Cdc != null && snapshot.byCdcProfile.ContainsKey(g.Cdc.Key))
                .ToDictionary(g => g.GroupID, g => new CdcOrderMatcher(g));
            var cdcStates = snapshot.byCdcProfile.Values.Select(v => new CdcState(v.Config, v)).ToArray();
            var readBuffer = GetReadBuffer();
            long offset = -1;
            using (var sha = SHA256.Create())
            {
                int n;
                while ((n = input.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    if (onBytesRead != null) onBytesRead(n);
                    for (int i = 0; i < n; i++)
                    {
                        if ((i & 0x3fff) == 0) ct.ThrowIfCancellationRequested();
                        offset++;
                        foreach (CdcState state in cdcStates)
                        {
                            long chunkStart;
                            string digest;
                            if (!state.Push(readBuffer[i], offset, sha, out chunkStart, out digest)) continue;
                            Match hit = OfferCdcChunk(state.Index, digest, chunkStart, gapMatchers, orderMatchers);
                            if (hit != null) return hit;
                        }
                    }
                }
                foreach (CdcState state in cdcStates)
                {
                    long chunkStart;
                    string digest;
                    if (!state.Flush(sha, out chunkStart, out digest)) continue;
                    Match hit = OfferCdcChunk(state.Index, digest, chunkStart, gapMatchers, orderMatchers);
                    if (hit != null) return hit;
                }
            }
            return null;
        }
        /// <summary>Kiểm thử CDC với mảng byte; quét tệp thật sử dụng ScanFile (streaming).</summary>
        public static Match ScanSlidingBytes(byte[] data, Snapshot snapshot, CancellationToken ct)
        {
            if (data == null || snapshot == null ||
                snapshot.byCdcProfile.Count == 0) return null;
            using (var stream = new MemoryStream(data, false)) return ScanSlidingStream(stream, snapshot, ct);
        }
    }
}
