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
            return m;
        }
    }
    internal sealed class Sketch20PreviewForm : Form
    {
        internal readonly Sketch20View View;
        internal Sketch20PreviewForm(int screen = 1)
        {
            Text = "ScannerVirus · 20 màn phác thảo"; AutoScaleMode = AutoScaleMode.None; StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1000, 690); ClientSize = new Size(1240, 800); KeyPreview = true;
            Padding = new Padding(1); BackColor = Sketch20Style.Border;
            View = new Sketch20View(Sketch20Samples.Create(screen), new Sketch20Actions()); Controls.Add(View); View.SelectScreen(screen);
            KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.F6) { View.SelectScreen(View.ScreenNumber % 20 + 1); e.Handled = true; } };
        }
    }
}
