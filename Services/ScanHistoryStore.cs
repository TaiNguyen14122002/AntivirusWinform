/*using System;
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
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using ScanAndRemoveVirus.Database;

namespace ScanAndRemoveVirus.Services
{
    public class HistoryEntry
    {
        /// <summary>
        /// ID thật của bản ghi trong dbo.ScanHistory.
        /// Dùng để xóa chính xác một bản ghi.
        /// </summary>
        public long ScanID { get; set; }

        public DateTime Time { get; set; }

        public string Type { get; set; }

        public string Scope { get; set; }

        public int Files { get; set; }

        public int Threats { get; set; }

        public double Seconds { get; set; }

        public string Result
        {
            get
            {
                return Threats > 0
                    ? "Phát hiện mối đe dọa"
                    : "An toàn";
            }
        }
    }

    /// <summary>
    /// Quản lý lịch sử quét.
    ///
    /// Dữ liệu lịch sử được lưu trong:
    /// dbo.ScanHistory
    ///
    /// Thời điểm cập nhật CSDL chữ ký được lưu trong:
    /// dbo.Settings.LastDatabaseUpdate
    ///
    /// Không còn sử dụng:
    /// - scanhistory.log
    /// - dbupdate.txt
    /// </summary>
    public static class ScanHistoryStore
    {
        private const int MaxEntries = 1000;

        private static readonly object Sync =
            new object();

        // =========================================================
        // ADD
        // =========================================================

        public static void Add(
            string type,
            string scope,
            int files,
            int threats,
            double seconds)
        {
            AddEntry(
                new HistoryEntry
                {
                    Time = DateTime.Now,
                    Type = type ?? "",
                    Scope = scope ?? "",
                    Files = files,
                    Threats = threats,
                    Seconds = seconds
                });
        }

        /// <summary>
        /// Thêm một bản ghi lịch sử vào dbo.ScanHistory.
        /// </summary>
        public static void AddEntry(HistoryEntry entry)
        {
            if (entry == null)
                return;

            lock (Sync)
            {
                try
                {
                    DateTime startedAt = entry.Time;

                    if (startedAt == DateTime.MinValue)
                        startedAt = DateTime.Now;

                    double seconds = entry.Seconds;

                    if (seconds < 0)
                        seconds = 0;

                    DateTime completedAt =
                        startedAt.AddSeconds(seconds);

                    string scanType =
                        ToDatabaseScanType(entry.Type);

                    string title =
                        Clean(entry.Type);

                    string scope =
                        Clean(entry.Scope);

                    string resultStatus =
                        entry.Threats > 0
                            ? "ThreatDetected"
                            : "Completed";

                    using (var conn = DbConnection.Create())
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
INSERT INTO dbo.ScanHistory
(
    ScanType,
    StartedAt,
    CompletedAt,
    ScanLocation,
    FilesScanned,
    ThreatCount,
    ResultStatus,
    ActivityTitle,
    ActivityDetails,
    ErrorMessage
)
VALUES
(
    @ScanType,
    @StartedAt,
    @CompletedAt,
    @ScanLocation,
    @FilesScanned,
    @ThreatCount,
    @ResultStatus,
    @ActivityTitle,
    @ActivityDetails,
    NULL
);

SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

                        cmd.Parameters.Add(
                            "@ScanType",
                            SqlDbType.VarChar,
                            20).Value = scanType;

                        cmd.Parameters.Add(
                            "@StartedAt",
                            SqlDbType.DateTime2).Value =
                            startedAt;

                        cmd.Parameters.Add(
                            "@CompletedAt",
                            SqlDbType.DateTime2).Value =
                            completedAt;

                        cmd.Parameters.Add(
                            "@ScanLocation",
                            SqlDbType.NVarChar,
                            1000).Value =
                            string.IsNullOrEmpty(scope)
                                ? (object)DBNull.Value
                                : scope;

                        cmd.Parameters.Add(
                            "@FilesScanned",
                            SqlDbType.BigInt).Value =
                            Math.Max(0, entry.Files);

                        cmd.Parameters.Add(
                            "@ThreatCount",
                            SqlDbType.Int).Value =
                            Math.Max(0, entry.Threats);

                        cmd.Parameters.Add(
                            "@ResultStatus",
                            SqlDbType.VarChar,
                            30).Value =
                            resultStatus;

                        cmd.Parameters.Add(
                            "@ActivityTitle",
                            SqlDbType.NVarChar,
                            200).Value =
                            string.IsNullOrEmpty(title)
                                ? (object)DBNull.Value
                                : title;

                        cmd.Parameters.Add(
                            "@ActivityDetails",
                            SqlDbType.NVarChar,
                            1000).Value =
                            string.IsNullOrEmpty(scope)
                                ? (object)DBNull.Value
                                : scope;

                        conn.Open();

                        object value =
                            cmd.ExecuteScalar();

                        if (value != null &&
                            value != DBNull.Value)
                        {
                            entry.ScanID =
                                Convert.ToInt64(value);
                        }
                    }

                    TrimOldEntries();
                }
                catch (Exception)
                {
                    // History là best-effort.
                    // Không để lỗi SQL làm dừng chức năng quét.
                }
            }
        }

        // =========================================================
        // REMOVE
        // =========================================================

        /// <summary>
        /// Xóa một bản ghi lịch sử.
        ///
        /// Ưu tiên ScanID vì đây là khóa chính.
        /// Nếu HistoryEntry cũ chưa có ScanID thì tìm theo
        /// StartedAt + ActivityTitle + ScanLocation.
        /// </summary>
        public static bool Remove(HistoryEntry entry)
        {
            if (entry == null)
                return false;

            lock (Sync)
            {
                try
                {
                    using (var conn = DbConnection.Create())
                    using (var cmd = conn.CreateCommand())
                    {
                        if (entry.ScanID > 0)
                        {
                            cmd.CommandText = @"
DELETE FROM dbo.ScanHistory
WHERE ScanID = @ScanID;";

                            cmd.Parameters.Add(
                                "@ScanID",
                                SqlDbType.BigInt).Value =
                                entry.ScanID;
                        }
                        else
                        {
                            cmd.CommandText = @"
DELETE FROM dbo.ScanHistory
WHERE ScanID =
(
    SELECT TOP (1) ScanID
    FROM dbo.ScanHistory
    WHERE StartedAt = @StartedAt
      AND ISNULL(ActivityTitle, '') = @ActivityTitle
      AND ISNULL(ScanLocation, '') = @ScanLocation
    ORDER BY ScanID ASC
);";

                            cmd.Parameters.Add(
                                "@StartedAt",
                                SqlDbType.DateTime2).Value =
                                entry.Time;

                            cmd.Parameters.Add(
                                "@ActivityTitle",
                                SqlDbType.NVarChar,
                                200).Value =
                                Clean(entry.Type);

                            cmd.Parameters.Add(
                                "@ScanLocation",
                                SqlDbType.NVarChar,
                                1000).Value =
                                Clean(entry.Scope);
                        }

                        conn.Open();

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        // =========================================================
        // READ
        // =========================================================

        /// <summary>
        /// Lấy tối đa 1000 bản ghi mới nhất.
        /// </summary>
        public static List<HistoryEntry> Entries()
        {
            var list =
                new List<HistoryEntry>();

            lock (Sync)
            {
                try
                {
                    using (var conn = DbConnection.Create())
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
SELECT TOP (@MaxEntries)
       ScanID,
       ScanType,
       StartedAt,
       CompletedAt,
       ScanLocation,
       FilesScanned,
       ThreatCount,
       ResultStatus,
       ActivityTitle,
       ActivityDetails
FROM dbo.ScanHistory
ORDER BY StartedAt DESC, ScanID DESC;";

                        cmd.Parameters.Add(
                            "@MaxEntries",
                            SqlDbType.Int).Value =
                            MaxEntries;

                        conn.Open();

                        using (var reader =
                            cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime started =
                                    reader.GetDateTime(
                                        reader.GetOrdinal(
                                            "StartedAt"));

                                DateTime? completed =
                                    null;

                                int completedOrdinal =
                                    reader.GetOrdinal(
                                        "CompletedAt");

                                if (!reader.IsDBNull(
                                    completedOrdinal))
                                {
                                    completed =
                                        reader.GetDateTime(
                                            completedOrdinal);
                                }

                                double seconds = 0;

                                if (completed.HasValue)
                                {
                                    seconds =
                                        (completed.Value -
                                         started)
                                        .TotalSeconds;

                                    if (seconds < 0)
                                        seconds = 0;
                                }

                                long files64 =
                                    Convert.ToInt64(
                                        reader[
                                            "FilesScanned"]);

                                int files;

                                if (files64 >
                                    int.MaxValue)
                                {
                                    files =
                                        int.MaxValue;
                                }
                                else if (files64 < 0)
                                {
                                    files = 0;
                                }
                                else
                                {
                                    files =
                                        (int)files64;
                                }

                                int threats =
                                    Convert.ToInt32(
                                        reader[
                                            "ThreatCount"]);

                                string databaseType =
                                    Convert.ToString(
                                        reader[
                                            "ScanType"]);

                                string activityTitle =
                                    reader[
                                        "ActivityTitle"] ==
                                    DBNull.Value
                                        ? null
                                        : Convert.ToString(
                                            reader[
                                                "ActivityTitle"]);

                                string scope =
                                    reader[
                                        "ScanLocation"] ==
                                    DBNull.Value
                                        ? ""
                                        : Convert.ToString(
                                            reader[
                                                "ScanLocation"]);

                                list.Add(
                                    new HistoryEntry
                                    {
                                        ScanID =
                                            Convert.ToInt64(
                                                reader[
                                                    "ScanID"]),

                                        Time =
                                            started,

                                        Type =
                                            !string.IsNullOrEmpty(
                                                activityTitle)
                                                ? activityTitle
                                                : FromDatabaseScanType(
                                                    databaseType),

                                        Scope =
                                            scope,

                                        Files =
                                            files,

                                        Threats =
                                            threats,

                                        Seconds =
                                            seconds
                                    });
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Trả danh sách rỗng nếu SQL lỗi.
                }
            }

            return list;
        }

        // =========================================================
        // EXPORT CSV
        // =========================================================

        public static void ExportCsv(
            string csvPath)
        {
            if (string.IsNullOrWhiteSpace(csvPath))
                return;

            var sb =
                new StringBuilder();

            sb.AppendLine(
                "Thời gian;Loại quét;Phạm vi;" +
                "Tệp đã quét;Mối đe dọa;" +
                "Thời lượng (giây)");

            foreach (
                HistoryEntry entry
                in Entries())
            {
                sb.AppendLine(
                    string.Join(
                        ";",
                        entry.Time.ToString(
                            "dd/MM/yyyy HH:mm:ss"),

                        Csv(entry.Type),

                        Csv(entry.Scope),

                        entry.Files.ToString(
                            CultureInfo.InvariantCulture),

                        entry.Threats.ToString(
                            CultureInfo.InvariantCulture),

                        entry.Seconds.ToString(
                            "0.0",
                            CultureInfo.InvariantCulture)));
            }

            File.WriteAllText(
                csvPath,
                sb.ToString(),
                new UTF8Encoding(true));
        }

        // =========================================================
        // STATISTICS
        // =========================================================

        public static HistoryEntry LatestOfType(
            string typePrefix)
        {
            if (string.IsNullOrEmpty(typePrefix))
                return null;

            foreach (
                HistoryEntry entry
                in Entries())
            {
                if (!string.IsNullOrEmpty(
                        entry.Type) &&
                    entry.Type.StartsWith(
                        typePrefix,
                        StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        public static long TotalFilesScanned()
        {
            try
            {
                using (var conn = DbConnection.Create())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT ISNULL(SUM(FilesScanned), 0)
FROM dbo.ScanHistory
WHERE ResultStatus <> 'Running';";

                    conn.Open();

                    return Convert.ToInt64(
                        cmd.ExecuteScalar());
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static long TotalThreatsDetected()
        {
            try
            {
                using (var conn = DbConnection.Create())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT ISNULL(SUM(
    CONVERT(BIGINT, ThreatCount)
), 0)
FROM dbo.ScanHistory
WHERE ResultStatus <> 'Running';";

                    conn.Open();

                    return Convert.ToInt64(
                        cmd.ExecuteScalar());
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // =========================================================
        // SIGNATURE DATABASE UPDATE
        // =========================================================

        /// <summary>
        /// Lấy thời điểm cập nhật CSDL virus gần nhất từ
        /// Settings.LastDatabaseUpdate.
        /// </summary>
        public static bool TryGetLastSignatureUpdate(
            out DateTime when)
        {
            when = DateTime.MinValue;

            try
            {
                using (var conn = DbConnection.Create())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT TOP (1)
       LastDatabaseUpdate
FROM dbo.Settings
ORDER BY SettingID ASC;";

                    conn.Open();

                    object value =
                        cmd.ExecuteScalar();

                    if (value == null ||
                        value == DBNull.Value)
                    {
                        return false;
                    }

                    when =
                        Convert.ToDateTime(value);

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Cập nhật thời điểm cập nhật CSDL chữ ký.
        /// </summary>
        public static void MarkSignatureUpdated(
            DateTime when)
        {
            try
            {
                using (var conn = DbConnection.Create())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
UPDATE dbo.Settings
SET LastDatabaseUpdate = @When,
    UpdatedAt = SYSDATETIME()
WHERE SettingID =
(
    SELECT MIN(SettingID)
    FROM dbo.Settings
);";

                    cmd.Parameters.Add(
                        "@When",
                        SqlDbType.DateTime2).Value =
                        when;

                    conn.Open();

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                // Không làm ứng dụng crash nếu SQL lỗi.
            }
        }

        // =========================================================
        // HELPERS
        // =========================================================

        /// <summary>
        /// Chuyển tên hiển thị dài thành mã <= 20 ký tự
        /// phù hợp với ScanHistory.ScanType VARCHAR(20).
        ///
        /// Không cần ALTER CSDL.
        /// </summary>
        private static string ToDatabaseScanType(
            string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return "Other";

            string value = type.Trim();

            if (value.StartsWith(
                "Quét nhanh",
                StringComparison.OrdinalIgnoreCase))
            {
                return "Quick";
            }

            if (value.StartsWith(
                "Quét toàn bộ",
                StringComparison.OrdinalIgnoreCase))
            {
                return "Full";
            }

            if (value.StartsWith(
                "Quét tùy chọn",
                StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith(
                    "Quét tuỳ chọn",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Custom";
            }

            if (value.StartsWith(
                "Bảo vệ thời gian thực",
                StringComparison.OrdinalIgnoreCase))
            {
                return "RealTime";
            }

            if (value.StartsWith(
                "Cập nhật CSDL",
                StringComparison.OrdinalIgnoreCase))
            {
                return "Update";
            }

            if (value.StartsWith(
                "Phát hiện hành vi",
                StringComparison.OrdinalIgnoreCase))
            {
                return "Behavior";
            }

            /*
             * ScanType chỉ VARCHAR(20).
             *
             * Không lưu chuỗi tiếng Việt tùy ý vào đây vì:
             * 1. VARCHAR không phải NVARCHAR.
             * 2. Giới hạn 20 ký tự.
             *
             * Tên đầy đủ đã được lưu ở ActivityTitle.
             */
            return "Other";
        }

        private static string FromDatabaseScanType(
            string type)
        {
            switch (type)
            {
                case "Quick":
                    return "Quét nhanh";

                case "Full":
                    return "Quét toàn bộ";

                case "Custom":
                    return "Quét tùy chọn";

                case "RealTime":
                    return "Bảo vệ thời gian thực";

                case "Update":
                    return "Cập nhật CSDL";

                case "Behavior":
                    return "Phát hiện hành vi đáng ngờ";

                default:
                    return type ?? "";
            }
        }

        private static void TrimOldEntries()
        {
            /*
             * Giữ hành vi MaxEntries = 1000 giống phiên bản
             * scanhistory.log trước đây.
             *
             * Chỉ xóa các bản ghi lịch sử không còn nằm trong
             * TOP 1000 mới nhất.
             *
             * Nếu bản ghi đã được ThreatDetections tham chiếu,
             * KHÔNG xóa để tránh vi phạm Foreign Key.
             */
            try
            {
                using (var conn = DbConnection.Create())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
DELETE FROM dbo.ScanHistory
WHERE ScanID NOT IN
(
    SELECT TOP (@MaxEntries)
           ScanID
    FROM dbo.ScanHistory
    ORDER BY StartedAt DESC,
             ScanID DESC
)
AND NOT EXISTS
(
    SELECT 1
    FROM dbo.ThreatDetections td
    WHERE td.ScanID =
          dbo.ScanHistory.ScanID
);";

                    cmd.Parameters.Add(
                        "@MaxEntries",
                        SqlDbType.Int).Value =
                        MaxEntries;

                    conn.Open();

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
            }
        }

        private static string Csv(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.IndexOf(';') >= 0 ||
                value.IndexOf('"') >= 0 ||
                value.IndexOf('\r') >= 0 ||
                value.IndexOf('\n') >= 0)
            {
                return "\"" +
                       value.Replace(
                           "\"",
                           "\"\"")
                       .Replace(
                           "\r",
                           " ")
                       .Replace(
                           "\n",
                           " ") +
                       "\"";
            }

            return value;
        }

        private static string Clean(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value
                .Replace(
                    "\r",
                    " ")
                .Replace(
                    "\n",
                    " ");
        }
    }
}