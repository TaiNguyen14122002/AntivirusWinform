using Microsoft.Win32;
using ScanAndRemoveVirus.Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Services
{
    public class SettingsFlags
    {
        // =========================
        // CÓ TRONG SQL SERVER
        // =========================

        public bool AutoStart;
        public bool AutoUpdate;
        public bool SendSamples;
        public bool ShowNotifications = true;

        public bool RealTimeOnPersist;
        public bool FileRestoreGuard = true;
        public bool UsbProtection = true;
        public bool DownloadProtection = true;

        // =========================
        // KHÔNG CÓ TRONG SQL SERVER
        // Vẫn lưu local settings.ini
        // =========================

        public bool BehaviorWatch = true;
        public bool StartupFoldersWatch = true;
        public bool VtAutoQuery;
    }

    /// <summary>
    /// Quản lý cài đặt ứng dụng.
    ///
    /// Các thiết lập có trong bảng dbo.Settings:
    ///     AutoStart
    ///     AutoUpdate
    ///     SubmitSamples
    ///     ShowNotifications
    ///     RealTimeProtection
    ///     FileProtection
    ///     USBProtection
    ///     DownloadProtection
    ///
    /// Các thiết lập chưa có cột trong CSDL:
    ///     BehaviorWatch
    ///     StartupFoldersWatch
    ///     VtAutoQuery
    ///
    /// Ba thiết lập trên vẫn được lưu trong settings.ini.
    ///
    /// AutoStart thực tế vẫn được áp dụng thông qua
    /// HKCU\Software\Microsoft\Windows\CurrentVersion\Run.
    /// </summary>
    public static class AppSettings
    {
        private const string RunKeyPath =
            @"Software\Microsoft\Windows\CurrentVersion\Run";

        private const string RunValueName =
            "ScanAndRemoveVirus";

        // Chỉ còn dùng để lưu những setting
        // chưa có cột tương ứng trong SQL Server.
        public static string SettingsPath
        {
            get
            {
                return DataDir.Resolve("settings.ini");
            }
        }

        // =========================================================
        // LOAD SETTINGS
        // =========================================================

        public static SettingsFlags Load()
        {
            SettingsFlags f = new SettingsFlags();

            // -----------------------------------------
            // 1. Đọc những setting có trong SQL Server
            // -----------------------------------------

            try
            {
                SettingsRepository repository =
                    new SettingsRepository();

                DataRow row = repository.Get();

                if (row != null)
                {
                    f.AutoStart =
                        GetBool(
                            row,
                            "AutoStart",
                            false);

                    f.AutoUpdate =
                        GetBool(
                            row,
                            "AutoUpdate",
                            false);

                    // Trong C# là SendSamples
                    // nhưng trong SQL là SubmitSamples.
                    f.SendSamples =
                        GetBool(
                            row,
                            "SubmitSamples",
                            false);

                    f.ShowNotifications =
                        GetBool(
                            row,
                            "ShowNotifications",
                            true);

                    f.RealTimeOnPersist =
                        GetBool(
                            row,
                            "RealTimeProtection",
                            false);

                    f.FileRestoreGuard =
                        GetBool(
                            row,
                            "FileProtection",
                            true);

                    f.UsbProtection =
                        GetBool(
                            row,
                            "USBProtection",
                            true);

                    f.DownloadProtection =
                        GetBool(
                            row,
                            "DownloadProtection",
                            true);
                }
            }
            catch (Exception)
            {
                // Nếu SQL Server tạm thời không truy cập được,
                // ứng dụng vẫn sử dụng các giá trị mặc định.
            }

            // -----------------------------------------
            // 2. Đọc những setting chưa có trong SQL
            // -----------------------------------------

            LoadLocalSettings(f);

            return f;
        }

        // =========================================================
        // SAVE SETTINGS
        // =========================================================

        public static void Save(SettingsFlags f)
        {
            if (f == null)
                return;

            // -----------------------------------------
            // 1. Lưu các setting có trong SQL Server
            // -----------------------------------------

            try
            {
                SettingsRepository repository =
                    new SettingsRepository();

                repository.UpdateGeneral(
                    f.AutoStart,
                    f.AutoUpdate,
                    f.SendSamples,
                    f.ShowNotifications);

                /*
                 * Bảng Settings hiện tại còn có:
                 *
                 * RealTimeProtection
                 * FileProtection
                 * USBProtection
                 * WebProtection
                 * DownloadProtection
                 * RansomwareProtection
                 *
                 * SettingsFlags hiện không quản lý trực tiếp
                 * WebProtection và RansomwareProtection.
                 *
                 * Vì vậy KHÔNG gọi UpdateProtection() ở đây,
                 * nếu gọi sẽ phải truyền giá trị Web/Ransomware
                 * không xác định và có thể ghi đè dữ liệu SQL.
                 *
                 * Những protection có trong SettingsFlags
                 * được cập nhật riêng bằng method bên dưới.
                 */

                UpdateProtectionSettings(
                    f.RealTimeOnPersist,
                    f.FileRestoreGuard,
                    f.UsbProtection,
                    f.DownloadProtection);
            }
            catch (Exception)
            {
                // Không làm ứng dụng crash nếu SQL tạm thời lỗi.
            }

            // -----------------------------------------
            // 2. Lưu các setting không có trong SQL
            // -----------------------------------------

            SaveLocalSettings(f);
        }

        // =========================================================
        // UPDATE PROTECTION SETTINGS
        // =========================================================

        private static void UpdateProtectionSettings(
            bool realTime,
            bool fileProtection,
            bool usbProtection,
            bool downloadProtection)
        {
            const string sql = @"
                UPDATE dbo.Settings
                SET
                    RealTimeProtection = @RealTime,
                    FileProtection = @FileProtection,
                    USBProtection = @USBProtection,
                    DownloadProtection = @DownloadProtection,
                    UpdatedAt = SYSDATETIME()
                WHERE SettingID =
                (
                    SELECT MIN(SettingID)
                    FROM dbo.Settings
                );";

            using (var conn = DbConnection.Create())
            using (var cmd =
                   new System.Data.SqlClient.SqlCommand(
                       sql,
                       conn))
            {
                cmd.Parameters.Add(
                    "@RealTime",
                    System.Data.SqlDbType.Bit)
                    .Value = realTime;

                cmd.Parameters.Add(
                    "@FileProtection",
                    System.Data.SqlDbType.Bit)
                    .Value = fileProtection;

                cmd.Parameters.Add(
                    "@USBProtection",
                    System.Data.SqlDbType.Bit)
                    .Value = usbProtection;

                cmd.Parameters.Add(
                    "@DownloadProtection",
                    System.Data.SqlDbType.Bit)
                    .Value = downloadProtection;

                conn.Open();

                cmd.ExecuteNonQuery();
            }
        }

        // =========================================================
        // LOCAL SETTINGS
        // =========================================================

        /// <summary>
        /// Chỉ đọc những setting KHÔNG có trong CSDL.
        /// </summary>
        private static void LoadLocalSettings(
            SettingsFlags f)
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return;

                foreach (
                    string line
                    in File.ReadAllLines(SettingsPath))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    int separator =
                        line.IndexOf('=');

                    if (separator <= 0)
                        continue;

                    string key =
                        line.Substring(
                            0,
                            separator).Trim();

                    string value =
                        line.Substring(
                            separator + 1).Trim();

                    bool result;

                    if (!bool.TryParse(
                        value,
                        out result))
                    {
                        continue;
                    }

                    switch (key)
                    {
                        case "BehaviorWatch":
                            f.BehaviorWatch = result;
                            break;

                        case "StartupFoldersWatch":
                            f.StartupFoldersWatch = result;
                            break;

                        case "VtAutoQuery":
                            f.VtAutoQuery = result;
                            break;
                    }
                }
            }
            catch (Exception)
            {
                // Nếu file local lỗi thì dùng mặc định.
            }
        }

        /// <summary>
        /// Chỉ lưu những setting KHÔNG có trong CSDL.
        /// </summary>
        private static void SaveLocalSettings(
            SettingsFlags f)
        {
            try
            {
                string directory =
                    Path.GetDirectoryName(
                        SettingsPath);

                if (!string.IsNullOrWhiteSpace(
                    directory))
                {
                    Directory.CreateDirectory(
                        directory);
                }

                File.WriteAllLines(
                    SettingsPath,
                    new[]
                    {
                        "BehaviorWatch="
                            + f.BehaviorWatch,

                        "StartupFoldersWatch="
                            + f.StartupFoldersWatch,

                        "VtAutoQuery="
                            + f.VtAutoQuery
                    });
            }
            catch (Exception)
            {
                // Không làm ứng dụng crash
                // nếu file local không ghi được.
            }
        }

        // =========================================================
        // HELPER ĐỌC BIT TỪ DATAROW
        // =========================================================

        private static bool GetBool(
            DataRow row,
            string columnName,
            bool defaultValue)
        {
            if (row == null)
                return defaultValue;

            if (row.Table == null)
                return defaultValue;

            if (!row.Table.Columns.Contains(
                columnName))
            {
                return defaultValue;
            }

            if (row[columnName] == DBNull.Value)
                return defaultValue;

            try
            {
                return Convert.ToBoolean(
                    row[columnName]);
            }
            catch
            {
                return defaultValue;
            }
        }

        // =========================================================
        // KHỞI ĐỘNG CÙNG WINDOWS (HKCU RUN, KHÔNG CẦN ADMIN)
        // =========================================================

        /// <summary>
        /// Trạng thái thực tế: khóa Run phải trỏ ĐÚNG tới file EXE đang chạy.
        /// Không xem một khóa cũ trỏ tới đường dẫn đã đổi là "đang bật".
        /// </summary>
        public static bool IsAutoStartEnabled()
        {
            try
            {
                if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                    return false;

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null) return false;
                    string registered = key.GetValue(RunValueName) as string;
                    if (string.IsNullOrWhiteSpace(registered)) return false;

                    // Chỉ chấp nhận đường dẫn tệp thực thi (có thể đặt trong ngoặc kép),
                    // không tin bất kỳ giá trị Run nào tồn tại nhưng đã lỗi thời.
                    string registeredPath = registered.Trim();
                    if (registeredPath.StartsWith("\"", StringComparison.Ordinal))
                    {
                        int closeQuote = registeredPath.IndexOf('"', 1);
                        if (closeQuote < 2) return false;
                        string options = registeredPath.Substring(closeQuote + 1).Trim();
                        // Cho phép khóa cũ chỉ có đường dẫn hoặc khóa mới có --autostart.
                        if (options.Length != 0 &&
                            !string.Equals(options, "--autostart", StringComparison.OrdinalIgnoreCase))
                            return false;
                        registeredPath = registeredPath.Substring(1, closeQuote - 1);
                    }

                    string actualPath = Path.GetFullPath(Application.ExecutablePath);
                    return string.Equals(Path.GetFullPath(registeredPath), actualPath,
                        StringComparison.OrdinalIgnoreCase) && File.Exists(actualPath);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Giữ overload cũ để không làm hỏng các thành phần đang gọi.
        public static bool ApplyAutoStart(bool on)
        {
            string error;
            return ApplyAutoStart(on, out error);
        }

        /// <summary>
        /// Bật: tạo/cập nhật HKCU\...\Run trỏ tới đường dẫn EXE tuyệt đối.
        /// Tắt: xóa riêng giá trị của ứng dụng, không sửa startup ứng dụng khác.
        /// Luôn đọc lại Registry sau thao tác; trả lỗi để giao diện thông báo.
        /// </summary>
        public static bool ApplyAutoStart(bool on, out string error)
        {
            error = null;
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                error = "Chức năng này chỉ hỗ trợ Windows.";
                return false;
            }

            try
            {
                if (on)
                {
                    string exePath = Path.GetFullPath(Application.ExecutablePath);
                    if (!File.Exists(exePath))
                    {
                        error = "Không tìm thấy tệp thực thi để đăng ký: " + exePath;
                        return false;
                    }
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                    {
                        if (key == null)
                        {
                            error = "Không thể mở hoặc tạo khóa tự khởi động trong Registry.";
                            return false;
                        }
                        key.SetValue(RunValueName, "\"" + exePath + "\" --autostart", RegistryValueKind.String);
                        key.Flush();
                    }
                    if (!IsAutoStartEnabled())
                    {
                        error = "Windows chưa xác nhận đường dẫn tự khởi động. Hãy kiểm tra quyền Registry và vị trí tệp EXE.";
                        return false;
                    }
                }
                else
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue(RunValueName, false);
                            key.Flush();
                        }
                    }
                    // Trường hợp tắt: phải kiểm tra thực sự đã xóa giá trị, kể cả
                    // khi giá trị cũ trỏ tới EXE khác.
                    using (RegistryKey read = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                    {
                        if (read != null && read.GetValue(RunValueName) != null)
                        {
                            error = "Giá trị tự khởi động vẫn còn tồn tại trong Registry.";
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                error = "Windows từ chối quyền chỉnh sửa mục khởi động: " + ex.Message;
            }
            catch (System.Security.SecurityException ex)
            {
                error = "Không có quyền truy cập Registry: " + ex.Message;
            }
            catch (IOException ex)
            {
                error = "Không thể ghi thông tin khởi động: " + ex.Message;
            }
            catch (Exception ex)
            {
                error = "Không thể thay đổi thiết lập khởi động: " + ex.Message;
            }
            return false;
        }

    }
}
