using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ScanAndRemoveVirus.Services
{
    /*/// <summary>
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
    }*/
    /// <summary>
    /// Tính kích thước tệp hoặc thư mục.
    ///
    /// Sử dụng cho danh sách thư mục/tệp được chọn
    /// trong chức năng Quét nâng cao.
    ///
    /// Class này KHÔNG sử dụng SQL Server vì chỉ thực hiện
    /// thao tác với hệ thống tệp.
    /// </summary>
    public static class DirectorySizeCalculator
    {
        /// <summary>
        /// Tính kích thước của một tệp hoặc thư mục.
        ///
        /// - File: trả về kích thước file.
        /// - Folder: tính tổng kích thước đệ quy.
        /// - Đường dẫn không tồn tại: trả về 0.
        /// - Thư mục không có quyền truy cập: bỏ qua.
        /// - Junction/Symbolic link: bỏ qua.
        /// - Có hỗ trợ CancellationToken.
        /// </summary>
        public static long GetSize(
            string path,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(path))
                return 0;

            // ==========================================
            // KIỂM TRA FILE
            // ==========================================

            try
            {
                if (File.Exists(path))
                {
                    ct.ThrowIfCancellationRequested();

                    FileInfo fileInfo =
                        new FileInfo(path);

                    return fileInfo.Length;
                }

                if (!Directory.Exists(path))
                    return 0;
            }
            catch (IOException)
            {
                return 0;
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }

            // ==========================================
            // TÍNH DUNG LƯỢNG THƯ MỤC
            // ==========================================

            long total = 0;

            Stack<string> stack =
                new Stack<string>();

            stack.Push(path);

            while (stack.Count > 0)
            {
                // Cho phép người dùng hủy tác vụ.
                ct.ThrowIfCancellationRequested();

                string currentDirectory =
                    stack.Pop();

                // ======================================
                // LẤY DANH SÁCH FILE
                // ======================================

                string[] files = null;

                try
                {
                    files =
                        Directory.GetFiles(
                            currentDirectory);
                }
                catch (UnauthorizedAccessException)
                {
                    // Không có quyền → bỏ qua.
                }
                catch (IOException)
                {
                    // Không đọc được → bỏ qua.
                }

                // ======================================
                // TÍNH DUNG LƯỢNG FILE
                // ======================================

                if (files != null)
                {
                    foreach (string file in files)
                    {
                        ct.ThrowIfCancellationRequested();

                        try
                        {
                            FileInfo info =
                                new FileInfo(file);

                            total += info.Length;
                        }
                        catch (IOException)
                        {
                            // File có thể đã bị xóa hoặc khóa.
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Không có quyền đọc file.
                        }
                    }
                }

                // ======================================
                // LẤY THƯ MỤC CON
                // ======================================

                string[] directories = null;

                try
                {
                    directories =
                        Directory.GetDirectories(
                            currentDirectory);
                }
                catch (UnauthorizedAccessException)
                {
                    // Không có quyền → bỏ qua.
                }
                catch (IOException)
                {
                    // Không đọc được → bỏ qua.
                }

                if (directories == null)
                    continue;

                // ======================================
                // ĐƯA THƯ MỤC CON VÀO STACK
                // ======================================

                foreach (
                    string directory
                    in directories)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        DirectoryInfo info =
                            new DirectoryInfo(
                                directory);

                        /*
                         * Bỏ qua Junction / Symbolic Link.
                         *
                         * Nếu không bỏ qua, có thể xảy ra:
                         *
                         * Folder A
                         *     ↓
                         * Junction → Folder A
                         *
                         * dẫn tới vòng lặp vô hạn.
                         */
                        if ((info.Attributes &
                             FileAttributes.ReparsePoint)
                            != 0)
                        {
                            continue;
                        }

                        stack.Push(directory);
                    }
                    catch (IOException)
                    {
                        // Bỏ qua thư mục lỗi.
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Không có quyền → bỏ qua.
                    }
                }
            }

            return total;
        }

        /// <summary>
        /// Định dạng dung lượng thành dạng dễ đọc.
        ///
        /// Ví dụ:
        /// 500 B
        /// 812 KB
        /// 2.45 MB
        /// 1.25 GB
        /// </summary>
        public static string Format(long bytes)
        {
            if (bytes <= 0)
                return "0 KB";

            if (bytes < 1024L)
            {
                return bytes + " B";
            }

            if (bytes < 1024L * 1024L)
            {
                return
                    (bytes / 1024.0)
                    .ToString("0.#")
                    + " KB";
            }

            if (bytes <
                1024L * 1024L * 1024L)
            {
                return
                    (bytes /
                     (1024.0 * 1024.0))
                    .ToString("0.##")
                    + " MB";
            }

            return
                (bytes /
                 (1024.0 *
                  1024.0 *
                  1024.0))
                .ToString("0.##")
                + " GB";
        }
    }
}
