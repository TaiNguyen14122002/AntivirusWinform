/*using System;
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
        /// Vị trí key: vtapikey.txt NGAY TẠI GỐC REPO (cạnh .csproj — commit có chủ đích
        /// để team dùng chung quota free-tier, xem README). Cách dò: từ thư mục exe leo lên
        /// từng bậc tìm ScanAndRemoveVirus.csproj — exe trong bin\Debug lẫn harness csc
        /// ở bất kỳ đâu trong repo đều resolving về CÙNG một file key. Ngoài repo -> AppData.
        /// </summary>
        private static string cachedApiKeyPath;

        public static string ApiKeyPath
        {
            get
            {
                if (cachedApiKeyPath != null) return cachedApiKeyPath;
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                for (int hop = 0; hop < 8 && dir != null; hop++, dir = dir.Parent)
                    if (File.Exists(Path.Combine(dir.FullName, "ScanAndRemoveVirus.csproj")))
                    {
                        cachedApiKeyPath = Path.Combine(dir.FullName, "vtapikey.txt");
                        return cachedApiKeyPath;
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
*/
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Kết quả tra cứu/phân tích tệp trên VirusTotal.
    ///
    /// Đây chỉ là model dữ liệu tạm thời dùng cho giao diện.
    /// Không lưu trực tiếp vào SQL Server vì CSDL hiện tại
    /// không có bảng VirusTotalReports.
    /// </summary>
    public class VirusTotalReport
    {
        /// <summary>
        /// true nếu hash/tệp đã có kết quả trên VirusTotal.
        /// </summary>
        public bool Found;

        /// <summary>
        /// true nếu kết quả đến từ tệp vừa được upload.
        /// </summary>
        public bool Uploaded;

        /// <summary>
        /// Số engine đánh dấu malicious.
        /// </summary>
        public int Malicious;

        /// <summary>
        /// Tổng số engine tham gia phân tích.
        /// </summary>
        public int TotalEngines;

        /// <summary>
        /// Thông báo lỗi mạng/API/quota.
        /// </summary>
        public string Error;

        /// <summary>
        /// Từ 2 engine trở lên đánh dấu malicious.
        /// </summary>
        public bool IsMalicious
        {
            get
            {
                return Malicious >= MaliciousThreshold;
            }
        }

        /// <summary>
        /// Chỉ có 1 engine đánh dấu malicious.
        /// </summary>
        public bool IsSuspicious
        {
            get
            {
                return Malicious == 1;
            }
        }

        /*
         * Một vendor đơn lẻ có khả năng là false positive,
         * nên chỉ coi là malicious khi >= 2.
         */
        public const int MaliciousThreshold = 2;

        /// <summary>
        /// Chuỗi tóm tắt để hiển thị trên UI.
        /// </summary>
        public string Summary()
        {
            if (!string.IsNullOrEmpty(Error))
            {
                return "Lỗi: " + Error;
            }

            string tag =
                Uploaded
                    ? "[đã phân tích mẫu vừa gửi] "
                    : "";

            if (!Found)
            {
                return tag +
                       "Mẫu chưa có trên VirusTotal";
            }

            if (Malicious == 0)
            {
                return tag +
                       string.Format(
                           "AN TOÀN — 0/{0} engine không đánh dấu độc",
                           TotalEngines);
            }

            if (Malicious == 1)
            {
                return tag +
                       string.Format(
                           "{0}/{1} engine đánh dấu ĐỘC HẠI " +
                           "(1 vendor — có thể báo nhầm)",
                           Malicious,
                           TotalEngines);
            }

            return tag +
                   string.Format(
                       "{0}/{1} engine đánh dấu ĐỘC HẠI",
                       Malicious,
                       TotalEngines);
        }
    }

    /// <summary>
    /// Client giao tiếp với VirusTotal API.
    ///
    /// Luồng xử lý:
    ///
    /// File
    ///   ↓
    /// SHA256
    ///   ↓
    /// QueryHash()
    ///   ↓
    /// VirusTotal
    ///
    /// Nếu hash chưa tồn tại và người dùng bật:
    ///
    /// Settings.SubmitSamples
    ///      ↓
    /// AppSettings.SendSamples
    ///      ↓
    /// UploadAndAnalyze()
    ///
    /// API key vẫn lưu local trong vtapikey.txt.
    ///
    /// Class này KHÔNG truy cập SQL Server trực tiếp.
    /// </summary>
    public static class VirusTotalClient
    {
        // =========================================================
        // API ENDPOINTS
        // =========================================================

        private const string BaseUrl =
            "https://www.virustotal.com/api/v3/files/";

        private const string UploadUrlEndpoint =
            "https://www.virustotal.com/api/v3/files/upload_url";

        private const string AnalysesUrl =
            "https://www.virustotal.com/api/v3/analyses/";

        // =========================================================
        // LIMITS
        // =========================================================

        /*
         * Giữ giới hạn 32 MB giống logic hiện tại.
         */
        private const long MaxUploadBytes =
            32L * 1024L * 1024L;

        /*
         * Chặn upload quá nhiều trong một phiên chạy app.
         */
        private const int MaxUploadsPerSession = 2;

        private const int PollTimeoutSeconds = 100;

        private const int PollIntervalSeconds = 8;

        // =========================================================
        // HTTP
        // =========================================================

        private static readonly HttpClient Http =
            new HttpClient();

        private static int _uploadsThisSession;

        // =========================================================
        // STATIC CONSTRUCTOR
        // =========================================================

        static VirusTotalClient()
        {
            /*
             * Upload có thể mất nhiều thời gian hơn
             * truy vấn hash thông thường.
             */
            Http.Timeout =
                TimeSpan.FromSeconds(120);

            /*
             * Hỗ trợ .NET Framework cũ chưa mặc định bật TLS 1.2.
             */
            ServicePointManager.SecurityProtocol |=
                SecurityProtocolType.Tls12;
        }

        // =========================================================
        // UPLOAD QUOTA
        // =========================================================

        public static void ResetUploadQuota()
        {
            Interlocked.Exchange(
                ref _uploadsThisSession,
                0);
        }

        // =========================================================
        // API KEY
        // =========================================================

        private static string cachedApiKeyPath;

        /// <summary>
        /// API key KHÔNG đưa vào SQL Server.
        ///
        /// Ưu tiên tìm vtapikey.txt tại gốc project.
        /// Nếu chạy ngoài project thì dùng DataDir.
        /// </summary>
        public static string ApiKeyPath
        {
            get
            {
                if (!string.IsNullOrEmpty(
                    cachedApiKeyPath))
                {
                    return cachedApiKeyPath;
                }

                try
                {
                    DirectoryInfo dir =
                        new DirectoryInfo(
                            AppDomain.CurrentDomain.BaseDirectory);

                    for (int hop = 0;
                         hop < 8 && dir != null;
                         hop++, dir = dir.Parent)
                    {
                        string project =
                            Path.Combine(
                                dir.FullName,
                                "ScanAndRemoveVirus.csproj");

                        if (File.Exists(project))
                        {
                            cachedApiKeyPath =
                                Path.Combine(
                                    dir.FullName,
                                    "vtapikey.txt");

                            return cachedApiKeyPath;
                        }
                    }
                }
                catch (Exception)
                {
                }

                cachedApiKeyPath =
                    DataDir.Resolve(
                        "vtapikey.txt");

                return cachedApiKeyPath;
            }
        }

        /// <summary>
        /// true nếu đã có API key.
        /// </summary>
        public static bool IsConfigured
        {
            get
            {
                return !string.IsNullOrEmpty(
                    LoadApiKey());
            }
        }

        /// <summary>
        /// Đọc API key từ file local.
        /// </summary>
        public static string LoadApiKey()
        {
            try
            {
                if (!File.Exists(ApiKeyPath))
                {
                    return null;
                }

                string key =
                    File.ReadAllText(
                        ApiKeyPath);

                if (string.IsNullOrWhiteSpace(key))
                {
                    return null;
                }

                return key.Trim();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Lưu API key local.
        ///
        /// Không lưu API key vào dbo.Settings vì schema hiện tại
        /// không có cột VirusTotalApiKey.
        /// </summary>
        public static void SaveApiKey(string key)
        {
            try
            {
                string directory =
                    Path.GetDirectoryName(
                        ApiKeyPath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(
                        directory);
                }

                File.WriteAllText(
                    ApiKeyPath,
                    (key ?? "").Trim(),
                    new UTF8Encoding(false));
            }
            catch (Exception)
            {
                /*
                 * Giữ tương thích với cách hoạt động cũ:
                 * lỗi lưu key không làm ứng dụng crash.
                 */
            }
        }

        // =========================================================
        // QUERY HASH OR UPLOAD
        // =========================================================

        /// <summary>
        /// Tra SHA256 trước.
        ///
        /// Nếu VirusTotal chưa có mẫu và người dùng bật
        /// Settings.SubmitSamples thì mới upload file.
        /// </summary>
        public static VirusTotalReport QueryHashOrUpload(
            string sha256,
            string path)
        {
            if (string.IsNullOrWhiteSpace(sha256))
            {
                return new VirusTotalReport
                {
                    Error =
                        "Không đọc được tệp để tính SHA256"
                };
            }

            VirusTotalReport report =
                QueryHash(sha256);

            /*
             * AppSettings.Load().SendSamples
             *
             * hiện được ánh xạ tới:
             *
             * dbo.Settings.SubmitSamples
             *
             * VirusTotalClient không cần truy cập SQL trực tiếp.
             */
            if (report.Error == null &&
                !report.Found)
            {
                try
                {
                    SettingsFlags settings =
                        AppSettings.Load();

                    if (settings != null &&
                        settings.SendSamples)
                    {
                        report =
                            UploadAndAnalyze(path);
                    }
                }
                catch (Exception)
                {
                    /*
                     * Nếu không đọc được Settings,
                     * mặc định KHÔNG upload file.
                     *
                     * Đây là lựa chọn an toàn hơn.
                     */
                }
            }

            return report;
        }

        // =========================================================
        // QUERY HASH
        // =========================================================

        public static VirusTotalReport QueryHash(
            string sha256)
        {
            var report =
                new VirusTotalReport();

            try
            {
                if (string.IsNullOrWhiteSpace(sha256))
                {
                    report.Error =
                        "SHA256 không hợp lệ";

                    return report;
                }

                string key =
                    LoadApiKey();

                if (string.IsNullOrEmpty(key))
                {
                    report.Error =
                        "Chưa cấu hình API key VirusTotal";

                    return report;
                }

                string hash =
                    sha256.Trim().ToLowerInvariant();

                using (var request =
                    new HttpRequestMessage(
                        HttpMethod.Get,
                        BaseUrl + hash))
                {
                    request.Headers.Add(
                        "x-apikey",
                        key);

                    using (HttpResponseMessage response =
                        Http.SendAsync(request).Result)
                    {
                        // Hash chưa tồn tại trên VirusTotal.
                        if (response.StatusCode ==
                            HttpStatusCode.NotFound)
                        {
                            report.Found = false;

                            return report;
                        }

                        // Rate limit.
                        if ((int)response.StatusCode == 429)
                        {
                            report.Error =
                                "Vượt giới hạn tốc độ của VirusTotal. " +
                                "Vui lòng thử lại sau.";

                            return report;
                        }

                        if (!response.IsSuccessStatusCode)
                        {
                            report.Error =
                                "API trả lỗi HTTP " +
                                (int)response.StatusCode;

                            return report;
                        }

                        string json =
                            response.Content
                                .ReadAsStringAsync()
                                .Result;

                        return ParseReport(json);
                    }
                }
            }
            catch (AggregateException ex)
            {
                Exception inner =
                    ex.GetBaseException();

                report.Error =
                    inner != null
                        ? inner.Message
                        : ex.Message;

                return report;
            }
            catch (Exception ex)
            {
                report.Error =
                    ex.Message;

                return report;
            }
        }

        // =========================================================
        // UPLOAD AND ANALYZE
        // =========================================================

        /// <summary>
        /// Upload file lên VirusTotal và chờ kết quả phân tích.
        ///
        /// Chỉ nên được gọi khi người dùng đã bật
        /// SubmitSamples.
        /// </summary>
        public static VirusTotalReport UploadAndAnalyze(
            string path)
        {
            var report =
                new VirusTotalReport();

            try
            {
                string key =
                    LoadApiKey();

                if (string.IsNullOrEmpty(key))
                {
                    report.Error =
                        "Chưa cấu hình API key VirusTotal";

                    return report;
                }

                if (string.IsNullOrWhiteSpace(path))
                {
                    report.Error =
                        "Không tìm thấy tệp để gửi";

                    return report;
                }

                FileInfo fileInfo;

                try
                {
                    fileInfo =
                        new FileInfo(path);
                }
                catch (Exception)
                {
                    report.Error =
                        "Đường dẫn tệp không hợp lệ";

                    return report;
                }

                if (!fileInfo.Exists ||
                    fileInfo.Length == 0)
                {
                    report.Error =
                        "Không tìm thấy tệp để gửi";

                    return report;
                }

                if (fileInfo.Length >
                    MaxUploadBytes)
                {
                    report.Error =
                        "Tệp vượt giới hạn upload cho phép";

                    return report;
                }

                if (Volatile.Read(
                        ref _uploadsThisSession) >=
                    MaxUploadsPerSession)
                {
                    report.Error =
                        "Đã dùng hết quota upload của phiên (" +
                        MaxUploadsPerSession +
                        " tệp)";

                    return report;
                }

                // =============================================
                // 1. XIN UPLOAD URL
                // =============================================

                string uploadUrl;

                using (var request =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        UploadUrlEndpoint))
                {
                    request.Headers.Add(
                        "x-apikey",
                        key);

                    using (HttpResponseMessage response =
                        Http.SendAsync(request).Result)
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            report.Error =
                                "upload_url: HTTP " +
                                (int)response.StatusCode;

                            return report;
                        }

                        string json =
                            response.Content
                                .ReadAsStringAsync()
                                .Result;

                        uploadUrl =
                            ExtractJsonStringValue(
                                json,
                                "data");
                    }
                }

                if (string.IsNullOrEmpty(uploadUrl))
                {
                    report.Error =
                        "upload_url: không đọc được phản hồi";

                    return report;
                }

                // =============================================
                // 2. UPLOAD FILE
                // =============================================

                string analysisId;

                using (var fileStream =
                    new FileStream(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite |
                        FileShare.Delete))
                using (var form =
                    new MultipartFormDataContent())
                using (var fileContent =
                    new StreamContent(fileStream))
                {
                    form.Add(
                        fileContent,
                        "file",
                        fileInfo.Name);

                    using (HttpResponseMessage response =
                        Http.PostAsync(
                            uploadUrl,
                            form).Result)
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            report.Error =
                                "upload: HTTP " +
                                (int)response.StatusCode;

                            return report;
                        }

                        string json =
                            response.Content
                                .ReadAsStringAsync()
                                .Result;

                        analysisId =
                            ExtractJsonStringValue(
                                json,
                                "id");
                    }
                }

                if (string.IsNullOrEmpty(analysisId))
                {
                    report.Error =
                        "Không đọc được mã phân tích";

                    return report;
                }

                Interlocked.Increment(
                    ref _uploadsThisSession);

                report.Uploaded = true;

                // =============================================
                // 3. POLL ANALYSIS
                // =============================================

                DateTime until =
                    DateTime.UtcNow.AddSeconds(
                        PollTimeoutSeconds);

                while (DateTime.UtcNow < until)
                {
                    Thread.Sleep(
                        PollIntervalSeconds * 1000);

                    try
                    {
                        using (var request =
                            new HttpRequestMessage(
                                HttpMethod.Get,
                                AnalysesUrl +
                                analysisId))
                        {
                            request.Headers.Add(
                                "x-apikey",
                                key);

                            using (
                                HttpResponseMessage response =
                                    Http.SendAsync(request).Result)
                            {
                                if (!response.IsSuccessStatusCode)
                                {
                                    continue;
                                }

                                string json =
                                    response.Content
                                        .ReadAsStringAsync()
                                        .Result;

                                /*
                                 * Chưa completed thì tiếp tục chờ.
                                 */
                                if (json.IndexOf(
                                        "\"completed\"",
                                        StringComparison.Ordinal) < 0)
                                {
                                    continue;
                                }

                                VirusTotalReport completed =
                                    ParseReport(json);

                                completed.Uploaded = true;

                                return completed;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        /*
                         * Poll thất bại tạm thời:
                         * tiếp tục thử cho tới timeout.
                         */
                    }
                }

                report.Error =
                    "Quá thời gian chờ — VirusTotal vẫn đang " +
                    "phân tích nền, hãy tra lại sau.";

                return report;
            }
            catch (AggregateException ex)
            {
                Exception inner =
                    ex.GetBaseException();

                report.Error =
                    inner != null
                        ? inner.Message
                        : ex.Message;

                return report;
            }
            catch (Exception ex)
            {
                report.Error =
                    ex.Message;

                return report;
            }
        }

        // =========================================================
        // JSON HELPERS
        // =========================================================

        /// <summary>
        /// Lấy giá trị string đơn giản:
        ///
        /// "field": "value"
        ///
        /// Giữ nguyên cách parse hiện tại để không cần
        /// thêm thư viện JSON ngoài.
        /// </summary>
        public static string ExtractJsonStringValue(
            string json,
            string field)
        {
            if (string.IsNullOrEmpty(json) ||
                string.IsNullOrEmpty(field))
            {
                return null;
            }

            Match match =
                Regex.Match(
                    json,
                    "\"" +
                    Regex.Escape(field) +
                    "\"\\s*:\\s*\"([^\"]+)\"");

            return match.Success
                ? match.Groups[1].Value
                : null;
        }

        /// <summary>
        /// Parse thống kê VirusTotal.
        ///
        /// Không cần SQL Server.
        /// </summary>
        public static VirusTotalReport ParseReport(
            string json)
        {
            var report =
                new VirusTotalReport();

            try
            {
                if (string.IsNullOrEmpty(json))
                {
                    report.Error =
                        "Phản hồi không hợp lệ";

                    return report;
                }

                int start =
                    json.IndexOf(
                        "\"last_analysis_stats\"",
                        StringComparison.Ordinal);

                if (start < 0)
                {
                    /*
                     * Response của analyses/{id} có thể dùng
                     * stats thay cho last_analysis_stats.
                     */
                    start =
                        json.IndexOf(
                            "\"stats\"",
                            StringComparison.Ordinal);
                }

                if (start < 0)
                {
                    report.Error =
                        "Phản hồi không hợp lệ";

                    return report;
                }

                int open =
                    json.IndexOf(
                        '{',
                        start);

                if (open < 0)
                {
                    report.Error =
                        "Phản hồi không hợp lệ";

                    return report;
                }

                int close =
                    FindMatchingBrace(
                        json,
                        open);

                if (close < 0)
                {
                    report.Error =
                        "Phản hồi không hợp lệ";

                    return report;
                }

                string stats =
                    json.Substring(
                        open,
                        close - open + 1);

                foreach (
                    Match match
                    in Regex.Matches(
                        stats,
                        "\"([a-z_]+)\"\\s*:\\s*(\\d+)"))
                {
                    int count;

                    if (!int.TryParse(
                            match.Groups[2].Value,
                            out count))
                    {
                        continue;
                    }

                    string key =
                        match.Groups[1].Value;

                    /*
                     * Tổng số engine:
                     *
                     * malicious
                     * suspicious
                     * undetected
                     * harmless
                     * timeout
                     * failure
                     * type-unsupported
                     *
                     * Tất cả số đếm trong stats được cộng lại.
                     */
                    report.TotalEngines +=
                        count;

                    if (string.Equals(
                        key,
                        "malicious",
                        StringComparison.Ordinal))
                    {
                        report.Malicious =
                            count;
                    }
                }

                report.Found = true;

                return report;
            }
            catch (Exception ex)
            {
                report.Error =
                    ex.Message;

                return report;
            }
        }

        /// <summary>
        /// Tìm dấu } tương ứng với dấu { ban đầu.
        /// </summary>
        private static int FindMatchingBrace(
            string text,
            int openIndex)
        {
            if (string.IsNullOrEmpty(text) ||
                openIndex < 0 ||
                openIndex >= text.Length ||
                text[openIndex] != '{')
            {
                return -1;
            }

            int depth = 0;

            bool insideString = false;

            bool escaped = false;

            for (int i = openIndex;
                 i < text.Length;
                 i++)
            {
                char c =
                    text[i];

                if (insideString)
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (c == '"')
                    {
                        insideString = false;
                    }

                    continue;
                }

                if (c == '"')
                {
                    insideString = true;
                    continue;
                }

                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;

                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
    }
}