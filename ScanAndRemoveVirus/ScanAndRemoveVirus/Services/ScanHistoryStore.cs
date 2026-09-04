using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ScanAndRemoveVirus.Services
{
    public class HistoryEntry
    {
        public DateTime Time { get; set; }
        public string Type { get; set; }   // Quét nhanh / Quét toàn bộ / Quét tùy chọn / Bảo vệ thời gian thực / Cập nhật CSDL
        public string Scope { get; set; }  // phạm vi: khu vực / đường dẫn
        public int Files { get; set; }
        public int Threats { get; set; }
        public double Seconds { get; set; }
        public string Result
        {
            get { return Threats > 0 ? "Phát hiện mối đe dọa" : "An toàn"; }
        }
    }

    // Lịch sử quét thật, lưu File: <solution>\AppData\scanhistory.log (xem DataDir)
    // Dòng: time|type|scope|files|threats|seconds (bỏ trường chứa '|')
    public static class ScanHistoryStore
    {
        private const int MaxEntries = 1000;
        private static readonly object Sync = new object();

        public static string LogPath
        {
            get { return DataDir.Resolve("scanhistory.log"); }
        }

        public static void Add(string type, string scope, int files, int threats, double seconds)
        {
            AddEntry(new HistoryEntry
            {
                Time = DateTime.Now,
                Type = type,
                Scope = scope ?? "",
                Files = files,
                Threats = threats,
                Seconds = seconds
            });
        }

        public static void AddEntry(HistoryEntry e)
        {
            lock (Sync)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                    var lines = new List<string>();
                    if (File.Exists(LogPath))
                    {
                        foreach (string l in File.ReadAllLines(LogPath))
                            if (l.Length > 0) lines.Add(l);
                    }
                    lines.Add(string.Join("|",
                        e.Time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                        Clean(e.Type), Clean(e.Scope),
                        e.Files.ToString(CultureInfo.InvariantCulture),
                        e.Threats.ToString(CultureInfo.InvariantCulture),
                        e.Seconds.ToString("0.0", CultureInfo.InvariantCulture)));
                    while (lines.Count > MaxEntries) lines.RemoveAt(0); // giữ MaxEntries mới nhất
                    File.WriteAllLines(LogPath, lines, new UTF8Encoding(false));
                }
                catch (Exception) { } // history best-effort
            }
        }

        // Xóa đúng 1 bản ghi (khớp thời gian + loại + phạm vi, lấy bản đầu tiên)
        public static bool Remove(HistoryEntry e)
        {
            if (e == null) return false;
            lock (Sync)
            {
                try
                {
                    if (!File.Exists(LogPath)) return false;
                    string want = string.Join("|",
                        e.Time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                        Clean(e.Type), Clean(e.Scope)) + "|";
                    var kept = new List<string>();
                    bool removed = false;
                    foreach (string l in File.ReadAllLines(LogPath))
                    {
                        if (l.Length == 0) continue;
                        if (!removed && l.StartsWith(want, StringComparison.Ordinal))
                        {
                            removed = true;
                            continue;
                        }
                        kept.Add(l);
                    }
                    if (removed) File.WriteAllLines(LogPath, kept, new UTF8Encoding(false));
                    return removed;
                }
                catch (Exception) { return false; }
            }
        }

        // Mới nhất -> cũ nhất
        public static List<HistoryEntry> Entries()
        {
            var list = new List<HistoryEntry>();
            lock (Sync)
            {
                try
                {
                    if (!File.Exists(LogPath)) return list;
                    foreach (string line in File.ReadAllLines(LogPath))
                    {
                        if (line.Length == 0) continue;
                        string[] p = line.Split('|');
                        if (p.Length < 6) continue;
                        DateTime t;
                        DateTime.TryParseExact(p[0], "yyyy-MM-dd HH:mm:ss",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out t);
                        int files, threats; double sec;
                        int.TryParse(p[3], out files);
                        int.TryParse(p[4], out threats);
                        double.TryParse(p[5], NumberStyles.Float, CultureInfo.InvariantCulture, out sec);
                        list.Add(new HistoryEntry
                        {
                            Time = t,
                            Type = p[1],
                            Scope = p[2],
                            Files = files,
                            Threats = threats,
                            Seconds = sec
                        });
                    }
                }
                catch (Exception) { }
            }
            list.Reverse();
            return list;
        }

        public static void ExportCsv(string csvPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Thời gian;Loại quét;Phạm vi;Tệp đã quét;Mối đe dọa;Thời lượng (giây)");
            foreach (HistoryEntry e in Entries())
                sb.AppendLine(string.Join(";",
                    e.Time.ToString("dd/MM/yyyy HH:mm:ss"),
                    Csv(e.Type), Csv(e.Scope),
                    e.Files.ToString(CultureInfo.InvariantCulture),
                    e.Threats.ToString(CultureInfo.InvariantCulture),
                    e.Seconds.ToString("0.0", CultureInfo.InvariantCulture)));
            File.WriteAllText(csvPath, sb.ToString(), new UTF8Encoding(true)); // BOM để Excel đọc đúng Unicode
        }

        private static string Csv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.IndexOf(';') >= 0 || s.IndexOf('"') >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        private static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace('|', '/').Replace("\r", " ").Replace("\n", " ");
        }

        // ================= Số liệu dẫn xuất cho các khu thống kê =================

        public static HistoryEntry LatestOfType(string typePrefix)
        {
            foreach (HistoryEntry e in Entries()) // đã đảo ngược: mới nhất trước
                if (e.Type.StartsWith(typePrefix, StringComparison.Ordinal)) return e;
            return null;
        }

        public static long TotalFilesScanned()
        {
            long sum = 0;
            foreach (HistoryEntry e in Entries()) sum += e.Files;
            return sum;
        }

        public static long TotalThreatsDetected()
        {
            long sum = 0;
            foreach (HistoryEntry e in Entries()) sum += e.Threats;
            return sum;
        }

        // ================= Tem thời điểm cập nhật CSDL chữ ký =================
        // Dùng chung giữa tab Tổng quan (nút Kiểm tra cập nhật) và tab Bảo vệ (panel trạng thái)
        public static string SignatureUpdatePath
        {
            get { return DataDir.Resolve("dbupdate.txt"); }
        }

        public static bool TryGetLastSignatureUpdate(out DateTime when)
        {
            when = DateTime.MinValue;
            try
            {
                if (File.Exists(SignatureUpdatePath))
                    return DateTime.TryParse(File.ReadAllText(SignatureUpdatePath), out when);
            }
            catch (Exception) { }
            return false;
        }

        public static void MarkSignatureUpdated(DateTime when)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SignatureUpdatePath));
            File.WriteAllText(SignatureUpdatePath, when.ToString("o"));
        }
    }
}
