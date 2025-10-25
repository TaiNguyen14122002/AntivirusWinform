using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>Nhật ký cục bộ cho đối tượng chưa được kiểm tra. Không cần sửa SQL.</summary>
    public static class LocalScanIssueStore
    {
        public sealed class Issue
        {
            public DateTime TimeUtc { get; set; }
            public string Path { get; set; }
            public string Type { get; set; }
            public string Reason { get; set; }
        }
        private static readonly object Sync = new object();
        private static string FilePath { get { return DataDir.Resolve("scan-issues.jsonl"); } }
        public static void Record(string path, string type, string reason)
        {
            try
            {
                var item = new Issue { TimeUtc = DateTime.UtcNow, Path = Limit(path, 2000),
                    Type = Limit(type, 60), Reason = Limit(reason, 1000) };
                string json = new JavaScriptSerializer().Serialize(item);
                lock (Sync)
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath));
                    if (File.Exists(FilePath) && new FileInfo(FilePath).Length > 2 * 1024 * 1024)
                    {
                        string[] lines = File.ReadAllLines(FilePath, Encoding.UTF8);
                        File.WriteAllLines(FilePath, lines.Skip(Math.Max(0, lines.Length - 800)), new UTF8Encoding(false));
                    }
                    File.AppendAllText(FilePath, json + Environment.NewLine, new UTF8Encoding(false));
                }
            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine("Issue log: " + ex.GetType().Name); }
        }
        public static List<Issue> Recent(int limit = 500)
        {
            lock (Sync)
            {
                var results = new List<Issue>();
                try
                {
                    if (!File.Exists(FilePath)) return results;
                    var parser = new JavaScriptSerializer();
                    foreach (var line in File.ReadAllLines(FilePath, Encoding.UTF8).Reverse().Take(Math.Max(1, Math.Min(2000, limit))))
                    {
                        try { results.Add(parser.Deserialize<Issue>(line)); } catch (Exception) { }
                    }
                }
                catch (Exception) { }
                return results;
            }
        }
        public static void Clear()
        {
            lock (Sync) { if (File.Exists(FilePath)) File.Delete(FilePath); }
        }
        private static string Limit(string s, int max)
        {
            if (s == null) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}
