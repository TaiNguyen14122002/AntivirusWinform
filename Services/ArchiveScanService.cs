using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Quét nội dung ZIP bằng tệp tạm cô lập tên ngẫu nhiên để tái sử dụng bộ quét hiện có.
    /// Không chạy, không mở shell và không sử dụng tên entry làm đường dẫn ghi.
    /// Giới hạn byte thực giải nén được kiểm tra trong vòng đọc, không tin header ZIP.
    /// </summary>
    internal static class ArchiveScanService
    {
        internal sealed class Outcome
        {
            internal int Examined;
            internal int Unchecked;
        }

        private sealed class Budget
        {
            internal const int MaxDepth = 3;
            internal const int MaxEntries = 5000;
            internal const long MaxOneEntry = 512L * 1024 * 1024;
            internal const long MaxAllEntries = 2L * 1024 * 1024 * 1024;
            internal const int MaxSeconds = 120;
            internal int Entries;
            internal long ExtractedBytes;
            internal readonly Stopwatch Clock = Stopwatch.StartNew();
            internal void Check(CancellationToken cancellation)
            {
                cancellation.ThrowIfCancellationRequested();
                if (Clock.Elapsed.TotalSeconds > MaxSeconds)
                    throw new InvalidDataException("Vượt thời gian xử lý tệp nén.");
            }
        }

        private static readonly string StagingBase =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ScanAndRemoveVirus", "ArchiveScanTemp");

        internal static bool IsStagingDirectory(string folder)
        {
            try
            {
                string path = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar);
                string root = Path.GetFullPath(StagingBase).TrimEnd(Path.DirectorySeparatorChar);
                return path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception) { return false; }
        }

        internal static bool IsUnsupportedArchive(string path)
        {
            string ext = ScanDisplayPath.Extension(path);
            return new[] { ".rar", ".7z", ".iso" }
                .Any(x => string.Equals(x, ext, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool IsZip(string file)
        {
            return !string.IsNullOrWhiteSpace(file) &&
                string.Equals(ScanDisplayPath.Extension(file), ".zip", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsSupportedArchive(string path)
        {
            string leaf = ScanDisplayPath.Leaf(path);
            return IsZip(path) || leaf.EndsWith(".tar", StringComparison.OrdinalIgnoreCase) ||
                leaf.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase) ||
                leaf.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
        }

        // .tar / .tar.gz / .tgz / .gz: .NET Framework có GZipStream, còn TAR đọc header
        // ở chế độ chỉ đọc; không gọi shell hoặc tiện ích giải nén bên ngoài.
        private static void ScanArchive(string diskPath, string displayPath, string tempRoot, int depth,
            Budget budget, Func<string, string, bool> inspect, Action<string, string> issue,
            Outcome outcome, CancellationToken cancellation)
        {
            budget.Check(cancellation);
            if (depth > Budget.MaxDepth)
            {
                outcome.Unchecked++;
                issue(displayPath, "Tệp nén lồng quá " + Budget.MaxDepth + " cấp.");
                return;
            }
            string leaf = ScanDisplayPath.Leaf(displayPath);
            if (IsZip(displayPath))
            {
                ScanZip(diskPath, displayPath, tempRoot, depth, budget, inspect, issue, outcome, cancellation);
            }
            else if (leaf.EndsWith(".tar", StringComparison.OrdinalIgnoreCase))
            {
                ScanTar(diskPath, displayPath, tempRoot, depth, budget, inspect, issue, outcome, cancellation);
            }
            else if (leaf.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ||
                     leaf.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
            {
                string decompressedName = leaf.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)
                    ? leaf.Substring(0, leaf.Length - 4) + ".tar" : leaf.Substring(0, leaf.Length - 3);
                string extension = ScanDisplayPath.Extension(decompressedName);
                if (extension.Length > 12) extension = ".dat";
                string staged = Path.Combine(tempRoot, Guid.NewGuid().ToString("N") + extension);
                string innerName = displayPath + " > " + decompressedName;
                try
                {
                    budget.Entries++;
                    if (budget.Entries > Budget.MaxEntries)
                        throw new InvalidDataException("Tổng số entry vượt giới hạn.");
                    using (var file = new FileStream(diskPath, FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete, 65536, FileOptions.SequentialScan))
                    using (var gz = new GZipStream(file, CompressionMode.Decompress))
                    using (var output = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        CopyLimited(gz, output, budget, cancellation);
                    if (IsSupportedArchive(decompressedName))
                        ScanArchive(staged, innerName, tempRoot, depth + 1, budget, inspect, issue, outcome, cancellation);
                    else if (inspect(staged, innerName))
                    {
                        outcome.Examined++;
                        if (IsUnsupportedArchive(decompressedName))
                        { outcome.Unchecked++; issue(innerName, "Chưa quét sâu nội dung định dạng nén này."); }
                    }
                    else
                    {
                        outcome.Unchecked++;
                        issue(innerName, "Không đọc được nội dung sau giải nén GZip.");
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) when (ex is IOException || ex is InvalidDataException ||
                    ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
                {
                    outcome.Unchecked++;
                    issue(innerName, "GZip chưa kiểm tra: " + ex.GetType().Name);
                }
                finally { try { File.Delete(staged); } catch (Exception) { } }
            }
        }

        private static void CopyLimited(Stream input, Stream output, Budget budget, CancellationToken cancel)
        {
            long thisEntry = 0;
            byte[] bytes = new byte[65536];
            int read;
            while ((read = input.Read(bytes, 0, bytes.Length)) > 0)
            {
                budget.Check(cancel);
                thisEntry += read;
                budget.ExtractedBytes += read;
                if (thisEntry > Budget.MaxOneEntry || budget.ExtractedBytes > Budget.MaxAllEntries)
                    throw new InvalidDataException("Giải nén vượt giới hạn tài nguyên.");
                output.Write(bytes, 0, read);
            }
        }

        private static long TarOctal(byte[] b, int start, int length)
        {
            long value = 0;
            bool found = false;
            for (int i = start; i < start + length; i++)
            {
                byte v = b[i];
                if (v == 0 || v == 32)
                {
                    if (found) break;
                    continue;
                }
                if (v < (byte)'0' || v > (byte)'7') throw new InvalidDataException("Header TAR không dùng dạng octal thông thường.");
                checked { value = value * 8 + (v - '0'); }
                found = true;
            }
            return value;
        }
        private static string TarString(byte[] buffer, int start, int size)
        {
            int len = 0;
            while (len < size && buffer[start + len] != 0) len++;
            return System.Text.Encoding.UTF8.GetString(buffer, start, len);
        }
        private static void ReadTarExactly(Stream stream, byte[] bytes, int count)
        {
            int n = 0;
            while (n < count)
            {
                int current = stream.Read(bytes, n, count - n);
                if (current <= 0) throw new InvalidDataException("TAR bị cắt giữa dữ liệu.");
                n += current;
            }
        }
        private static void ScanTar(string archivePath, string virtualPath, string tempRoot, int depth,
            Budget budget, Func<string, string, bool> inspect, Action<string, string> issue,
            Outcome outcome, CancellationToken cancellation)
        {
            using (var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 65536, FileOptions.SequentialScan))
            {
                byte[] header = new byte[512];
                while (stream.Position < stream.Length)
                {
                    budget.Check(cancellation);
                    if (stream.Length - stream.Position < 512) throw new InvalidDataException("TAR thiếu header 512 byte.");
                    ReadTarExactly(stream, header, 512);
                    if (header.All(x => x == 0)) return;
                    long expectedChecksum = TarOctal(header, 148, 8);
                    long calculated = 0;
                    for (int i = 0; i < 512; i++)
                        calculated += (i >= 148 && i < 156) ? 32 : header[i];
                    if (calculated != expectedChecksum) throw new InvalidDataException("Checksum header TAR không hợp lệ.");
                    long size = TarOctal(header, 124, 12);
                    string name = TarString(header, 0, 100);
                    string prefix = TarString(header, 345, 155);
                    if (prefix.Length > 0) name = prefix + "/" + name;
                    string entryPath = virtualPath + " > " + name;
                    char type = (char)header[156];
                    budget.Entries++;
                    if (budget.Entries > Budget.MaxEntries)
                    { outcome.Unchecked++; issue(entryPath, "Vượt giới hạn số entry TAR."); return; }
                    long rounded = checked(((size + 511) / 512) * 512);
                    if (rounded > stream.Length - stream.Position)
                        throw new InvalidDataException("Kích thước nội dung TAR vượt kích thước tệp.");
                    long next = stream.Position + rounded;
                    try
                    {
                        if (type == '5') continue; // directory
                        if (type != '0' && type != '\0')
                        {
                            outcome.Unchecked++;
                            issue(entryPath, "Loại entry TAR không hỗ trợ (link, PAX hoặc special).");
                            continue;
                        }
                        if (UnsafeEntryPath(name) || size > Budget.MaxOneEntry ||
                            size > Budget.MaxAllEntries - budget.ExtractedBytes)
                        {
                            outcome.Unchecked++;
                            issue(entryPath, "Tên hoặc dung lượng entry TAR không an toàn.");
                            continue;
                        }
                        string ext = ScanDisplayPath.Extension(name);
                        if (name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)) ext = ".tar.gz";
                        if (ext.Length > 12 || ext.Any(ch => !char.IsLetterOrDigit(ch) && ch != '.')) ext = ".dat";
                        string staged = Path.Combine(tempRoot, Guid.NewGuid().ToString("N") + ext);
                        try
                        {
                            using (var output = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                            {
                                byte[] copy = new byte[65536];
                                long remaining = size;
                                while (remaining > 0)
                                {
                                    budget.Check(cancellation);
                                    int n = stream.Read(copy, 0, (int)Math.Min(copy.Length, remaining));
                                    if (n <= 0) throw new InvalidDataException("TAR thiếu nội dung entry.");
                                    output.Write(copy, 0, n);
                                    budget.ExtractedBytes += n;
                                    if (budget.ExtractedBytes > Budget.MaxAllEntries)
                                        throw new InvalidDataException("Tổng giải nén TAR vượt giới hạn.");
                                    remaining -= n;
                                }
                            }
                            if (IsSupportedArchive(name))
                                ScanArchive(staged, entryPath, tempRoot, depth + 1, budget, inspect, issue, outcome, cancellation);
                            else if (inspect(staged, entryPath))
                            {
                                outcome.Examined++;
                                if (IsUnsupportedArchive(name))
                                { outcome.Unchecked++; issue(entryPath, "Chưa quét sâu nội dung định dạng nén này."); }
                            }
                            else
                            {
                                outcome.Unchecked++;
                                issue(entryPath, "Không phân tích được nội dung TAR entry.");
                            }
                        }
                        finally { try { File.Delete(staged); } catch (Exception) { } }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) when (ex is IOException || ex is InvalidDataException ||
                        ex is UnauthorizedAccessException || ex is NotSupportedException || ex is ArgumentException)
                    {
                        outcome.Unchecked++;
                        issue(entryPath, "TAR entry chưa kiểm tra: " + ex.GetType().Name);
                    }
                    finally { stream.Position = next; }
                }
            }
        }

        /// <param name="inspect">Đường dẫn tệp tạm, đường dẫn ảo entry. Tệp tạm chỉ tồn tại khi callback chạy.</param>
        internal static Outcome Scan(string archivePath,
            Func<string, string, bool> inspect, Action<string, string> issue, CancellationToken cancellation)
        {
            var outcome = new Outcome();
            var budget = new Budget();
            string tempRoot = Path.Combine(StagingBase, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                ScanArchive(archivePath, archivePath, tempRoot, 1, budget, inspect, issue, outcome, cancellation);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException ||
                ex is UnauthorizedAccessException || ex is NotSupportedException ||
                ex is ArgumentException || ex is System.Security.SecurityException)
            {
                issue(archivePath, "Không thể đọc tệp nén: " + ex.GetType().Name);
                outcome.Unchecked++;
            }
            finally
            {
                try { Directory.Delete(tempRoot, true); }
                catch (Exception) { /* File locked by antivirus: cleanup is best effort */ }
            }
            return outcome;
        }

        private static bool UnsafeEntryPath(string name)
        {
            if (string.IsNullOrEmpty(name) || name[0] == '/' || name[0] == '\\') return true;
            // Windows drive path and UNC roots are not valid archive-relative paths.
            if (name.Length >= 2 && char.IsLetter(name[0]) && name[1] == ':') return true;
            string[] segments = name.Replace('\\', '/').Split('/');
            return segments.Any(segment => segment == "." || segment == ".." ||
                segment.Any(ch => ch == '\0' || char.IsControl(ch)));
        }

        private static void ScanZip(string zipPath, string virtualPath, string tempRoot,
            int depth, Budget budget, Func<string, string, bool> inspect,
            Action<string, string> issue, Outcome outcome, CancellationToken cancellation)
        {
            budget.Check(cancellation);
            if (depth > Budget.MaxDepth)
            {
                outcome.Unchecked++;
                issue(virtualPath, "ZIP lồng quá " + Budget.MaxDepth + " cấp.");
                return;
            }

            using (var input = new FileStream(zipPath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 65536, FileOptions.SequentialScan))
            using (var zip = new ZipArchive(input, ZipArchiveMode.Read, false))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    budget.Check(cancellation);
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    string entryName = entry.FullName.Replace('\\', '/');
                    string entryPath = virtualPath + " > " + entryName;
                    budget.Entries++;
                    if (budget.Entries > Budget.MaxEntries)
                    {
                        outcome.Unchecked++;
                        issue(entryPath, "Vượt giới hạn " + Budget.MaxEntries + " entry mỗi phiên đọc ZIP.");
                        return;
                    }
                    // ZIP entry có thể có tên hợp lệ trên Linux nhưng không hợp lệ với
                    // System.IO.Path của Windows (ví dụ chứa '|', ':', '?').
                    // Không gọi Path.IsPathRooted trên tên entry chưa tin cậy.
                    if (UnsafeEntryPath(entryName))
                    {
                        outcome.Unchecked++;
                        issue(entryPath, "Tên entry không an toàn.");
                        continue;
                    }
                    int unixType = (entry.ExternalAttributes >> 16) & 0xF000;
                    if (unixType == 0xA000)
                    {
                        outcome.Unchecked++;
                        issue(entryPath, "Liên kết tượng trưng trong ZIP chưa được hỗ trợ.");
                        continue;
                    }
                    if (entry.Length > Budget.MaxOneEntry ||
                        entry.Length > Budget.MaxAllEntries - budget.ExtractedBytes ||
                        (entry.CompressedLength > 0 && entry.Length / Math.Max(1, entry.CompressedLength) > 1000))
                    {
                        outcome.Unchecked++;
                        issue(entryPath, "Entry vượt giới hạn kích thước hoặc tỷ lệ nén.");
                        continue;
                    }
                    // Chỉ sử dụng phần mở rộng có ký tự hợp lệ cho tên tệp tạm GUID.
                    // Không đưa nguyên entry.FullName qua Path.GetExtension của Windows.
                    string ext = ScanDisplayPath.Extension(entry.Name);
                    if (entry.Name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)) ext = ".tar.gz";
                    if (ext.Length > 12 || ext.Any(ch => !char.IsLetterOrDigit(ch) && ch != '.')) ext = ".dat";
                    // Tên GUID bất biến: không bao giờ ghi theo entry.FullName, chống Zip Slip.
                    string staged = Path.Combine(tempRoot, Guid.NewGuid().ToString("N") + ext);
                    try
                    {
                        long current = 0;
                        using (var contents = entry.Open())
                        using (var output = new FileStream(staged, FileMode.CreateNew, FileAccess.Write,
                            FileShare.None, 65536, FileOptions.SequentialScan))
                        {
                            byte[] buffer = new byte[65536];
                            int n;
                            while ((n = contents.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                budget.Check(cancellation);
                                current += n;
                                budget.ExtractedBytes += n;
                                if (current > Budget.MaxOneEntry || budget.ExtractedBytes > Budget.MaxAllEntries)
                                    throw new InvalidDataException("Dữ liệu thực giải nén vượt giới hạn.");
                                output.Write(buffer, 0, n);
                            }
                        }
                        if (IsSupportedArchive(entry.Name))
                            ScanArchive(staged, entryPath, tempRoot, depth + 1, budget, inspect, issue, outcome, cancellation);
                        else
                        {
                            if (IsUnsupportedArchive(entry.Name))
                            {
                                outcome.Unchecked++;
                                issue(entryPath, "Chưa hỗ trợ kiểm tra nội dung định dạng nén này.");
                            }
                            if (inspect(staged, entryPath))
                                outcome.Examined++;
                            else
                            {
                                outcome.Unchecked++;
                                issue(entryPath, "Không đọc được nội dung entry để phân tích.");
                            }
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) when (ex is IOException || ex is InvalidDataException ||
                        ex is UnauthorizedAccessException || ex is NotSupportedException ||
                        ex is ArgumentException || ex is System.Security.SecurityException)
                    {
                        outcome.Unchecked++;
                        issue(entryPath, "Chưa kiểm tra được entry: " + ex.GetType().Name);
                    }
                    finally
                    {
                        try { File.Delete(staged); }
                        catch (Exception) { }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tên entry trong ZIP là đường dẫn ẢO, tuyệt đối không chuyển nó cho
    /// System.IO.Path để xác định tên hoặc đuôi tệp trên Windows.
    /// Không dùng các hàm này khi đọc, ghi, xóa hoặc di chuyển file vật lý.
    /// </summary>
    internal static class ScanDisplayPath
    {
        internal static string Leaf(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            int archiveBoundary = path.LastIndexOf(" > ", StringComparison.Ordinal);
            string last = archiveBoundary >= 0 ? path.Substring(archiveBoundary + 3) : path;
            int separator = Math.Max(last.LastIndexOf('/'), last.LastIndexOf('\\'));
            return separator >= 0 ? last.Substring(separator + 1) : last;
        }

        internal static string Extension(string path)
        {
            string leaf = Leaf(path);
            int dot = leaf.LastIndexOf('.');
            if (dot <= 0 || dot == leaf.Length - 1) return string.Empty;
            return leaf.Substring(dot);
        }

        internal static bool IsVirtual(string path)
        {
            return !string.IsNullOrEmpty(path) && path.Contains(" > ");
        }

        internal static string Parent(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            int boundary = path.LastIndexOf(" > ", StringComparison.Ordinal);
            if (boundary >= 0)
            {
                string entry = path.Substring(boundary + 3);
                int innerSeparator = Math.Max(entry.LastIndexOf('/'), entry.LastIndexOf('\\'));
                return innerSeparator >= 0
                    ? path.Substring(0, boundary + 3) + entry.Substring(0, innerSeparator)
                    : path.Substring(0, boundary);
            }
            int separator = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
            return separator >= 0 ? path.Substring(0, separator) : string.Empty;
        }
    }
}
