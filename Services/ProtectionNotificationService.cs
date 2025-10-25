using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ScanAndRemoveVirus.Services
{
    internal enum ProtectionNoticeLevel { Info, Success, Warning, Error }

    internal sealed class ProtectionNotice
    {
        internal string Title;
        internal string Message;
        internal ProtectionNoticeLevel Level;
        internal DateTime UtcTime;
    }

    /// <summary>
    /// Thông báo không chặn luồng quét. Dedupe theo sự kiện để FileSystemWatcher
    /// không tạo hàng trăm thông báo khi một tệp ghi thành nhiều lần.
    /// </summary>
    internal static class ProtectionNotificationService
    {
        internal static event Action<ProtectionNotice> Received;
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, DateTime> LastByKey =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        internal static void Publish(string title, string message, ProtectionNoticeLevel level = ProtectionNoticeLevel.Info,
            string key = null, int cooldownSeconds = 8, bool explicitUserAction = false)
        {
            // Không làm gián đoạn hay làm hỏng engine vì lỗi thông báo.
            try
            {
                if (!explicitUserAction && !FeatureFlags.ThreatAlerts) return;
                string dedupeKey = key ?? title + ":" + message;
                DateTime now = DateTime.UtcNow;
                lock (Sync)
                {
                    DateTime prior;
                    if (LastByKey.TryGetValue(dedupeKey, out prior) &&
                        (now - prior).TotalSeconds < cooldownSeconds) return;
                    LastByKey[dedupeKey] = now;
                    if (LastByKey.Count > 512)
                    {
                        var expired = new List<string>();
                        foreach (var entry in LastByKey)
                            if ((now - entry.Value).TotalMinutes > 10) expired.Add(entry.Key);
                        foreach (string stale in expired) LastByKey.Remove(stale);
                    }
                }
                Action<ProtectionNotice> handlers = Received;
                if (handlers == null) return;
                var notice = new ProtectionNotice { Title = title ?? "Antivirus", Message = message ?? "",
                    Level = level, UtcTime = now };
                foreach (Action<ProtectionNotice> handler in handlers.GetInvocationList())
                {
                    try { handler(notice); }
                    catch (Exception ex) { Trace.WriteLine("UI notification: " + ex.Message); }
                }
            }
            catch (Exception ex) { Trace.WriteLine("Publish notification: " + ex.Message); }
        }
    }
}
