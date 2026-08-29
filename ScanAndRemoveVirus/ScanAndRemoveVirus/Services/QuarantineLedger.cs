using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ScanAndRemoveVirus.Services
{
    // Sổ cái các tệp đã cách ly: %AppData%\ScanAndRemoveVirus\quarantine.log
    // Mỗi dòng: id|originalPath|yyyy-MM-dd HH:mm:ss|threat|name|sizeBytes (NTFS không có '|')
    internal static class QuarantineLedger
    {
        private static readonly object Sync = new object();

        private static string LogPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ScanAndRemoveVirus", "quarantine.log");
            }
        }

        public static void Record(string id, string originalPath, DateTime time,
            string threat, string name, string sizeBytes)
        {
            lock (Sync)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                    File.AppendAllText(LogPath, string.Join("|",
                        id, originalPath,
                        time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                        Clean(threat), Clean(name), sizeBytes) + Environment.NewLine,
                        new UTF8Encoding(false));
                }
                catch (Exception) { } // sổ hỏng không chặn việc cách ly
            }
        }

        public static void Remove(string id)
        {
            lock (Sync)
            {
                try
                {
                    if (!File.Exists(LogPath)) return;
                    var keep = new List<string>();
                    foreach (string line in File.ReadAllLines(LogPath))
                    {
                        if (line.Length == 0) continue;
                        if (line.Split('|')[0] != id) keep.Add(line);
                    }
                    File.WriteAllLines(LogPath, keep, new UTF8Encoding(false));
                }
                catch (Exception) { }
            }
        }

        public static List<ScanEngine.QuarantinedItem> ReadAll()
        {
            var items = new List<ScanEngine.QuarantinedItem>();
            lock (Sync)
            {
                try
                {
                    if (!File.Exists(LogPath)) return items;
                    var lines = File.ReadAllLines(LogPath);
                    var kept = new List<string>();
                    foreach (string line in lines)
                    {
                        if (line.Length == 0) continue;
                        string[] p = line.Split('|');
                        if (p.Length < 6) continue;
                        // Tự dọn: tệp .qtn đã bị xóa tay ngoài thư mục cách ly -> bỏ khỏi sổ
                        string stored = Path.Combine(ScanEngine.QuarantineDir, p[0]);
                        if (!File.Exists(stored)) continue;
                        kept.Add(line);
                        DateTime t;
                        DateTime.TryParseExact(p[2], "yyyy-MM-dd HH:mm:ss",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out t);
                        items.Add(new ScanEngine.QuarantinedItem
                        {
                            Id = p[0],
                            OriginalPath = p[1],
                            DetectedTime = t,
                            Threat = p[3],
                            Name = p[4],
                            Size = FormatSize(p[5])
                        });
                    }
                    if (kept.Count < lines.Length)
                        File.WriteAllLines(LogPath, kept, new UTF8Encoding(false));
                }
                catch (Exception) { }
            }
            return items;
        }

        private static string Clean(string s)
        {
            // loại ký tự phá format dòng
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace('|', '/').Replace("\r", " ").Replace("\n", " ");
        }

        public static string FormatSize(string bytesText)
        {
            long b;
            if (!long.TryParse(bytesText, out b)) return "—";
            if (b < 1024) return b + " B";
            if (b < 1024 * 1024) return (b / 1024.0).ToString("0.#") + " KB";
            if (b < 1024L * 1024 * 1024) return (b / (1024.0 * 1024)).ToString("0.#") + " MB";
            return (b / (1024.0 * 1024 * 1024)).ToString("0.#") + " GB";
        }
    }
}
