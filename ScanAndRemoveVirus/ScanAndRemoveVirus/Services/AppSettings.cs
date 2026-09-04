using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ScanAndRemoveVirus.Services
{
    public class SettingsFlags
    {
        public bool AutoStart;        // chạy cùng Windows (đăng ký thật vào HKCU\...\Run)
        public bool AutoUpdate;       // row "Tự động cập nhật"
        public bool SendSamples;
        public bool ShowNotifications = true; // row "Cảnh báo mối đe dọa" (pop-up)
        public bool RealTimeOnPersist;
        public bool FileRestoreGuard = true;
        public bool UsbProtection = true;
        public bool DownloadProtection = true;
        public bool BehaviorWatch = true;
        public bool StartupFoldersWatch = true;
        public bool VtAutoQuery;
    }

    /// <summary>
    /// Cài đặt ứng dụng: <solution>\AppData\settings.ini (xem DataDir)
    /// Riêng "Chạy cùng Windows" được áp dụng THẬT vào khóa Run của HKCU (không cần admin).
    /// </summary>
    public static class AppSettings
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "ScanAndRemoveVirus";

        public static string SettingsPath
        {
            get { return DataDir.Resolve("settings.ini"); }
        }

        public static SettingsFlags Load()
        {
            var f = new SettingsFlags();
            try
            {
                if (!File.Exists(SettingsPath)) return f;
                foreach (string line in File.ReadAllLines(SettingsPath))
                {
                    string[] kv = line.Split('=');
                    if (kv.Length != 2) continue;
                    bool v;
                    if (!bool.TryParse(kv[1].Trim(), out v)) continue;
                    switch (kv[0].Trim())
                    {
                        case "AutoStart": f.AutoStart = v; break;
                        case "AutoUpdate": f.AutoUpdate = v; break;
                        case "SendSamples": f.SendSamples = v; break;
                        case "ShowNotifications": f.ShowNotifications = v; break;
                        case "RealTimeOn": f.RealTimeOnPersist = v; break;
                        case "FileRestoreGuard": f.FileRestoreGuard = v; break;
                        case "UsbProtection": f.UsbProtection = v; break;
                        case "DownloadProtection": f.DownloadProtection = v; break;
                        case "BehaviorWatch": f.BehaviorWatch = v; break;
                        case "StartupFoldersWatch": f.StartupFoldersWatch = v; break;
                        case "VtAutoQuery": f.VtAutoQuery = v; break;
                    }
                }
            }
            catch (Exception) { }
            return f;
        }

        public static void Save(SettingsFlags f)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            File.WriteAllLines(SettingsPath, new[]
            {
                "AutoStart=" + f.AutoStart,
                "AutoUpdate=" + f.AutoUpdate,
                "SendSamples=" + f.SendSamples,
                "ShowNotifications=" + f.ShowNotifications,
                "RealTimeOn=" + f.RealTimeOnPersist,
                "FileRestoreGuard=" + f.FileRestoreGuard,
                "UsbProtection=" + f.UsbProtection,
                "DownloadProtection=" + f.DownloadProtection,
                "BehaviorWatch=" + f.BehaviorWatch,
                "StartupFoldersWatch=" + f.StartupFoldersWatch,
                "VtAutoQuery=" + f.VtAutoQuery
            });
        }

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                    return key != null && key.GetValue(RunValueName) != null;
            }
            catch (Exception) { return false; }
        }

        /// <summary>Ghi/xóa giá trị Run HKCU. Trả về false nếu registry từ chối (kể lại UI).</summary>
        public static bool ApplyAutoStart(bool on)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key == null) return false;
                    if (on)
                        key.SetValue(RunValueName, "\"" + Application.ExecutablePath + "\"");
                    else if (key.GetValue(RunValueName) != null)
                        key.DeleteValue(RunValueName);
                }
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
