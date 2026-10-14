using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    // Chỉ được dùng bởi --phac-thao và renderer test; không ghi SQL, không gọi API,
    // không gửi tệp, không sửa các tệp/thư mục trên máy người dùng.
    internal static class Sketch20Samples
    {
        internal static Sketch20Model Create(int screen)
        {
            var m = new Sketch20Model { Sample = true, Version = typeof(Sketch20Samples).Assembly.GetName().Version.ToString(3), Running = screen != 20, Scanning = screen == 18,
                ScannedFiles = screen == 18 ? 1310 : 12486, Progress = 64, ThreatCount = screen == 19 ? 3 : 0,
                LastDate = "04/10/2026", LastTime = "09:42", LastType = "Quét nhanh", Signature = "04/10/2026 · 09:10",
                DataFolder = @"C:\Users\User\AppData\Local\Antivirus", ScanPath = @"C:\Users\User\Downloads",
                VtScore = "18/72", VtDetections = 18, VtEngineCount = 72,
                VtSummary = "18 công cụ đánh dấu tệp đáng ngờ.", VtTime = "04/10/2026 · 09:27" };
            foreach (string key in new[] { "rt", "restore", "usb", "download", "behavior", "startup", "aq", "autostart", "update", "alerts" }) m.Flags[key] = true;
            m.Flags["rt"] = m.Running; m.Flags["vtq"] = m.Flags["samples"] = false;
            string[] names = { "invoice_update.exe", "startup_helper.exe", "eicar-test.txt", "setup_script.ps1" };
            string[] paths = { @"C:\Users\User\Downloads", @"C:\Users\User\AppData\Roaming", @"C:\Users\User\Desktop\Test", @"C:\Users\User\Downloads" };
            string[] reasons = { "HEUR: Suspicious.PE", "Signature: Trojan.Sample", "EICAR-Test-File", "HEUR: Encoded.Script" };
            string[] dates = { "04/10/2026", "03/10/2026", "03/10/2026", "02/10/2026" }, times = { "09:25", "16:08", "14:30", "10:12" }, sizes = { "248 KB", "172 KB", "68 B", "12 KB" };
            for (int i = 0; i < 4; i++) m.Quarantine.Add(new Sketch20File { Index = i, Id = "QTN-20261004-00" + (i + 1), Name = names[i], Path = paths[i], Reason = reasons[i], Date = dates[i], Time = times[i], Size = sizes[i], Severity = i < 2 ? "Cao" : i == 2 ? "Kiểm thử" : "Trung bình", Resolved = true, Status = "Đang cách ly", FileType = "Windows PE · 64 bit", Source = "Bảo vệ thời gian thực", Created = "04/10/2026 · 09:24", Sha256 = "9b72c1f43a61b7e71f04c6492c30c88e765241a19a2fd583fdd160fd8bc4f614", Md5 = "3e7653b2b6f8b32212b0acb80cd7ab89" });
            m.Detail = m.Quarantine[0];
            if (screen == 19) m.Threats = m.Quarantine.Take(3).ToList();
            m.Activities.Add(new[] { "check", "Hoàn tất quét nhanh", "2.048 tệp · Không phát hiện mối đe dọa", "09:42" });
            m.Activities.Add(new[] { "archive", "Đã cách ly một tệp nghi ngờ", "invoice_update.exe · Bảo vệ thời gian thực", "09:25" });
            m.Activities.Add(new[] { "refresh", "Cập nhật cơ sở dữ liệu chữ ký", "Ứng dụng đang sử dụng dữ liệu mới nhất", "09:10" });
            string[] types = { "Quét nhanh", "Thời gian thực", "Quét thư mục", "Quét tệp", "Quét toàn bộ" }, counts = { "2.048", "1", "486", "1", "9.950" }, durations = { "00:38", "00:01", "00:14", "00:01", "04:52" };
            for (int i = 0; i < 5; i++) m.History.Add(new Sketch20History { Index = i, Date = i == 0 ? "04/10/2026" : dates[i - 1], Time = i == 0 ? "09:42" : times[i - 1], Type = types[i], Result = i == 0 ? "An toàn" : "Đã cách ly", Files = counts[i], Threats = i == 0 ? "0" : "1", Duration = durations[i], Risk = i > 0 });
            m.Engines.AddRange(new[] { new[] { "Engine A", "Trojan.Generic" }, new[] { "Engine B", "Malware.Suspicious" }, new[] { "Engine C", "HEUR: Trojan.Win32" }, new[] { "Engine D", "Riskware.Generic" } });
            m.Behavior.AddRange(new[] { new[] { "Tiến trình", "invoice_update.exe" }, new[] { "Khởi động cùng máy", "Cần kiểm tra", "warn", "Tham chiếu đến thư mục Startup." }, new[] { "Thực thi lệnh", "Đáng ngờ", "risk", "Có chuỗi lệnh PowerShell được mã hóa." }, new[] { "Kết nối mạng", "Có chuỗi địa chỉ trong nội dung tệp" }, new[] { "Kết luận", "Giữ cách ly và đối chiếu với kết quả chữ ký." } });
            m.Strings.AddRange(new[] { "This program cannot be run in DOS mode.", "kernel32.dll", "CreateProcessW", "powershell -EncodedCommand ...", @"Software\Microsoft\Windows\CurrentVersion\Run", "https://sample.invalid/update" });
            m.HistoryTotal = m.History.Count; m.HistoryPage = "1 / 1";
            if (screen != 5 && (screen < 12 || screen > 14)) ApplyReferenceData(m, screen);
            return m;
        }
        private static void ApplyReferenceData(Sketch20Model m, int screen)
        {
            m.ScannedFiles = screen == 18 ? 48526 : 125432; m.Progress = 68; m.ElapsedSeconds = screen == 18 ? 134 : 0;
            m.LastDate = screen == 19 ? "Hôm nay, 07:32" : screen == 20 ? "Hôm qua, 21:48" : "Hôm nay, 09:24";
            m.LastTime = ""; m.Signature = "2024.11.08.001"; m.VtTime = "05/11/2024, 14:12";
            m.ScanPath = @"C:\Users\Admin\Downloads\setup_toolkit_v2.4.1.exe";
            m.KeyConfigured = true; m.Flags["vtq"] = true;
            m.Activities.Clear();
            m.Activities.AddRange(new[] {
                new[] { "check", "Hoàn tất quét nhanh", "Đã quét 42.591 tệp, không phát hiện mối đe dọa nào.", "Hôm nay, 09:24" },
                new[] { "archive", "Đã cách ly một tệp nghi ngờ", "setup_crack.exe đã được đưa vào vùng cách ly.", "Hôm qua, 16:03" },
                new[] { "refresh", "Cập nhật cơ sở dữ liệu chữ ký", "Đã cập nhật lên phiên bản 2024.11.08.001.", "Hôm qua, 10:17" },
                new[] { "check", "Hoàn tất quét nâng cao", "Đã quét 203.448 tệp, không phát hiện mối đe dọa nào.", "05/11/2024, 21:36" },
                new[] { "settings", "Kích hoạt bảo vệ thời gian thực", "Tính năng bảo vệ thời gian thực đã được bật.", "05/11/2024, 14:12" } });
            if (screen == 20) m.Activities[0] = new[] { "alert", "Bảo vệ thời gian thực đã bị tắt", "Thiết bị của bạn hiện không được bảo vệ.", "Hôm nay, 09:24" };
            if (screen == 18)
            {
                m.ThreatCount = 3;
                m.Activities.Clear(); m.Activities.AddRange(new[] {
                    new[] { "refresh", "Đang quét tệp", m.ScanPath, "Hôm nay, 10:24" },
                    new[] { "archive", "Phát hiện mối đe dọa", "Trojan.Generic.12345", "Hôm nay, 10:22" },
                    new[] { "file", "Đã cách ly tệp", @"C:\Users\Admin\Desktop\malware.exe", "Hôm nay, 10:22" },
                    new[] { "settings", "Bắt đầu quét tùy chỉnh", "4 vị trí được chọn", "Hôm nay, 10:20" } });
            }
            m.Detail.Size = "12,5 MB"; m.Detail.Path = @"C:\Users\Admin\Downloads\invoice_update.exe";
            m.Detail.Date = "05/11/2024"; m.Detail.Time = "14:12"; m.Detail.Created = "05/11/2024, 14:10"; m.Detail.FileType = "Ứng dụng (.exe)";
            m.Detail.Sha256 = "3f2a6e9b4c9b4d7e8a0f6c1d2b3e4f567890abcdef1234567890fedcba9876543";
            m.Detail.Md5 = "f4d2a9c6e8b1d3f0745e8c9a1b2d3e4f";
            if (screen >= 9 && screen <= 11) { m.Detail.Name = "setup_crack.exe"; m.Detail.Path = @"C:\Users\Admin\Downloads\setup_crack.exe"; m.Detail.Size = screen == 11 ? "42.591 KB" : "125.432 KB"; m.Detail.Time = "16:03"; m.Detail.Id = "ISO-20241105-16-03-7F3A9C"; }
            if (screen == 4)
            {
                m.Quarantine.Clear();
                string[] names = { "setup_crack.exe", "keygen.exe", "game_patch.dll", "virus_sample.zip", "suspicious_script.vbs", "malware.docm" };
                string[] paths = { @"C:\Users\Admin\Downloads\", @"D:\Software\Keygen\", @"C:\Games\Patch\", @"D:\Test\", @"C:\Users\Admin\Documents\", @"C:\Users\Admin\Downloads\" };
                string[] reasons = { "Heuristic: CrackTool (Gen.Variant)", "HackTool: Keygen (Gen)", "Gen.Variant.Malware", "Trojan: Win32/Trojan (Gen)", "Suspicious Script Behavior", "Macro: Suspicious Behavior" };
                for (int i = 0; i < names.Length; i++) m.Quarantine.Add(new Sketch20File { Index = i, Id = "REF-" + i, Name = names[i], Path = paths[i], Reason = reasons[i], Date = new[] { "05/11/2024", "04/11/2024", "02/11/2024", "01/11/2024", "30/10/2024", "29/10/2024" }[i], Time = new[] { "16:03", "10:17", "14:38", "22:11", "11:08", "18:25" }[i], Severity = i == 2 || i == 4 ? "Trung bình" : "Cao", Size = "", Resolved = true, Status = "Đang cách ly" });
            }
            m.History.Clear();
            string[] types = { "Quét toàn bộ hệ thống", "Quét thư mục tùy chọn", "Quét tệp riêng lẻ", "Quét nhanh", "Quét nhanh", "Quét toàn bộ hệ thống", "Quét thư mục tùy chọn", "Quét tệp riêng lẻ" };
            string[] counts = { "203.448", "12.931", "1", "42.591", "38.204", "187.332", "6.421", "1" };
            string[] durations = { "18 phút", "3 phút", "12 giây", "4 phút", "3 phút", "25 phút", "1 phút", "10 giây" };
            for (int i = 0; i < types.Length; i++) m.History.Add(new Sketch20History { Index = i, Date = new[] { "05/11/2024", "05/11/2024", "04/11/2024", "03/11/2024", "02/11/2024", "01/11/2024", "30/10/2024", "29/10/2024" }[i], Time = new[] { "21:36", "14:12", "10:17", "16:03", "09:24", "22:11", "11:08", "18:25" }[i], Type = types[i], Files = counts[i], Duration = durations[i], Risk = i == 1 || i == 2 || i == 5, Threats = i == 1 ? "2" : i == 2 ? "1" : i == 5 ? "3" : "0", Result = i == 1 || i == 2 || i == 5 ? "Đã cách ly" : "An toàn" });
            m.HistoryTotal = 8;
            m.Engines[1][1] = "Trojan.Win32"; m.Engines[2][1] = "Gen:Variant.Trojan"; m.Engines[3][1] = "Undetected";
            m.Behavior.Clear(); m.Behavior.AddRange(new[] {
                new[] { "Tiến trình", "Tạo 3 tiến trình con", "risk", "Hoạt động của tiến trình trong hệ thống" },
                new[] { "Khởi động cùng máy", "Thêm vào Registry Run", "risk", "Kiểm tra cơ chế tự khởi động" },
                new[] { "Thực thi lệnh", "Thực thi cmd.exe / powershell", "warn", "Các lệnh hệ thống đã thực thi" },
                new[] { "Kết nối mạng", "Kết nối đến 2 địa chỉ IP lạ", "warn", "Các kết nối ra bên ngoài" },
                new[] { "Kết luận", "Có dấu hiệu khả nghi, cần phân tích sâu hơn", "risk", "Đánh giá tổng hợp từ các hành vi" } });
            m.Strings.Clear(); m.Strings.AddRange(new[] { @"C:\Windows\System32\cmd.exe", "powershell.exe -ExecutionPolicy Bypass", "http://185.199.110.20/api/v1/update", "https://api.telegram.org/bot", @"Software\Microsoft\Windows\CurrentVersion\Run", "CreateRemoteThread", "VirtualAllocEx", "LoadLibraryA", "user32.dll", "kernel32.dll", "MZ", "This program cannot be run in DOS mode." });
        }
    }
    internal sealed class Sketch20PreviewForm : Form
    {
        internal readonly Sketch20View View;
        internal Sketch20PreviewForm(int screen = 1)
        {
            Text = "ScannerVirus · 20 màn phác thảo"; AutoScaleMode = AutoScaleMode.None; StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1000, 690); ClientSize = new Size(1240, 800); KeyPreview = true;
            Padding = Padding.Empty; BackColor = SystemColors.Control;
            View = new Sketch20View(Sketch20Samples.Create(screen), new Sketch20Actions()); Controls.Add(View); View.SelectScreen(screen);
            Sketch20Chrome.Attach(this, View);
            KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.F6) { View.SelectScreen(View.ScreenNumber % 20 + 1); e.Handled = true; } };
        }
    }
}
