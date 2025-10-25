using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Phân tích HÀNH VI xảy ra trong thời gian chạy, không phải kiểm tra chuỗi tĩnh trong file.
    /// Đầu vào là các sự kiện Windows (WMI process start và FileSystemWatcher).
    /// Không thực thi mẫu, không kết luận malware, không tự cách ly.
    /// FileSystemWatcher KHÔNG cung cấp PID gây thay đổi file: cảnh báo chỉ gắn với thư mục.
    /// </summary>
    public sealed class BehavioralFinding
    {
        public string Scope { get; set; }
        public string Evidence { get; set; }
        public int Score { get; set; }
    }

    public sealed class BehavioralActivityAnalyzer
    {
        private sealed class FileActivity
        {
            public DateTime Time;
            public string Path;
            public WatcherChangeTypes Change;
            public bool ExtensionChanged;
        }
        private sealed class DirectoryState
        {
            public readonly List<FileActivity> Events = new List<FileActivity>();
            public readonly Dictionary<string, DateTime> RecentEvents =
                new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            public DateTime LastAlert;
        }

        private static readonly TimeSpan Window = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan AlertCooldown = TimeSpan.FromMinutes(2);
        private static readonly HashSet<string> Documents = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".pdf", ".txt", ".rtf",
            ".csv", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".zip", ".7z", ".rar",
            ".sql", ".db", ".sqlite", ".json", ".xml", ".cs", ".py"
        };

        private readonly object sync = new object();
        private readonly Dictionary<string, DirectoryState> directories =
            new Dictionary<string, DirectoryState>(StringComparer.OrdinalIgnoreCase);

        public void Reset()
        {
            lock (sync) directories.Clear();
        }

        /// <summary>Đánh giá metadata tiến trình quan sát được qua WMI (không đọc/chạy payload).</summary>
        public static BehavioralFinding AssessProcessStart(string executablePath, string commandLine, string parentName)
        {
            string exe = executablePath ?? "";
            string name = Path.GetFileName(exe).ToLowerInvariant();
            string cmd = (commandLine ?? "").ToLowerInvariant();
            string parent = (parentName ?? "").ToLowerInvariant();
            int score = 0;
            var reasons = new List<string>();

            bool shell = new[] { "cmd.exe", "powershell.exe", "pwsh.exe", "wscript.exe",
                "cscript.exe", "mshta.exe", "rundll32.exe", "regsvr32.exe" }.Contains(name);
            bool office = new[] { "winword.exe", "excel.exe", "powerpnt.exe",
                "outlook.exe", "onenote.exe" }.Contains(parent);
            if (office && shell)
            {
                score += 85;
                reasons.Add("ứng dụng Office khởi chạy shell/script");
            }
            if ((name == "powershell.exe" || name == "pwsh.exe") &&
                (cmd.Contains("-enc ") || cmd.Contains("-encodedcommand") || cmd.Contains("frombase64string")))
            {
                score += 80;
                reasons.Add("tiến trình PowerShell nhận lệnh mã hóa");
            }
            if (shell && (cmd.Contains("downloadstring") || cmd.Contains("invoke-webrequest") ||
                cmd.Contains("iex(") || cmd.Contains("invoke-expression")))
            {
                score += 70;
                reasons.Add("shell có lệnh tải/thi hành dữ liệu");
            }
            string temp = Path.GetTempPath();
            if (!string.IsNullOrEmpty(exe) && !string.IsNullOrEmpty(temp) &&
                exe.StartsWith(temp, StringComparison.OrdinalIgnoreCase) &&
                (name.EndsWith(".exe") || name.EndsWith(".com")))
            {
                score += 65;
                reasons.Add("tệp thực thi khởi chạy trong thư mục Temp");
            }
            if (score < 65) return null;
            return new BehavioralFinding
            {
                Score = Math.Min(100, score),
                Scope = string.IsNullOrWhiteSpace(exe) ? "(tiến trình không rõ đường dẫn)" : exe,
                Evidence = string.Join("; ", reasons.ToArray())
            };
        }

        /// <summary>
        /// Gom chuỗi sự kiện trong 30 giây theo THƯ MỤC, chống sự kiện lặp và cảnh báo liên tục.
        /// Nguồn dữ liệu không định danh được tiến trình đã ghi file.
        /// </summary>
        public BehavioralFinding ObserveFileChange(string path, string oldPath,
            WatcherChangeTypes change, DateTime timeUtc)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                (change != WatcherChangeTypes.Changed && change != WatcherChangeTypes.Renamed &&
                 change != WatcherChangeTypes.Deleted)) return null;
            string folder;
            try { folder = Path.GetDirectoryName(Path.GetFullPath(path)); }
            catch (Exception) { return null; }
            if (string.IsNullOrEmpty(folder)) return null;

            string newExtension = Path.GetExtension(path);
            string oldExtension = string.IsNullOrWhiteSpace(oldPath) ? "" : Path.GetExtension(oldPath);
            bool renamedDoc = change == WatcherChangeTypes.Renamed && Documents.Contains(oldExtension) &&
                !string.Equals(oldExtension, newExtension, StringComparison.OrdinalIgnoreCase);
            bool knownDoc = Documents.Contains(newExtension) || Documents.Contains(oldExtension);
            if (!knownDoc) return null;
            lock (sync)
            {
                DirectoryState state;
                if (!directories.TryGetValue(folder, out state))
                {
                    if (directories.Count >= 128)
                    {
                        // Chỉ giữ những thư mục vẫn có hoạt động gần đây (giới hạn RAM).
                        foreach (string stale in directories.Where(pair =>
                            pair.Value.Events.Count == 0 || pair.Value.Events[pair.Value.Events.Count - 1].Time < timeUtc - Window)
                            .Select(pair => pair.Key).ToArray()) directories.Remove(stale);
                        if (directories.Count >= 128) directories.Clear();
                    }
                    state = new DirectoryState();
                    directories[folder] = state;
                }

                state.Events.RemoveAll(item => item.Time < timeUtc - Window);
                foreach (string key in state.RecentEvents.Where(pair => pair.Value < timeUtc.AddSeconds(-1))
                    .Select(pair => pair.Key).ToArray()) state.RecentEvents.Remove(key);
                string duplicateKey = ((int)change).ToString() + "|" + path;
                DateTime last;
                if (state.RecentEvents.TryGetValue(duplicateKey, out last) &&
                    (timeUtc - last).TotalMilliseconds >= 0 && (timeUtc - last).TotalMilliseconds < 400)
                    return null;
                state.RecentEvents[duplicateKey] = timeUtc;
                state.Events.Add(new FileActivity
                {
                    Path = path, Change = change, ExtensionChanged = renamedDoc, Time = timeUtc
                });
                if (state.Events.Count > 512) state.Events.RemoveRange(0, state.Events.Count - 512);

                int renameCount = state.Events.Where(x => x.ExtensionChanged)
                    .Select(x => x.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                int modifiedCount = state.Events.Where(x => x.Change == WatcherChangeTypes.Changed)
                    .Select(x => x.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                int deletedCount = state.Events.Where(x => x.Change == WatcherChangeTypes.Deleted)
                    .Select(x => x.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                int score = 0;
                string evidence = null;
                if (renameCount >= 12)
                {
                    score = 90;
                    evidence = renameCount + " tài liệu bị đổi phần mở rộng trong 30 giây";
                }
                else if (renameCount >= 6 && deletedCount >= 6)
                {
                    score = 80;
                    evidence = renameCount + " tài liệu đổi phần mở rộng và " + deletedCount + " tài liệu bị xóa trong 30 giây";
                }
                else if (modifiedCount >= 35)
                {
                    score = 68;
                    evidence = modifiedCount + " tài liệu khác nhau bị sửa nhanh trong 30 giây";
                }
                if (score == 0 || (timeUtc - state.LastAlert) < AlertCooldown) return null;
                state.LastAlert = timeUtc;
                return new BehavioralFinding
                {
                    Score = score,
                    Scope = folder,
                    Evidence = evidence + " (nguồn: FileSystemWatcher; chưa xác định tiến trình gây ra)"
                };
            }
        }
    }
}
