using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ScanAndRemoveVirus.Services
{
    public sealed class VirusTotalEngine
    {
        public string Name;
        public string Category;
        public string Result;
        public string Version;
        public string Update;
    }

    public class VirusTotalReport
    {
        public bool Found;
        public bool Uploaded;
        public int Malicious;
        public int Suspicious;
        public int Harmless;
        public int Undetected;
        public int TotalEngines;
        public string Error;
        public string AnalysisId;
        public DateTime? AnalysisTime;
        public List<VirusTotalEngine> Engines = new List<VirusTotalEngine>();
        public const int MaliciousThreshold = 2;
        public bool IsMalicious { get { return Malicious >= MaliciousThreshold; } }
        public bool IsSuspicious { get { return Malicious == 1 || Suspicious > 0; } }
        public string Summary()
        {
            if (!string.IsNullOrEmpty(Error)) return "Lỗi: " + Error;
            string tag = Uploaded ? "[đã gửi mẫu] " : "";
            if (!Found) return Uploaded ? "Đã gửi mẫu, đang chờ VirusTotal phân tích. Tra lại hash sau ít phút." : tag + "Chưa có báo cáo trên VirusTotal (không đồng nghĩa an toàn)";
            if (TotalEngines == 0) return tag + "Đã nhận phản hồi, chưa có kết quả phân tích";
            if (Malicious == 0 && Suspicious == 0) return tag + "0/" + TotalEngines + " công cụ báo độc (không bảo đảm tệp an toàn)";
            return tag + Malicious + "/" + TotalEngines + " công cụ báo độc; " + Suspicious + " công cụ nghi ngờ";
        }
    }

    /// <summary>
    /// VirusTotal API v3. Mặc định CHỈ gửi SHA-256; không âm thầm tải tệp lên.
    /// API key lưu bằng Windows DPAPI (CurrentUser), ngoài thư mục mã nguồn.
    /// Không dùng SQL và không thêm/sửa bảng SQL.
    /// </summary>
    public static class VirusTotalClient
    {
        private const string Base = "https://www.virustotal.com/api/v3/";
        private const int MaxCallsPerMinute = 4; // mặc định public API; quota thực tế tùy tài khoản
        private const int MaxUploadsPerSession = 2;
        private const long MaxUploadBytes = 32L * 1024 * 1024;
        private const string ApiKeyEnvironment = "VIRUSTOTAL_API_KEY";
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(85) };
        private static readonly object Gate = new object();
        private static readonly Queue<DateTime> Calls = new Queue<DateTime>();
        private static DateTime BlockedUntil = DateTime.MinValue;
        private static int _uploadsThisSession;
        private static string cachedApiKeyPath;

        static VirusTotalClient() { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; }
        // Giữ thuộc tính cũ tương thích mã giao diện và các chương trình kiểm thử.
        public static string ApiKeyPath
        {
            get
            {
                if (cachedApiKeyPath == null)
                    cachedApiKeyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ScanAndRemoveVirus", "virustotal-api-key.dpapi");
                return cachedApiKeyPath;
            }
        }
        public static bool IsConfigured { get { return !string.IsNullOrEmpty(LoadApiKey()); } }
        public static string LoadApiKey()
        {
            string environmental = Environment.GetEnvironmentVariable(ApiKeyEnvironment);
            if (!string.IsNullOrWhiteSpace(environmental)) return environmental.Trim();
            try
            {
                if (!File.Exists(ApiKeyPath)) return null;
                byte[] protectedBytes = File.ReadAllBytes(ApiKeyPath);
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException) { return null; }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }
        public static void SaveApiKey(string key)
        {
            string clean = (key ?? "").Trim();
            if (!Regex.IsMatch(clean, "^[a-fA-F0-9]{64}$"))
                throw new ArgumentException("API key phải gồm 64 ký tự hex; vui lòng kiểm tra lại.");
            string dir = Path.GetDirectoryName(ApiKeyPath);
            Directory.CreateDirectory(dir);
            byte[] cipher = ProtectedData.Protect(Encoding.UTF8.GetBytes(clean), null, DataProtectionScope.CurrentUser);
            string temp = ApiKeyPath + ".new";
            try
            {
                File.WriteAllBytes(temp, cipher);
                if (File.Exists(ApiKeyPath)) File.Delete(ApiKeyPath);
                File.Move(temp, ApiKeyPath);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        public static void ClearApiKey()
        {
            if (File.Exists(ApiKeyPath)) File.Delete(ApiKeyPath);
        }
        public static void ResetUploadQuota() { Interlocked.Exchange(ref _uploadsThisSession, 0); }

        // Synchronous public signatures preserved; UI must invoke on Task.Run.
        // Khi 404 chỉ trả trạng thái chưa có mẫu; KHÔNG tự gửi nội dung tệp.
        public static VirusTotalReport QueryHashOrUpload(string sha256, string path) { return QueryHash(sha256); }
        public static VirusTotalReport QueryHash(string sha256)
        {
            if (string.IsNullOrWhiteSpace(sha256) || !Regex.IsMatch(sha256.Trim(), "^[a-fA-F0-9]{64}$"))
                return new VirusTotalReport { Error = "SHA-256 phải gồm 64 ký tự hex" };
            string key = LoadApiKey();
            if (string.IsNullOrEmpty(key)) return new VirusTotalReport { Error = "Chưa lưu API key VirusTotal trong Cài đặt" };
            string quotaError;
            if (!TryReserve(out quotaError)) return new VirusTotalReport { Error = quotaError };
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, Base + "files/" + sha256.Trim().ToLowerInvariant()))
                {
                    request.Headers.TryAddWithoutValidation("x-apikey", key);
                    using (var response = Http.SendAsync(request).GetAwaiter().GetResult())
                    {
                        if (response.StatusCode == HttpStatusCode.NotFound) return new VirusTotalReport();
                        if (!response.IsSuccessStatusCode) return ResponseError(response);
                        return ParseReport(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                    }
                }
            }
            catch (Exception ex) { return new VirusTotalReport { Error = FriendlyNetworkError(ex) }; }
        }
        // Kiểm tra key bằng hash SHA-256 của tệp trống. HTTP 404 = key dùng được, mẫu chưa có.
        public static string TestApiKey()
        {
            if (!IsConfigured) return "Chưa cấu hình API key.";
            VirusTotalReport report = QueryHash("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
            return report.Error == null ? "Kết nối VirusTotal API thành công." : report.Error;
        }

        // CHỈ được gọi sau khi người dùng chủ động xác nhận gửi nội dung tệp.
        public static VirusTotalReport UploadAndAnalyze(string path)
        {
            string key = LoadApiKey();
            if (string.IsNullOrWhiteSpace(key)) return new VirusTotalReport { Error = "Chưa cấu hình API key" };
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return new VirusTotalReport { Error = "Không tìm thấy tệp" };
            var fi = new FileInfo(path);
            if (fi.Length == 0 || fi.Length > MaxUploadBytes)
                return new VirusTotalReport { Error = "Tệp trống hoặc vượt giới hạn gửi 32 MB" };
            if (Interlocked.Increment(ref _uploadsThisSession) > MaxUploadsPerSession)
            {
                Interlocked.Decrement(ref _uploadsThisSession);
                return new VirusTotalReport { Error = "Giới hạn 2 lần gửi mẫu trong một phiên" };
            }
            string quotaError;
            if (!TryReserve(out quotaError))
            {
                Interlocked.Decrement(ref _uploadsThisSession);
                return new VirusTotalReport { Error = quotaError };
            }
            try
            {
                // Tệp <=32 MB: gửi thẳng POST /files. /files/upload_url dùng cho tệp >32 MB.
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var form = new MultipartFormDataContent())
                using (var file = new StreamContent(stream))
                using (var request = new HttpRequestMessage(HttpMethod.Post, Base + "files"))
                {
                    form.Add(file, "file", fi.Name);
                    request.Content = form;
                    request.Headers.TryAddWithoutValidation("x-apikey", key);
                    using (var response = Http.SendAsync(request).GetAwaiter().GetResult())
                    {
                        if (!response.IsSuccessStatusCode) return ResponseError(response);
                        var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        string id = StringValue(Dict(AsDict(Dict(ParseJson(json), "data")), "id"));
                        if (string.IsNullOrEmpty(id)) return new VirusTotalReport { Error = "VirusTotal không trả mã phân tích" };
                        return new VirusTotalReport { Uploaded = true, AnalysisId = id };
                    }
                }
            }
            catch (Exception ex) { return new VirusTotalReport { Error = FriendlyNetworkError(ex) }; }
        }

        private static bool TryReserve(out string error)
        {
            lock (Gate)
            {
                DateTime now = DateTime.UtcNow;
                while (Calls.Count > 0 && (now - Calls.Peek()).TotalSeconds >= 60) Calls.Dequeue();
                if (now < BlockedUntil)
                {
                    error = "VirusTotal đang giới hạn truy vấn. Thử lại sau khoảng " + (int)Math.Ceiling((BlockedUntil - now).TotalSeconds) + " giây.";
                    return false;
                }
                if (Calls.Count >= MaxCallsPerMinute)
                {
                    error = "Đã sử dụng 4 yêu cầu/phút (mức giới hạn bảo thủ). Thử lại sau " +
                        (int)Math.Ceiling(60 - (now - Calls.Peek()).TotalSeconds) + " giây.";
                    return false;
                }
                Calls.Enqueue(now); error = null; return true;
            }
        }
        private static VirusTotalReport ResponseError(HttpResponseMessage response)
        {
            int status = (int)response.StatusCode;
            if (status == 429)
            {
                int retry = 60;
                if (response.Headers.RetryAfter != null)
                {
                    if (response.Headers.RetryAfter.Delta.HasValue)
                        retry = (int)Math.Ceiling(response.Headers.RetryAfter.Delta.Value.TotalSeconds);
                    else if (response.Headers.RetryAfter.Date.HasValue)
                        retry = (int)Math.Ceiling((response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow).TotalSeconds);
                }
                retry = Math.Max(1, retry);
                lock (Gate) BlockedUntil = DateTime.UtcNow.AddSeconds(retry);
                return new VirusTotalReport { Error = "HTTP 429: hết hạn mức; thử lại sau " + retry + " giây" };
            }
            if (status == 401) return new VirusTotalReport { Error = "HTTP 401: API key không hợp lệ hoặc đã bị thu hồi" };
            if (status == 403) return new VirusTotalReport { Error = "HTTP 403: tài khoản không có quyền cho yêu cầu này" };
            if (status == 400) return new VirusTotalReport { Error = "HTTP 400: yêu cầu không hợp lệ" };
            return new VirusTotalReport { Error = "VirusTotal trả HTTP " + status };
        }
        private static string FriendlyNetworkError(Exception ex)
        {
            if (ex is TaskCanceledException || ex is OperationCanceledException) return "Hết thời gian kết nối VirusTotal";
            var inner = ex.GetBaseException();
            return "Không truy cập được VirusTotal: " + (inner == null ? ex.Message : inner.Message);
        }
        public static string ExtractJsonStringValue(string json, string field)
        {
            try
            {
                var root = ParseJson(json);
                var value = StringValue(Dict(root, field));
                if (!string.IsNullOrEmpty(value)) return value;
                return StringValue(Dict(AsDict(Dict(root, "data")), field));
            }
            catch (Exception) { return null; }
        }
        private static Dictionary<string, object> ParseJson(string json)
        {
            return new JavaScriptSerializer { MaxJsonLength = 16 * 1024 * 1024, RecursionLimit = 128 }
                .DeserializeObject(json) as Dictionary<string, object>;
        }
        private static object Dict(Dictionary<string, object> src, string key)
        {
            object val;
            return src != null && src.TryGetValue(key, out val) ? val : null;
        }
        private static Dictionary<string, object> AsDict(object val) { return val as Dictionary<string, object>; }
        private static string StringValue(object val) { return val == null ? null : Convert.ToString(val); }
        private static int IntValue(object val)
        {
            int num;
            return int.TryParse(StringValue(val), out num) ? Math.Max(0, num) : 0;
        }
        public static VirusTotalReport ParseReport(string json)
        {
            try
            {
                var root = ParseJson(json);
                var data = AsDict(Dict(root, "data"));
                var attrs = AsDict(Dict(data, "attributes"));
                if (attrs == null) return new VirusTotalReport { Error = "Thiếu thuộc tính dữ liệu trong báo cáo VirusTotal" };
                var stats = AsDict(Dict(attrs, "last_analysis_stats")) ?? AsDict(Dict(attrs, "stats"));
                if (stats == null) return new VirusTotalReport { Error = "Báo cáo chưa có thống kê phân tích" };
                var report = new VirusTotalReport { Found = true };
                report.Malicious = IntValue(Dict(stats, "malicious"));
                report.Suspicious = IntValue(Dict(stats, "suspicious"));
                report.Harmless = IntValue(Dict(stats, "harmless"));
                report.Undetected = IntValue(Dict(stats, "undetected"));
                foreach (var pair in stats) report.TotalEngines += IntValue(pair.Value);
                var engines = AsDict(Dict(attrs, "last_analysis_results")) ?? AsDict(Dict(attrs, "results"));
                if (engines != null)
                {
                    foreach (var entry in engines)
                    {
                        var e = AsDict(entry.Value);
                        if (e == null) continue;
                        report.Engines.Add(new VirusTotalEngine {
                            Name = StringValue(Dict(e, "engine_name")) ?? entry.Key,
                            Category = StringValue(Dict(e, "category")) ?? "unknown",
                            Result = StringValue(Dict(e, "result")),
                            Version = StringValue(Dict(e, "engine_version")),
                            Update = StringValue(Dict(e, "engine_update"))
                        });
                    }
                    report.Engines.Sort((x, y) => {
                        bool a = x.Category == "malicious" || x.Category == "suspicious";
                        bool b = y.Category == "malicious" || y.Category == "suspicious";
                        return a == b ? StringComparer.CurrentCultureIgnoreCase.Compare(x.Name, y.Name) : (a ? -1 : 1);
                    });
                }
                long timestamp;
                if (long.TryParse(StringValue(Dict(attrs, "last_analysis_date")), out timestamp))
                    report.AnalysisTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
                return report;
            }
            catch (Exception) { return new VirusTotalReport { Error = "Không phân tích được phản hồi JSON của VirusTotal" }; }
        }
    }
}
