using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using ScanAndRemoveVirus.Database;

namespace ScanAndRemoveVirus.Services
{
    // Báo cáo HTML đã encode mọi chuỗi lấy từ SQL; mặc định ẩn đường dẫn cá nhân.
    public static class ScanReportExporter
    {
        private static string Encode(object value)
        {
            return WebUtility.HtmlEncode(value == null || value == DBNull.Value ? "" : Convert.ToString(value));
        }
        public static int ExportRecentHtml(string outputFile, bool includePaths)
        {
            if (string.IsNullOrWhiteSpace(outputFile)) throw new ArgumentException("Cần chọn đường dẫn lưu báo cáo.");
            DataTable data = new DataTable();
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand(@"SELECT TOP (10000) ScanID, ScanType, StartedAt, CompletedAt,
                ScanLocation, FilesScanned, ThreatCount, ResultStatus
                FROM dbo.ScanHistory ORDER BY StartedAt DESC", cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                da.Fill(data);
            }
            long totalScanned = 0, totalThreats = 0;
            int suspicious = 0, incomplete = 0;
            foreach (DataRow row in data.Rows)
            {
                if (row["FilesScanned"] != DBNull.Value) totalScanned += Convert.ToInt64(row["FilesScanned"]);
                if (row["ThreatCount"] != DBNull.Value) totalThreats += Convert.ToInt64(row["ThreatCount"]);
                string status = Convert.ToString(row["ResultStatus"]);
                if (string.Equals(status, "Suspicious", StringComparison.OrdinalIgnoreCase)) suspicious++;
                if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase)) incomplete++;
            }
            int pendingHistory = OfflineHistoryQueue.PendingCount;
            int uncheckedItems = LocalScanIssueStore.Recent(2000).Count;
            var html = new StringBuilder(32 * 1024);
            html.Append("<!doctype html><html lang='vi'><head><meta charset='utf-8'>")
                .Append("<meta name='viewport' content='width=device-width, initial-scale=1'>")
                .Append("<title>Báo cáo lịch sử AntivirusWinform</title><style>")
                .Append("body{font-family:\"Segoe UI\",sans-serif;margin:30px;color:#18283e;background:#f5f8fe}")
                .Append("h1{color:#1456ae;font-size:26px}table{width:100%;border-collapse:collapse;background:white}")
                .Append("th,td{border-bottom:1px solid #d8e3f0;padding:12px;text-align:left;overflow-wrap:anywhere}")
                .Append("th{background:#eaf2ff}p{color:#526784}.small{font-size:12px}</style></head><body>")
                .Append("<h1>Báo cáo lịch sử quét AntivirusWinform</h1><p>Thời điểm xuất: ")
                .Append(Encode(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))).Append("</p>")
                .Append("<p>").Append(data.Rows.Count).Append(" phiên quét gần nhất; kết quả không chứng minh mọi tệp đều an toàn.</p>")
                .Append("<p>Tệp đã thử quét: ").Append(totalScanned)
                .Append(" | Phát hiện: ").Append(totalThreats)
                .Append(" | Phiên nghi vấn: ").Append(suspicious)
                .Append(" | Phiên bị hủy/lỗi: ").Append(incomplete)
                .Append(" | Sự kiện chưa kiểm tra gần đây (tối đa 2.000): ").Append(uncheckedItems)
                .Append(" | Lịch sử chưa đồng bộ SQL: ").Append(pendingHistory).Append("</p>")
                .Append("<table><thead><tr><th>Mã</th><th>Chế độ</th><th>Bắt đầu</th><th>Hoàn tất</th>")
                .Append("<th>Phạm vi</th><th>Tệp</th><th>Phát hiện</th><th>Trạng thái</th></tr></thead><tbody>");
            foreach (DataRow r in data.Rows)
            {
                html.Append("<tr><td>").Append(Encode(r["ScanID"])).Append("</td><td>")
                    .Append(Encode(r["ScanType"])).Append("</td><td>")
                    .Append(Encode(r["StartedAt"])).Append("</td><td>")
                    .Append(Encode(r["CompletedAt"])).Append("</td><td>")
                    .Append(includePaths ? Encode(r["ScanLocation"]) : "Đã ẩn")
                    .Append("</td><td>").Append(Encode(r["FilesScanned"]))
                    .Append("</td><td>").Append(Encode(r["ThreatCount"]))
                    .Append("</td><td>").Append(Encode(r["ResultStatus"]))
                    .Append("</td></tr>");
            }
            html.Append("</tbody></table><p class='small'>Nguồn: AntivirusDB. Báo cáo giới hạn 10.000 phiên gần nhất. ")
                .Append("Không chứa API key hoặc nội dung của các tệp được quét.</p></body></html>");
            string full = Path.GetFullPath(outputFile);
            string dir = Path.GetDirectoryName(full);
            if (!Directory.Exists(dir)) throw new DirectoryNotFoundException("Thư mục lưu báo cáo không tồn tại.");
            string tmp = full + ".writing-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllText(tmp, html.ToString(), new UTF8Encoding(true));
                if (File.Exists(full)) File.Replace(tmp, full, null);
                else File.Move(tmp, full);
            }
            finally { try { if (File.Exists(tmp)) File.Delete(tmp); } catch { } }
            return data.Rows.Count;
        }
    }
}
