using System;
using System.IO;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Một nguồn duy nhất cho thư mục dữ liệu của app (settings, sổ cách ly, lịch sử...).
    /// Mặc định: %AppData%\ScanAndRemoveVirus.
    /// Đặt biến môi trường XVIRUS_DATA_DIR để trỏ sang kho dữ liệu riêng —
    /// dùng khi chạy test cách ly để không đụng dữ liệu thật của người dùng.
    /// </summary>
    internal static class DataDir
    {
        public static string Resolve(string fileName)
        {
            string custom = Environment.GetEnvironmentVariable("XVIRUS_DATA_DIR");
            string root = string.IsNullOrEmpty(custom)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ScanAndRemoveVirus")
                : custom;
            return Path.Combine(root, fileName);
        }
    }
}
