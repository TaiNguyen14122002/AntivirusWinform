using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Hàng đợi lịch sử chờ SQL Server, sử dụng tệp JSON riêng từng bản ghi.
    /// ID giao dịch được lưu trong cột ActivityDetails HIỆN CÓ (không thêm bảng/cột)
    /// để tra cứu khi client mất kết nối sau lúc SQL đã commit.
    /// </summary>
    public static class OfflineHistoryQueue
    {
        private static readonly object Sync = new object();
        private static int replaying;
        private static string Root { get { return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScanAndRemoveVirus", "PendingHistory"); } }
        public static int PendingCount
        {
            get { try { return Directory.Exists(Root) ? Directory.GetFiles(Root, "*.json").Length : 0; }
                catch (Exception) { return 0; } }
        }
        public static string EnsurePending(HistoryEntry entry)
        {
            if (entry == null) throw new ArgumentNullException("entry");
            lock (Sync)
            {
                if (string.IsNullOrWhiteSpace(entry.OfflineEventId))
                    entry.OfflineEventId = Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(Root);
                string target = Path.Combine(Root, entry.OfflineEventId + ".json");
                if (!File.Exists(target))
                {
                    string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    try
                    {
                        byte[] bytes = new UTF8Encoding(false).GetBytes(new JavaScriptSerializer().Serialize(entry));
                        using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                        File.Move(temp, target);
                    }
                    finally { try { if (File.Exists(temp)) File.Delete(temp); } catch (Exception) { } }
                }
                return entry.OfflineEventId;
            }
        }
        public static void MarkDone(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            lock (Sync)
            {
                string file = Path.Combine(Root, id + ".json");
                try { if (File.Exists(file)) File.Delete(file); }
                catch (Exception ex) { System.Diagnostics.Trace.WriteLine("Đã ghi SQL nhưng chưa xóa queue: " + ex); }
            }
        }
        public static int Replay(int maxBatch = 50)
        {
            if (Interlocked.CompareExchange(ref replaying, 1, 0) != 0) return 0;
            int replayed = 0;
            try
            {
                if (!Directory.Exists(Root)) return 0;
                string[] files = Directory.GetFiles(Root, "*.json")
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).Take(Math.Max(1, Math.Min(100, maxBatch))).ToArray();
                foreach (string file in files)
                {
                    try
                    {
                        var entry = new JavaScriptSerializer().Deserialize<HistoryEntry>(File.ReadAllText(file, Encoding.UTF8));
                        if (entry == null || string.IsNullOrWhiteSpace(entry.OfflineEventId)) continue;
                        ScanHistoryStore.AddEntry(entry); // SQL xác nhận theo OfflineEventId trước khi INSERT
                        if (entry.ScanID <= 0) break; // SQL còn lỗi, giữ bản ghi cho lần sau
                        replayed++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine("Đồng bộ ngoại tuyến: " + ex.GetType().Name);
                        break; // giữ nguyên bản ghi, không bỏ dữ liệu
                    }
                }
            }
            finally { Interlocked.Exchange(ref replaying, 0); }
            return replayed;
        }
    }
}
