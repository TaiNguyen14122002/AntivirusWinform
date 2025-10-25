using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Journal bền vững cho giao dịch giữa NTFS và SQL Server; không thay đổi schema.
    /// KHÔNG tự di chuyển tệp khi phát hiện trạng thái dở dang (cần người dùng xác minh).
    /// </summary>
    public static class QuarantineRecoveryJournal
    {
        public sealed class Operation
        {
            public string Id { get; set; }
            public string Action { get; set; }
            public long DetectionId { get; set; }
            public string OriginalPath { get; set; }
            public string QuarantinePath { get; set; }
            public string Sha256 { get; set; }
            public string State { get; set; }
            public DateTime TimeUtc { get; set; }
            public string Error { get; set; }
        }
        private static readonly object Sync = new object();
        private static string Root { get { return Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData), "ScanAndRemoveVirus", "RecoveryJournal"); } }

        public static string Begin(string action, long detectionId, string originalPath, string quarantinePath, string sha256)
        {
            var op = new Operation { Id = Guid.NewGuid().ToString("N"), Action = action,
                DetectionId = detectionId, OriginalPath = originalPath, QuarantinePath = quarantinePath,
                Sha256 = sha256, State = "Prepared", TimeUtc = DateTime.UtcNow };
            Write(op);  // phải ghi thành công trước khi File.Move
            return op.Id;
        }
        public static void Stage(string id, string stage, string error = null)
        {
            if (string.IsNullOrEmpty(id)) return;
            lock (Sync)
            {
                Operation op = Read(id);
                if (op == null) throw new IOException("Thiếu nhật ký giao dịch cách ly: " + id);
                op.State = stage;
                op.Error = error;
                Write(op);
            }
        }
        public static List<Operation> Outstanding()
        {
            lock (Sync)
            {
                var result = new List<Operation>();
                try
                {
                    if (!Directory.Exists(Root)) return result;
                    foreach (string name in Directory.GetFiles(Root, "*.json").Take(3000))
                    {
                        try
                        {
                            Operation op = new JavaScriptSerializer().Deserialize<Operation>(File.ReadAllText(name, Encoding.UTF8));
                            if (op != null && op.State != "Completed" && op.State != "RolledBack") result.Add(op);
                        }
                        catch (Exception) { }
                    }
                }
                catch (Exception) { }
                return result.OrderByDescending(x => x.TimeUtc).ToList();
            }
        }
        public static string Diagnose(Operation op)
        {
            if (op == null) return "Không có thông tin.";
            bool original = !string.IsNullOrWhiteSpace(op.OriginalPath) && File.Exists(op.OriginalPath);
            bool quarantined = !string.IsNullOrWhiteSpace(op.QuarantinePath) && File.Exists(op.QuarantinePath);
            return (original ? "Gốc: có" : "Gốc: thiếu") + "; " +
                   (quarantined ? "Cách ly: có" : "Cách ly: thiếu") +
                   "; SQL chưa được xác minh. KHÔNG tự xóa/di chuyển tệp.";
        }
        public static void PruneCompleted(int maxAgeDays = 30)
        {
            lock (Sync)
            {
                if (!Directory.Exists(Root)) return;
                foreach (string file in Directory.GetFiles(Root, "*.json"))
                {
                    try
                    {
                        Operation item = new JavaScriptSerializer().Deserialize<Operation>(File.ReadAllText(file, Encoding.UTF8));
                        if (item != null && (item.State == "Completed" || item.State == "RolledBack") &&
                            item.TimeUtc < DateTime.UtcNow.AddDays(-maxAgeDays)) File.Delete(file);
                    }
                    catch (Exception) { }
                }
            }
        }
        private static Operation Read(string id)
        {
            string file = Path.Combine(Root, id + ".json");
            return File.Exists(file) ? new JavaScriptSerializer().Deserialize<Operation>(File.ReadAllText(file, Encoding.UTF8)) : null;
        }
        private static void Write(Operation op)
        {
            lock (Sync)
            {
                Directory.CreateDirectory(Root);
                string destination = Path.Combine(Root, op.Id + ".json");
                string temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    byte[] bytes = new UTF8Encoding(false).GetBytes(new JavaScriptSerializer().Serialize(op));
                    using (var fs = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { fs.Write(bytes, 0, bytes.Length); fs.Flush(true); }
                    if (File.Exists(destination)) File.Replace(temp, destination, null);
                    else File.Move(temp, destination);
                }
                finally { try { if (File.Exists(temp)) File.Delete(temp); } catch (Exception) { } }
            }
        }
    }
}
