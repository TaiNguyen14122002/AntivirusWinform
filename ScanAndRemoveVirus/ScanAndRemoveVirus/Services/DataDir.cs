using System;
using System.IO;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Một nguồn duy nhất cho thư mục dữ liệu của app (settings, sổ cách ly, lịch sử...).
    /// Mặc định: \AppData\ ngay trong solution (tự dò bằng thư mục Samples\ ở gốc solution,
    /// cùng kiểu leo-cây với TestSamples.FolderPath) — dữ liệu đi theo repo, dễ thấy/dễ dọn.
    /// Không thấy Samples\ (chạy ngoài repo) -> fallback %AppData%\ScanAndRemoveVirus.
    /// Đặt biến môi trường XVIRUS_DATA_DIR để trỏ sang kho dữ liệu riêng —
    /// dùng khi chạy test cách ly để không đụng dữ liệu thật của người dùng.
    /// </summary>
    internal static class DataDir
    {
        private static string cachedRoot;

        private static string Root
        {
            get
            {
                if (cachedRoot != null) return cachedRoot;
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                for (int hop = 0; hop < 7 && dir != null; hop++, dir = dir.Parent)
                {
                    if (Directory.Exists(Path.Combine(dir.FullName, "Samples")))
                    {
                        cachedRoot = Path.Combine(dir.FullName, "AppData");
                        return cachedRoot;
                    }
                }
                cachedRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ScanAndRemoveVirus");
                return cachedRoot;
            }
        }

        public static string Resolve(string fileName)
        {
            string custom = Environment.GetEnvironmentVariable("XVIRUS_DATA_DIR");
            return Path.Combine(string.IsNullOrEmpty(custom) ? Root : custom, fileName);
        }
    }
}
