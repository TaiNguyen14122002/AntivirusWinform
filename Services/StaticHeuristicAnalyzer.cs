using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Phân tích heuristic TĨNH - không thực thi tệp hoặc chạy tiến trình ngoài.
    /// Chỉ gọi sau khi MD5/SHA-1/SHA-256 và CDC đều KHÔNG phát hiện.
    /// Kết quả là NGHI VẤN, không phải bằng chứng xác nhận mã độc.
    /// Phạm vi đọc giới hạn để không làm chậm quét tệp lớn.
    /// </summary>
    public static class StaticHeuristicAnalyzer
    {
        public const int MinimumSuspicionScore = 65;
        private const int MaxReadBytes = 1024 * 1024; // tối đa 1 MiB cho heuristic/tệp
        private const int BufferBytes = 64 * 1024;
        private static readonly Encoding ByteEncoding = Encoding.GetEncoding(28591);
        private static readonly HashSet<string> ScriptExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".ps1", ".psm1", ".vbs", ".vbe", ".js", ".jse", ".wsf", ".hta", ".bat", ".cmd" };

        public sealed class Finding
        {
            public int Score { get; internal set; }
            public string Category { get; internal set; }
            public string Evidence { get; internal set; }
        }

        public static Finding Analyze(string path, CancellationToken cancellation, Action<long> onBytesRead = null)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            cancellation.ThrowIfCancellationRequested();
            string extension = Path.GetExtension(path);
            bool scripted = ScriptExtensions.Contains(extension);

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, BufferBytes, FileOptions.SequentialScan))
            {
                int lead = stream.ReadByte();
                int second = stream.ReadByte();
                if (onBytesRead != null) onBytesRead(Math.Min(2, stream.Length));
                bool executable = lead == 'M' && second == 'Z';
                if (!scripted && !executable) return null; // đa số tệp dữ liệu không tốn thêm thời gian
                stream.Position = 0;
                int limit = (int)Math.Min(MaxReadBytes, stream.Length);
                byte[] data = new byte[limit];
                int read = 0;
                while (read < limit)
                {
                    cancellation.ThrowIfCancellationRequested();
                    int n = stream.Read(data, read, Math.Min(BufferBytes, limit - read));
                    if (n == 0) break;
                    read += n;
                    if (onBytesRead != null) onBytesRead(n);
                }
                if (read == 0) return null;

                if (scripted && !executable) return AnalyzeScript(data, read);
                if (executable) return AnalyzePortableExecutable(data, read);
                return null;
            }
        }

        private static Finding AnalyzeScript(byte[] data, int count)
        {
            // Dùng Latin1 tương đương ánh xạ byte->char, không phụ thuộc codepage hệ thống.
            // Đồng thời nhận UTF-16LE phổ biến trong script Windows.
            string ascii = ByteEncoding.GetString(data, 0, count).ToLowerInvariant();
            string unicode = count >= 4 ? Encoding.Unicode.GetString(data, 0, count - count % 2).ToLowerInvariant() : "";
            var findings = new List<string>();
            int score = 0;
            bool network = Any(ascii, unicode, "downloadstring", "downloadfile", "invoke-webrequest", "xmlhttp", "winhttp.winhttprequest", "bitsadmin /transfer");
            bool execute = Any(ascii, unicode, "invoke-expression", "iex(", "iex ", ".run(", ".exec(", "start-process", "wscript.shell", "shell.application");
            bool encoded = Any(ascii, unicode, "frombase64string", "encodedcommand", "-encodedcommand", " -enc ", "-e ");
            bool powershell = Any(ascii, unicode, "powershell", "pwsh", "invoke-expression", "iex(", "iex ");
            bool persistence = Any(ascii, unicode, "currentversion\\run", "schtasks /create", "new-scheduledtask", "reg add ") && execute;
            bool shadow = Any(ascii, unicode, "vssadmin delete shadows", "wmic shadowcopy delete", "delete shadows /all") && execute;

            if (network && execute)
            {
                score += 70;
                findings.Add("kết hợp tải nội dung từ mạng và thực thi script/lệnh");
            }
            if (encoded && execute && powershell)
            {
                score += 72;
                findings.Add("lệnh thực thi kết hợp payload mã hóa/base64");
            }
            if (shadow)
            {
                score += 76;
                findings.Add("lệnh can thiệp bản sao lưu cùng cơ chế thực thi");
            }
            if (persistence && network)
            {
                score += 35;
                findings.Add("tải nội dung từ mạng kết hợp thiết lập tự khởi động");
            }
            return Result(score, "Script", findings);
        }

        private static Finding AnalyzePortableExecutable(byte[] data, int count)
        {
            // Không nhận diện chỉ từ 'MZ': phải có cấu trúc PE đủ hợp lệ.
            if (count < 256) return null;
            long pe = UInt32(data, 0x3c);
            if (pe < 64 || pe > count - 24) return null;
            int pos = (int)pe;
            if (data[pos] != 'P' || data[pos + 1] != 'E' || data[pos + 2] != 0 || data[pos + 3] != 0)
                return null;
            int sectionCount = UInt16(data, pos + 6);
            int optionalHeaderLength = UInt16(data, pos + 20);
            long startSections = pe + 24L + optionalHeaderLength;
            if (sectionCount < 1 || sectionCount > 96 || startSections > count ||
                startSections + (long)sectionCount * 40 > count) return null;

            var findings = new List<string>();
            int score = 0;
            bool writableExecutable = false;
            bool highEntropyExecutable = false;
            for (int i = 0; i < sectionCount; i++)
            {
                int section = (int)startSections + i * 40;
                uint characteristics = UInt32(data, section + 36);
                bool executable = (characteristics & 0x20000000U) != 0;
                bool writable = (characteristics & 0x80000000U) != 0;
                if (executable && writable) writableExecutable = true;

                if (!executable) continue;
                long rawSize = UInt32(data, section + 16);
                long rawOffset = UInt32(data, section + 20);
                if (rawSize >= 4096 && rawOffset >= 0 && rawOffset < count)
                {
                    int available = (int)Math.Min(65536, Math.Min(rawSize, count - rawOffset));
                    if (available >= 4096 && ShannonEntropy(data, (int)rawOffset, available) >= 7.45)
                        highEntropyExecutable = true;
                }
            }
            if (writableExecutable)
            {
                score += 38;
                findings.Add("section PE vừa ghi được vừa thực thi (W+X)");
            }
            if (highEntropyExecutable)
            {
                score += 27;
                findings.Add("entropy cao ở section có quyền thực thi");
            }

            // Trích chuỗi ASCII và UTF-16LE từ giới hạn 1 MiB đầu, không nạp cả tệp.
            string ascii = ByteEncoding.GetString(data, 0, count).ToLowerInvariant();
            string unicode = Encoding.Unicode.GetString(data, 0, count - count % 2).ToLowerInvariant();
            bool memory = Any(ascii, unicode, "virtualallocex", "virtualalloc", "ntallocatevirtualmemory", "virtualprotect");
            bool write = Any(ascii, unicode, "writeprocessmemory", "ntwritevirtualmemory");
            bool thread = Any(ascii, unicode, "createremotethread", "ntcreatethreadex", "queueuserapc");
            if (memory && write && thread)
            {
                score += 77;
                findings.Add("tổ hợp API thao tác bộ nhớ tiến trình và tạo luồng từ xa");
            }
            else if (memory && write)
            {
                score += 28;
                findings.Add("API cấp phát và ghi vào bộ nhớ tiến trình");
            }
            bool downloader = Any(ascii, unicode, "urldownloadtofile", "winhttpopen", "internetopenurl");
            bool launcher = Any(ascii, unicode, "winexec", "createprocess", "shellexecute");
            if (downloader && launcher)
            {
                score += 42;
                findings.Add("kết hợp API tải dữ liệu và khởi chạy tiến trình");
            }
            return Result(score, "PE", findings);
        }

        private static Finding Result(int score, string category, List<string> evidence)
        {
            if (score < MinimumSuspicionScore || evidence.Count == 0) return null;
            return new Finding
            {
                Score = Math.Min(100, score), Category = category,
                Evidence = string.Join("; ", evidence.ToArray())
            };
        }

        private static bool Any(string ascii, string unicode, params string[] patterns)
        {
            foreach (string pattern in patterns)
                if (ascii.IndexOf(pattern, StringComparison.Ordinal) >= 0 ||
                    unicode.IndexOf(pattern, StringComparison.Ordinal) >= 0) return true;
            return false;
        }
        private static ushort UInt16(byte[] input, int i)
        {
            return (ushort)(input[i] | input[i + 1] << 8);
        }
        private static uint UInt32(byte[] input, int i)
        {
            return (uint)input[i] | ((uint)input[i + 1] << 8) | ((uint)input[i + 2] << 16) | ((uint)input[i + 3] << 24);
        }
        private static double ShannonEntropy(byte[] data, int start, int length)
        {
            var frequencies = new int[256];
            for (int i = start; i < start + length; i++) frequencies[data[i]]++;
            double entropy = 0;
            foreach (int frequency in frequencies)
            {
                if (frequency == 0) continue;
                double p = (double)frequency / length;
                entropy -= p * Math.Log(p, 2);
            }
            return entropy;
        }
    }
}
