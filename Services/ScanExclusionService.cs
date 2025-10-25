using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ScanAndRemoveVirus.Database;

namespace ScanAndRemoveVirus.Services
{
    // Ngoại lệ chỉ áp dụng cho cảnh báo HEURISTIC; không bao giờ vô hiệu hóa chữ ký Hash/CDC.
    public static class ScanExclusionService
    {
        private static readonly object Sync = new object();
        private static HashSet<string> hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static DateTime nextRefreshUtc = DateTime.MinValue;
        public static void Invalidate() { lock (Sync) nextRefreshUtc = DateTime.MinValue; }
        public static bool IsHeuristicExcluded(string physicalPath)
        {
            HashSet<string> snapshot;
            lock (Sync)
            {
                if (DateTime.UtcNow >= nextRefreshUtc)
                {
                    try
                    {
                        var fresh = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        using (var cn = DbConnection.Create())
                        using (var cmd = new SqlCommand(@"SELECT SHA256 FROM dbo.ScanExclusions
                            WHERE IsActive=1 AND ExpiresAtUtc > SYSUTCDATETIME() AND Scope='Heuristic'", cn))
                        {
                            cn.Open();
                            using (var rd = cmd.ExecuteReader())
                                while (rd.Read()) fresh.Add(Convert.ToString(rd[0]).Trim());
                        }
                        hashes = fresh;
                        nextRefreshUtc = DateTime.UtcNow.AddMinutes(2);
                    }
                    catch
                    {
                        // Khi SQL lỗi: KHÔNG cho phép tiếp tục áp dụng cache ngoại lệ cũ.
                        hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        nextRefreshUtc = DateTime.UtcNow.AddSeconds(15);
                    }
                }
                snapshot = hashes;
            }
            if (snapshot.Count == 0) return false;
            string sha = ScanEngine.ComputeFileSha256(physicalPath);
            return !string.IsNullOrEmpty(sha) && snapshot.Contains(sha);
        }
        public static DataTable List()
        {
            var dt = new DataTable();
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand(@"SELECT TOP (300) ExclusionID, FilePath, SHA256, Scope, Reason,
                ExpiresAtUtc, IsActive FROM dbo.ScanExclusions ORDER BY ExclusionID DESC", cn))
            using (var da = new SqlDataAdapter(cmd)) da.Fill(dt);
            return dt;
        }
        public static void AddHeuristicExclusion(string path, string reason, int days)
        {
            if (days < 1 || days > 90) throw new ArgumentOutOfRangeException("days");
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Cần nhập lý do ngoại lệ.");
            if (!File.Exists(path)) throw new FileNotFoundException("Không tìm thấy tệp đã chọn.");
            string hash = ScanEngine.ComputeFileSha256(path);
            if (string.IsNullOrWhiteSpace(hash)) throw new IOException("Không tính được SHA-256 của tệp.");
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand(@"INSERT INTO dbo.ScanExclusions
                (SHA256, FilePath, Scope, Reason, CreatedBy, ExpiresAtUtc, IsActive)
                VALUES(@h,@path,'Heuristic',@reason,@user,@expiry,1)", cn))
            {
                cmd.Parameters.Add("@h", SqlDbType.Char, 64).Value = hash;
                cmd.Parameters.Add("@path", SqlDbType.NVarChar, 1000).Value = Path.GetFullPath(path);
                cmd.Parameters.Add("@reason", SqlDbType.NVarChar, 500).Value = reason.Trim().Substring(0, Math.Min(500, reason.Trim().Length));
                cmd.Parameters.Add("@user", SqlDbType.NVarChar, 128).Value = Environment.UserName;
                cmd.Parameters.Add("@expiry", SqlDbType.DateTime2).Value = DateTime.UtcNow.AddDays(days);
                cn.Open(); cmd.ExecuteNonQuery();
            }
            Invalidate();
        }
        public static void Disable(long id)
        {
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand("UPDATE dbo.ScanExclusions SET IsActive=0 WHERE ExclusionID=@id", cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
                cn.Open(); cmd.ExecuteNonQuery();
            }
            Invalidate();
        }
    }
}
