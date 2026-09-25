using System.IO;
using System.Threading;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Tính kích thước thư mục/tệp đệ quy cho bảng "Danh sách thư mục đã chọn" của trang Quét nâng cao
    /// (SPEC-UcTongQuan.md §5.3). Chạy được ở thread nền, hủy được, và KHÔNG ném lỗi khi gặp
    /// thư mục không có quyền đọc — cộng dồn phần đọc được rồi trả về.
    /// </summary>
    public static class DirectorySizeCalculator
    {
        /// <summary>Kích thước (byte) của tệp hoặc thư mục; trả 0 nếu đường dẫn không tồn tại.</summary>
        public static long GetSize(string path, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(path)) return 0;
            try
            {
                if (File.Exists(path)) return new FileInfo(path).Length;
                if (!Directory.Exists(path)) return 0;
            }
            catch (IOException) { return 0; }
            catch (System.UnauthorizedAccessException) { return 0; }

            long total = 0;
            var stack = new System.Collections.Generic.Stack<string>();
            stack.Push(path);
            while (stack.Count > 0)
            {
                ct.ThrowIfCancellationRequested();
                string dir = stack.Pop();
                string[] files = null, dirs = null;
                try { files = Directory.GetFiles(dir); }
                catch (System.UnauthorizedAccessException) { }
                catch (IOException) { }
                if (files != null)
                {
                    foreach (string f in files)
                    {
                        try { total += new FileInfo(f).Length; }
                        catch (IOException) { }
                        catch (System.UnauthorizedAccessException) { }
                    }
                }
                try { dirs = Directory.GetDirectories(dir); }
                catch (System.UnauthorizedAccessException) { }
                catch (IOException) { }
                if (dirs == null) continue;
                foreach (string d in dirs)
                {
                    try
                    {
                        // Bỏ junction/symlink để không lặp vô hạn (giống ScanEngine)
                        if ((new DirectoryInfo(d).Attributes & FileAttributes.ReparsePoint) != 0) continue;
                        stack.Push(d);
                    }
                    catch (IOException) { }
                    catch (System.UnauthorizedAccessException) { }
                }
            }
            return total;
        }

        /// <summary>Định dạng kích thước thân thiện: "2.45 MB", "812 KB", "0 KB".</summary>
        public static string Format(long bytes)
        {
            if (bytes <= 0) return "0 KB";
            if (bytes < 1024L) return bytes + " B";
            if (bytes < 1024L * 1024L) return (bytes / 1024.0).ToString("0.#") + " KB";
            if (bytes < 1024L * 1024L * 1024L) return (bytes / (1024.0 * 1024.0)).ToString("0.##") + " MB";
            return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("0.##") + " GB";
        }
    }
}
