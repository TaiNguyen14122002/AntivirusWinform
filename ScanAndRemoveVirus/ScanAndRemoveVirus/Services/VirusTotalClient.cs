using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ScanAndRemoveVirus.Services
{
    public class VirusTotalReport
    {
        public bool Found;            // mẫu có trong DB VirusTotal
        public bool Uploaded;         // kết quả đến từ LẦN UPLOAD tệp này (chứ không phải mẫu có sẵn)
        public int Malicious;         // số engine đánh dấu độc
        public int TotalEngines;      // tổng engine đã phân tích
        public string Error;          // lỗi mạng / API key / rate limit

        public bool IsMalicious { get { return Malicious >= MaliciousThreshold; } }
        public bool IsSuspicious { get { return Malicious == 1; } }
        public const int MaliciousThreshold = 2; // 1 vendor đơn lẻ dễ false positive

        public string Summary()
        {
            if (Error != null) return "Lỗi: " + Error;
            string tag = Uploaded ? "[đã phân tích mẫu vừa gửi] " : "";
            if (!Found) return tag + "Mẫu chưa có trên VirusTotal (thường là vô hại)";
            if (Malicious == 0)
                return tag + string.Format("AN TOÀN — 0/{0} engine không đánh dấu độc", TotalEngines);
            return tag + string.Format("{0}/{1} engine đánh dấu ĐỘC HẠI {2}",
                Malicious, TotalEngines, IsMalicious ? "" : " (1 vendor — có thể báo nhầm)");
        }
    }

    /// <summary>
    /// Client VirusTotal (kỹ thuật 1 nâng cấp: tra chữ ký/hash trên đám mây).
    /// Mặc định CHỈ gửi hash SHA256; nội dung tệp chỉ upload khi người dùng bật
    /// "Gửi mẫu ẩn danh" (chkSendSamples) trong phần Cài đặt.
    /// Free tier: 4 request/phút, 500/ngày, upload ≤32MB -> chỉ dùng cho tệp người dùng
    /// chủ động tra hoặc heuristic nghi vấn (quota 2 upload/phiên), không tra đại trà.
    /// API key: đặt trong <solution>\AppData\vtapikey.txt (xem DataDir) —
    /// hoặc ngay cạnh .csproj, ưu tiên trước (xem ApiKeyPath)
    /// </summary>
    public static class VirusTotalClient
    {
        private const string BaseUrl = "https://www.virustotal.com/api/v3/files/";
        private const string UploadUrlEndpoint = "https://www.virustotal.com/api/v3/files/upload_url";
        private const string AnalysesUrl = "https://www.virustotal.com/api/v3/analyses/";
        private const long MaxUploadBytes = 32L * 1024 * 1024; // trần free tier /files/upload_url
        private const int MaxUploadsPerSession = 2;
        private const int PollTimeoutSeconds = 100;
        private const int PollIntervalSeconds = 8;
        private static readonly HttpClient Http = new HttpClient();
        private static int _uploadsThisSession;

        static VirusTotalClient()
        {
            Http.Timeout = TimeSpan.FromSeconds(120); // upload tệp cần lâu hơn GET hash
            // .NET Framework mặc định có thể chưa bật TLS 1.2 cho older config
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public static void ResetUploadQuota() { _uploadsThisSession = 0; }

        /// <summary>
        /// Vị trí key: vtapikey.txt NGAY TRONG THƯ MỤC PROJECT (cạnh .csproj — đã có trong
        /// .gitignore nên không bao giờ commit). Cách dò: từ thư mục exe leo lên từng bậc,
        /// ở mỗi bậc kiểm tra cả chính nó lẫn thư mục con "ScanAndRemoveVirus" xem có
        /// ScanAndRemoveVirus.csproj — nhờ vậy exe chạy từ bin\Debug VÀ harness trong Tests\_bin
        /// (anh-em ruột với project) đều resolving về CÙNG một file key. Ngoài repo -> AppData.
        /// </summary>
        private static string cachedApiKeyPath;

        public static string ApiKeyPath
        {
            get
            {
                if (cachedApiKeyPath != null) return cachedApiKeyPath;
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                for (int hop = 0; hop < 8 && dir != null; hop++, dir = dir.Parent)
                {
                    foreach (string cand in new[] { dir.FullName, Path.Combine(dir.FullName, "ScanAndRemoveVirus") })
                        if (File.Exists(Path.Combine(cand, "ScanAndRemoveVirus.csproj")))
                        {
                            cachedApiKeyPath = Path.Combine(cand, "vtapikey.txt");
                            return cachedApiKeyPath;
                        }
                }
                cachedApiKeyPath = DataDir.Resolve("vtapikey.txt");
                return cachedApiKeyPath;
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

        /// <summary>
        /// Chiến lược đầy đủ: tra hash trước; nếu 404 (chưa có mẫu) VÀ người dùng đã bật
        /// "Gửi mẫu ẩn danh" trong Cài đặt thì upload phân tích mới. Mặc định không upload gì.
        /// </summary>
        public static VirusTotalReport QueryHashOrUpload(string sha256, string path)
        {
            if (string.IsNullOrEmpty(sha256))
                return new VirusTotalReport { Error = "Không đọc được tệp để tính SHA256" };
            VirusTotalReport rep = QueryHash(sha256);
            if (rep.Error == null && !rep.Found && AppSettings.Load().SendSamples)
                rep = UploadAndAnalyze(path);
            return rep;
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

        /// <summary>
        /// "Gửi mẫu ẩn danh" được bật: upload tệp (≤32MB) lên VT rồi poll kết quả phân tích mới.
        /// CHẤM DỨT tình trạng "chưa có mẫu" cho zero-day — nhưng nội dung tệp RỜI KHỎI MÁY,
        /// vì vậy bắt buộc quota phiên + chỉ gọi sau khi tra hash ra 404.
        /// </summary>
        public static VirusTotalReport UploadAndAnalyze(string path)
        {
            var report = new VirusTotalReport();
            try
            {
                string key = LoadApiKey();
                if (string.IsNullOrEmpty(key)) { report.Error = "Chưa cấu hình API key VirusTotal"; return report; }
                var fi = new FileInfo(path);
                if (!fi.Exists || fi.Length == 0) { report.Error = "Không tìm thấy tệp để gửi"; return report; }
                if (fi.Length > MaxUploadBytes) { report.Error = "Tệp vượt giới hạn upload miễn phí (32MB)"; return report; }
                if (_uploadsThisSession >= MaxUploadsPerSession)
                { report.Error = "Đã dùng hết quota upload của phiên (2 tệp)"; return report; }

                // 1) Xin URL upload dùng một lần
                string uploadUrl;
                using (var req = new HttpRequestMessage(HttpMethod.Post, UploadUrlEndpoint))
                {
                    req.Headers.Add("x-apikey", key);
                    using (var resp = Http.SendAsync(req).Result)
                    {
                        if (!resp.IsSuccessStatusCode) { report.Error = "upload_url: HTTP " + (int)resp.StatusCode; return report; }
                        uploadUrl = ExtractJsonStringValue(resp.Content.ReadAsStringAsync().Result, "data");
                    }
                }
                if (string.IsNullOrEmpty(uploadUrl)) { report.Error = "upload_url: không đọc được phản hồi"; return report; }

                // 2) STREAM tệp tới URL (multipart) — không ReadAllBytes, tránh nạp 32MB vào RAM
                string analysisId;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                using (var form = new MultipartFormDataContent())
                {
                    form.Add(new StreamContent(fs), "file", fi.Name);
                    using (var resp = Http.PostAsync(uploadUrl, form).Result)
                    {
                        if (!resp.IsSuccessStatusCode) { report.Error = "upload: HTTP " + (int)resp.StatusCode; return report; }
                        analysisId = ExtractJsonStringValue(resp.Content.ReadAsStringAsync().Result, "id");
                    }
                }
                if (string.IsNullOrEmpty(analysisId)) { report.Error = "Không đọc được mã phân tích"; return report; }
                _uploadsThisSession++;
                report.Uploaded = true;

                // 3) Poll analyses/{id} tới khi completed (tối đa ~100s)
                var until = DateTime.Now.AddSeconds(PollTimeoutSeconds);
                while (DateTime.Now < until)
                {
                    Thread.Sleep(PollIntervalSeconds * 1000);
                    try
                    {
                        using (var req = new HttpRequestMessage(HttpMethod.Get, AnalysesUrl + analysisId))
                        {
                            req.Headers.Add("x-apikey", key);
                            using (var resp = Http.SendAsync(req).Result)
                            {
                                if (!resp.IsSuccessStatusCode) continue;
                                string json = resp.Content.ReadAsStringAsync().Result;
                                if (json.IndexOf("\"completed\"", StringComparison.Ordinal) < 0) continue;
                                VirusTotalReport done = ParseReport(json);
                                done.Uploaded = true;
                                return done;
                            }
                        }
                    }
                    catch (Exception) { }
                }
                report.Error = "Quá thời gian chờ — VT vẫn đang phân tích nền, hãy tra lại sau";
                return report;
            }
            catch (Exception ex) { report.Error = ex.Message; return report; }
        }

        // Móc giá trị chuỗi "field":"giá-trị" khỏi JSON (dùng cho data-url và id)
        public static string ExtractJsonStringValue(string json, string field)
        {
            if (string.IsNullOrEmpty(json)) return null;
            Match m = Regex.Match(json, "\"" + Regex.Escape(field) + "\"\\s*:\\s*\"([^\"]+)\"");
            return m.Success ? m.Groups[1].Value : null;
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
