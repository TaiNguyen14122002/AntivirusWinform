using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace ScanAndRemoveVirus.Services
{
    public class VirusTotalReport
    {
        public bool Found;            // mẫu có trong DB VirusTotal
        public int Malicious;         // số engine đánh dấu độc
        public int TotalEngines;      // tổng engine đã phân tích
        public string Error;          // lỗi mạng / API key / rate limit

        public bool IsMalicious { get { return Malicious >= MaliciousThreshold; } }
        public bool IsSuspicious { get { return Malicious == 1; } }
        public const int MaliciousThreshold = 2; // 1 vendor đơn lẻ dễ false positive

        public string Summary()
        {
            if (Error != null) return "Lỗi: " + Error;
            if (!Found) return "Mẫu chưa có trên VirusTotal (thường là vô hại)";
            if (Malicious == 0)
                return string.Format("AN TOÀN — {0}/{0} engine không đánh dấu độc", TotalEngines);
            return string.Format("{0}/{1} engine đánh dấu ĐỘC HẠI {2}",
                Malicious, TotalEngines, IsMalicious ? "" : " (1 vendor — có thể báo nhầm)");
        }
    }

    /// <summary>
    /// Client VirusTotal (kỹ thuật 1 nâng cấp: tra chữ ký/hASH trên đám mây).
    /// Chỉ gửi hash SHA256 — không bao giờ upload nội dung tệp của người dùng.
    /// Free tier: 4 request/phút, 500/ngày -> CHỈ dùng cho tệp người dùng chủ động tra
    /// hoặc tệp heuristic nghi vấn, không tra đại trà trong quét_full.
    /// API key: đặt trong %AppData%\ScanAndRemoveVirus\vtapikey.txt
    /// </summary>
    public static class VirusTotalClient
    {
        private const string BaseUrl = "https://www.virustotal.com/api/v3/files/";
        private static readonly HttpClient Http = new HttpClient();

        static VirusTotalClient()
        {
            Http.Timeout = TimeSpan.FromSeconds(30);
            // .NET Framework mặc định có thể chưa bật TLS 1.2 cho older config
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public static string ApiKeyPath
        {
            get
            {
                return System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ScanAndRemoveVirus", "vtapikey.txt");
            }
        }

        public static bool IsConfigured { get { return !string.IsNullOrEmpty(LoadApiKey()); } }

        public static string LoadApiKey()
        {
            try
            {
                return System.IO.File.Exists(ApiKeyPath)
                    ? System.IO.File.ReadAllText(ApiKeyPath).Trim() : null;
            }
            catch (Exception) { return null; }
        }

        public static void SaveApiKey(string key)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ApiKeyPath));
            System.IO.File.WriteAllText(ApiKeyPath, (key ?? "").Trim(), new UTF8Encoding(false));
        }

        public static VirusTotalReport QueryHash(string sha256)
        {
            var report = new VirusTotalReport();
            try
            {
                string key = LoadApiKey();
                if (string.IsNullOrEmpty(key)) { report.Error = "Chưa cấu hình API key VirusTotal"; return report; }

                using (var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl + sha256))
                {
                    request.Headers.Add("x-apikey", key);
                    using (var resp = Http.SendAsync(request).Result)
                    {
                        if (resp.StatusCode == HttpStatusCode.NotFound) { report.Found = false; return report; }
                        if ((int)resp.StatusCode == 429)
                        {
                            report.Error = "Vượt giới hạn tốc độ (miễn phí: 4 request/phút). Đợi 1 phút rồi thử lại.";
                            return report;
                        }
                        if (!resp.IsSuccessStatusCode)
                        {
                            report.Error = "API trả lỗi HTTP " + (int)resp.StatusCode;
                            return report;
                        }
                        return ParseReport(resp.Content.ReadAsStringAsync().Result);
                    }
                }
            }
            catch (Exception ex)
            {
                report.Error = ex.Message;
                return report;
            }
        }

        // Công khai để test offline không cần mạng
        public static VirusTotalReport ParseReport(string json)
        {
            var report = new VirusTotalReport();
            try
            {
                int start = json.IndexOf("\"last_analysis_stats\"", StringComparison.Ordinal);
                if (start < 0) { report.Error = "Phản hồi không hợp lệ"; return report; }
                int open = json.IndexOf('{', start + 20);
                if (open < 0) { report.Error = "Phản hồi không hợp lệ"; return report; }
                int close = json.IndexOf('}', open);
                if (close < 0) { report.Error = "Phản hồi không hợp lệ"; return report; }
                string stats = json.Substring(open, close - open);

                foreach (Match m in Regex.Matches(stats, "\"([a-z_]+)\"\\s*:\\s*(\\d+)"))
                {
                    int n;
                    if (!int.TryParse(m.Groups[2].Value, out n)) continue;
                    string k = m.Groups[1].Value;
                    report.TotalEngines += n;
                    if (k == "malicious") report.Malicious = n;
                }
                report.Found = true;
                return report;
            }
            catch (Exception ex)
            {
                report.Error = ex.Message;
                return report;
            }
        }
    }
}
