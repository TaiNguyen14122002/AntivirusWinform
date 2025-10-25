using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;
using ScanAndRemoveVirus.Database;

namespace ScanAndRemoveVirus.Services
{
    public sealed class SecurityCheckResult
    {
        public string Check { get; set; }
        public string Status { get; set; }
        public string Details { get; set; }
        public string Advice { get; set; }
    }
    // Chỉ đọc trạng thái Windows, KHÔNG thay đổi Firewall/AV/Registry/Update.
    public static class SecurityHealthCheckService
    {
        public static Task<List<SecurityCheckResult>> RunAsync()
        {
            return Task.Run(() =>
            {
                var results = new List<SecurityCheckResult>();
                RunSafe(results, "Windows Firewall", CheckFirewall);
                RunSafe(results, "Windows Security Antivirus", CheckAntivirus);
                RunSafe(results, "Windows Update", CheckWindowsUpdate);
                RunSafe(results, "Ứng dụng tự khởi động", CheckStartup);
                RunSafe(results, "Mã hóa ổ đĩa", CheckEncryption);
                return results;
            });
        }
        private static void RunSafe(List<SecurityCheckResult> results, string title, Func<SecurityCheckResult> check)
        {
            try { results.Add(check()); }
            catch (Exception ex) { results.Add(new SecurityCheckResult { Check = title,
                Status = "Không xác định", Details = ex.GetType().Name + ": " + ex.Message,
                Advice = "Kiểm tra quyền truy cập hoặc dịch vụ Windows có liên quan." }); }
        }
        private static SecurityCheckResult CheckFirewall()
        {
            Type type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            if (type == null) throw new NotSupportedException("Windows Firewall COM không khả dụng.");
            dynamic policy = Activator.CreateInstance(type);
            int active = (int)policy.CurrentProfileTypes;
            bool protectedAll = true;
            var text = new List<string>();
            foreach (int flag in new[] { 1, 2, 4 })
            {
                if ((active & flag) == 0) continue;
                bool enabled = (bool)policy.FirewallEnabled[flag];
                text.Add((flag == 1 ? "Domain" : flag == 2 ? "Private" : "Public") + ": " + (enabled ? "Bật" : "Tắt"));
                if (!enabled) protectedAll = false;
            }
            return new SecurityCheckResult { Check = "Windows Firewall", Status = protectedAll ? "Đạt" : "Cần chú ý",
                Details = string.Join("; ", text), Advice = "Kiểm tra Windows Defender Firewall trong Control Panel." };
        }
        private static SecurityCheckResult CheckAntivirus()
        {
            var products = new List<string>();
            using (var searcher = new ManagementObjectSearcher(@"\\.\root\SecurityCenter2", "SELECT displayName,productState FROM AntiVirusProduct"))
            using (var items = searcher.Get())
            {
                foreach (ManagementObject o in items)
                {
                    products.Add(Convert.ToString(o["displayName"]));
                    o.Dispose();
                }
            }
            return new SecurityCheckResult { Check = "Windows Security Antivirus",
                Status = products.Count > 0 ? "Có sản phẩm" : "Không xác định",
                Details = products.Count == 0 ? "Windows không trả về thông tin sản phẩm." : string.Join(", ", products),
                Advice = "Danh sách sản phẩm không chứng minh bảo vệ đang bật; mở Windows Security để xác minh." };
        }
        private static SecurityCheckResult CheckWindowsUpdate()
        {
            string state = null;
            using (var searcher = new ManagementObjectSearcher("SELECT State, StartMode FROM Win32_Service WHERE Name='wuauserv'"))
            using (var items = searcher.Get())
            {
                foreach (ManagementObject o in items)
                {
                    state = Convert.ToString(o["State"]) + ", Khởi động: " + Convert.ToString(o["StartMode"]);
                    o.Dispose();
                }
            }
            return new SecurityCheckResult { Check = "Windows Update", Status = state == null ? "Không xác định" : "Thông tin",
                Details = state ?? "Không đọc được dịch vụ Windows Update.",
                Advice = "Trạng thái dịch vụ không cho biết máy có thiếu bản vá; hãy mở Windows Update." };
        }
        private static SecurityCheckResult CheckStartup()
        {
            int count = 0;
            foreach (var pair in new[] {
                new { Root = Registry.CurrentUser, Path = @"Software\Microsoft\Windows\CurrentVersion\Run" },
                new { Root = Registry.LocalMachine, Path = @"Software\Microsoft\Windows\CurrentVersion\Run" } })
            {
                using (var key = pair.Root.OpenSubKey(pair.Path, false))
                    if (key != null) count += key.GetValueNames().Length;
            }
            return new SecurityCheckResult { Check = "Ứng dụng tự khởi động", Status = "Thông tin",
                Details = "Đã đọc " + count + " mục trong Registry Run (không kết luận chúng là mã độc).",
                Advice = "Xem Startup Apps để kiểm tra ứng dụng tự khởi động." };
        }
        private static SecurityCheckResult CheckEncryption()
        {
            var volumes = new List<string>();
            bool unprotected = false, unknown = false;
            using (var searcher = new ManagementObjectSearcher(@"\\.\root\CIMV2\Security\MicrosoftVolumeEncryption",
                "SELECT DriveLetter, ProtectionStatus FROM Win32_EncryptableVolume"))
            using (var items = searcher.Get())
            {
                foreach (ManagementObject o in items)
                {
                    string letter = Convert.ToString(o["DriveLetter"]);
                    int status = o["ProtectionStatus"] == null ? -1 : Convert.ToInt32(o["ProtectionStatus"]);
                    string meaning = status == 1 ? "Bật" : status == 0 ? "Tắt" : "Không xác định";
                    if (status == 0) unprotected = true;
                    if (status != 0 && status != 1) unknown = true;
                    volumes.Add((string.IsNullOrWhiteSpace(letter) ? "Ổ không có ký tự" : letter) + ": " + meaning);
                    o.Dispose();
                }
            }
            return new SecurityCheckResult { Check = "Mã hóa ổ đĩa",
                Status = volumes.Count == 0 || unknown ? "Không xác định" : unprotected ? "Cần chú ý" : "Đạt",
                Details = volumes.Count == 0 ? "Không tìm thấy thông tin BitLocker." : string.Join("; ", volumes),
                Advice = "Kiểm tra trực tiếp Manage BitLocker; trạng thái có thể không áp dụng cho ổ USB." };
        }
        public static void SaveResults(IEnumerable<SecurityCheckResult> items)
        {
            try
            {
                using (var cn = DbConnection.Create())
                {
                    cn.Open();
                    foreach (var item in items)
                    using (var cmd = new SqlCommand(@"INSERT INTO dbo.SecurityHealthChecks (CheckType, Status, Details)
                        VALUES(@n,@s,@d)", cn))
                    {
                        cmd.Parameters.Add("@n", SqlDbType.NVarChar, 80).Value = item.Check;
                        cmd.Parameters.Add("@s", SqlDbType.VarChar, 20).Value = item.Status;
                        cmd.Parameters.Add("@d", SqlDbType.NVarChar, 1000).Value = (object)item.Details ?? DBNull.Value;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { /* Có thể xem kết quả chỉ đọc ngay cả khi SQL không khả dụng. */ }
        }
    }
}
