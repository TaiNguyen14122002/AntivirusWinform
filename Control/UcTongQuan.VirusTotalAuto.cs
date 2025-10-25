using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcTongQuan
    {
        // Tra cứu theo yêu cầu khi người dùng mở Chi tiết tệp hoặc tab VirusTotal.
        // Không tự gửi nội dung tệp, không mở hộp thoại và không gọi mạng trên UI thread.
        private sealed class VtCachedResult
        {
            internal DateTime ExpiresUtc;
            internal VirusTotalReport Report;
        }
        private sealed class VtLookupResult
        {
            internal string Hash;
            internal VirusTotalReport Report;
        }

        private readonly object vtCacheLock = new object();
        private readonly Dictionary<string, VtCachedResult> vtHashCache =
            new Dictionary<string, VtCachedResult>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<ThreatRow> vtLookupInFlight = new HashSet<ThreatRow>();
        private readonly HashSet<ThreatRow> vtHashPreparing = new HashSet<ThreatRow>();

        internal bool IsVirusTotalDetailQuerying(ThreatRow row)
        {
            return row != null && (vtLookupInFlight.Contains(row) ||
                (FeatureFlags.VtAutoQuery && VirusTotalClient.IsConfigured &&
                 row.VtReport == null && !IsValidVtSha256(row.HashSha256) && vtHashPreparing.Contains(row)));
        }

        private static bool IsValidVtSha256(string sha)
        {
            return !string.IsNullOrWhiteSpace(sha) && sha.Length == 64 && sha.All(Uri.IsHexDigit);
        }

        private VirusTotalReport GetCachedVtReport(string sha)
        {
            lock (vtCacheLock)
            {
                VtCachedResult value;
                if (vtHashCache.TryGetValue(sha, out value))
                {
                    if (DateTime.UtcNow < value.ExpiresUtc) return value.Report;
                    vtHashCache.Remove(sha);
                }
                return null;
            }
        }

        private void RememberVtReport(string sha, VirusTotalReport report)
        {
            if (report == null || report.Error != null) return;
            lock (vtCacheLock)
            {
                // Báo cáo đã có: 30 phút. HTTP 404: 3 phút để có thể tra lại mẫu mới.
                vtHashCache[sha] = new VtCachedResult
                {
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(report.Found ? 30 : 3),
                    Report = report
                };
                if (vtHashCache.Count > 150)
                {
                    string oldest = vtHashCache.OrderBy(pair => pair.Value.ExpiresUtc).First().Key;
                    vtHashCache.Remove(oldest);
                }
            }
        }

        /// <summary>
        /// Tự tra SHA-256 của tệp đang xem, có nút tra lại để bỏ cache.
        /// 404 là chưa có báo cáo (KHÔNG kết luận an toàn); 401/403/429/lỗi mạng giữ nguyên báo lỗi.
        /// </summary>
        private async void LookupVirusTotalForCurrentDetail(bool force = false)
        {
            if (IsDisposed || detailRowIndex < 0 || detailRowIndex >= threatRows.Count) return;
            ThreatRow row = threatRows[detailRowIndex];
            if (vtLookupInFlight.Contains(row)) return;
            if (!force && row.VtReport != null) return;
            // Tôn trọng công tắc "Tự động tra cứu SHA-256" ở Quản trị.
            if (!force && !FeatureFlags.VtAutoQuery) return;
            // Chờ lượt hash nền hiện có, tránh đọc trùng tệp lớn.
            if (!force && !IsValidVtSha256(row.HashSha256) && vtHashPreparing.Contains(row)) return;
            // Không hiện hộp thoại cấu hình API khi chỉ xem thông tin tệp.
            if (!VirusTotalClient.IsConfigured) return;

            vtLookupInFlight.Add(row);
            if (force) row.VtReport = null;
            if (detailRowIndex >= 0 && detailRowIndex < threatRows.Count &&
                ReferenceEquals(threatRows[detailRowIndex], row))
            {
                CapNhatVtPanel(row);
                tabDaNap = -1;
                NapTabChiTiet(detailRowIndex, tabChiTietDong == null ? 0 : tabChiTietDong.SelectedIndex);
            }
            string knownHash = row.HashSha256;
            string path = row.Path;
            try
            {
                VtLookupResult result = await Task.Run(() =>
                {
                    string hash = (knownHash ?? "").Trim();
                    if (!IsValidVtSha256(hash))
                    {
                        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                            return new VtLookupResult { Report = new VirusTotalReport {
                                Error = "Không có SHA-256 hợp lệ và tệp không còn ở đường dẫn gốc. " +
                                        "Không thể tra cứu VirusTotal chỉ bằng tên tệp." } };
                        try { hash = ScanEngine.ComputeFileSha256(path); }
                        catch (Exception ex) { return new VtLookupResult {
                            Report = new VirusTotalReport { Error = "Không đọc được SHA-256: " + ex.Message } }; }
                    }
                    if (!IsValidVtSha256(hash)) return new VtLookupResult {
                        Report = new VirusTotalReport { Error = "Không thể tính SHA-256 hợp lệ cho tệp." } };
                    hash = hash.ToLowerInvariant();
                    VirusTotalReport cached = force ? null : GetCachedVtReport(hash);
                    VirusTotalReport report = cached ?? VirusTotalClient.QueryHash(hash);
                    if (cached == null) RememberVtReport(hash, report);
                    return new VtLookupResult { Hash = hash, Report = report };
                });

                if (IsDisposed) return;
                if (IsValidVtSha256(result.Hash)) row.HashSha256 = result.Hash;
                row.VtReport = result.Report ?? new VirusTotalReport { Error = "Không nhận được phản hồi từ VirusTotal." };
                row.VtQueriedAt = DateTime.Now;
                row.VtLink = row.VtReport.Found && IsValidVtSha256(row.HashSha256)
                    ? "https://www.virustotal.com/gui/file/" + row.HashSha256 : null;
                if (detailRowIndex >= 0 && detailRowIndex < threatRows.Count &&
                    ReferenceEquals(threatRows[detailRowIndex], row))
                {
                    CapNhatVtPanel(row);
                    tabDaNap = -1;
                    NapTabChiTiet(detailRowIndex, tabChiTietDong == null ? 0 : tabChiTietDong.SelectedIndex);
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    row.VtReport = new VirusTotalReport { Error = "Tra cứu VirusTotal thất bại: " + ex.Message };
                    row.VtQueriedAt = DateTime.Now;
                    if (detailRowIndex >= 0 && detailRowIndex < threatRows.Count &&
                        ReferenceEquals(threatRows[detailRowIndex], row)) CapNhatVtPanel(row);
                }
            }
            finally { vtLookupInFlight.Remove(row); }
        }
    }
}
